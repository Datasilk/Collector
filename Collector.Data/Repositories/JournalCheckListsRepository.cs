using Collector.Common.Entities;
using Collector.Data.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Collector.Data.Repositories
{
    public class JournalCheckListsRepository : IJournalCheckListsRepository
    {
        private readonly IDbConnection _db;

        public JournalCheckListsRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<JournalCheckList> GetById(int id)
        {
            var sql = @"
                SELECT * FROM public.""JournalCheckLists"" WHERE ""Id"" = @Id;
                SELECT * FROM public.""JournalCheckListItems"" WHERE ""CheckListId"" = @Id ORDER BY ""Sort"" ASC;
            ";
            
            using (var multi = await _db.QueryMultipleAsync(sql, new { Id = id }))
            {
                var checkList = await multi.ReadFirstOrDefaultAsync<JournalCheckList>();
                if (checkList != null)
                {
                    // Store items in the Items property for the controller to access
                    checkList.Items = (await multi.ReadAsync<JournalCheckListItem>()).ToList();
                }
                return checkList;
            }
        }

        public async Task<List<JournalCheckList>> GetByEntryId(Guid entryId)
        {
            var sql = @"
                SELECT * FROM public.""JournalCheckLists"" WHERE ""EntryId"" = @EntryId ORDER BY ""Created"" DESC;
                SELECT i.* FROM public.""JournalCheckListItems"" i
                INNER JOIN public.""JournalCheckLists"" c ON i.""CheckListId"" = c.""Id""
                WHERE c.""EntryId"" = @EntryId
                ORDER BY i.""CheckListId"", i.""Sort"" ASC;
            ";
            
            using (var multi = await _db.QueryMultipleAsync(sql, new { EntryId = entryId }))
            {
                var checklists = (await multi.ReadAsync<JournalCheckList>()).AsList();
                var items = (await multi.ReadAsync<JournalCheckListItem>()).AsList();
                
                if (checklists.Any() && items.Any())
                {
                    // Group items by checklist ID and assign to appropriate checklists
                    var itemsByChecklistId = items.GroupBy(i => i.CheckListId).ToDictionary(g => g.Key, g => g.ToList());
                    
                    foreach (var checklist in checklists)
                    {
                        if (itemsByChecklistId.TryGetValue(checklist.Id, out var checklistItems))
                        {
                            checklist.Items = checklistItems;
                        }
                        else
                        {
                            checklist.Items = new List<JournalCheckListItem>();
                        }
                    }
                }
                else
                {
                    // Initialize empty items list for each checklist
                    foreach (var checklist in checklists)
                    {
                        checklist.Items = new List<JournalCheckListItem>();
                    }
                }
                
                return checklists;
            }
        }

        public async Task<List<JournalCheckList>> FilterByUser(Guid userId, string search, int limit)
        {
            var sql = @"
                SELECT c.*, e.""Title"" as EntryTitle 
                FROM public.""JournalCheckLists"" c
                LEFT JOIN public.""JournalEntries"" e ON c.""EntryId"" = e.""Id""
                WHERE c.""AppUserId"" = @UserId
                AND (@Search IS NULL OR @Search = '' OR c.""Title"" ILIKE @SearchPattern OR c.""Description"" ILIKE @SearchPattern OR e.""Title"" ILIKE @SearchPattern)
                ORDER BY c.""Created"" DESC
                LIMIT @Limit;
            ";
            
            var searchPattern = string.IsNullOrEmpty(search) ? null : $"%{search}%";
            var checklists = await _db.QueryAsync<JournalCheckList>(sql, new { UserId = userId, Search = search, SearchPattern = searchPattern, Limit = limit });
            
            return checklists.AsList();
        }

        public async Task<int> Add(JournalCheckList checkList)
        {
            var sql = @"
                INSERT INTO public.""JournalCheckLists"" (""AppUserId"", ""EntryId"", ""ThemeId"", ""Title"", ""Description"", ""Status"")
                VALUES (@AppUserId, @EntryId, @ThemeId, @Title, @Description, @Status)
                RETURNING ""Id""";
            
            return await _db.QuerySingleAsync<int>(sql, checkList);
        }

        public async Task<bool> Update(JournalCheckList checkList)
        {
            var sql = @"
                UPDATE public.""JournalCheckLists""
                SET ""ThemeId"" = @ThemeId,
                    ""Title"" = @Title,
                    ""Description"" = @Description,
                    ""Status"" = @Status
                WHERE ""Id"" = @Id";
            
            var rowsAffected = await _db.ExecuteAsync(sql, checkList);
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateTitle(int id, string title)
        {
            var sql = @"UPDATE public.""JournalCheckLists"" SET ""Title"" = @Title WHERE ""Id"" = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Title = title });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateDescription(int id, string description)
        {
            var sql = @"UPDATE public.""JournalCheckLists"" SET ""Description"" = @Description WHERE ""Id"" = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Description = description });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatus(int id, int status)
        {
            var sql = @"UPDATE public.""JournalCheckLists"" SET ""Status"" = @Status WHERE ""Id"" = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateEntryId(int id, Guid entryId)
        {
            var sql = @"UPDATE public.""JournalCheckLists"" SET ""EntryId"" = @EntryId WHERE ""Id"" = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id, EntryId = entryId });
            return rowsAffected > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var sql = @"DELETE FROM public.""JournalCheckLists"" WHERE ""Id"" = @Id";
            var rowsAffected = await _db.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
    }
}
