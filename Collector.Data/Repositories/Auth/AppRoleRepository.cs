using Dapper;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using System.Data;

namespace Collector.Data.Repositories.Auth
{
    public class AppRoleRepository : IAppRoleRepository, IDisposable
    {
        private string _tableName = string.Empty;
        readonly IDbConnection _dbConnection;

        public AppRoleRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void Dispose()
        {
            _dbConnection.Dispose();
        }

        public Task<AppRole> Add(AppRole role)
        {
            throw new NotImplementedException();
        }

        public Task<AppRole> Edit(AppRole role)
        {
            throw new NotImplementedException();
        }

        public Task Delete(AppRole role)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<AppRole>> GetAll()
        {
            string query = "SELECT * FROM AppRoles ORDER BY [Name]";
            return await _dbConnection.QueryAsync<AppRole>(query);
        }

        public Task<AppRole> FindById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AppRole> FindByName(string name)
        {
            string query = $"SELECT TOP 1 * FROM AppRoles WHERE [Name] = @Name";
            return await _dbConnection.QueryFirstOrDefaultAsync<AppRole>(query, new { Name = name });
        }
    }
}
