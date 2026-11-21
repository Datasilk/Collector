using Collector.Chat.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Collector.Chat.Models;
using Collector.Chat.Tools.GatherResearch;
using Collector.Common.Extensions.Strings;
using Collector.Data.Interfaces;
using Collector.Domains;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for extracting content from a URL. The actual extraction is delegated
/// to a site-specific <see cref="IResearchSourceHelper"/> resolved by domain
/// (YouTube → captions transcript, everything else → shared downloader +
/// article analyzer), so each site gets handled in its own way while the
/// shared-data contract stays identical.
/// </summary>
public class WebScrapeTool : IChatTool
{
    private readonly LLMClient? _llm;
    private readonly IDomainsRepository? _domainsRepository;
    /// <summary>(appUserId, domain, ct) → fresh cookies from the client extension.</summary>
    private readonly Func<Guid, string, CancellationToken, Task<string>>? _requestCookies;
    /// <summary>HTML → structured-JSON parser (LLM_HtmlParsers active model).</summary>
    private readonly HtmlParser? _htmlParser;
    /// <summary>New site-search endpoints discovered on scraped pages land here.</summary>
    private readonly ISearchEnginesRepository? _searchEnginesRepo;

    public WebScrapeTool(
        LLMClient? llm = null,
        IDomainsRepository? domainsRepository = null,
        Func<Guid, string, CancellationToken, Task<string>>? requestCookies = null,
        ILLMHtmlParsersRepository? htmlParsersRepository = null,
        ISearchEnginesRepository? searchEnginesRepo = null)
    {
        _llm = llm;
        _domainsRepository = domainsRepository;
        _requestCookies = requestCookies;
        _htmlParser = new HtmlParser(htmlParsersRepository, llm);
        _searchEnginesRepo = searchEnginesRepo;
    }

    public string ToolKey => "web-scrape";

    public string Description => "Extract content from a URL via a site-specific research helper (article text, transcripts, images, videos, files)";

    public bool AvailableToOllama => false;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "url",
            DataType = "string",
            Description = "A URL to extract content from"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField { Key = "title", DataType = "string" },
        new ToolResponseField { Key = "content", DataType = "string" },
        new ToolResponseField { Key = "url", DataType = "string" }
    };

    public async Task Run(
        string userMessage,
        string ragContext,
        List<Dictionary<string, List<string>>> data,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        Guid appUserId,
        Collector.Chat.Models.ExecutionPlan? plan = null,
        int currentStepIndex = -1,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null,
        OnSaveChatHistory? onSaveChatHistory = null,
        SendMessage? sendMessage = null,
        CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("web-scrape", "Run");
        try
        {
            onProgress(10, "Preparing to extract content...");

            // Get URL from plan args or check if this is a follow-up call with URL in data
            string url = string.Empty;
            if (plan != null && currentStepIndex >= 0 && currentStepIndex < plan.Steps.Count)
            {
                var step = plan.Steps[currentStepIndex];
                if (step.Args != null && step.Args.TryGetValue("url", out var urlObj) && urlObj != null)
                {
                    url = urlObj.ToString() ?? string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                onError("No URL provided for web scraping");
                return;
            }

            // Never re-download a page the entry already lists as a source —
            // coverage loops can re-emit the same URLs across waves.
            if (AlreadyCollected(data, url))
            {
                onProgress(100, "Source already collected — skipping download");
                sendMessage?.Invoke($"Skipping {url} — already collected in this entry's sources.", null, null);
                onComplete($"Skipped {url} — already a recorded source");
                return;
            }

            // Domain → helper dispatch: YouTube pulls the caption transcript,
            // everything else goes through the shared article pipeline.
            var helper = ResearchHelpers.Resolve(url);
            onProgress(30, $"Extracting content from {url}...");

            var page = await helper.ExtractAsync(url,
                (percent, message) => onProgress(Math.Min(90, percent), message),
                cancellationToken, appUserId,
                _requestCookies == null
                    ? null
                    : (domain, ct) => _requestCookies(appUserId, domain, ct),
                _htmlParser == null ? null : (html, ct) => _htmlParser.ParseAsync(html, userMessage, ct),
                _searchEnginesRepo,
                _llm == null || _searchEnginesRepo == null
                    ? null
                    : (Func<string, Task<string>>)(p => _llm.LocalAsync(
                        Prompts.ScriptAnalysisSystem(), p, json: true, cancellationToken)));

            if (page == null)
            {
                RecordFailedPage(data, url);
                onError($"Failed to download or parse content from URL: {url}");
                return;
            }

            // Bot-lite stub: the host served a near-empty DOM (PMC ~30
            // elements). Not a real page — don't store it, and count the
            // host so gather-research stops re-queuing its siblings.
            if (!page.IsFile && page.DomElements >= 0 && page.DomElements < 60)
            {
                RecordFailedPage(data, url);
                var stubMsg = $"Skipped {page.Url} — host served a stub page ({page.DomElements} DOM elements; likely a bot wall)";
                ChatDetailLog.Line($"web-scrape: {stubMsg}");
                onProgress(100, stubMsg);
                onComplete(stubMsg);
                return;
            }

            // Unknown domain → register it AND analyze it now — the research
            // pipeline does not depend on the DomainWorker being started.
            await TryRegisterDomainAsync(page, onProgress, cancellationToken);

            // The pipeline is English-first — helpers that return another
            // language get their content translated before it hits storage.
            await TranslateToEnglishIfNeededAsync(page, onProgress, cancellationToken);

            onProgress(90, "Storing extracted data...");

            data.Add(page.ToDataItem());

            if (page.IsFile)
            {
                onProgress(100, $"Detected downloadable file ({page.FileContentType})");
                onComplete($"Downloadable file found at {page.Url} ({page.FileContentType})");
                return;
            }

            // Save a compact reference to chat history for fact extraction —
            // tool-result clearing: the memory layer gets url + title + section
            // map + a bounded excerpt, never the full page text.
            var excerpt = string.Join("\n\n", page.ContentSections);
            if (excerpt.Length > 1200) excerpt = excerpt.Substring(0, 1200) + "…";
            var reference = $"Web page scraped: {page.Title} ({page.Url})" +
                (page.Headers.Count > 0 ? $"\nSections: {string.Join(", ", page.Headers.Take(12))}" : "") +
                (excerpt.Length > 0 ? $"\nExcerpt:\n{excerpt}" : "");
            if (reference.Length > 60)
            {
                onSaveChatHistory?.Invoke(reference);
            }

            onProgress(100, "Content extracted successfully");

            var summary = !string.IsNullOrEmpty(page.Title)
                ? $"Successfully scraped: {page.Title}"
                : "Successfully scraped web page content";

            onComplete(summary);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            onError($"Unexpected error during web scraping: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Has the session's journal entry already recorded this URL as a
    /// research source? Reads the entry's research object from the
    /// session-entry marker in shared data. Best-effort — any failure
    /// means "not collected" so scraping proceeds.
    /// </summary>
    /// <summary>
    /// Record a scrape failure in shared data so coverage loops stop
    /// re-queuing it: the URL joins "failed-pages" (exact dedupe) and its
    /// host joins "failed-hosts" — gather-research blocks a host entirely
    /// after 2 failures this run (bot walls rarely lift mid-run).
    /// </summary>
    private static void RecordFailedPage(List<Dictionary<string, List<string>>> data, string url)
    {
        try
        {
            ResearchReviewTool.AddToList(data, "failed-pages", "urls", new[] { url });
            if (Uri.TryCreate(url, UriKind.Absolute, out var u))
                ResearchReviewTool.AddToList(data, "failed-hosts", "hosts", new[] { u.Host });
        }
        catch { /* bookkeeping is best-effort */ }
    }

    private static bool AlreadyCollected(List<Dictionary<string, List<string>>> data, string url)
    {
        try
        {
            var entryId = ResearchReviewTool.GetSessionEntryId(data);
            if (entryId == null) return false;
            var research = JournalEntryDocument.Load(entryId.Value, ResearchReviewTool.GetSessionJournalId(data)).Research;
            return research?.HasSource(url) == true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Insert the page's domain into the Domains table when it isn't known,
    /// then run the shared DomainAnalyzer on it immediately — the same
    /// homepage-download + LLM-categorize pipeline the DomainWorker runs,
    /// so research never depends on that worker being started. Best-effort:
    /// registration/analysis failure never breaks extraction.
    /// </summary>
    private async Task TryRegisterDomainAsync(
        ResearchPage page, OnProgress onProgress, CancellationToken cancellationToken)
    {
        ChatDetailLog.Fn("web-scrape", "TryRegisterDomainAsync");
        if (_domainsRepository == null || page.IsFile || string.IsNullOrWhiteSpace(page.Url)) return;
        try
        {
            var domain = page.Url.GetDomainName();
            if (string.IsNullOrWhiteSpace(domain)) return;
            if (_domainsRepository.GetInfo(domain) != null) return;

            var domainId = _domainsRepository.Add(domain, page.Title ?? "");
            onProgress(85, $"Registered new domain for analysis: {domain}");

            if (domainId <= 0 || _llm == null) return;

            var result = await DomainAnalyzer.AnalyzeAsync(
                _domainsRepository, domainId,
                (system, user) => _llm.RemoteAsync(system, user,
                    cancellationToken: cancellationToken, debugType: "domain-analysis"),
                msg => onProgress(86, msg),
                cancellationToken);

            if (result.Status == DomainAnalyzer.Status.Analyzed)
            {
                onProgress(88, $"Analyzed {domain}: type={result.Type}, lang={result.Language}");
            }
            else
            {
                onProgress(88, $"Domain analysis for {domain}: {result.Failure ?? result.Status.ToString()}");
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* registration/analysis is best-effort — never breaks extraction */ }
    }

    /// <summary>
    /// Non-English sources get translated — the helper declares the website's
    /// language (HTML lang / subtitle track); when it can't, one detect call
    /// on a content sample decides. Everything from a non-English source is
    /// translated; a failed translation keeps the original text.
    /// </summary>
    private async Task TranslateToEnglishIfNeededAsync(
        ResearchPage page, OnProgress onProgress, CancellationToken cancellationToken)
    {
        ChatDetailLog.Fn("web-scrape", "TranslateToEnglishIfNeededAsync");
        if (_llm == null || page.IsFile || page.ContentSections.Count == 0) return;

        bool english;
        if (!string.IsNullOrWhiteSpace(page.Language))
        {
            english = page.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            var sample = string.Join(' ', page.ContentSections.Take(3));
            if (sample.Length > 600) sample = sample.Substring(0, 600);
            if (string.IsNullOrWhiteSpace(sample)) return;

            try
            {
                var detect = await _llm.LocalAsync(
                    Prompts.LanguageDetectSystem(), "TEXT:\n" + sample, json: true, cancellationToken);
                english = LLMJson.TryParse<LanguageDetectResponse>(detect, out var d) && (d?.English ?? true);
            }
            catch (OperationCanceledException) { throw; }
            catch { return; /* can't tell — leave as-is */ }
        }
        if (english) return;

        onProgress(80, "Translating content to English...");
        try
        {
            var translated = new List<string>();
            foreach (var section in page.ContentSections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var t = await _llm.LocalAsync(Prompts.TranslateSystem(), section, json: false, cancellationToken);
                translated.Add(string.IsNullOrWhiteSpace(t) ? section : t.Trim());
            }
            page.ContentSections = translated;

            if (!string.IsNullOrWhiteSpace(page.Title))
            {
                var t = await _llm.LocalAsync(Prompts.TranslateSystem(), page.Title, json: false, cancellationToken);
                if (!string.IsNullOrWhiteSpace(t)) page.Title = t.Trim();
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* translation is best-effort — original text is fine */ }
    }

    private class LanguageDetectResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("english")]
        public bool English { get; set; }
    }
}
