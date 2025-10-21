using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalModulesRepository : IJournalModulesRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalModulesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(JournalModule module)
        {
            return _dbConnection.Execute(@"
                INSERT INTO [dbo].[JournalModules] 
                ([JournalId], [JournalEntryId], [ModuleId], [Sort], [Width], [Height]) 
                VALUES (
                    @JournalId, 
                    @JournalEntryId, 
                    @ModuleId, 
                    CASE WHEN @Sort = 0 
                        THEN COALESCE((SELECT MAX([Sort]) FROM [dbo].[JournalModules] WHERE [JournalId] = @JournalId), 0) + 1 
                        ELSE @Sort 
                    END, 
                    @Width, 
                    @Height
                )", 
                module);
        }

        public List<JournalModule> GetAllByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalModule>(@"SELECT * FROM [dbo].[JournalModules] 
                WHERE [JournalId] = @journalId 
                ORDER BY [Sort]", 
                new { journalId }).ToList();
        }

        public List<JournalModule> GetAllByEntryId(Guid entryId)
        {
            return _dbConnection.Query<JournalModule>(@"SELECT * FROM [dbo].[JournalModules] 
                WHERE [JournalEntryId] = @entryId 
                ORDER BY [Sort]", 
                new { entryId }).ToList();
        }

        public JournalModule GetById(int journalId, Guid entryId, string moduleId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalModule>(@"SELECT * FROM [dbo].[JournalModules] 
                WHERE [JournalId] = @journalId 
                AND [JournalEntryId] = @entryId 
                AND [ModuleId] = @moduleId", 
                new { journalId, entryId, moduleId });
        }

        public void Update(JournalModule module)
        {
            var rowsAffected = _dbConnection.Execute(@"UPDATE [dbo].[JournalModules] 
                SET [Sort] = @Sort, [Width] = @Width, [Height] = @Height 
                WHERE [JournalId] = @JournalId 
                AND [JournalEntryId] = @JournalEntryId 
                AND [ModuleId] = @ModuleId", 
                module);
            
            // If no rows were updated, the module doesn't exist, so add it
            if (rowsAffected == 0)
            {
                Add(module);
            }
        }

        public void Delete(int journalId, Guid entryId, string moduleId)
        {
            _dbConnection.Execute(@"DELETE FROM [dbo].[JournalModules] 
                WHERE [JournalId] = @journalId 
                AND [JournalEntryId] = @entryId 
                AND [ModuleId] = @moduleId", 
                new { journalId, entryId, moduleId });
        }

        public void DeleteAllByEntryId(Guid entryId)
        {
            _dbConnection.Execute(@"DELETE FROM [dbo].[JournalModules] 
                WHERE [JournalEntryId] = @entryId", 
                new { entryId });
        }

        public void ResortModules(int journalId, List<JournalModule> modules)
        {
            // Update sort order for all modules in the journal
            for (int i = 0; i < modules.Count; i++)
            {
                _dbConnection.Execute(@"UPDATE [dbo].[JournalModules] 
                    SET [Sort] = @Sort 
                    WHERE [JournalId] = @JournalId 
                    AND [JournalEntryId] = @JournalEntryId 
                    AND [ModuleId] = @ModuleId", 
                    new { 
                        Sort = i + 1, 
                        JournalId = journalId,
                        JournalEntryId = modules[i].JournalEntryId,
                        ModuleId = modules[i].ModuleId 
                    });
            }
        }
    }
}
