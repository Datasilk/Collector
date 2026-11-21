using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Collector.Chat.Helpers;
using Collector.Data.Interfaces;

namespace Collector.Chat.Tools.GatherResearch;

/// <summary>
/// Sends an article's raw HTML to the active Ollama HTML parser model
/// (LLM_HtmlParsers) and returns the structured JSON it produces —
/// the WebScraper.md schema describes the expected shape. The result is
/// persisted next to the entry under research/{####}.json.
/// </summary>
public class HtmlParser
{
    private readonly ILLMHtmlParsersRepository? _repo;
    private readonly LLMClient? _llm;

    public HtmlParser(ILLMHtmlParsersRepository? repo, LLMClient? llm)
    {
        _repo = repo;
        _llm = llm;
    }

    /// <summary>True when a parser model is configured and a client exists.</summary>
    public bool Available
    {
        get
        {
            try { return _llm != null && _repo?.GetActive() != null; }
            catch { return false; }
        }
    }

    /// <summary>
    /// Prompt the active parser model with rawHtml + the WebScraper.md JSON
    /// schema. Returns the model's JSON response, or null when no parser is
    /// configured / the call fails.
    /// </summary>
    /// <summary>Parser-model input cap. HTML tokenizes at ~3.5-4 chars/token
    /// so 380K chars ≈ 95-110K input tokens — inside num_ctx 131072 with the
    /// 16K output reserve and a small headroom for the instruction/schema.
    /// Going higher means raising NumCtx too (KV-memory bound).</summary>
    private const int MaxHtmlChars = 380_000;
    private static readonly TimeSpan ParseTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Generation options matching Jina's own ReaderLM-v2 harness —
    /// deterministic decoding for extraction, a real context window (Ollama's
    /// 4K default would truncate the prompt to ~5% of the page), and a 32K
    /// output budget for full-page JSON — large pages exceeded 16K tokens and
    /// returned truncated dumps. Integration repairs truncated JSON, so a
    /// capped output still yields partial content.</summary>
    private static readonly OllamaSharp.Models.RequestOptions ParserOptions = new()
    {
        NumCtx = 131072,
        NumPredict = 32768,
        Temperature = 0f,
        TopK = 1,
        RepeatPenalty = 1.13f
    };

    public async Task<string?> ParseAsync(string rawHtml, string? subject = null, CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("web-scrape", "HtmlParser.ParseAsync");
        if (_llm == null || _repo == null || string.IsNullOrWhiteSpace(rawHtml)) return null;

        string? modelId;
        try { modelId = _repo.GetActive()?.Id; }
        catch { return null; }
        if (string.IsNullOrWhiteSpace(modelId)) return null;

        // Pre-strip: scripts/styles/inline-SVGs are most of a page's byte
        // size and none of its content — Wikipedia raw HTML is ~4MB without
        // this, which wedged the parser for minutes.
        var stripped = Regex.Replace(rawHtml,
            @"<(script|style|svg|noscript|iframe|form|select|template)\b[^>]*>.*?</\1\s*>",
            "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (stripped.Length < rawHtml.Length)
            ChatDetailLog.Line($"parser input cleaned: {rawHtml.Length} → {stripped.Length} chars");
        rawHtml = stripped;

        if (rawHtml.Length > MaxHtmlChars)
        {
            ChatDetailLog.Line($"parser input trimmed: {rawHtml.Length} → {MaxHtmlChars} chars");
            rawHtml = rawHtml.Substring(0, MaxHtmlChars);
        }

        // Jina's trained prompt shape for JSON extraction: instruction →
        // ```html block → "The JSON schema is as follows:" → ```json block.
        // Single user turn — the model wasn't trained with a system message.
        var instruction = "Extract the main content from the given HTML and organize it into a logical, well-structured JSON object based on the data found on the page.";
        if (!string.IsNullOrWhiteSpace(subject))
            instruction += " Include only content that relates to the research subject below — omit navigation, ads, related-article links, and any text that does not relate to this subject.\n" +
                           "Research subject: " + subject.Trim();
        var schema = LoadSchema();
        var user = instruction + "\n\n" +
                   "```html\n" + rawHtml + "\n```\n" +
                   "The JSON schema is as follows:```json\n" + schema + "\n```";

        // OllamaSharp doesn't reliably abort an in-flight generate on token
        // cancellation, so bound the WAIT instead: abandon the call after the
        // timeout (its request may finish in the background — harmless).
        var call = _llm.LocalAsync(
            "",
            user, json: true, cancellationToken: cancellationToken, model: modelId,
            options: ParserOptions);
        var winner = await Task.WhenAny(call, Task.Delay(ParseTimeout, cancellationToken));
        if (winner != call)
        {
            ChatDetailLog.Line("parser model timed out — continuing without parsed JSON");
            return null;
        }
        string? response;
        try { response = (await call)?.Trim(); }
        catch (Exception ex)
        {
            // e.g. the Ollama HttpClient's own 100s timeout — degrade to
            // "no parsed JSON" instead of aborting the whole run.
            ChatDetailLog.Line($"parser call failed: {ex.Message} — continuing without parsed JSON\n{ex.StackTrace}");
            return null;
        }
        return string.IsNullOrWhiteSpace(response) ? null : response;
    }
    /// <summary>JSON schema block inside WebScraper.md (embedded resource).</summary>
    private static string? _schema;

    public static string LoadSchema()
    {
        if (_schema != null) return _schema;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith("WebScraper.md")) continue;
                using var stream = asm.GetManifestResourceStream(name);
                if (stream == null) break;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var text = reader.ReadToEnd();
                var m = Regex.Match(text, @"```json\s*(\{.*?\})\s*```", RegexOptions.Singleline);
                _schema = m.Success ? m.Groups[1].Value : text.Trim();
                return _schema;
            }
        }
        catch { }
        _schema = "{}";
        return _schema;
    }
}
