namespace Collector.CyberScout.Models
{
    public class DomainInfo
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> Type { get; set; } = new List<string>();

        public List<string> Services { get; set; } = new List<string>();

        public string Company { get; set; } = string.Empty;

        public bool PayWall { get; set; } = false;

        public bool Free { get; set; } = true;
    }
}
