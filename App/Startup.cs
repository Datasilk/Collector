﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Collector.SignalR.Hubs;
using Microsoft.Extensions.DependencyInjection;

public class Startup : Datasilk.Startup
{

    public override void ConfiguringServices(IServiceCollection services)
    {
        //use SignalR service
        services.AddSignalR();
    }

    public override void Configured(IApplicationBuilder app, IHostingEnvironment env, IConfigurationRoot config)
    {
        base.Configured(app, env, config);
        Query.Sql.connectionString = Server.sqlConnectionString;
        var resetPass = Query.Users.HasPasswords();
        Server.hasAdmin = Query.Users.HasAdmin();
        Server.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        Server.Cache.Add("browserPath", config.GetSection("browser:path").Value);

        //set up SignalR hubs
        app.UseSignalR(routes =>
        {
            routes.MapHub<ArticleHub>("/articlehub");
        });
    }
}
