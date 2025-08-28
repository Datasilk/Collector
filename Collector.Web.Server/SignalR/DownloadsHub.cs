using System.Net;
using Microsoft.AspNetCore.SignalR;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Common.Extensions.Strings;
using Collector.Data.Interfaces;
using Collector.Data.Enums;
using Collector.Common.Models.Articles;

namespace Collector.Web.Server.SignalR
{
    public class DownloadsHub : Hub
    {
        private readonly ILogger<DownloadsHub> _logger;
        private readonly IDownloadsRepository _downloadsRepository;
        private readonly IDomainsRepository _domainsRepository;
        private readonly IBlacklistsRepository _blacklistsRepository;
        private readonly IArticlesRepository _articlesRepository;
        private readonly IFeedsRepository _feedsRepository;
        
        private List<string> StopQueues = new List<string>();
        private static int downloadsArchived = 0;

        public DownloadsHub(
            ILogger<DownloadsHub> logger,
            IDownloadsRepository downloadsRepository,
            IDomainsRepository domainsRepository,
            IBlacklistsRepository blacklistsRepository,
            IArticlesRepository articlesRepository,
            IFeedsRepository feedsRepository)
        {
            _logger = logger;
            _downloadsRepository = downloadsRepository;
            _domainsRepository = domainsRepository;
            _blacklistsRepository = blacklistsRepository;
            _articlesRepository = articlesRepository;
            _feedsRepository = feedsRepository;
        }

        public async Task CheckQueue(string id, int feedId, string domainName, int sort)
        {
            try
            {
                await Clients.Caller.SendAsync("update", "Checking queue...");
                
                // Get the next download from the queue
                var queue = _downloadsRepository.CheckQueue(feedId, domainName, 60, (QueueSort)sort);
                
                _logger.LogInformation("Checking download queue for feed {FeedId}, domain {DomainName}, sort {Sort}", feedId, domainName, sort);
                
                if (queue != null)
                {
                    if (CheckToStopQueue(id, Clients.Caller)) { return; }

                    // Validate domain
                    if (!ValidateDomain(queue.domain))
                    {
                        // Invalid domain, mark as deleted
                        DeleteAllArticles(queue.domainId);
                        _domainsRepository.IsDeleted(queue.domainId, true);
                        await Clients.Caller.SendAsync("update", "Invalid Domain");
                        await Clients.Caller.SendAsync("checked", 1, 0);
                        return;
                    }

                    // Validate URL
                    if (!ValidateURL(queue.url))
                    {
                        // Delete download from database
                        _downloadsRepository.Delete(queue.qid);
                        await Clients.Caller.SendAsync("update", "Invalid URL");
                        await Clients.Caller.SendAsync("checked", 1, 0);
                        return;
                    }

                    // Process download rules
                    bool downloadOnly = ProcessDownloadRules(queue, out bool shouldSkip);
                    if (shouldSkip)
                    {
                        await Clients.Caller.SendAsync("update", $"URL matches download rule and will be skipped (<a href=\"{queue.url}\" target=\"_blank\">{queue.url}</a>)");
                        await Clients.Caller.SendAsync("checked", 1, 0);
                        return;
                    }

                    // Download and process the article
                    await ProcessArticleDownload(id, queue, downloadOnly, sort);
                }
                else
                {
                    await Clients.Caller.SendAsync("update", "No downloads queued at the moment...");
                    await Clients.Caller.SendAsync("checked", 0, 0, 0, 0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking download queue");
                
                if (ex.Message.Contains("No connection could be made because the target machine actively refused it"))
                {
                    await Clients.Caller.SendAsync("update", ex.Message);
                }
                else
                {
                    await Clients.Caller.SendAsync("update", "An error occurred while checking the download queue");
                    await Clients.Caller.SendAsync("checked", 1, 0, 0, 0, 0, 0);
                }
                
                // TODO: Replace with proper logging
                // Query.Logs.LogError(0, "", "DownloadHub", ex.Message, ex.StackTrace);
            }
        }

        public async Task StopQueue(string id)
        {
            StopQueues.Add(id);
            await Clients.Caller.SendAsync("update", "Stopping download queue...");
            _logger.LogInformation("Stopping download queue with ID {Id}", id);
        }

        private bool CheckToStopQueue(string id, IClientProxy Caller)
        {
            if (StopQueues.Contains(id))
            {
                StopQueues.Remove(id);
                Caller.SendAsync("update", "Stopped download queue");
                return true;
            }
            return false;
        }

        public async Task CheckFeeds(int feedId)
        {
            try
            {
                _logger.LogInformation("Checking feeds with ID {FeedId}", feedId);
                await Clients.Caller.SendAsync("update", "Checking feeds...");
                
                // Get feeds to check
                var feeds = _feedsRepository.Check(feedId);
                
                await Clients.Caller.SendAsync("update", $"Checking {feeds.Count} feed{(feeds.Count != 1 ? "s" : "")}...");
                
                var i = 0;
                var len = feeds.Count;
                
                if (len == 0)
                {
                    await Clients.Caller.SendAsync("update", "Checked feeds.");
                    await Clients.Caller.SendAsync("checkedfeeds");
                    return;
                }
                
                foreach (var feed in feeds)
                {
                    i++;
                    
                    // Update last checked timestamp
                    _feedsRepository.UpdateLastChecked(feed.feedId);
                    
                    // Process feed based on type
                    if (feed.doctype == FeedDocType.RSS)
                    {
                        await ProcessRssFeed(feed, i, len);
                    }
                    else if (feed.doctype == FeedDocType.HTML)
                    {
                        await ProcessHtmlFeed(feed, i, len);
                    }
                }
                
                await Clients.Caller.SendAsync("update", "Checked all feeds.");
                await Clients.Caller.SendAsync("checkedfeeds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feeds");
                await Clients.Caller.SendAsync("update", $"Error checking feeds: {ex.Message}");
            }
        }

        // Helper methods for processing downloads and feeds
        
        private async Task ProcessArticleDownload(string id, DownloadQueue queue, bool downloadOnly, int sort)
        {
            try {
                AnalyzedArticle article = new AnalyzedArticle();
                
                await Clients.Caller.SendAsync("update", $"Downloading <a href=\"{queue.url}\" target=\"_blank\">{queue.url}</a>...");
                
                // Download content
                string result = "";
                string newurl = "";
                bool isEmpty = false;
                
                try
                {
                    // Use the Article.Download method from Collector.Common
                    result = Common.Article.Download(queue.url, out newurl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading article from {Url}", queue.url);
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
                await Clients.Caller.SendAsync("update", "Validating download...");
                if (sort == 2)
                {
                    // Don't create articles for homepages
                    downloadOnly = true;
                }
                
                if (CheckToStopQueue(id, Clients.Caller)) { return; }
                
                // Process the downloaded content
                if (string.IsNullOrEmpty(result))
                {
                    await Clients.Caller.SendAsync("update", $"Download timed out for URL: <a href=\"{queue.url}\" target=\"_blank\">{queue.url}</a>");
                    await Clients.Caller.SendAsync("checked", 0, 0);
                    if (sort == 2) { isEmpty = true; }
                }
                else if (result.StartsWith("file:"))
                {
                    await Clients.Caller.SendAsync("update", $"URL points to a file of type \"{result.Substring(5)}\"");
                    await Clients.Caller.SendAsync("checked", 1, 0);
                    return;
                }
                else if (result.StartsWith("\"Uncaught TypeError") || result.StartsWith("Object reference not set to an instance of an object"))
                {
                    await Clients.Caller.SendAsync("update", "Error parsing DOM!");
                    await Clients.Caller.SendAsync("checked", 1, 0);
                }
                else if (result.StartsWith("log: "))
                {
                    await Clients.Caller.SendAsync("update", "Request timeout!");
                    await Clients.Caller.SendAsync("checked", 1, 0);
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
                        _logger.LogError(ex, "Error parsing DOM for {Url}", queue.url);
                        await Clients.Caller.SendAsync("update", "Error parsing DOM!");
                        await Clients.Caller.SendAsync("checked", 1, 0);
                    }
                }
                
                // Archive the download and notify client
                _downloadsRepository.Archive(queue.qid);
                downloadsArchived++;
                
                await Clients.Caller.SendAsync("checked", 0, 1, 0, 0, 0, 0);
                
                // Check if we should move archived downloads
                if (downloadsArchived > 1000)
                {
                    downloadsArchived = 0;
                    await Clients.Caller.SendAsync("update", "Archiving the last 1,000 completed downloads");
                    _downloadsRepository.MoveArchived();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing article download for {Url}", queue.url);
                await Clients.Caller.SendAsync("update", $"Error: {ex.Message}");
                await Clients.Caller.SendAsync("checked", 1, 0, 0, 0, 0, 0);
            }
        }
        
        private async Task HandleRedirectedUrl(DownloadQueue queue, string newurl)
        {
            await Clients.Caller.SendAsync("update", $"Redirected URL to <a href=\"{newurl}\" target=\"_blank\">{newurl}</a>");
            
            // Archive current download
            _downloadsRepository.Archive(queue.qid);
            downloadsArchived++;
            
            if (newurl.Length > 255)
            {
                await Clients.Caller.SendAsync("update", "Redirected URL is too long");
                await Clients.Caller.SendAsync("checked", 1, 0);
                return;
            }
            
            // Create new download for the redirected URL
            string domain = newurl.GetDomainName();
            long newQid = _downloadsRepository.AddQueueItem(newurl, domain, queue.parentId, queue.feedId);
            
            await Clients.Caller.SendAsync("update", "Added redirected URL to queue");
            await Clients.Caller.SendAsync("checked", 1, 0);
        }
        
        private async Task ProcessRssFeed(Feed feed, int index, int totalFeeds)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var response = client.DownloadString(feed.url);
                    var content = Syndication.Read(response);
                    var links = content.items.Select(a => a.link);
                    var urls = new Dictionary<string, List<KeyValuePair<string, string>>>();

                    // Separate links by domain
                    foreach (var url in links)
                    {
                        if (string.IsNullOrEmpty(url)) continue;
                        
                        var uri = url.CleanUrl(false);
                        if (!ValidateURL(uri) || !ValidateDomain(uri.GetDomainName())) continue;
                        
                        var domain = uri.GetDomainName();
                        if (!urls.ContainsKey(domain))
                        {
                            urls.Add(domain, new List<KeyValuePair<string, string>>());
                        }
                        
                        var querystring = url.CleanUrl(onlyKeepQueries: new string[] { "id=", "item" }).Replace(uri, "");
                        urls[domain].Add(new KeyValuePair<string, string>(uri, querystring));
                    }

                    // Process URLs for each domain
                    int totalAdded = 0;
                    foreach (var domain in urls.Keys)
                    {
                        if (_blacklistsRepository.CheckDomain(domain)) continue;

                        var domainInfo = _domainsRepository.GetInfo(domain);
                        var downloadRules = _domainsRepository.GetDownloadRules(domainInfo.domainId);
                        ValidateURLs(domain, downloadRules, urls, out var urlsChecked);
                        
                        if (urlsChecked.Count > 0)
                        {
                            int added = _downloadsRepository.AddQueueItems(urlsChecked.ToArray(), domain, feed.domainId, feed.feedId);
                            totalAdded += added;
                        }
                    }
                    
                    await Clients.Caller.SendAsync("feed", totalAdded, 
                        $"<span>({index} of {totalFeeds}) Found {totalAdded} new link{(totalAdded != 1 ? "s" : "")} from {feed.title}: <a href=\"{feed.url}\" target=\"_blank\">{feed.url}</a></span>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RSS feed {Url}", feed.url);
                await Clients.Caller.SendAsync("update", $"Error processing feed {feed.url}: {ex.Message}");
            }
        }
        
        private async Task ProcessHtmlFeed(Feed feed, int index, int totalFeeds)
        {
            try
            {
                string result;
                string newurl = "";
                
                try
                {
                    result = Common.Article.Download(feed.url, out newurl);
                    if (!string.IsNullOrEmpty(newurl))
                    {
                        feed.url = newurl;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading HTML feed {Url}", feed.url);
                    _feedsRepository.UpdateLastChecked(feed.feedId);
                    await Clients.Caller.SendAsync("update", $"Error downloading {feed.url}!");
                    return;
                }
                
                AnalyzedArticle article;
                try
                {
                    article = Html.DeserializeArticle(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing DOM for HTML feed {Url}", feed.url);
                    _feedsRepository.UpdateLastChecked(feed.feedId);
                    await Clients.Caller.SendAsync("update", $"Error parsing feed DOM for {feed.url}!");
                    return;
                }
                
                // Extract links from the article
                var links = article.elements
                    .Where(a => a.tagName == "a" && a.attribute.ContainsKey("href"))
                    .Select(a => a.attribute["href"]);
                
                var urls = new Dictionary<string, List<KeyValuePair<string, string>>>();
                
                // Process links
                foreach (var url in links)
                {
                    if (string.IsNullOrEmpty(url)) continue;
                    
                    var uri = url.CleanUrl(false);
                    if (!ValidateURL(uri) || !ValidateDomain(uri.GetDomainName())) continue;
                    
                    var domain = uri.GetDomainName();
                    if (!urls.ContainsKey(domain))
                    {
                        urls.Add(domain, new List<KeyValuePair<string, string>>());
                    }
                    
                    var querystring = url.CleanUrl(onlyKeepQueries: new string[] { "id=", "item" }).Replace(uri, "");
                    urls[domain].Add(new KeyValuePair<string, string>(uri, querystring));
                }
                
                // Process URLs for each domain
                int totalAdded = 0;
                foreach (var domain in urls.Keys)
                {
                    if (_blacklistsRepository.CheckDomain(domain)) continue;

                    var domainInfo = _domainsRepository.GetInfo(domain);
                    var downloadRules = _domainsRepository.GetDownloadRules(domainInfo.domainId);
                    ValidateURLs(domain, downloadRules, urls, out var urlsChecked);
                    
                    if (urlsChecked.Count > 0)
                    {
                        int added = _downloadsRepository.AddQueueItems(urlsChecked.ToArray(), domain, feed.domainId, feed.feedId);
                        totalAdded += added;
                    }
                }
                
                await Clients.Caller.SendAsync("feed", totalAdded, 
                    $"<span>({index} of {totalFeeds}) Found {totalAdded} new link{(totalAdded != 1 ? "s" : "")} from {feed.title}: <a href=\"{feed.url}\" target=\"_blank\">{feed.url}</a></span>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing HTML feed {Url}", feed.url);
                await Clients.Caller.SendAsync("update", $"Error processing feed {feed.url}: {ex.Message}");
            }
        }
        
        private bool ProcessDownloadRules(DownloadQueue queue, out bool shouldSkip)
        {
            shouldSkip = false;
            bool downloadOnly = false;
            
            foreach (var rule in queue.downloadRules)
            {
                if (!rule.rule && !string.IsNullOrEmpty(rule.url) && 
                    CheckDownloadRule(rule.url, "", "", queue.url, "", ""))
                {
                    shouldSkip = true;
                    return false;
                }
                else if (rule.rule && CheckDownloadRule(rule.url, "", "", queue.url, "", ""))
                {
                    downloadOnly = true;
                }
            }
            
            return downloadOnly;
        }

        private void DeleteAllArticles(int domainId)
        {
            var domain = _domainsRepository.GetById(domainId);
            if (domain == null) throw new Exception("Domain not found");

            if (!string.IsNullOrEmpty(domain.domain))
            {
                // Get all articles for this domain from the articles repository
                // Using null for subjectId array to get all subjects
                var articles = _articlesRepository.GetList(
                    subjectId: null,
                    domainId: domainId,
                    isActive: Data.Enums.ArticleIsActive.Both,  // Get both active and inactive articles
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
                        Console.WriteLine($"Error deleting article file: {ex.Message}");
                    }
                }
            }

            // Delete all articles from database
            _domainsRepository.DeleteAllArticles(domainId);
        }

        private bool ValidateURL(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            
            try
            {
                // Basic URL validation
                var uri = new Uri(url);
                return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
            }
            catch
            {
                return false;
            }
        }
        
        private bool ValidateDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return false;
            
            // Basic domain validation - can be expanded as needed
            return domain.Contains(".") && !domain.Contains(" ");
        }

        private void ValidateURLs(string domain, List<DownloadRule> downloadRules, Dictionary<string, List<KeyValuePair<string, string>>> urls, out List<string> urlsChecked)
        {
            urlsChecked = new List<string>();
            
            if (!urls.ContainsKey(domain) || urls[domain] == null)
            {
                return;
            }
            
            foreach (var urlPair in urls[domain])
            {
                string url = urlPair.Key + urlPair.Value;
                bool shouldAdd = true;
                
                // Check against download rules
                foreach (var rule in downloadRules)
                {
                    if (!rule.rule && CheckDownloadRule(rule.url, rule.title, rule.summary, url, "", ""))
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                
                if (shouldAdd)
                {
                    urlsChecked.Add(url);
                }
            }
        }
        
        private bool CheckDownloadRule(string ruleUrl, string ruleTitle, string ruleSummary, string url, string title, string summary)
        {
            // Simple implementation - can be expanded as needed
            if (!string.IsNullOrEmpty(ruleUrl) && url.Contains(ruleUrl))
            {
                return true;
            }
            
            if (!string.IsNullOrEmpty(ruleTitle) && !string.IsNullOrEmpty(title) && title.Contains(ruleTitle))
            {
                return true;
            }
            
            if (!string.IsNullOrEmpty(ruleSummary) && !string.IsNullOrEmpty(summary) && summary.Contains(ruleSummary))
            {
                return true;
            }
            
            return false;
        }
        
        // Using Strings.Web.GetDomainName instead of a local implementation
    }
}
