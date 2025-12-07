using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Collector.Web.Server.SignalR
{
    /// <summary>
    /// Cookie data structure returned from Chrome extension
    /// </summary>
    public class CookieData
    {
        public string Domain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
        public double ExpirationDate { get; set; }
        public string? SameSite { get; set; }
    }

    /// <summary>
    /// Pending cookie request tracking
    /// </summary>
    public class PendingCookieRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public TaskCompletionSource<List<CookieData>> TaskCompletionSource { get; set; } = default!;
        public CancellationTokenRegistration CancellationRegistration { get; set; }
    }

    /// <summary>
    /// SignalR hub for Chrome extension communication.
    /// Handles cookie requests from workers and responses from Chrome extensions.
    /// Authentication is done via the Authenticate method after connection.
    /// </summary>
    public class ChromeExtensionHub : Hub
    {
        private readonly ILogger<ChromeExtensionHub> _logger;
        private readonly IConfiguration _configuration;

        // Maps connectionId -> appUserId (only authenticated connections)
        private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();
        
        // Maps appUserId -> list of connectionIds (user can have multiple extensions)
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
        
        // Tracks which connections are authenticated
        private static readonly ConcurrentDictionary<string, bool> _authenticatedConnections = new();
        
        // Pending cookie requests: requestId -> PendingCookieRequest
        private static readonly ConcurrentDictionary<string, PendingCookieRequest> _pendingRequests = new();

        private static readonly object _lock = new();

        public ChromeExtensionHub(ILogger<ChromeExtensionHub> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Authenticate the connection using a JWT token.
        /// Must be called before any other methods will work.
        /// </summary>
        public async Task<bool> Authenticate(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Authenticate called with empty token");
                return false;
            }

            var connectionId = Context.ConnectionId;

            try
            {
                // Validate the JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Auth:JWT:Secret"] ?? "");
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidAudience = _configuration["Auth:JWT:ValidAudience"],
                    ValidIssuer = _configuration["Auth:JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                // Extract appUserId from claims (claim name is "AppUser" in AuthService)
                var appUserIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "AppUser");
                if (appUserIdClaim == null || string.IsNullOrWhiteSpace(appUserIdClaim.Value))
                {
                    _logger.LogWarning("Token valid but no AppUser claim found. Available claims: {Claims}", 
                        string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}")));
                    return false;
                }

                var appUserId = appUserIdClaim.Value;

                // Mark connection as authenticated and register user
                lock (_lock)
                {
                    _authenticatedConnections[connectionId] = true;
                    _connectionUsers[connectionId] = appUserId;

                    if (!_userConnections.TryGetValue(appUserId, out var connections))
                    {
                        connections = new HashSet<string>();
                        _userConnections[appUserId] = connections;
                    }
                    connections.Add(connectionId);
                }

                _logger.LogInformation("Chrome extension authenticated: User={AppUserId}, Connection={ConnectionId}", appUserId, connectionId);
                return true;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return false;
            }
        }

        /// <summary>
        /// Check if the current connection is authenticated
        /// </summary>
        private bool IsAuthenticated()
        {
            return _authenticatedConnections.TryGetValue(Context.ConnectionId, out var isAuth) && isAuth;
        }

        /// <summary>
        /// Called by Chrome extension to respond to a cookie request.
        /// Requires authentication.
        /// </summary>
        public Task CookieResponse(string requestId, List<CookieData> cookies)
        {
            if (!IsAuthenticated())
            {
                _logger.LogWarning("CookieResponse called by unauthenticated connection: {ConnectionId}", Context.ConnectionId);
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(requestId))
            {
                _logger.LogWarning("CookieResponse called with empty requestId");
                return Task.CompletedTask;
            }

            if (_pendingRequests.TryRemove(requestId, out var pending))
            {
                pending.CancellationRegistration.Dispose();
                pending.TaskCompletionSource.TrySetResult(cookies ?? new List<CookieData>());
                _logger.LogInformation("Cookie response received: RequestId={RequestId}, CookieCount={Count}", requestId, cookies?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("Cookie response for unknown request: RequestId={RequestId}", requestId);
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            lock (_lock)
            {
                // Remove from authenticated connections
                _authenticatedConnections.TryRemove(connectionId, out _);
                
                if (_connectionUsers.TryRemove(connectionId, out var appUserId))
                {
                    if (_userConnections.TryGetValue(appUserId, out var connections))
                    {
                        connections.Remove(connectionId);
                        if (connections.Count == 0)
                        {
                            _userConnections.TryRemove(appUserId, out _);
                        }
                    }
                    _logger.LogInformation("Chrome extension disconnected: User={AppUserId}, Connection={ConnectionId}", appUserId, connectionId);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Static method to request cookies from a user's Chrome extension.
        /// Called by workers that need browser cookies.
        /// </summary>
        public static async Task<List<CookieData>> RequestCookiesAsync(
            IHubContext<ChromeExtensionHub> hubContext,
            string appUserId,
            string domain,
            CancellationToken cancellationToken = default,
            int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(appUserId) || string.IsNullOrWhiteSpace(domain))
            {
                return new List<CookieData>();
            }

            // Get connections for user
            HashSet<string>? connections;
            lock (_lock)
            {
                if (!_userConnections.TryGetValue(appUserId, out connections) || connections.Count == 0)
                {
                    return new List<CookieData>();
                }
                // Copy to avoid holding lock
                connections = new HashSet<string>(connections);
            }

            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<List<CookieData>>();

            // Setup timeout
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var pending = new PendingCookieRequest
            {
                RequestId = requestId,
                Domain = domain,
                TaskCompletionSource = tcs
            };

            // Register cancellation
            pending.CancellationRegistration = linkedCts.Token.Register(() =>
            {
                if (_pendingRequests.TryRemove(requestId, out _))
                {
                    tcs.TrySetResult(new List<CookieData>());
                }
            });

            _pendingRequests[requestId] = pending;

            try
            {
                // Send request to all user's connections (first response wins)
                foreach (var connectionId in connections)
                {
                    await hubContext.Clients.Client(connectionId).SendAsync("RequestCookie", requestId, domain, linkedCts.Token);
                }

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _pendingRequests.TryRemove(requestId, out _);
                return new List<CookieData>();
            }
        }

        /// <summary>
        /// Check if a user has any connected Chrome extensions
        /// </summary>
        public static bool HasConnectedExtension(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) return false;

            lock (_lock)
            {
                return _userConnections.TryGetValue(appUserId, out var connections) && connections.Count > 0;
            }
        }

        /// <summary>
        /// Get count of connected extensions for a user
        /// </summary>
        public static int GetConnectionCount(string appUserId)
        {
            if (string.IsNullOrWhiteSpace(appUserId)) return 0;

            lock (_lock)
            {
                if (_userConnections.TryGetValue(appUserId, out var connections))
                {
                    return connections.Count;
                }
            }
            return 0;
        }
    }
}
