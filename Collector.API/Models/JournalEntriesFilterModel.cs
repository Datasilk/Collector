using System.Collections.Generic;

namespace Collector.API.Models
{
    public class JournalEntriesFilterModel
    {
        public string Search { get; set; }
        public string Sort { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
        public List<int> Tags { get; set; }
    }
}
