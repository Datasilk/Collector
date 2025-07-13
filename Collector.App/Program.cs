using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".Collector";
    options.Cookie.IsEssential = true;
});
builder.Services.AddSignalR();
builder.Services.AddMvc().AddRazorRuntimeCompilation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
//app.UseHttpsRedirection();

//handle static files
var provider = new FileExtensionContentTypeProvider();

// Add static file mappings
provider.Mappings[".svg"] = "image/svg";
var options = new StaticFileOptions
{
    ContentTypeProvider = provider
};
app.UseStaticFiles(options);

app.UseRouting();
app.UseAuthorization();


//check if app is running in Docker Container
Collector.App.IsDocker = System.Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

switch (app.Environment.EnvironmentName.ToLower())
{
    case "production":
        Collector.App.Environment = Collector.Environment.production;
        break;
    case "staging":
        Collector.App.Environment = Collector.Environment.staging;
        break;
    default:
        Collector.App.Environment = Collector.Environment.development;
        break;
}

//load application-wide cache
Collector.App.ConfigFilename = "config" +
    (Collector.App.IsDocker ? ".docker" : "") +
    (Collector.App.Environment == Collector.Environment.production ? ".prod" : "") + ".json";

var builtConfig = new ConfigurationBuilder()
                .AddJsonFile(Collector.App.MapPath(Collector.App.ConfigFilename))
                .AddEnvironmentVariables().Build();
builtConfig.Bind(Collector.App.Config);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<Collector.SignalR.DashboardHub>("/dashboardhub");

app.Start();

//get IP addresses for running application
var server = app.Services.GetRequiredService<IServer>();
var addressFeature = server.Features.Get<IServerAddressesFeature>();
if (addressFeature != null)
{
    foreach (var address in addressFeature.Addresses)
    {
        Console.WriteLine($"Listening to {address}");
        Collector.App.Addresses.Add(address);
    }
}

app.WaitForShutdown();