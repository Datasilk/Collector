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
        
        // Chat History
        Guid AddMessage(ChatHistory message);
        List<ChatHistory> GetMessagesByChatId(Guid chatId);
        void DeleteMessage(Guid messageId);
        
        // Chat Context (RAG)
        void StoreContext(Guid appUserId, Guid chatId, string content, float[] embedding, string metadata = null);
        List<(string Content, string Metadata, float Distance)> GetContext(Guid appUserId, float[] queryEmbedding, int topK = 5);
    }
}
