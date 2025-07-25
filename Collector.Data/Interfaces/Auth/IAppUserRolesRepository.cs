using Collector.Data.Entities.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Data.Interfaces.Users
{
    public interface IAppUserRolesRepository : IDisposable
    {
        Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
        Task<bool> HasRoleAsync(Guid userId, string roleName);
        Task AddRoleAsync(Guid userId, string roleName);
    }
}
