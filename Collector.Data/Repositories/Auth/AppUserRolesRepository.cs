using Dapper;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Data.Repositories.Auth
{
    public class AppUserRolesRepository : IAppUserRolesRepository, IDisposable
    {
        private string _tableName = "";
        private string _roleTableName = "";

        readonly IDbConnection _dbConnection;
        public AppUserRolesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
            var attribute = typeof(AppUserRole).GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (attribute != null)
                _tableName = attribute.Name;
            var rattribute = typeof(AppRole).GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (rattribute != null)
                _roleTableName = rattribute.Name;
        }

        public Task AddRoleAsync(Guid userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _dbConnection?.Dispose();
        }

        public async Task<bool> HasRoleAsync(Guid userId, string roleName)
        {
            string query = $"SELECT Count(*) FROM {_tableName} WHERE AppUserId = @userId AND [Name] = @roleName";
            return await _dbConnection.ExecuteScalarAsync<bool>(query, new { userId, roleName });
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
        {
            string query = $"SELECT r.[Name] FROM {_roleTableName} r join {_tableName} ur on ur.siteroleid = r.id WHERE ur.AppUserId = @userId";
            return await _dbConnection.QueryAsync<string>(query, new { userId });
        }
    }
}
