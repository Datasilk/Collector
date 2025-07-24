using System;
using Collector.Data.Enums;

namespace Collector.Data.Entities
{
    public class Domain
    {
        public int domainId { get; set; }
        public bool paywall { get; set; }
        public bool free { get; set; }
        public bool https { get; set; }
        public bool www { get; set; }
        public bool empty { get; set; }
        public bool deleted { get; set; }
        public DomainType type { get; set; }
        public DomainType type2 { get; set; }
        public string domain { get; set; }
        public string lang { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTime lastchecked { get; set; }
        public int articles { get; set; }
        public int inqueue { get; set; }
        public bool whitelisted { get; set; }
        public bool blacklisted { get; set; }
    }
}
