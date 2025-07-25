using Collector.API.Models;
using Collector.Auth.Services;
using Collector.Auth.Policies;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Services;
using System.Text.RegularExpressions;

namespace Collector.API.Controllers.Admin
{
    [Route("/api/admin/users")]
    [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
    public class UsersController : ApiController
    {
        readonly IAuthService _authRepo;
        readonly IAppUserRepository _userRepo;
        readonly IAppRoleRepository _roleRepo;
        readonly IEmailService _emailService;
        public UsersController(
            IAuthService authRepo,
            IAppUserRepository userRepo,
            IAppRoleRepository roleRep,
            IEmailService emailService
        )
        {
            _authRepo = authRepo;
            _userRepo = userRepo;
            _roleRepo = roleRep;
            _emailService = emailService;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var users = (await _userRepo.GetAll()).Select(a => new { a.Email, a.FullName, a.Id, a.LastLogin, a.Status, a.Created });
            return Json(new ApiResponse { success = true, data = users });
        }

        [HttpGet("get-all-filtered")]
        public async Task<IActionResult> GetAllFiltered(string fullName, int role, int? radioStationId, string sort)
        {
            var users = await _userRepo.GetAllFiltered(fullName, role, radioStationId ?? -1, sort);
            return Json(new ApiResponse { success = true, data = users });
        }

        [HttpGet("get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleRepo.GetAll();
            return Json(new ApiResponse { success = true, data = roles });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> Get(string id)
        {
            Guid userGuid;
            if (Guid.TryParse(id, out userGuid))
            {
                var user = await _userRepo.FindByGuidAsync(userGuid);
                if (user == null) return Json(new ApiResponse { success = false, data = null });
                user.PasswordHash = null;
                return Json(new ApiResponse { success = true, data = user });
            }
            else return Json(new ApiResponse { success = false, message = "User not found" });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUser([FromBody] NewAdminUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return BadRequest("Email is required.");
            var emailRegex = new Regex("^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,10}$");
            if (!emailRegex.IsMatch(user.Email)) return BadRequest("Email is invalid.");
            if (!user.Role.HasValue) { user.Role = 2; } //2 = user role

            var userExists = await _userRepo.FindByUserEmailAsync(user.Email);
            if (userExists != null && userExists.Email == user.Email)
            {
                return Json(new ApiResponse { success = false, message = "An account with this email address already exists" });
            }
            AppUser newUser = new AppUser();
            newUser.Email = user.Email;
            newUser.FullName = user.FullName;
            newUser.Status = AppUserStatus.Active;
            newUser.EmailConfirmed = false;
            if (user.IsAdmin)
            {
                newUser.UserRoles = _roleRepo.GetAll().Result.Where(a => a.Name == "admin").Select(a => new AppUserRole() { AppRoleId = a.Id }).ToList();
            }

            _authRepo.GenerateResetPasswordHash(newUser, 24);

            try
            {
                //send user email before creating account
                _emailService.SendNewUserEmail(newUser);
            }
            catch (Exception ex)
            {
                //do not create account if we can't send email to user
                return Problem(ex.Message);
            }

            //create new user in database
            newUser = await _userRepo.Add(newUser);

            ApiAppService.TotalUsers++;

            return Ok(new ApiResponse { success = true, data = newUser });
        }

        [HttpPost("update-email")]
        public async Task<IActionResult> UpdateEmail([FromBody] AppUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return BadRequest("Email is required.");
            var emailRegex = new Regex("^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,10}$");
            if (!emailRegex.IsMatch(user.Email)) return Json(new ApiResponse { success = false, message = "Email is invalid." });

            var userExists = await _userRepo.FindByUserEmailAsync(user.Email);
            if (userExists != null && userExists.Email == user.Email)
            {
                return Json(new ApiResponse { success = false, message = "An account with this email address already exists" });
            }

            _authRepo.GenerateResetPasswordHash(user, 24);
            user.EmailConfirmed = false;

            try
            {
                //send user email before creating account
                _emailService.SendResetPasswordEmail(user);
            }
            catch (Exception ex)
            {
                //do not create account if we can't send email to user
                return Problem(ex.Message);
            }
            await _userRepo.UpdatePasswordResetHash(user);

            return Ok(new ApiResponse { success = true, data = user });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AppUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return BadRequest("Email is required.");
            var emailRegex = new Regex("^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,10}$");
            if (!emailRegex.IsMatch(user.Email)) return BadRequest("Email is invalid.");

            _authRepo.GenerateResetPasswordHash(user, 24);

            try
            {
                //send user email before creating account
                _emailService.SendResetPasswordEmail(user);
            }
            catch (Exception ex)
            {
                //do not create account if we can't send email to user
                return Problem(ex.Message);
            }

            await _userRepo.UpdatePasswordResetHash(user);

            return Ok(new ApiResponse { success = true, data = user });
        }

        [HttpPost("edit")]
        public async Task<IActionResult> Edit([FromBody] UpdateAdminUser user)
        {
            var saveduser = await _userRepo.FindByGuidAsync(user.Id);
            saveduser.FullName = user.FullName;
            saveduser.Status = user.Status;
            _userRepo.UpdateInfo(saveduser);

            return Json(new ApiResponse { success = true });
        }

        [HttpPost("update-lock")]
        public async Task<IActionResult> UpdateLock([FromBody] UpdateLockRequest request)
        {
            var success = await _userRepo.UpdateLock(request.UserId, request.lockUser);
            return Json(new ApiResponse { success = success });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            Guid userGuid;
            if (Guid.TryParse(id, out userGuid))
            {
                try
                {
                    await _userRepo.DeleteUserAsync(userGuid);
                }
                catch (Exception ex)
                {
                    return Json(new ApiResponse { success = false, message = ex.Message });
                }

                return Json(new ApiResponse { success = true });
            }
            else return Json(new ApiResponse { success = false, message = "User not found" });
        }

    }
}
