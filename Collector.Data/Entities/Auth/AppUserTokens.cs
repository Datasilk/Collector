using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Data.Entities.Auth
{
    [Table("AppUserTokens")]
    public class AppUserTokens
    {
        public string Token { get; set; }
        public Guid? AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }
        public bool IsSpecialUser { get; set; } = false;
        public string SpecialUserName { get; set; } = string.Empty;
        
        public DateTime Expiry { get; set; }
        public DateTime Created { get; set; }
        public string IPAddress { get; set; }
        public DateTime? Revoked { get; set; }
        public string ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow > Expiry;
        public bool IsRevoked => Revoked.HasValue || ReplacedByToken != null;
        public bool IsActive => !(IsRevoked || IsExpired);

    }
}
