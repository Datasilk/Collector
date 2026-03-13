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
            chat.Id = Guid.NewGuid();
            _dbConnection.Execute(@"
                INSERT INTO public.""Chats"" 
                (""Id"", ""AppUserId"", ""Title"", ""Status"") 
                VALUES (@Id, @AppUserId, @Title, @Status)", 
                chat);
            return chat.Id;
        }

        public Chat GetById(Guid chatId)
        {
            return _dbConnection.QuerySingleOrDefault<Chat>(@"
                SELECT * FROM public.""Chats"" 
                WHERE ""Id"" = @chatId AND ""Status"" > 0", 
                new { chatId });
        }

        public List<Chat> GetAllByUserId(Guid appUserId)
        {
            return _dbConnection.Query<Chat>(@"
                SELECT * FROM public.""Chats"" 
                WHERE ""AppUserId"" = @appUserId AND ""Status"" > 0
                ORDER BY ""Modified"" DESC", 
                new { appUserId }).ToList();
        }

        public List<Chat> GetByUserIdPaginated(Guid appUserId, int start, int length)
        {
            return _dbConnection.Query<Chat>(@"
                SELECT * FROM public.""Chats"" 
                WHERE ""AppUserId"" = @appUserId AND ""Status"" > 0
                ORDER BY ""Modified"" DESC
                LIMIT @length OFFSET @start", 
                new { appUserId, start, length }).ToList();
        }

        public void UpdateTitle(Guid chatId, string title)
        {
            _dbConnection.Execute(@"
                UPDATE public.""Chats"" 
                SET ""Title"" = @title, ""Modified"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""Id"" = @chatId", 
                new { chatId, title });
        }

        public void UpdateModified(Guid chatId)
        {
            _dbConnection.Execute(@"
                UPDATE public.""Chats"" 
                SET ""Modified"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""Id"" = @chatId", 
                new { chatId });
        }

        public void Delete(Guid chatId)
        {
            _dbConnection.Execute(@"
                UPDATE public.""Chats"" 
                SET ""Status"" = 0
                WHERE ""Id"" = @chatId", 
                new { chatId });
        }

        // Chat History
        public Guid AddMessage(ChatHistory message)
        {
            message.Id = Guid.NewGuid();
            _dbConnection.Execute(@"
                INSERT INTO public.""ChatHistory"" 
                (""Id"", ""ChatId"", ""Role"", ""Content"", ""Model"", ""Status"") 
                VALUES (@Id, @ChatId, @Role, @Content, @Model, @Status)", 
                message);
            return message.Id;
        }

        public List<ChatHistory> GetMessagesByChatId(Guid chatId)
        {
            return _dbConnection.Query<ChatHistory>(@"
                SELECT * FROM public.""ChatHistory"" 
                WHERE ""ChatId"" = @chatId AND ""Status"" > 0
                ORDER BY ""Created"" ASC", 
                new { chatId }).ToList();
        }

        public void DeleteMessage(Guid messageId)
        {
            _dbConnection.Execute(@"
                UPDATE public.""ChatHistory"" 
                SET ""Status"" = 0
                WHERE ""Id"" = @messageId", 
                new { messageId });
        }

        // Chat Context (RAG)
        public void StoreContext(Guid appUserId, Guid chatId, string content, float[] embedding, string metadata = null)
        {
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embedding);
            _dbConnection.Execute(@"
                INSERT INTO public.""ChatContextChunks"" 
                (""AppUserId"", ""ChatId"", ""Content"", ""Embedding"", ""Metadata"") 
                VALUES (@appUserId, @chatId, @content, CAST(@embeddingJson AS VECTOR(768)), @metadata)", 
                new { appUserId, chatId, content, embeddingJson, metadata });
        }

        public List<(string Content, string Metadata, float Distance)> GetContext(Guid appUserId, float[] queryEmbedding, int topK = 5)
        {
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(queryEmbedding);
            var results = _dbConnection.Query<ContextResult>(@"
                SELECT 
                    ""Content"",
                    ""Metadata"",
                    (""Embedding"" <=> CAST(@embeddingJson AS VECTOR(768))) AS Distance
                FROM public.""ChatContextChunks""
                WHERE ""AppUserId"" = @appUserId
                ORDER BY ""Embedding"" <=> CAST(@embeddingJson AS VECTOR(768))
                LIMIT @topK", 
                new { topK, embeddingJson, appUserId });

            return results.Select(r => (r.Content, r.Metadata, r.Distance)).ToList();
        }

        public bool HasSimilarContext(Guid appUserId, float[] embedding, float similarityThreshold = 0.1f)
        {
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embedding);
            var minDistance = _dbConnection.ExecuteScalar<float?>(@"
                SELECT MIN(""Embedding"" <=> CAST(@embeddingJson AS VECTOR(768))) AS Distance
                FROM public.""ChatContextChunks""
                WHERE ""AppUserId"" = @appUserId", 
                new { embeddingJson, appUserId });

            return minDistance.HasValue && minDistance.Value <= similarityThreshold;
        }

        private class ContextResult
        {
            public string Content { get; set; }
            public string Metadata { get; set; }
            public float Distance { get; set; }
        }
    }
}
