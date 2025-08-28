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
                App.Logger.LogInformation("Checking queue...");

                // Check if the operation was canceled before starting
                if (App.StopRequested)
                {
                    App.Logger.LogInformation("Queue processing stopped by user.");
                    return;
                }

                // Get the next download from the queue
                var queue = App.DownloadsRepository.CheckQueue(feedId, domainName, 60, (QueueSort)sort);

                App.Logger.LogInformation("Checking download queue for feed {FeedId}, domain {DomainName}, sort {Sort}", feedId, domainName, sort);

                if (queue != null)
                {
                    if (App.StopRequested)
                    {
                        App.Logger.LogInformation("Queue processing stopped by user.");
                        return;
                    }

                    // Validate domain
                    if (!App.ValidateDomain(queue.domain))
                    {
                        // Invalid domain, mark as deleted
                        DeleteAllArticles(queue.domainId);
                        App.DomainsRepository.IsDeleted(queue.domainId, true);
                        App.Logger.LogInformation("Invalid Domain");
                        return;
                    }

                    // Validate URL
                    if (!App.ValidateURL(queue.url))
                    {
                        // Delete download from database
                        App.DownloadsRepository.Delete(queue.qid);
                        App.Logger.LogInformation("Invalid URL");
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
                    await ProcessArticleDownload(id, queue, downloadOnly, sort);
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

        private static async Task ProcessArticleDownload(string id, DownloadQueue queue, bool downloadOnly, int sort)
        {
            try
            {
                // Check if the operation was canceled before starting
                if (App.StopRequested)
                {
                    App.Logger.LogInformation("Download processing stopped by user.");
                    return;
                }

                AnalyzedArticle article = new AnalyzedArticle();

                App.Logger.LogInformation("Downloading {Url}...", queue.url);

                // Download content
                string result = "";
                string newurl = "";
                bool isEmpty = false;

                try
                {
                    // Use the Article.Download method from Collector.Common
                    result = Common.Article.Download(queue.url, out newurl);

                    // Check if the operation was canceled during download
                    if (App.StopRequested)
                    {
                        App.Logger.LogInformation("Download processing stopped by user.");
                        return;
                    }
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
                    await HandleRedirectedUrl(queue, newurl);
                    return;
                }

                // Validate download results
                App.Logger.LogInformation("Validating download...");
                if (sort == 2)
                {
                    // Don't create articles for homepages
                    downloadOnly = true;
                }

                // Check if the operation was canceled
                if (App.StopRequested)
                {
                    App.Logger.LogInformation("Download processing stopped by user.");
                    return;
                }

                // Process the downloaded content
                if (string.IsNullOrEmpty(result))
                {
                    App.Logger.LogInformation("Download timed out for URL: {Url}", queue.url);
                    if (sort == 2) { isEmpty = true; }
                }
                else if (result.StartsWith("file:"))
                {
                    App.Logger.LogInformation("URL points to a file of type \"{FileType}\"", result.Substring(5));
                    return;
                }
                else if (result.StartsWith("\"Uncaught TypeError") || result.StartsWith("Object reference not set to an instance of an object"))
                {
                    App.Logger.LogInformation("Error parsing DOM!");
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
                        App.Logger.LogInformation("Error parsing DOM!");
                    }
                }

                // Archive the download and notify client
                App.DownloadsRepository.Archive(queue.qid);
                App.DownloadsArchived++;

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
            }
        }

        private static async Task HandleRedirectedUrl(DownloadQueue queue, string newurl)
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
            string domain = newurl.GetDomainName();
            long newQid = App.DownloadsRepository.AddQueueItem(newurl, domain, queue.parentId, queue.feedId);

            App.Logger.LogInformation("Added redirected URL to queue");
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
