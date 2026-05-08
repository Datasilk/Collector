using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Collector.Chat.Models;
using Collector.Common;
using OllamaSharp;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for gathering research by searching major search engines and collecting relevant URLs
/// </summary>
public class GatherResearchTool : IChatTool
{
    private readonly OllamaApiClient _ollama;
    private readonly string _ollamaModel;

    public GatherResearchTool(OllamaApiClient ollama, string ollamaModel)
    {
        _ollama = ollama;
        _ollamaModel = ollamaModel;
    }

    public string ToolKey => "gather-research";

    public string Description => "Search major search engines to find relevant URLs for research. Automatically adds web-scrape steps to the plan for each discovered URL.";

    public bool AvailableToOllama => true;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "context",
            DataType = "string",
            Description = "a brief description of the context in which we will formulate our search query from"
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
        List<Dictionary<string, List<string>>> data,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        Guid appUserId,
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

            // Get parameters from plan args
            var args = plan?.Steps.Count > currentStepIndex && currentStepIndex >= 0
                ? plan.Steps[currentStepIndex].Args
                : new Dictionary<string, object>();

            var context = args?.GetValueOrDefault("context", "")?.ToString() ?? "";
            var maxResultsStr = args?.GetValueOrDefault("maxResults", "5")?.ToString() ?? "5";
            var maxResults = int.TryParse(maxResultsStr, out var parsed) ? parsed : 5;

            // Generate search query using Ollama based on context, user message, and RAG context
            onProgress(10, "Generating optimal search query...");
            var query = await GenerateSearchQueryAsync(context, userMessage, ragContext);
            
            if (string.IsNullOrWhiteSpace(query))
            {
                onError("Failed to generate search query", null);
                return;
            }

            var allUrls = new List<string>();
            var searchEngines = new[] { "google"};//, "bing", "duckduckgo" };

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

            onProgress(80, $"Found {finalUrls.Count} relevant URLs for query: {query}");

            // Add web-scrape steps for each URL
            if (plan != null && currentStepIndex >= 0)
            {
                onProgress(90, "Adding web-scrape steps to plan...");

                // Track the index where we start adding web-scrape steps
                int lastWebScrapeIndex = currentStepIndex;

                // Insert steps in reverse order so first URL ends up first in the list
                for (int i = finalUrls.Count - 1; i >= 0; i--)
                {
                    var url = finalUrls[i];
                    PlanUtility.AddStepToPlan(plan, currentStepIndex, "web-scrape", new Dictionary<string, object>
                    {
                        { "url", url }
                    });
                    lastWebScrapeIndex = currentStepIndex + 1; // Points to the step we just added
                }

                // Add clean-web-content step after all web-scrape steps
                // Insert at the position after the last web-scrape step
                int cleanContentIndex = lastWebScrapeIndex + (finalUrls.Count - 1);
                PlanUtility.AddStepToPlan(plan, cleanContentIndex, "clean-web-content", new Dictionary<string, object>
                {
                    { "userMessage", userMessage }
                });

                // Add add-edit-journal-entry step after clean-web-content
                int addEditIndex = cleanContentIndex + 1;
                PlanUtility.AddStepToPlan(plan, addEditIndex, "add-edit-journal-entry", new Dictionary<string, object>
                {
                    { "context", "Create journal entry from cleaned web content" }
                });
            }

            onProgress(100, $"Research complete. Added {finalUrls.Count} URLs to scrape, plus content cleaning and journal entry steps.");

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
            var domJson = Charlotte.Download(searchUrl, out resolvedUrl);

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
            "facebook.com",
            "twitter.com",
            "instagram.com",
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

    /// <summary>
    /// Generate an optimal search query using Ollama based on context, user message, and RAG context
    /// </summary>
    private async Task<string> GenerateSearchQueryAsync(string context, string userMessage, string ragContext)
    {
        try
        {
            var systemPrompt = @"You are a search query optimization expert. Your task is to generate a concise, effective search query that will find accurate and relevant information for the user.

Rules for generating the search query:
1. Create a query that is 2-8 words long
2. Focus on the most specific, unique keywords that will yield accurate results
3. Remove filler words (what, how, why, when, where, is, are, the, a, an)
4. Use technical terms and proper nouns when relevant
5. The query should directly address what the user is looking for
6. Return ONLY the search query string, nothing else

Examples:
- User wants to know about Python list comprehension syntax -> Query: Python list comprehension syntax
- User wants to compare PostgreSQL vs MySQL performance -> Query: PostgreSQL vs MySQL performance benchmark
- User needs information about React hooks best practices -> Query: React hooks best practices 2024";

            var userPrompt = $"Context from conversation: {context}\n\nUser's current question: {userMessage}\n\nPrevious conversation context:\n{ragContext}\n\nBased on all the above information, generate a concise, effective search query (2-8 words) that will find accurate information for the user. Return ONLY the search query:";

            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = _ollamaModel,
                System = systemPrompt,
                Prompt = userPrompt,
                Stream = false
            };

            var responseContent = "";
            await foreach (var response in _ollama.GenerateAsync(request))
            {
                if (response?.Response != null)
                {
                    responseContent += response.Response;
                }
            }

            // Clean up the response - take only the first line, remove quotes, trim whitespace
            var query = responseContent?.Trim() ?? "";
            
            // Remove quotes if present
            query = query.Trim('"', '\'', '`');
            
            // Take only the first line if multiple lines
            var firstNewline = query.IndexOf('\n');
            if (firstNewline > 0)
            {
                query = query.Substring(0, firstNewline).Trim();
            }
            
            // Remove common prefixes that Ollama might add
            var prefixes = new[] { "Search query:", "Query:", "Search:", "search query:", "query:", "search:" };
            foreach (var prefix in prefixes)
            {
                if (query.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(prefix.Length).Trim();
                }
            }

            return query;
        }
        catch (Exception ex)
        {
            // Fallback to using user message directly
            return userMessage;
        }
    }
}
