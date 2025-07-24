using System.Collections.Generic;
using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.Data.Interfaces
{
    public interface IWordsRepository
    {
        void Add(string word, int subjectId, GrammarType grammarType = GrammarType.Noun, int score = 1);
        void BulkAdd(string[] words, int subjectId);
        List<Word> GetList(string[] words);
    }
}
