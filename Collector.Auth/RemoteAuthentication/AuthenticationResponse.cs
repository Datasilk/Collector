using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Collector.Auth.Authentication
{
    public class AuthenticationResponse
    {
        public Guid? UserId { get; set; }
        public string SpecialUserName { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get;set; }
        public List<Claim> Claims { get; set; } = new List<Claim>();
        public AuthenticationResponseCode ResponseCode { get; set; } = AuthenticationResponseCode.Unauthorized;
    }

    public enum AuthenticationResponseCode
    {
        Unauthorized,
        AccountLocked,
        LocalSuccess,
        RemoteSuccess
    }
}
