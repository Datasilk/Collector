using Collector.Common.Entities;
using Collector.Data.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Collector.Data.Repositories
{
    public class JournalVideosRepository : IJournalVideosRepository
    {
        private readonly IDbConnection _db;

        public JournalVideosRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<JournalVideo> GetById(int id)
        {
            var sql = @"SELECT * FROM JournalVideos WHERE Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<JournalVideo>(sql, new { Id = id });
        }

        public async Task<JournalVideo> GetByModuleId(string moduleId)
        {
            var sql = @"SELECT * FROM JournalVideos WHERE ModuleId = @ModuleId";
            return await _db.QueryFirstOrDefaultAsync<JournalVideo>(sql, new { ModuleId = moduleId });
        }

        public async Task<List<JournalVideo>> GetByEntryId(Guid entryId)
        {
            var sql = @"SELECT * FROM JournalVideos WHERE JournalEntryId = @EntryId ORDER BY Id DESC";
            var result = await _db.QueryAsync<JournalVideo>(sql, new { EntryId = entryId });
            return result.ToList();
        }

        public async Task<List<JournalVideo>> GetByJournalId(int journalId)
        {
            var sql = @"SELECT * FROM JournalVideos WHERE JournalId = @JournalId ORDER BY Id DESC";
            var result = await _db.QueryAsync<JournalVideo>(sql, new { JournalId = journalId });
            return result.ToList();
        }

        public async Task<int> Add(JournalVideo video)
        {
            var sql = @"
                INSERT INTO JournalVideos (JournalId, JournalEntryId, ModuleId, Filename, OriginalFilename, Duration, Width, Height, Metadata, Title, Description)
                VALUES (@JournalId, @JournalEntryId, @ModuleId, @Filename, @OriginalFilename, @Duration, @Width, @Height, @Metadata, @Title, @Description);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            return await _db.QuerySingleAsync<int>(sql, video);
        }

        public async Task<bool> Update(JournalVideo video)
        {
            var sql = @"
                UPDATE JournalVideos
                SET Filename = @Filename,
                    Duration = @Duration,
                    Width = @Width,
                    Height = @Height,
                    Metadata = @Metadata,
                    Title = @Title,
                    Description = @Description
                WHERE Id = @Id";
            
            var rowsAffected = await _db.ExecuteAsync(sql, video);
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateTitle(int id, string title)
        {
            var sql = @"UPDATE JournalVideos SET Title = @Title WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Title = title });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateDescription(int id, string description)
        {
            var sql = @"UPDATE JournalVideos SET Description = @Description WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Description = description });
            return rowsAffected > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var sql = @"DELETE FROM JournalVideos WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteByModuleId(Guid entryId, string moduleId)
        {
            var sql = @"DELETE FROM JournalVideos WHERE JournalEntryId = @EntryId AND ModuleId = @ModuleId";
            var rowsAffected = await _db.ExecuteAsync(sql, new { EntryId = entryId, ModuleId = moduleId });
            return rowsAffected > 0;
        }
    }
}
