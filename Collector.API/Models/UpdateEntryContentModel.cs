using System;

namespace Collector.API.Models
{
    public class UpdateEntryContentModel
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
    }
}
