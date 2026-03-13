using System.Reflection;
using Serilog;
using Serilog.Events;
using Collector.Common;
using Collector.Auth.Services;
using Collector.API.Services;
using Collector.Web.Server.Workers;
using Microsoft.AspNetCore.SignalR;

using Microsoft.AspNetCore.Http.Features;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

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
        );
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            // Allow localhost origins and chrome-extension:// origins
            // Using SetIsOriginAllowed to support dynamic chrome extension IDs
            policy.SetIsOriginAllowed(origin => 
                origin.StartsWith("chrome-extension://") || 
                origin.StartsWith("http://localhost") ||
                origin.StartsWith("https://localhost"))
                .AllowAnyHeader()
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
builder.AddAuthService();

// Register workers
builder.Services.AddTransient<VideoWorker>();
builder.Services.AddTransient<ChatWorker>();

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

//load LLM keys
foreach(var llm in Collector.Common.LLMs.Available)
{
    llm.Value.PrivateKey = builder.Configuration["LLM:" + llm.Key + ":PrivateKey"] ?? "";
}

// Configure Ollama for local LLM reasoning
LLMOllama.Url = builder.Configuration["Ollama:Url"] ?? "http://localhost:11434";
LLMOllama.Model = builder.Configuration["Ollama:Model"] ?? "qwen2.5:0.5b";
LLMOllama.UseGpu = builder.Configuration.GetValue<bool>("Ollama:UseGpu", false);
LLMOllama.NumGpu = builder.Configuration.GetValue<int>("Ollama:NumGpu", 1);
LLMOllama.ContextSize = builder.Configuration.GetValue<int>("Ollama:ContextSize", 2048); //2048 (96MB), 4096 (192MB), 8192 (384MB), or 32768 (1.5GB)
LLMOllama.Temperature = builder.Configuration.GetValue<float>("Ollama:Temperature", 0.7f);
LLMOllama.AutoPullModel = builder.Configuration.GetValue<bool>("Ollama:AutoPullModel", true);

builder.Services.AddSingleton(sp => new OllamaSharp.OllamaApiClient(LLMOllama.Url));

Console.WriteLine($"Ollama Configuration:");
Console.WriteLine($"  URL: {LLMOllama.Url}");
Console.WriteLine($"  Model: {LLMOllama.Model}");
Console.WriteLine($"  GPU: {(LLMOllama.UseGpu ? $"Enabled ({LLMOllama.NumGpu} GPU(s))" : "Disabled")}");
Console.WriteLine($"  Context Size: {LLMOllama.ContextSize}");
Console.WriteLine($"  Temperature: {LLMOllama.Temperature}");

if (LLMOllama.AutoPullModel)
{
    try
    {
        var tempOllama = new OllamaSharp.OllamaApiClient(LLMOllama.Url);
        var models = await tempOllama.ListLocalModelsAsync();
        
        // Check and pull main chat model
        var modelExists = models.Any(m => m.Name == LLMOllama.Model);
        if (!modelExists)
        {
            Console.WriteLine($"\n{LLMOllama.Model} model not found. Pulling from Ollama...");
            Console.WriteLine("This may take several minutes depending on your connection...");
            var pullRequest = new OllamaSharp.Models.PullModelRequest { Model = LLMOllama.Model };
            await foreach (var status in tempOllama.PullModelAsync(pullRequest))
            {
                if (status?.Status != null)
                {
                    Console.WriteLine($"  {status.Status}");
                }
            }
            Console.WriteLine($"{LLMOllama.Model} model pulled successfully.\n");
        }
        else
        {
            Console.WriteLine($"{LLMOllama.Model} model is already available.\n");
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
        Console.WriteLine($"\n⚠️  WARNING: Could not connect to Ollama at {LLMOllama.Url}");
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

// Map SignalR hubs BEFORE static files and controllers
app.MapHub<TextEditorHub>("/text-editor");
app.MapHub<VideoHub>("/video-download");
app.MapHub<WebContentHub>("/web-content");
app.MapHub<WorkerHub>("/worker");
app.MapHub<ChromeExtensionHub>("/chrome-extension");

// Register worker routes
WorkerRoutes.Register<VideoWorker>("video-worker");
WorkerRoutes.Register<ChatWorker>("chat-worker");

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

app.Run();