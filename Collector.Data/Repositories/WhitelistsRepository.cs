using System.Collections.Generic;
using System.Data;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class WhitelistsRepository : IWhitelistsRepository
    {
        private readonly IDbConnection _dbConnection;

        public WhitelistsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public List<string> GetDomainsList()
        {
            return _dbConnection.Query<string>("EXEC Whitelist_Domains_GetList").AsList();
        }

        public void AddDomain(string domain)
        {
            _dbConnection.Execute("EXEC Whitelist_Domain_Add @domain=@domain", new { domain });
        }

        public void RemoveDomain(string domain)
        {
            _dbConnection.Execute("EXEC Whitelist_Domain_Remove @domain=@domain", new { domain });
        }

        public bool CheckDomain(string domain)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Whitelist_Domain_Check @domain=@domain", new { domain }) > 0;
        }
    }
}
