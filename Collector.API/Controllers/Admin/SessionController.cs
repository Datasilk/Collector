using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Collector.API.Models;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using System.Security.Claims;

namespace Collector.API.Controllers.Admin
{
    [Route("api/admin/session")]
    [Authorize]
    public class SessionController : ApiController
    {
        readonly ILogger<SessionController> _logger;
        readonly IAppUserRepository _userRepo;

        public SessionController(ILogger<SessionController> logger, IAppUserRepository userRepo)
        {
            _logger = logger;
            _userRepo = userRepo;
        }

        [HttpGet("keepalive")]
        public IActionResult KeepAlive()
        {
            return Json(new { success = true });
        }

        [HttpPost("get-user-roles-by-email")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserRolesByEmail([FromBody] AppUser user, IFormCollection form)
        {
            var users = await _userRepo.GetRolesByUserEmailAsync(user.Email);
            var claims = HttpContext.User.Claims;
            var roles = claims.Where(a => a.Type == ClaimTypes.Role);
            var len = roles.Count();
            if (users != null && users.UserRoles.Count > 0)
            {
                return Ok(new ApiResponse { success = true, data = users.UserRoles });
            }
            return Ok(new ApiResponse { success = false });
        }

    }
}
