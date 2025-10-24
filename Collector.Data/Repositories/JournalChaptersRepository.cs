using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalChaptersRepository : IJournalChaptersRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalChaptersRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void Add(JournalChapter chapter)
        {
            _dbConnection.Execute(@"
                INSERT INTO [dbo].[JournalChapters] 
                ([ChapterId], [JournalId], [Title], [Sort], [Icon], [Color], [Description]) 
                VALUES (
                    ISNULL((SELECT MAX([ChapterId]) FROM [dbo].[JournalChapters] WHERE [JournalId] = @journalId), 0) + 1,
                    @journalId,
                    @title,
                    ISNULL((SELECT MAX([Sort]) FROM [dbo].[JournalChapters] WHERE [JournalId] = @journalId), 0) + 1,
                    @icon,
                    @color,
                    @description
                )", 
                new { 
                    journalId = chapter.JournalId,
                    title = chapter.Title,
                    icon = chapter.Icon,
                    color = chapter.Color,
                    description = chapter.Description
                });
        }

        public JournalChapter GetById(int journalId, int chapterId)
        {
            return _dbConnection.QuerySingleOrDefault<JournalChapter>(@"
                SELECT * FROM [dbo].[JournalChapters] 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId });
        }

        public List<JournalChapter> GetAllByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalChapter>(@"
                SELECT * FROM [dbo].[JournalChapters] 
                WHERE [JournalId] = @journalId 
                ORDER BY [Sort] ASC", 
                new { journalId }).ToList();
        }

        public void Rename(int journalId, int chapterId, string title)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalChapters] 
                SET [Title] = @title 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId, title });
        }

        public void UpdateDescription(int journalId, int chapterId, string description)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalChapters] 
                SET [Description] = @description 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId, description });
        }

        public void ChangeColor(int journalId, int chapterId, int color)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalChapters] 
                SET [Color] = @color 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId, color });
        }

        public void ChangeIcon(int journalId, int chapterId, int icon)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalChapters] 
                SET [Icon] = @icon 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId, icon });
        }

        public void UpdateSort(int journalId, int chapterId, int sort)
        {
            _dbConnection.Execute(@"
                UPDATE [dbo].[JournalChapters] 
                SET [Sort] = @sort 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId, sort });
        }

        public void Delete(int journalId, int chapterId)
        {
            _dbConnection.Execute(@"
                DELETE FROM [dbo].[JournalChapters] 
                WHERE [JournalId] = @journalId AND [ChapterId] = @chapterId", 
                new { journalId, chapterId });
        }
    }
}
