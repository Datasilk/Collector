using System;

namespace Collector.API.Models
{
    public class MoveEntryModel
    {
        public Guid EntryId { get; set; }
        public int TargetJournalId { get; set; }
    }
}
