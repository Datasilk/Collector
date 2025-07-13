using Microsoft.AspNetCore.SignalR;

namespace Collector.SignalR
{
    public class DashboardHub : Hub
    {
        public async Task handshake()
        {
            await Clients.Caller.SendAsync("update", "Connected to Collector server v0.1.0");
        }
    }
}
