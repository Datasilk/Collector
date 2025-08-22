using System;

namespace Collector.API.Models
{
    public class JournalEntryModel
    {
        public Guid Id { get; set; }
        public int JournalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
