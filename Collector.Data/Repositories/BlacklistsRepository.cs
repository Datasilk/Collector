using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class BlacklistsRepository : IBlacklistsRepository
    {
        readonly IDbConnection _dbConnection;

        public BlacklistsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        #region "Domains"

        public List<string> GetDomainsList()
        {
            return _dbConnection.Query<string>("SELECT * FROM public.\"Blacklist_Domains_GetList\"()").ToList();
        }

        public void AddDomain(string domain)
        {
            _dbConnection.Execute("SELECT public.\"Blacklist_Domain_Add\"(@domain)", new { domain });
        }

        public void RemoveDomain(string domain)
        {
            _dbConnection.Execute("SELECT public.\"Blacklist_Domain_Remove\"(@domain)", new { domain });
        }

        public bool CheckDomain(string domain)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Blacklist_Domain_Check\"(@domain)", new { domain }) > 0;
        }

        public List<Blacklist> CheckAllDomains(string[] domains)
        {
            return _dbConnection.Query<Blacklist>("SELECT * FROM public.\"Blacklist_Domains_CheckAll\"(@domains)", new { domains = string.Join(",", domains) }).ToList();
        }

        #endregion

        #region "Wildcards"

        public List<string> GetWildcardsList()
        {
            return _dbConnection.Query<string>("SELECT * FROM public.\"Blacklist_Wildcards_GetList\"()").ToList();
        }

        public void AddWildcard(string domain)
        {
            _dbConnection.Execute("SELECT public.\"Blacklist_Wildcard_Add\"(@domain)", new { domain });
        }

        public void RemoveWildcard(string domain)
        {
            _dbConnection.Execute("SELECT public.\"Blacklist_Wildcard_Remove\"(@domain)", new { domain });
        }

        #endregion
    }
}
