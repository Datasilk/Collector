using System;
using System.Collections.Generic;

namespace Collector.Data.Entities
{
    public class DownloadQueue
    {
        public Int64 qid { get; set; }
        public int domainId { get; set; }
        public int parentId { get; set; }
        public int feedId { get; set; }
        public int status { get; set; }
        public string url { get; set; }
        public string domain { get; set; }
        public int articles { get; set; }
        public DateTime datecreated { get; set; }
        public List<DownloadRule> downloadRules { get; set; }
    }
}
