using System.Collections.Generic;

namespace Collector.Data.Entities
{
    public class JournalEntryFilterResult
    {
        public List<JournalEntry> Entries { get; set; }
        public int TotalCount { get; set; }
    }
}
