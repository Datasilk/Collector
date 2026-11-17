using System;
using System.Collections.Generic;
using System.Linq;
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
    // Elements that are page chrome, never article content
    private static readonly HashSet<string> SkipTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "!--", "nav", "footer", "aside", "form", "button",
        "select", "option", "input", "label", "noscript", "fieldset", "legend"
    };

    private static readonly string[] FileExtensions =
    {
        ".pdf", ".zip", ".doc", ".docx", ".xls", ".xlsx", ".csv",
        ".ppt", ".pptx", ".epub", ".rar", ".7z", ".txt", ".rtf"
    };

    private static readonly string[] VideoHostHints =
    {
        "youtube.com", "youtu.be", "vimeo.com", "dailymotion.com",
        "twitch.tv", "rumble.com", "odysee.com"
    };

    /// <summary>Fallback helper — matches everything; must stay last in the registry.</summary>
    public bool Match(string url) => true;

    public async Task<ResearchPage?> ExtractAsync(
        string url,
        Action<int, string>? onProgress = null,
        CancellationToken cancellationToken = default,
        Guid? appUserId = null,
        Func<string, CancellationToken, Task<string>>? requestCookies = null)
    {
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
        Html.GetArticleInfoFromDOM(analyzedArticle);

        var indexes = new List<AnalyzedElement>();
        Html.GetBestElementIndexes(analyzedArticle, indexes);
        Html.GetArticleElements(analyzedArticle, indexes);

        var page = new ResearchPage
        {
            Url = resolvedUrl ?? url,
            Title = analyzedArticle.title ?? string.Empty,
            Language = DetectLanguage(analyzedArticle.rawHtml ?? domJson)
        };
        var bodyElements = analyzedArticle.bodyElements ?? new List<DomElement>();

        int headerIndex = 0, contentIndex = 0, imageIndex = 0;
        foreach (var element in bodyElements)
        {
            if (element.tagName != null && SkipTags.Contains(element.tagName)) continue;

            // Header elements (h1-h6)
            if (element.tagName.StartsWith("h") && element.tagName.Length == 2 &&
                char.IsDigit(element.tagName[1]))
            {
                var headerText = element.text?.Trim();
                if (!string.IsNullOrWhiteSpace(headerText))
                {
                    page.Headers.Add(headerText);
                    page.Layout.Add($"header:{headerIndex++}");
                }
                continue;
            }

            // Image elements
            if (element.tagName == "img")
            {
                var src = element.attribute?.GetValueOrDefault("src", "");
                if (!string.IsNullOrWhiteSpace(src))
                {
                    page.Images.Add(MakeAbsolute(src, page.Url));
                    page.Layout.Add($"image:{imageIndex++}");
                }
                continue;
            }

            // Video elements and embeds
            if (element.tagName is "video" or "iframe" or "embed" or "source")
            {
                var src = element.attribute?.GetValueOrDefault("src", "");
                if (!string.IsNullOrWhiteSpace(src))
                {
                    src = MakeAbsolute(src, page.Url);
                    if (!page.Videos.Contains(src)) page.Videos.Add(src);
                }
                continue;
            }

            // Anchors (links, videos, files)
            if (element.tagName == "a")
            {
                var href = element.attribute?.GetValueOrDefault("href", "");
                var linkText = element.text?.Trim();
                if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith("#") && !href.StartsWith("javascript:"))
                {
                    href = MakeAbsolute(href, page.Url);
                    var entry = !string.IsNullOrWhiteSpace(linkText) ? $"{linkText}|{href}" : href;

                    if (IsFileUrl(href)) page.Files.Add(entry);
                    else if (IsVideoUrl(href)) { if (!page.Videos.Contains(href)) page.Videos.Add(href); }
                    else page.Links.Add(entry);
                }
                continue;
            }

            // Content elements
            if (element.tagName is "p" or "div" or "article" or "section" or "span" or "li" or "td" or "blockquote" or "pre" or "code")
            {
                var text = element.text?.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 20)
                {
                    page.ContentSections.Add(text);
                    page.Layout.Add($"content:{contentIndex++}");
                }
            }
        }

        // No structured content — fall back to the article summary/raw text
        if (page.ContentSections.Count == 0)
        {
            if (!string.IsNullOrEmpty(analyzedArticle.summary))
            {
                page.ContentSections.Add(analyzedArticle.summary);
                page.Layout.Add("content:0");
            }
            else if (!string.IsNullOrEmpty(analyzedArticle.rawText))
            {
                page.ContentSections.Add(analyzedArticle.rawText);
                page.Layout.Add("content:0");
            }
        }

        // All images from the article (including those outside bodyElements)
        if (analyzedArticle.images != null)
        {
            foreach (var img in analyzedArticle.images)
            {
                if (!string.IsNullOrWhiteSpace(img.url) && !page.Images.Contains(img.url))
                    page.Images.Add(MakeAbsolute(img.url, page.Url));
            }
        }

        return page;
    }

    /// <summary>
    /// The website's declared language — &lt;html lang="…"&gt; in the raw markup,
    /// or the serialized DOM's lang attribute. Returns the ISO code or null.
    /// </summary>
    private static string? DetectLanguage(string markup)
    {
        if (string.IsNullOrWhiteSpace(markup)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(markup,
            "lang\\s*[=:]\\s*[\"']([a-zA-Z]{2,3}(?:-[a-zA-Z]{2,4})?)[\"']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string MakeAbsolute(string href, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(href)) return href;
        if (href.StartsWith("//")) return "https:" + href;
        if (href.StartsWith("/") && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            return new Uri(baseUri, href).ToString();
        return href;
    }

    private static bool IsFileUrl(string url)
    {
        var path = url.Split('?', '#')[0].ToLowerInvariant();
        return FileExtensions.Any(ext => path.EndsWith(ext));
    }

    private static bool IsVideoUrl(string url)
    {
        var lower = url.ToLowerInvariant();
        return VideoHostHints.Any(h => lower.Contains(h));
    }
}
