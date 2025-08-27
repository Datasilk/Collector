using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class DomainModel
    {
        [Required]
        public string Domain { get; set; }
    }

    public class DomainsListModel
    {
        [Required]
        public string[] Domains { get; set; }
    }

    public class BlacklistDomainResponse
    {
        public string Domain { get; set; }
        public bool IsBlacklisted { get; set; }
    }

    public class BlacklistDomainsResponse
    {
        public List<BlacklistDomainResponse> Results { get; set; }
    }
}
