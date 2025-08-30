using Microsoft.Extensions.Logging;
using Collector.Common;
using Collector.Common.Extensions.Strings;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Common.Models.Articles;

namespace Collector.CyberScout
{
    public class Scout
    {
        public static async Task CheckQueue(string id, int feedId, string domainName, int sort)
        {
            try
            {
                // Check if the operation was canceled before starting
                if (IsCanceledByUser()) return;

                // Get the next download from the queue
                var queue = App.DownloadsRepository.CheckQueue(feedId, domainName, 60, (QueueSort)sort);

                if (queue != null)
                {
                    if (IsCanceledByUser()) return;

                    // Validate domain
                    if (!App.ValidateDomain(queue.domain))
                    {
                        // Invalid domain, mark as deleted
                        DeleteAllArticles(queue.domainId);
                        App.DomainsRepository.IsDeleted(queue.domainId, true);
                        App.Logger.LogError("Invalid domain: {domain}", queue.domain);
                        return;
                    }

                    // Validate URL
                    if (!App.ValidateURL(queue.url))
                    {
                        // Delete download from database
                        App.DownloadsRepository.Delete(queue.qid);
                        App.Logger.LogError("Invalid URL: {url}", queue.url);
                        return;
                    }

                    // Process download rules
                    bool downloadOnly = ProcessDownloadRules(queue, out bool shouldSkip);
                    if (shouldSkip)
                    {
                        App.Logger.LogInformation("URL matches download rule and will be skipped ({Url})", queue.url);
                        return;
                    }

                    // Download and process the article
                    ProcessArticleDownload(id, queue, downloadOnly, sort);
                }
                else
                {
                    App.Logger.LogInformation("No downloads queued at the moment...");
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Error checking download queue");

                if (ex.Message.Contains("No connection could be made because the target machine actively refused it"))
                {
                    App.Logger.LogError("Connection error: {Message}", ex.Message);
                }
                else
                {
                    App.Logger.LogError("An error occurred while checking the download queue");
                }
            }
        }

        private static void ProcessArticleDownload(string id, DownloadQueue queue, bool downloadOnly, int sort)
        {
            try
            {
                if (IsCanceledByUser()) return;
                App.Logger.LogInformation("Downloading {Url}...", queue.url);

                // Download content
                AnalyzedArticle article = new AnalyzedArticle();
                string result = "";
                string newurl = "";
                bool isEmpty = false;
                bool archive = true;

                try
                {
                    result = Common.Article.Download(queue.url, out newurl);
                    if (IsCanceledByUser()) return;
                }
                catch (Exception ex)
                {
                    App.Logger.LogError(ex, "Error downloading article from {Url}", queue.url);
                    downloadOnly = true;
                    if (sort == 2) { isEmpty = true; }
                }

                // Handle URL redirection
                if (newurl != queue.url && !string.IsNullOrEmpty(newurl))
                {
                    var qid = queue.qid;
                    HandleRedirectedUrl(queue, newurl);
                    if (qid == queue.qid) return;
                    ProcessDownloadRules(queue, out bool shouldSkip);
                    if (shouldSkip) return;
                }

                // Validate download results
                if (sort == 2)
                {
                    // Don't create articles for homepages
                    downloadOnly = true;
                }

                if (IsCanceledByUser()) return;

                // Process the downloaded content
                if (string.IsNullOrEmpty(result))
                {
                    App.Logger.LogInformation("Downloaded content is missing for URL: {Url}", queue.url);

                    App.DownloadsRepository.UpdateQueueType(queue.qid, Common.Enums.QueueFileType.Timeout);
                    if (sort == 2)
                    {
                        archive = false; //don't archive the domain if it timed out
                        isEmpty = true;
                    }
                }
                else if (result.StartsWith("file:"))
                {
                    App.Logger.LogInformation("URL points to a file of type \"{FileType}\"", result.Substring(5));
                    //update file type for download record
                    App.DownloadsRepository.UpdateQueueType(queue.qid, Downloads.GetFileType(result.Substring(5)));
                }
                else if (result.StartsWith("\"Uncaught TypeError") || result.StartsWith("Object reference not set to an instance of an object"))
                {
                    App.Logger.LogInformation("Error parsing DOM! {msg}", result);
                    archive = false;
                }
                else if (result.StartsWith("log: "))
                {
                    App.Logger.LogInformation("Request timeout!");
                    if (sort == 2) { isEmpty = true; }
                }
                else if (!isEmpty)
                {
                    try
                    {
                        article = Html.DeserializeArticle(result);
                        article.feedId = queue.feedId;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.LogError(ex, "Error parsing DOM for {Url}", queue.url);
                        archive = false;
                        App.DownloadsRepository.UpdateQueueType(queue.qid, Common.Enums.QueueFileType.Error);
                        return;
                    }
                }

                if (article.url.StartsWith("chrome-error") || article.elements.Count < 20)
                {
                    isEmpty = true;
                }

                Data.Entities.Article existingArticle = new Data.Entities.Article();
                Data.Entities.Article articleInfo = new Data.Entities.Article();
                var isHomePage = false;

                if (isEmpty == false)
                {
                    //merge analyzed article into Article entity
                    existingArticle = App.ArticlesRepository.GetByUrl(queue.url) ?? new Data.Entities.Article { url = queue.url };
                    articleInfo = Articles.Merge(existingArticle, article);

                    //check if URL is homepage
                    isHomePage = articleInfo.url.Length >= queue.domain.Length + 7 &&
                            articleInfo.url.Substring(articleInfo.url.IndexOf(queue.domain) + queue.domain.Length).Length <= 2;

                    //validate title & summary
                    var checkTitleSummary = (articleInfo.title.ToLower() + " " + articleInfo.summary.ToLower()).Trim();
                    var checkTitle = articleInfo.title.ToLower();
                    if (isHomePage && checkTitleSummary != "" &&
                        (Rules.badHomePageTitles.Any(a => checkTitleSummary.IndexOf(a) >= 0) ||
                         Rules.badHomePageTitlesStartWith.Any(a => checkTitle.StartsWith(a))
                        ))
                    {
                        isEmpty = true;
                    }
                }

                if (isEmpty)
                {
                    //domain doesn't contain any content //////////////////////////////
                    if (queue.articles == 0) { App.DomainsRepository.IsEmpty(queue.domainId, true); }
                    App.Logger.LogInformation("Domain is empty");
                    return;
                }
                else if (sort == 2)
                {
                    App.DomainsRepository.UpdateHttpsWww(queue.domainId, queue.url.Contains("https://"), queue.url.Contains("www."));
                }

                //process article /////////////////////////////////////////////////////
                if (IsCanceledByUser()) { return; }

                //check all download rules against article info
                if (downloadOnly == false)
                {
                    //check page title for phrases that will flag the url as empty
                    if ((articleInfo.title != "" && Rules.badPageTitles.Any(a => articleInfo.title.IndexOf(a) >= 0)) ||
                        (articleInfo.summary != "" && Rules.badPageTitles.Any(a => articleInfo.summary.IndexOf(a) >= 0)))
                    {
                        downloadOnly = true;
                    }

                    //check default download rules
                    if (Domains.CheckDefaultDownloadLinksOnlyRules(queue.url, articleInfo.title, articleInfo.summary))
                    {
                        downloadOnly = true;
                    }
                    else
                    {
                        //check domain-specific download rules
                        foreach (var rule in queue.downloadRules)
                        {
                            if (rule.rule == true && Domains.CheckDownloadRule(rule.url, rule.title, rule.summary, queue.url, articleInfo.title, articleInfo.summary) == true)
                            {
                                downloadOnly = true;
                                break;
                            }
                        }
                    }
                }

                //get article score
                //Article.GetScore(article, articleInfo);

                //get page score
                if (downloadOnly == false)
                {
                    // Calculate page score based on content quality
                    var pageScore = 0;
                    pageScore += !string.IsNullOrEmpty(article.title) ? 20 : 0;
                    pageScore += !string.IsNullOrEmpty(article.summary) ? 20 : 0;
                    pageScore += article.elements != null && article.elements.Count > 20 ? 20 : 0;
                    pageScore += article.totalWords > 200 ? 20 : 0;
                    pageScore += article.totalSentences > 10 ? 20 : 0;

                    if (pageScore <= 40)
                    {
                        //do not save such a low-scoring download as an article
                        downloadOnly = true;
                    }
                }

                if (downloadOnly == false)
                {
                    //save article to database
                    if (articleInfo.articleId <= 0)
                    {
                        //add article (which also archives download)
                        articleInfo.articleId = App.ArticlesRepository.Add(articleInfo);
                    }

                    // Save article HTML content to disk using Files utility
                    var domain = queue.url.GetDomainName();
                    var firstTwoLetters = domain.Length >= 2 ? domain.Substring(0, 2) : domain;
                    var relpath = $"articles/{firstTwoLetters}/{domain}/{articleInfo.articleId}.html";
                    Files.SaveFile(relpath, result);
                }

                //get URLs from all anchor links on page
                App.Logger.LogInformation("Collecting links from article...");
                var links = new List<string>();
                if (article.elements != null)
                {
                    foreach (var element in article.elements.Where(e => e.tagName.ToLower() == "a"))
                    {
                        if (element.attribute != null && element.attribute.ContainsKey("href"))
                        {
                            var href = element.attribute["href"];
                            if (!string.IsNullOrEmpty(href) && href.StartsWith("http"))
                            {
                                links.Add(href);
                            }
                        }
                    }
                }
                var addedLinks = 0;
                var addedDomains = 0;
                var urls = new Dictionary<string, List<KeyValuePair<string, string>>>();

                foreach (var url in links)
                {
                    try
                    {
                        if (IsCanceledByUser()) { return; }

                        //validate link url
                        if (string.IsNullOrEmpty(url)) { continue; }
                        string uri = Web.CleanUrl(url, false);
                        if (!App.ValidateURL(uri)) { continue; }
                        if (!Domains.ValidateURL(uri)) { continue; }
                        var domain = uri.GetDomainName();
                        if (!urls.ContainsKey(domain))
                        {
                            urls.Add(domain, new List<KeyValuePair<string, string>>());
                        }
                        var querystring = Web.CleanUrl(url, onlyKeepQueries: ["id=", "item"]).Replace(uri, "");
                        urls[domain].Add(new KeyValuePair<string, string>(uri, querystring));
                    }
                    catch (Exception ex) { }
                }
                article.urls = urls;

                //get all download rules for all domains found on the page
                var downloadRules = new List<DownloadRule>();
                if (urls.Keys.Count > 0)
                {
                    downloadRules = App.DomainsRepository.GetDownloadRulesForDomains(urls.Keys.ToArray());
                }

                //add all found links to the download queue
                var keys = urls.Keys.ToArray();
                var blacklist = App.BlacklistsRepository.CheckAllDomains(keys);
                for (var x = 0; x < keys.Length; x++)
                {
                    var domain = keys[x];
                    try
                    {
                        if (IsCanceledByUser()) { return; }
                        if (blacklist.Any(a => a.domain == domain)) { continue; }
                        var rules = downloadRules.Where(b => b.domain == domain);
                        if (urls[domain] == null || urls[domain].Count == 0) { continue; }

                        //filter URLs that pass the download rules
                        App.ValidateURLs(domain, downloadRules.Where(b => b.domain == domain).ToList(), urls, out var urlsChecked);

                        //add filtered URLs to download queue
                        if (urlsChecked != null && urlsChecked.Count > 0)
                        {
                            var count = App.DownloadsRepository.AddQueueItems(urlsChecked.ToArray(), domain, queue.domainId, queue.feedId);
                            article.urlLinks = urlsChecked;
                            if (count > 0)
                            {
                                addedLinks += count;
                                addedDomains += 1;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.IndexOf("Timeout") >= 0)
                        {
                            App.Logger.LogError("Error: Timeout expired when adding links to download queue");
                            App.DownloadsRepository.UpdateQueueType(queue.qid, Common.Enums.QueueFileType.Timeout);
                        }
                    }
                }

                

                if (isHomePage && article != null)
                {
                    Domain.Analyze(article, queue.domainId);
                }

                App.Logger.LogInformation("Found {0} link{1} on {2} domain{3}...",
                    addedLinks, (addedLinks != 1 ? "s" : ""), addedDomains, (addedDomains != 1 ? "s" : ""));

                if (archive)
                {
                    // Archive the download
                    App.DownloadsRepository.Archive(queue.qid);
                    App.DownloadsArchived++;
                }

                // Check if we should move archived downloads
                if (App.DownloadsArchived > 1000)
                {
                    App.DownloadsArchived = 0;
                    App.Logger.LogInformation("Archiving the last 1,000 completed downloads");
                    App.DownloadsRepository.MoveArchived();
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Error processing article download for {Url}", queue.url);
                App.Logger.LogError("Error: {Message}", ex.Message);
                App.Logger.LogError("Stack: {Stack}", ex.StackTrace);
            }
        }

        private static bool IsCanceledByUser()
        {
            if (App.StopRequested)
            {
                App.Logger.LogInformation("Download processing stopped by user.");
                return true;
            }
            return false;
        }

        private static void HandleRedirectedUrl(DownloadQueue queue, string newurl)
        {
            App.Logger.LogInformation("Redirected URL to {NewUrl}", newurl);

            // Archive current download
            App.DownloadsRepository.Archive(queue.qid);
            App.DownloadsArchived++;

            if (newurl.Length > 255)
            {
                App.Logger.LogInformation("Redirected URL is too long");
                return;
            }

            // Create new download for the redirected URL
            string domainName = newurl.GetDomainName();
            queue.qid = App.DownloadsRepository.AddQueueItem(newurl, domainName, queue.parentId, queue.feedId);
            queue.url = newurl;
            var domain = App.DomainsRepository.GetInfo(domainName);
            if (domain != null)
            {
                queue.domainId = domain.domainId;
                queue.domain = domain.domain;
                queue.downloadRules = App.DomainsRepository.GetDownloadRulesForDomains([domain.domain]).ToList();
            }
        }

        private static bool ProcessDownloadRules(DownloadQueue queue, out bool shouldSkip)
        {
            shouldSkip = false;
            bool downloadOnly = false;

            foreach (var rule in queue.downloadRules)
            {
                if (!rule.rule && !string.IsNullOrEmpty(rule.url) &&
                    App.CheckDownloadRule(rule.url, "", "", queue.url, "", ""))
                {
                    shouldSkip = true;
                    return false;
                }
                else if (rule.rule && App.CheckDownloadRule(rule.url, "", "", queue.url, "", ""))
                {
                    downloadOnly = true;
                }
            }

            return downloadOnly;
        }

        private static void DeleteAllArticles(int domainId)
        {
            var domain = App.DomainsRepository.GetById(domainId);
            if (domain == null) throw new Exception("Domain not found");

            if (!string.IsNullOrEmpty(domain.domain))
            {
                // Get all articles for this domain from the articles repository
                // Using null for subjectId array to get all subjects
                var articles = App.ArticlesRepository.GetList(
                    subjectId: null,
                    domainId: domainId,
                    isActive: ArticleIsActive.Both,  // Get both active and inactive articles
                    isDeleted: false,               // Don't include already deleted articles
                    length: 10000                   // Get a large number to ensure we get all articles
                );

                // Delete each article file
                foreach (var article in articles)
                {
                    try
                    {
                        var domainName = article.url.GetDomainName();
                        Files.DeleteFile(domainName.Substring(0, 2) + "\\" + domain.domain + "\\" + article.articleId + ".html");
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with database deletion
                        App.Logger.LogError("Error deleting article file: {Message}", ex.Message);
                    }
                }
            }

            // Delete all articles from database
            App.DomainsRepository.DeleteAllArticles(domainId);
        }
    }
}
