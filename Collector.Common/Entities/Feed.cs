using System;
using Collector.Data.Enums;

namespace Collector.Data.Entities
{
    public class Feed
    {
        public int feedId { get; set; }
        public int domainId { get; set; }
        public FeedDocType doctype { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public int? checkIntervals { get; set; }
        public DateTime? lastChecked { get; set; }
        public string filter { get; set; }
        public string category { get; set; }
        public int categoryId { get; set; }
    }

    public class FeedWithLog: Feed
    {
        public short? loglinks { get; set; }
        public DateTime? logdatechecked { get; set; }
    }

    public class FeedCategory
    {
        public int categoryId { get; set; }
        public string title { get; set; }
    }
}
