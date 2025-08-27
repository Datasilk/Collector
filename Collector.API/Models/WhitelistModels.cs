using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class WhitelistDomainModel
    {
        [Required]
        [MaxLength(255)]
        public string Domain { get; set; }
    }
}
