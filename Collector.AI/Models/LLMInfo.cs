using System.Collections.Generic;

namespace Collector.AI.Models
{
    public class LLMInfo
    {
        public string PrivateKey { get; set; }
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public Dictionary<string, object> ExtraBody { get; set; }
    }
}
