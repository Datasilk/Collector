using Collector.Data.Entities.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Data.Interfaces.Users
{
    public interface IAppUserTokenRepository
    {
        Task Add(AppUserTokens token);
        Task<AppUserTokens> FindByToken(string token);
        Task<AppUserTokens> FindByTokenIP(string token, string userIP);
        Task<bool> IsTokenUnique(string token);
        Task<bool> IsTokenValid(string token);
        Task ExpireToken(string token);
        Task ExpireAllTokens(Guid siteUserId);
        Task ExtendRefreshToken(AppUserTokens token);
    }
}
