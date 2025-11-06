using System;
using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.API.Models
{
    public class JournalModuleResortModel
    {
        public int JournalId { get; set; }
        public List<ModuleSortItem> Modules { get; set; }
    }

    public class ModuleSortItem
    {
        public Guid? JournalEntryId { get; set; }
        public string ModuleId { get; set; }
    }
}
