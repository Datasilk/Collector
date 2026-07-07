using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Data.Enums;

namespace Collector.Data.Repositories
{
    public class ArticlesRepository : IArticlesRepository
    {
        readonly IDbConnection _dbConnection;

        public ArticlesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(Article article)
        {
            return _dbConnection.ExecuteScalar<int>(@"SELECT public.""Article_Add""(
                @feedId,
                @subjects,
                @subjectId,
                @score,
                @domain,
                @url,
                @title,
                @summary,
                @filesize,
                @linkcount,
                @linkwordcount,
                @wordcount,
                @sentencecount,
                @paragraphcount,
                @importantcount,
                @yearstart,
                @yearend,
                @years,
                @images,
                @datepublished,
                @relavance,
                @importance,
                @fiction,
                @analyzed,
                @active)", article);
        }

        public void Clean(int articleId)
        {
            _dbConnection.Execute("SELECT public.\"Article_Clean\"(@articleId)", new { articleId });
        }

        public bool Exists(string url)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Article_Exists\"(@url)", new { url }) > 0;
        }
        public List<ArticleDetails> GetList(int[] subjectId, int feedId = 0, int domainId = 0, int score = 0, string search = "", ArticleIsActive isActive = ArticleIsActive.Both, bool isDeleted = false, int minImages = 0, DateTime? dateStart = null, DateTime? dateEnd = null, ArticleSortBy orderBy = ArticleSortBy.BestScore, int start = 1, int length = 50, bool bugsOnly = false)
        {
            return _dbConnection.Query<ArticleDetails>(@"SELECT * FROM public.""Articles_GetList""(
                @subjectIds,
                @search,
                @feedId,
                @domainId,
                @score,
                @isActive,
                @isDeleted,
                @minImages,
                @dateStart,
                @dateEnd,
                @orderBy,
                @start,
                @length,
                @bugsOnly)",
                new {
                    subjectIds = subjectId != null && subjectId.Length == 0 ? "" : string.Join(",", subjectId),
                    feedId,
                    domainId,
                    score,
                    search,
                    isActive = (int)isActive,
                    isDeleted,
                    minImages,
                    dateStart = dateStart,
                    dateEnd = dateEnd,
                    orderBy = (int)orderBy,
                    start,
                    length,
                    bugsOnly
                }).ToList();
        }

        public int GetCount(int[] subjectId, int feedId = 0, int domainId = 0, int score = 0, string search = "", ArticleIsActive isActive = ArticleIsActive.Both, bool isDeleted = false, int minImages = 0, DateTime? dateStart = null, DateTime? dateEnd = null, bool bugsOnly = false){
            return _dbConnection.ExecuteScalar<int>(@"SELECT public.""Articles_GetCount""(
                @subjectIds,
                @search,
                @feedId,
                @domainId,
                @score,
                @isActive,
                @isDeleted,
                @minImages,
                @dateStart,
                @dateEnd,
                @bugsOnly)",
                new {
                    subjectIds = subjectId.Length == 0 ? "" : string.Join(",", subjectId),
                    feedId,
                    domainId,
                    score,
                    search,
                    isActive = (int)isActive,
                    isDeleted,
                    minImages,
                    dateStart = dateStart,
                    dateEnd = dateEnd,
                    bugsOnly
                });
        }

        public ArticleDetails GetByUrl(string url){
            return _dbConnection.QueryFirstOrDefault<ArticleDetails>("SELECT * FROM public.\"Article_GetByUrl\"(@url)", new { url });
        }

        public ArticleDetails GetById(int articleId){
            return _dbConnection.QueryFirstOrDefault<ArticleDetails>("SELECT * FROM public.\"Article_GetById\"(@articleId)", new { articleId });
        }

        public void Remove(int articleId){
            _dbConnection.Execute("SELECT public.\"Article_Remove\"(@articleId)", new { articleId });
        }

        public void Update(ArticleDetails article){
            _dbConnection.Execute(@"SELECT public.""Article_Update""(
                @articleId,
                @subjects,
                @subjectId,
                @score,
                @title,
                @summary,
                @filesize,
                @wordcount,
                @sentencecount,
                @paragraphcount,
                @importantcount,
                @yearstart,
                @yearend,
                @years,
                @images,
                @datepublished,
                @relavance,
                @importance,
                @fiction,
                @analyzed)", article);
        }

        public void UpdateUrl(int articleId, string url, string domain, int parentId = 0){
            _dbConnection.Execute("SELECT public.\"Article_UpdateUrl\"(@articleId, @url, @domain, @parentId)", new { articleId, url, domain, parentId });
        }

        public void UpdateCache(int articleId, bool cached){
            _dbConnection.Execute("SELECT public.\"Article_UpdateCache\"(@articleId, @cached)", new { articleId, cached });
        }

        public void Visited(int articleId){
            _dbConnection.Execute("SELECT public.\"Article_Visited\"(@articleId)", new { articleId });
        }

        #region "Dates, sentences, subjects, words, etc"

        public void AddDate(int articleId, DateTime date, bool hasYear, bool hasMonth, bool hasDay)
        {
            _dbConnection.Execute("SELECT public.\"ArticleDate_Add\"(@articleId, @date, @hasYear, @hasMonth, @hasDay)", new { articleId, date, hasYear, hasMonth, hasDay });
        }

        public void AddSentence(int articleId, int index, string sentence)
        {
            _dbConnection.Execute("SELECT public.\"ArticleSentence_Add\"(@articleId, @index, @sentence)", new { articleId, index, sentence });
        }

        public void RemoveSentences(int articleId)
        {
            _dbConnection.Execute("SELECT public.\"ArticleSentences_Remove\"(@articleId)", new { articleId });
        }

        public void AddSubject(int articleId, int subjectId, DateTime? datePublished = null, int score = 0)
        {
            _dbConnection.Execute("SELECT public.\"ArticleSubject_Add\"(@articleId, @subjectId, @datePublished, @score)", new { articleId, subjectId, datePublished, score });
        }

        public void RemoveSubjects(int articleId, int subjectId = 0)
        {
            _dbConnection.Execute("SELECT public.\"ArticleSubjects_Remove\"(@articleId, @subjectId)", new { articleId, subjectId });
        }

        public void AddWord(int articleId, int wordId, int count)
        {
            _dbConnection.Execute("SELECT public.\"ArticleWord_Add\"(@articleId, @wordId, @count)", new { articleId, wordId, count });
        }

        public void RemoveWords(int articleId, string word = "")
        {
            _dbConnection.Execute("SELECT public.\"ArticleWords_Remove\"(@articleId, @word)", new { articleId, word });
        }

        #endregion

    
    }
}
