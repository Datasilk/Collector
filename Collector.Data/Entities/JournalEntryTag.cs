using System;

namespace Collector.Data.Entities
{
    public class JournalEntryTag
    {
        public int TagId { get; set; }
        public Guid JournalEntryId { get; set; }
        public string Name { get; set; }
    }
}
