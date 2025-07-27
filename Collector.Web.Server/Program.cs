using System.Reflection;
using Serilog;
using Collector.Auth.Services;
using Collector.API.Services;
using Microsoft.AspNetCore.Http.Features;

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
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
        });
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");


app.Run();
