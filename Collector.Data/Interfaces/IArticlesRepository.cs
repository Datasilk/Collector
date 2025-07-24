using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.Data.Interfaces
{
    public interface IArticlesRepository
    {
        int Add(Article article);
        void Clean(int articleId);
        bool Exists(string url);
        List<ArticleDetails> GetList(int[] subjectId, int feedId = 0, int domainId = 0, int score = 0, string search = "", ArticleIsActive isActive = ArticleIsActive.Both, bool isDeleted = false, int minImages = 0, DateTime? dateStart = null, DateTime? dateEnd = null, ArticleSortBy orderBy = ArticleSortBy.BestScore, int start = 1, int length = 50, bool bugsOnly = false);
        int GetCount(int[] subjectId, int feedId = 0, int domainId = 0, int score = 0, string search = "", ArticleIsActive isActive = ArticleIsActive.Both, bool isDeleted = false, int minImages = 0, DateTime? dateStart = null, DateTime? dateEnd = null, bool bugsOnly = false);
        ArticleDetails GetByUrl(string url);
        ArticleDetails GetById(int articleId);
        void Remove(int articleId);
        void Update(ArticleDetails article);
        void UpdateUrl(int articleId, string url, string domain, int parentId = 0);
        void UpdateCache(int articleId, bool cached);
        void Visited(int articleId);
        
        // Dates, sentences, subjects, words, etc
        void AddDate(int articleId, DateTime date, bool hasYear, bool hasMonth, bool hasDay);
        void AddSentence(int articleId, int index, string sentence);
        void RemoveSentences(int articleId);
        void AddSubject(int articleId, int subjectId, DateTime? datePublished = null, int score = 0);
        void RemoveSubjects(int articleId, int subjectId = 0);
        void AddWord(int articleId, int wordId, int count);
        void RemoveWords(int articleId, string word = "");
    }
}
