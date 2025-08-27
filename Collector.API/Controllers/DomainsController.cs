using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.API.Controllers
{
    [Authorize]
    [Route("api/domains")]
    public class DomainsController : ApiController
    {
        private readonly IDomainsRepository _domainsRepository;

        public DomainsController(IDomainsRepository domainsRepository)
        {
            _domainsRepository = domainsRepository;
        }

        #region Domain Management

        [HttpPost("list")]
        public IActionResult GetDomains([FromBody] DomainFilterModel filter)
        {
            try
            {
                var domains = _domainsRepository.GetList(
                    filter.SubjectIds,
                    filter.Type,
                    filter.DomainType,
                    filter.DomainType2,
                    filter.Sort,
                    filter.Lang,
                    filter.Search,
                    filter.Start,
                    filter.Length,
                    filter.ParentId
                );

                var count = _domainsRepository.GetCount(
                    filter.SubjectIds,
                    filter.Type,
                    filter.DomainType,
                    filter.DomainType2,
                    filter.Sort,
                    filter.Lang,
                    filter.Search,
                    filter.ParentId
                );

                return Json(new ApiResponse
                {
                    success = true,
                    data = new
                    {
                        domains,
                        totalCount = count
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetDomain(int id)
        {
            try
            {
                var domain = _domainsRepository.GetById(id);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                return Json(new ApiResponse { success = true, data = domain });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("info/{domain}")]
        public IActionResult GetDomainInfo(string domain)
        {
            try
            {
                var domainInfo = _domainsRepository.GetInfo(domain);
                if (domainInfo == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                return Json(new ApiResponse { success = true, data = domainInfo });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult AddDomain([FromBody] AddDomainModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                _domainsRepository.Add(model.Domain, model.Title, model.ParentId, model.Type);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-info")]
        public IActionResult UpdateDomainInfo([FromBody] UpdateDomainInfoModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.UpdateInfo(model.DomainId, model.Title, model.Description, model.Lang);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-type")]
        public IActionResult UpdateDomainType([FromBody] UpdateDomainTypeModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.UpdateDomainType(model.DomainId, model.Type);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-type2")]
        public IActionResult UpdateDomainType2([FromBody] UpdateDomainTypeModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.UpdateDomainType2(model.DomainId, model.Type);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-language")]
        public IActionResult UpdateLanguage([FromBody] UpdateDomainInfoModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.UpdateLanguage(model.DomainId, model.Lang);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-https-www")]
        public IActionResult UpdateHttpsWww([FromBody] UpdateDomainHttpsWwwModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.UpdateHttpsWww(model.DomainId, model.Https, model.Www);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("require-subscription")]
        public IActionResult RequireSubscription([FromBody] DomainStatusModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.RequireSubscription(model.DomainId, model.Status);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("has-free-content")]
        public IActionResult HasFreeContent([FromBody] DomainStatusModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.HasFreeContent(model.DomainId, model.Status);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("is-empty")]
        public IActionResult IsEmpty([FromBody] DomainStatusModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.IsEmpty(model.DomainId, model.Status);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("is-deleted")]
        public IActionResult IsDeleted([FromBody] DomainStatusModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.IsDeleted(model.DomainId, model.Status);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("find-title/{id}")]
        public IActionResult FindDomainTitle(int id)
        {
            try
            {
                var domain = _domainsRepository.GetById(id);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var title = _domainsRepository.FindDomainTitle(id);
                return Json(new ApiResponse { success = true, data = title });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("find-description/{id}")]
        public IActionResult FindDescription(int id)
        {
            try
            {
                var domain = _domainsRepository.GetById(id);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var description = _domainsRepository.FindDescription(id);
                return Json(new ApiResponse { success = true, data = description });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("links/{id}")]
        public IActionResult GetLinks(int id)
        {
            try
            {
                var domain = _domainsRepository.GetById(id);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var links = _domainsRepository.GetLinks(id);
                return Json(new ApiResponse { success = true, data = links });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("delete/{id}")]
        public IActionResult DeleteDomain(int id)
        {
            try
            {
                var domain = _domainsRepository.GetById(id);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.Delete(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("delete-articles/{id}")]
        public IActionResult DeleteAllArticles(int id)
        {
            try
            {
                var domain = _domainsRepository.GetById(id);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.DeleteAllArticles(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Analyzer Rules

        [HttpGet("analyzer-rules/{domainId}")]
        public IActionResult GetAnalyzerRules(int domainId)
        {
            try
            {
                var domain = _domainsRepository.GetById(domainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var rules = _domainsRepository.GetAnalyzerRules(domainId);
                return Json(new ApiResponse { success = true, data = rules });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("analyzer-rules/add")]
        public IActionResult AddAnalyzerRule([FromBody] AnalyzerRuleModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.AddAnalyzerRule(model.DomainId, model.Selector, model.Rule);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("analyzer-rules/remove/{ruleId}")]
        public IActionResult RemoveAnalyzerRule(int ruleId)
        {
            try
            {
                _domainsRepository.RemoveAnalyzerRule(ruleId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Download Rules

        [HttpGet("download-rules/{domainId}")]
        public IActionResult GetDownloadRules(int domainId)
        {
            try
            {
                var domain = _domainsRepository.GetById(domainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var rules = _domainsRepository.GetDownloadRules(domainId);
                return Json(new ApiResponse { success = true, data = rules });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("download-rules/for-domains")]
        public IActionResult GetDownloadRulesForDomains([FromBody] string[] domains)
        {
            if (domains == null || domains.Length == 0)
                return Json(new ApiResponse { success = false, message = "No domains provided" });

            try
            {
                var rules = _domainsRepository.GetDownloadRulesForDomains(domains);
                return Json(new ApiResponse { success = true, data = rules });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("download-rules/add")]
        public IActionResult AddDownloadRule([FromBody] DownloadRuleModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var domain = _domainsRepository.GetById(model.DomainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.AddDownloadRule(model.DomainId, model.Rule, model.Url, model.Title, model.Summary);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("download-rules/remove/{ruleId}")]
        public IActionResult RemoveDownloadRule(int ruleId)
        {
            try
            {
                _domainsRepository.RemoveDownloadRule(ruleId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Clean Downloads

        [HttpGet("downloads-to-clean/{domainId}")]
        public IActionResult GetDownloadsToClean(int domainId)
        {
            try
            {
                var domain = _domainsRepository.GetById(domainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var downloads = _domainsRepository.GetDownloadsToClean(domainId);
                return Json(new ApiResponse { success = true, data = downloads });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("downloads-to-clean/{domainId}/top")]
        public IActionResult GetTopDownloadsToClean(int domainId)
        {
            try
            {
                var domain = _domainsRepository.GetById(domainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                var downloads = _domainsRepository.GetDownloadsToClean(domainId, true);
                return Json(new ApiResponse { success = true, data = downloads });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("clean-downloads/{domainId}")]
        public IActionResult CleanDownloads(int domainId)
        {
            try
            {
                var domain = _domainsRepository.GetById(domainId);
                if (domain == null)
                    return Json(new ApiResponse { success = false, message = "Domain not found" });

                _domainsRepository.CleanDownloads(domainId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Collections

        [HttpGet("collections")]
        public IActionResult GetCollections()
        {
            try
            {
                var collections = _domainsRepository.GetCollectionsList();
                return Json(new ApiResponse { success = true, data = collections });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("collections/add")]
        public IActionResult AddCollection([FromBody] DomainCollectionModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var collectionId = _domainsRepository.AddCollection(
                    model.ColGroupId,
                    model.Name,
                    model.Search,
                    model.SubjectId,
                    model.FilterType,
                    model.Type,
                    model.Sort,
                    model.Lang
                );

                return Json(new ApiResponse { success = true, data = collectionId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("collections/remove/{colId}")]
        public IActionResult RemoveCollection(int colId)
        {
            try
            {
                var result = _domainsRepository.RemoveCollection(colId);
                return Json(new ApiResponse { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Collection Groups

        [HttpGet("collection-groups")]
        public IActionResult GetCollectionGroups()
        {
            try
            {
                var groups = _domainsRepository.GetCollectionGroupsList();
                return Json(new ApiResponse { success = true, data = groups });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("collection-groups/add")]
        public IActionResult AddCollectionGroup([FromBody] CollectionGroupModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var groupId = _domainsRepository.AddCollectionGroup(model.Name);
                return Json(new ApiResponse { success = true, data = groupId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("collection-groups/remove/{groupId}")]
        public IActionResult RemoveCollectionGroup(int groupId)
        {
            try
            {
                var result = _domainsRepository.RemoveCollectionGroup(groupId);
                return Json(new ApiResponse { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Domain Type Matches

        [HttpGet("type-matches")]
        public IActionResult GetDomainTypeMatches()
        {
            try
            {
                var matches = _domainsRepository.GetDomainTypeMatchesList();
                return Json(new ApiResponse { success = true, data = matches });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("type-matches/add")]
        public IActionResult AddDomainTypeMatch([FromBody] DomainTypeMatchModel model)
        {
            if (!ModelState.IsValid)
                return Json(new ApiResponse { success = false, message = "Invalid model state" });

            try
            {
                var matchId = _domainsRepository.AddDomainTypeMatch(
                    model.Parts,
                    model.Type,
                    model.Type2,
                    model.Threshold,
                    model.Rank
                );

                return Json(new ApiResponse { success = true, data = matchId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("type-matches/remove/{matchId}")]
        public IActionResult RemoveDomainTypeMatch(int matchId)
        {
            try
            {
                var result = _domainsRepository.RemoveDomainTypeMatch(matchId);
                return Json(new ApiResponse { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
