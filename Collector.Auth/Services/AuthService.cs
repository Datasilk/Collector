using Collector.Auth.Authentication;
using Collector.Auth.Models;
using Collector.Auth.RemoteAuthentication;
using Collector.Data.Entities.Auth;
using Collector.Data.Interfaces.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Collector.Auth.Policies;

namespace Collector.Auth.Services
{
    public interface IAuthService
    {
        public AuthSettings Settings();

        Task<AuthenticationResponse> Authenticate(string userName, string password, string UserIP);
        Task<AppUserTokens> GenerateRefreshToken(string UserIP, Guid? AppUserId = null, string SpecialUser = "");
        Task<string> RefreshUserToken(string token, string UserIP);
        Task<AppUser?> ActivateAccount(string hash);

        Task<string> GenerateResetPasswordHash(string emailAddress, int hours = 1);
        void GenerateResetPasswordHash(AppUser user, int hours = 1);
        
		Task<bool> CheckPasswordResetHash(string hashPassword);
		Task<string> ResetPassword(string newPasswordHash, string resetHash);

		Task<string> GenerateOneTimeLogin(string emailAddress);
		Task<AppUser> EmailAuthToken(string token);
		Task<AuthenticationResponse> AuthenticateOneTimeEmailLogin(AppUser user);
	}

    public class AuthService : IAuthService
    {
        readonly IAppUserRepository _userRepo;
        readonly IAppRoleRepository _roleRepo;
        readonly IAppUserTokenRepository _tokenRepo;

        readonly IRemoteAuth _remoteAuthService;

        public AuthSettings _authSettings;

        public AuthService(
            IOptions<AuthSettings> authSettings,
            IAppUserRepository userRepo,
            IAppRoleRepository roleRepo,
            IAppUserTokenRepository tokenRepo,
            IRemoteAuth remoteAuthService
            )
        {
            _authSettings = authSettings.Value;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _tokenRepo = tokenRepo;
            _remoteAuthService = remoteAuthService;
        }

        #region "Authentication"

        protected string GenerateToken(ClaimsIdentity identity)
        {
            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
            {
                Issuer = _authSettings.JWT.ValidIssuer,
                Audience = _authSettings.JWT.ValidAudience,
                Subject = identity,
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_authSettings.JWT.ExpiryMins)),
                SigningCredentials = new SigningCredentials(
                             new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                             _authSettings.JWT.Secret)),
                             SecurityAlgorithms.HmacSha256Signature)
            };
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            SecurityToken secToken = new JwtSecurityTokenHandler().CreateToken(descriptor);
            return handler.WriteToken(secToken);
        }

        public async Task<AuthenticationResponse> Authenticate(string UserName, string Password, string UserIP)
        {
            string token = string.Empty;
            var response = new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.RemoteSuccess };
            if (!UserName.Contains('@'))
            {
                //used for authenticating remote users from Active Directory
                if (await _remoteAuthService.AuthenticateAsync(UserName, Password, _authSettings.CentralAuthKey, UserIP) == RemoteAuthenicationResponse.Valid)
                {
                    var imToken = await GenerateSpecialUserLogin(UserName, response);
                    response.JwtToken = imToken;
                    response.DisplayName = $"{UserName} (Admin)";
                    response.Email = $"{UserName}@collector.ai";
                    return response;
                }
                return new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.Unauthorized };    
            }
            else 
                return await AuthorizeLocalUser(UserName, Password, UserIP);
        }

        private async Task<AuthenticationResponse> AuthorizeLocalUser(string UserName, string Password, string UserIP)
        {
            
            var user = await _userRepo.FindByUserEmailAsync(UserName);
            if (user == null || user.EmailConfirmed == false) return new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.Unauthorized };
            if (user.AccessFailedCount > 5) return new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.AccountLocked };

            var loginResult = new PasswordHasher<object>().VerifyHashedPassword(new object(), user.PasswordHash, Password);
            if (loginResult == PasswordVerificationResult.Success)
            {
                //Turn identity to token
                var response = new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.LocalSuccess, UserId = user.Id, DisplayName = user.FullName, Email = user.Email };
                string token = await GenerateLocalUserLogin(user, response);
                response.JwtToken = token;
                return response;
            }

            return new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.Unauthorized };

        }

        private async Task<string> GenerateLocalUserLogin(AppUser user, AuthenticationResponse response = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("LastUpdated", DateTime.UtcNow.ToString()),
                new Claim("AppUser", user.Id.ToString()),
            };

            var roles = new List<string>();

            foreach (var role in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.AppRole.Name));
                roles.Add(role.AppRole.Name);
            }

            if (response != null)
            {
                response.Claims = claims;
                response.Roles = roles;
            }

            ClaimsIdentity userIdentity = new ClaimsIdentity(claims, "login");
            return GenerateToken(userIdentity);
        }

        private async Task<string> GenerateSpecialUserLogin(string userName, AuthenticationResponse response = null)
        {
            var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, $"{userName}@collector.ai"),
                            new Claim(ClaimTypes.Email, $"{userName}@collector.ai"),
                            new Claim(ClaimTypes.Role, nameof(AuthConstants.RoleType.admin)),
                            new Claim("lastupdated", DateTime.UtcNow.ToString()),
                            new Claim("AppUser", Guid.NewGuid().ToString())
            };

            //add all roles to admin user
            var allRoles = await _roleRepo.GetAll();
            var roles = new List<string>();
            foreach (var role in allRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                roles.Add(role.Name);
            }

            if (response != null)
            {
                response.Claims = claims;
                response.Roles = roles;
            }
            ClaimsIdentity userIdentity = new ClaimsIdentity(claims, "login");
            
            return GenerateToken(userIdentity);
        }

        public async Task<AppUserTokens> GenerateRefreshToken(string UserIP, Guid? AppUserId = null, string SpecialUser = "")
        {
            var token = new AppUserTokens
            {
                Token = await GetUniqueToken(),
                AppUserId = AppUserId,
                IsSpecialUser = (!string.IsNullOrEmpty(SpecialUser)),
                SpecialUserName = SpecialUser,
                Expiry = DateTime.UtcNow.AddMinutes(int.Parse(_authSettings.JWT.RefreshExpiryMins)),
                Created = DateTime.UtcNow,
                IPAddress = UserIP
            };
            if (!await _tokenRepo.IsTokenUnique(token.Token)) return await GenerateRefreshToken(UserIP);
            
            await _tokenRepo.Add(token);
            return token;
        }

        public async Task<string> RefreshUserToken(string token, string UserIP)
        {
            var refreshToken = await _tokenRepo.FindByTokenIP(token, UserIP);
            if (refreshToken != null && refreshToken.IsActive)
            {
                if (_authSettings.JWT.UseRollingRefreshTokens)
                {
                    //Set a rolling expiry on refresh token

                    refreshToken.Expiry = DateTime.UtcNow.AddMinutes(int.Parse(_authSettings.JWT.RefreshExpiryMins));
                    await _tokenRepo.ExtendRefreshToken(refreshToken);
                }

                if (refreshToken.AppUserId.HasValue)
                {
                    var user = await _userRepo.FindByGuidAsync(refreshToken.AppUserId.Value);
                    return await GenerateLocalUserLogin(user);
                }
                else if (refreshToken.IsSpecialUser)
                {
                    return await GenerateSpecialUserLogin(refreshToken.SpecialUserName);

                }
            }
            return "";
        }

        private async Task<string> GetUniqueToken()
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            if (await _tokenRepo.IsTokenUnique(token)) return token;
            return await GetUniqueToken();
        }


        public async Task<AppUser?> ActivateAccount(string hash)
        {
            var user = await _userRepo.FindByPasswordResetHashAsync(hash);

            if (user != null)
            {
                await _userRepo.ActivateAccount(user);
            }
            return user;
        }

        #endregion

        #region "Reset Password"

        public async Task<string> GenerateResetPasswordHash(string emailAddress, int hours = 1)
        {
            var hash = Guid.NewGuid().ToString("N");
            var user = await _userRepo.FindByUserEmailAsync(emailAddress);
            if (user != null)
            {
                user.PasswordResetTime = DateTime.UtcNow.AddHours(hours);
                user.PasswordResetHash = hash;
                await _userRepo.UpdatePasswordResetHash(user);
            }
            return hash;
        }

        public void GenerateResetPasswordHash(AppUser user, int hours = 1)
        {
            var hash = Guid.NewGuid().ToString("N");
            if (user != null)
            {
                user.PasswordResetTime = DateTime.UtcNow.AddHours(hours);
                user.PasswordResetHash = hash;
            }
        }

        public async Task<bool> CheckPasswordResetHash(string hash)
        {
            var user = await _userRepo.FindByPasswordResetHashAsync(hash);
            if ((user != null && !user.EmailConfirmed) || 
                (user != null && user.EmailConfirmed && user.PasswordResetTime != null && user.PasswordResetTime.Value >= DateTime.UtcNow))
            {
                return true;
            }
            return false;
        }


        public async Task<string> ResetPassword(string newPasswordHash, string resetHash)
        {
            var user = await _userRepo.FindByPasswordResetHashAsync(resetHash);
            if (user != null)
            {
                if (!user.EmailConfirmed && user.PasswordResetHash != null && user.PasswordResetTime != null)
                {
                    user.PasswordResetTime = null;
                    user.PasswordResetHash = null;
                    user.PasswordHash = newPasswordHash;

                    await _userRepo.UpdatePasswordHash(user);
                }
                else if (user.EmailConfirmed && user.PasswordResetHash != null && user.PasswordResetTime != null && user.PasswordResetTime.Value >= DateTime.UtcNow)
                {
                    user.PasswordResetTime = null;
                    user.PasswordResetHash = null;
                    user.PasswordHash = newPasswordHash;

                    await _userRepo.UpdatePasswordHash(user);
                }
                else
                {
                    return string.Empty;
                }
            }

            if (user == null)
                return string.Empty;

            return newPasswordHash;
        }

        #endregion

        #region "One Time Login"

        public async Task<string> GenerateOneTimeLogin(string emailAddress)
		{
			var user = await _userRepo.FindByUserEmailAsync(emailAddress);
			var hash = string.Empty;

			//&& user.OneTimeLoginExpiry == null
			if (user != null)
			{
				hash = Guid.NewGuid().ToString("N");
				user.OneTimeLoginExpiry = DateTime.UtcNow.AddHours(1);
				user.OneTimeLoginToken = hash;
				await _userRepo.UpdateOneTimeLoginToken(user);
			}
			return hash;
		}

        public async Task<AppUser> EmailAuthToken(string token)
        {			
			var user = await _userRepo.FindByOneTimeLoginToken(token);
            if (user != null)
            {
				user = await _userRepo.FindByUserEmailAsync(user.Email);
				if (user.OneTimeLoginExpiry != null && user.OneTimeLoginExpiry.Value >= DateTime.UtcNow)
                {
                    user.OneTimeLoginExpiry = null;
                    user.OneTimeLoginToken = null;
                    await _userRepo.UpdateOneTimeLoginToken(user);
                }
                else
                {
                    return null;
                }
            }
			return user;
		}

		public async Task<AuthenticationResponse> AuthenticateOneTimeEmailLogin(AppUser user)
        {
			if (user != null)
			{
				var response = new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.LocalSuccess, UserId = user.Id, DisplayName = user.FullName, Email = user.Email };
				string token = await GenerateLocalUserLogin(user, response);
				response.JwtToken = token;
				return response;
			}
			return new AuthenticationResponse { ResponseCode = AuthenticationResponseCode.Unauthorized };
		}

        #endregion

        public AuthSettings Settings()
        {
            return _authSettings;
        }
    }
}
