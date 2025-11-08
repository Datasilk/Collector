using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalFilesRepository : IJournalFilesRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalFilesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(JournalFile file)
        {
            var id = _dbConnection.QuerySingle<int>(@"
                INSERT INTO [dbo].[JournalFiles] 
                ([JournalId], [JournalEntryId], [ModuleId], [Filename], [FileSize], [DateUploaded])
                OUTPUT INSERTED.Id
                VALUES (@JournalId, @JournalEntryId, @ModuleId, @Filename, @FileSize, @DateUploaded)", 
                file);
            return id;
        }

        public JournalFile GetById(int id)
        {
            return _dbConnection.QuerySingleOrDefault<JournalFile>(@"
                SELECT * FROM [dbo].[JournalFiles] 
                WHERE [Id] = @id", 
                new { id });
        }

        public JournalFile GetByModuleId(Guid entryId, string moduleId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalFile>(@"
                SELECT * FROM [dbo].[JournalFiles] 
                WHERE [JournalEntryId] = @entryId 
                AND [ModuleId] = @moduleId", 
                new { entryId, moduleId });
        }

        public List<JournalFile> GetAllByEntryId(Guid entryId)
        {
            return _dbConnection.Query<JournalFile>(@"
                SELECT * FROM [dbo].[JournalFiles] 
                WHERE [JournalEntryId] = @entryId", 
                new { entryId }).ToList();
        }

        public List<JournalFile> GetAllByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalFile>(@"
                SELECT * FROM [dbo].[JournalFiles] 
                WHERE [JournalId] = @journalId", 
                new { journalId }).ToList();
        }

        public void Update(JournalFile file)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalFiles] 
                SET [Filename] = @Filename, 
                    [FileSize] = @FileSize 
                WHERE [Id] = @Id", 
                file);
        }

        public void Delete(int id)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalFiles] 
                WHERE [Id] = @id", 
                new { id });
        }

        public void DeleteByModuleId(Guid entryId, string moduleId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalFiles] 
                WHERE [JournalEntryId] = @entryId 
                AND [ModuleId] = @moduleId", 
                new { entryId, moduleId });
        }

        public void DeleteAllByEntryId(Guid entryId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalFiles] 
                WHERE [JournalEntryId] = @entryId", 
                new { entryId });
        }
    }
}
