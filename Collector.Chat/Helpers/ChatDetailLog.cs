using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Collector.Chat.Helpers;

/// <summary>
/// Development-only debugging log, written to logs/chat/details/{chatId}.log
/// under the server content root — same filenaming as the SignalR event log
/// in logs/chat/, but only records what helps debug a research run: the user
/// message, plan steps, tool calls, search-engine traffic, candidate links
/// with relevance verdicts, per-page content/fact dumps, topics added, and
/// every function entry inside the tools.
///
/// Ambient access: the worker assigns <see cref="Current"/> before executing
/// a plan; every tool and helper then logs via the static helpers with no
/// plumbing through call signatures. AsyncLocal keeps concurrent chats
/// isolated. Everything is a no-op when disabled or unbound — logging must
/// never break the research path.
/// </summary>
public sealed class ChatDetailLog
{
    private static readonly AsyncLocal<ChatDetailLog?> _current = new();

    /// <summary>The log bound to the current async context (set by the worker).</summary>
    public static ChatDetailLog? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    private readonly bool _enabled;
    private readonly string _logDir = "";
    private readonly List<string> _pending = new();
    private readonly object _lock = new();
    private Guid _chatId;
    private int _dumpCounter;

    public ChatDetailLog(string contentRoot, bool enabled)
    {
        _enabled = enabled;
        if (_enabled)
            _logDir = Path.Combine(contentRoot, "logs", "chat", "details");
    }

    /// <summary>Bind the log to a chat session; flushes buffered lines.</summary>
    public void SetChatId(Guid chatId)
    {
        if (!_enabled || chatId == Guid.Empty) return;
        lock (_lock)
        {
            if (_chatId == chatId) return;
            var firstBind = _chatId == Guid.Empty;
            _chatId = chatId;
            if (firstBind)
                Append($"========== detail log for chat {chatId} ==========");
            foreach (var line in _pending) Append(line);
            _pending.Clear();
        }
    }

    /// <summary>Append one timestamped line to the detail log.</summary>
    public void Write(string text)
    {
        if (!_enabled) return;
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {text}";
        lock (_lock)
        {
            if (_chatId == Guid.Empty) _pending.Add(line);
            else Append(line);
        }
    }

    /// <summary>Write a dump file into {chatId}/ and log its link line.
    /// Returns the generated filename (e.g. "003.txt"), or null when the
    /// dump wasn't written.</summary>
    private string? DumpFile(string suffix, string content, bool newFile = true)
    {
        if (!_enabled || _chatId == Guid.Empty) return null;
        try
        {
            var dir = Path.Combine(_logDir, _chatId.ToString());
            Directory.CreateDirectory(dir);
            // Facts dumps share the number of their page's content dump
            // (001.txt → 001-facts.txt); only content dumps advance it.
            if (newFile || _dumpCounter == 0) _dumpCounter++;
            var name = $"{_dumpCounter:000}{suffix}.txt";
            File.WriteAllText(Path.Combine(dir, name), content);
            return name;
        }
        catch { return null; }
    }

    private void Append(string line)
    {
        try
        {
            Directory.CreateDirectory(_logDir);
            File.AppendAllText(Path.Combine(_logDir, _chatId + ".log"), line + Environment.NewLine);
        }
        catch { /* logging must never break the chat path */ }
    }

    // ---- Static helpers (no-op without a bound log) ----

    /// <summary>Free-form line.</summary>
    public static void Line(string text) => Current?.Write(text);

    /// <summary>Function entry inside a tool — "fn {tool}.{function}".</summary>
    public static void Fn(string tool, string function) =>
        Current?.Write($"fn {tool}.{function}");

    /// <summary>A search-engine URL about to be requested.</summary>
    public static void Search(string engine, string url) =>
        Current?.Write($"search {engine}: {url}");

    /// <summary>The candidate link list with relevance verdicts.</summary>
    public static void Links(IEnumerable<(string Title, string Url, bool Relevant)> links)
    {
        var log = Current; if (log == null) return;
        var items = new List<object>();
        foreach (var l in links)
            items.Add(new { title = l.Title, url = l.Url, relevant = l.Relevant });
        log.Write("links: " + JsonSerializer.Serialize(items));
    }

    /// <summary>Dump raw extracted page content → {session}/NNN.txt and log the link line.</summary>
    public static void DumpPageContent(string url, string content)
    {
        var log = Current; if (log == null) return;
        var name = log.DumpFile("", content ?? "");
        if (name != null) log.Write($"downloaded content into {name} from {url}");
    }

    /// <summary>Dump the extracted fact list → {session}/NNN-facts.txt and log the link line.</summary>
    public static void DumpFacts(string url, IEnumerable<string> facts)
    {
        var log = Current; if (log == null) return;
        var sb = new StringBuilder();
        foreach (var f in facts) sb.AppendLine("- " + f);
        var name = log.DumpFile("-facts", sb.ToString(), newFile: false);
        if (name != null) log.Write($"extracted facts into {name} from {url}");
    }

    /// <summary>A topic or subtopic added to the research tree (indent by depth).</summary>
    public static void TopicAdded(string title, int depth) =>
        Current?.Write(new string(' ', depth * 2) + $"- {title}");
}
