using System.Text;
using Collector.Common.Models.Articles;
using Collector.Common.Extensions.Strings;
using Collector.Common.DOM;
using Collector.Data.Entities;

namespace Collector.Common
{
    public static class Articles
    {
        public static double Version { get; set; } = 0.1;
        public static string BrowserEndpoint { get; set; } = "";

        /// <summary>
        /// Merges data from an AnalyzedArticle into an Article entity
        /// </summary>
        /// <param name="article">The article entity to update</param>
        /// <param name="analyzed">The analyzed article data</param>
        /// <returns>The updated article entity</returns>
        public static Data.Entities.Article Merge(Data.Entities.Article articleInfo, AnalyzedArticle article)
        {
            if (articleInfo == null || article == null) return articleInfo;

            try
            {
                //get filesize of article
                FileSize(article);
                article.url = articleInfo.url;
                article.id = articleInfo.articleId;
                article.domain = articleInfo.url.GetDomainName();

                //get article info
                Html.GetArticleInfoFromDOM(article);

                //get article contents
                var elements = new List<AnalyzedElement>();
                Html.GetBestElementIndexes(article, elements);
                Html.GetArticleElements(article, elements);

                //get total words, sentences, and important words
                var text = Html.GetArticleText(article);
                var words = Html.CleanWords(Html.SeparateWordsFromText(text));
                article.totalWords = words.Length;
                article.totalSentences = Html.GetSentences(text).Count;
                //var important = Query.Words.GetList(words.Distinct().ToArray());
                //article.totalImportantWords = important.Count;

                //copy info from Analyzed Article into Query Article
                articleInfo.title = article.title != null ? article.title : "";
                articleInfo.analyzecount++;
                articleInfo.analyzed = Version;
                articleInfo.cached = true;
                articleInfo.domain = article.domain;
                articleInfo.feedId = article.feedId;
                articleInfo.fiction = (short)(article.fiction == true ? 1 : 0);
                articleInfo.filesize = article.fileSize;
                articleInfo.importance = (short)article.importance;
                articleInfo.importantcount = (short)article.totalImportantWords;
                articleInfo.paragraphcount = (short)article.totalParagraphs;
                articleInfo.relavance = (short)article.relevance;
                try
                {
                    var subj = article.subjects.OrderBy(a => a.score * -1).First();
                    if (subj != null)
                    {
                        articleInfo.score = (short)subj.score;
                        articleInfo.subjectId = subj.id;
                        articleInfo.subjects = Convert.ToByte(article.subjects.Count);
                    }
                }
                catch (Exception) { }
                articleInfo.sentencecount = (short)article.totalSentences;
                articleInfo.summary = article.summary;
                articleInfo.wordcount = article.totalWords;
                articleInfo.yearstart = (short)article.yearStart;
                articleInfo.yearend = (short)article.yearEnd;
                try
                {
                    articleInfo.years = string.Join(",", article.years.ToArray());
                }
                catch (Exception) { }
            }
            catch (Exception ex)
            {
                // Log exception or handle error
                Console.WriteLine($"Error merging article data: {ex.Message}");
            }

            return articleInfo;
        }

        public static void FileSize(AnalyzedArticle article)
        {
            article.fileSize = int.Parse((Encoding.Unicode.GetByteCount(article.rawHtml) / 1024).ToString("c").Replace("$", "").Replace(",", "").Replace(".00", ""));
        }

        public static IEnumerable<DomElement> GetLinks(AnalyzedArticle article)
        {
            var links = article.bodyElements.Where(a => a.HasTagInHierarchy("a"))
                .Select(a => article.elements[a.hierarchyIndexes[a.HierarchyTagIndex("a")]]).ToList();
            return links;
        }

        public static int GetLinkWords(IEnumerable<DomElement> links)
        {
            var total = 0;
            foreach (var link in links)
            {

                total += Html.CleanWords(Html.SeparateWordsFromText(string.Join(" ", Html.GetTextFromElement(link)))).Length;
            }
            return total;
        }
    }
}
