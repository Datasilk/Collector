using System;

namespace Collector.API.Models
{
    public class UpdateJournalEntryIdModel
    {
        public int JournalId { get; set; }
        public Guid? EntryId { get; set; }
    }
}
