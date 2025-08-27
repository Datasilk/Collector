using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Collector.Data.Enums;

namespace Collector.API.Models
{
    public class FeedModel
    {
        public int FeedId { get; set; }
        public int DomainId { get; set; }
        public FeedDocType DocType { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int? CheckIntervals { get; set; }
        public DateTime? LastChecked { get; set; }
        public string Filter { get; set; }
        public string Category { get; set; }
        public int CategoryId { get; set; }
    }

    public class FeedWithLogModel : FeedModel
    {
        public short? LogLinks { get; set; }
        public DateTime? LogDateChecked { get; set; }
    }

    public class FeedCategoryModel
    {
        public int CategoryId { get; set; }
        public string Title { get; set; }
    }

    public class AddFeedModel
    {
        [Required]
        public FeedDocType DocType { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        [Url]
        public string Url { get; set; }
        
        [Required]
        public string Domain { get; set; }
        
        public string Filter { get; set; }
        
        public int CheckIntervals { get; set; } = 720;
    }

    public class AddFeedCategoryModel
    {
        [Required]
        public string Title { get; set; }
    }

    public class LogCheckedLinksModel
    {
        [Required]
        public int FeedId { get; set; }
        
        [Required]
        public int Count { get; set; }
    }

    public class FeedListWithLogsModel
    {
        public int Days { get; set; } = 7;
        public DateTime? DateStart { get; set; }
    }
}
