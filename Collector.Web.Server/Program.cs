using System.Reflection;
using Serilog;
using Collector.Common;
using Collector.Auth.Services;
using Collector.API.Services;
using Microsoft.AspNetCore.Http.Features;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((host, config) => config
    .ReadFrom.Configuration(host.Configuration)
    .Enrich.FromLogContext()
);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            // For SignalR, we need to allow credentials and specify allowed origins
            // instead of AllowAnyOrigin (which doesn't work with AllowCredentials)
            policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://localhost:7126",
                "https://localhost:7783"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

// Add SignalR services
builder.Services.AddSignalR();

builder.Services.AddControllers()
    .AddApplicationPart(Assembly.Load("Collector.API"))
    .AddApplicationPart(Assembly.Load("Collector.Auth"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

//Initilaize Collector Services
builder.AddApiStartupService();
builder.AddAuthService();

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
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to run download-tools.bat: {ex.Message}");
    }
}

var app = builder.Build();

//Response Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    await next();
});

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
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hubs BEFORE static files and controllers
app.MapHub<TextEditorHub>("/text-editor");
app.MapHub<VideoHub>("/video-download");
app.MapHub<WebContentHub>("/web-content");

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

app.Run();
