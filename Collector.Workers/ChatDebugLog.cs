using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace Collector.Workers;

/// <summary>
/// Development-only per-chat-session debug log. Every SignalR event emitted
/// during a chat is appended to logs/chat/{chatId}.log under the server
/// content root so a session can be replayed from a file instead of the
/// browser console. Events arriving before the chat row exists (new chats
/// are created mid-flow) are buffered and flushed once the id is known.
/// </summary>
public sealed class ChatDebugLog
{
    private readonly bool _enabled;
    private readonly string _logDir = "";
    private readonly List<string> _pending = new();
    private readonly object _lock = new();
    private Guid _chatId;

    public ChatDebugLog(IHostEnvironment environment)
    {
        _enabled = environment.IsDevelopment();
        if (_enabled)
            _logDir = Path.Combine(environment.ContentRootPath, "logs", "chat");
    }

    /// <summary>Bind the log to a chat session; flushes any buffered lines.</summary>
    public void SetChatId(Guid chatId)
    {
        if (!_enabled || chatId == Guid.Empty)
            return;
        lock (_lock)
        {
            if (_chatId == chatId)
                return;
            var firstBind = _chatId == Guid.Empty;
            _chatId = chatId;
            if (firstBind)
                Append($"========== chat session {chatId} ==========");
            foreach (var line in _pending)
                Append(line);
            _pending.Clear();
        }
    }

    /// <summary>Log a worker event (SignalR event name + payload).</summary>
    public void Log(string eventName, object? payload)
    {
        if (!_enabled)
            return;
        string text;
        try
        {
            text = payload is string s ? s : JsonSerializer.Serialize(payload);
        }
        catch
        {
            text = payload?.ToString() ?? "";
        }
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {eventName} :: {text}";
        lock (_lock)
        {
            if (_chatId == Guid.Empty)
                _pending.Add(line);
            else
                Append(line);
        }
    }

    private void Append(string line)
    {
        try
        {
            Directory.CreateDirectory(_logDir);
            File.AppendAllText(Path.Combine(_logDir, _chatId + ".log"), line + Environment.NewLine);
        }
        catch
        {
            // Logging must never break the chat path.
        }
    }
}
