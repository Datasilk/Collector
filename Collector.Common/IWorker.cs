using System.Threading;
using System.Threading.Tasks;

namespace Collector.Common
{
    /// <summary>
    /// Base interface for workers. Workers define public async methods that can be invoked
    /// dynamically by name, similar to SignalR hubs. Methods receive appUserId, workerId,
    /// and a CancellationToken as the first three parameters, followed by any custom args.
    /// </summary>
    public interface IWorker
    {
        Task Stop();

        /// <summary>
        /// Request to send details about worker progress
        /// </summary>
        Task Progress(string appUserId, Guid workerId);
    }
}