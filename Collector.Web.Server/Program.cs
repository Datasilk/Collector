using System.Data;
using System.Reflection;
using System.Net.NetworkInformation;
using Serilog;
using Serilog.Events;
using Collector.Common;
using Collector.Auth.Services;
using Collector.API.Services;
using Collector.Web.Server.SignalR;
using Collector.Workers;
using Collector.Workers.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((host, config) =>
{
    config
        .Enrich.FromLogContext()
        // Write to console but filter out all Microsoft.* logs
        .WriteTo.Logger(lc => lc
            .Filter.ByExcluding(le =>
                le.Properties.TryGetValue("SourceContext", out var sc)
                && sc is ScalarValue sv
                && sv.Value is string s
                && s.StartsWith("Microsoft"))
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
        )
        // Write all logs to file with rolling daily files
        .WriteTo.File(
            path: "logs/collector-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 104857600, // 100MB
            rollOnFileSizeLimit: true
        );
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            // Allow localhost, chrome-extension, and any HTTPS origins for mobile debugging
            // Using SetIsOriginAllowed to support dynamic origins (chrome extensions, Tailscale IPs, etc.)
            policy.SetIsOriginAllowed(origin =>
                origin.StartsWith("chrome-extension://") ||
                origin.StartsWith("http://localhost") ||
                origin.StartsWith("https://localhost") ||
                origin.StartsWith("https://100.64.0") || // Allow any HTTPS origin for mobile/Tailscale debugging
                origin.StartsWith("http://100.64.0") || // Allow any HTTP origin for development
                origin.StartsWith("https://collector-dev-local.datasilk.io") //allow sub domains used for testing collector
                ).AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

// Add SignalR services with increased message size for cache support
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB (default is 32KB)
});

builder.Services.AddControllers()
    .AddApplicationPart(Assembly.Load("Collector.API"))
    .AddApplicationPart(Assembly.Load("Collector.Auth"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

//Initilaize Collector Services
builder.AddApiStartupService();

// Configure Charlotte Router endpoint
var charlotteConfig = builder.Configuration.GetSection("Charlotte");
var routerEndpoint = charlotteConfig["RouterEndpoint"];
if (!string.IsNullOrEmpty(routerEndpoint))
{
    Charlotte.RouterEndpoint = routerEndpoint;
    Console.WriteLine($"Charlotte Router configured: {routerEndpoint}");
}
builder.AddAuthService();

// Register workers
builder.Services.AddTransient<VideoWorker>();
builder.Services.AddTransient<YoutubeChannelWorker>();
builder.Services.AddTransient<YouTubeEntryCreator>();
builder.Services.AddTransient<ChatWorker>();
builder.Services.AddTransient<DownloadWorker>();
builder.Services.AddTransient<DomainWorker>();

// Configure request limits for large file uploads (5GB for video files)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 5368709120; // 5 GB
});
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 5368709120; // 5 GB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 5368709120; // 5 GB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

//Add Swagger
builder.Services.AddSwaggerGen(e =>
{
    e.DescribeAllParametersInCamelCase();
    e.SchemaFilter<Collector.API.Swagger.EnumSchemaFilter>();
});

// Configure Ollama for local LLM reasoning
Collector.AI.Ollama.Config.Url = builder.Configuration["Ollama:Url"] ?? "http://localhost:11434";
Collector.AI.Ollama.Config.Model = builder.Configuration["Ollama:Model"] ?? "qwen2.5:0.5b";
Collector.AI.Ollama.Config.UseGpu = builder.Configuration.GetValue<bool>("Ollama:UseGpu", false);
Collector.AI.Ollama.Config.NumGpu = builder.Configuration.GetValue<int>("Ollama:NumGpu", 1);
Collector.AI.Ollama.Config.ContextSize = builder.Configuration.GetValue<int>("Ollama:ContextSize", 2048); //2048 (96MB), 4096 (192MB), 8192 (384MB), or 32768 (1.5GB)
Collector.AI.Ollama.Config.Temperature = builder.Configuration.GetValue<float>("Ollama:Temperature", 0.7f);
Collector.AI.Ollama.Config.AutoPullModel = builder.Configuration.GetValue<bool>("Ollama:AutoPullModel", true);

builder.Services.AddSingleton(sp => new OllamaSharp.OllamaApiClient(Collector.AI.Ollama.Config.Url));

Console.WriteLine($"Ollama Configuration:");
Console.WriteLine($"  URL: {Collector.AI.Ollama.Config.Url}");
Console.WriteLine($"  Model: {Collector.AI.Ollama.Config.Model}");
Console.WriteLine($"  GPU: {(Collector.AI.Ollama.Config.UseGpu ? $"Enabled ({Collector.AI.Ollama.Config.NumGpu} GPU(s))" : "Disabled")}");
Console.WriteLine($"  Context Size: {Collector.AI.Ollama.Config.ContextSize}");
Console.WriteLine($"  Temperature: {Collector.AI.Ollama.Config.Temperature}");

if (Collector.AI.Ollama.Config.AutoPullModel)
{
    try
    {
        var tempOllama = new OllamaSharp.OllamaApiClient(Collector.AI.Ollama.Config.Url);
        var models = await tempOllama.ListLocalModelsAsync();

        // Check and pull main chat model
        var modelExists = models.Any(m => m.Name == Collector.AI.Ollama.Config.Model);
        if (!modelExists)
        {
            Console.WriteLine($"\n{Collector.AI.Ollama.Config.Model} model not found. Pulling from Ollama...");
            Console.WriteLine("This may take several minutes depending on your connection...");
            var pullRequest = new OllamaSharp.Models.PullModelRequest { Model = Collector.AI.Ollama.Config.Model };
            await foreach (var status in tempOllama.PullModelAsync(pullRequest))
            {
                if (status?.Status != null)
                {
                    Console.WriteLine($"  {status.Status}");
                }
            }
            Console.WriteLine($"{Collector.AI.Ollama.Config.Model} model pulled successfully.\n");
        }
        else
        {
            Console.WriteLine($"{Collector.AI.Ollama.Config.Model} model is already available.\n");
        }

        // Check and pull embedding model for RAG
        var embeddingModelExists = models.Any(m => m.Name == "nomic-embed-text");
        if (!embeddingModelExists)
        {
            Console.WriteLine("\nnomic-embed-text embedding model not found. Pulling from Ollama...");
            Console.WriteLine("This model is required for RAG (Retrieval-Augmented Generation)...");
            var pullRequest = new OllamaSharp.Models.PullModelRequest { Model = "nomic-embed-text" };
            await foreach (var status in tempOllama.PullModelAsync(pullRequest))
            {
                if (status?.Status != null)
                {
                    Console.WriteLine($"  {status.Status}");
                }
            }
            Console.WriteLine("nomic-embed-text model pulled successfully.\n");
        }
        else
        {
            Console.WriteLine("nomic-embed-text embedding model is already available.\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n⚠️  WARNING: Could not connect to Ollama at {Collector.AI.Ollama.Config.Url}");
        Console.WriteLine($"Error: {ex.Message}\n");
        Console.WriteLine("To use the AI chat feature, you need to:");
        Console.WriteLine("1. Download Ollama from: https://ollama.ai/download");
        Console.WriteLine("2. Install and run Ollama");
        Console.WriteLine("3. Restart the Collector server application\n");
        Console.WriteLine("The application will continue to run, but AI chat will not work until Ollama is running.\n");
    }
}

// Set all file storage paths from configuration
Files.ArticlesPath = builder.Configuration["Storage:Articles"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "articles");
Files.FilesPath = builder.Configuration["Storage:Files"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "files");
Files.ImagesPath = builder.Configuration["Storage:Images"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "images");
Files.JournalPath = builder.Configuration["Storage:Journal"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "journal-entries");
Files.VideosPath = builder.Configuration["Storage:Videos"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "videos");

// Download required tools (ffmpeg, ffprobe, yt-dlp) if not already present
var downloadToolsScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "download-tools.bat");
if (File.Exists(downloadToolsScript))
{
    try
    {
        Console.WriteLine($"Running download-tools.bat...");
        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = downloadToolsScript,
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process != null)
        {
            process.WaitForExit();

            // Add application base directory to PATH for this process so it can access the tools
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            Environment.SetEnvironmentVariable("PATH", $"{baseDir};{currentPath}");
            Console.WriteLine($"Finished running download-tools.bat");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to run download-tools.bat: {ex.Message}");
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/server-error");
    app.UseStatusCodePagesWithReExecute("/error");
    app.UseHsts();
    app.UseHealthChecks("/healthcheck");
}

//Add Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Collector API v1");
    });
}

app.UseCors();
//app.UseHttpsRedirection();
app.UseRouting();
app.Use(async (context, next) =>
{
    const string tokenCookieName = "collector_token";
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        if (context.Request.Cookies.TryGetValue(tokenCookieName, out var token) && !string.IsNullOrEmpty(token))
        {
            context.Request.Headers["Authorization"] = $"Bearer {token}";
        }
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();

// Reset all PostgreSQL sequences to MAX(id) + 1 before starting real-time services
try
{
    using var scope = app.Services.CreateScope();
    var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "SELECT public.\"ResetAllSequences\"()";
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
        command.ExecuteNonQuery();
    }
    Console.WriteLine("PostgreSQL sequences reset successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to reset PostgreSQL sequences: {ex.Message}");
}

// Load enabled LLM models from PostgreSQL into the OpenAI registry
try
{
    using var scope = app.Services.CreateScope();
    var llmRepo = scope.ServiceProvider.GetRequiredService<Collector.Data.Interfaces.ILLMModelsRepository>();
    var models = llmRepo.GetAll();
    foreach (var model in models)
    {
        if (!model.Enabled) continue;
        Collector.AI.OpenAI.AddModel(new Collector.AI.Models.LLMModel
        {
            ModelId = model.ModelId,
            Name = model.Name,
            Model = model.Model,
            Endpoint = model.Endpoint,
            PrivateKey = model.PrivateKey,
            Type = model.Type,
            Enabled = model.Enabled,
            Preferred = model.Preferred,
            ExtraBody = string.IsNullOrWhiteSpace(model.ExtraBody)
                ? new System.Collections.Generic.Dictionary<string, object>()
                : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(model.ExtraBody)
        });
    }
    var preferred = models.FirstOrDefault(m => m.Enabled && m.Preferred);
    if (preferred != null)
    {
        Collector.AI.OpenAI.PreferredModel = preferred.ModelId;
    }
    Console.WriteLine($"Loaded {Collector.AI.OpenAI.Available.Count} enabled LLM model(s) from PostgreSQL.");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to load LLM models from PostgreSQL: {ex.Message}");
}

// Map SignalR hubs BEFORE static files and controllers
app.MapHub<TextEditorHub>("/text-editor");
app.MapHub<VideoHub>("/video-download");
app.MapHub<WebContentHub>("/web-content");
app.MapHub<WorkerHub>("/worker");
app.MapHub<ChromeExtensionHub>("/chrome-extension");

// Register worker routes
WorkerRoutes.Register<VideoWorker>("video-worker");
WorkerRoutes.Register<YoutubeChannelWorker>("youtube-channel-worker");
WorkerRoutes.Register<ChatWorker>("chat-worker");
WorkerRoutes.Register<DownloadWorker>("download-worker");
WorkerRoutes.Register<DomainWorker>("domain-worker");

// Set WorkerHub context for worker-to-client communication
Workers.SetHubContext(app.Services.GetRequiredService<IHubContext<WorkerHub>>());

// Configure static files with SVG support
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".svg"] = "image/svg+xml";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

// Map controllers
app.MapControllers();

// SPA fallback to index.html for React app
app.MapFallbackToFile("index.html");

Console.WriteLine(
    "Collector Web Server {0} started.",
    typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion.Split("+")[0] ?? "unknown");

// Display all available IP addresses
Console.WriteLine("\nAvailable IP addresses:");
foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
{
    if (networkInterface.OperationalStatus == OperationalStatus.Up)
    {
        var ipProperties = networkInterface.GetIPProperties();
        foreach (var unicastAddress in ipProperties.UnicastAddresses)
        {
            if (unicastAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Console.WriteLine($"  {unicastAddress.Address} ({networkInterface.Description})");
            }
        }
    }
}

// Display all URLs the server is listening on
var urls = builder.Configuration.GetSection("Urls").Value;
if (!string.IsNullOrEmpty(urls))
{
    Console.WriteLine("\nListening on:");
    foreach (var url in urls.Split(';'))
    {
        Console.WriteLine($"  {url}");
    }
}
else
{
    // Fallback to checking ASPNETCORE_URLS environment variable
    var aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (!string.IsNullOrEmpty(aspnetUrls))
    {
        Console.WriteLine("\nListening on:");
        foreach (var url in aspnetUrls.Split(';'))
        {
            Console.WriteLine($"  {url}");
        }
    }
    else
    {
        // Default fallback
        Console.WriteLine("\nListening on:");
        Console.WriteLine("  http://0.0.0.0:7780");
        Console.WriteLine("  https://0.0.0.0:7781");
    }
}


app.Run();