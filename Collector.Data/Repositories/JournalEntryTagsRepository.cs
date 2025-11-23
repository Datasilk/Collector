using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Collector.Data.Repositories
{
    public class JournalEntryTagsRepository : IJournalEntryTagsRepository
    {
        private readonly IDbConnection _dbConnection;

        public JournalEntryTagsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void Add(JournalEntryTag entryTag)
        {
            _dbConnection.Execute(@"
                INSERT INTO [dbo].[JournalEntryTags] ([TagId], [JournalEntryId])
                VALUES (@TagId, @JournalEntryId)", entryTag);
        }

        public void Remove(int tagId, Guid journalEntryId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalEntryTags]
                WHERE [TagId] = @TagId AND [JournalEntryId] = @JournalEntryId",
                new { TagId = tagId, JournalEntryId = journalEntryId });
        }

        public void RemoveByEntry(Guid journalEntryId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalEntryTags]
                WHERE [JournalEntryId] = @JournalEntryId",
                new { JournalEntryId = journalEntryId });
        }

        public List<JournalEntryTag> GetByEntryId(Guid journalEntryId)
        {
            return _dbConnection.Query<JournalEntryTag>(@"
                SELECT jet.[TagId], jet.[JournalEntryId], jt.[Tag] AS [Name]
                FROM [dbo].[JournalEntryTags] jet
                INNER JOIN [dbo].[JournalTags] jt ON jt.[Id] = jet.[TagId]
                WHERE jet.[JournalEntryId] = @JournalEntryId",
                new { JournalEntryId = journalEntryId }).ToList();
        }

        public List<int> GetTagIdsByEntry(Guid journalEntryId)
        {
            return _dbConnection.Query<int>(@"
                SELECT [TagId] FROM [dbo].[JournalEntryTags]
                WHERE [JournalEntryId] = @JournalEntryId",
                new { JournalEntryId = journalEntryId }).ToList();
        }

        public List<JournalEntryTag> GetByTagId(int tagId)
        {
            return _dbConnection.Query<JournalEntryTag>(@"
                SELECT jet.[TagId], jet.[JournalEntryId], jt.[Tag] AS [Name]
                FROM [dbo].[JournalEntryTags] jet
                INNER JOIN [dbo].[JournalTags] jt ON jt.[Id] = jet.[TagId]
                WHERE jet.[TagId] = @TagId",
                new { TagId = tagId }).ToList();
        }
    }
}
