using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IWhitelistsRepository
    {
        List<string> GetDomainsList();
        void AddDomain(string domain);
        void RemoveDomain(string domain);
        bool CheckDomain(string domain);
    }
}
