using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class CommonWordsRepository : ICommonWordsRepository
    {
        readonly IDbConnection _dbConnection;

        public CommonWordsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void Add(string[] words)
        {
            _dbConnection.Execute("EXEC CommonWords_Add @words=@words", new { words = string.Join(",", words) });
        }

        public List<string> GetList()
        {
            return _dbConnection.Query<CommonWord>("EXEC CommonWords_GetList").Select(a => a.word).ToList();
        }
    }
}
