using System;

namespace Collector.Data.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }
        public Guid AppUserId { get; set; }
        public string Title { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int Status { get; set; }
        /// <summary>Journal entry this chat session is currently working on (research-agent sessions).</summary>
        public Guid? JournalEntryId { get; set; }
        /// <summary>Running summary of compacted older turns (session-context compaction).</summary>
        public string ContextSummary { get; set; }
        /// <summary>Messages at or before this timestamp are folded into ContextSummary.</summary>
        public DateTime? ContextSummarizedThrough { get; set; }
        /// <summary>Estimated prompt tokens for the session after the last turn (drives the context bar).</summary>
        public int ContextTokens { get; set; }
    }
}
