using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Collector.API.Models;
using Collector.Data.Interfaces;
using Collector.Common;

namespace Collector.API.Controllers
{
    [Authorize]
    [Route("api/whitelists")]
    public class WhitelistsController : ApiController
    {
        private readonly IWhitelistsRepository _whitelistsRepository;

        public WhitelistsController(IWhitelistsRepository whitelistsRepository)
        {
            _whitelistsRepository = whitelistsRepository;
        }

        [HttpGet("domains")]
        public IActionResult GetDomains()
        {
            try
            {
                var domains = _whitelistsRepository.GetDomainsList();
                return Json(new ApiResponse { success = true, data = domains });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("check")]
        public IActionResult CheckDomain([FromQuery] string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return Json(new ApiResponse { success = false, message = "Domain is required" });
            }

            try
            {
                var isWhitelisted = _whitelistsRepository.CheckDomain(domain);
                return Json(new ApiResponse { success = true, data = isWhitelisted });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("domain")]
        public IActionResult AddDomain([FromBody] WhitelistDomainModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _whitelistsRepository.AddDomain(model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("domain")]
        public IActionResult RemoveDomain([FromBody] WhitelistDomainModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _whitelistsRepository.RemoveDomain(model.Domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
