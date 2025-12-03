using System;
using System.Linq;
using System.Threading.Tasks;
using Collector.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Collector.Web.Server.SignalR
{
    public class WorkerHub : Hub
    {
        private readonly ILogger<WorkerHub> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WorkerHub(ILogger<WorkerHub> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Starts a worker for a specific user and returns the workerId.
        /// </summary>
        /// <param name="appUserId">The application user ID associated with the worker.</param>
        /// <param name="route">The logical route for the worker (used to resolve worker type).</param>
        /// <param name="method">The worker method to invoke.</param>
        /// <param name="args">Arguments object to pass into the worker method (properties mapped by name).</param>
        /// <param name="customId">Optional custom identifier to associate with the worker (e.g., moduleId).</param>
        /// <returns>The created worker's Guid identifier.</returns>
        public async Task<Guid> Call(string appUserId, string route, string method, object? args, string? customId, string? url)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));
            if (string.IsNullOrWhiteSpace(route)) throw new ArgumentNullException(nameof(route));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            try
            {
                // Create a scope that will NOT be disposed until the worker completes
                var scope = _serviceProvider.CreateScope();
                var workerType = WorkerRoutes.GetWorkerType(route);
                var worker = (IWorker?)scope.ServiceProvider.GetService(workerType);

                if (worker == null)
                {
                    scope.Dispose();
                    throw new HubException($"Worker not resolved for route: {route}");
                }

                // Wrap args in array for StartWorker (it will detect JSON object and map by property name)
                var argsArray = args != null ? new object?[] { args } : null;
                var workerTask = Workers.StartWorker(appUserId, worker, method, argsArray, scope);
                if (!string.IsNullOrWhiteSpace(customId))
                {
                    Workers.SetCustomId(appUserId, workerTask.WorkerId, customId);
                }
                Workers.SetRoute(appUserId, workerTask.WorkerId, route);
                Workers.SetUrl(appUserId, workerTask.WorkerId, url ?? string.Empty);
                _logger.LogInformation("Started worker {WorkerId} for user {AppUserId} on route {Route} method {Method} customId {CustomId}", workerTask.WorkerId, appUserId, route, method, customId);

                return await Task.FromResult(workerTask.WorkerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting worker for user {AppUserId} on route {Route} and method {Method}", appUserId, route, method);
                throw;
            }
        }

        /// <summary>
        /// Stops a running worker for a user based on workerId.
        /// </summary>
        /// <param name="appUserId">The application user ID associated with the worker.</param>
        /// <param name="workerId">The worker's Guid identifier.</param>
        public async Task Stop(string appUserId, Guid workerId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));

            try
            {
                Workers.StopWorker(appUserId, workerId);
                _logger.LogInformation("Stopped worker {WorkerId} for user {AppUserId}", workerId, appUserId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping worker {WorkerId} for user {AppUserId}", workerId, appUserId);
                throw;
            }
        }

        public async Task ProgressAll(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));

            try
            {
                var workers = Workers.GetWorkers(appUserId);
                foreach (var (workerId, worker) in workers)
                {
                    await worker.Progress(appUserId, workerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending progress for all workers for user {AppUserId}", appUserId);
                throw;
            }
        }

        public async Task<object[]> GetWorkersForUser(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));

            try
            {
                var workers = Workers.GetWorkersWithCustomId(appUserId);
                return workers
                    .Select(w => new { workerId = w.WorkerId, customId = w.CustomId, route = w.Route, url = w.Url })
                    .Cast<object>()
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workers for user {AppUserId}", appUserId);
                throw;
            }
        }

        public async Task RequestProgress(string appUserId, Guid workerId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));

            try
            {
                var workers = Workers.GetWorkers(appUserId);
                _logger.LogInformation("RequestProgress: Found {Count} workers for user {AppUserId}, looking for {WorkerId}", workers.Count(), appUserId, workerId);
                var workerTuple = workers.FirstOrDefault(w => w.WorkerId == workerId);
                if (workerTuple.Worker != null)
                {
                    _logger.LogInformation("RequestProgress: Calling Progress on worker {WorkerId}", workerId);
                    await workerTuple.Worker.Progress(appUserId, workerId);
                }
                else
                {
                    _logger.LogWarning("RequestProgress: Worker {WorkerId} not found for user {AppUserId}", workerId, appUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting progress for worker {WorkerId} for user {AppUserId}", workerId, appUserId);
                throw;
            }
        }
    }
}
