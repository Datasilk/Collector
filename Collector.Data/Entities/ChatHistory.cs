using System;

namespace Collector.Data.Entities
{
    public class ChatHistory
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public int Role { get; set; }
        public string Content { get; set; }
        public string Model { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
    }
}
