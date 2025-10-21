using System;

namespace Collector.API.Models
{
    public class JournalModuleDeleteModel
    {
        public int JournalId { get; set; }
        public Guid EntryId { get; set; }
        public string ModuleId { get; set; }
    }
}
