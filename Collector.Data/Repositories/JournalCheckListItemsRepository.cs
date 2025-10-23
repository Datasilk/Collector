using Collector.Common.Entities;
using Collector.Data.Interfaces;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Collector.Data.Repositories
{
    public class JournalCheckListItemsRepository : IJournalCheckListItemsRepository
    {
        private readonly IDbConnection _db;

        public JournalCheckListItemsRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<JournalCheckListItem> GetById(int id)
        {
            var sql = @"SELECT * FROM JournalCheckListItems WHERE Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<JournalCheckListItem>(sql, new { Id = id });
        }

        public async Task<List<JournalCheckListItem>> GetByCheckListId(int checkListId)
        {
            var sql = @"SELECT * FROM JournalCheckListItems WHERE CheckListId = @CheckListId ORDER BY Sort ASC";
            var result = await _db.QueryAsync<JournalCheckListItem>(sql, new { CheckListId = checkListId });
            return result.AsList();
        }

        public async Task<int> Add(JournalCheckListItem item)
        {
            var sql = @"
                INSERT INTO JournalCheckListItems (CheckListId, Title, Icon, Status)
                VALUES (@CheckListId, @Title, @Icon, @Status);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            return await _db.QuerySingleAsync<int>(sql, item);
        }

        public async Task<bool> Update(JournalCheckListItem item)
        {
            var sql = @"
                UPDATE JournalCheckListItems
                SET Title = @Title,
                    Icon = @Icon,
                    Status = @Status
                WHERE Id = @Id";
            
            var rowsAffected = await _db.ExecuteAsync(sql, item);
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateTitle(int id, string title)
        {
            var sql = @"UPDATE JournalCheckListItems SET Title = @Title WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Title = title });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateIcon(int id, int icon)
        {
            var sql = @"UPDATE JournalCheckListItems SET Icon = @Icon WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Icon = icon });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatus(int id, int status)
        {
            var sql = @"UPDATE JournalCheckListItems SET Status = @Status WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var sql = @"DELETE FROM JournalCheckListItems WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> ResortItems(List<JournalCheckListItem> items)
        {
            if (items == null || !items.Any()) return false;
            var sql = @"UPDATE JournalCheckListItems SET Sort = @Sort WHERE Id = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, items);
            return rowsAffected == items.Count;
        }
    }
}
