using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class NewUser
    {
        [Required]
        public string FullName { get; set; } = "";
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public string Password { get; set; } = "";
        public string? Token { get; set; } // Invite token, optional
    }
}
