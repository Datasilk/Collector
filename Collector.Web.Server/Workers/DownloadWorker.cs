using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Common.Extensions.Strings;
using Collector.Common.Models.Articles;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Collector.Web.Server.Workers
{
    public class DownloadWorker : IWorker
    {
        private readonly ILogger<DownloadWorker> _logger;
        private readonly IDownloadsRepository _downloadsRepository;
        private readonly IDomainsRepository _domainsRepository;
        private readonly IBlacklistsRepository _blacklistsRepository;
        private readonly IArticlesRepository _articlesRepository;
        private readonly IFeedsRepository _feedsRepository;
        private readonly IHubContext<WorkerHub> _hubContext;
        private readonly object _stateLock = new();
        private bool _running = false;
        private string _currentStatus = "Idle";
        private int _downloadsProcessed = 0;
        private int _articlesSaved = 0;
        private int _linksFound = 0;
        private Guid _currentWorkerId = Guid.Empty;

        public DownloadWorker(
            ILogger<DownloadWorker> logger,
            IDownloadsRepository downloadsRepository,
            IDomainsRepository domainsRepository,
            IBlacklistsRepository blacklistsRepository,
            IArticlesRepository articlesRepository,
            IFeedsRepository feedsRepository,
            IHubContext<WorkerHub> hubContext)
        {
            _logger = logger;
            _downloadsRepository = downloadsRepository;
            _domainsRepository = domainsRepository;
            _blacklistsRepository = blacklistsRepository;
            _articlesRepository = articlesRepository;
            _feedsRepository = feedsRepository;
            _hubContext = hubContext;
        }

        public Task Stop()
        {
            lock (_stateLock)
            {
                _running = false;
                _currentStatus = "Stopped";
            }
            return Task.CompletedTask;
        }

        public async Task Progress(string appUserId, Guid workerId)
        {
            int processed, saved, links;
            string status;
            lock (_stateLock)
            {
                processed = _downloadsProcessed;
                saved = _articlesSaved;
                links = _linksFound;
                status = _currentStatus;
            }

            await SendWorkerMessage(appUserId, workerId, "DownloadProgress",
                new { processed, saved, links, status }, CancellationToken.None);
        }

        public async Task Start(string appUserId, Guid workerId, int feedId, string domain, int sort, CancellationToken cancellationToken)
        {
            _currentWorkerId = workerId;
            lock (_stateLock)
            {
                _running = true;
                _downloadsProcessed = 0;
                _articlesSaved = 0;
                _linksFound = 0;
                _currentStatus = "Starting download worker...";
            }

            await SendWorkerMessage(appUserId, workerId, "DownloadStarted",
                new { feedId, domain, sort }, cancellationToken);

            try
            {
                // Check feeds first
                await CheckFeeds(appUserId, workerId, feedId, cancellationToken);

                // Process download queue
                while (!cancellationToken.IsCancellationRequested)
                {
                    bool shouldContinue;
                    lock (_stateLock)
                    {
                        shouldContinue = _running;
                    }
                    if (!shouldContinue) break;

                    var processed = await ProcessNextQueueItem(appUserId, workerId, feedId, domain, sort, cancellationToken);
                    if (!processed)
                    {
                        // No more items, wait before checking again
                        _currentStatus = "No downloads queued, waiting 60 seconds...";
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = _currentStatus, type = "info" }, cancellationToken);
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _currentStatus = "Cancelled";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download worker error");
                _currentStatus = $"Error: {ex.Message}";
                await SendWorkerMessage(appUserId, workerId, "DownloadError",
                    new { message = ex.Message }, cancellationToken);
            }

            await SendWorkerMessage(appUserId, workerId, "DownloadComplete",
                new { processed = _downloadsProcessed, saved = _articlesSaved, links = _linksFound }, cancellationToken);
        }

        private async Task CheckFeeds(string appUserId, Guid workerId, int feedId, CancellationToken cancellationToken)
        {
            try
            {
                _currentStatus = "Checking feeds...";
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = _currentStatus }, cancellationToken);

                var feeds = _feedsRepository.Check(feedId);
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Checking {feeds.Count} feed{(feeds.Count != 1 ? "s" : "")}..." }, cancellationToken);

                var i = 0;
                var len = feeds.Count;
                if (len == 0)
                {
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = "Checked feeds." }, cancellationToken);
                    return;
                }

                foreach (var feed in feeds)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    i++;
                    _feedsRepository.UpdateLastChecked(feed.feedId);
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"[{i}/{len}] Checking {feed.doctype} feed: {feed.title}", type = "info" }, cancellationToken);

                    if (feed.doctype == FeedDocType.RSS)
                    {
                        await ProcessRssFeed(appUserId, workerId, feed, i, len, cancellationToken);
                    }
                    else if (feed.doctype == FeedDocType.HTML)
                    {
                        await ProcessHtmlFeed(appUserId, workerId, feed, i, len, cancellationToken);
                    }
                }

                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = "Checked all feeds.", type = "success" }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feeds");
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error checking feeds: {ex.Message}", type = "error" }, cancellationToken);
            }
        }

        private async Task<bool> ProcessNextQueueItem(string appUserId, Guid workerId, int feedId, string domain, int sort, CancellationToken cancellationToken)
        {
            try
            {
                _currentStatus = "Checking queue...";
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = _currentStatus }, cancellationToken);

                var queue = _downloadsRepository.CheckQueue(feedId, domain, 60, (QueueSort)sort);
                if (queue == null)
                {
                    return false;
                }

                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Processing queue item: {queue.url} (domain: {queue.domain}, feedId: {queue.feedId})", type = "info" }, cancellationToken);

                // Validate domain
                if (!ValidateDomain(queue.domain))
                {
                    DeleteAllArticles(queue.domainId);
                    _domainsRepository.IsDeleted(queue.domainId, true);
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = "Invalid Domain", type = "warning" }, cancellationToken);
                    return true;
                }

                // Validate URL
                if (!ValidateURL(queue.url))
                {
                    _downloadsRepository.Delete(queue.qid);
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = "Invalid URL", type = "warning" }, cancellationToken);
                    return true;
                }

                // Process download rules
                bool downloadOnly = ProcessDownloadRules(queue, out bool shouldSkip);
                if (shouldSkip)
                {
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"URL matches download rule and will be skipped ({queue.url})", type = "warning" }, cancellationToken);
                    return true;
                }

                await ProcessArticleDownload(appUserId, workerId, queue, downloadOnly, sort, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item");
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error: {ex.Message}", type = "error" }, cancellationToken);
                return true;
            }
        }

        private async Task ProcessArticleDownload(string appUserId, Guid workerId, DownloadQueue queue, bool downloadOnly, int sort, CancellationToken cancellationToken)
        {
            try
            {
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Downloading {queue.url}...", type = "info" }, cancellationToken);

                string result = "";
                string newurl = "";
                bool isEmpty = false;
                bool isSslError = false;

                try
                {
                    result = Common.Article.Download(queue.url, out newurl, (redirectUrl, redirectStatus) =>
                    {
                        string redirectMessage = redirectStatus switch
                        {
                            0 => $"Too many redirects while navigating {queue.url}",
                            -1 => $"Timeout while navigating {queue.url}",
                            -2 => $"Retrying {queue.url}...",
                            -3 => $"Retrying via Charlotte {queue.url}...",
                            _ => $"Navigating to {redirectUrl}..."
                        };
                        SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = redirectMessage, type = redirectStatus > 0 ? "info" : "warning" }, cancellationToken).GetAwaiter().GetResult();
                    }, (errorUrl, errorStatus) =>
                    {
                        if (errorStatus == -1)
                        {
                            isSslError = true;
                            _domainsRepository.IsEmpty(queue.domainId, true);
                            SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                                new { message = $"SSL connection failed for {errorUrl}; marking domain as empty", type = "error" }, cancellationToken).GetAwaiter().GetResult();
                        }
                    });
                    if (!string.IsNullOrEmpty(result))
                    {
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = $"Downloaded {queue.url} ({result.Length} chars)", type = "success" }, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading article from {Url}", queue.url);
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"Error downloading {queue.url}: {ex.Message}", type = "error" }, cancellationToken);
                    downloadOnly = true;
                    if (sort == 2) { isEmpty = true; }
                }

                if (string.IsNullOrEmpty(result) && string.IsNullOrEmpty(newurl))
                {
                    // Download failed completely; archive the queue item now so it isn't reprocessed
                    _downloadsRepository.Archive(queue.qid);
                    _downloadsProcessed++;
                    if (!isSslError)
                    {
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = $"Download timed out for URL: {queue.url}", type = "error" }, cancellationToken);
                        _currentStatus = $"Processed {queue.url}";
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = _currentStatus, type = "success" }, cancellationToken);
                    }
                    else
                    {
                        _currentStatus = $"SSL failure for {queue.url}";
                    }
                    return;
                }

                // Handle URL redirection
                if (!string.IsNullOrEmpty(newurl) && newurl != queue.url)
                {
                    long redirectedQid = await HandleRedirectedUrl(appUserId, workerId, queue, newurl, cancellationToken);
                    if (redirectedQid > 0)
                    {
                        var redirectedQueue = _downloadsRepository.CheckQueue(queueId: redirectedQid);
                        if (redirectedQueue != null)
                        {
                            await ProcessArticleDownload(appUserId, workerId, redirectedQueue, downloadOnly, sort, cancellationToken);
                        }
                    }
                    return;
                }

                // Validate download results
                _currentStatus = "Validating download...";
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = _currentStatus }, cancellationToken);

                if (sort == 2)
                {
                    downloadOnly = true;
                }

                if (string.IsNullOrEmpty(result))
                {
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"Download timed out for URL: {queue.url}", type = "error" }, cancellationToken);
                    if (sort == 2) { isEmpty = true; }
                }
                else if (result.StartsWith("file:"))
                {
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"URL points to a file of type \"{result.Substring(5)}\"", type = "warning" }, cancellationToken);
                    return;
                }
                else if (result.StartsWith("\"Uncaught TypeError") || result.StartsWith("Object reference not set to an instance of an object"))
                {
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = "Error parsing DOM!", type = "error" }, cancellationToken);
                }
                else if (result.StartsWith("log: "))
                {
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = "Request timeout!", type = "error" }, cancellationToken);
                    if (sort == 2) { isEmpty = true; }
                }

                AnalyzedArticle article = null;
                if (!isEmpty && !string.IsNullOrEmpty(result) && !result.StartsWith("file:"))
                {
                    try
                    {
                        article = Html.DeserializeArticle(result);
                        article.feedId = queue.feedId;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing DOM for {Url}", queue.url);
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = $"Error parsing DOM! {ex.Message}", type = "error" }, cancellationToken);
                    }
                }

                if (article != null && !downloadOnly)
                {
                    // Save article to database
                    await SaveArticle(appUserId, workerId, queue, article, cancellationToken);
                    _articlesSaved++;

                    // Extract and queue links
                    var extractedLinks = await ExtractAndQueueLinks(appUserId, workerId, queue, article, cancellationToken);
                    _linksFound += extractedLinks;
                }

                // Archive the download
                _downloadsRepository.Archive(queue.qid);
                _downloadsProcessed++;
                _currentStatus = $"Processed {queue.url}";
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = _currentStatus, type = "success" }, cancellationToken);

                if (_downloadsProcessed % 1000 == 0)
                {
                    _downloadsRepository.MoveArchived();
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = "Archived completed downloads", type = "success" }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing article download for {Url}", queue.url);
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error: {ex.Message}", type = "error" }, cancellationToken);
            }
        }

        private async Task SaveArticle(string appUserId, Guid workerId, DownloadQueue queue, AnalyzedArticle article, CancellationToken cancellationToken)
        {
            try
            {
                var existing = _articlesRepository.GetByUrl(queue.url);
                if (existing == null)
                {
                    var newArticle = new Data.Entities.Article
                    {
                        feedId = queue.feedId,
                        domain = queue.domain,
                        url = queue.url,
                        title = article.title ?? "",
                        summary = article.summary ?? "",
                        wordcount = article.totalWords,
                        sentencecount = (short?)article.totalSentences,
                        paragraphcount = (short?)article.totalParagraphs,
                        importantcount = (short?)article.totalImportantWords,
                        score = (short?)article.relevance,
                        active = true,
                        datecreated = DateTime.UtcNow
                    };
                    _articlesRepository.Add(newArticle);
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"Saved article: {article.title}", type = "success" }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving article for {Url}", queue.url);
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error saving article for {queue.url}: {ex.Message}", type = "error" }, cancellationToken);
            }
        }

        private async Task<int> ExtractAndQueueLinks(string appUserId, Guid workerId, DownloadQueue queue, AnalyzedArticle article, CancellationToken cancellationToken)
        {
            try
            {
                var links = article.elements
                    .Where(a => a.tagName == "a" && a.attribute.ContainsKey("href"))
                    .Select(a => a.attribute["href"])
                    .Where(url => !string.IsNullOrEmpty(url))
                    .Distinct()
                    .ToList();

                var urlsByDomain = new Dictionary<string, List<string>>();
                foreach (var url in links)
                {
                    try
                    {
                        var uri = url.CleanUrl(false);
                        if (!ValidateURL(uri) || !ValidateDomain(uri.GetDomainName())) continue;
                        var domain = uri.GetDomainName();
                        if (_blacklistsRepository.CheckDomain(domain)) continue;
                        if (!urlsByDomain.ContainsKey(domain)) urlsByDomain[domain] = new List<string>();
                        urlsByDomain[domain].Add(uri);
                    }
                    catch { }
                }

                int totalAdded = 0;
                foreach (var kvp in urlsByDomain)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var domain = kvp.Key;
                    var domainInfo = _domainsRepository.GetInfo(domain);
                    if (domainInfo == null) continue;
                    var downloadRules = _domainsRepository.GetDownloadRules(domainInfo.domainId);
                    var validUrls = new List<string>();
                    foreach (var url in kvp.Value)
                    {
                        bool shouldAdd = true;
                        foreach (var rule in downloadRules)
                        {
                            if (!rule.rule && CheckDownloadRule(rule.url, rule.title, rule.summary, url, "", ""))
                            {
                                shouldAdd = false;
                                break;
                            }
                        }
                        if (shouldAdd) validUrls.Add(url);
                    }
                    if (validUrls.Count > 0)
                    {
                        totalAdded += _downloadsRepository.AddQueueItems(validUrls.ToArray(), domain, queue.domainId, queue.feedId);
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = $"Added {validUrls.Count} link(s) to queue from {domain}", type = "success" }, cancellationToken);
                    }
                }

                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Found {totalAdded} new link(s) on {urlsByDomain.Count} domain(s)", type = "success" }, cancellationToken);
                return totalAdded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting links from {Url}", queue.url);
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error extracting links: {ex.Message}", type = "error" }, cancellationToken);
                return 0;
            }
        }

        private async Task<long> HandleRedirectedUrl(string appUserId, Guid workerId, DownloadQueue queue, string newurl, CancellationToken cancellationToken)
        {
            await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                new { message = $"Redirected URL to {newurl}", type = "warning" }, cancellationToken);
            _downloadsRepository.Archive(queue.qid);
            if (newurl.Length > 255)
            {
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = "Redirected URL is too long", type = "error" }, cancellationToken);
                return 0;
            }
            string domain = newurl.GetDomainName();
            long qid = _downloadsRepository.AddQueueItem(newurl, domain, queue.parentId, queue.feedId);
            await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                new { message = "Added redirected URL to queue", type = "success" }, cancellationToken);
            return qid;
        }

        private async Task ProcessRssFeed(string appUserId, Guid workerId, Feed feed, int index, int totalFeeds, CancellationToken cancellationToken)
        {
            try
            {
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Downloading RSS feed: {feed.url}", type = "info" }, cancellationToken);
                using var client = new WebClient();
                var response = client.DownloadString(feed.url);
                var content = Syndication.Read(response);
                var links = content.items.Select(a => a.link).Where(url => !string.IsNullOrEmpty(url));
                var urlsByDomain = new Dictionary<string, List<string>>();

                foreach (var url in links)
                {
                    try
                    {
                        var uri = url.CleanUrl(false);
                        if (!ValidateURL(uri) || !ValidateDomain(uri.GetDomainName())) continue;
                        var domain = uri.GetDomainName();
                        if (_blacklistsRepository.CheckDomain(domain)) continue;
                        if (!urlsByDomain.ContainsKey(domain)) urlsByDomain[domain] = new List<string>();
                        urlsByDomain[domain].Add(uri);
                    }
                    catch { }
                }

                int totalAdded = 0;
                foreach (var kvp in urlsByDomain)
                {
                    var domainInfo = _domainsRepository.GetInfo(kvp.Key);
                    if (domainInfo == null) continue;
                    var downloadRules = _domainsRepository.GetDownloadRules(domainInfo.domainId);
                    var validUrls = new List<string>();
                    foreach (var url in kvp.Value)
                    {
                        bool shouldAdd = true;
                        foreach (var rule in downloadRules)
                        {
                            if (!rule.rule && CheckDownloadRule(rule.url, rule.title, rule.summary, url, "", ""))
                            {
                                shouldAdd = false;
                                break;
                            }
                        }
                        if (shouldAdd) validUrls.Add(url);
                    }
                    if (validUrls.Count > 0)
                    {
                        totalAdded += _downloadsRepository.AddQueueItems(validUrls.ToArray(), kvp.Key, feed.domainId, feed.feedId);
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = $"Added {validUrls.Count} link(s) to queue from {kvp.Key} (RSS)", type = "success" }, cancellationToken);
                    }
                }

                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"({index} of {totalFeeds}) Found {totalAdded} new link(s) from {feed.title}", type = "success" }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RSS feed {Url}", feed.url);
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error processing RSS feed {feed.url}: {ex.Message}", type = "error" }, cancellationToken);
            }
        }

        private async Task ProcessHtmlFeed(string appUserId, Guid workerId, Feed feed, int index, int totalFeeds, CancellationToken cancellationToken)
        {
            try
            {
                string result;
                string newurl = "";
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Downloading HTML feed: {feed.url}", type = "info" }, cancellationToken);
                try
                {
                    result = Common.Article.Download(feed.url, out newurl, (redirectUrl, redirectStatus) =>
                    {
                        string redirectMessage = redirectStatus switch
                        {
                            0 => $"Too many redirects while navigating {feed.url}",
                            -1 => $"Timeout while navigating {feed.url}",
                            -2 => $"Retrying {feed.url}...",
                            _ => $"Navigating to {redirectUrl}..."
                        };
                        SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = redirectMessage, type = redirectStatus > 0 ? "info" : "warning" }, cancellationToken).GetAwaiter().GetResult();
                    }, (errorUrl, errorStatus) =>
                    {
                        if (errorStatus == -1)
                        {
                            _domainsRepository.IsEmpty(feed.domainId, true);
                            SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                                new { message = $"SSL connection failed for {errorUrl}; marking domain as empty", type = "error" }, cancellationToken).GetAwaiter().GetResult();
                        }
                    });
                    if (!string.IsNullOrEmpty(newurl)) feed.url = newurl;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading HTML feed {Url}", feed.url);
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"Error downloading HTML feed {feed.url}: {ex.Message}", type = "error" }, cancellationToken);
                    _feedsRepository.UpdateLastChecked(feed.feedId);
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
                    var htmlPreview = result?.Length > 200000 ? result.Substring(0, 200000) : result;
                    await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                        new { message = $"Error parsing HTML feed {feed.url}: {ex.Message}", type = "error", html = htmlPreview }, cancellationToken);
                    _feedsRepository.UpdateLastChecked(feed.feedId);
                    return;
                }

                var links = article.elements
                    .Where(a => a.tagName == "a" && a.attribute.ContainsKey("href"))
                    .Select(a => a.attribute["href"])
                    .Where(url => !string.IsNullOrEmpty(url));

                var urlsByDomain = new Dictionary<string, List<string>>();
                foreach (var url in links)
                {
                    try
                    {
                        var uri = url.CleanUrl(false);
                        if (!ValidateURL(uri) || !ValidateDomain(uri.GetDomainName())) continue;
                        var domain = uri.GetDomainName();
                        if (_blacklistsRepository.CheckDomain(domain)) continue;
                        if (!urlsByDomain.ContainsKey(domain)) urlsByDomain[domain] = new List<string>();
                        urlsByDomain[domain].Add(uri);
                    }
                    catch { }
                }

                int totalAdded = 0;
                foreach (var kvp in urlsByDomain)
                {
                    var domainInfo = _domainsRepository.GetInfo(kvp.Key);
                    if (domainInfo == null) continue;
                    var downloadRules = _domainsRepository.GetDownloadRules(domainInfo.domainId);
                    var validUrls = new List<string>();
                    foreach (var url in kvp.Value)
                    {
                        bool shouldAdd = true;
                        foreach (var rule in downloadRules)
                        {
                            if (!rule.rule && CheckDownloadRule(rule.url, rule.title, rule.summary, url, "", ""))
                            {
                                shouldAdd = false;
                                break;
                            }
                        }
                        if (shouldAdd) validUrls.Add(url);
                    }
                    if (validUrls.Count > 0)
                    {
                        totalAdded += _downloadsRepository.AddQueueItems(validUrls.ToArray(), kvp.Key, feed.domainId, feed.feedId);
                        await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                            new { message = $"Added {validUrls.Count} link(s) to queue from {kvp.Key} (HTML)", type = "success" }, cancellationToken);
                    }
                }

                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"({index} of {totalFeeds}) Found {totalAdded} new link(s) from {feed.title}", type = "success" }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing HTML feed {Url}", feed.url);
                await SendWorkerMessage(appUserId, workerId, "DownloadUpdate",
                    new { message = $"Error processing HTML feed {feed.url}: {ex.Message}", type = "error" }, cancellationToken);
            }
        }

        private bool ProcessDownloadRules(DownloadQueue queue, out bool shouldSkip)
        {
            shouldSkip = false;
            bool downloadOnly = false;
            foreach (var rule in queue.downloadRules ?? new List<DownloadRule>())
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
            if (domain == null) return;
            if (!string.IsNullOrEmpty(domain.domain))
            {
                var articles = _articlesRepository.GetList(
                    subjectId: null,
                    domainId: domainId,
                    isActive: ArticleIsActive.Both,
                    isDeleted: false,
                    length: 10000);
                foreach (var article in articles)
                {
                    try
                    {
                        var domainName = article.url.GetDomainName();
                        Files.DeleteFile(Files.Paths.Articles, domainName.Substring(0, 2) + "\\" + domain.domain + "\\" + article.articleId + ".html");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting article file: {ex.Message}");
                    }
                }
            }
            _domainsRepository.DeleteAllArticles(domainId);
        }

        private bool ValidateURL(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            try
            {
                var uri = new Uri(url);
                return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
            }
            catch { return false; }
        }

        private bool ValidateDomain(string domain)
        {
            return !string.IsNullOrEmpty(domain) && domain.Contains(".") && !domain.Contains(" ");
        }

        private bool CheckDownloadRule(string ruleUrl, string ruleTitle, string ruleSummary, string url, string title, string summary)
        {
            if (!string.IsNullOrEmpty(ruleUrl) && url.Contains(ruleUrl)) return true;
            if (!string.IsNullOrEmpty(ruleTitle) && !string.IsNullOrEmpty(title) && title.Contains(ruleTitle)) return true;
            if (!string.IsNullOrEmpty(ruleSummary) && !string.IsNullOrEmpty(summary) && summary.Contains(ruleSummary)) return true;
            return false;
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }
    }
}
