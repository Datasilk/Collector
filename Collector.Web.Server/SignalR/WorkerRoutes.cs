using System;
using System.Collections.Concurrent;
using Collector.Common;

namespace Collector.Web.Server.SignalR
{
    public static class WorkerRoutes
    {
        private static readonly ConcurrentDictionary<string, Type> _routes = new(StringComparer.OrdinalIgnoreCase);

        public static void Register<TWorker>(string route) where TWorker : class, IWorker
        {
            if (string.IsNullOrWhiteSpace(route)) throw new ArgumentNullException(nameof(route));
            _routes[route] = typeof(TWorker);
        }

        public static Type GetWorkerType(string route)
        {
            if (string.IsNullOrWhiteSpace(route)) throw new ArgumentNullException(nameof(route));
            if (_routes.TryGetValue(route, out var type))
            {
                return type;
            }

            throw new InvalidOperationException($"Worker route not registered: {route}");
        }
    }
}
