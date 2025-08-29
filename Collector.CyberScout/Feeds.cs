using System.Net;
using System.Data;
using Microsoft.Extensions.Logging;
using Collector.Common;
using Collector.Common.Extensions.Strings;
using Collector.Common.Models.Articles;
using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.CyberScout
{
    public class Feeds
    {
        public static async Task CheckFeeds(int? feedId)
        {
            try
            {
                App.Logger.LogInformation("Checking feeds with ID {FeedId}", feedId);

                // Get feeds to check
                var feeds = App.FeedsRepository.Check(feedId ?? 0);

                App.Logger.LogInformation("Checking {Count} feed{Plural}...", feeds.Count, feeds.Count != 1 ? "s" : "");

                var i = 0;
                var len = feeds.Count;

                if (len == 0)
                {
                    App.Logger.LogInformation("No feeds to check.");
                    return;
                }

                foreach (var feed in feeds)
                {
                    if (App.StopRequested)
                    {
                        App.Logger.LogInformation("Feed checking stopped by user.");
                        return;
                    }

                    i++;

                    // Update last checked timestamp
                    //App.FeedsRepository.UpdateLastChecked(feed.feedId);

                    // Process feed based on type
                    if (feed.doctype == FeedDocType.RSS)
                    {
                        await ProcessRssFeed(feed, i, len);
                    }
                    else if (feed.doctype == FeedDocType.HTML)
                    {
                        await ProcessHtmlFeed(feed, i, len);
                    }

                    // Check for cancellation after each feed
                    if (App.StopRequested)
                    {
                        App.Logger.LogInformation("Feed checking stopped by user.");
                        return;
                    }
                }

                App.Logger.LogInformation("Checked all feeds.");
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Error checking feeds: {Message}", ex.Message);
            }
        }

        private static async Task ProcessRssFeed(Feed feed, int index, int totalFeeds)
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
                        if (!App.ValidateURL(uri) || !App.ValidateDomain(uri.GetDomainName())) continue;

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
                        if (App.BlacklistsRepository.CheckDomain(domain)) continue;

                        var domainInfo = App.DomainsRepository.GetInfo(domain);
                        var downloadRules = App.DomainsRepository.GetDownloadRules(domainInfo.domainId);
                        App.ValidateURLs(domain, downloadRules, urls, out var urlsChecked);

                        if (urlsChecked.Count > 0)
                        {
                            int added = App.DownloadsRepository.AddQueueItems(urlsChecked.ToArray(), domain, feed.domainId, feed.feedId);
                            totalAdded += added;
                        }
                    }

                    App.Logger.LogInformation("({Index} of {Total}) Found {Added} new link{Plural} from {Title}: {Url}",
                        index, totalFeeds, totalAdded, totalAdded != 1 ? "s" : "", feed.title, feed.url);
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Error processing RSS feed {Url}: {Message}", feed.url, ex.Message);
            }
        }

        private static async Task ProcessHtmlFeed(Feed feed, int index, int totalFeeds)
        {
            try
            {
                string result;
                string newurl = "";

                try
                {
                    //TODO: make Charlotte download the feed url instead
                    result = Common.Article.Download(feed.url, out newurl);
                    if (!string.IsNullOrEmpty(newurl))
                    {
                        feed.url = newurl;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.LogError(ex, "Error downloading HTML feed {Url}!", feed.url);
                    return;
                }

                AnalyzedArticle article;
                if (result.StartsWith("Error calling Charlotte Web Router") || result.StartsWith("log: "))
                {
                    App.Logger.LogError(result);
                    return;
                }
                try
                {
                    article = Html.DeserializeArticle(result);
                }
                catch (Exception ex)
                {
                    App.Logger.LogError(ex, "Error parsing DOM for HTML feed {Url}!", feed.url);
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
                    if (!App.ValidateURL(uri) || !App.ValidateDomain(uri.GetDomainName())) continue;

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
                    //skip if domain is in blacklist
                    if (App.BlacklistsRepository.CheckDomain(domain)) continue;

                    var domainInfo = App.DomainsRepository.GetInfo(domain);
                    if (domainInfo != null)
                    {
                        var downloadRules = App.DomainsRepository.GetDownloadRules(domainInfo.domainId);
                        App.ValidateURLs(domain, downloadRules, urls, out var urlsChecked);

                        if (urlsChecked.Count > 0)
                        {
                            int added = App.DownloadsRepository.AddQueueItems(urlsChecked.ToArray(), domain, feed.domainId, feed.feedId);
                            totalAdded += added;
                        }
                    }
                    else
                    {
                        //domain doesn't exist in the database yet
                        App.Logger.LogInformation("Domain (" + domain + ") doesn't exist yet");
                        var domainId = App.DomainsRepository.Add(domain, "", feed.domainId);
                        App.ValidateURLs(domain, null, urls, out var urlsChecked);
                        if (urlsChecked.Count > 0)
                        {
                            int added = App.DownloadsRepository.AddQueueItems(urlsChecked.ToArray(), domain, feed.domainId, feed.feedId);
                            totalAdded += added;
                        }
                    }
                }

                App.Logger.LogInformation("({Index} of {Total}) {Added} {Url}",
                    index.ToString().Fill(totalFeeds.ToString().Length, ' '), 
                    totalFeeds,
                    ("Found " + totalAdded.ToString().FillLeft(4, ' ') + " new link" + (totalAdded != 1 ? "s" : " ") + " from " + (feed.title + " : ").Fill(28, ' ')).Fill(20 + 28, ' '),
                    feed.url);
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Error processing HTML feed {Url}: {Message}", feed.url, ex.Message);
            }
        }
    }
}
