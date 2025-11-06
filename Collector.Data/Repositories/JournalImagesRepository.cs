using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalImagesRepository : IJournalImagesRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalImagesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(JournalImage image)
        {
            var id = _dbConnection.QuerySingle<int>(@"
                INSERT INTO [dbo].[JournalImages] 
                ([JournalId], [JournalEntryId], [ModuleId], [Filename], [Width], [Height])
                OUTPUT INSERTED.Id
                VALUES (@JournalId, @JournalEntryId, @ModuleId, @Filename, @Width, @Height)", 
                image);
            return id;
        }

        public JournalImage GetById(int id)
        {
            return _dbConnection.QuerySingleOrDefault<JournalImage>(@"
                SELECT * FROM [dbo].[JournalImages] 
                WHERE [Id] = @id", 
                new { id });
        }

        public JournalImage GetByModuleId(Guid entryId, string moduleId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalImage>(@"
                SELECT * FROM [dbo].[JournalImages] 
                WHERE [JournalEntryId] = @entryId 
                AND [ModuleId] = @moduleId", 
                new { entryId, moduleId });
        }

        public List<JournalImage> GetAllByEntryId(Guid entryId)
        {
            return _dbConnection.Query<JournalImage>(@"
                SELECT * FROM [dbo].[JournalImages] 
                WHERE [JournalEntryId] = @entryId", 
                new { entryId }).ToList();
        }

        public List<JournalImage> GetAllByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalImage>(@"
                SELECT * FROM [dbo].[JournalImages] 
                WHERE [JournalId] = @journalId", 
                new { journalId }).ToList();
        }

        public void Update(JournalImage image)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalImages] 
                SET [Filename] = @Filename, 
                    [Width] = @Width, 
                    [Height] = @Height 
                WHERE [Id] = @Id", 
                image);
        }

        public void Delete(int id)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalImages] 
                WHERE [Id] = @id", 
                new { id });
        }

        public void DeleteByModuleId(Guid entryId, string moduleId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalImages] 
                WHERE [JournalEntryId] = @entryId 
                AND [ModuleId] = @moduleId", 
                new { entryId, moduleId });
        }

        public void DeleteAllByEntryId(Guid entryId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalImages] 
                WHERE [JournalEntryId] = @entryId", 
                new { entryId });
        }
    }
}
