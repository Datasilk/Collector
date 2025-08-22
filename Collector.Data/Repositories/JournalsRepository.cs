using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class JournalsRepository : IJournalsRepository
    {
        readonly IDbConnection _dbConnection;

        public JournalsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(Journal journal)
        {
            return _dbConnection.QuerySingle<int>(@"INSERT INTO [dbo].[Journals] 
                ([AppUserId], [CategoryId], [Title], [ThemeId], [Color]) 
                OUTPUT INSERTED.[Id]
                VALUES (@AppUserId, @CategoryId, @Title, @ThemeId, @Color)", 
                journal);
        }

        public List<Journal> GetAllByUserId(Guid appUserId)
        {
            return _dbConnection.Query<Journal>(@"SELECT * FROM [dbo].[Journals] 
                WHERE [AppUserId] = @appUserId", 
                new { appUserId }).ToList();
        }
        
        public Journal GetById(int journalId)
        {
            return _dbConnection.QuerySingleOrDefault<Journal>(@"SELECT * FROM [dbo].[Journals] 
                WHERE [Id] = @journalId", 
                new { journalId });
        }

        public void Rename(int journalId, string title)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[Journals] 
                SET [Title] = @title 
                WHERE [Id] = @journalId", 
                new { journalId, title });
        }

        public void ChangeColor(int journalId, string color)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[Journals] 
                SET [Color] = @color 
                WHERE [Id] = @journalId", 
                new { journalId, color });
        }

        public void Archive(int journalId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[Journals] 
                SET [Status] = 2 
                WHERE [Id] = @journalId", 
                new { journalId });
        }

        public void Unarchive(int journalId)
        {
            _dbConnection.Execute(@"UPDATE [dbo].[Journals] 
                SET [Status] = 0 
                WHERE [Id] = @journalId", 
                new { journalId });
        }
    }
}
