using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collector.Data.Entities.Auth;

namespace Collector.Data.Interfaces.Users
{
    public interface IAppRoleRepository : IDisposable
    {
        Task<AppRole> Add(AppRole role);
        Task<AppRole> Edit(AppRole role);
        Task Delete(AppRole role);
        Task<IEnumerable<AppRole>> GetAll();
        Task<AppRole> FindById(int id);
        Task<AppRole> FindByName(string name);
    }
}
