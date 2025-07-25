using System.ComponentModel.DataAnnotations.Schema;

namespace Collector.Data.Entities.Auth
{
    [Table("AppUserRoles")]
    public class AppUserRole
    {
        public Guid AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }
        public int AppRoleId { get; set; }
        public virtual AppRole AppRole { get; set; }

    }
}
