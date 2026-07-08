using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Common.Models.Articles;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
namespace Collector.Web.Server.Workers
{
    public class DomainWorker : IWorker
    {
        private readonly ILogger<DomainWorker> _logger;
        private readonly IDomainsRepository _domainsRepository;
        private readonly IHubContext<WorkerHub> _hubContext;

        private readonly object _stateLock = new object();
        private bool _running;
        private int _domainsProcessed;
        private string _currentStatus = "";
        private string _currentDomain = "";

        public DomainWorker(ILogger<DomainWorker> logger, IDomainsRepository domainsRepository, IHubContext<WorkerHub> hubContext)
        {
            _logger = logger;
            _domainsRepository = domainsRepository;
            _hubContext = hubContext;
        }

        public Task Stop()
        {
            lock (_stateLock)
            {
                _running = false;
                _currentStatus = "Stopped";
            }
            return Task.CompletedTask;
        }

        public async Task Progress(string appUserId, Guid workerId)
        {
            int processed;
            string status, domain;
            lock (_stateLock)
            {
                processed = _domainsProcessed;
                status = _currentStatus;
                domain = _currentDomain;
            }
            await SendWorkerMessage(appUserId, workerId, "DomainAnalysisProgress", new { processed, status, domain }, CancellationToken.None);
        }

        public async Task Start(string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                _running = true;
                _domainsProcessed = 0;
                _currentStatus = "Starting analyzer...";
                _currentDomain = "";
            }

            await SendWorkerMessage(appUserId, workerId, "DomainAnalysisStarted", new { mode = "queue" }, cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested && _running)
                {
                    bool shouldContinue;
                    lock (_stateLock) { shouldContinue = _running; }
                    if (!shouldContinue) break;

                    Domain? domain;
                    lock (_stateLock) { _currentStatus = "Checking for unanalyzed domains..."; }
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = "Checking for unanalyzed domains...", type = "info" }, cancellationToken);

                    try
                    {
                        domain = _domainsRepository.GetNextUnanalyzedDomain();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get next unanalyzed domain");
                        await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = $"Database error: {ex.Message}", type = "error" }, cancellationToken);
                        break;
                    }

                    if (domain == null)
                    {
                        lock (_stateLock) { _currentStatus = "No unanalyzed domains found, waiting 60 seconds..."; }
                        await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = "No unanalyzed domains found, waiting 60 seconds...", type = "info" }, cancellationToken);
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                        }
                        catch (TaskCanceledException) { }
                        continue;
                    }

                    lock (_stateLock)
                    {
                        _currentDomain = domain.domain;
                        _currentStatus = $"Analyzing {domain.domain}...";
                    }

                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = $"Analyzing {domain.domain}...", type = "info" }, cancellationToken);
                    await AnalyzeSingleDomain(appUserId, workerId, domain.domainId, false, cancellationToken);

                    lock (_stateLock)
                    {
                        _domainsProcessed++;
                        _currentDomain = "";
                    }

                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisProgress", new { processed = _domainsProcessed, domain = domain.domain, status = "Running" }, cancellationToken);
                }

                lock (_stateLock) { _currentStatus = "Analyzer stopped"; }
                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisComplete", new { mode = "queue", processed = _domainsProcessed }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain analyzer queue error");
                lock (_stateLock) { _currentStatus = $"Error: {ex.Message}"; }
                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisError", new { message = ex.Message }, cancellationToken);
            }
        }

        public async Task AnalyzeDomain(string appUserId, Guid workerId, int domainId, CancellationToken cancellationToken)
        {
            await SendWorkerMessage(appUserId, workerId, "DomainAnalysisStarted", new { domainId, mode = "single" }, cancellationToken);
            await AnalyzeSingleDomain(appUserId, workerId, domainId, true, cancellationToken);
        }

        private async Task AnalyzeSingleDomain(string appUserId, Guid workerId, int domainId, bool sendComplete, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                var domain = _domainsRepository.GetById(domainId);
                if (domain == null)
                {
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = "Domain not found", type = "error" }, cancellationToken);
                    return;
                }

                var url = $"http{(domain.https ? "s" : "")}://{(domain.www ? "www." : "")}{domain.domain}";
                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = $"Downloading homepage for {domain.domain}...", type = "info" }, cancellationToken);

                string newUrl;
                var html = Common.Article.Download(url, out newUrl);
                if (string.IsNullOrEmpty(html))
                {
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = $"Failed to download homepage for {domain.domain}", type = "error" }, cancellationToken);
                    return;
                }

                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = "Deserializing page content...", type = "info" }, cancellationToken);

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
                    _logger.LogError(ex, "Failed to deserialize article for domain {Domain}", domain.domain);
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = $"Failed to parse homepage: {ex.Message}", type = "error" }, cancellationToken);
                    return;
                }

                var text = ExtractText(article);
                if (string.IsNullOrWhiteSpace(text))
                {
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = "No text content found on homepage", type = "warning" }, cancellationToken);
                    _domainsRepository.IsEmpty(domainId, true);
                    return;
                }

                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = $"Analyzing {text.Length} characters with LLM...", type = "info" }, cancellationToken);

                var analysis = await AnalyzeWithLLM(domain.domain, text);
                if (analysis == null)
                {
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = "LLM analysis failed", type = "error" }, cancellationToken);
                    return;
                }

                var type = ParseDomainType(analysis.Type);
                var type2 = ParseDomainType(analysis.Type2);

                if (type2 != DomainType.unused)
                {
                    _domainsRepository.UpdateDomainTypes(domainId, type, type2);
                }
                else if (type != DomainType.unused)
                {
                    _domainsRepository.UpdateDomainType(domainId, type);
                }

                if (!string.IsNullOrWhiteSpace(analysis.Language))
                {
                    _domainsRepository.UpdateLanguage(domainId, analysis.Language);
                }

                _domainsRepository.RequireSubscription(domainId, analysis.RequiresSubscription);
                _domainsRepository.HasFreeContent(domainId, analysis.HasFreeContent);
                _domainsRepository.IsEmpty(domainId, analysis.IsEmpty);

                if (sendComplete)
                {
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisComplete", new
                    {
                        domainId,
                        type = type.ToString(),
                        type2 = type2.ToString(),
                        language = analysis.Language,
                        requiresSubscription = analysis.RequiresSubscription,
                        hasFreeContent = analysis.HasFreeContent,
                        isEmpty = analysis.IsEmpty
                    }, cancellationToken);
                }
                else
                {
                    await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new
                    {
                        message = $"Completed {domain.domain}: type={type}, type2={type2}, lang={analysis.Language}, paywall={analysis.RequiresSubscription}, free={analysis.HasFreeContent}, empty={analysis.IsEmpty}",
                        type = "success"
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain analysis worker error");
                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = ex.Message, type = "error" }, cancellationToken);
            }
        }

        private static string ExtractText(AnalyzedArticle article)
        {
            if (!string.IsNullOrWhiteSpace(article.rawText)) return article.rawText;
            var texts = article.elements.Where(e => !string.IsNullOrWhiteSpace(e.text)).Select(e => e.text);
            return string.Join(" ", texts);
        }

        private async Task<DomainAnalysisResult?> AnalyzeWithLLM(string domain, string text)
        {
            const int maxChars = 8000;
            if (text.Length > maxChars) text = text.Substring(0, maxChars);

            var system = @"Analyze the following homepage text and classify the website. 

## Rules ##
For type and type2, pick the most important relatable two types from this comma-delimited list of all available domain types based on the domain name & page text extracted from the DOM. 
When combining the two types into a phrase, it should make sense. flip values from type to type 2 if the phrase sounds better that way: 

" + GetDomainTypeNames() + @"

Use ""unused"" if you cannot determine the type.
For language, use the ISO 639-1 code (e.g., en, es, fr) or an empty string if unknown.
requiresSubscription: true if the site appears to require payment/subscription to access main content.
hasFreeContent: true if the site offers free content without requiring payment.
isEmpty: true if the page is a placeholder, domain parking page, fake site, error page, or has no real content.

## Output ##
ONLY output the exact JSON structure with the values determined by examining the homepage text extracted from the DOM and following the rules above:
{
    ""type"": """",
    ""type2"": """",
    ""language"": ""en"",
    ""requiresSubscription"": bool,
    ""hasFreeContent"": bool,
    ""isEmpty"": bool
}";

            var user = @"Domain: " + domain + @"
Homepage text extracted from the DOM:" + text;

            string responseContent;
            try
            {
                responseContent = await LLMs.Prompt(system, "", user);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM analysis request failed");
                return null;
            }

            if (string.IsNullOrWhiteSpace(responseContent)) return null;

            responseContent = StripMarkdownCodeFence(responseContent);

            try
            {
                return JsonSerializer.Deserialize<DomainAnalysisResult>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM analysis response: {Response}", responseContent);
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

        private static string GetDomainTypeNames()
        {
            return string.Join(", ", Enum.GetNames(typeof(DomainType)));
        }

        private static DomainType ParseDomainType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DomainType.unused;
            if (Enum.TryParse<DomainType>(value, true, out var type)) return type;
            return DomainType.unused;
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }

        private class DomainAnalysisResult
        {
            public string Type { get; set; } = "unused";
            public string Type2 { get; set; } = "unused";
            public string Language { get; set; } = "";
            public bool RequiresSubscription { get; set; }
            public bool HasFreeContent { get; set; } = true;
            public bool IsEmpty { get; set; }
        }
    }
}
