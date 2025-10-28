using System;

namespace Collector.Data.Entities
{
    public class JournalImage
    {
        public int Id { get; set; }
        public int JournalId { get; set; }
        public Guid JournalEntryId { get; set; }
        public string ModuleId { get; set; }
        public string Filename { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
