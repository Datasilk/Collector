using System;

namespace Collector.Common.Entities
{
    public class JournalCheckListItem
    {
        public int Id { get; set; }
        public int CheckListId { get; set; }
        public string Title { get; set; }
        public int Icon { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
    }
}
