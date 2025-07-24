using Dapper;
using System.Data;
using System.Text.Json;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class DomainsRepository : IDomainsRepository
    {
        readonly IDbConnection _dbConnection;

        public DomainsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        #region "Add"
        public int Add(string domain, string title = "", int parentId = 0, int type = 0)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_Add @domain=@domain, @title=@title, @parentId=@parentId, @type=@type", new { domain, title, parentId, type });
        }
        #endregion

        #region "Get"

        public List<Domain> GetList(int[] subjectIds = null, DomainFilterType type = DomainFilterType.All, DomainType domainType = DomainType.unused, DomainType domainType2 = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "", string search = "", int start = 1, int length = 50, int parentId = -1)
        {
            return _dbConnection.Query<Domain>("EXEC Domains_GetList @subjectIds=@subjectIds, @lang=@lang, @search=@search, @type=@type, @domainType=@domainType, @domainType2=@domainType2, @sort=@sort, @start=@start, @length=@length, @parentId=@parentId", 
                new { subjectIds = subjectIds?.Length > 0 ? string.Join(",", subjectIds) : "", lang, search, type, domainType, domainType2, sort, start, length, parentId }).ToList();
        }

        public int GetCount(int[] subjectIds = null, DomainFilterType type = DomainFilterType.All, DomainType domainType = DomainType.unused, DomainType domainType2 = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "", string search = "", int parentId = -1)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domains_GetCount @subjectIds=@subjectIds, @lang=@lang, @search=@search, @type=@type, @domainType=@domainType, @domainType2=@domainType2, @sort=@sort, @parentId=@parentId", 
                new { subjectIds = subjectIds?.Length > 0 ? string.Join(",", subjectIds) : "", lang, search, type, domainType, domainType2, sort, parentId });
        }

        public Domain GetInfo(string domain)
        {
            return _dbConnection.QueryFirstOrDefault<Domain>("EXEC Domain_GetInfo @domain=@domain", new { domain });
        }

        public Domain GetById(int domainId)
        {
            return _dbConnection.QueryFirstOrDefault<Domain>("EXEC Domain_GetById @domainId=@domainId", new { domainId });
        }

        #endregion

        #region "Links"
        public List<Domain> GetLinks(int domainId)
        {
            return _dbConnection.Query<Domain>("EXEC DomainLinks_GetList @domainId=@domainId", new { domainId }).ToList();
        }
        #endregion

        #region "Analyzer Rules"
        public int AddAnalyzerRule(int domainId, string selector, bool rule)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_AnalyzerRule_Add @domainId=@domainId, @selector=@selector, @rule=@rule", new { domainId, selector, rule });
        }

        public List<AnalyzerRule> GetAnalyzerRules(int domainId)
        {
            return _dbConnection.Query<AnalyzerRule>("EXEC Domain_AnalyzerRules_GetList @domainId=@domainId", new { domainId }).ToList();
        }

        public void RemoveAnalyzerRule(int ruleId)
        {
            _dbConnection.Execute("EXEC Domain_AnalyzerRule_Remove @ruleId=@ruleId", new { ruleId });
        }
        #endregion

        #region "Download Rules"
        public int AddDownloadRule(int domainId, bool rule, string url, string title, string summary)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_DownloadRule_Add @domainId=@domainId, @rule=@rule, @url=@url, @title=@title, @summary=@summary", new { domainId, rule, url, title, summary });
        }

        public List<DownloadRule> GetDownloadRules(int domainId)
        {
            return _dbConnection.Query<DownloadRule>("EXEC Domain_DownloadRules_GetList @domainId=@domainId", new { domainId }).ToList();
        }

        public List<DownloadRule> GetDownloadRulesForDomains(string[] domains)
        {
            return _dbConnection.Query<DownloadRule>("EXEC Domain_DownloadRules_GetForDomains @domains=@domains", new { domains = string.Join(",", domains) }).ToList();
        }

        public void RemoveDownloadRule(int ruleId)
        {
            _dbConnection.Execute("EXEC Domain_DownloadRule_Remove @ruleId=@ruleId", new { ruleId });
        }
        #endregion

        #region "Clean"
        public CleanDownload GetDownloadsToClean(int domainId, bool topten = false)
        {
            using var gridReader = _dbConnection.QueryMultiple("EXEC Domain_GetDownloadsToClean @domainId=@domainId, @topten=@topten", new { domainId, topten });
            
            var clean = new CleanDownload();
            clean.totalArticles = gridReader.ReadFirst<int>();
            clean.articles = gridReader.Read<Article>().ToList();
            clean.totalDownloads = gridReader.ReadFirst<int>();
            return clean;
        }

        public void CleanDownloads(int domainId)
        {
            _dbConnection.Execute("EXEC Domain_CleanDownloads @domainId=@domainId", new { domainId });
        }
        #endregion

        #region "Delete"
        public void DeleteAllArticles(int domainId)
        {
            _dbConnection.Execute("EXEC Domain_DeleteAllArticles @domainId=@domainId", new { domainId });
        }

        public void Delete(int domainId)
        {
            _dbConnection.Execute("EXEC Domain_Delete @domainId=@domainId", new { domainId });
        }
        #endregion

        #region "Update"
        public void RequireSubscription(int domainId, bool required)
        {
            _dbConnection.Execute("EXEC Domain_RequireSubscription @domainId=@domainId, @required=@required", new { domainId, required });
        }

        public void HasFreeContent(int domainId, bool free)
        {
            _dbConnection.Execute("EXEC Domain_HasFreeContent @domainId=@domainId, @free=@free", new { domainId, free });
        }

        public string FindDomainTitle(int domainId)
        {
            return _dbConnection.ExecuteScalar<string>("EXEC Domain_FindTitle @domainId=@domainId", new { domainId });
        }

        public string FindDescription(int domainId)
        {
            return _dbConnection.ExecuteScalar<string>("EXEC Domain_FindDescription @domainId=@domainId", new { domainId });
        }

        public void UpdateInfo(int domainId, string title, string description, string lang)
        {
            _dbConnection.Execute("EXEC Domain_UpdateInfo @domainId=@domainId, @title=@title, @description=@description, @lang=@lang", new { domainId, title, description, lang });
        }

        public void UpdateDomainType(int domainId, DomainType type)
        {
            _dbConnection.Execute("EXEC Domain_UpdateType @domainId=@domainId, @type=@type", new { domainId, type = (int)type });
        }

        public void UpdateDomainType2(int domainId, DomainType type)
        {
            _dbConnection.Execute("EXEC Domain_UpdateType2 @domainId=@domainId, @type=@type", new { domainId, type = (int)type });
        }

        public void UpdateLanguage(int domainId, string lang)
        {
            _dbConnection.Execute("EXEC Domain_UpdateLanguage @domainId=@domainId, @lang=@lang", new { domainId, lang });
        }

        public void UpdateHttpsWww(int domainId, bool https, bool www)
        {
            _dbConnection.Execute("EXEC Domain_UpdateHttpsWww @domainId=@domainId, @https=@https, @www=@www", new { domainId, https, www });
        }

        public void IsEmpty(int domainId, bool empty)
        {
            _dbConnection.Execute("EXEC Domain_IsEmpty @domainId=@domainId, @empty=@empty", new { domainId, empty });
        }

        public void IsDeleted(int domainId, bool delete)
        {
            _dbConnection.Execute("EXEC Domain_IsDeleted @domainId=@domainId, @delete=@delete", new { domainId, delete });
        }
        #endregion

        #region "Collections"
        public int AddCollection(int colgroupId, string name, string search = "", int subjectId = 0, DomainFilterType filtertype = DomainFilterType.All, DomainType type = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "")
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_Collection_Add @colgroupId=@colgroupId, @name=@name, @search=@search, @subjectId=@subjectId, @filtertype=@filtertype, @type=@type, @sort=@sort, @lang=@lang", 
                new { colgroupId, name, search, subjectId, filtertype, type, sort, lang });
        }

        public int AddCollection(DomainCollection collection)
        {
            return AddCollection(collection.colgroupId, collection.name, collection.search, collection.subjectId, collection.filtertype, collection.type, collection.sort, collection.lang);
        }

        public DomainCollectionsAndGroups GetCollectionsList()
        {
            using var gridReader = _dbConnection.QueryMultiple("EXEC Domain_Collections_GetList");
            
            var collections = gridReader.Read<DomainCollection>().ToList();
            var groups = gridReader.Read<CollectionGroup>().ToList();
            return new DomainCollectionsAndGroups()
            {
                Collections = collections,
                Groups = groups
            };
        }

        public int RemoveCollection(int colId)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_Collection_Remove @colId=@colId", new { colId });
        }
        #endregion

        #region "Collection Groups"
        public int AddCollectionGroup(string name)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_CollectionGroup_Add @name=@name", new { name });
        }

        public int RemoveCollectionGroup(int colgroupId)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Domain_CollectionGroup_Remove @colgroupId=@colgroupId", new { colgroupId });
        }

        public List<CollectionGroup> GetCollectionGroupsList()
        {
            return _dbConnection.Query<CollectionGroup>("EXEC Domain_CollectionGroups_GetList").ToList();
        }
        #endregion

        #region "DomainTypeMatches"
        public int AddDomainTypeMatch(List<DomainTypeMatchPart> parts, int type, int type2, int threshold, int rank)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC DomainTypeMatches_Add @words=@words, @type=@type, @type2=@type2, @threshold=@threshold, @rank=@rank", 
                new { words = JsonSerializer.Serialize(parts), type, type2, threshold, rank });
        }

        public int RemoveDomainTypeMatch(int matchId)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC DomainTypeMatches_Remove @matchId=@matchId", new { matchId });
        }

        public List<DomainTypeMatch> GetDomainTypeMatchesList()
        {
            var result = _dbConnection.Query<DomainTypeMatch>("EXEC DomainTypeMatches_GetList").ToList();
            foreach (var elem in result)
            {
                elem.parts = JsonSerializer.Deserialize<List<DomainTypeMatchPart>>(elem.words);
            }
            return result;
        }
        #endregion
    }
}
