using Collector.API.Models;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ArticlesController : ApiController
    {
        private readonly IArticlesRepository _articlesRepository;

        public ArticlesController(IArticlesRepository articlesRepository)
        {
            _articlesRepository = articlesRepository;
        }

        [HttpPost("get-articles")]
        public IActionResult GetArticles([FromBody] ArticleListRequestModel request)
        {
            try
            {
                var articles = _articlesRepository.GetList(
                    request.SubjectIds,
                    request.FeedId,
                    request.DomainId,
                    request.Score,
                    request.Search,
                    request.IsActive,
                    request.IsDeleted,
                    request.MinImages,
                    request.DateStart,
                    request.DateEnd,
                    request.OrderBy,
                    request.Start,
                    request.Length,
                    request.BugsOnly
                );

                var count = _articlesRepository.GetCount(
                    request.SubjectIds,
                    request.FeedId,
                    request.DomainId,
                    request.Score,
                    request.Search,
                    request.IsActive,
                    request.IsDeleted,
                    request.MinImages,
                    request.DateStart,
                    request.DateEnd,
                    request.BugsOnly
                );

                var response = new ArticleListResponse
                {
                    Articles = articles,
                    TotalCount = count
                };

                return Json(new ApiResponse { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetArticle(int id)
        {
            try
            {
                var article = _articlesRepository.GetById(id);
                if (article == null)
                {
                    return Json(new ApiResponse { success = false, message = "Article not found" });
                }
                return Json(new ApiResponse { success = true, data = article });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("url")]
        public IActionResult GetArticleByUrl([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return Json(new ApiResponse { success = false, message = "URL is required" });
            }

            try
            {
                var article = _articlesRepository.GetByUrl(url);
                if (article == null)
                {
                    return Json(new ApiResponse { success = false, message = "Article not found" });
                }
                return Json(new ApiResponse { success = true, data = article });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("exists")]
        public IActionResult ArticleExists([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return Json(new ApiResponse { success = false, message = "URL is required" });
            }

            try
            {
                bool exists = _articlesRepository.Exists(url);
                return Json(new ApiResponse { success = true, data = exists });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateArticle([FromBody] ArticleCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                var article = new Article
                {
                    feedId = model.FeedId,
                    subjects = model.Subjects,
                    subjectId = model.SubjectId,
                    score = model.Score,
                    domain = model.Domain,
                    url = model.Url,
                    title = model.Title,
                    summary = model.Summary,
                    filesize = model.Filesize,
                    linkcount = model.Linkcount,
                    linkwordcount = model.Linkwordcount,
                    wordcount = model.Wordcount,
                    sentencecount = model.Sentencecount,
                    paragraphcount = model.Paragraphcount,
                    importantcount = model.Importantcount,
                    yearstart = model.Yearstart,
                    yearend = model.Yearend,
                    years = model.Years,
                    images = model.Images,
                    datepublished = model.DatePublished,
                    relavance = model.Relavance,
                    importance = model.Importance,
                    fiction = model.Fiction,
                    analyzed = model.Analyzed,
                    active = model.Active
                };

                var articleId = _articlesRepository.Add(article);
                return Json(new ApiResponse { success = true, data = articleId });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        public IActionResult UpdateArticle([FromBody] ArticleUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                var article = _articlesRepository.GetById(model.ArticleId);
                if (article == null)
                {
                    return Json(new ApiResponse { success = false, message = "Article not found" });
                }

                article.subjects = model.Subjects ?? article.subjects;
                article.subjectId = model.SubjectId ?? article.subjectId;
                article.score = model.Score ?? article.score;
                article.domain = model.Domain ?? article.domain;
                article.url = model.Url ?? article.url;
                article.title = model.Title ?? article.title;
                article.summary = model.Summary ?? article.summary;
                article.filesize = model.Filesize ?? article.filesize;
                article.linkcount = model.Linkcount ?? article.linkcount;
                article.linkwordcount = model.Linkwordcount ?? article.linkwordcount;
                article.wordcount = model.Wordcount ?? article.wordcount;
                article.sentencecount = model.Sentencecount ?? article.sentencecount;
                article.paragraphcount = model.Paragraphcount ?? article.paragraphcount;
                article.importantcount = model.Importantcount ?? article.importantcount;
                article.yearstart = model.Yearstart ?? article.yearstart;
                article.yearend = model.Yearend ?? article.yearend;
                article.years = model.Years ?? article.years;
                article.images = model.Images ?? article.images;
                article.datepublished = model.DatePublished ?? article.datepublished;
                article.relavance = model.Relavance ?? article.relavance;
                article.importance = model.Importance ?? article.importance;
                article.fiction = model.Fiction ?? article.fiction;
                article.analyzed = model.Analyzed ?? article.analyzed;

                _articlesRepository.Update(article);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPut("url")]
        public IActionResult UpdateArticleUrl([FromBody] ArticleUrlUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _articlesRepository.UpdateUrl(model.ArticleId, model.Url, model.Domain, model.ParentId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPut("cache")]
        public IActionResult UpdateArticleCache([FromBody] ArticleCacheUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _articlesRepository.UpdateCache(model.ArticleId, model.Cached);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPut("visited/{id}")]
        public IActionResult MarkArticleVisited(int id)
        {
            try
            {
                _articlesRepository.Visited(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult RemoveArticle(int id)
        {
            try
            {
                _articlesRepository.Remove(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("clean/{id}")]
        public IActionResult CleanArticle(int id)
        {
            try
            {
                _articlesRepository.Clean(id);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #region "Dates, sentences, subjects, words, etc"

        [HttpPost("date")]
        public IActionResult AddArticleDate([FromBody] ArticleDateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _articlesRepository.AddDate(model.ArticleId, model.Date, model.HasYear, model.HasMonth, model.HasDay);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("sentence")]
        public IActionResult AddArticleSentence([FromBody] ArticleSentenceModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _articlesRepository.AddSentence(model.ArticleId, model.Index, model.Sentence);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("sentences/{articleId}")]
        public IActionResult RemoveArticleSentences(int articleId)
        {
            try
            {
                _articlesRepository.RemoveSentences(articleId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("subject")]
        public IActionResult AddArticleSubject([FromBody] ArticleSubjectModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _articlesRepository.AddSubject(model.ArticleId, model.SubjectId, model.DatePublished, model.Score);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("subjects/{articleId}")]
        public IActionResult RemoveArticleSubjects(int articleId, [FromQuery] int subjectId = 0)
        {
            try
            {
                _articlesRepository.RemoveSubjects(articleId, subjectId);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpPost("word")]
        public IActionResult AddArticleWord([FromBody] ArticleWordModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse { success = false, message = "Invalid model state" });
            }

            try
            {
                _articlesRepository.AddWord(model.ArticleId, model.WordId, model.Count);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpDelete("words/{articleId}")]
        public IActionResult RemoveArticleWords(int articleId, [FromQuery] string word = "")
        {
            try
            {
                _articlesRepository.RemoveWords(articleId, word);
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
