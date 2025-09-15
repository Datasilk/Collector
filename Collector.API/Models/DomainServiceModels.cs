namespace Collector.API.Models
{
    public class DomainServiceSearchModel
    {
        public string Search { get; set; } = "";
        public int Start { get; set; } = 0;
        public int Length { get; set; } = 50;
    }
}
