using Collector.Chat.Helpers;
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
        ChatDetailLog.Fn("gather-research", "Run");
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
            // Follow-up steps queued by coverage review / deepening carry an
            // explicit query — they must SEARCH, not re-download the URL that
            // happens to live in the original user message (which never
            // changes between steps and would dead-end every follow-up on
            // "already in sources").
            var query = args?.GetValueOrDefault("query", "")?.ToString()?.Trim() ?? "";
            var directUrls = ExtractUrls(args?.GetValueOrDefault("url", "")?.ToString() ?? "");
            if (directUrls.Count == 0 && string.IsNullOrWhiteSpace(query))
            {
                directUrls = ExtractUrls(userMessage)
                    .Concat(ExtractUrls(context))
                    .Distinct()
                    .ToList();
            }
            directUrls = directUrls.Take(maxResults).ToList();
            var urlDirect = directUrls.Count > 0;

            // Remember user-named pages — their research questions are scoped
            // to the page itself, so if every one is skipped/failed the
            // coverage review has nothing left to chase.
            if (urlDirect)
                ResearchReviewTool.AddToList(data, "url-direct", "urls", directUrls);
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

            // Follow-up queries arrive as full natural-language questions —
            // engines lock onto individual words ("primary", "have") and return
            // dictionary/e-commerce junk. Record the original question for
            // coverage dedupe, but search with distilled keywords.
            var askedQuery = query;
            var searchQuery = urlDirect ? query : DistillToKeywords(query);

            if (string.IsNullOrWhiteSpace(query))
            {
                onError("Failed to generate search query", null);
                return;
            }

            // Track every asked query — the coverage loop never re-asks one.
            // Both the raw question and the distilled form are recorded so a
            // rephrased repeat still trips the dedupe.
            ResearchReviewTool.AddToList(data, "asked-queries", "queries",
                string.Equals(askedQuery, searchQuery, StringComparison.OrdinalIgnoreCase)
                    ? new[] { askedQuery }
                    : new[] { askedQuery, searchQuery });

            // First research step of a run: derive the research questions the
            // coverage review (research-review) will check the entry against.
            if (ResearchReviewTool.GetList(data, "research-questions", "questions").Count == 0)
            {
                try
                {
                    var questions = await GenerateResearchQuestionsAsync(userMessage, ragContext, urlDirect, cancellationToken);
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
                    ? $"Searching the web for information about \"{searchQuery}\"…"
                    : $"{context.Trim().TrimEnd('.')} — searching for \"{searchQuery}\"…", null, null);

            var allLinks = new List<SearchHelper.ResultLink>();
            var searchIndex = -1;
            if (!urlDirect)
            {
                // Record the engine call in run-shared data — sources gathered
                // from its results get tagged with this query's index.
                searchIndex = ResearchReviewTool.GetList(data, "search-queries", "queries").Count;
                ResearchReviewTool.AddToList(data, "search-queries", "queries",
                    new[] { System.Net.WebUtility.UrlEncode(searchQuery) });

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
                        var (urls, article) = await SearchHelper.SearchEnginePageAsync(engine, searchQuery,
                            (msg) => onProgress(10 + progressPercent, msg), _searchEnginesRepo,
                            p => AnalyzeScriptAsync(p, cancellationToken), cancellationToken: cancellationToken);
                        // Re-extract with anchor titles when we have the parsed
                        // SERP — titles drive the relevance filter below.
                        if (article?.elements != null)
                            allLinks.AddRange(SearchHelper.ExtractResultLinks(article, engine));
                        else
                            allLinks.AddRange(urls.Select(u => new SearchHelper.ResultLink { Url = u }));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        onProgress(10 + progressPercent, $"{engine.Name} search failed: {ex.Message}");
                        continue;
                    }

                    // Highest-priority engine produced enough results; no need to hit lower-priority engines
                    if (allLinks.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            onProgress(70, "Filtering and deduplicating URLs...");

            // Filter out sponsored links, duplicates, and invalid URLs —
            // URL-direct mode already carries the user's chosen pages.
            var dedupedLinks = DedupeLinksByDomain(allLinks);

            // Relevance filter — judge result TITLES against the research
            // request before spending scrape attempts on SERP noise
            // (shopping listings, dictionary hits, unrelated topics).
            var filteredUrls = urlDirect
                ? directUrls
                : (await FilterLinksByRelevanceAsync(dedupedLinks, searchQuery, userMessage, onProgress, cancellationToken))
                    .Select(l => l.Url).ToList();

            // Direct image/media asset URLs aren't research pages — scraping
            // a .jpg yields nothing and pollutes the Sources module.
            filteredUrls = filteredUrls.Where(u => !IsMediaAssetUrl(u)).ToList();

            // Never re-collect a URL the entry already lists as a research
            // source — coverage waves re-emit them constantly otherwise.
            var skipped = 0;
            var entryId = ResearchReviewTool.GetSessionEntryId(data);
            if (entryId != null)
            {
                try
                {
                    var research = JournalEntryDocument.Load(entryId.Value, ResearchReviewTool.GetSessionJournalId(data)).Research;
                    if (research != null)
                    {
                        var fresh = filteredUrls.Where(u => !research.HasSource(u)).ToList();
                        skipped = filteredUrls.Count - fresh.Count;
                        filteredUrls = fresh;
                    }
                }
                catch { /* dedup is best-effort */ }
            }

            // Never re-attempt a URL already planned this run — failed scrapes
            // never become sources, so without this the same dead URLs get
            // re-queued by every coverage wave.
            var alreadyPlanned = ResearchReviewTool.GetList(data, "planned-pages", "urls")
                .Select(u => u.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (alreadyPlanned.Count > 0)
            {
                var fresh = filteredUrls.Where(u => !alreadyPlanned.Contains(u)).ToList();
                skipped += filteredUrls.Count - fresh.Count;
                filteredUrls = fresh;
            }

            // Scrape failures leave a trail: never re-plan a URL that already
            // failed, and stop feeding hosts that walled us 2+ times this run —
            // otherwise every coverage wave re-queues the same walled domains.
            var failedPages = ResearchReviewTool.GetList(data, "failed-pages", "urls")
                .Select(u => u.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var failedHosts = ResearchReviewTool.GetList(data, "failed-hosts", "hosts")
                .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (failedPages.Count > 0 || failedHosts.Count > 0)
            {
                var fresh = filteredUrls.Where(u =>
                    !failedPages.Contains(u)
                    && !(Uri.TryCreate(u, UriKind.Absolute, out var fu) && failedHosts.Contains(fu.Host)))
                    .ToList();
                skipped += filteredUrls.Count - fresh.Count;
                filteredUrls = fresh;
            }

            // Limit to max results
            var finalUrls = filteredUrls.Take(maxResults).ToList();

            // Record planned pages so research-review can report the tally
            // ("collected 8 of 10 pages — 2 failed") — failed scrapes never
            // reach shared data, so the planned list is the denominator.
            ResearchReviewTool.AddToList(data, "planned-pages", "urls", finalUrls);

            // Provenance: remember which search query produced each URL so the
            // integrated source can carry searchIndex back to research.search.
            if (searchIndex >= 0)
                ResearchReviewTool.AddToList(data, "source-search", "pairs",
                    finalUrls.Select(u => $"{searchIndex}|{u}"));

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

            onProgress(80, $"Found {finalUrls.Count} relevant URLs for query: {searchQuery}");
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
    private async Task<List<string>> GenerateResearchQuestionsAsync(string userMessage, string ragContext, bool urlDirect, CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("gather-research", "GenerateResearchQuestionsAsync");
        var userPrompt = $"User's research request: {userMessage}\n\n" +
            (string.IsNullOrWhiteSpace(ragContext) ? "" : $"Conversation context:\n{ragContext}\n\n") +
            (urlDirect
                ? "The research is limited to the specific URL(s) named above — every question must be answerable from that page's own content.\n\n"
                : "") +
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
        ChatDetailLog.Fn("gather-research", "AnalyzeScriptAsync");
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
        ChatDetailLog.Fn("gather-research", "GenerateSearchQueryAsync");
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

    /// <summary>Domain dedupe on result links — first link per host wins.</summary>
    private static List<SearchHelper.ResultLink> DedupeLinksByDomain(List<SearchHelper.ResultLink> links)
    {
        ChatDetailLog.Fn("gather-research", "DedupeLinksByDomain");
        var seen = new HashSet<string>();
        var kept = new List<SearchHelper.ResultLink>();
        foreach (var link in links)
        {
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var uri)) continue;
            if (seen.Add(uri.Host.ToLowerInvariant())) kept.Add(link);
        }
        return kept;
    }

    /// <summary>
    /// SERP-level relevance check: hand the result titles + URLs to the local
    /// model and keep only the ones it judges on-topic for the research
    /// request. Fails open — when the model can't decide, all links pass.
    /// </summary>
    private async Task<List<SearchHelper.ResultLink>> FilterLinksByRelevanceAsync(
        List<SearchHelper.ResultLink> links,
        string searchQuery,
        string userMessage,
        OnProgress onProgress,
        CancellationToken cancellationToken)
    {
        ChatDetailLog.Fn("gather-research", "FilterLinksByRelevanceAsync");
        // Deterministic junk filter — dictionary/thesaurus/definition pages
        // pass the LLM check too often (literal word overlap) and never
        // contain research content.
        var kept = links.Where(l => !IsJunkResult(l)).ToList();
        if (kept.Count < links.Count)
        {
            ChatDetailLog.Links(links.Select(l => (l.Title ?? "", l.Url ?? "", kept.Contains(l))));
            links = kept.Count > 0 ? kept : links;
        }
        if (links.Count <= 1)
        {
            ChatDetailLog.Links(links.Select(l => (l.Title ?? "", l.Url ?? "", true)));
            return links;
        }
        try
        {
            var numbered = links.Select((l, i) =>
                $"{i + 1}. {(string.IsNullOrWhiteSpace(l.Title) ? "(no title)" : l.Title)} — {l.Url}");
            var system = @"You are a search-result filter for a research pipeline. Given a research request and a numbered list of search results (title — URL), decide which results will contain substantive, on-topic content.
Keep: encyclopedia/reference articles, news coverage, educational or scholarly pages about the subject.
Reject: shopping/product listings, dictionary/definition entries, image or video galleries, login or forum pages, and pages about a different subject that happens to share a word with the query.
Respond with ONLY valid JSON in this exact format:
{""relevant"":[1,2,3]}";
            var user = $"RESEARCH REQUEST: {userMessage}\n\nSEARCH QUERY: {searchQuery}\n\nRESULTS:\n{string.Join("\n", numbered)}\n\nWhich result numbers are relevant?";

            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = _ollamaModel,
                System = system,
                Prompt = user,
                Stream = false,
                Format = "json"
            };
            var text = "";
            await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
            {
                if (response?.Response != null) text += response.Response;
            }

            if (LLMJson.TryParse<ResultFilterResponse>(text, out var r) && r?.Relevant != null)
            {
                var keep = r.Relevant.Where(n => n >= 1 && n <= links.Count).ToHashSet();
                if (keep.Count == 0)
                {
                    ChatDetailLog.Links(links.Select(l => (l.Title ?? "", l.Url ?? "", true)));
                    return links; // empty verdicts aren't trustworthy — keep all
                }
                ChatDetailLog.Links(links.Select((l, i) => (l.Title ?? "", l.Url ?? "", keep.Contains(i + 1))));
                var filtered = links.Where((l, i) => keep.Contains(i + 1)).ToList();
                var dropped = links.Count - filtered.Count;
                if (dropped > 0)
                    onProgress(72, $"Relevance filter: kept {filtered.Count} of {links.Count} results");
                return filtered;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* filter is best-effort */ }
        ChatDetailLog.Links(links.Select(l => (l.Title ?? "", l.Url ?? "", true)));
        return links;
    }

    private class ResultFilterResponse
    {
        [JsonPropertyName("relevant")] public List<int>? Relevant { get; set; }
    }

    /// <summary>Reference sites that only define words — never useful as
    /// research sources. Match on host plus dictionary-ish paths.</summary>
    private static bool IsJunkResult(SearchHelper.ResultLink link)
    {
        var url = link.Url ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
        var host = u.Host.ToLowerInvariant();
        if (JunkHosts.Any(h => host == h || host.EndsWith("." + h))) return true;
        var path = u.AbsolutePath.ToLowerInvariant();
        return JunkPathMarkers.Any(m => path.Contains(m));
    }

    private static readonly string[] JunkHosts =
    {
        "dictionary.com", "merriam-webster.com", "thesaurus.com",
        "thefreedictionary.com", "collinsdictionary.com", "urbandictionary.com",
        "vocabulary.com", "wordreference.com", "synonyms.com",
        "powerthesaurus.org", "wiktionary.org", "definitions.net",
        "yourdictionary.com", "wordnik.com"
    };

    private static readonly string[] JunkPathMarkers =
    {
        "/dictionary/", "/thesaurus/", "/definition/", "/definitions/",
        "/define/", "/synonyms/", "/meaning/"
    };

    /// <summary>Direct media/asset URLs (images, video, css, fonts) — scraping
    /// one returns no article, so never queue them as research pages.</summary>
    private static bool IsMediaAssetUrl(string url)
    {
        var clean = url.Split('?', '#')[0];
        return System.Text.RegularExpressions.Regex.IsMatch(clean,
            @"\.(jpe?g|png|gif|webp|svg|bmp|ico|mp[34]|webm|mov|css|js|woff2?)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Reduce a query string to keyword form. Already-terse queries pass
    /// through unchanged; natural-language questions (which engines mishandle
    /// by fixating on single words) get stopword-stripped.
    /// </summary>
    private static string DistillToKeywords(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return query ?? "";
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var looksLikeQuestion = query.EndsWith("?") || words.Length > 8;
        if (!looksLikeQuestion) return query;
        var distilled = DeriveQueryFromMessage(query.TrimEnd('?').Trim());
        return string.IsNullOrWhiteSpace(distilled) ? query : distilled;
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
            "where", "who", "whom", "whose", "which", "can", "could", "you", "please",
            "tell", "me", "about", "find", "out", "all", "details", "information",
            "research", "on", "of", "for", "in", "to", "and", "or", "put", "it",
            "its", "my", "journal", "save", "this", "that", "these", "those", "i",
            "want", "need", "get", "give", "write", "create", "make", "entry", "into",
            "gather", "collect", "look", "up", "everything", "know", "do", "does",
            "did", "doing", "have", "has", "had", "having", "be", "been", "being",
            "will", "would", "shall", "should", "may", "might", "must", "with",
            "from", "by", "at", "as", "if", "than", "then", "so", "such", "not",
            "no", "between", "during", "before", "after", "while", "each", "other",
            "they", "them", "their", "he", "she", "his", "her", "we", "our", "your",
            "any", "some", "only", "also", "just", "over", "under", "associated",
            "related", "regarding", "explain", "describe", "discuss", "compare",
            "list", "examples", "example", "use", "used", "using", "today", "there",
            "here", "much", "many", "most", "more", "both", "through", "across",
            "within", "against", "per", "via", "versus", "vs", "etc", "people"
        };
        var words = (userMessage ?? "")
            .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '?', '!', '(', ')', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 && w.Any(char.IsLetterOrDigit) && !stopwords.Contains(w))
            .Take(14)
            .ToList();
        // Subject-first ordering: search engines weight leading terms, so
        // capitalized words (proper nouns — the actual subject) go before
        // generic qualifiers ("primary", "significant") that otherwise make
        // engines return dictionary definitions.
        var entities = words.Where(w => char.IsUpper(w[0])).ToList();
        var rest = words.Where(w => !char.IsUpper(w[0])).ToList();
        var query = string.Join(' ', entities.Concat(rest).Take(10)).Trim();
        return string.IsNullOrWhiteSpace(query) ? (userMessage ?? "").Trim() : query;
    }
}
