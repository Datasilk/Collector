using System;

namespace Collector.API.Models
{
    public class UpdateEntryThumbnailModel
    {
        public Guid Id { get; set; }
        public string Thumbnail { get; set; }
        public string ThumbnailModuleId { get; set; }
        public Guid? SourceEntryId { get; set; }
    }
}
