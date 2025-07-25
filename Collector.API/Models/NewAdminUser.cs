using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class NewAdminUser
    {
        [Required]
        public string FullName { get; set; } = "";
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public int? Role { get; set; } = 2;
        [Required]
        public bool IsAdmin { get; set; } = false;
    }
}
