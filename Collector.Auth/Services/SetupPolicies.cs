using Collector.Auth.Policies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Collector.Auth.Services
{
    public static class SetupPolicies
    {
        public static void AddPolicies(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.Add(AuthConstants.Policy.ManageUsers, policy => policy.Admins());
                options.Add(AuthConstants.Policy.ManageRadioStations, policy => policy.Admins());
                options.Add(AuthConstants.Policy.ManagePresentationTemplates, policy => policy.Admins());
                options.Add(AuthConstants.Policy.ManagePresentationThemes, policy => policy.Admins());
            });
        }

        #region "Helpers"

        private static void Add(this AuthorizationOptions options, AuthConstants.Policy type, Action<AuthorizationPolicyBuilder> policy)
        {
            options.AddPolicy(Enum.GetName(type), policy);
        }

        private static AuthorizationPolicyBuilder Admins(this AuthorizationPolicyBuilder policy) { return policy.RequireClaim(ClaimTypes.Role, nameof(AuthConstants.RoleType.admin)); }

        #endregion
    }
}
