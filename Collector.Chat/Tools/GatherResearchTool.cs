using System.Text.RegularExpressions;
using System.Web;
using Collector.Chat.Models;
using Collector.Common;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for gathering research by searching major search engines and collecting relevant URLs
/// </summary>
public class GatherResearchTool : IChatTool
{
    public string ToolKey => "gather-research";

    public string Description => "Search major search engines to find relevant URLs for research. Automatically adds web-scrape steps to the plan for each discovered URL.";

    public bool AvailableToOllama => true;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "query",
            DataType = "string",
            Description = "The search query to use"
        },
        new ToolParameter
        {
            Key = "maxResults",
            DataType = "int",
            Description = "Maximum number of URLs to collect (default: 5)"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField
        {
            Key = "urls",
            DataType = "array"
        },
        new ToolResponseField
        {
            Key = "count",
            DataType = "int"
        }
    };

    public async Task Run(
        string userMessage,
        string ragContext,
        Dictionary<string, string> data,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        ExecutionPlan? plan = null,
        int currentStepIndex = -1,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null,
        OnSaveChatHistory? onSaveChatHistory = null,
        SendMessage? sendMessage = null)
    {
        try
        {
            onProgress(5, "Initializing research gathering...");

            // Get parameters
            var query = data.GetValueOrDefault("query", userMessage);
            var maxResultsStr = data.GetValueOrDefault("maxResults", "5");
            var maxResults = int.TryParse(maxResultsStr, out var parsed) ? parsed : 5;

            if (string.IsNullOrWhiteSpace(query))
            {
                onError("No search query provided", null);
                return;
            }

            var allUrls = new List<string>();
            var searchEngines = new[] { "google", "bing", "duckduckgo" };

            // Search each engine
            for (int i = 0; i < searchEngines.Length; i++)
            {
                var engine = searchEngines[i];
                var progressPercent = (int)((i / (double)searchEngines.Length) * 60);
                onProgress(10 + progressPercent, $"Searching {engine}...");

                try
                {
                    var urls = await SearchEngineAsync(engine, query, onError);
                    allUrls.AddRange(urls);
                }
                catch (Exception ex)
                {
                    // Log but continue with other engines
                    onProgress(10 + progressPercent, $"{engine} search failed: {ex.Message}");
                }
            }

            onProgress(70, "Filtering and deduplicating URLs...");

            // Filter out sponsored links, duplicates, and invalid URLs
            var filteredUrls = FilterUrls(allUrls);

            // Limit to max results
            var finalUrls = filteredUrls.Take(maxResults).ToList();

            if (finalUrls.Count == 0)
            {
                onError("No relevant URLs found from search engines", null);
                return;
            }

            onProgress(80, $"Found {finalUrls.Count} relevant URLs");

            // Store URLs in data dictionary
            data["research_urls"] = string.Join("|", finalUrls);
            data["research_url_count"] = finalUrls.Count.ToString();

            // Add web-scrape steps for each URL
            if (plan != null && currentStepIndex >= 0)
            {
                onProgress(90, "Adding web-scrape steps to plan...");

                // Insert steps in reverse order so first URL ends up first in the list
                for (int i = finalUrls.Count - 1; i >= 0; i--)
                {
                    var url = finalUrls[i];
                    PlanUtility.AddStepToPlan(plan, currentStepIndex, "web-scrape", new Dictionary<string, object>
                    {
                        { "url", url }
                    });
                }
            }

            onProgress(100, $"Research complete. Added {finalUrls.Count} URLs to scrape.");

            onComplete($"Gathered {finalUrls.Count} URLs from search engines");
        }
        catch (Exception ex)
        {
            onError($"Failed to gather research: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Search a specific engine and return URLs
    /// </summary>
    private async Task<List<string>> SearchEngineAsync(string engine, string query, OnError onError)
    {
        var urls = new List<string>();
        var searchUrl = BuildSearchUrl(engine, query);

        if (string.IsNullOrEmpty(searchUrl))
        {
            return urls;
        }

        try
        {
            // Download search results page
            string resolvedUrl;
            var domJson = await Task.Run(() => Charlotte.Download(searchUrl, out resolvedUrl));

            if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("file:") || domJson.StartsWith("Error"))
            {
                return urls;
            }

            // Parse and extract URLs
            var analyzedArticle = Html.DeserializeArticle(domJson);
            Html.GetArticleInfoFromDOM(analyzedArticle);

            // Extract URLs from the page
            urls = ExtractUrlsFromSearchResults(analyzedArticle, engine);
        }
        catch (Exception ex)
        {
            onError($"Error searching {engine}: {ex.Message}", ex);
        }

        return urls;
    }

    /// <summary>
    /// Build search URL for the given engine
    /// </summary>
    private string BuildSearchUrl(string engine, string query)
    {
        var encodedQuery = HttpUtility.UrlEncode(query);

        return engine.ToLower() switch
        {
            "google" => $"https://www.google.com/search?q={encodedQuery}&num=20",
            "bing" => $"https://www.bing.com/search?q={encodedQuery}&count=20",
            "duckduckgo" => $"https://duckduckgo.com/html/?q={encodedQuery}",
            _ => ""
        };
    }

    /// <summary>
    /// Extract URLs from search result page based on engine
    /// </summary>
    private List<string> ExtractUrlsFromSearchResults(Collector.Common.Models.Articles.AnalyzedArticle article, string engine)
    {
        var urls = new List<string>();

        if (article?.elements == null)
        {
            return urls;
        }

        foreach (var element in article.elements)
        {
            if (element?.attribute == null)
            {
                continue;
            }

            // Look for href attributes
            if (element.attribute.TryGetValue("href", out var href))
            {
                var url = href?.ToString() ?? "";

                // Filter based on engine-specific patterns
                if (IsValidSearchResultUrl(url, engine))
                {
                    // Clean and normalize URL
                    url = CleanUrl(url);
                    if (!string.IsNullOrEmpty(url) && !urls.Contains(url))
                    {
                        urls.Add(url);
                    }
                }
            }
        }

        return urls;
    }

    /// <summary>
    /// Check if URL is a valid search result (not sponsored, not a navigation link, etc.)
    /// </summary>
    private bool IsValidSearchResultUrl(string url, string engine)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Skip common non-result URLs
        var invalidPatterns = new[]
        {
            "/search?",
            "/images?",
            "/maps?",
            "/news?",
            "/videos?",
            "/shopping?",
            "/books?",
            "/flights?",
            "/finance?",
            "/advanced_search?",
            "/preferences?",
            "/support?",
            "/tools?",
            "/about?",
            "/help?",
            "javascript:",
            "#",
            "/ads?",
            "/aclk?",
            "/url?sa=",
            "sponsored",
            "adurl=",
            "/sh/aclk",
            "/prbadclk",
            "/shopping/"
        };

        foreach (var pattern in invalidPatterns)
        {
            if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Skip major search engine domains
        var searchDomains = new[]
        {
            "google.com",
            "google.co.",
            "bing.com",
            "duckduckgo.com",
            "yahoo.com",
            "baidu.com",
            "yandex.com",
            "wikipedia.org",
            "facebook.com",
            "twitter.com",
            "instagram.com",
            "youtube.com",
            "tiktok.com",
            "pinterest.com",
            "reddit.com"
        };

        foreach (var domain in searchDomains)
        {
            if (url.Contains(domain, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Must be HTTP/HTTPS
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Clean and normalize URL
    /// </summary>
    private string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "";
        }

        // Remove tracking parameters
        url = Regex.Replace(url, @"[?&](utm_|ref_|source|medium|campaign|gclid|fbclid|msclkid)=[^&]*", "");

        // Remove empty query strings
        if (url.EndsWith("?"))
        {
            url = url.TrimEnd('?');
        }

        return url;
    }

    /// <summary>
    /// Filter and deduplicate URLs
    /// </summary>
    private List<string> FilterUrls(List<string> urls)
    {
        var seenDomains = new HashSet<string>();
        var filtered = new List<string>();

        foreach (var url in urls)
        {
            try
            {
                var uri = new Uri(url);
                var domain = uri.Host.ToLowerInvariant();

                // Skip if we've already seen this domain
                if (seenDomains.Contains(domain))
                {
                    continue;
                }

                seenDomains.Add(domain);
                filtered.Add(url);
            }
            catch
            {
                // Skip invalid URLs
            }
        }

        return filtered;
    }
}
