using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Collector.Data.Entities
{
    public class DomainTypeMatch
    {
        public int matchId { get; set; }
        public int type { get; set; }
        public int type2 { get; set; }
        public string words { get; set; }
        public List<DomainTypeMatchPart> parts  { get; set; }
    }

    public class DomainTypeMatchPart
    {
        [JsonPropertyName("w")]
        public string[] words { get; set; }
        [JsonPropertyName("t")]
        public int threshold { get; set; }
    }
}
