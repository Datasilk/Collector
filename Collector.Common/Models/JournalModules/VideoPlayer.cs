using System.Text.Json.Serialization;

namespace Collector.Common.Models.JournalModules
{
    /// <summary>
    /// Video player module for journal entries
    /// </summary>
    public class VideoPlayer : JournalModule
    {
        /// <summary>
        /// Database ID of the video record
        /// </summary>
        [JsonPropertyName("videoId")]
        public int? VideoId { get; set; }

        /// <summary>
        /// Relative path to the video file (e.g., "entryId/filename.mp4")
        /// </summary>
        [JsonPropertyName("videoPath")]
        public string VideoPath { get; set; }

        /// <summary>
        /// Relative path to the thumbnail image
        /// </summary>
        [JsonPropertyName("thumbnailPath")]
        public string ThumbnailPath { get; set; }

        /// <summary>
        /// Original URL of the video (e.g., YouTube URL)
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// Title of the video
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Whether the video has been successfully downloaded
        /// </summary>
        [JsonPropertyName("downloaded")]
        public bool? Downloaded { get; set; }

        /// <summary>
        /// Flag to trigger automatic download when module is created
        /// </summary>
        [JsonPropertyName("autoTryAgain")]
        public bool? AutoTryAgain { get; set; }

        /// <summary>
        /// Width of the video in pixels
        /// </summary>
        [JsonPropertyName("width")]
        public int? Width { get; set; }

        /// <summary>
        /// Height of the video in pixels
        /// </summary>
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        /// <summary>
        /// Duration of the video in seconds
        /// </summary>
        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        public VideoPlayer()
        {
            Type = "video-player";
        }
    }
}
