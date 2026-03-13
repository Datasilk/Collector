using System;

namespace Collector.Data.Entities
{
    public class OllamaModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public int Status { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
