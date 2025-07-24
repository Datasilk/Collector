using System;
using System.Collections.Generic;

namespace Collector.Data.Entities
{
    public class DomainCollection
    {
        public int colId { get; set; }
        public int colgroupId { get; set; }
        public string name { get; set; }
        public string search { get; set; }
        public int subjectId { get; set; }
        public DomainFilterType filtertype { get; set; }
        public DomainType type { get; set; }
        public DomainSort sort { get; set; }
        public string lang { get; set; }
        public DateTime datecreated { get; set; }
        public string groupName { get; set; }
    }

    public class DomainCollectionsAndGroups
    {
        public List<DomainCollection> Collections { get; set; }
        public List<CollectionGroup> Groups { get; set; }
    }
}
