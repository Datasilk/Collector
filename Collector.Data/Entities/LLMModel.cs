using System;

namespace Collector.Data.Entities
{
    public class LLMModel
    {
        public int ModelId { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public string PrivateKey { get; set; }
        public int Type { get; set; }
        public bool Enabled { get; set; }
        public bool Preferred { get; set; }
        public string ExtraBody { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
