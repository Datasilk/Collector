using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collector.Data.Entities
{
    public class JournalEntry
    {
        public Guid Id { get; set; }
        public int JournalId { get; set; }
        public Guid? ParentEntryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int Status { get; set; }
        public int? ChapterId { get; set; }
        public bool Encrypted { get; set; }
        public string Thumbnail { get; set; }
        public string ThumbnailModuleId { get; set; }
        public bool Favorite { get; set; }

        [NotMapped]
        public List<JournalEntryTag> Tags { get; set; } = new List<JournalEntryTag>();

        [NotMapped]
        public string ParentEntryName { get; set; }
    }
}
