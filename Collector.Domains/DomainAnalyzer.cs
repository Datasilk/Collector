using System.Text.Json;
using Collector.Common;
using Collector.Common.DOM;
using Collector.Common.Models;
using Collector.Common.Models.Articles;
using Collector.Data.Enums;
using Collector.Data.Interfaces;

namespace Collector.Domains;

/// <summary>
/// Single-domain analysis core — the same pipeline the DomainWorker's
/// unanalyzed-domain loop runs: download the homepage through the shared
/// downloader, deserialize + article-analyze the DOM, classify it with an
/// LLM, and persist type/language/paywall/title info into the Domains table.
///
/// Extracted so the research pipeline can analyze a newly registered domain
/// inline without the worker being started. The LLM call is a delegate so
/// each caller picks its endpoint (worker → OpenAI.Prompt, chat → LLMClient).
/// </summary>
public static class DomainAnalyzer
{
    /// <summary>Machine-readable outcome of one analysis pass.</summary>
    public enum Status
    {
        Analyzed,
        DomainNotFound,
        DownloadFailed,
        ParseFailed,
        NoTextContent,
        LlmFailed
    }

    public sealed class Result
    {
        public Status Status { get; set; }
        public int DomainId { get; set; }
        public string Domain { get; set; } = "";
        public DomainType Type { get; set; } = DomainType.unused;
        public DomainType Type2 { get; set; } = DomainType.unused;
        public string Language { get; set; } = "";
        public bool RequiresSubscription { get; set; }
        public bool HasFreeContent { get; set; }
        public bool IsEmpty { get; set; }
        public string? Failure { get; set; }
    }

    /// <summary>LLM call signature: (system, user) → raw response text.</summary>
    public delegate Task<string?> LlmCall(string system, string user);

    /// <summary>
    /// Analyze one domain row end-to-end. Every step is best-effort — a
    /// failure maps to a Status value instead of throwing so callers can
    /// report it. Cancellation is honored inside the download + LLM call.
    /// </summary>
    public static async Task<Result> AnalyzeAsync(
        IDomainsRepository domainsRepository,
        int domainId,
        LlmCall llm,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new Result { DomainId = domainId };
        var domain = domainsRepository.GetById(domainId);
        if (domain == null)
        {
            result.Status = Status.DomainNotFound;
            result.Failure = "domain not found";
            return result;
        }
        result.Domain = domain.domain ?? "";

        var url = $"http{(domain.https ? "s" : "")}://{(domain.www ? "www." : "")}{domain.domain}";
        onProgress?.Invoke($"Downloading homepage for {result.Domain}...");

        string html;
        try
        {
            html = await Task.Run(
                () => Common.Article.Download(url, out _),
                cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            result.Status = Status.DownloadFailed;
            result.Failure = $"could not reach {result.Domain}: {ex.Message}";
            return result;
        }

        if (string.IsNullOrEmpty(html) || html.StartsWith("file:"))
        {
            result.Status = Status.DownloadFailed;
            result.Failure = $"failed to download homepage for {result.Domain}";
            return result;
        }

        onProgress?.Invoke("Deserializing page content...");
        AnalyzedArticle article;
        try
        {
            article = Html.DeserializeArticle(html);
            Html.GetArticleInfoFromDOM(article);
            var indexes = new List<AnalyzedElement>();
            Html.GetBestElementIndexes(article, indexes);
            Html.GetArticleElements(article, indexes);
        }
        catch (Exception ex)
        {
            result.Status = Status.ParseFailed;
            result.Failure = $"failed to parse homepage: {ex.Message}";
            return result;
        }

        var text = ExtractText(article);
        if (string.IsNullOrWhiteSpace(text))
        {
            domainsRepository.IsEmpty(domainId, true);
            result.Status = Status.NoTextContent;
            result.IsEmpty = true;
            result.Failure = "no text content found on homepage";
            return result;
        }

        cancellationToken.ThrowIfCancellationRequested();
        onProgress?.Invoke($"Analyzing {text.Length} characters with LLM...");

        var analysis = await AnalyzeWithLlmAsync(result.Domain, text, llm);
        if (analysis == null)
        {
            result.Status = Status.LlmFailed;
            result.Failure = "LLM analysis failed";
            return result;
        }

        result.Type = ParseDomainType(analysis.Type);
        result.Type2 = ParseDomainType(analysis.Type2);
        result.Language = analysis.Language ?? "";
        result.RequiresSubscription = analysis.RequiresSubscription;
        result.HasFreeContent = analysis.HasFreeContent;
        result.IsEmpty = analysis.IsEmpty;

        if (result.Type2 != DomainType.unused)
        {
            domainsRepository.UpdateDomainTypes(domainId, result.Type, result.Type2);
        }
        else if (result.Type != DomainType.unused)
        {
            domainsRepository.UpdateDomainType(domainId, result.Type);
        }

        if (!string.IsNullOrWhiteSpace(analysis.Language))
        {
            domainsRepository.UpdateLanguage(domainId, analysis.Language);
        }

        domainsRepository.RequireSubscription(domainId, analysis.RequiresSubscription);
        domainsRepository.HasFreeContent(domainId, analysis.HasFreeContent);
        domainsRepository.IsEmpty(domainId, analysis.IsEmpty);

        // UpdateInfo overwrites unconditionally — preserve existing values
        // for any field the LLM returned empty.
        var newTitle = !string.IsNullOrWhiteSpace(analysis.Title) ? analysis.Title : (domain.title ?? "");
        var newDescription = !string.IsNullOrWhiteSpace(analysis.Description) ? analysis.Description : (domain.description ?? "");
        var newLang = !string.IsNullOrWhiteSpace(analysis.Language) ? analysis.Language : (domain.lang ?? "");
        if (!string.IsNullOrWhiteSpace(newTitle) || !string.IsNullOrWhiteSpace(newDescription))
        {
            domainsRepository.UpdateInfo(domainId, newTitle, newDescription, newLang);
        }
        if (!string.IsNullOrWhiteSpace(analysis.Company))
        {
            domainsRepository.UpdateCompany(domainId, analysis.Company);
        }

        result.Status = Status.Analyzed;
        return result;
    }

    private static string ExtractText(AnalyzedArticle article)
    {
        if (!string.IsNullOrWhiteSpace(article.rawText)) return article.rawText;
        var texts = article.elements.Where(e => !string.IsNullOrWhiteSpace(e.text)).Select(e => e.text);
        return string.Join(" ", texts);
    }

    private static async Task<DomainAnalysisResult?> AnalyzeWithLlmAsync(
        string domain, string text, LlmCall llm)
    {
        const int maxChars = 8000;
        if (text.Length > maxChars) text = text.Substring(0, maxChars);

        var system = @"Analyze the following homepage text and classify the website.

## Rules ##
For type and type2, pick the most important relatable two types from this comma-delimited list of all available domain types based on the domain name & page text extracted from the DOM.
When combining the two types into a phrase, it should make sense. flip values from type to type 2 if the phrase sounds better that way:

" + string.Join(", ", Enum.GetNames(typeof(DomainType))) + @"

Use ""unused"" if you cannot determine the type.
For language, use the ISO 639-1 code (e.g., en, es, fr) or an empty string if unknown.
requiresSubscription: true if the site appears to require payment/subscription to access main content.
hasFreeContent: true if the site offers free content without requiring payment.
isEmpty: true if the page is a placeholder, domain parking page, fake site, error page, or has no real content.
title: a short, human-readable title for the website (e.g. the site or publication name). Empty string if unknown.
description: a one-to-two sentence description of what the website is about. Empty string if unknown.
company: the company, organization, or individual that owns/publishes the website. Empty string if unknown.

## Output ##
ONLY output the exact JSON structure with the values determined by examining the homepage text extracted from the DOM and following the rules above:
{
    ""type"": """",
    ""type2"": """",
    ""language"": ""en"",
    ""requiresSubscription"": bool,
    ""hasFreeContent"": bool,
    ""isEmpty"": bool,
    ""title"": """",
    ""description"": """",
    ""company"": """"
}";

        var user = @"Domain: " + domain + @"
Homepage text extracted from the DOM:" + text;

        string? responseContent;
        try
        {
            responseContent = await llm(system, user);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(responseContent)) return null;

        responseContent = StripMarkdownCodeFence(responseContent);

        try
        {
            return JsonSerializer.Deserialize<DomainAnalysisResult>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static string StripMarkdownCodeFence(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            content = content.Substring(7).TrimStart();
        }
        else if (content.StartsWith("```"))
        {
            content = content.Substring(3).TrimStart();
        }
        if (content.EndsWith("```"))
        {
            content = content.Substring(0, content.Length - 3).TrimEnd();
        }
        return content;
    }

    private static DomainType ParseDomainType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DomainType.unused;
        if (Enum.TryParse<DomainType>(value, true, out var type)) return type;
        return DomainType.unused;
    }

    private class DomainAnalysisResult
    {
        public string Type { get; set; } = "unused";
        public string Type2 { get; set; } = "unused";
        public string Language { get; set; } = "";
        public bool RequiresSubscription { get; set; }
        public bool HasFreeContent { get; set; } = true;
        public bool IsEmpty { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Company { get; set; } = "";
    }
}
