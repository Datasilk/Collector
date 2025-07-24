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
            return _dbConnection.ExecuteScalar<int>(@"EXEC Article_Add 
                @feedId=@feedId,
                @subjects=@subjects,
                @subjectId=@subjectId,
                @score=@score,
                @domain=@domain,
                @url=@url,
                @title=@title,
                @summary=@summary,
                @filesize=@filesize,
                @linkcount=@linkcount,
                @linkwordcount=@linkwordcount,
                @wordcount=@wordcount,
                @sentencecount=@sentencecount,
                @paragraphcount=@paragraphcount,
                @importantcount=@importantcount,
                @yearstart=@yearstart,
                @yearend=@yearend,
                @years=@years,
                @images=@images,
                @datepublished=@datepublished,
                @relavance=@relavance,
                @importance=@importance,
                @fiction=@fiction,
                @analyzed=@analyzed,
                @active=@active ", article);
        }

        public void Clean(int articleId)
        {
            _dbConnection.Execute("EXEC Article_Clean @articleId=@articleId", new { articleId });
        }

        public bool Exists(string url)
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Article_Exists @url=@url", new { url }) > 0;
        }
        public List<ArticleDetails> GetList(int[] subjectId, int feedId = 0, int domainId = 0, int score = 0, string search = "", ArticleIsActive isActive = ArticleIsActive.Both, bool isDeleted = false, int minImages = 0, DateTime? dateStart = null, DateTime? dateEnd = null, ArticleSortBy orderBy = ArticleSortBy.BestScore, int start = 1, int length = 50, bool bugsOnly = false)
        {
            return _dbConnection.Query<ArticleDetails>(@"EXEC Articles_GetList 
                @subjectIds=@subjectIds,
                @feedId=@feedId,
                @domainId=@domainId,
                @score=@score,
                @search=@search,
                @isActive=@isActive,
                @isDeleted=@isDeleted,
                @minImages=@minImages,
                @dateStart=@dateStart,
                @dateEnd=@dateEnd,
                @orderBy=@orderBy,
                @start=@start,
                @length=@length,
                @bugsOnly=@bugsOnly", 
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
                    orderby = (int)orderBy,
                    start,
                    length,
                    bugsOnly 
                }).ToList();
        }

        public int GetCount(int[] subjectId, int feedId = 0, int domainId = 0, int score = 0, string search = "", ArticleIsActive isActive = ArticleIsActive.Both, bool isDeleted = false, int minImages = 0, DateTime? dateStart = null, DateTime? dateEnd = null, bool bugsOnly = false){
            return _dbConnection.ExecuteScalar<int>(@"EXEC Articles_GetCount 
                @subjectIds=@subjectIds,
                @feedId=@feedId,
                @domainId=@domainId,
                @score=@score,
                @search=@search,
                @isActive=@isActive,
                @isDeleted=@isDeleted,
                @minImages=@minImages,
                @dateStart=@dateStart,
                @dateEnd=@dateEnd,
                @bugsOnly=@bugsOnly", 
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
            return _dbConnection.QueryFirstOrDefault<ArticleDetails>("EXEC Article_GetByUrl @url=@url", new { url });
        }

        public ArticleDetails GetById(int articleId){
            return _dbConnection.QueryFirstOrDefault<ArticleDetails>("EXEC Article_GetById @articleId=@articleId", new { articleId });
        }

        public void Remove(int articleId){
            _dbConnection.Execute("EXEC Article_Remove @articleId=@articleId", new { articleId });
        }

        public void Update(ArticleDetails article){
            _dbConnection.Execute(@"EXEC Article_Update 
                @articleId=@articleId, 
                @subjects=@subjects, 
                @subjectId=@subjectId, 
                @score=@score, 
                @domain=@domain, 
                @url=@url, 
                @title=@title, 
                @summary=@summary, 
                @filesize=@filesize, 
                @linkcount=@linkcount, 
                @linkwordcount=@linkwordcount, 
                @wordcount=@wordcount, 
                @sentencecount=@sentencecount, 
                @paragraphcount=@paragraphcount, 
                @importantcount=@importantcount, 
                @yearstart=@yearstart, 
                @yearend=@yearend, 
                @years=@years, 
                @images=@images, 
                @datepublished=@datepublished, 
                @relavance=@relavance, 
                @importance=@importance, 
                @fiction=@fiction, 
                @analyzed=@analyzed", article);
        }

        public void UpdateUrl(int articleId, string url, string domain, int parentId = 0){
            _dbConnection.Execute("EXEC Article_UpdateUrl @articleId=@articleId, @url=@url, @domain=@domain, @parentId=@parentId", new { articleId, url, domain, parentId });
        }

        public void UpdateCache(int articleId, bool cached){
            _dbConnection.Execute("EXEC Article_UpdateCache @articleId=@articleId, @cached=@cached", new { articleId, cached });
        }

        public void Visited(int articleId){
            _dbConnection.Execute("EXEC Article_Visited @articleId=@articleId", new { articleId });
        }

        #region "Dates, sentences, subjects, words, etc"

        public void AddDate(int articleId, DateTime date, bool hasYear, bool hasMonth, bool hasDay)
        {
            _dbConnection.Execute("EXEC ArticleDate_Add @articleId=@articleId, @date=@date, @hasYear=@hasYear, @hasMonth=@hasMonth, @hasDay=@hasDay", new { articleId, date, hasYear, hasMonth, hasDay });
        }

        public void AddSentence(int articleId, int index, string sentence)
        {
            _dbConnection.Execute("EXEC ArticleSentence_Add @articleId=@articleId, @index=@index, @sentence=@sentence", new { articleId, index, sentence });
        }

        public void RemoveSentences(int articleId)
        {
            _dbConnection.Execute("EXEC ArticleSentences_Remove @articleId=@articleId", new { articleId });
        }

        public void AddSubject(int articleId, int subjectId, DateTime? datePublished = null, int score = 0)
        {
            _dbConnection.Execute("EXEC ArticleSubject_Add @articleId=@articleId, @subjectId=@subjectId, @datePublished=@datePublished, @score=@score", new { articleId, subjectId, datePublished, score });
        }

        public void RemoveSubjects(int articleId, int subjectId = 0)
        {
            _dbConnection.Execute("EXEC ArticleSubjects_Remove @articleId=@articleId, @subjectId=@subjectId", new { articleId, subjectId });
        }

        public void AddWord(int articleId, int wordId, int count)
        {
            _dbConnection.Execute("EXEC ArticleWord_Add @articleId=@articleId, @wordId=@wordId, @count=@count", new { articleId, wordId, count });
        }

        public void RemoveWords(int articleId, string word = "")
        {
            _dbConnection.Execute("EXEC ArticleWords_Remove @articleId=@articleId, @word=@word", new { articleId, word });
        }

        #endregion

    
    }
}
