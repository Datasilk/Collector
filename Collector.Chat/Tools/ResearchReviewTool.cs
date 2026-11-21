using Collector.Chat.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using OllamaSharp;
using Collector.Chat.Models;
using Collector.Data.Interfaces;

namespace Collector.Chat.Tools;

/// <summary>
/// Coverage check at the end of a research run. Compares the research
/// questions captured by gather-research against the journal entry's current
/// text; for every question still unanswered (and not already asked), it
/// appends new gather-research steps — so the run iterates until the entry
/// holds a complete set of knowledge. The loop guard is question/query
/// dedupe (tracked in the shared-data markers), never an iteration cap.
/// </summary>
public class ResearchReviewTool : IChatTool
{
    private readonly IJournalEntriesRepository _entriesRepository;
    private readonly OllamaApiClient _ollama;
    private readonly string _ollamaModel;
    private readonly LLMClient _llm;

    /// <summary>Max new gather-research steps a single review may add.</summary>
    private const int MaxNewSearches = 4;
    /// <summary>Hard budget on total planned pages per run — beyond this,
    /// follow-ups stop regardless of remaining gaps (the loop guard alone
    /// lets the coverage LLM rephrase gaps into endless new searches).</summary>
    private const int MaxPlannedPages = 25;
    /// <summary>Chars of entry text sent to the coverage check.</summary>
    private const int EntryTextBudget = 7000;

    public ResearchReviewTool(
        IJournalEntriesRepository entriesRepository,
        OllamaApiClient ollama,
        string ollamaModel = "llama3.2",
        LLMClient? llm = null)
    {
        _entriesRepository = entriesRepository;
        _ollama = ollama;
        _ollamaModel = ollamaModel;
        _llm = llm ?? new LLMClient(ollama, ollamaModel);
    }

    public string ToolKey => "research-review";

    public string Description => "Check whether all research questions are covered by the journal entry; queue more research for gaps.";

    public bool AvailableToOllama => false;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter { Key = "context", DataType = "string", Description = "Optional extra context for the coverage check" }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField { Key = "gaps", DataType = "int" },
        new ToolResponseField { Key = "covered", DataType = "boolean" }
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
        ChatDetailLog.Fn("research-review", "Run");
        try
        {
            onProgress(10, "Reviewing research coverage...");

            var questions = GetList(data, "research-questions", "questions");
            if (questions.Count == 0)
            {
                onComplete("No tracked research questions — nothing to review");
                return;
            }

            // Questions previously judged unanswerable leave the coverage set —
            // chasing them again would just loop on sources that can't help.
            var unanswerableSet = GetList(data, "unanswerable-questions", "questions")
                .Select(Normalize).ToHashSet();
            if (unanswerableSet.Count > 0)
            {
                questions = questions.Where(q => !unanswerableSet.Contains(Normalize(q))).ToList();
            }
            if (questions.Count == 0)
            {
                onComplete("All remaining research questions were marked unanswerable");
                return;
            }

            // URL-direct obligations die with their pages: when every
            // user-named URL was relevance-skipped or failed, the questions
            // scoped to those pages can't be covered — retire them instead
            // of queueing searches that chase an off-topic page.
            var directUrls = GetList(data, "url-direct", "urls")
                .Select(u => u.Trim()).ToList();
            if (directUrls.Count > 0)
            {
                var deadUrls = GetList(data, "url-direct-skipped", "urls")
                    .Concat(GetList(data, "failed-pages", "urls"))
                    .Select(u => u.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (directUrls.All(u => deadUrls.Contains(u)))
                {
                    AddToList(data, "unanswerable-questions", "questions", questions);
                    onComplete($"URL-named page(s) were skipped or failed — their {questions.Count} research question(s) are unanswerable");
                    return;
                }
            }

            // Load the entry's current text
            var entryId = GetSessionEntryId(data);
            string entryText = "";
            ResearchObject? research = null;
            if (entryId != null)
            {
                var doc = JournalEntryDocument.Load(entryId.Value, GetSessionJournalId(data));
                entryText = doc.ExtractText();
                if (entryText.Length > EntryTextBudget)
                    entryText = entryText.Substring(0, EntryTextBudget) + "\n…(truncated)";
                research = doc.Research;
            }

            // Subtopic deepening pass (once per request) — shallow topics get a
            // targeted search round of up to 3 pages each, ~15 pages total.
            var deepenMsg = "";
            if (research != null && entryId != null)
            {
                deepenMsg = await RunDeepeningPassAsync(data, plan, currentStepIndex, entryId.Value, research, cancellationToken);
            }

            // Run accounting — planned pages vs. what actually landed in
            // shared data (failed scrapes never create an item, so the
            // difference is the failure count).
            var scrapeTypes = new HashSet<string> { "web-scrape", "content", "file-download" };
            var collected = data
                .Where(i => i.TryGetValue("type", out var tl) && tl.Any(t => scrapeTypes.Contains(t)))
                .Select(i => i.TryGetValue("url", out var u) ? u.FirstOrDefault() : null)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var planned = GetList(data, "planned-pages", "urls")
                .Select(u => u.Trim()).Where(u => u.Length > 0).Distinct().Count();
            var tally = planned > 0
                ? $"Collected research from {collected} of {planned} page{(planned == 1 ? "" : "s")}" +
                  (planned - collected > 0 ? $" — {planned - collected} failed to scrape" : "") + "."
                : "";

            onProgress(40, "Checking coverage against journal entry...");

            var system = Prompts.CoverageCheckSystem();
            var user = Prompts.CoverageCheckUser(questions, entryText);

            var result = await LLMJson.ParseWithRetryAsync<CoverageResponse>(
                prompt => _llm.LocalAsync(system, prompt, json: true, cancellationToken,
                    onRequest: onRawRequest != null ? (t, p) => onRawRequest("coverage-check", p) : null,
                    onResponse: onRawResponse != null ? (t, r) => onRawResponse("coverage-check", r) : null,
                    debugType: "coverage-check"),
                user,
                r => r == null ? "response was empty" : (r.Unanswered == null ? "missing \"unanswered\" array" : null),
                maxAttempts: 2);

            var unanswered = result?.Unanswered?
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .Select(q => q.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            // Filter out anything already asked (the loop guard — never re-ask)
            var asked = GetList(data, "asked-queries", "queries")
                .Select(Normalize)
                .ToHashSet();
            var gaps = unanswered
                .Where(q => !asked.Contains(Normalize(q)))
                .Take(MaxNewSearches)
                .ToList();

            // Page-budget guard — the dedupe above can't catch semantically
            // rephrased gaps, so without a hard cap failed-scrape runs queue
            // searches forever and the plan grows without bound.
            var budgetMsg = "";
            if (gaps.Count > 0 && planned >= MaxPlannedPages)
            {
                gaps = new List<string>();
                budgetMsg = $"Page budget reached ({planned} planned) — not queueing further research.";
            }

            // Stale questions — searched before, still uncovered. Judge once
            // whether the collected sources can even answer them; unanswerable
            // ones leave the coverage set permanently.
            var unanswerableMsg = budgetMsg;
            var stale = unanswered.Where(q => asked.Contains(Normalize(q))).ToList();
            if (stale.Count > 0 && research != null)
            {
                var marked = await CheckAnswerabilityAsync(research, stale, onRawRequest, onRawResponse, cancellationToken);
                var markedNorm = marked.Select(Normalize).ToHashSet();
                var markedOriginals = stale.Where(q => markedNorm.Contains(Normalize(q))).ToList();
                if (markedOriginals.Count > 0)
                {
                    AddToList(data, "unanswerable-questions", "questions", markedOriginals);
                    unanswered = unanswered.Where(q => !markedNorm.Contains(Normalize(q))).ToList();
                    unanswerableMsg = $"{markedOriginals.Count} question{(markedOriginals.Count == 1 ? "" : "s")} can't be answered by the collected sources.";
                }
            }

            if (gaps.Count == 0)
            {
                if (unanswered.Count == 0)
                {
                    onProgress(100, "All research questions are covered");
                    sendMessage?.Invoke("All research questions are covered by the journal entry." +
                        (tally.Length > 0 ? " " + tally : "") +
                        (unanswerableMsg.Length > 0 ? " " + unanswerableMsg : "") +
                        (deepenMsg.Length > 0 ? " " + deepenMsg : ""), null, null);
                    onComplete($"Coverage check complete — all {questions.Count} question(s) covered" +
                        (tally.Length > 0 ? $"; {tally.TrimEnd('.')}" : "") +
                        (unanswerableMsg.Length > 0 ? $"; {unanswerableMsg.TrimEnd('.')}" : "") +
                        (deepenMsg.Length > 0 ? $"; {deepenMsg}" : ""));
                }
                else
                {
                    // Unanswered questions remain but every one has already
                    // been searched — chasing them again would just loop.
                    var exhausted = $"{unanswered.Count} question{(unanswered.Count == 1 ? " is" : "s are")} still unanswered, but each was already researched — stopping follow-ups.";
                    onProgress(100, exhausted);
                    sendMessage?.Invoke(exhausted +
                        (tally.Length > 0 ? " " + tally : "") +
                        (unanswerableMsg.Length > 0 ? " " + unanswerableMsg : "") +
                        (deepenMsg.Length > 0 ? " " + deepenMsg : ""), null, null);
                    onComplete($"Coverage check complete — {unanswered.Count} question(s) unanswered after research" +
                        (tally.Length > 0 ? $"; {tally.TrimEnd('.')}" : "") +
                        (unanswerableMsg.Length > 0 ? $"; {unanswerableMsg.TrimEnd('.')}" : "") +
                        (deepenMsg.Length > 0 ? $"; {deepenMsg}" : ""));
                }
                if (plan != null && currentStepIndex >= 0 && currentStepIndex < plan.Steps.Count)
                {
                    plan.Steps[currentStepIndex].Result ??= new Dictionary<string, object>();
                    plan.Steps[currentStepIndex].Result["covered"] = unanswered.Count == 0 ? "true" : "false";
                    plan.Steps[currentStepIndex].Result["gaps"] = "0";
                }
                return;
            }

            // Queue a gather-research step per unanswered question
            if (plan != null && currentStepIndex >= 0)
            {
                for (int i = gaps.Count - 1; i >= 0; i--)
                {
                    PlanUtility.AddStepToPlan(plan, currentStepIndex, "gather-research", new Dictionary<string, object>
                    {
                        { "query", gaps[i] },
                        { "context", "Research gap found during coverage review" }
                    });
                }
            }

            sendMessage?.Invoke(
                $"Coverage check: {gaps.Count} question{(gaps.Count == 1 ? "" : "s")} still unanswered — researching further…" +
                (tally.Length > 0 ? " " + tally : "") +
                (unanswerableMsg.Length > 0 ? " " + unanswerableMsg : "") +
                (deepenMsg.Length > 0 ? " " + deepenMsg : ""),
                null, null);
            onProgress(100, $"Queued {gaps.Count} follow-up search(es)");
            onComplete($"Coverage check found {gaps.Count} gap(s); queued follow-up research" +
                (deepenMsg.Length > 0 ? $"; {deepenMsg}" : ""));

            if (plan != null && currentStepIndex >= 0 && currentStepIndex < plan.Steps.Count)
            {
                plan.Steps[currentStepIndex].Result ??= new Dictionary<string, object>();
                plan.Steps[currentStepIndex].Result["covered"] = "false";
                plan.Steps[currentStepIndex].Result["gaps"] = gaps.Count.ToString();
            }
        }
        catch (Exception ex)
        {
            // Coverage review is advisory — never fail the research run over it
            onProgress(100, "Coverage review skipped");
            onComplete($"Coverage review skipped ({ex.Message})");
        }
    }

    /// <summary>
    /// Deepening pass — after initial collection, topics with thin fact sets
    /// (&lt;3 facts, or judged too generic by the local LLM) each get one
    /// targeted search of up to 3 pages, ~15 pages total per request. Each
    /// topic is deepened at most once; deepened pages never trigger another
    /// pass (the "deepening-done" marker).
    /// </summary>
    private async Task<string> RunDeepeningPassAsync(
        List<Dictionary<string, List<string>>> data,
        ExecutionPlan? plan,
        int currentStepIndex,
        Guid entryId,
        ResearchObject research,
        CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("research-review", "RunDeepeningPassAsync");
        try
        {
            if (plan == null || currentStepIndex < 0) return "";
            if (GetList(data, "deepening-done", "flag").Count > 0) return "";

            // Respect the same page budget as gap follow-ups.
            var plannedSoFar = GetList(data, "planned-pages", "urls")
                .Select(u => u.Trim()).Where(u => u.Length > 0).Distinct().Count();
            if (plannedSoFar >= MaxPlannedPages) return "";

            var entry = _entriesRepository.GetById(entryId);
            var entryTitle = entry?.Title ?? "";
            if (string.IsNullOrWhiteSpace(entryTitle)) return "";

            var deepened = GetList(data, "deepened-topics", "topics")
                .Select(ResearchObject.Normalize).ToHashSet();

            var candidates = research.AllTopics()
                .Where(t => !t.Archived && !deepened.Contains(ResearchObject.Normalize(t.Topic)))
                .ToList();

            var shallow = new List<ResearchTopic>();
            var llmChecks = 0;
            foreach (var t in candidates)
            {
                if (ResearchObject.TextItemCount(t) < 3) { shallow.Add(t); continue; }
                if (llmChecks >= 6) continue;
                llmChecks++;
                if (!await IsDetailedAsync(t, cancellationToken)) shallow.Add(t);
            }

            const int MaxDeepenPages = 15;
            var budget = MaxDeepenPages;
            var queued = new List<string>();
            foreach (var t in shallow)
            {
                var pages = Math.Min(3, budget);
                if (pages <= 0) break;
                budget -= pages;
                AddToList(data, "deepened-topics", "topics", new[] { t.Topic });
                queued.Add($"{t.Topic} ({pages} pages)");
                PlanUtility.AddStepToPlan(plan, currentStepIndex, "gather-research", new Dictionary<string, object>
                {
                    { "query", $"{entryTitle} {t.Topic}" },
                    { "maxResults", pages },
                    { "context", $"Deepening research for the \"{t.Topic}\" section" }
                });
            }

            AddToList(data, "deepening-done", "flag", new[] { "true" });
            return queued.Count == 0
                ? ""
                : $"Deepening {queued.Count} thin topic{(queued.Count == 1 ? "" : "s")}: {string.Join(", ", queued)}.";
        }
        catch { return ""; }
    }

    /// <summary>Cheap local-LLM depth check — are a topic's facts specific
    /// enough, or generic filler?</summary>
    private async Task<bool> IsDetailedAsync(ResearchTopic topic, CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("research-review", "IsDetailedAsync");
        try
        {
            var raw = await _llm.LocalAsync(Prompts.DepthCheckSystem(),
                Prompts.DepthCheckUser(topic), json: true, cancellationToken);
            if (LLMJson.TryParse<DepthResponse>(raw, out var r) && r != null)
                return r.Detailed;
        }
        catch { }
        return true; // assume detailed on failure — deepening is best-effort
    }

    private class DepthResponse
    {
        [JsonPropertyName("detailed")] public bool Detailed { get; set; }
    }

    /// <summary>
    /// One batched answerability judgment over questions that were already
    /// searched but remain uncovered — "can these sources even contain the
    /// answer?" Unanswerable questions get permanently excluded from coverage
    /// so the review loop can't chase them forever.
    /// </summary>
    private async Task<List<string>> CheckAnswerabilityAsync(
        ResearchObject research,
        List<string> staleQuestions,
        OnRawRequest? onRawRequest,
        OnRawResponse? onRawResponse,
        CancellationToken cancellationToken)
    {
        ChatDetailLog.Fn("research-review", "CheckAnswerabilityAsync");
        try
        {
            var sources = string.Join("\n", research.Sources
                .Take(30)
                .Select(s => $"- {s.Title} ({s.Url})"));
            var result = await LLMJson.ParseWithRetryAsync<AnswerabilityResponse>(
                prompt => _llm.LocalAsync(Prompts.AnswerabilitySystem(), prompt, json: true, cancellationToken,
                    onRequest: onRawRequest != null ? (t, p) => onRawRequest("answerability-check", p) : null,
                    onResponse: onRawResponse != null ? (t, r) => onRawResponse("answerability-check", r) : null,
                    debugType: "answerability-check"),
                Prompts.AnswerabilityUser(sources, staleQuestions),
                r => r == null ? "response was empty" : (r.Unanswerable == null ? "missing \"unanswerable\"" : null),
                maxAttempts: 2);
            return result?.Unanswerable?
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .Select(q => q.Trim())
                .ToList() ?? new List<string>();
        }
        catch (OperationCanceledException) { throw; }
        catch { return new List<string>(); }
    }

    private class AnswerabilityResponse
    {
        [JsonPropertyName("unanswerable")] public List<string>? Unanswerable { get; set; }
    }

    public static Guid? GetSessionEntryId(List<Dictionary<string, List<string>>> data)
    {
        foreach (var item in data)
        {
            if (item.TryGetValue("type", out var t) && t.Contains("session-entry") &&
                item.TryGetValue("entryId", out var ids) &&
                Guid.TryParse(ids.FirstOrDefault(), out var id))
            {
                return id;
            }
        }
        return null;
    }

    /// <summary>Journal id from the session-entry marker (0 when unknown).</summary>
    public static int GetSessionJournalId(List<Dictionary<string, List<string>>> data)
    {
        foreach (var item in data)
        {
            if (item.TryGetValue("type", out var t) && t.Contains("session-entry") &&
                item.TryGetValue("journalId", out var ids) &&
                int.TryParse(ids.FirstOrDefault(), out var id))
            {
                return id;
            }
        }
        return 0;
    }

    /// <summary>Get a string list from a shared-data document.</summary>
    public static List<string> GetList(List<Dictionary<string, List<string>>> data, string type, string key)
    {
        foreach (var item in data)
        {
            if (item.TryGetValue("type", out var t) && t.Contains(type) &&
                item.TryGetValue(key, out var list))
            {
                return list.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
        }
        return new List<string>();
    }

    /// <summary>Append values to a shared-data document list (created if absent).</summary>
    public static void AddToList(List<Dictionary<string, List<string>>> data, string type, string key, IEnumerable<string> values)
    {
        foreach (var item in data)
        {
            if (item.TryGetValue("type", out var t) && t.Contains(type))
            {
                if (!item.TryGetValue(key, out var list)) item[key] = list = new List<string>();
                list.AddRange(values);
                return;
            }
        }
        var doc = new Dictionary<string, List<string>>
        {
            ["type"] = new List<string> { type },
            [key] = new List<string>(values)
        };
        data.Add(doc);
    }

    private static string Normalize(string s)
        => string.Join(" ", (s ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private class CoverageResponse
    {
        [JsonPropertyName("unanswered")]
        public List<string>? Unanswered { get; set; }
    }
}
