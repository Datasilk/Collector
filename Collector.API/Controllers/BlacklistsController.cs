using Collector.API.Models;
using Collector.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers
{
    [ApiController]
    [Route("api/blacklists")]
    [Authorize]
    public class BlacklistsController : ApiController
    {
        private readonly IBlacklistsRepository _blacklistsRepository;

        public BlacklistsController(IBlacklistsRepository blacklistsRepository)
        {
            _blacklistsRepository = blacklistsRepository;
        }

        #region "Domains"

        [HttpGet("domains")]
        public IActionResult GetDomainsList()
        {
            try
            {
                var domains = _blacklistsRepository.GetDomainsList();
                return Json(new ApiResponse { success = true, data = domains });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("domains")]
        public IActionResult AddDomain([FromBody] DomainModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _blacklistsRepository.AddDomain(model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("domains")]
        public IActionResult RemoveDomain([FromBody] DomainModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _blacklistsRepository.RemoveDomain(model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("domains/check/{domain}")]
        public IActionResult CheckDomain(string domain)
        {
            try
            {
                var result = _blacklistsRepository.CheckDomain(domain);
                return Json(new ApiResponse { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("domains/check-all")]
        public IActionResult CheckAllDomains([FromBody] DomainsListModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                var results = _blacklistsRepository.CheckAllDomains(model.Domains);
                
                var response = results.Select(r => new BlacklistDomainResponse
                {
                    Domain = r.domain,
                    IsBlacklisted = true
                }).ToList();

                return Json(new ApiResponse { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion

        #region "Wildcards"

        [HttpGet("wildcards")]
        public IActionResult GetWildcardsList()
        {
            try
            {
                var wildcards = _blacklistsRepository.GetWildcardsList();
                return Json(new ApiResponse { success = true, data = wildcards });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("wildcards")]
        public IActionResult AddWildcard([FromBody] DomainModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _blacklistsRepository.AddWildcard(model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("wildcards")]
        public IActionResult RemoveWildcard([FromBody] DomainModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _blacklistsRepository.RemoveWildcard(model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
