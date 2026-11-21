using Collector.Chat.Helpers;
using System.Collections.Generic;

namespace Collector.Chat.Tools.GatherResearch;

/// <summary>
/// A page extracted by a research-source helper — the common shape every
/// site-specific extractor produces so downstream tools stay site-agnostic.
/// </summary>
public class ResearchPage
{
    /// <summary>Final resolved URL (after redirects).</summary>
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<string> ContentSections { get; set; } = new();
    public List<string> Links { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public List<string> Videos { get; set; } = new();
    public List<string> Files { get; set; } = new();
    public List<string> Layout { get; set; } = new();

    /// <summary>ISO language code of the source ("en", "fr", "de-DE"…) when
    /// the helper could determine it — HTML lang attribute, subtitle track…
    /// Null = unknown; downstream falls back to content-based detection.</summary>
    public string? Language { get; set; }

    /// <summary>True when the URL is a downloadable file (pdf, zip…) rather
    /// than an extractable page — stored as a "file-download" data item.</summary>
    public bool IsFile { get; set; }
    /// <summary>MIME/content type when <see cref="IsFile"/> is true.</summary>
    public string? FileContentType { get; set; }

    /// <summary>Structured JSON produced by the HTML parser model
    /// (LLM_HtmlParsers) from the article's raw HTML — persisted next to the
    /// entry under research/{####}.json and referenced from the source.</summary>
    public string? ParsedJson { get; set; }

    /// <summary>Number of DOM elements the article analyzer produced. A
    /// suspiciously small count means a bot-lite stub was served (PMC
    /// returns ~30-element shells) rather than the real article. -1 = the
    /// helper didn't go through DOM analysis (e.g. YouTube transcripts).</summary>
    public int DomElements { get; set; } = -1;

    /// <summary>Staging filename (under journals/_staging/) holding the raw
    /// article HTML the parser consumed — moved beside the dump as
    /// research/{####}.html during integration so the two can be compared.</summary>
    public string? RawHtmlFile { get; set; }

    /// <summary>Project this page into the shared-data dictionary shape the
    /// research pipeline already consumes ("web-scrape"/"file-download").</summary>
    public Dictionary<string, List<string>> ToDataItem()
    {
        if (IsFile)
        {
            return new Dictionary<string, List<string>>
            {
                ["type"] = new List<string> { "file-download" },
                ["url"] = new List<string> { Url },
                ["contentType"] = new List<string> { FileContentType ?? "" }
            };
        }

        var item = new Dictionary<string, List<string>>
        {
            ["type"] = new List<string> { "web-scrape" },
            ["url"] = new List<string> { Url },
            ["title"] = new List<string> { Title }
        };
        if (Headers.Count > 0) item["headers"] = Headers;
        if (ContentSections.Count > 0) item["content"] = ContentSections;
        if (Links.Count > 0) item["links"] = Links;
        if (Images.Count > 0) item["images"] = Images;
        if (Videos.Count > 0) item["videos"] = Videos;
        if (Files.Count > 0) item["files"] = Files;
        if (Layout.Count > 0) item["layout"] = Layout;
        if (!string.IsNullOrWhiteSpace(ParsedJson)) item["parsed"] = new List<string> { ParsedJson };
        if (!string.IsNullOrWhiteSpace(RawHtmlFile)) item["rawhtml"] = new List<string> { RawHtmlFile };
        return item;
    }
}
