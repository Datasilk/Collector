using System;

namespace Collector.Data.Entities
{
    /// <summary>
    /// A retrieved ChatContextChunks row with its similarity distance and
    /// creation time — used for recency x importance x similarity re-ranking
    /// in the chat memory layer.
    /// </summary>
    public class ContextChunk
    {
        public string Content { get; set; }
        public string Metadata { get; set; }
        public float Distance { get; set; }
        public DateTime Created { get; set; }
    }
}
