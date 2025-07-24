using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface IBlacklistsRepository
    {
        // Domains
        List<string> GetDomainsList();
        void AddDomain(string domain);
        void RemoveDomain(string domain);
        bool CheckDomain(string domain);
        List<Blacklist> CheckAllDomains(string[] domains);

        // Wildcards
        List<string> GetWildcardsList();
        void AddWildcard(string domain);
        void RemoveWildcard(string domain);
    }
}
