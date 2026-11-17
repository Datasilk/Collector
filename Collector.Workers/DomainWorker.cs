using System;
using System.Threading;
using System.Threading.Tasks;
using Collector.AI;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;
using Collector.Domains;
using Collector.Workers.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
namespace Collector.Workers
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
                var result = await DomainAnalyzer.AnalyzeAsync(
                    _domainsRepository, domainId,
                    (system, user) => OpenAI.Prompt(system, "", user),
                    msg => SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate",
                        new { message = msg, type = "info" }, cancellationToken).GetAwaiter().GetResult(),
                    cancellationToken);

                switch (result.Status)
                {
                    case DomainAnalyzer.Status.Analyzed:
                        if (sendComplete)
                        {
                            await SendWorkerMessage(appUserId, workerId, "DomainAnalysisComplete", new
                            {
                                domainId,
                                type = result.Type.ToString(),
                                type2 = result.Type2.ToString(),
                                language = result.Language,
                                requiresSubscription = result.RequiresSubscription,
                                hasFreeContent = result.HasFreeContent,
                                isEmpty = result.IsEmpty
                            }, cancellationToken);
                        }
                        else
                        {
                            await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new
                            {
                                message = $"Completed {result.Domain}: type={result.Type}, type2={result.Type2}, lang={result.Language}, paywall={result.RequiresSubscription}, free={result.HasFreeContent}, empty={result.IsEmpty}",
                                type = "success"
                            }, cancellationToken);
                        }
                        break;
                    case DomainAnalyzer.Status.NoTextContent:
                        await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate",
                            new { message = result.Failure, type = "warning" }, cancellationToken);
                        break;
                    default:
                        await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate",
                            new { message = result.Failure ?? $"Analysis failed: {result.Status}", type = "error" }, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain analysis worker error");
                await SendWorkerMessage(appUserId, workerId, "DomainAnalysisUpdate", new { message = ex.Message, type = "error" }, cancellationToken);
            }
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }
    }
}
