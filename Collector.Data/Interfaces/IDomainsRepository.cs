using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.Data.Interfaces
{
    public interface IDomainsRepository
    {
        // Add
        int Add(string domain, string title = "", int parentId = 0, int type = 0);

        // Get
        List<Domain> GetList(int[] subjectIds = null, DomainFilterType type = DomainFilterType.All, DomainType domainType = DomainType.unused, DomainType domainType2 = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "", string search = "", int start = 1, int length = 50, int parentId = -1);
        int GetCount(int[] subjectIds = null, DomainFilterType type = DomainFilterType.All, DomainType domainType = DomainType.unused, DomainType domainType2 = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "", string search = "", int parentId = -1);
        Domain GetInfo(string domain);
        Domain GetById(int domainId);

        // Links
        List<Domain> GetLinks(int domainId);

        // Analyzer Rules
        int AddAnalyzerRule(int domainId, string selector, bool rule);
        List<AnalyzerRule> GetAnalyzerRules(int domainId);
        void RemoveAnalyzerRule(int ruleId);

        // Download Rules
        int AddDownloadRule(int domainId, bool rule, string url, string title, string summary);
        List<DownloadRule> GetDownloadRules(int domainId);
        List<DownloadRule> GetDownloadRulesForDomains(string[] domains);
        void RemoveDownloadRule(int ruleId);

        // Clean
        CleanDownload GetDownloadsToClean(int domainId, bool topten = false);
        void CleanDownloads(int domainId);

        // Delete
        void DeleteAllArticles(int domainId);
        void Delete(int domainId);

        // Update
        void RequireSubscription(int domainId, bool required);
        void HasFreeContent(int domainId, bool free);
        string FindDomainTitle(int domainId);
        string FindDescription(int domainId);
        void UpdateInfo(int domainId, string title, string description, string lang);
        void UpdateCompany(int domainId, string company);
        void UpdateDomainType(int domainId, DomainType type);
        void UpdateDomainType2(int domainId, DomainType type);
        void UpdateDomainTypes(int domainId, DomainType type, DomainType type2);
        void UpdateLanguage(int domainId, string lang);
        void UpdateHttpsWww(int domainId, bool https, bool www);
        void IsEmpty(int domainId, bool empty);
        void IsDeleted(int domainId, bool delete);

        // Collections
        int AddCollection(int colgroupId, string name, string search = "", int subjectId = 0, DomainFilterType filtertype = DomainFilterType.All, DomainType type = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "");
        int AddCollection(DomainCollection collection);
        DomainCollectionsAndGroups GetCollectionsList();
        int RemoveCollection(int colId);

        // Collection Groups
        int AddCollectionGroup(string name);
        int RemoveCollectionGroup(int colgroupId);
        List<CollectionGroup> GetCollectionGroupsList();

        // DomainTypeMatches
        int AddDomainTypeMatch(List<DomainTypeMatchPart> parts, int type, int type2, int threshold, int rank);
        int RemoveDomainTypeMatch(int matchId);
        List<DomainTypeMatch> GetDomainTypeMatchesList();

        // Domain Services
        Dictionary<string, int> GetServiceIdsByNames(string[] serviceNames);
        void AddDomainServices(int domainId, int[] serviceIds);
    }
}
