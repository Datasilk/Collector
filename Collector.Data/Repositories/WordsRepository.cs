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
            _dbConnection.Execute("SELECT public.\"Word_Add\"(@word, @subjectId, @grammartype, @score)", 
                new { word, subjectId, grammartype = (int)grammarType, score });
        }

        public void BulkAdd(string[] words, int subjectId)
        {
            _dbConnection.Execute("SELECT public.\"Words_BulkAdd\"(@words, @subjectId)", 
                new { words = string.Join(",", words), subjectId });
        }

        public List<Word> GetList(string[] words)
        {
            return _dbConnection.Query<Word>("SELECT * FROM public.\"Words_GetList\"(@words)", 
                new { words = string.Join(",", words) }).ToList();
        }
    }
}
