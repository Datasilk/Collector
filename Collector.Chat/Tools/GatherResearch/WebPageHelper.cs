using Collector.Chat.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Common.DOM;
using Collector.Common.Models;
using Collector.Common.Models.Articles;

namespace Collector.Chat.Tools.GatherResearch;

/// <summary>
/// Generic web page extractor — the terminal fallback. Downloads through the
/// shared pipeline (Common.Article.Download → redirect/retry/SSL handling →
/// Charlotte browser fallback → Html.DeserializeArticle article analyzer)
/// and classifies the result into text, headers, images, videos and files.
/// </summary>
public class WebPageHelper : IResearchSourceHelper
{
    /// <summary>Minimum real pixel dimension (width or height) for a kept image.</summary>
    private const int MinImageWidth = 640;
    /// <summary>Every dump image is downloaded + identified — cap the checks per page.</summary>
    private const int MaxImageChecks = 10;
    /// <summary>Stop downloading once this many images are verified — a gallery never needs more.</summary>
    private const int EnoughImages = 10;
    /// <summary>Fewer elements than this almost certainly means a stub
    /// interstitial, not a real page (PMC's cookie-required shell is ~19).</summary>
    private const int ThinDomElements = 60;
    /// <summary>Raster images under this size are icons/spacers — a real
    /// 640px photo almost never compresses below 8 KB.</summary>
    private const int MinImageBytes = 8192;
    /// <summary>Cleaned body text under this length is a rendered shell
    /// (bot-wall, cookie interstitial) even when the element count looks
    /// fine — matches the integration gate's thin-page floor.</summary>
    private const int ThinBodyChars = 200;
    private static readonly System.Text.RegularExpressions.Regex ThinTextRegex = new(
        @"<[^>]+>", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>UI-chrome URL markers — directory segments or filename tokens
    /// that signal icons, avatars, profile photos, sprites and social/chatbot
    /// assets. Such files can still clear the 640px floor (an SVG viewBox or a
    /// large headshot) but never belong in an article gallery.</summary>
    private static readonly System.Text.RegularExpressions.Regex ChromeImageUrl = new(
        @"\.(svg|ico)($|[?#])" +
        @"|/(chatbots?|icons?|avatars?|profile[_-]?photos?|profiles?|headshots?|bylines?|authors?|contributors?|sprites?|logos?|favicons?|gravatars?|mastheads?|wordmarks?|social[_-]?media|badges?|emojis?|smilies)/" +
        @"|[/_\-.](icon|logo|avatar|headshot|byline|profile|sprite|badge|button|arrow|spinner|loader|favicon|masthead|chatbot|wordmark|emoji|share)[/_\-.]",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Fallback helper — matches everything; must stay last in the registry.</summary>
    public bool Match(string url) => true;

    public async Task<ResearchPage?> ExtractAsync(
        string url,
        Action<int, string>? onProgress = null,
        CancellationToken cancellationToken = default,
        Guid? appUserId = null,
        Func<string, CancellationToken, Task<string>>? requestCookies = null,
        Func<string, CancellationToken, Task<string?>>? htmlParser = null,
        Collector.Data.Interfaces.ISearchEnginesRepository? searchEnginesRepo = null,
        Func<string, Task<string>>? scriptAnalyzer = null)
    {
        ChatDetailLog.Fn("web-scrape", "WebPageHelper.ExtractAsync");
        // Download via the shared downloader pipeline — same path
        // DownloadWorker uses: HEAD/GET handling, redirects, retries,
        // SSL failure detection, Charlotte browser fallback, file detection.
        string resolvedUrl = url;
        var downloadFailed = false;
        var domJson = await Task.Run(() => Common.Article.Download(url, out resolvedUrl,
            onRedirect: (redirectUrl, status) =>
            {
                if (status > 0) onProgress?.Invoke(35, $"Navigating to {redirectUrl}...");
            },
            onError: (errorUrl, status) =>
            {
                if (status == -1) downloadFailed = true;
            }), cancellationToken);

        if (downloadFailed) return null;

        // Non-HTML content (PDFs, zips, docs…) — a downloadable file, not a page.
        if (!string.IsNullOrWhiteSpace(domJson) &&
            domJson.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return new ResearchPage
            {
                Url = resolvedUrl ?? url,
                IsFile = true,
                FileContentType = domJson.Substring(5)
            };
        }

        if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("Error")) return null;

        onProgress?.Invoke(60, "Analyzing HTML document...");
        cancellationToken.ThrowIfCancellationRequested();

        var analyzedArticle = Html.DeserializeArticle(domJson);

        // Thin-DOM retry at the fetch layer: Charlotte's headless browsers get
        // bot/cookie interstitials on some sites (PMC serves a ~19-element
        // cookie-required shell to BOTH backends) while a plain GET returns
        // the full page. Direct HTTP carries a different fingerprint, so
        // retry once and keep whichever produced the larger DOM.
        if ((analyzedArticle.elements?.Count ?? 0) < ThinDomElements)
        {
            var rawHtml = await SearchHelper.FetchRawHtmlAsync(resolvedUrl ?? url);
            var directArticle = SearchHelper.AnalyzeRawHtml(rawHtml);
            if ((directArticle?.elements?.Count ?? 0) > (analyzedArticle.elements?.Count ?? 0))
            {
                ChatDetailLog.Line($"thin Charlotte DOM ({analyzedArticle.elements.Count} elements) — direct fetch produced {directArticle.elements.Count}, using it");
                directArticle.url = resolvedUrl ?? url;
                analyzedArticle = directArticle;
            }
        }

        Html.GetArticleInfoFromDOM(analyzedArticle);

        // Search-engine discovery uses the RAW DOM — CleanDOM strips form
        // elements and attributes, so this must see the unmodified elements.
        var foundSearchForm = false;
        if (searchEnginesRepo != null)
        {
            try
            {
                foundSearchForm = SearchHelper.DiscoverSearchEngines(analyzedArticle,
                    resolvedUrl ?? url, searchEnginesRepo, null);
            }
            catch { /* discovery is best-effort */ }
        }

        // Cleaned DOM → flat HTML for the parser model (LLM_HtmlParsers). The
        // dump it returns is the page's preferred content source — persisted
        // next to the entry as research/{####}.json.
        var cleanedElements = Html.CleanDOM(analyzedArticle.elements);
        var cleanHtml = Html.FormatHtml(cleanedElements).ToString();
        ChatDetailLog.Line($"cleaned DOM: {analyzedArticle.rawHtml?.Length ?? 0} → {cleanHtml.Length} chars");

        // Text-thin check — a DOM can clear the element bar yet hold only a
        // rendered bot-wall/cookie shell (nav + scripts, no body). If the
        // cleaned text is still tiny, retry the direct fetch once and keep
        // whichever produced more body text.
        var bodyText = ThinTextRegex.Replace(cleanHtml ?? "", " ").Trim();
        if (bodyText.Length < ThinBodyChars)
        {
            var retryHtml = await SearchHelper.FetchRawHtmlAsync(resolvedUrl ?? url);
            var retryArticle = SearchHelper.AnalyzeRawHtml(retryHtml);
            if ((retryArticle?.elements?.Count ?? 0) > 0)
            {
                Html.GetArticleInfoFromDOM(retryArticle!);
                var altClean = Html.FormatHtml(Html.CleanDOM(retryArticle!.elements)).ToString();
                var altText = ThinTextRegex.Replace(altClean ?? "", " ").Trim();
                if (altText.Length > bodyText.Length)
                {
                    ChatDetailLog.Line($"thin body text ({bodyText.Length} chars) — direct fetch produced {altText.Length}, using it");
                    retryArticle.url = resolvedUrl ?? url;
                    analyzedArticle = retryArticle;
                    cleanHtml = altClean;
                }
            }
        }

        string? parsedJson = null;
        string? rawHtmlFile = null;
        if (!string.IsNullOrWhiteSpace(cleanHtml))
        {
            // Stage the cleaned HTML BEFORE the parser call — the file must
            // exist for inspection even if the model hangs or dies, and
            // integration later pairs it with the dump as research/{####}.html.
            try
            {
                var staging = $"_staging/{Guid.NewGuid():N}.html";
                if (Files.SaveFile(Files.Paths.Journal, staging, cleanHtml))
                    rawHtmlFile = staging;
            }
            catch { /* staging is diagnostic-only */ }

            if (htmlParser != null)
            {
                onProgress?.Invoke(62, "Parsing HTML structure...");
                try { parsedJson = await htmlParser(cleanHtml, cancellationToken); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    ChatDetailLog.Line($"html parser failed on {resolvedUrl ?? url}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        // When the dump claims the page hosts a search form but DOM discovery
        // found none, analyze its scripts for a JS-driven search endpoint —
        // again on the raw article. If static analysis also finds nothing but
        // a JS-driven search input exists, drive it with the browser probe
        // and capture the request the site actually fires.
        if (searchEnginesRepo != null && !foundSearchForm)
        {
            var dumpClaimsForm = DumpClaimsSearchForm(parsedJson);
            if (scriptAnalyzer != null && dumpClaimsForm)
            {
                try
                {
                    foundSearchForm = await SearchHelper.DiscoverFromScriptsAsync(analyzedArticle,
                        resolvedUrl ?? url, searchEnginesRepo, scriptAnalyzer, null);
                }
                catch { /* discovery is best-effort */ }
            }
            if (!foundSearchForm &&
                (dumpClaimsForm || SearchHelper.HasJsDrivenSearchInput(analyzedArticle)))
            {
                try
                {
                    await SearchHelper.DiscoverViaProbeAsync(resolvedUrl ?? url,
                        searchEnginesRepo, null);
                }
                catch { /* discovery is best-effort */ }
            }
        }

        var page = new ResearchPage
        {
            Url = resolvedUrl ?? url,
            Title = analyzedArticle.title ?? "",
            ParsedJson = parsedJson,
            RawHtmlFile = rawHtmlFile,
            DomElements = analyzedArticle.elements?.Count ?? 0
        };

        // ContentSections is the designed no-parser fallback: the relevance
        // gate and ParseSections consume it whenever ParsedJson yields no
        // usable text — which includes dumps that are empty, malformed, or
        // schema-shaped. Always populate it so a parser failure can't lose
        // the page's real content. Chrome detection must run on the RAW
        // elements — CleanDOM's clones strip class/id attributes and styles,
        // which are exactly the markers nav/sidebar detection needs.
        ContentSectionsFromDom(page, analyzedArticle.elements ?? cleanedElements);

        // Every image the dump names is downloaded and measured — only those
        // whose max dimension clears the floor are kept for the entry.
        page.Images = await WideImagesOnlyAsync(
            ParsedJsonImageUrls(parsedJson), new HashSet<string>(), cancellationToken);

        return page;
    }

    /// <summary>Chunk visible text out of the DOM into ContentSections — the
    /// no-parser fallback consumed by the relevance gate, media placement and
    /// ParseSections when ParsedJson is empty. Text inside chrome containers
    /// (nav, header, footer, menus, cookie banners, hidden elements) is
    /// skipped — an unfiltered blob starts with menu text and the
    /// topic-assign model correctly marks it "skip", losing the whole page.
    /// Requires the RAW element list (CleanDOM strips class/id/style).</summary>
    internal static void ContentSectionsFromDom(ResearchPage page, List<DomElement> cleaned)
    {
        var text = new StringBuilder();
        foreach (var el in cleaned)
        {
            if (el.tagName != "#text" || string.IsNullOrWhiteSpace(el.text)) continue;
            if (IsChromeText(el)) continue;
            text.Append(el.text.Trim()).Append(' ');
            if (text.Length > 15000) break;
        }

        var remaining = text.ToString().Trim();
        while (remaining.Length > 0 && page.ContentSections.Count < 5)
        {
            var take = Math.Min(3000, remaining.Length);
            if (take < remaining.Length)
            {
                var lastSpace = remaining.LastIndexOf(' ', take);
                if (lastSpace > take / 2) take = lastSpace;
            }
            page.ContentSections.Add(remaining.Substring(0, take).Trim());
            remaining = remaining.Substring(take).TrimStart();
        }
    }

    /// <summary>Chrome containers whose text never belongs in article
    /// content — checked via the DOM hierarchy of each #text node.</summary>
    private static readonly string[] ChromeTags =
        { "nav", "header", "footer", "aside", "form", "script", "style", "noscript", "select", "button" };

    private static bool IsChromeText(DomElement el)
    {
        foreach (var tag in ChromeTags)
            if (el.HasTagInHierarchy(tag)) return true;
        // Class/id heuristics — Wikipedia's menus are div-wrapped, not <nav>.
        foreach (var anc in el.Hierarchy())
        {
            if (anc.style?.TryGetValue("display", out var disp) == true && disp == "none") return true;
            if (anc.attribute?.ContainsKey("hidden") == true
                || anc.attribute?.TryGetValue("aria-hidden", out var ah) == true && ah == "true") return true;
            var id = anc.attribute?.TryGetValue("id", out var v) == true ? v : "";
            var marker = (id + " " + string.Join(" ", anc.className ?? new List<string>())).ToLowerInvariant();
            if (marker.Contains("navbox") || marker.Contains("sidebar") || marker.Contains("cookie")
                || marker.Contains("banner") || marker.Contains("mw-panel") || marker.Contains("vector-menu")
                || marker.Contains("sitenotice") || marker.Contains("mw-indicators")) return true;
        }
        return false;
    }

    /// <summary>All image URLs the parser dump declares — hero_image plus
    /// sections[].images[] and root images[]. Malformed dumps yield none.</summary>
    private static List<string> ParsedJsonImageUrls(string? parsedJson)
    {
        var urls = new List<string>();
        if (string.IsNullOrWhiteSpace(parsedJson)) return urls;
        try
        {
            if (AddEditJournalEntryTool.ParseModelJson(parsedJson)
                is not { } root) return urls;
            void Collect(System.Text.Json.Nodes.JsonNode? node)
            {
                if (node is not System.Text.Json.Nodes.JsonObject o) return;
                var u = o["url"]?.ToString();
                if (!string.IsNullOrWhiteSpace(u) && !urls.Contains(u)) urls.Add(u);
            }
            Collect(root["hero_image"]);
            if (root["sections"] is System.Text.Json.Nodes.JsonArray sections)
                foreach (var s in sections)
                    if (s is System.Text.Json.Nodes.JsonObject so &&
                        so["images"] is System.Text.Json.Nodes.JsonArray imgs)
                        foreach (var im in imgs) Collect(im);
            if (root["images"] is System.Text.Json.Nodes.JsonArray rootImgs)
                foreach (var im in rootImgs) Collect(im);
        }
        catch { /* malformed dump — no image urls */ }
        return urls;
    }

    /// <summary>
    /// Keep only images verified at least 640px in their largest dimension —
    /// each URL is downloaded and identified (bounded to the first few
    /// candidates, short timeout). Order is preserved.
    /// </summary>
    private static async Task<List<string>> WideImagesOnlyAsync(
        List<string> images, HashSet<string> declaredWide, CancellationToken cancellationToken)
    {
        ChatDetailLog.Fn("web-scrape", "WideImagesOnlyAsync");
        var ok = new HashSet<string>(declaredWide, StringComparer.OrdinalIgnoreCase);
        // URL-level chrome rejection — SVG icons, avatars and profile photos
        // can clear the pixel floor but are never gallery content. Cheaper
        // than spending a download on them.
        var content = images.Where(u => !ChromeImageUrl.IsMatch(u)).ToList();
        if (content.Count != images.Count)
            ChatDetailLog.Line($"image chrome filter dropped {images.Count - content.Count} UI asset(s)");
        var unknown = content.Where(u => !ok.Contains(u)).Take(MaxImageChecks).ToList();
        if (unknown.Count > 0)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
            var failures = 0;
            var lastError = "";
            // Sequential with spacing + a Retry-After retry — hotlinked CDNs
            // (Wikimedia) return 429 on bursts even at low concurrency.
            foreach (var u in unknown)
            {
                if (ok.Count >= EnoughImages) break;
                try
                {
                    using var resp = await client.GetAsync(u, cancellationToken);
                    if ((int)resp.StatusCode == 429)
                    {
                        var wait = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3);
                        resp.Dispose();
                        await Task.Delay(wait > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : wait, cancellationToken);
                        using var retry = await client.GetAsync(u, cancellationToken);
                        if (!retry.IsSuccessStatusCode) { failures++; lastError = $"HTTP {(int)retry.StatusCode}"; continue; }
                        var rbytes = await retry.Content.ReadAsByteArrayAsync(cancellationToken);
                        if (IsGalleryWorthy(rbytes, retry.Content?.Headers?.ContentType?.MediaType)) ok.Add(u);
                        else failures++;
                        continue;
                    }
                    if (!resp.IsSuccessStatusCode) { failures++; lastError = $"HTTP {(int)resp.StatusCode}"; continue; }
                    var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (IsGalleryWorthy(bytes, resp.Content?.Headers?.ContentType?.MediaType)) ok.Add(u);
                    else failures++;
                }
                catch (Exception ex) { failures++; lastError = ex.Message; }
                await Task.Delay(250, cancellationToken);
            }
            if (failures > 0)
                ChatDetailLog.Line($"image size check: {failures} rejected or failed ({lastError})");
        }
        var kept = content.Where(ok.Contains).ToList();
        if (kept.Count != images.Count)
            ChatDetailLog.Line($"images kept {kept.Count}/{images.Count} at >= {MinImageWidth}px width or height");
        return kept;
    }

    /// <summary>Gallery floor after download: a minimum byte size plus a
    /// verified max dimension. Tiny files are icons/spacers; SVG/icon media
    /// types are chrome even when their markup claims 640px+.</summary>
    private static bool IsGalleryWorthy(byte[] bytes, string? mediaType)
    {
        if (bytes.Length < MinImageBytes) return false;
        if (mediaType != null &&
            (mediaType.Contains("svg", StringComparison.OrdinalIgnoreCase)
             || mediaType.Contains("icon", StringComparison.OrdinalIgnoreCase)))
            return false;
        return Images.ImageMaxDimension(bytes) >= MinImageWidth;
    }

    /// <summary>Does the parser dump claim this page exposes a site-search
    /// facility? (search_engine_submit_form.url non-empty). Used to gate the
    /// script-analysis tier — DOM discovery already ran by this point.</summary>
    private static bool DumpClaimsSearchForm(string? parsedJson)
    {
        if (string.IsNullOrWhiteSpace(parsedJson)) return false;
        try
        {
            var root = AddEditJournalEntryTool.ParseModelJson(parsedJson);
            var form = root?["search_engine_submit_form"];
            var url = form is System.Text.Json.Nodes.JsonObject fo
                ? fo["url"]?.ToString() : form?.ToString();
            return !string.IsNullOrWhiteSpace(url);
        }
        catch { return false; }
    }
}
