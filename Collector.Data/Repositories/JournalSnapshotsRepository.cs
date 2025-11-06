using Dapper;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalSnapshotsRepository : IJournalSnapshotsRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalSnapshotsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(JournalSnapshot snapshot)
        {
            var id = _dbConnection.ExecuteScalar<int>(@"
                DECLARE @id INT = NEXT VALUE FOR [dbo].[SequenceJournalEntrySnapshots];
                INSERT INTO [dbo].[JournalEntrySnapshots] 
                ([Id], [EntryId], [JournalId], [ChapterId], [Title], [Description], [Created], [Modified], [Status], [Encrypted], [Thumbnail])
                VALUES (@id, @entryId, @journalId, @chapterId, @title, @description, @created, @modified, @status, @encrypted, @thumbnail);
                SELECT @id;", 
                new { 
                    entryId = snapshot.EntryId,
                    journalId = snapshot.JournalId,
                    chapterId = snapshot.ChapterId,
                    title = snapshot.Title, 
                    description = snapshot.Description,
                    created = snapshot.Created,
                    modified = snapshot.Modified,
                    status = snapshot.Status,
                    encrypted = snapshot.Encrypted,
                    thumbnail = snapshot.Thumbnail
                });
            return id;
        }

        public JournalSnapshot GetById(int snapshotId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalSnapshot>(@"
                SELECT * FROM [dbo].[JournalEntrySnapshots] 
                WHERE [Id] = @snapshotId", 
                new { snapshotId });
        }

        public List<JournalSnapshot> GetAllByEntryId(Guid entryId)
        {
            return _dbConnection.Query<JournalSnapshot>(@"
                SELECT * FROM [dbo].[JournalEntrySnapshots] 
                WHERE [EntryId] = @entryId
                ORDER BY [CreatedSnapshot] DESC", 
                new { entryId }).ToList();
        }

        public void Delete(int snapshotId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalEntrySnapshots] 
                WHERE [Id] = @snapshotId", 
                new { snapshotId });
        }
    }
}
