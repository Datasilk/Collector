using System;

namespace Collector.Common.Entities
{
    public class JournalVideo
    {
        public int Id { get; set; }
        public int JournalId { get; set; }
        public Guid JournalEntryId { get; set; }
        public string ModuleId { get; set; }
        public string Filename { get; set; }
        public string OriginalFilename { get; set; }
        public string Url { get; set; }
        public bool Downloaded { get; set; }
        public int Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Metadata { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
