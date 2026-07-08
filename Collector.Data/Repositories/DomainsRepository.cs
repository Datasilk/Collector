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
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_Add\"(@domain, @title, @parentId, @type)", new { domain, title, parentId, type });
        }
        #endregion

        #region "Get"

        public List<Domain> GetList(int[] subjectIds = null, DomainFilterType type = DomainFilterType.All, DomainType domainType = DomainType.unused, DomainType domainType2 = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "", string search = "", int start = 1, int length = 50, int parentId = -1, int[] serviceIds = null)
        {
            return _dbConnection.Query<Domain>("SELECT * FROM public.\"Domains_GetList\"(@subjectIds, @lang, @search, @type, @domainType, @domainType2, @sort, @start, @length, @parentId, @serviceIds)", 
                new { subjectIds = subjectIds?.Length > 0 ? string.Join(",", subjectIds) : "", lang, search, type = (int)type, domainType = (int)domainType, domainType2 = (int)domainType2, sort = (int)sort, start, length, parentId, serviceIds = serviceIds?.Length > 0 ? string.Join(",", serviceIds) : null }).ToList();
        }

        public int GetCount(int[] subjectIds = null, DomainFilterType type = DomainFilterType.All, DomainType domainType = DomainType.unused, DomainType domainType2 = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "", string search = "", int parentId = -1, int[] serviceIds = null)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domains_GetCount\"(@subjectIds, @lang, @search, @type, @domainType, @domainType2, @sort, @parentId, @serviceIds)", 
                new { subjectIds = subjectIds?.Length > 0 ? string.Join(",", subjectIds) : "", lang, search, type = (int)type, domainType = (int)domainType, domainType2 = (int)domainType2, sort = (int)sort, parentId, serviceIds = serviceIds?.Length > 0 ? string.Join(",", serviceIds) : null });
        }

        public Domain GetInfo(string domain)
        {
            return _dbConnection.QueryFirstOrDefault<Domain>("SELECT * FROM public.\"Domain_GetInfo\"(@domain)", new { domain });
        }

        public Domain GetById(int domainId)
        {
            return _dbConnection.QueryFirstOrDefault<Domain>("SELECT * FROM public.\"Domain_GetById\"(@domainId)", new { domainId });
        }

        public Domain GetNextUnanalyzedDomain()
        {
            return _dbConnection.QueryFirstOrDefault<Domain>("SELECT * FROM public.\"Domains_GetNextUnanalyzed\"()");
        }

        #endregion

        #region "Links"
        public List<Domain> GetLinks(int domainId)
        {
            return _dbConnection.Query<Domain>("SELECT * FROM public.\"DomainLinks_GetList\"(@domainId)", new { domainId }).ToList();
        }
        #endregion

        #region "Analyzer Rules"
        public int AddAnalyzerRule(int domainId, string selector, bool rule)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_AnalyzerRule_Add\"(@domainId, @selector, @rule)", new { domainId, selector, rule });
        }

        public List<AnalyzerRule> GetAnalyzerRules(int domainId)
        {
            return _dbConnection.Query<AnalyzerRule>("SELECT * FROM public.\"Domain_AnalyzerRules_GetList\"(@domainId)", new { domainId }).ToList();
        }

        public void RemoveAnalyzerRule(int ruleId)
        {
            _dbConnection.Execute("SELECT public.\"Domain_AnalyzerRule_Remove\"(@ruleId)", new { ruleId });
        }
        #endregion

        #region "Download Rules"
        public int AddDownloadRule(int domainId, bool rule, string url, string title, string summary)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_DownloadRule_Add\"(@domainId, @rule, @url, @title, @summary)", new { domainId, rule, url, title, summary });
        }

        public List<DownloadRule> GetDownloadRules(int domainId)
        {
            return _dbConnection.Query<DownloadRule>("SELECT * FROM public.\"Domain_DownloadRules_GetList\"(@domainId)", new { domainId }).ToList();
        }

        public List<DownloadRule> GetDownloadRulesForDomains(string[] domains)
        {
            return _dbConnection.Query<DownloadRule>("SELECT * FROM public.\"Domain_DownloadRules_GetForDomains\"(@domains)", new { domains = string.Join(",", domains) }).ToList();
        }

        public void RemoveDownloadRule(int ruleId)
        {
            _dbConnection.Execute("SELECT public.\"Domain_DownloadRule_Remove\"(@ruleId)", new { ruleId });
        }
        #endregion

        #region "Clean"
        public CleanDownload GetDownloadsToClean(int domainId, bool topten = false)
        {
            var clean = new CleanDownload();
            clean.articles = _dbConnection.Query<Article>("SELECT * FROM public.\"Domain_GetDownloadsToClean_Articles\"(@domainId, @topten)", new { domainId, topten }).ToList();
            clean.totalArticles = clean.articles.Count;
            clean.totalDownloads = _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_GetDownloadsToClean_DownloadsCount\"(@domainId)", new { domainId });
            return clean;
        }

        public void CleanDownloads(int domainId)
        {
            _dbConnection.Execute("SELECT public.\"Domain_CleanDownloads\"(@domainId)", new { domainId });
        }
        #endregion

        #region "Delete"
        public void DeleteAllArticles(int domainId)
        {
            _dbConnection.Execute("SELECT public.\"Domain_DeleteAllArticles\"(@domainId)", new { domainId });
        }

        public void Delete(int domainId)
        {
            _dbConnection.Execute("SELECT public.\"Domain_Delete\"(@domainId)", new { domainId });
        }
        #endregion

        #region "Update"
        public void RequireSubscription(int domainId, bool required)
        {
            _dbConnection.Execute("SELECT public.\"Domain_RequireSubscription\"(@domainId, @required)", new { domainId, required });
        }

        public void HasFreeContent(int domainId, bool free)
        {
            _dbConnection.Execute("SELECT public.\"Domain_HasFreeContent\"(@domainId, @free)", new { domainId, free });
        }

        public string FindDomainTitle(int domainId)
        {
            return _dbConnection.ExecuteScalar<string>("SELECT public.\"Domain_FindTitle\"(@domainId)", new { domainId });
        }

        public string FindDescription(int domainId)
        {
            return _dbConnection.ExecuteScalar<string>("SELECT public.\"Domain_FindDescription\"(@domainId)", new { domainId });
        }

        public void UpdateInfo(int domainId, string title, string description, string lang)
        {
            _dbConnection.Execute("SELECT public.\"Domain_UpdateInfo\"(@domainId, @title, @description, @lang)", new { domainId, title, description, lang });
        }

        public void UpdateCompany(int domainId, string company)
        {
            try
            {
                _dbConnection.Execute("UPDATE public.\"Domains\" SET \"company\" = @company, \"dateupdated\" = CURRENT_TIMESTAMP WHERE \"domainId\" = @domainId", 
                    new { domainId, company });
            }
            catch (Exception ex)
            {
                // Log exception if needed
            }
        }

        public void UpdateDomainType(int domainId, DomainType type)
        {
            _dbConnection.Execute("SELECT public.\"Domain_UpdateType\"(@domainId, @type)", new { domainId, type = (int)type });
        }

        public void UpdateDomainType2(int domainId, DomainType type)
        {
            _dbConnection.Execute("SELECT public.\"Domain_UpdateType2\"(@domainId, @type)", new { domainId, type = (int)type });
        }

        public void UpdateDomainTypes(int domainId, DomainType type, DomainType type2)
        {
            try
            {
                _dbConnection.Execute("UPDATE public.\"Domains\" SET \"type\" = @type, \"type2\" = @type2, \"dateupdated\" = CURRENT_TIMESTAMP WHERE \"domainId\" = @domainId", 
                    new { domainId, type = (int)type, type2 = (int)type2 });
            }
            catch (Exception ex)
            {
                // Log exception if needed
            }
        }

        public void UpdateLanguage(int domainId, string lang)
        {
            _dbConnection.Execute("SELECT public.\"Domain_UpdateLanguage\"(@domainId, @lang)", new { domainId, lang });
        }

        public void UpdateHttpsWww(int domainId, bool https, bool www)
        {
            _dbConnection.Execute("SELECT public.\"Domain_UpdateHttpsWww\"(@domainId, @https, @www)", new { domainId, https, www });
        }

        public void IsEmpty(int domainId, bool empty)
        {
            _dbConnection.Execute("SELECT public.\"Domain_IsEmpty\"(@domainId, @empty)", new { domainId, empty });
        }

        public void IsDeleted(int domainId, bool delete)
        {
            _dbConnection.Execute("SELECT public.\"Domain_IsDeleted\"(@domainId, @delete)", new { domainId, delete });
        }
        #endregion

        #region "Collections"
        public int AddCollection(int colgroupId, string name, string search = "", int subjectId = 0, DomainFilterType filtertype = DomainFilterType.All, DomainType type = DomainType.unused, DomainSort sort = DomainSort.Alphabetical, string lang = "")
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_Collection_Add\"(@colgroupId, @name, @search, @subjectId, @filtertype, @type, @sort, @lang)", 
                new { colgroupId, name, search, subjectId, filtertype = (int)filtertype, type = (int)type, sort = (int)sort, lang });
        }

        public int AddCollection(DomainCollection collection)
        {
            return AddCollection(collection.colgroupId, collection.name, collection.search, collection.subjectId, collection.filtertype, collection.type, collection.sort, collection.lang);
        }

        public DomainCollectionsAndGroups GetCollectionsList()
        {
            var collections = _dbConnection.Query<DomainCollection>("SELECT * FROM public.\"Domain_Collections_GetList\"()").ToList();
            var groups = _dbConnection.Query<CollectionGroup>("SELECT * FROM public.\"Domain_CollectionGroups_GetList\"()").ToList();
            return new DomainCollectionsAndGroups()
            {
                Collections = collections,
                Groups = groups
            };
        }

        public int RemoveCollection(int colId)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_Collection_Remove\"(@colId)", new { colId });
        }
        #endregion

        #region "Collection Groups"
        public int AddCollectionGroup(string name)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_CollectionGroup_Add\"(@name)", new { name });
        }

        public int RemoveCollectionGroup(int colgroupId)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Domain_CollectionGroup_Remove\"(@colgroupId)", new { colgroupId });
        }

        public List<CollectionGroup> GetCollectionGroupsList()
        {
            return _dbConnection.Query<CollectionGroup>("SELECT * FROM public.\"Domain_CollectionGroups_GetList\"()").ToList();
        }
        #endregion

        #region "DomainTypeMatches"
        public int AddDomainTypeMatch(List<DomainTypeMatchPart> parts, int type, int type2, int threshold, int rank)
        {
            _dbConnection.Execute("SELECT public.\"DomainTypeMatches_Add\"(@type, @type2, @words, @threshold, @rank)", 
                new { words = JsonSerializer.Serialize(parts), type, type2, threshold, rank });
            return 0;
        }

        public int RemoveDomainTypeMatch(int matchId)
        {
            _dbConnection.Execute("SELECT public.\"DomainTypeMatches_Remove\"(@matchId)", new { matchId });
            return 0;
        }

        public List<DomainTypeMatch> GetDomainTypeMatchesList()
        {
            var result = _dbConnection.Query<DomainTypeMatch>("SELECT * FROM public.\"DomainTypeMatches_GetList\"()").ToList();
            foreach (var elem in result)
            {
                elem.parts = JsonSerializer.Deserialize<List<DomainTypeMatchPart>>(elem.words);
            }
            return result;
        }
        #endregion

        #region "Domain Services"
        public Dictionary<string, int> GetServiceIdsByNames(string[] serviceNames)
        {
            try
            {
                var result = new Dictionary<string, int>();
                var services = _dbConnection.Query<dynamic>("SELECT * FROM public.\"DomainServices_GetByNames\"(@serviceNames)", 
                    new { serviceNames = string.Join(",", serviceNames) });
                
                foreach (var service in services)
                {
                    result.Add(service.Name, service.Id);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return new Dictionary<string, int>();
            }
        }

        public void AddDomainServices(int domainId, int[] serviceIds)
        {
            try
            {
                _dbConnection.Execute("SELECT public.\"DomainServices_Add\"(@domainId, @serviceIds)", 
                    new { domainId, serviceIds = string.Join(",", serviceIds) });
            }
            catch (Exception ex)
            {
                // Log exception if needed
            }
        }

        public (List<DomainService> services, int totalCount) GetDomainServices(string search = "", int start = 0, int length = 50)
        {
            try
            {
                var services = _dbConnection.Query<DomainService>("SELECT * FROM public.\"DomainServices_Filter\"(@search, @start, @length)", 
                    new { search, start, length }).ToList();
                var totalCount = _dbConnection.ExecuteScalar<int>("SELECT public.\"DomainServices_Filter_Count\"(@search)", 
                    new { search });
                
                return (services, totalCount);
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return (new List<DomainService>(), 0);
            }
        }
        #endregion
    }
}
