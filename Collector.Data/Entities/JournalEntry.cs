using System;

namespace Collector.Data.Entities
{
    public class JournalEntry
    {
        public Guid Id { get; set; }
        public int JournalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int Status { get; set; }
        public int? ChapterId { get; set; }
        public bool Encrypted { get; set; }
        public string Thumbnail { get; set; }
    }
}
