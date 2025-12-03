using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Collector.Web.Server.SignalR
{
    public class WorkerTask
    {
        public Guid WorkerId { get; set; }
        public IWorker Worker { get; set; } = default!;
        public Task? Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = default!;
        public string CustomId { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public IServiceScope? Scope { get; set; }
    }

    public static class Workers
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, List<WorkerTask>> _workersByUser = new Dictionary<string, List<WorkerTask>>();

        public static WorkerTask StartWorker(string appUserId, IWorker worker, string method, object?[]? args, IServiceScope? scope = null)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));
            if (worker == null) throw new ArgumentNullException(nameof(worker));

            var workerTask = new WorkerTask
            {
                WorkerId = Guid.NewGuid(),
                Worker = worker,
                CancellationTokenSource = new CancellationTokenSource(),
                Scope = scope
            };

            lock (_lock)
            {
                if (!_workersByUser.TryGetValue(appUserId, out var list))
                {
                    list = new List<WorkerTask>();
                    _workersByUser[appUserId] = list;
                }
                list.Add(workerTask);
            }

            workerTask.Task = Task.Run(async () =>
            {
                try
                {
                    await InvokeWorkerMethod(worker, method, appUserId, workerTask.WorkerId, args, workerTask.CancellationTokenSource.Token);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    // Cleanup when the task finishes
                    RemoveWorker(appUserId, workerTask.WorkerId);
                    // Dispose the scope so injected services are released
                    workerTask.Scope?.Dispose();
                }
            }, workerTask.CancellationTokenSource.Token);

            return workerTask;
        }

        private static async Task InvokeWorkerMethod(IWorker worker, string method, string appUserId, Guid workerId, object?[]? args, CancellationToken cancellationToken)
        {
            var workerType = worker.GetType();
            var methodInfo = workerType.GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Method '{method}' not found on worker type '{workerType.Name}'");
            }

            var parameters = methodInfo.GetParameters();
            var invokeArgs = new List<object?>();

            // Check if args contains a single JSON object to map by property name
            Dictionary<string, JsonElement>? argsDict = null;
            if (args != null && args.Length == 1 && args[0] is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                argsDict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in je.EnumerateObject())
                {
                    argsDict[prop.Name] = prop.Value;
                }
            }

            // Build the argument list, matching parameters by name (for object) or position (for array)
            int argIndex = 0;
            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(string) && param.Name == "appUserId")
                {
                    invokeArgs.Add(appUserId);
                }
                else if (param.ParameterType == typeof(Guid) && param.Name == "workerId")
                {
                    invokeArgs.Add(workerId);
                }
                else if (param.ParameterType == typeof(CancellationToken))
                {
                    invokeArgs.Add(cancellationToken);
                }
                else if (argsDict != null && param.Name != null && argsDict.TryGetValue(param.Name, out var jsonValue))
                {
                    // Map by property name from JSON object
                    invokeArgs.Add(ConvertArg(jsonValue, param.ParameterType));
                }
                else if (argsDict == null && args != null && argIndex < args.Length)
                {
                    // Fall back to positional mapping for arrays
                    var arg = args[argIndex];
                    invokeArgs.Add(ConvertArg(arg, param.ParameterType));
                    argIndex++;
                }
                else if (param.HasDefaultValue)
                {
                    invokeArgs.Add(param.DefaultValue);
                }
                else
                {
                    invokeArgs.Add(param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
                }
            }

            var result = methodInfo.Invoke(worker, invokeArgs.ToArray());
            if (result is Task task)
            {
                await task;
            }
        }

        private static object? ConvertArg(object? arg, Type targetType)
        {
            if (arg == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (arg is JsonElement je)
            {
                return JsonSerializer.Deserialize(je.GetRawText(), targetType);
            }

            if (targetType.IsAssignableFrom(arg.GetType()))
            {
                return arg;
            }

            return Convert.ChangeType(arg, targetType);
        }

        public static void StopWorker(string appUserId, Guid workerId)
        {
            WorkerTask? workerTask = null;

            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    workerTask = list.FirstOrDefault(w => w.WorkerId == workerId);
                }
            }

            if (workerTask == null)
            {
                return;
            }

            try
            {
                workerTask.Worker.Stop();
            }
            catch
            {
                // Swallow exceptions from worker.Stop to ensure cleanup continues
            }

            try
            {
                workerTask.CancellationTokenSource.Cancel();
            }
            catch
            {
            }

            RemoveWorker(appUserId, workerId);
        }

        public static void SetCustomId(string appUserId, Guid workerId, string customId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));
            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    var workerTask = list.FirstOrDefault(w => w.WorkerId == workerId);
                    if (workerTask != null)
                    {
                        workerTask.CustomId = customId ?? string.Empty;
                    }
                }
            }
        }

        public static IEnumerable<(Guid WorkerId, IWorker Worker)> GetWorkers(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));

            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    // Return a copy to avoid external mutation
                    return list.Select(w => (w.WorkerId, w.Worker)).ToList();
                }
            }

            return Enumerable.Empty<(Guid WorkerId, IWorker Worker)>();
        }

        public static IEnumerable<(Guid WorkerId, string CustomId, string Route, string Url)> GetWorkersWithCustomId(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));

            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    return list
                        .Where(w => !string.IsNullOrEmpty(w.CustomId))
                        .Select(w => (w.WorkerId, w.CustomId, w.Route, w.Url))
                        .ToList();
                }
            }

            return Enumerable.Empty<(Guid WorkerId, string CustomId, string Route, string Url)>();
        }

        public static void SetRoute(string appUserId, Guid workerId, string route)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));
            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    var workerTask = list.FirstOrDefault(w => w.WorkerId == workerId);
                    if (workerTask != null)
                    {
                        workerTask.Route = route ?? string.Empty;
                    }
                }
            }
        }

        public static void SetUrl(string appUserId, Guid workerId, string url)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) throw new ArgumentNullException(nameof(appUserId));
            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    var workerTask = list.FirstOrDefault(w => w.WorkerId == workerId);
                    if (workerTask != null)
                    {
                        workerTask.Url = url ?? string.Empty;
                    }
                }
            }
        }

        private static void RemoveWorker(string appUserId, Guid workerId)
        {
            lock (_lock)
            {
                if (_workersByUser.TryGetValue(appUserId, out var list))
                {
                    var index = list.FindIndex(w => w.WorkerId == workerId);
                    if (index >= 0)
                    {
                        list.RemoveAt(index);
                    }

                    if (list.Count == 0)
                    {
                        _workersByUser.Remove(appUserId);
                    }
                }
            }
        }
    }
}
