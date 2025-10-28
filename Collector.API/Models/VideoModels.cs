using System;

namespace Collector.API.Models
{
    public class DeleteVideoModel
    {
        public Guid EntryId { get; set; }
        public string ModuleId { get; set; }
        public bool DeleteFiles { get; set; }
    }
}
