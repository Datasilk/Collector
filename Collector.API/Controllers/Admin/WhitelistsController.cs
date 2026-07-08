using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;
using Collector.Data.Interfaces;
using Collector.Auth.Policies;

namespace Collector.API.Controllers.Admin
{
    [Route("/api/admin/whitelists")]
    [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
    public class WhitelistsController : ApiController
    {
        private readonly IWhitelistsRepository _whitelistsRepository;

        public WhitelistsController(IWhitelistsRepository whitelistsRepository)
        {
            _whitelistsRepository = whitelistsRepository;
        }

        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string search = "", [FromQuery] int status = 0, [FromQuery] string sort = "Name ASC", [FromQuery] int start = 1, [FromQuery] int length = 100)
        {
            try
            {
                var domains = _whitelistsRepository.GetDomainsList();
                var filtered = domains.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(d => d.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                var sorted = sort.ToLowerInvariant() switch
                {
                    "name desc" => filtered.OrderByDescending(d => d),
                    _ => filtered.OrderBy(d => d)
                };

                var totalCount = sorted.Count();
                var paged = sorted.Skip(start - 1).Take(length).Select(d => new
                {
                    name = d,
                    domainCount = 1,
                    status = "Active"
                }).ToList();

                return Json(new ApiResponse
                {
                    success = true,
                    data = new
                    {
                        items = paged,
                        totalCount = totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost]
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

        [HttpDelete("{domain}")]
        public IActionResult RemoveDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                return Json(new ApiResponse { success = false, message = "Domain is required" });
            }

            try
            {
                _whitelistsRepository.RemoveDomain(domain);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
