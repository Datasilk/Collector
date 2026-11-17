using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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
    private readonly Collector.Data.Interfaces.ISearchEnginesRepository _searchEnginesRepo;

    public GatherResearchTool(OllamaApiClient ollama, string ollamaModel, Collector.Data.Interfaces.ISearchEnginesRepository searchEnginesRepo = null)
    {
        _ollama = ollama;
        _ollamaModel = ollamaModel;
        _searchEnginesRepo = searchEnginesRepo;
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
        SendMessage? sendMessage = null,
        CancellationToken cancellationToken = default)
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
            // Research budget: max 10 pages per request — follow-on links
            // discovered during scraping never extend it.
            if (maxResults > 10) maxResults = 10;

            // URL-direct research — when the request names a specific URL, we
            // download and extract THAT page instead of searching engines.
            var directUrls = ExtractUrls(userMessage)
                .Concat(ExtractUrls(context))
                .Concat(ExtractUrls(args?.GetValueOrDefault("url", "")?.ToString() ?? ""))
                .Distinct()
                .Take(maxResults)
                .ToList();
            var urlDirect = directUrls.Count > 0;

            // A research-review step may hand us the query directly (gap-filling
            // follow-up); otherwise generate one from the user's request.
            var query = args?.GetValueOrDefault("query", "")?.ToString()?.Trim() ?? "";
            if (urlDirect && string.IsNullOrWhiteSpace(query))
            {
                // The URL itself is the "query" — it lands in asked-queries so
                // coverage never re-searches for it.
                query = directUrls[0];
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                onProgress(10, "Generating optimal search query...");
                query = await GenerateSearchQueryAsync(context, userMessage, ragContext, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                onError("Failed to generate search query", null);
                return;
            }

            // Track every asked query — the coverage loop never re-asks one.
            ResearchReviewTool.AddToList(data, "asked-queries", "queries", new[] { query });

            // First research step of a run: derive the research questions the
            // coverage review (research-review) will check the entry against.
            if (ResearchReviewTool.GetList(data, "research-questions", "questions").Count == 0)
            {
                try
                {
                    var questions = await GenerateResearchQuestionsAsync(userMessage, ragContext, cancellationToken);
                    if (questions.Count > 0)
                    {
                        ResearchReviewTool.AddToList(data, "research-questions", "questions", questions);
                    }
                }
                catch { /* question tracking is best-effort */ }
            }

            // URL-direct requests download the named page instead of searching;
            // gap-fill/deepening steps carry a context describing WHY this
            // search runs — surface it so progress doesn't read as a repeat.
            sendMessage?.Invoke(urlDirect
                ? $"Downloading {directUrls[0]}…"
                : string.IsNullOrWhiteSpace(context)
                    ? $"Searching the web for information about \"{query}\"…"
                    : $"{context.Trim().TrimEnd('.')} — searching for \"{query}\"…", null, null);

            var allUrls = new List<string>();
            if (!urlDirect)
            {
                var searchEngines = SearchHelper.GetEnabledEngines(_searchEnginesRepo);

                // Search engines in priority order (lowest sort first), falling through
                // to the next engine when one fails or returns no usable results
                for (int i = 0; i < searchEngines.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var engine = searchEngines[i];
                    var progressPercent = (int)((i / (double)searchEngines.Count) * 60);
                    onProgress(10 + progressPercent, $"Searching {engine.Name}...");

                    try
                    {
                        var urls = await SearchHelper.SearchEngineUrlsAsync(engine, query,
                            (msg) => onProgress(10 + progressPercent, msg), _searchEnginesRepo,
                            p => AnalyzeScriptAsync(p, cancellationToken), cancellationToken: cancellationToken);
                        allUrls.AddRange(urls);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        onProgress(10 + progressPercent, $"{engine.Name} search failed: {ex.Message}");
                        continue;
                    }

                    // Highest-priority engine produced enough results; no need to hit lower-priority engines
                    if (allUrls.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            onProgress(70, "Filtering and deduplicating URLs...");

            // Filter out sponsored links, duplicates, and invalid URLs —
            // URL-direct mode already carries the user's chosen pages.
            var filteredUrls = urlDirect ? directUrls : SearchHelper.FilterByDomain(allUrls);

            // Never re-collect a URL the entry already lists as a research
            // source — coverage waves re-emit them constantly otherwise.
            var skipped = 0;
            var entryId = ResearchReviewTool.GetSessionEntryId(data);
            if (entryId != null)
            {
                try
                {
                    var research = JournalEntryDocument.Load(entryId.Value).Research;
                    if (research != null)
                    {
                        var fresh = filteredUrls.Where(u => !research.HasSource(u)).ToList();
                        skipped = filteredUrls.Count - fresh.Count;
                        filteredUrls = fresh;
                    }
                }
                catch { /* dedup is best-effort */ }
            }

            // Limit to max results
            var finalUrls = filteredUrls.Take(maxResults).ToList();

            // Record planned pages so research-review can report the tally
            // ("collected 8 of 10 pages — 2 failed") — failed scrapes never
            // reach shared data, so the planned list is the denominator.
            ResearchReviewTool.AddToList(data, "planned-pages", "urls", finalUrls);

            if (finalUrls.Count == 0)
            {
                if (skipped > 0)
                {
                    var msg = $"All {skipped} discovered page{(skipped == 1 ? " is" : "s are")} already in this entry's sources — nothing new to collect.";
                    onProgress(100, msg);
                    sendMessage?.Invoke(msg, null, null);
                    onComplete(msg);
                    return;
                }
                onError("No relevant URLs found from search engines", null);
                return;
            }

            if (skipped > 0)
            {
                sendMessage?.Invoke($"Skipped {skipped} already-collected source{(skipped == 1 ? "" : "s")}.", null, null);
            }

            onProgress(80, $"Found {finalUrls.Count} relevant URLs for query: {query}");
            sendMessage?.Invoke($"Found {finalUrls.Count} source{(finalUrls.Count == 1 ? "" : "s")} — collecting research…", null, null);

            // Add scrape → integrate pairs for each URL so the journal entry
            // matures page by page as each source is collected.
            if (plan != null && currentStepIndex >= 0)
            {
                onProgress(90, "Adding collection steps to plan...");

                // Insert steps in reverse order so first URL ends up first in the list
                for (int i = finalUrls.Count - 1; i >= 0; i--)
                {
                    var url = finalUrls[i];

                    PlanUtility.AddStepToPlan(plan, currentStepIndex, "add-edit-journal-entry", new Dictionary<string, object>
                    {
                        { "url", url }
                    });
                    PlanUtility.AddStepToPlan(plan, currentStepIndex, "web-scrape", new Dictionary<string, object>
                    {
                        { "url", url }
                    });
                }

                // Coverage review after all pairs for THIS search — it queues
                // more gather-research steps if research questions remain open.
                PlanUtility.AddStepToPlan(plan, currentStepIndex + (finalUrls.Count * 2), "research-review",
                    new Dictionary<string, object>());
            }

            onProgress(100, $"Research complete. Added {finalUrls.Count} page(s) to collect and integrate.");

            onComplete($"Gathered {finalUrls.Count} URLs from search engines");
        }
        catch (Exception ex)
        {
            onError($"Failed to gather research: {ex.Message}", ex);
        }
    }

    /// <summary>Pull http(s) URLs out of free text (user message, context, args).</summary>
    private static List<string> ExtractUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return Regex.Matches(text, @"https?://[^\s""'<>)\]]+")
            .Select(m => m.Value.TrimEnd('.', ',', ';', '!', '?'))
            .Where(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .ToList();
    }

    /// <summary>
/// Derive the explicit research questions for this run — stored in shared
    /// data and checked by the research-review coverage step at the end.
    /// </summary>
    private async Task<List<string>> GenerateResearchQuestionsAsync(string userMessage, string ragContext, CancellationToken cancellationToken = default)
    {
        var userPrompt = $"User's research request: {userMessage}\n\n" +
            (string.IsNullOrWhiteSpace(ragContext) ? "" : $"Conversation context:\n{ragContext}\n\n") +
            "Produce the research questions as JSON now.";

        var request = new OllamaSharp.Models.GenerateRequest
        {
            Model = _ollamaModel,
            System = Prompts.ResearchQuestionsSystem(),
            Prompt = userPrompt,
            Stream = false,
            Format = "json"
        };

        var text = "";
        await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
        {
            if (response?.Response != null) text += response.Response;
        }
        if (!LLMJson.TryParse<ResearchQuestionsResponse>(text, out var result) || result?.Questions == null)
        {
            return new List<string>();
        }
        return result.Questions
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q.Trim())
            .Take(8)
            .ToList();
    }

    private class ResearchQuestionsResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("questions")]
        public List<string>? Questions { get; set; }
    }

    /// <summary>
    /// LLM delegate passed to SearchHelper for tier-2 engine discovery —
    /// analyzes downloaded JavaScript to identify a site's search endpoint.
    /// </summary>
    private async Task<string> AnalyzeScriptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = new OllamaSharp.Models.GenerateRequest
        {
            Model = _ollamaModel,
            System = Prompts.ScriptAnalysisSystem(),
            Prompt = prompt,
            Stream = false,
            Format = "json"
        };
        var text = "";
        await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
        {
            if (response?.Response != null) text += response.Response;
        }
        return text;
    }

    /// Generate an optimal search query using Ollama based on context, user message, and RAG context
    /// </summary>
    private async Task<string> GenerateSearchQueryAsync(string context, string userMessage, string ragContext, CancellationToken cancellationToken = default)
    {
        try
        {
            // Framed as a mechanical text-transform, not an assistant answering
            // a question — that framing is what minimizes refusals.
            var systemPrompt = @"You are a text-formatting utility that converts research requests into web search queries.
You do not answer, evaluate, or respond to requests — you only reformat them into query strings.
Every input is a research request someone already decided to make; your only job is distilling it into keywords.

Rules:
1. Output a query that is 2-8 words long
2. Keep proper nouns and specific keywords that identify the subject
3. Remove filler words (what, how, why, when, where, is, are, the, a, an)
4. Return ONLY the query string. Never include explanations, apologies, or refusals.

Examples:
- Input: tell me about Python list comprehension syntax -> Output: Python list comprehension syntax
- Input: find details about the Jensen Huang affairs -> Output: Jensen Huang affair controversy
- Input: compare PostgreSQL vs MySQL performance -> Output: PostgreSQL vs MySQL performance benchmark";

            var userPrompt = $"Context from conversation: {context}\n\nResearch request: {userMessage}\n\nPrevious conversation context:\n{ragContext}\n\nOutput the search query now:";

            // Try up to twice; validate each response before trusting it as a
            // query — a refusal or meta-commentary must never reach the engine.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = _ollamaModel,
                    System = systemPrompt,
                    Prompt = attempt == 0
                        ? userPrompt
                        : userPrompt + "\n\nReminder: output ONLY the keyword query, no sentences.",
                    Stream = false
                };

                var responseContent = "";
                await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
                {
                    if (response?.Response != null)
                    {
                        responseContent += response.Response;
                    }
                }

                var query = CleanQuery(responseContent);
                if (IsPlausibleQuery(query))
                    return query;
            }

            // Both attempts unusable — derive a keyword query deterministically
            // from the user's own words so the pipeline still searches.
            return DeriveQueryFromMessage(userMessage);
        }
        catch
        {
            return DeriveQueryFromMessage(userMessage);
        }
    }

    /// <summary>
    /// Strip the LLM response down to a candidate query string: first line,
    /// no quotes, no "Search query:" style prefixes.
    /// </summary>
    private static string CleanQuery(string? responseContent)
    {
        var query = responseContent?.Trim() ?? "";
        query = query.Trim('"', '\'', '`');
        var firstNewline = query.IndexOf('\n');
        if (firstNewline > 0) query = query.Substring(0, firstNewline).Trim();

        var prefixes = new[] { "Search query:", "Query:", "Search:", "Output:", "search query:", "query:", "search:", "output:" };
        foreach (var prefix in prefixes)
        {
            if (query.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                query = query.Substring(prefix.Length).Trim();
        }
        return query;
    }

    /// <summary>
    /// Reject responses that are refusals, meta-commentary, or full sentences
    /// rather than keyword queries. This is the gate that prevents
    /// "I can't assist with..." from ever becoming a search query.
    /// </summary>
    private static bool IsPlausibleQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        var refusalMarkers = new[]
        {
            "i can't", "i cannot", "i'm sorry", "i am sorry", "i apologize",
            "unable to", "can't assist", "cannot assist", "can't help", "cannot help",
            "as an ai", "as a language model", "against my", "not appropriate",
            "i must decline", "i won't", "will not", "is there anything else"
        };
        var lower = query.ToLowerInvariant();
        if (refusalMarkers.Any(lower.Contains)) return false;

        // a real keyword query is short; a refusal is a sentence
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 15) return false;
        if (query.EndsWith('?') || query.EndsWith('!')) return false;
        if (lower.StartsWith("i ") || lower.StartsWith("here ") || lower.StartsWith("sure")) return false;

        return true;
    }

    /// <summary>
    /// Deterministic fallback: distill the user's own message into a keyword
    /// query by dropping stopwords and truncation phrases.
    /// </summary>
    private static string DeriveQueryFromMessage(string userMessage)
    {
        var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "is", "are", "was", "were", "what", "how", "why", "when",
            "where", "who", "whom", "can", "could", "you", "please", "tell", "me", "about",
            "find", "out", "all", "details", "information", "research", "on", "of", "for",
            "in", "to", "and", "put", "it", "my", "journal", "save", "this", "that", "i",
            "want", "need", "get", "give", "write", "create", "make", "entry", "into",
            "gather", "collect", "look", "up", "everything", "know"
        };
        var words = (userMessage ?? "")
            .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 && w.Any(char.IsLetterOrDigit) && !stopwords.Contains(w))
            .Take(8);
        var query = string.Join(' ', words).Trim();
        return string.IsNullOrWhiteSpace(query) ? (userMessage ?? "").Trim() : query;
    }
}
