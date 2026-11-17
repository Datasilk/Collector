using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Collector.Common;
using Collector.Common.DOM;
using Collector.Common.Models.Articles;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Chat.Tools;

/// <summary>
/// Shared web fetch/search plumbing used by the chat tools: query the
/// configured search engines, scrape pages through the Charlotte router +
/// article analyzer, and extract result links or media assets.
/// </summary>
public static class SearchHelper
{
    // Engines that fail repeatedly (blocked, bot-walled) sink to the back of
    // the queue for the process lifetime — reset automatically on success.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, int> _engineFailures = new();

    /// <summary>
    /// Load enabled engines ordered for a query: engines matching the
    /// preferred content type first, then web engines, then everything else —
    /// sort order breaks ties within each group. Falls back to a built-in
    /// DuckDuckGo/Google set when nothing is configured.
    /// </summary>
    public static List<SearchEngine> GetEnabledEngines(ISearchEnginesRepository? repo, string preferredType = "web")
    {
        var engines = new List<SearchEngine>();

        try
        {
            if (repo != null)
            {
                engines = repo.GetEnabled()
                    .Where(e => !string.IsNullOrWhiteSpace(e.Url) && e.Url.Contains("{search}"))
                    .ToList();
            }
        }
        catch
        {
            // Fall through to built-in defaults
        }

        if (engines.Count == 0)
        {
            engines.Add(new SearchEngine
            {
                Name = "DuckDuckGo",
                Url = "https://duckduckgo.com/html/?q={search}",
                Sort = 1,
                Enabled = true
            });
        }

        var type = (preferredType ?? "web").ToLowerInvariant();
        return engines
            .OrderBy(e => (e.Type ?? "web").Equals(type, StringComparison.OrdinalIgnoreCase) ? 0
                        : (e.Type ?? "web").Equals("web", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
            .ThenBy(e => _engineFailures.TryGetValue(e.SearchEngineId, out var fails) ? fails : 0)
            .ThenBy(e => e.Sort)
            .ThenBy(e => e.SearchEngineId)
            .ToList();
    }

    /// <summary>
    /// Run a query across the configured engines (priority order) and return
    /// filtered result URLs. Stops early once <paramref name="maxResults"/>
    /// results are collected.
    /// </summary>
    public static async Task<List<string>> SearchUrlsAsync(
        string query,
        ISearchEnginesRepository? repo,
        int maxResults = 5,
        string contentType = "web",
        Action<int, string>? onProgress = null,
        Action<string>? onDebug = null,
        Func<string, Task<string>>? scriptAnalyzer = null,
        bool allowEngineDomains = false)
    {
        var allUrls = new List<string>();
        var engines = GetEnabledEngines(repo, contentType);

        for (int i = 0; i < engines.Count; i++)
        {
            var engine = engines[i];
            var progressPercent = (int)((i / (double)engines.Count) * 60);
            onProgress?.Invoke(10 + progressPercent, $"Searching {engine.Name}...");

            try
            {
                var urls = await SearchEngineUrlsAsync(engine, query, onDebug, repo, scriptAnalyzer, allowEngineDomains);
                onDebug?.Invoke($"{engine.Name} returned {urls.Count} result url(s)");
                _engineFailures.TryRemove(engine.SearchEngineId, out _);
                if (urls.Count == 0)
                    _engineFailures.AddOrUpdate(engine.SearchEngineId, 1, (_, n) => n + 1);
                allUrls.AddRange(urls);
            }
            catch (Exception ex)
            {
                _engineFailures.AddOrUpdate(engine.SearchEngineId, 1, (_, n) => n + 1);
                onProgress?.Invoke(10 + progressPercent, $"{engine.Name} search failed: {ex.Message}");
                continue;
            }

            if (allUrls.Count >= maxResults)
                break;
        }

        var filtered = FilterByDomain(allUrls).Take(maxResults).ToList();
        onDebug?.Invoke($"Search complete: {allUrls.Count} raw urls → {filtered.Count} after domain dedupe");
        return filtered;
    }

    /// <summary>
    /// Search a specific engine and return result page URLs.
    /// </summary>
    public static async Task<List<string>> SearchEngineUrlsAsync(SearchEngine engine, string query, Action<string>? onDebug = null, ISearchEnginesRepository? repo = null, Func<string, Task<string>>? scriptAnalyzer = null, bool allowEngineDomains = false, string? urlOverride = null, CancellationToken cancellationToken = default)
        => (await SearchEnginePageAsync(engine, query, onDebug, repo, scriptAnalyzer, allowEngineDomains, urlOverride, cancellationToken)).Urls;

    /// <summary>Search an engine and return result URLs plus the parsed result page.</summary>
    public static async Task<(List<string> Urls, AnalyzedArticle? Article)> SearchEnginePageAsync(
        SearchEngine engine, string query, Action<string>? onDebug = null,
        ISearchEnginesRepository? repo = null, Func<string, Task<string>>? scriptAnalyzer = null,
        bool allowEngineDomains = false, string? urlOverride = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var urls = new List<string>();

        if ((engine.Method ?? "GET").Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            // POST engines can't go through Charlotte (GET-only) — send the
            // request directly and extract result links from the raw HTML.
            var html = await PostEngineRequestAsync(engine, query, onDebug);
            if (string.IsNullOrWhiteSpace(html))
                return (urls, null);
            var engineHost = Uri.TryCreate(engine.Url, UriKind.Absolute, out var eu) ? eu.Host : "";
            var postArticle = AnalyzeRawHtml(html);
            return (ExtractLinksFromHtml(html, engine.Url, engineHost, allowEngineDomains), postArticle);
        }

        var searchUrl = urlOverride ?? engine.Url.Replace("{search}", WebUtility.UrlEncode(query));

        // Direct fetch first — plain HTTP carries a different fingerprint than
        // Charlotte's headless browser and bypasses its bot-detection flags
        // (DDG/Google serve anomaly/consent walls to headless Chrome but real
        // results to a plain request). Charlotte stays as the fallback for
        // JS-rendered pages.
        var rawHtml = await FetchRawHtmlAsync(searchUrl, onDebug);
        if (!string.IsNullOrWhiteSpace(rawHtml))
        {
            if (LooksLikeBotWall(rawHtml))
                onDebug?.Invoke($"{engine.Name}: served a bot-detection page over direct HTTP");
            var directArticle = AnalyzeRawHtml(rawHtml);
            urls = ExtractResultUrls(directArticle, engine, allowEngineDomains);
            onDebug?.Invoke($"{engine.Name}: direct fetch → {urls.Count} result url(s) ({directArticle?.elements?.Count ?? 0} elements)");
            if (urls.Count > 0)
            {
                if (repo != null && directArticle?.elements != null)
                {
                    try
                    {
                        var found = DiscoverSearchEngines(directArticle, searchUrl, repo, onDebug);
                        if (!found && scriptAnalyzer != null)
                            await DiscoverFromScriptsAsync(directArticle, searchUrl, repo, scriptAnalyzer, onDebug);
                    }
                    catch { /* discovery is best-effort */ }
                }
                return (urls, directArticle);
            }
            onDebug?.Invoke($"{engine.Name}: direct fetch yielded no links — falling back to Charlotte");
        }

        onDebug?.Invoke($"{engine.Name} → Charlotte: {searchUrl}");
        cancellationToken.ThrowIfCancellationRequested();

        // Download search results page
        var domJson = Charlotte.Download(searchUrl, out var resolvedUrl);

        if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("file:") || domJson.StartsWith("Error"))
        {
            onDebug?.Invoke($"{engine.Name}: Charlotte returned no usable DOM ({(domJson?.Length ?? 0)} chars{(string.IsNullOrEmpty(domJson) ? "" : ": " + domJson.Substring(0, Math.Min(120, domJson.Length)))})");
            return (urls, null);
        }

        if (domJson.Contains("anomaly", StringComparison.OrdinalIgnoreCase)
            || domJson.Contains("captcha", StringComparison.OrdinalIgnoreCase))
            onDebug?.Invoke($"{engine.Name}: Charlotte DOM looks like a bot-detection page");

        // Parse and extract URLs
        var analyzedArticle = Html.DeserializeArticle(domJson);
        Html.GetArticleInfoFromDOM(analyzedArticle);
        onDebug?.Invoke($"{engine.Name}: DOM parsed — {analyzedArticle?.elements?.Count ?? 0} elements, resolved={resolvedUrl}");
        urls = ExtractResultUrls(analyzedArticle, engine, allowEngineDomains);

        // Search-result pages are prime discovery ground: the page's own
        // search form tells us how to query this site directly next time.
        if (repo != null && analyzedArticle?.elements != null)
        {
            try
            {
                var page = resolvedUrl ?? searchUrl;
                var found = DiscoverSearchEngines(analyzedArticle, page, repo, onDebug);
                if (!found && scriptAnalyzer != null)
                    await DiscoverFromScriptsAsync(analyzedArticle, page, repo, scriptAnalyzer, onDebug);
            }
            catch { /* discovery is best-effort */ }
        }
        return (urls, analyzedArticle);
    }

    private static readonly HashSet<string> PagerParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "start", "first", "b", "s", "p", "page", "pn", "pg", "offset", "skip",
        "startindex", "startpage", "pagestart", "pagenumber", "sp", "np", "from"
    };

    /// <summary>
    /// Find the "next page" link on a search result page. Primary signal is
    /// structural: a same-host, same-path anchor whose query params match the
    /// current page's except for ONE numeric param that increased (b=1→b=8,
    /// start=0→start=10, p=1→p=2) — catches any pager markup (">", "»",
    /// "more", bare numbers). Text/rel/aria hints are a fallback that also
    /// breaks ties toward the immediate next page.
    /// </summary>
    public static string? FindNextPageUrl(AnalyzedArticle? article, string currentUrl)
    {
        if (article?.elements == null || !Uri.TryCreate(currentUrl, UriKind.Absolute, out var cur))
            return null;

        var curParams = ParseQueryParams(cur.Query);
        var curPath = NormalizePath(cur.AbsolutePath);

        // rank 0 = delta on a known pager param, 1 = delta on any param,
        // 2 = text/rel/aria hint only. Smallest delta wins within a rank.
        var candidates = new List<(int Rank, long Delta, int Hint, string Url)>();

        foreach (var el in article.elements)
        {
            if (el?.tagName != "a" || el.attribute == null
                || !el.attribute.TryGetValue("href", out var href)
                || string.IsNullOrWhiteSpace(href))
                continue;

            var abs = MakeAbsoluteUrl(UnwrapRedirectUrl(href, cur.Host), currentUrl);
            if (!Uri.TryCreate(abs, UriKind.Absolute, out var link)
                || (link.Scheme != "http" && link.Scheme != "https")
                || !link.Host.Equals(cur.Host, StringComparison.OrdinalIgnoreCase)
                || NormalizePath(link.AbsolutePath) != curPath)
                continue;

            el.attribute.TryGetValue("rel", out var rel);
            el.attribute.TryGetValue("aria-label", out var aria);
            el.attribute.TryGetValue("title", out var title);
            var text = (el.text ?? "").Trim().ToLowerInvariant();
            var cls = string.Join(" ", el.className ?? new List<string>()).ToLowerInvariant()
                      + " " + (el.id ?? "").ToLowerInvariant();

            var hintScore =
                (rel?.Contains("next", StringComparison.OrdinalIgnoreCase) == true
                 || aria?.Contains("next", StringComparison.OrdinalIgnoreCase) == true
                 || cls.Contains("pnnext") || cls.Contains("next-page") || cls.Contains("pagination-next")) ? 3
                : (title?.Contains("next", StringComparison.OrdinalIgnoreCase) == true
                   || text is "next" or "next page" or "›" or "»" or ">" or "more results" or "more") ? 2
                : text.Length > 0 && text.Length <= 3 && text.All(char.IsDigit) ? 1
                : 0;

            // Structural check: every differing param must be a new-or-
            // increased numeric (b=1→8, start=0→10, or a new pager param).
            // Non-numeric changes = session tokens/noise — only tolerated on
            // links that also look like pagers.
            var linkParams = ParseQueryParams(link.Query);
            long delta = long.MaxValue; var pagerNamed = false;
            var anyGrown = false; var otherDiffs = 0;
            foreach (var key in linkParams.Keys.Union(curParams.Keys))
            {
                linkParams.TryGetValue(key, out var lv);
                curParams.TryGetValue(key, out var cv);
                if (string.Equals(lv, cv, StringComparison.OrdinalIgnoreCase)) continue;

                if (long.TryParse(lv, out var lnum) && lnum > 0
                    && (cv == null || (long.TryParse(cv, out var cnum) && lnum > cnum)))
                {
                    anyGrown = true;
                    delta = Math.Min(delta, lnum - (cv != null && long.TryParse(cv, out var cc) ? cc : 0));
                    if (PagerParams.Contains(key)) pagerNamed = true;
                    continue;
                }
                // new numeric params equal to 0 (bct=0) are neutral, not diffs
                if (cv == null && long.TryParse(lv, out var z) && z == 0) continue;
                otherDiffs++;
            }

            var isPagerDelta = anyGrown && otherDiffs == 0;
            var isHinted = hintScore > 0 && anyGrown && otherDiffs <= 1;
            if (!isPagerDelta && !isHinted) continue;

            var rank = isPagerDelta ? (pagerNamed ? 0 : 1) : 2;
            candidates.Add((rank, delta == long.MaxValue ? long.MaxValue : delta, hintScore, abs));
        }

        return candidates
            .OrderBy(c => c.Rank)
            .ThenBy(c => c.Delta)
            .ThenByDescending(c => c.Hint)
            .FirstOrDefault().Url;
    }

    private static Dictionary<string, string> ParseQueryParams(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var key = idx > 0 ? pair.Substring(0, idx) : pair;
            var val = idx > 0 ? pair.Substring(idx + 1) : "";
            dict[key] = val;
        }
        return dict;
    }

    private static string NormalizePath(string path)
        => Regex.Replace(path, ";.*$|/+$", "");

    /// <summary>
    /// Execute a Postman-style POST engine: substitute {search} into the
    /// request-body vars, apply header vars, return the response HTML.
    /// </summary>
    private static async Task<string> PostEngineRequestAsync(SearchEngine engine, string query, Action<string>? onDebug)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");

        if (!string.IsNullOrWhiteSpace(engine.HeaderVars))
        {
            try
            {
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(engine.HeaderVars);
                if (headers != null)
                    foreach (var h in headers)
                        client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
            }
            catch { /* malformed header vars — send without them */ }
        }

        var url = engine.Url.Replace("{search}", WebUtility.UrlEncode(query));
        var vars = ParseBodyVars(engine.RequestBodyVars)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Replace("{search}", query));

        HttpResponseMessage response;
        if ((engine.RequestBodyDataType ?? "form").Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(vars);
            response = await client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        }
        else
        {
            response = await client.PostAsync(url, new FormUrlEncodedContent(vars));
        }

        onDebug?.Invoke($"{engine.Name} → POST {url}: {(int)response.StatusCode}");
        if (!response.IsSuccessStatusCode) return "";
        return await response.Content.ReadAsStringAsync();
    }

    private static Dictionary<string, string> ParseBodyVars(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch { return new Dictionary<string, string>(); }
    }

    // Shared client for direct fetches — reused so we don't churn sockets.
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    static SearchHelper()
    {
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        _http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    }

    /// <summary>Fetch a page's raw HTML with a plain HTTP client (no rendering).</summary>
    private static async Task<string> FetchRawHtmlAsync(string url, Action<string>? onDebug = null)
    {
        onDebug?.Invoke($"direct fetch → {url}");
        try
        {
            var resp = await _http.GetAsync(url);
            var html = resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync() : "";
            onDebug?.Invoke($"direct fetch ← HTTP {(int)resp.StatusCode}, {html.Length} chars");
            return html;
        }
        catch (Exception ex)
        {
            onDebug?.Invoke($"direct fetch ← {url} failed: {ex.Message}");
            // .NET's SChannel TLS fingerprint is rejected by some bot walls —
            // curl.exe (OpenSSL signature) ships with Windows and passes.
            return await FetchViaCurlAsync(url, onDebug);
        }
    }

    /// <summary>
    /// Last-resort direct fetch via curl.exe — .NET's SChannel TLS handshake
    /// (JA3 fingerprint) is rejected by some bot walls (DDG returns
    /// "SSL connection could not be established") while curl's OpenSSL
    /// signature passes. curl.exe ships with Windows 10+.
    /// </summary>
    private static async Task<string> FetchViaCurlAsync(string url, Action<string>? onDebug = null)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "curl.exe",
                Arguments = $"-sL --max-time 25 -A \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36\" \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return "";
            var html = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            onDebug?.Invoke($"curl ← {html.Length} chars (exit {proc.ExitCode})");
            return proc.ExitCode == 0 ? html : "";
        }
        catch (Exception ex)
        {
            onDebug?.Invoke($"curl fetch failed: {ex.Message}");
            return "";
        }
    }

    /// <summary>Build an AnalyzedArticle from raw HTML via the Common DOM parser.</summary>
    private static AnalyzedArticle? AnalyzeRawHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        try
        {
            var parser = new Parser(html);
            var article = new AnalyzedArticle();
            article.elements = parser.Elements;
            return article;
        }
        catch { return null; }
    }

    /// <summary>Detect bot-detection/captcha walls in raw HTML responses.</summary>
    private static bool LooksLikeBotWall(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;
        return Regex.IsMatch(html, "(anomaly-modal|captcha|cf-challenge|are you a robot|unusual traffic|verify you are human|before you continue)",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Regex href extraction for raw-HTML engine responses (POST engines
    /// bypass Charlotte, so there's no analyzed DOM to walk).
    /// </summary>
    private static List<string> ExtractLinksFromHtml(string html, string baseUrl, string engineHost, bool allowEngineDomains = false)
    {
        var urls = new List<string>();
        foreach (Match m in Regex.Matches(html, "<a\\s[^>]*href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase))
        {
            var href = m.Groups[1].Value;
            var abs = MakeAbsoluteUrl(href, baseUrl);
            if (IsValidResultUrl(abs, engineHost, allowEngineDomains) && !urls.Contains(abs))
                urls.Add(abs);
        }
        return urls;
    }

    /// <summary>
    /// Scrape a page through the shared downloader + article analyzer and
    /// return its analyzed content. Returns null when the page can't be used.
    /// When a repo is supplied, the DOM is also scanned for the site's own
    /// search form so new engines can be discovered automatically.
    /// </summary>
    public static async Task<AnalyzedArticle?> FetchArticle(
        string url,
        ISearchEnginesRepository? repo = null,
        Action<string>? onDebug = null,
        Func<string, Task<string>>? scriptAnalyzer = null)
    {
        // Direct fetch first (different fingerprint, skips headless bot flags);
        // Charlotte renders the fallback for thin/JS-driven DOMs.
        AnalyzedArticle? article = null;
        var resolvedUrl = url;
        var html = await FetchRawHtmlAsync(url, onDebug);
        if (!string.IsNullOrWhiteSpace(html))
        {
            article = AnalyzeRawHtml(html);
            if (article?.elements == null || article.elements.Count < 40)
            {
                onDebug?.Invoke($"FetchArticle {url}: direct fetch too thin ({article?.elements?.Count ?? 0} elements) — trying Charlotte");
                article = null;
            }
        }

        if (article == null)
        {
            var domJson = Charlotte.Download(url, out resolvedUrl);
            if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("file:") || domJson.StartsWith("Error"))
            {
                onDebug?.Invoke($"FetchArticle {url}: Charlotte returned no usable DOM ({(string.IsNullOrEmpty(domJson) ? "empty" : domJson.Substring(0, Math.Min(120, domJson.Length)))})");
                return null;
            }
            article = Html.DeserializeArticle(domJson);
            Html.GetArticleInfoFromDOM(article);
        }

        if (repo != null && article?.elements != null)
        {
            try
            {
                var page = resolvedUrl ?? url;
                var found = DiscoverSearchEngines(article, page, repo, onDebug);
                if (!found && scriptAnalyzer != null)
                    await DiscoverFromScriptsAsync(article, page, repo, scriptAnalyzer, onDebug);
            }
            catch { /* discovery is best-effort */ }
        }
        return article;
    }

    /// <summary>
    /// Find a real image for a subject: search the configured engines, scrape
    /// the top result pages, and score their images for relevance/quality.
    /// Returns the best image URL + the page it came from, or null.
    /// </summary>
    public static async Task<(string ImageUrl, string SourcePage)?> FetchImageAsync(
        string subject,
        ISearchEnginesRepository? repo,
        Action<int, string>? onProgress = null,
        Action<string>? onDebug = null,
        Func<string, Task<string>>? scriptAnalyzer = null)
    {
        var subjectWords = subject
            .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Select(w => w.ToLowerInvariant())
            .ToList();

        (string Url, int Score)? best = null;
        var triedEngineIds = new HashSet<int>();
        var triedPageUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const int maxRounds = 8; // generous — keeps hunting while progress occurs

        // Persistent loop: query every image engine we have; when none yields
        // a usable image, crawl the web for MORE image engines and retry.
        // Only stops when a real image is found or nothing new is left to try.
        for (var round = 1; round <= maxRounds; round++)
        {
            // Ensure capability — discovers engines when none exist; on later
            // rounds it keeps hunting for MORE of them.
            var enginesBefore = repo?.GetEnabled().Count ?? 0;
            await EnsureEnginesForTypeAsync("image", repo, scriptAnalyzer, onProgress, onDebug,
                findMore: round > 1);
            var discoveredThisRound = Math.Max(0, (repo?.GetEnabled().Count ?? 0) - enginesBefore);

            // --- Query every image engine we haven't tried yet (paginated) ---
            var imageEngines = GetEnabledEngines(repo, "image")
                .Where(e => (e.Type ?? "web").Equals("image", StringComparison.OrdinalIgnoreCase)
                            && triedEngineIds.Add(e.SearchEngineId))
                .ToList();
            if (round == 1)
                onDebug?.Invoke($"Image fetch: {imageEngines.Count} image engine(s) available after capability check");

            foreach (var engine in imageEngines)
            {
                onProgress?.Invoke(30, $"Searching {engine.Name} for images…");
                try
                {
                    var candidates = await ImageEngineSearchAsync(engine, subject, onDebug);
                    onDebug?.Invoke($"{engine.Name}: {candidates.Count} image candidate(s) extracted");
                    foreach (var url in candidates)
                    {
                        var score = ScoreImageCandidate(url, subjectWords);
                        if (score >= 2 && (best == null || score > best.Value.Score))
                            best = (url, score);
                    }
                    if (best?.Score >= 3)
                        return (best.Value.Url, engine.Url);
                }
                catch (Exception ex)
                {
                    onDebug?.Invoke($"{engine.Name} image search failed: {ex.Message}");
                }
            }

            // --- Web fallback: search the subject, follow result links ---
            // Result URLs that ARE images get scored directly; the rest get
            // scraped for embedded images. All links are tried, not just top-3.
            var pageUrls = await SearchUrlsAsync(subject, repo, maxResults: 12,
                contentType: "web", onProgress, onDebug, scriptAnalyzer);
            foreach (var pageUrl in pageUrls)
            {
                if (!triedPageUrls.Add(pageUrl)) continue;
                try
                {
                    // A result that serves image bytes is itself a candidate.
                    if (await IsImageUrlAsync(pageUrl))
                    {
                        var s = ScoreImageCandidate(pageUrl, subjectWords);
                        if (s >= 2 && (best == null || s > best.Value.Score))
                        {
                            best = (pageUrl, s);
                            if (s >= 3) return (pageUrl, pageUrl);
                        }
                        continue;
                    }

                    var article = await FetchArticle(pageUrl, repo, onDebug, scriptAnalyzer);
                    var pageImages = ExtractImagesFromArticle(article, pageUrl);
                    onDebug?.Invoke($"{pageUrl}: {pageImages.Count} image(s) on page");
                    foreach (var abs in pageImages)
                    {
                        var score = ScoreImageCandidate(abs, subjectWords);
                        if (score >= 2 && (best == null || score > best.Value.Score))
                            best = (abs, score);
                    }
                    if (best?.Score >= 3)
                        return (best.Value.Url, pageUrl);
                }
                catch { }
            }

            if (best != null)
            {
                onDebug?.Invoke($"Image fetch: accepting best candidate score {best.Value.Score} — {best.Value.Url}");
                return (best.Value.Url, best.Value.Url);
            }

            // Nothing yet — only keep going if discovery is still productive.
            if (discoveredThisRound == 0 && imageEngines.Count == 0 && round > 1)
            {
                onDebug?.Invoke($"Image fetch: no new engines discovered and no candidates — stopping after round {round}");
                break;
            }
            onDebug?.Invoke($"Image fetch: round {round} done, no usable image — {discoveredThisRound} new engine(s) discovered, retrying");
        }

        onDebug?.Invoke("Image fetch: exhausted all rounds — no image matched the subject strongly enough");
        return null;
    }

    /// <summary>
    /// True when the URL serves image bytes (Content-Type image/*). Result
    /// links sometimes point straight at an image rather than a page.
    /// </summary>
    private static async Task<bool> IsImageUrlAsync(string url)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode
                && (resp.Content.Headers.ContentType?.MediaType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>
    /// Score an image URL for use as a subject photo: rejects non-image files,
    /// icons/logos/tracking pixels, and boosts URLs containing subject words.
    /// </summary>
    private static int ScoreImageCandidate(string url, List<string> subjectWords)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return 0;

        var lower = url.ToLowerInvariant();
        var path = lower.Split('?', '#')[0];

        // Must look like an image file (or a content image endpoint)
        var ext = path.Length > 4 ? path.Substring(path.LastIndexOf('.') + 1) : "";
        var imageExts = new[] { "jpg", "jpeg", "png", "webp", "gif" };
        if (!imageExts.Contains(ext) && !lower.Contains("/image"))
            return 0;
        if (ext == "svg" || ext == "gif")
            return 0;

        // Reject chrome/junk
        var junk = new[]
        {
            "icon", "logo", "sprite", "favicon", "avatar", "badge", "spacer",
            "pixel", "1x1", "tracking", "doubleclick", "gravatar", "emoji",
            "smiley", "blank", "transparent", "loader", "spinner", "button",
            "/assets/", "template", "landing-page", "landing_", "faq",
            "placeholder", "dummy", "noimage", "no-image", "/static/",
            "default-", "og-image", "opengraph", "share-image", "card-image"
        };
        if (junk.Any(j => lower.Contains(j)))
            return 0;

        var score = 1;
        var fileName = path.Substring(path.LastIndexOf('/') + 1);
        score += subjectWords.Count(w => fileName.Contains(w));
        // Wikipedia/Commons originals and CDN paths tend to be real photos
        if (lower.Contains("upload.wikimedia.org") || lower.Contains("wikimedia"))
            score += 2;
        return score;
    }

    /// <summary>
    /// Extract URLs from a search result page, filtering engine chrome.
    /// </summary>
    public static List<string> ExtractResultUrls(AnalyzedArticle article, SearchEngine engine, bool allowEngineDomains = false)
    {
        var urls = new List<string>();

        if (article?.elements == null)
        {
            return urls;
        }

        // Engine's own host — skip self-referential links (navigation, help, settings)
        var engineHost = "";
        if (Uri.TryCreate(engine.Url.Replace("{search}", "x"), UriKind.Absolute, out var engineUri))
        {
            engineHost = engineUri.Host;
        }

        foreach (var element in article.elements)
        {
            if (element?.tagName != "a" || element.attribute == null)
            {
                continue;
            }

            if (element.attribute.TryGetValue("href", out var href))
            {
                var url = MakeAbsoluteUrl(UnwrapRedirectUrl(href?.ToString() ?? "", engineHost), engine.Url.Replace("{search}", "x"));

                if (IsValidResultUrl(url, engineHost, allowEngineDomains))
                {
                    url = CleanResultUrl(url);
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
    /// Engines wrap result links in redirect URLs — DuckDuckGo's uddg=,
    /// Google's /url?q= — which look like self-domain links and get filtered.
    /// Unwrap the real destination before validation.
    /// </summary>
    private static string UnwrapRedirectUrl(string url, string engineHost)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        // DOM parsers can leave &amp; entities in hrefs — decode them first
        // so query params actually separate.
        var decoded = WebUtility.UrlDecode(WebUtility.HtmlDecode(url));

        // Bing wraps every result as bing.com/ck/a?...&u=a1<base64url(real-url)>
        var bingU = Regex.Match(decoded, @"[?&]u=(a1[^&]+)");
        if (bingU.Success)
        {
            var b64 = bingU.Groups[1].Value.Substring(2)
                .Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4) { case 2: b64 += "=="; break; case 3: b64 += "="; break; }
            try
            {
                var target = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                if (target.StartsWith("http://") || target.StartsWith("https://"))
                    return target;
            }
            catch { }
        }

        foreach (Match m in Regex.Matches(decoded, @"(?:uddg|q|u|url|ru|target|dest|redirect)=([^&]+)", RegexOptions.IgnoreCase))
        {
            var candidate = WebUtility.UrlDecode(m.Groups[1].Value);
            if ((candidate.StartsWith("http://") || candidate.StartsWith("https://"))
                && (string.IsNullOrEmpty(engineHost)
                    || !candidate.Contains(engineHost, StringComparison.OrdinalIgnoreCase)))
                return candidate;
        }
        return url;
    }

    /// <summary>
    /// Check if URL is a valid search result (not sponsored, not a navigation link, etc.)
    /// </summary>
    private static bool IsValidResultUrl(string url, string engineHost, bool allowEngineDomains = false)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var resultUri))
            return false;
        var resultHost = resultUri.Host;

        if (!string.IsNullOrEmpty(engineHost))
        {
            if (allowEngineDomains)
            {
                // Meta-search: reject links back to the engine's registrable
                // domain — Yahoo nav (travel/autos/local/login.yahoo.com) is
                // noise; the engines we're hunting live on OTHER domains.
                var engineBase = BaseDomain(engineHost);
                if (resultHost.Equals(engineBase, StringComparison.OrdinalIgnoreCase)
                    || resultHost.EndsWith("." + engineBase, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else
            {
                var engineDomain = engineHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                    ? engineHost.Substring(4)
                    : engineHost;
                if (resultHost.Equals(engineDomain, StringComparison.OrdinalIgnoreCase)
                    || resultHost.EndsWith("." + engineDomain, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        // Skip common non-result URLs
        var invalidPatterns = new[]
        {
            "/search?", "/images?", "/maps?", "/news?", "/videos?", "/shopping?",
            "/books?", "/flights?", "/finance?", "/advanced_search?",
            "/preferences?", "/support?", "/tools?", "/about?", "/help?",
            "javascript:", "#", "/ads?", "/aclk?", "/url?sa=", "sponsored",
            "adurl=", "/sh/aclk", "/prbadclk", "/shopping/", "login.", "signin"
        };

        foreach (var pattern in invalidPatterns)
        {
            if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Skip major search engine / social domains — except during
        // meta-search, where those domains ARE what we're hunting for.
        if (allowEngineDomains)
            return true;

        var searchDomains = new[]
        {
            "google.com", "google.co.", "bing.com", "duckduckgo.com",
            "yahoo.com", "baidu.com", "yandex.com", "facebook.com",
            "twitter.com", "instagram.com", "tiktok.com", "pinterest.com",
            "reddit.com"
        };

        foreach (var domain in searchDomains)
        {
            if (url.Contains(domain, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return url.StartsWith("http://") || url.StartsWith("https://");
    }

    /// <summary>Last-two-labels host (yahoo.com for search.yahoo.com) — registrable-domain approximation.</summary>
    private static string BaseDomain(string host)
    {
        var h = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host.Substring(4) : host;
        var parts = h.Split('.');
        return parts.Length >= 2 ? parts[^2] + "." + parts[^1] : h;
    }

    /// <summary>Clean tracking params off a result URL.</summary>
    private static string CleanResultUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "";
        }

        url = Regex.Replace(url, @"[?&](utm_|ref_|source|medium|campaign|gclid|fbclid|msclkid)=[^&]*", "");

        if (url.EndsWith("?"))
        {
            url = url.TrimEnd('?');
        }

        return url;
    }

    /// <summary>Deduplicate URLs by domain, preserving order.</summary>
    public static List<string> FilterByDomain(List<string> urls)
    {
        var seenDomains = new HashSet<string>();
        var filtered = new List<string>();

        foreach (var url in urls)
        {
            try
            {
                var uri = new Uri(url);
                var domain = uri.Host.ToLowerInvariant();
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

    // Input names that almost always carry the search term on real sites.
    private static readonly HashSet<string> SearchInputNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "q", "query", "search", "s", "keyword", "keywords", "searchterm",
        "search_term", "searchquery", "search_query", "searchtext",
        "search_text", "term", "find", "lookup"
    };

    /// <summary>
    /// Scan an analyzed DOM for a usable search form: an &lt;input type="search"&gt;
    /// or an input named q/query/search/etc. inside a &lt;form&gt;. When found,
    /// derive a Postman-style engine definition (method, URL template, body
    /// vars, headers) and persist it if we don't already know an engine for
    /// that host. Discovery is silent — it never throws.
    /// </summary>
    public static bool DiscoverSearchEngines(
        AnalyzedArticle article,
        string pageUrl,
        ISearchEnginesRepository repo,
        Action<string>? onDebug = null)
    {
        if (article?.elements == null || !Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri))
            return false;

        // Index elements by their position in the list for parent lookups.
        var elements = article.elements;
        var candidatesSeen = 0;

        for (int i = 0; i < elements.Count; i++)
        {
            var input = elements[i];
            if (input?.tagName != "input" || input.attribute == null)
                continue;

            var inputType = input.attribute.TryGetValue("type", out var t) ? t : "text";
            var inputName = input.attribute.TryGetValue("name", out var n) ? n : null;
            var inputId = input.attribute.TryGetValue("id", out var idAttr) ? idAttr : "";
            var placeholder = input.attribute.TryGetValue("placeholder", out var ph) ? ph : "";

            var looksLikeSearch =
                inputType.Equals("search", StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(inputName) && SearchInputNames.Contains(inputName))
                || inputId.Contains("search", StringComparison.OrdinalIgnoreCase)
                || placeholder.Contains("search", StringComparison.OrdinalIgnoreCase);

            if (!looksLikeSearch || string.IsNullOrWhiteSpace(inputName))
                continue;

            candidatesSeen++;
            onDebug?.Invoke($"Discovery: search-like input found (name={inputName}, type={inputType}) on {pageUrl}");

            // Walk ancestors for the enclosing <form>.
            DomElement? form = null;
            var cursor = input;
            var hops = 0;
            while (cursor.parent >= 0 && cursor.parent < elements.Count && hops++ < 20)
            {
                cursor = elements[cursor.parent];
                if (cursor?.tagName == "form") { form = cursor; break; }
            }
            if (form?.attribute == null)
            {
                onDebug?.Invoke("Discovery: input has no enclosing <form> — likely JS-driven");
                continue;
            }

            // Upload forms (reverse-image-search, file pickers) can't be
            // driven by a text query — skip them as engines.
            var formHasFileInput = elements.Any(e => e?.tagName == "input" && e.attribute != null
                && e.attribute.TryGetValue("type", out var ft)
                && ft.Equals("file", StringComparison.OrdinalIgnoreCase)
                && DescendsFrom(e, form, elements));
            if (formHasFileInput)
            {
                onDebug?.Invoke("Discovery: form contains a file-upload input — can't accept a text query, skipped");
                continue;
            }

            var action = form.attribute.TryGetValue("action", out var a) ? a : pageUri.GetLeftPart(UriPartial.Path);
            var method = form.attribute.TryGetValue("method", out var m) && m.Equals("post", StringComparison.OrdinalIgnoreCase)
                ? "POST" : "GET";
            var actionAbs = MakeAbsoluteUrl(action, pageUrl);
            if (!Uri.TryCreate(actionAbs, UriKind.Absolute, out var actionUri)
                || (actionUri.Scheme != "http" && actionUri.Scheme != "https"))
            {
                onDebug?.Invoke($"Discovery: form action '{action}' didn't resolve to a usable URL");
                continue;
            }

            // Collect sibling inputs: the search field becomes {search},
            // hidden fields become static body vars / query defaults.
            var bodyVars = new Dictionary<string, string>();
            var queryParts = new List<string>();
            foreach (var el in elements.Where(e => e?.tagName == "input" && e.attribute != null))
            {
                if (!DescendsFrom(el, form, elements)) continue;

                var name = el.attribute.TryGetValue("name", out var nm) ? nm : null;
                if (string.IsNullOrEmpty(name)) continue;
                var val = name == inputName
                    ? "{search}"
                    : (el.attribute.TryGetValue("value", out var v) ? v ?? "" : "");

                if (method == "POST") bodyVars[name] = val;
                else queryParts.Add($"{name}={val}");
            }

            string url;
            if (method == "POST")
            {
                url = actionAbs;
                if (!queryParts.Any(p => p.Contains("{search}")) && !bodyVars.Values.Any(v => v.Contains("{search}")))
                {
                    onDebug?.Invoke($"Discovery: form at {actionAbs} has no field to carry the search term — skipped");
                    continue; // no way to inject the term — not a usable engine
                }
            }
            else
            {
                if (!queryParts.Any(p => p.Contains("{search}")))
                {
                    onDebug?.Invoke($"Discovery: GET form at {actionAbs} produced no {{search}} param — skipped");
                    continue;
                }
                url = actionAbs + (actionAbs.Contains('?') ? "&" : "?") + string.Join("&", queryParts);
            }

            var engineType = InferEngineType(url, pageUrl);
            if (repo.ExistsForHostAndType(actionUri.Host, engineType))
            {
                onDebug?.Invoke($"Discovery: engine for {actionUri.Host} (type={engineType}) already known — skipped");
                continue;
            }

            var engine = new SearchEngine
            {
                Name = $"{actionUri.Host} {engineType} search",
                Url = url,
                Type = engineType,
                Method = method,
                RequestBodyVars = method == "POST" && bodyVars.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(bodyVars) : null,
                RequestBodyDataType = "form",
                DiscoveredFrom = pageUrl,
                Sort = 900, // discovered engines rank below curated ones until promoted
                Enabled = true
            };
            repo.Add(engine);
            onDebug?.Invoke($"Discovered search engine: {engine.Name} ({engine.Method} {engine.Url})");
            return true; // one engine per page is plenty
        }

        if (candidatesSeen == 0)
            onDebug?.Invoke($"Discovery: no search-like inputs among {elements.Count} elements on {pageUrl}");
        return false;
    }

    /// <summary>True when el's parent chain reaches ancestor within the element list.</summary>
    private static bool DescendsFrom(DomElement el, DomElement ancestor, List<DomElement> elements)
    {
        var cursor = el; var depth = 0;
        while (cursor.parent >= 0 && cursor.parent < elements.Count && depth++ < 20)
        {
            cursor = elements[cursor.parent];
            if (cursor == ancestor) return true;
        }
        return false;
    }

    /// <summary>
    /// Tier-2 discovery: no &lt;form&gt; was found, so the site's search likely
    /// runs through JavaScript. Charlotte gives us DOM JSON only — script
    /// bodies aren't executed — so we collect inline script text plus the
    /// first few external .js files, pre-filter for fetch/XHR/search-like
    /// call sites, and ask the LLM to identify the real endpoint, method
    /// and query parameter. The result becomes a Postman-style engine entry.
    /// </summary>
    private static async Task<bool> DiscoverFromScriptsAsync(
        AnalyzedArticle article,
        string pageUrl,
        ISearchEnginesRepository repo,
        Func<string, Task<string>> scriptAnalyzer,
        Action<string>? onDebug = null)
    {
        var inline = new List<string>();
        var srcUrls = new List<string>();
        foreach (var el in article.elements.Where(e => e?.tagName == "script"))
        {
            if (el.attribute?.TryGetValue("src", out var src) == true && !string.IsNullOrWhiteSpace(src))
                srcUrls.Add(MakeAbsoluteUrl(src, pageUrl));
            else if (!string.IsNullOrWhiteSpace(el.text))
                inline.Add(el.text);
        }

        // Download external scripts — app/bundle/search-named files first,
        // capped so we never pull megabytes of bundles into the prompt.
        var candidates = new List<string>();
        candidates.AddRange(inline.Take(4));

        var preferred = srcUrls
            .OrderBy(u => Regex.IsMatch(u, "(app|main|bundle|search|index)", RegexOptions.IgnoreCase) ? 0 : 1)
            .Take(5);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        foreach (var src in preferred)
        {
            try
            {
                var js = await http.GetStringAsync(src);
                if (!string.IsNullOrWhiteSpace(js))
                    candidates.Add(js.Length > 300_000 ? js[..300_000] : js);
            }
            catch { /* unreachable script — skip */ }
        }

        // Pre-filter: only code containing request/search call sites is worth
        // spending an LLM call on. Extract ±600 chars around each hit.
        var markers = new[] { "fetch(", "XMLHttpRequest", "axios", "/search", "autocomplete", "suggest", "?q=", "q%3D" };
        var snippets = new List<string>();
        foreach (var js in candidates)
        {
            foreach (var marker in markers)
            {
                int idx = 0, hits = 0;
                while ((idx = js.IndexOf(marker, idx, StringComparison.OrdinalIgnoreCase)) >= 0 && hits++ < 4)
                {
                    var start = Math.Max(0, idx - 600);
                    var len = Math.Min(1400, js.Length - start);
                    snippets.Add(js.Substring(start, len));
                    idx += marker.Length;
                    if (snippets.Count >= 12) break;
                }
                if (snippets.Count >= 12) break;
            }
            if (snippets.Count >= 12) break;
        }
        if (snippets.Count == 0)
        {
            onDebug?.Invoke($"JS discovery: {inline.Count} inline + {srcUrls.Count} external scripts on {pageUrl}, none contained search call sites");
            return false;
        }
        onDebug?.Invoke($"JS discovery: {snippets.Count} search-like code snippet(s) on {pageUrl} — asking LLM to identify the endpoint");

        var pageHost = Uri.TryCreate(pageUrl, UriKind.Absolute, out var pu) ? pu.Host : "";
        var prompt = $$"""
            You are analyzing JavaScript from the website {{pageHost}} (page: {{pageUrl}}).
            The site has a search feature but no HTML <form> — it calls a search
            endpoint from JavaScript. From the code snippets below, identify the
            endpoint used for site search.

            Respond with ONLY valid JSON:
            {
              "found": true/false,
              "url": "the search endpoint URL (relative paths OK, e.g. /api/search)",
              "method": "GET or POST",
              "queryParam": "the parameter/property name carrying the search term",
              "extraParams": {"name": "static-value"},
              "contentType": "form or json",
              "evidence": "one line explaining how you know this is the search endpoint"
            }
            Set "found": false if no plausible site-search endpoint exists
            (analytics/tracking/auth endpoints do not count).

            JavaScript snippets:
            {{string.Join("\n---\n", snippets)}}
            """;

        var response = await scriptAnalyzer(prompt);
        var json = LLMJson.Extract(response);
        if (string.IsNullOrWhiteSpace(json))
        {
            onDebug?.Invoke("JS discovery: LLM returned no usable JSON");
            return false;
        }

        string endpoint, method, queryParam, contentType;
        Dictionary<string, string> extra = new();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("found", out var f) || f.ValueKind != System.Text.Json.JsonValueKind.True)
            {
                onDebug?.Invoke("JS discovery: LLM found no plausible search endpoint in the snippets");
                return false;
            }
            endpoint = root.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
            method = root.TryGetProperty("method", out var m) && m.GetString()?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true
                ? "POST" : "GET";
            queryParam = root.TryGetProperty("queryParam", out var qp) ? qp.GetString() ?? "" : "";
            contentType = root.TryGetProperty("contentType", out var ct) && ct.GetString()?.Equals("json", StringComparison.OrdinalIgnoreCase) == true
                ? "json" : "form";
            if (root.TryGetProperty("extraParams", out var ep) && ep.ValueKind == System.Text.Json.JsonValueKind.Object)
                foreach (var p in ep.EnumerateObject())
                    extra[p.Name] = p.Value.ValueKind == System.Text.Json.JsonValueKind.String ? p.Value.GetString() ?? "" : p.Value.ToString();
        }
        catch { return false; }

        var endpointAbs = MakeAbsoluteUrl(endpoint, pageUrl);
        if (string.IsNullOrWhiteSpace(queryParam)
            || !Uri.TryCreate(endpointAbs, UriKind.Absolute, out var endpointUri)
            || (endpointUri.Scheme != "http" && endpointUri.Scheme != "https"))
        {
            onDebug?.Invoke($"JS discovery: LLM endpoint unusable (url='{endpoint}', param='{queryParam}')");
            return false;
        }
        var engineType = InferEngineType(endpointAbs, pageUrl);
        if (repo.ExistsForHostAndType(endpointUri.Host, engineType))
            return false;

        string engineUrl;
        string? bodyVars = null;
        if (method == "POST")
        {
            engineUrl = endpointAbs;
            var vars = new Dictionary<string, string>(extra) { [queryParam] = "{search}" };
            bodyVars = System.Text.Json.JsonSerializer.Serialize(vars);
        }
        else
        {
            var parts = new List<string> { $"{queryParam}={{search}}" };
            parts.AddRange(extra.Select(kv => $"{kv.Key}={kv.Value}"));
            engineUrl = endpointAbs + (endpointAbs.Contains('?') ? "&" : "?") + string.Join("&", parts);
        }

        repo.Add(new SearchEngine
        {
            Name = $"{endpointUri.Host} {engineType} search (js)",
            Url = engineUrl,
            Type = engineType,
            Method = method,
            RequestBodyVars = bodyVars,
            RequestBodyDataType = contentType,
            DiscoveredFrom = pageUrl,
            Sort = 900,
            Enabled = true
        });
        onDebug?.Invoke($"Discovered JS search engine: {endpointUri.Host} ({method} {engineUrl})");
        return true;
    }

    /// <summary>
    /// Infer an engine's content type from its resolved search URL and the
    /// page it was discovered on. URL structure carries the signal — e.g.
    /// bing.com/images/search, ?tbm=isch, duckduckgo.com/?iax=images.
    /// </summary>
    private static string InferEngineType(string engineUrl, string pageUrl)
    {
        var probe = (engineUrl ?? "").ToLowerInvariant();

        // Unambiguous endpoint params — checked against the engine URL only.
        if (probe.Contains("tbm=isch") || probe.Contains("udm=2")
            || probe.Contains("iax=images") || probe.Contains("ia=images"))
            return "image";
        if (probe.Contains("tbm=vid")) return "video";
        if (probe.Contains("tbm=nws")) return "news";

        // Engine URL structure — host/path markers only. The discovering
        // page's URL must NOT contribute: an article titled "best image
        // search engines" would otherwise poison every form found on it.
        if (Regex.IsMatch(probe, @"(images?\.|/images?([/?#.]|$)|imghp|reverse-image|tineye|photo-search|pixabay|unsplash|shutterstock|gettyimages|flickr)"))
            return "image";
        if (Regex.IsMatch(probe, @"(youtubei|youtube|vimeo|dailymotion|twitch|/videos?[/?.])"))
            return "video";
        if (Regex.IsMatch(probe, @"(/news[/?.]|news\.)"))
            return "news";
        if (Regex.IsMatch(probe, @"(torrent|magnet)"))
            return "torrent";
        if (Regex.IsMatch(probe, @"(wiki|fandom)"))
            return "wiki";
        if (Regex.IsMatch(probe, @"(scholar|arxiv|pubmed|sci-hub|jstor|doi\.org)"))
            return "science-journal";

        // Last resort — the discovering page's HOST can hint at the site type
        // (a form on youtube.com is a video search), but never its path: page
        // titles like "best-image-search-engines.html" are not evidence.
        if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri))
        {
            var host = pageUri.Host.ToLowerInvariant();
            if (Regex.IsMatch(host, @"(youtube|vimeo|dailymotion|twitch|tiktok)")) return "video";
            if (Regex.IsMatch(host, @"(flickr|pixabay|unsplash|tineye|shutterstock|gettyimages|imgur|pinterest)")) return "image";
            if (Regex.IsMatch(host, @"(scholar|arxiv|pubmed|jstor)")) return "science-journal";
            if (Regex.IsMatch(host, @"(torrent|piratebay|1337x)")) return "torrent";
            if (Regex.IsMatch(host, @"(wikipedia|fandom|wiki)")) return "wiki";
        }

        return "web";
    }

    /// <summary>
    /// Ensure at least one enabled engine exists for a content type. When the
    /// database has none, meta-search the web engines for engines of that
    /// type ("image search engine", etc.), scrape the top results, and let
    /// form/JS discovery learn their search interfaces — the newly added
    /// engines are then available for the real query. Returns true when a
    /// suitable engine exists (or was discovered).
    /// </summary>
    public static async Task<bool> EnsureEnginesForTypeAsync(
        string contentType,
        ISearchEnginesRepository? repo,
        Func<string, Task<string>>? scriptAnalyzer = null,
        Action<int, string>? onProgress = null,
        Action<string>? onDebug = null,
        bool findMore = false)
    {
        if (repo == null || string.IsNullOrWhiteSpace(contentType) || contentType == "web")
            return true; // web engines are seeded; nothing to ensure

        bool HasType() =>
            repo.GetEnabled().Any(e => (e.Type ?? "web").Equals(contentType, StringComparison.OrdinalIgnoreCase));

        try
        {
            // findMore mode keeps crawling even when engines exist — the
            // image fetcher calls it again when existing engines yielded no
            // usable image.
            if (!findMore && HasType()) return true;
        }
        catch { return true; } // repo read failed — don't block the fetch on it

        int TypeCount() => repo.GetEnabled().Count(e => (e.Type ?? "web").Equals(contentType, StringComparison.OrdinalIgnoreCase));
        var typeBaseline = 0;
        try { typeBaseline = TypeCount(); } catch { }
        bool MadeProgress() { try { return TypeCount() > typeBaseline; } catch { return false; } }

        onProgress?.Invoke(15, findMore
            ? $"Existing {contentType} engines yielded nothing — searching the web for more…"
            : $"No {contentType} search engines configured — searching the web for one…");

        // Never probe non-HTML resources — a CSS/JS asset can't contain a form.
        var staticExt = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico", ".woff", ".json", ".xml" };
        bool IsHtmlPage(string u) => !staticExt.Any(e => u.Split('?', '#')[0].EndsWith(e, StringComparison.OrdinalIgnoreCase));

        // Persistent crawl: query variants × every enabled engine × result
        // pagination. Every result URL gets queued for probing, and outbound
        // links that look like {type} engines jump the queue. We don't stop
        // until a {type} engine exists or the crawl budget is exhausted.
        var queries = new[]
        {
            $"best {contentType} search engine",
            $"{contentType} search engines list",
            $"free {contentType} search sites"
        };
        var pending = new Queue<string>();       // result links
        var typedPending = new Queue<string>();  // outbound links matching the type
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int probes = 0, probeBudget = 30;

        async Task<bool> ProbeNext()
        {
            while (probes < probeBudget
                && (typedPending.Count > 0 || pending.Count > 0))
            {
                var url = typedPending.Count > 0 ? typedPending.Dequeue() : pending.Dequeue();
                probes++;
                try
                {
                    onDebug?.Invoke($"Probing {url} for a {contentType} search interface…");
                    var article = await FetchArticle(url, repo, onDebug, scriptAnalyzer);
                    if (MadeProgress()) return true;

                    // Two-hop: pages ABOUT engines link to the engines —
                    // queue outbound links that look like {type} engines.
                    if (article?.elements != null)
                    {
                        foreach (var el in article.elements)
                        {
                            if (el?.tagName != "a" || el?.attribute == null
                                || !el.attribute.TryGetValue("href", out var href)
                                || string.IsNullOrWhiteSpace(href)) continue;
                            var abs = MakeAbsoluteUrl(href, url);
                            if (abs.StartsWith("http") && IsHtmlPage(abs)
                                && InferEngineType(abs, "") == contentType && seen.Add(abs))
                            {
                                typedPending.Enqueue(abs);
                                onDebug?.Invoke($"Queued {contentType}-like link: {abs}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    onDebug?.Invoke($"{url}: probe threw — {ex.Message}");
                }
            }
            return MadeProgress();
        }

        var webEngines = GetEnabledEngines(repo, "web");
        foreach (var query in queries)
        {
            foreach (var engine in webEngines)
            {
                // Page 1, then follow the SERP's own "next page" link — no
                // pager guessing; whatever the engine links to is the next page.
                string? nextUrl = null;
                for (var page = 1; page <= 6; page++)
                {
                    List<string> fresh;
                    string? following = null;
                    try
                    {
                        var (urls, article) = await SearchEnginePageAsync(engine, query, onDebug,
                            repo, scriptAnalyzer, allowEngineDomains: true, urlOverride: nextUrl);
                        fresh = urls.Where(u => IsHtmlPage(u) && seen.Add(u)).ToList();
                        foreach (var u in fresh) pending.Enqueue(u);
                        var pageKey = nextUrl ?? engine.Url.Replace("{search}", query);
                        following = FindNextPageUrl(article, pageKey);
                        if (following != null && !seen.Add("__page__" + following))
                            following = null; // already visited this page
                    }
                    catch (Exception ex)
                    {
                        onDebug?.Invoke($"{engine.Name} page {page} failed: {ex.Message}");
                        break;
                    }

                    onDebug?.Invoke($"{engine.Name} '{query}' page {page}: {fresh.Count} new result url(s) — {pending.Count} queued, {typedPending.Count} typed");

                    if (await ProbeNext()) return true;

                    if (fresh.Count == 0 || following == null) break; // engine exhausted for this query
                    nextUrl = following;
                    onDebug?.Invoke($"{engine.Name}: next page → {nextUrl}");
                }
                if (MadeProgress()) return true;
            }
            if (MadeProgress()) return true;
        }

        // Drain whatever is still queued before declaring failure.
        if (await ProbeNext()) return true;

        var found = HasType();
        onDebug?.Invoke(found
            ? $"Engine discovery succeeded for type '{contentType}'"
            : $"Engine discovery exhausted after {probes} probes — no {contentType} engine could be learned");
        return found;
    }

    /// <summary>
    /// Query an image-type engine and extract direct image URLs from its
    /// result page: img element srcs, media-url query params embedded in
    /// result links, and analyzer-detected images.
    /// </summary>
    private static async Task<List<string>> ImageEngineSearchAsync(
        SearchEngine engine, string query, Action<string>? onDebug)
    {
        var images = new List<string>();

        if ((engine.Method ?? "GET").Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var html = await PostEngineRequestAsync(engine, query, onDebug);
            if (string.IsNullOrWhiteSpace(html)) return images;
            var baseUrl = engine.Url;
            foreach (Match m in Regex.Matches(html, "(?:src|data-src)=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase))
            {
                var abs = MakeAbsoluteUrl(m.Groups[1].Value, baseUrl);
                if (!images.Contains(abs)) images.Add(abs);
            }
            foreach (Match m in Regex.Matches(html, "(?:imgurl|mediaurl|murl|ou)=([^&\"']+)", RegexOptions.IgnoreCase))
            {
                var dec = WebUtility.UrlDecode(m.Groups[1].Value);
                if (!images.Contains(dec)) images.Add(dec);
            }
            return images;
        }

        // Follow the SERP's own next-page link for up to 3 pages — an image
        // SERP's first page may be all chrome/sponsored tiles.
        var searchUrl = engine.Url.Replace("{search}", WebUtility.UrlEncode(query));
        var seenPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var pageNum = 1; pageNum <= 3 && !string.IsNullOrEmpty(searchUrl) && seenPages.Add(searchUrl); pageNum++)
        {
            AnalyzedArticle? article = null;
            var page = searchUrl;
            var raw = await FetchRawHtmlAsync(searchUrl, onDebug);
            if (!string.IsNullOrWhiteSpace(raw))
                article = AnalyzeRawHtml(raw);
            if (article?.elements == null || article.elements.Count < 40)
            {
                onDebug?.Invoke($"{engine.Name} → Charlotte: {searchUrl}");
                var domJson = Charlotte.Download(searchUrl, out var resolvedUrl);
                if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("file:") || domJson.StartsWith("Error"))
                    break;
                article = Html.DeserializeArticle(domJson);
                Html.GetArticleInfoFromDOM(article);
                page = resolvedUrl ?? searchUrl;
            }

            foreach (var img in ExtractImagesFromArticle(article, page))
                if (!images.Contains(img)) images.Add(img);

            var next = FindNextPageUrl(article, page);
            if (next == null || next == searchUrl) break;
            searchUrl = next;
            onDebug?.Invoke($"{engine.Name}: image results page {pageNum + 1} → {next}");
        }

        return images;
    }

    /// <summary>
    /// Pull image URLs out of an analyzed article: &lt;img&gt; element srcs
    /// (incl. data-src/srcset lazy-loads), image URLs embedded in link query
    /// params (imgurl=/mediaurl=/murl=/ou=), and analyzer-detected images.
    /// Works on both Charlotte-analyzed and raw-Parser articles.
    /// </summary>
    private static List<string> ExtractImagesFromArticle(AnalyzedArticle? article, string page)
    {
        var images = new List<string>();
        if (article?.elements != null)
        {
            foreach (var el in article.elements)
            {
                if (el?.tagName != "img" || el.attribute == null) continue;
                var src = el.attribute.TryGetValue("src", out var s) ? s
                        : el.attribute.TryGetValue("data-src", out var ds) ? ds : null;
                if (string.IsNullOrWhiteSpace(src) && el.attribute.TryGetValue("srcset", out var ss))
                    src = ss?.Split(',')?.FirstOrDefault()?.Trim()?.Split(' ')?.FirstOrDefault();
                var abs = MakeAbsoluteUrl(src ?? "", page);
                if (!string.IsNullOrWhiteSpace(abs) && !images.Contains(abs)) images.Add(abs);
            }

            // Result links carrying the real image in a query param
            // (Google's imgurl=, Bing's mediaurl=/murl=)
            foreach (var el in article.elements)
            {
                if (el?.tagName != "a" || el?.attribute == null
                    || !el.attribute.TryGetValue("href", out var href)
                    || string.IsNullOrWhiteSpace(href)) continue;
                foreach (Match m in Regex.Matches(href, "(?:imgurl|mediaurl|murl|ou)=([^&]+)", RegexOptions.IgnoreCase))
                {
                    var dec = WebUtility.UrlDecode(m.Groups[1].Value);
                    if (!images.Contains(dec)) images.Add(dec);
                }
            }
        }

        // Analyzer-detected images as a final source (Charlotte path only)
        if (article?.images != null)
            foreach (var img in article.images.Where(i => !string.IsNullOrWhiteSpace(i.url)))
            {
                var abs = MakeAbsoluteUrl(img.url, page);
                if (!images.Contains(abs)) images.Add(abs);
            }

        return images;
    }

    /// <summary>Resolve a possibly-relative URL against the page it came from.</summary>
    public static string MakeAbsoluteUrl(string href, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(href)) return href;
        if (href.StartsWith("//")) return "https:" + href;
        if (href.StartsWith("/") && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return new Uri(baseUri, href).ToString();
        }
        return href;
    }
}
