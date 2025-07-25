using Collector.Auth.Authentication;
using Collector.Auth.Models;
using Collector.Auth.Services;
using Collector.Data.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Collector.Auth.Controller
{
    [Route("api/auth")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        readonly IAuthService _authService;
        readonly IAppUserRepository _userRepo;
        readonly IAuthEmailService _emailService;
		readonly IAppRoleRepository _roleRepo;
        readonly AuthSettings _authSettings;

        public AccountController(
            IOptions<AuthSettings> authSettings,
            IAuthService authService,
            IAppUserRepository userRepo,
            IAuthEmailService emailService,
			IAppRoleRepository roleRepo)
        {
            _authSettings = authSettings.Value;
            _authService = authService;
            _userRepo = userRepo;
            _emailService = emailService;
			_roleRepo = roleRepo;
		}

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginCredentials loginUser)
        {
            var result = await _authService.Authenticate(loginUser.Username, loginUser.Password, IPAddress);
            switch (result.ResponseCode)
            {
                case AuthenticationResponseCode.LocalSuccess:
                case AuthenticationResponseCode.RemoteSuccess:
                    {
                        var refreshToken = await _authService.GenerateRefreshToken(IPAddress, AppUserId: result.UserId, SpecialUser: result.SpecialUserName);
						
                        return Ok(new { success = true, data = new
                        {
                            appUserId = result.UserId,
                            displayName = result.DisplayName != "" ? result.DisplayName : result.Email.Split("@")[0],
                            token = result.JwtToken,
                            refreshToken = refreshToken.Token,
                            roles = result.Roles
                        } });
                    }
                case AuthenticationResponseCode.AccountLocked:
                    return Ok(new { success = false, message = "Your account has been locked. Too many invalid login attempts." });
                default:
                    return Ok(new { success = false, message = "Invalid username and/or password." });
            }    
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody]RefreshToken refreshtoken)
        {
            var result = await _authService.RefreshUserToken(refreshtoken.Token, IPAddress);
            if (string.IsNullOrEmpty(result))
                return Ok(new { success = false, message = "RefreshToken is invalid" });
            return Ok(new { success = true, data = new { token = result } });
        }

        [HttpGet("check-auth")]
        [Authorize]
        public IActionResult CheckAuth()
        {
            return Ok(new { success = true });
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccount activate)
        {
            var user = await _authService.ActivateAccount(activate.Hash);

            if (user == null)
            {
                return Ok(new
                {
                    success = false,
                    hash = (string?)null,
                    message = "Your account could not be activated. Please contact support."
                });
            }

            return Ok(new { success = true, data =  new { hasPass = !string.IsNullOrEmpty(user.PasswordHash) } });
        }

        [HttpPost("forgot-password")]
		public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword username)
		{
			var hash = await _authService.GenerateResetPasswordHash(username.Username);

			System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
			msg.To.Add(username.Username);
			msg.Subject = "Password Reset Request";
			msg.IsBodyHtml = true;
            var resetPasswordUrl = $"{_authSettings.Domain}/reset-password/{hash}";

            msg.Body = $@"<p>We received a request to reset your password for your account on <strong>{System.Net.WebUtility.HtmlEncode(_authSettings.Domain)}</strong>.</p>
						 <p>If you made this request, please click the link below to reset your password:<br />
						 <a href=""{resetPasswordUrl}""><b>Reset My Password</b></a><br/></p>
						 
						 <p>If the link doesn’t work, you can copy and paste the following URL into your browser:<br />
                         <span style=""word-wrap: break-word;""><b>{resetPasswordUrl}</b></span><br /></p>

						 <p>This link will expire in 1 hours for security purposes.<br/></p>
						 <p>If you didn’t request a password reset, you can safely ignore this email—your password will remain unchanged.</p>";

            _emailService.SendEmail(msg);

			return Ok(new { success = true, data = hash });
		}

        [HttpPost("check-password-reset")]
        public async Task<IActionResult> CheckPasswordReset([FromBody] ResetPassword passwordResetHash)
        {
            var hash = await _authService.CheckPasswordResetHash(passwordResetHash.Hash);

            if (!hash)
            {
                return Ok(new
                {
                    success = false,
                    hash = (string?)null,
                    message = "Your password has already been created. If you need to reset your password, go to the login page and click the link labeled \"Forgot Password\"."
                });
            }

            return Ok(new { success = true });
        }

        [HttpPost("update-password")]
		public async Task<IActionResult> UpdatePassword([FromBody] UpdatePassword passwordHash)
		{
			if (string.IsNullOrEmpty(passwordHash.Password)) passwordHash.Password = Guid.NewGuid().ToString();
			passwordHash.Password = new PasswordHasher<object>().HashPassword(new object(), passwordHash.Password);

			var hash = await _authService.ResetPassword(passwordHash.Password, passwordHash.Hash);
			if (string.IsNullOrEmpty(hash))
				return Ok(new { success = false, message = "The link has expired. If you need to reset your password, go to the login page and click the link labeled \"Forgot Password\"" });

			return Ok(new { success = true });
		}

		[HttpPost("one-time-login")]
		public async Task<IActionResult> OneTimeLogin([FromBody] OneTimeLogin username)
		{
			var hash = await _authService.GenerateOneTimeLogin(username.Username);
			if (!string.IsNullOrEmpty(hash))
			{
				System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
				msg.To.Add(username.Username);
				msg.Subject = "One Time Login Link";
				msg.IsBodyHtml = true;
				msg.Body = string.Format(@"
                        <p>The following link can be used to log into the Freedom-Ineteriors portal:<br />
                        <a href=""{0}"">{0}</a></p>
                        <p>Link is only valid for 1 hour.</p>"
							, _authSettings.Domain + "/account/email-auth/" + hash);

				_emailService.SendEmail(msg);

				return Ok(new { success = true, data = hash });
			}

			return Ok(new { success = false, msg = "Invalid email address." });
		}

		[HttpPost("email-auth")]
		public async Task<IActionResult> EmailVerify([FromBody] EmailAuth emailAuthHash)
		{
			var user = await _authService.EmailAuthToken(emailAuthHash.Token);
			if (user != null)
            {
				var result = await _authService.AuthenticateOneTimeEmailLogin(user);
				switch (result.ResponseCode)
				{
					case AuthenticationResponseCode.LocalSuccess:
					case AuthenticationResponseCode.RemoteSuccess:
						{
							var refreshToken = await _authService.GenerateRefreshToken(IPAddress, AppUserId: result.UserId, SpecialUser: result.SpecialUserName);
							int? districtId = null;
							return Ok(new
							{
								success = true,
								data = new
								{
									token = result.JwtToken,
									refreshToken = refreshToken.Token,
									displayName = result.DisplayName,
									email = result.Email,
									districtId,
								}
							});
						}
					case AuthenticationResponseCode.AccountLocked:
						return Ok(new { success = false, message = "Your account has been locked. Too many invalid login attempts." });
					default:
						return Ok(new { success = false, message = "Invalid username and/or password." });
				}
			}
            return Ok(new { success = false, message = "Invalid token." });
        }

        #region "Helpers"
        private string IPAddress
        {
            get
            {
                // get source ip address for the current request
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                    return Request.Headers["X-Forwarded-For"];
                else
                    return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
        }
        #endregion
    }
}
