using System;

namespace Collector.Data.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }
        public Guid AppUserId { get; set; }
        public string Title { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int Status { get; set; }
    }
}
