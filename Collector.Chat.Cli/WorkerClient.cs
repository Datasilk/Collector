using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Collector.Chat.Cli;

/// <summary>
/// Thin SignalR client for the server's WorkerHub (/worker). Mirrors what
/// the React client does: connect, RegisterUser, Call("chat-worker",
/// "SendMessage", args), then stream WorkerProgress events until a terminal
/// event (ChatResponse / ChatError / ChatStopped) or timeout.
/// </summary>
public class WorkerClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly string _appUserId;
    private readonly List<(string EventName, JsonElement Payload)> _events = new();
    private readonly TaskCompletionSource _terminal = new();
    private readonly HashSet<string> _entryIds = new(StringComparer.OrdinalIgnoreCase);
    private string? _workerId;
    private Guid? _chatId;
    private bool _verbose;

    private static readonly HashSet<string> TerminalEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "ChatResponse", "ChatError", "ChatStopped"
    };

    /// <summary>Events still arriving that must not end the wait: the plan
    /// summary is saved+sent as ChatResponse last, so it truly is terminal.</summary>
    public IReadOnlyList<(string EventName, JsonElement Payload)> Events => _events;
    public IReadOnlyCollection<string> EntryIds => _entryIds;
    public Guid? ChatId => _chatId;
    public string? WorkerId => _workerId;

    public WorkerClient(string serverUrl, string appUserId)
    {
        _appUserId = appUserId;
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl.TrimEnd('/') + "/worker")
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _connection.On<string, string, string, JsonElement>("WorkerProgress", (appUserId, workerId, eventName, payload) =>
        {
            // WorkerProgress is broadcast to every client — only handle ours.
            if (!string.Equals(appUserId, _appUserId, StringComparison.OrdinalIgnoreCase)) return;
            if (_workerId != null && !string.Equals(workerId, _workerId, StringComparison.OrdinalIgnoreCase)) return;

            _events.Add((eventName, payload));
            PrintEvent(eventName, payload);
            CaptureEntryIds(payload);
            if (eventName == "ChatCreated" &&
                payload.TryGetProperty("id", out var idEl) &&
                Guid.TryParse(idEl.GetString(), out var cid))
                _chatId = cid;

            if (TerminalEvents.Contains(eventName))
                _terminal.TrySetResult();
        });

        _connection.On<JsonElement>("JournalEntryUpdated", payload =>
        {
            if (payload.TryGetProperty("entryId", out var idEl))
            {
                var id = idEl.GetString();
                if (!string.IsNullOrEmpty(id)) _entryIds.Add(id);
            }
            if (_verbose)
                Console.WriteLine($"  [JournalEntryUpdated] {payload.GetRawText()}");
        });

        // The chat pipeline can ask the client for cookies when a fetch is
        // blocked. A headless CLI has no browser — answer immediately with an
        // empty string so the worker's 30s wait doesn't stall the plan.
        _connection.On<JsonElement>("RequestCookies", async request =>
        {
            try
            {
                var requestId = request.TryGetProperty("requestId", out var r) ? r.GetString() : null;
                if (requestId != null)
                    await _connection.InvokeAsync("CookieResponse", requestId, "");
            }
            catch { /* connection may be closing */ }
        });
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _connection.StartAsync(ct);
        await _connection.InvokeAsync("RegisterUser", _appUserId, ct);
    }

    /// <summary>
    /// Send a message through the chat-worker route and stream events to the
    /// console until a terminal event or the timeout fires. Returns the chat
    /// id when one was created/known.
    /// </summary>
    public async Task<Guid?> SendMessageAsync(
        string message, Guid? chatId, Guid? journalEntryId,
        string model, string mode, int timeoutSeconds, bool verbose,
        CancellationToken ct = default)
    {
        _verbose = verbose;
        var args = new { chatId, message, model, mode, journalEntryId };

        _workerId = (await _connection.InvokeAsync<JsonElement>(
            "Call", _appUserId, "chat-worker", "SendMessage", (object)args,
            (string?)null, (string?)null, ct)).GetString();

        Console.WriteLine($"workerId: {_workerId}");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await _terminal.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"\n(timed out after {timeoutSeconds}s waiting for a terminal event)");
        }
        return _chatId ?? chatId;
    }

    public async Task StopAsync()
    {
        if (_workerId == null) return;
        try
        {
            await _connection.InvokeAsync("Stop", _appUserId, Guid.Parse(_workerId));
        }
        catch { /* best effort */ }
    }

    private void CaptureEntryIds(JsonElement payload)
    {
        // Entry ids surface in ChatMessage linkUrls (/journal/{j}/entry/{id})
        // and inside step results. Scan the serialized payload for both.
        try
        {
            var text = payload.GetRawText();
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(
                         text, @"/journal/\d+/entry/([0-9a-fA-F-]{36})"))
                _entryIds.Add(m.Groups[1].Value);
            if (payload.ValueKind == JsonValueKind.Object &&
                payload.TryGetProperty("entryId", out var e) &&
                e.ValueKind == JsonValueKind.String)
                _entryIds.Add(e.GetString()!);
        }
        catch { /* malformed payloads are ignored */ }
    }

    private void PrintEvent(string eventName, JsonElement payload)
    {
        var text = payload.ValueKind == JsonValueKind.String
            ? payload.GetString() ?? ""
            : payload.GetRawText();

        switch (eventName)
        {
            case "PlanProgress":
                var pct = payload.TryGetProperty("percent", out var p) ? p.GetInt32() : 0;
                var msg = payload.TryGetProperty("message", out var m) ? m.GetString() : "";
                Console.Write($"\r  [{pct,3}%] {Truncate(msg, 90)}              \r");
                break;
            case "OllamaRequest":
            case "OllamaResponse":
            case "Debug":
            case "ServerException":
                if (_verbose) Console.WriteLine($"\n  [{eventName}] {Truncate(text, 2000)}");
                break;
            default:
                Console.WriteLine($"\n  [{eventName}] {Truncate(text, _verbose ? 4000 : 600)}");
                break;
        }
    }

    private static string Truncate(string? s, int n)
        => string.IsNullOrEmpty(s) ? "" : (s!.Length <= n ? s : s.Substring(0, n) + "…");

    public async ValueTask DisposeAsync()
    {
        try { await _connection.DisposeAsync(); } catch { }
    }
}
