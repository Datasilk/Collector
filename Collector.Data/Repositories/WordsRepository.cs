using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class WordsRepository : IWordsRepository
    {
        private readonly IDbConnection _dbConnection;

        public WordsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void Add(string word, int subjectId, GrammarType grammarType = GrammarType.Noun, int score = 1)
        {
            _dbConnection.Execute("EXEC Word_Add @word=@word, @subjectId=@subjectId, @grammartype=@grammartype, @score=@score", 
                new { word, subjectId, grammartype = (int)grammarType, score });
        }

        public void BulkAdd(string[] words, int subjectId)
        {
            _dbConnection.Execute("EXEC Words_BulkAdd @words=@words, @subjectId=@subjectId", 
                new { words = string.Join(",", words), subjectId });
        }

        public List<Word> GetList(string[] words)
        {
            return _dbConnection.Query<Word>("EXEC Words_GetList @words=@words", 
                new { words = string.Join(",", words) }).ToList();
        }
    }
}
