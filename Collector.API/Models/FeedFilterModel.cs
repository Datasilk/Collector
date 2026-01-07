namespace Collector.API.Models
{
    public class FeedFilterModel
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public string Search { get; set; }
        public string Sort { get; set; }
    }
}
