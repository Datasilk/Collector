using System;

namespace Collector.Data.Entities
{
    public class JournalFile
    {
        public int Id { get; set; }
        public int JournalId { get; set; }
        public Guid JournalEntryId { get; set; }
        public string ModuleId { get; set; }
        public string Filename { get; set; }
        public long FileSize { get; set; }
        public DateTime DateUploaded { get; set; }
    }
}
