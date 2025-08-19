using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Collector.Data.Services;
using Collector.Auth.Services;

namespace Collector.API.Services
{
    public static class ApiStartupService
    {
        public static void AddApiStartupService(this WebApplicationBuilder builder)
        {
            //HttpClientFactory
            builder.Services.AddHttpClient();

            //If Using Dapper use this setup
            builder.AddDapperStartupService();

            //Dependency Injection
            builder.Services.AddScoped<IAuthEmailService, AuthEmailService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
        }
    }
}
