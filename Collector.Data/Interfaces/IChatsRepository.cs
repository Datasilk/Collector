using System;
using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface IChatsRepository
    {
        Guid Add(Chat chat);
        Chat GetById(Guid chatId);
        List<Chat> GetAllByUserId(Guid appUserId);
        List<Chat> GetByUserIdPaginated(Guid appUserId, int start, int length);
        void UpdateTitle(Guid chatId, string title);
        void UpdateModified(Guid chatId);
        void Delete(Guid chatId);
        void SetJournalEntryId(Guid chatId, Guid? journalEntryId);
        /// <summary>Persist compaction state: running summary + the timestamp boundary + token estimate.</summary>
        void SetContextCompaction(Guid chatId, string summary, DateTime summarizedThrough, int tokensUsed);
        /// <summary>Update only the token estimate (each turn, no compaction).</summary>
        void SetContextTokens(Guid chatId, int tokensUsed);
        
        // Chat History
        Guid AddMessage(ChatHistory message);
        List<ChatHistory> GetMessagesByChatId(Guid chatId);
        void DeleteMessage(Guid messageId);
        
        // Chat Context (RAG)
        void StoreContext(Guid appUserId, Guid chatId, string content, float[] embedding, string metadata = null);
        List<(string Content, string Metadata, float Distance)> GetContext(Guid appUserId, float[] queryEmbedding, int length = 5, float? distance = null);
        List<ContextChunk> GetContextCandidates(Guid appUserId, float[] queryEmbedding, int fetchCount, float? distance = null);
        /// <summary>Chat-scoped duplicate check for session memory notes.</summary>
        bool HasSimilarContextForChat(Guid chatId, float[] embedding, float similarityThreshold = 0.1f);
        List<(string Content, string Metadata)> GetRecentContextByMetadataKind(Guid chatId, string kind, int length = 5);
        bool HasSimilarContext(Guid appUserId, float[] embedding, float similarityThreshold = 0.1f);
    }
}
