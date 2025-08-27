using System;
using System.Collections.Generic;
using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.API.Models
{
    public class DownloadQueueItemModel
    {
        public long QueueId { get; set; }
        public string Url { get; set; }
        public string Domain { get; set; }
        public int ParentId { get; set; }
        public int FeedId { get; set; }
        public QueueStatus Status { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class AddQueueItemModel
    {
        public string Url { get; set; }
        public string Domain { get; set; }
        public int ParentId { get; set; } = 0;
        public int FeedId { get; set; } = 0;
    }

    public class AddQueueItemsModel
    {
        public string[] Urls { get; set; }
        public string Domain { get; set; }
        public int ParentId { get; set; } = 0;
        public int FeedId { get; set; } = 0;
    }

    public class UpdateQueueItemModel
    {
        public long QueueId { get; set; }
        public QueueStatus Status { get; set; } = QueueStatus.downloaded;
    }

    public class UpdateUrlModel
    {
        public long QueueId { get; set; }
        public string Url { get; set; }
        public string Domain { get; set; }
    }

    public class CheckQueueModel
    {
        public int FeedId { get; set; } = 0;
        public string Domain { get; set; } = "";
        public int DomainDelay { get; set; } = 60;
        public QueueSort Sort { get; set; } = QueueSort.Newest;
    }

    public class QueueItemResponse
    {
        public DownloadQueue QueueItem { get; set; }
    }

    public class QueueCountResponse
    {
        public int Count { get; set; }
    }

    public class QueueActionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class AddQueueItemResponse
    {
        public long QueueId { get; set; }
    }

    public class AddQueueItemsResponse
    {
        public int Count { get; set; }
    }
}
