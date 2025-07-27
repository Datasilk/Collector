using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Collector.Data.Interfaces;
using Collector.Data.Interfaces.Users;
using Collector.Data.Repositories;
using Collector.Data.Repositories.Auth;

namespace Collector.Data.Services
{
    public static class DapperStartupService
    {
        public static void AddDapperStartupService(this WebApplicationBuilder builder)
        {

            //Database
            builder.Services.AddTransient<IDbConnection>((sp) => new SqlConnection(builder.Configuration.GetConnectionString("Database")));

            //Auth Tables
            builder.Services.AddTransient<IAppUserRepository, AppUserRepository>();
            builder.Services.AddTransient<IAppRoleRepository, AppRoleRepository>();
            builder.Services.AddTransient<IAppUserRolesRepository, AppUserRolesRepository>();
            builder.Services.AddTransient<IAppUserTokenRepository, AppUserTokenRepository>();

            //Tables
            builder.Services.AddTransient<IArticlesRepository, ArticlesRepository>();
            builder.Services.AddTransient<IBlacklistsRepository, BlacklistsRepository>();
            builder.Services.AddTransient<ICommonWordsRepository, CommonWordsRepository>();
            builder.Services.AddTransient<IDomainsRepository, DomainsRepository>();
            builder.Services.AddTransient<IDownloadsRepository, DownloadsRepository>();
            builder.Services.AddTransient<IFeedsRepository, FeedsRepository>();
            builder.Services.AddTransient<ISubjectsRepository, SubjectsRepository>();
            builder.Services.AddTransient<IWhitelistsRepository, WhitelistsRepository>();
            builder.Services.AddTransient<IWordsRepository, WordsRepository>();
        }
    }
}
