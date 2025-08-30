using Collector.Data.Entities.Auth;
using Collector.Data.Models;

namespace Collector.Data.Interfaces.Users
{
    public interface IAppUserRepository : IDisposable
    {
        int GetTotalUsers();
        Task<IEnumerable<AppUser>> GetAll();
        Task<(IList<FilteredUserResult> items, int totalCount)> GetAllFiltered(string fullName, int role, int radioStationId, string sort, int page = 1, int pageSize = 10);
        Task<AppUser> FindByUserEmailAsync(string emailAddress, bool activeOnly = true);
		Task<AppUser> FindByGuidAsync(Guid userId, bool activeOnly = false);
        Task<AppUser> GetRolesByUserEmailAsync(string emailAddress);

        Task<AppUser> Add(AppUser user);
        void UpdateInfo(AppUser user);
        void UpdateEmail(AppUser user);

        Task UpdateRole(Guid siteUserId, List<Guid> roleIds);
        Task<bool> UpdateLock(Guid UserId, bool lockUser);
        Task ActivateAccount(AppUser user);
        Task<AppUser> UpdatePasswordResetHash(AppUser user);
        
        Task DeleteUserAsync(Guid userId);
        
		Task<AppUser> FindByPasswordResetHashAsync(string hashPassword, bool activeOnly = true);
		Task<AppUser> UpdatePasswordHash(AppUser user);

		Task<AppUser> FindByOneTimeLoginToken(string emailAuthToken, bool activeOnly = true);
		Task<AppUser> UpdateOneTimeLoginToken(AppUser user);
    }
}
