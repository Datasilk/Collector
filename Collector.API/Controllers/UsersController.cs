using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Collector.API.Models;
using Collector.API.Services;
using Collector.Auth.Services;
using Collector.Data.Entities;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces;
using Collector.Data.Interfaces.Users;
using Collector.Auth.Policies;

namespace Collector.API.Controllers
{

    [Route("/api/users")]
    public class UsersController : ApiController
    {
        readonly IAuthService _authService;
        readonly IAppUserRepository _userRepository;
        readonly IAppRoleRepository _roleRepository;
        readonly IEmailService _emailService;
        public UsersController(
            IAuthService authService,
            IAppUserRepository userRepository,
            IAppRoleRepository roleRepository,
            IEmailService emailService
        )
        {
            _authService = authService;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _emailService = emailService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUser([FromBody] NewUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return BadRequest("Email is required.");
            var emailRegex = new Regex("^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,10}$");
            if (!emailRegex.IsMatch(user.Email)) return BadRequest("Email is invalid.");
            if (string.IsNullOrEmpty(user.Password)) user.Password = Guid.NewGuid().ToString();

            var userExists = await _userRepository.FindByUserEmailAsync(user.Email);
            if (userExists != null && userExists.Email == user.Email)
            {
                return Json(new ApiResponse { success = false, message = "An account with this email address already exists" });
            }
            AppUser newUser = new AppUser();
            newUser.Email = user.Email;
            newUser.FullName = user.FullName;
            newUser.Status = AppUserStatus.Active;
            newUser.EmailConfirmed = false;
            _authService.GenerateResetPasswordHash(newUser, 24);
            newUser.PasswordHash = new PasswordHasher<object>().HashPassword(new object(), user.Password);

            if(ApiAppService.TotalUsers == 0)
            {
                //check how many users exist
                ApiAppService.TotalUsers = _userRepository.GetTotalUsers();
            }
            if (ApiAppService.TotalUsers == 0)
            {
                //add admin roles to first user in database
                newUser.UserRoles = _roleRepository.GetAll().Result.Where(a => a.Name == "admin").Select(a => new AppUserRole() { AppRoleId = a.Id }).ToList();
            }

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

            newUser = await _userRepository.Add(newUser);

            ApiAppService.TotalUsers++;

            return Ok(new ApiResponse { success = true, data = newUser });
        }

        [HttpGet("get/{id}")]
        [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
        public async Task<IActionResult> Get(string id)
        {
            Guid userGuid;
            if (Guid.TryParse(id, out userGuid))
            {
                var user = await _userRepository.FindByGuidAsync(userGuid);
                if (user == null) return Json(new ApiResponse { success = false, data = null });
                user.PasswordHash = null;
                return Json(new ApiResponse { success = true, data = user });
            }
            else return Json(new ApiResponse { success = false, message = "User not found" });
        }

        [HttpGet("my-info")]
        [Authorize]
        public async Task<IActionResult> GetMyInfo()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Json(new ApiResponse { success = false, message = "Could not find user" });
            var user = await _userRepository.FindByGuidAsync(userId);
            if (user == null)
                return Json(new ApiResponse { success = false, message = "User not found" });
            user.PasswordHash = null;
            return Json(new ApiResponse { success = true, data = user });
        }

        [HttpPost("update-email")]
        [Authorize]
        public async Task<IActionResult> UpdateEmail([FromBody] AppUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return BadRequest("Email is required.");
            var emailRegex = new Regex("^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,10}$");
            if (!emailRegex.IsMatch(user.Email)) return Json(new ApiResponse { success = false, message = "Email is invalid." });

            var userExists = await _userRepository.FindByUserEmailAsync(user.Email);
            if (userExists != null && userExists.Email == user.Email)
            {
                return Json(new ApiResponse { success = false, message = "An account with this email address already exists" });
            }

            _authService.GenerateResetPasswordHash(user, 24);
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
            await _userRepository.UpdatePasswordResetHash(user);

            return Ok(new ApiResponse { success = true, data = user });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AppUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return BadRequest("Email is required.");
            var emailRegex = new Regex("^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,10}$");
            if (!emailRegex.IsMatch(user.Email)) return BadRequest("Email is invalid.");

            _authService.GenerateResetPasswordHash(user, 24);

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

            await _userRepository.UpdatePasswordResetHash(user);

            return Ok(new ApiResponse { success = true, data = user });
        }

        [HttpPost("edit")]
        [Authorize(Policy = nameof(AuthConstants.Policy.ManageUsers))]
        public async Task<IActionResult> Edit([FromBody] AppUser user)
        {
            if (!string.IsNullOrEmpty(user.password))
                user.PasswordHash = new PasswordHasher<object>().HashPassword(new object(), user.password);
            _userRepository.UpdateInfo(user);
            return Json(new ApiResponse { success = true });
        }
    }
}
