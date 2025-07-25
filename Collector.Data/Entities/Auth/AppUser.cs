using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Collector.Data.Entities.Auth
{
    [Table("AppUsers")]
    public class AppUser
    {
        public Guid? Id { get; set; }
        [Required]
        public string FullName { get; set; } = "";
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        public bool EmailConfirmed { get; set; } = false;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime? LockoutEndDate { get; set; } = null;
        public bool LockoutEnabled { get; set; } = false;
        public int AccessFailedCount { get; set; } = 0;
        public DateTime? AccessFailedTime { get; set; } = null;

        public string PasswordResetHash { get; set; } = string.Empty;
        public DateTime? PasswordResetTime { get; set; } = null;
        public string NewEmail { get; set; } = string.Empty;

        public string OneTimeLoginToken { get; set; } = string.Empty;
        public DateTime? OneTimeLoginExpiry { get; set; } = null;

        public AppUserStatus Status { get; set; }
        public DateTime Created { get; set; }

        public virtual ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
        public virtual ICollection<AppUserTokens> UserTokens { get; set; } = new List<AppUserTokens>();

        [NotMapped]
        public string password { get; set; } = "";
        [NotMapped]
        public bool IsAdmin { get; set; } = false;
        [NotMapped]
        public DateTime? LastLogin { get; set; }
    }

    public enum AppUserStatus
    {
        Pending,
        Active,
        Inactive,
        Deleted
    }
}
