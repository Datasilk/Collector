using System.Reflection;
using Serilog;
using Collector.Auth.Services;
using Collector.API.Services;
using Microsoft.AspNetCore.Http.Features;
using Collector.Web.Server.SignalR;

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

// Configure request limits for large file uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1048576 * 10; // 10 MB
});
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 1048576 * 10; // 10 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 1048576 * 10; // 10 MB
    options.MultipartBodyLengthLimit = 1048576 * 10; // 10 MB
    options.MultipartHeadersLengthLimit = 1048576 * 10; // 10 MB
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
    llm.Value.PrivateKey = builder.Configuration.GetSection("LLM:" + llm.Key + ":PrivateKey").Value ?? "";
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
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<TextEditorHub>("/text-editor");

app.Run();
