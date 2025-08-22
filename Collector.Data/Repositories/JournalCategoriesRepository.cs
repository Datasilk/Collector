using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalCategoriesRepository : IJournalCategoriesRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalCategoriesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(JournalCategory journalCategory)
        {
            return _dbConnection.QuerySingle<int>(@"INSERT INTO [dbo].[JournalCategories] 
                ([AppUserId], [Title], [Color]) 
                OUTPUT INSERTED.[Id]
                VALUES (@appUserId, @title, @color)", 
                new { 
                    appUserId = journalCategory.AppUserId, 
                    title = journalCategory.Title, 
                    color = journalCategory.Color 
                });
        }

        public JournalCategory GetById(int journalCategoryId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalCategory>(@"SELECT * FROM [dbo].[JournalCategories] 
                WHERE [Id] = @journalCategoryId", 
                new { journalCategoryId });
        }

        public List<JournalCategory> GetAllByUserId(Guid appUserId)
        {
            return _dbConnection.Query<JournalCategory>(@"SELECT * FROM [dbo].[JournalCategories] 
                WHERE [AppUserId] = @appUserId", 
                new { appUserId }).ToList();
        }
        
        public List<JournalCategory> GetAllWithJournalsByUserId(Guid appUserId, int? sort = null, string search = null)
        {
            var categoryDict = new Dictionary<int, JournalCategory>();
            var searchParam = !string.IsNullOrEmpty(search) ? $"%{search}%" : null;
            
            // Build the journal query with optional search
            string journalQuery = @"SELECT * FROM [dbo].[Journals] WHERE [CategoryId] IN 
                  (SELECT [Id] FROM [dbo].[JournalCategories] WHERE [AppUserId] = @appUserId)";
            
            if (!string.IsNullOrEmpty(search))
            {
                journalQuery += " AND [Title] LIKE @searchParam";
            }
            
            // Add sorting
            journalQuery += GetSortClause(sort);
            
            // Build the category query
            string categoryQuery = @"SELECT * FROM [dbo].[JournalCategories] WHERE [AppUserId] = @appUserId";
            
            // Execute both queries
            using (var multi = _dbConnection.QueryMultiple(
                $"{categoryQuery}; {journalQuery};",
                new { appUserId, searchParam }))
            {
                // First, get all categories
                var categories = multi.Read<JournalCategory>().ToList();
                foreach (var category in categories)
                {
                    category.Journals = new List<Journal>();
                    categoryDict.Add(category.Id, category);
                }
                
                // Then, get all journals and add them to their respective categories
                var journals = multi.Read<Journal>().ToList();
                foreach (var journal in journals)
                {
                    if (journal.CategoryId > 0 && categoryDict.TryGetValue(journal.CategoryId, out var category))
                    {
                        category.Journals.Add(journal);
                    }
                }
                
                // If search is used, remove empty categories
                if (!string.IsNullOrEmpty(search))
                {
                    return categoryDict.Values.Where(c => c.Journals.Count > 0).ToList();
                }
            }
            
            return categoryDict.Values.ToList();
        }
        
        private string GetSortClause(int? sort)
        {
            switch (sort)
            {
                case 0:
                    return " ORDER BY [Title] ASC";
                case 1:
                    return " ORDER BY [Title] DESC";
                case 2:
                    return " ORDER BY [Created] DESC";
                case 3:
                    return " ORDER BY [Created] ASC";
                default:
                    return " ORDER BY [Title] ASC"; // Default sorting
            }
        }

        public void Rename(int journalCategoryId, string title)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalCategories] 
                SET [Title] = @title 
                WHERE [Id] = @journalCategoryId", 
                new { journalCategoryId, title });
        }

        public void ChangeColor(int journalCategoryId, string color)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalCategories] 
                SET [Color] = @color 
                WHERE [Id] = @journalCategoryId", 
                new { journalCategoryId, color });
        }

        public void Archive(int journalCategoryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalCategories] 
                SET [Status] = 2 
                WHERE [Id] = @journalCategoryId", 
                new { journalCategoryId });
        }

        public void Unarchive(int journalCategoryId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[JournalCategories] 
                SET [Status] = 0 
                WHERE [Id] = @journalCategoryId", 
                new { journalCategoryId });
        }
    }
}
