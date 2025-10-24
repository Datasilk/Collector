using System;

namespace Collector.API.Models
{
    public class UpdateEntryCreatedModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
    }
}
