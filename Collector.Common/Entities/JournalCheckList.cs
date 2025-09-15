using System;
using System.Collections.Generic;

namespace Collector.Common.Entities
{
    public class JournalCheckList
    {
        public int Id { get; set; }
        public Guid AppUserId { get; set; }
        public Guid EntryId { get; set; }
        public int? ThemeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
        public List<JournalCheckListItem> Items { get; set; }
    }
}
