using System.Text.Json.Serialization;

namespace Collector.Common.Models
{
    /// <summary>
    /// Base class for all journal module types
    /// </summary>
    public class JournalModule
    {
        /// <summary>
        /// Unique identifier for the module instance
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Type of module (e.g., "video-player", "text-editor", "image")
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
