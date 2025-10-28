using System;

namespace Collector.API.Models
{
    public class UpdateEntryThumbnailModel
    {
        public Guid Id { get; set; }
        public string Thumbnail { get; set; }
    }
}
