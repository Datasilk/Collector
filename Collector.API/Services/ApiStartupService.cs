using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Collector.Data.Services;

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
        }
    }
}
