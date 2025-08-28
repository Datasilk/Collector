namespace Collector.Data.Entities
{
    public class AnalyzerRule
    {
        public int ruleId { get; set; }
        public int domainId { get; set; }
        public string selector { get; set; }
        public bool rule { get; set; }
        public DateTime datecreated { get; set; }
    }
}
