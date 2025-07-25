using Collector.Data.Entities.Auth;
using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class UpdateAdminUser
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string FullName { get; set; } = "";
        [Required]
        public bool IsAdmin { get; set; } = false;
        [Required]
        public AppUserStatus Status { get; set; } = AppUserStatus.Active;
    }
}
