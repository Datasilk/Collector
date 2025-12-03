using System;

namespace Collector.API.Models
{
    public class JournalEntryParentModel
    {
        public Guid Id { get; set; }
        public Guid? ParentEntryId { get; set; }
    }
}
