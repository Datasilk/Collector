using Collector.Common.Enums;

namespace Collector.API.Models
{
    public class UpdateQueueTypeModel
    {
        public long QueueId { get; set; }
        public QueueFileType Type { get; set; }
    }
}
