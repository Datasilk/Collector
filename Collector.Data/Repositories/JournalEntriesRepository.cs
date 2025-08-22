using Dapper;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalEntriesRepository : IJournalEntriesRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalEntriesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public Guid Add(JournalEntry journalEntry)
        {
            journalEntry.Id = Guid.NewGuid();
            _dbConnection.Execute(@"INSERT INTO [dbo].[JournalEntries] 
                ([Id], [JournalId], [Title], [Description]) 
                VALUES (@id, @journalId, @title, @description)", 
                new { 
                    id = journalEntry.Id,
                    journalId = journalEntry.JournalId, 
                    title = journalEntry.Title, 
                    description = journalEntry.Description 
                });
            return journalEntry.Id;
        }

        public void Rename(Guid journalEntryId, string title)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Title] = @title 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, title });
        }

        public void UpdateDescription(Guid journalEntryId, string description)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Description] = @description 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, description });
        }

        public void Archive(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = 2 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void Unarchive(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = 0 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void Publish(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Status] = 1 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void Modify(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Modified] = GETUTCDATE() 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public List<JournalEntry> GetAllByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalEntry>(@"SELECT * FROM [dbo].[JournalEntries] 
                WHERE [JournalId] = @journalId
                ORDER BY [Created] DESC", 
                new { journalId }).ToList();
        }

        public JournalEntry GetById(Guid journalEntryId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalEntry>(@"SELECT * FROM [dbo].[JournalEntries] 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }

        public void UpdateJournalId(Guid journalEntryId, int journalId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [JournalId] = @journalId 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId, journalId });
        }

        public void UpdateLastModified(Guid journalEntryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalEntries] 
                SET [Modified] = GETUTCDATE() 
                WHERE [Id] = @journalEntryId", 
                new { journalEntryId });
        }
    }
}
