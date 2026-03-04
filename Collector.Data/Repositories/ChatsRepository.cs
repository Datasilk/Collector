using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class ChatsRepository : IChatsRepository
    {
        readonly IDbConnection _dbConnection;

        public ChatsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public Guid Add(Chat chat)
        {
            return _dbConnection.QuerySingle<Guid>(@"
                INSERT INTO [dbo].[Chats] 
                ([AppUserId], [Title], [Status]) 
                OUTPUT INSERTED.[Id]
                VALUES (@AppUserId, @Title, @Status)", 
                chat);
        }

        public Chat GetById(Guid chatId)
        {
            return _dbConnection.QuerySingleOrDefault<Chat>(@"
                SELECT * FROM [dbo].[Chats] 
                WHERE [Id] = @chatId AND [Status] > 0", 
                new { chatId });
        }

        public List<Chat> GetAllByUserId(Guid appUserId)
        {
            return _dbConnection.Query<Chat>(@"
                SELECT * FROM [dbo].[Chats] 
                WHERE [AppUserId] = @appUserId AND [Status] > 0
                ORDER BY [Modified] DESC", 
                new { appUserId }).ToList();
        }

        public List<Chat> GetByUserIdPaginated(Guid appUserId, int start, int length)
        {
            return _dbConnection.Query<Chat>(@"
                SELECT * FROM [dbo].[Chats] 
                WHERE [AppUserId] = @appUserId AND [Status] > 0
                ORDER BY [Modified] DESC
                OFFSET @start ROWS
                FETCH NEXT @length ROWS ONLY", 
                new { appUserId, start, length }).ToList();
        }

        public void UpdateTitle(Guid chatId, string title)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[Chats] 
                SET [Title] = @title, [Modified] = GETUTCDATE()
                WHERE [Id] = @chatId", 
                new { chatId, title });
        }

        public void UpdateModified(Guid chatId)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[Chats] 
                SET [Modified] = GETUTCDATE()
                WHERE [Id] = @chatId", 
                new { chatId });
        }

        public void Delete(Guid chatId)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[Chats] 
                SET [Status] = 0
                WHERE [Id] = @chatId", 
                new { chatId });
        }

        // Chat History
        public Guid AddMessage(ChatHistory message)
        {
            return _dbConnection.QuerySingle<Guid>(@"
                INSERT INTO [dbo].[ChatHistory] 
                ([ChatId], [Role], [Content], [Model], [Status]) 
                OUTPUT INSERTED.[Id]
                VALUES (@ChatId, @Role, @Content, @Model, @Status)", 
                message);
        }

        public List<ChatHistory> GetMessagesByChatId(Guid chatId)
        {
            return _dbConnection.Query<ChatHistory>(@"
                SELECT * FROM [dbo].[ChatHistory] 
                WHERE [ChatId] = @chatId AND [Status] > 0
                ORDER BY [Created] ASC", 
                new { chatId }).ToList();
        }

        public void DeleteMessage(Guid messageId)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[ChatHistory] 
                SET [Status] = 0
                WHERE [Id] = @messageId", 
                new { messageId });
        }

        // Chat Context (RAG)
        public void StoreContext(Guid appUserId, Guid chatId, string content, float[] embedding, string metadata = null)
        {
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embedding);
            _dbConnection.Execute(@"
                INSERT INTO [dbo].[ChatContextChunks] 
                ([AppUserId], [ChatId], [Content], [Embedding], [Metadata]) 
                VALUES (@appUserId, @chatId, @content, CAST(@embeddingJson AS VECTOR(768)), @metadata)", 
                new { appUserId, chatId, content, embeddingJson, metadata });
        }

        public List<(string Content, string Metadata, float Distance)> GetContext(Guid appUserId, float[] queryEmbedding, int topK = 5)
        {
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(queryEmbedding);
            var results = _dbConnection.Query<ContextResult>(@"
                SELECT TOP (@topK)
                    [Content],
                    [Metadata],
                    VECTOR_DISTANCE('cosine', [Embedding], CAST(@embeddingJson AS VECTOR(768))) AS Distance
                FROM [dbo].[ChatContextChunks]
                WHERE [AppUserId] = @appUserId
                ORDER BY Distance ASC", 
                new { topK, embeddingJson, appUserId });

            return results.Select(r => (r.Content, r.Metadata, r.Distance)).ToList();
        }

        private class ContextResult
        {
            public string Content { get; set; }
            public string Metadata { get; set; }
            public float Distance { get; set; }
        }
    }
}
