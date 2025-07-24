using System;

namespace Collector.Data.Entities
{
    public class DownloadRule
    {
        public int ruleId { get; set; }
        public int domainId { get; set; }
        public bool rule { get; set; }
        public string domain { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public string summary { get; set; }
        public DateTime datecreated { get; set; }
    }
}
