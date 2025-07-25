using System.ComponentModel.DataAnnotations;

namespace Collector.Auth.Models
{
    public class LoginCredentials
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
