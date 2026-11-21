using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Collector.Common.Encryption;

namespace Collector.Chat.Cli;

/// <summary>
/// Journal-entry file access: reads {entryId:N}.json from the journal
/// storage path, transparently decrypting AES-encrypted files with the
/// owner's key from the database. Also locates media downloaded during
/// research (images under journal-entries/{entryId}/ and media modules in
/// the entry JSON).
/// </summary>
public class EntryStore
{
    private readonly CliConfig _config;
    private readonly Db _db;

    public EntryStore(CliConfig config, Db db)
    {
        _config = config;
        _db = db;
    }

    public string? EntryFilePath(Guid entryId, int journalId = 0)
    {
        if (string.IsNullOrWhiteSpace(_config.JournalPath)) return null;

        // New layout: journal-entries/{journalId}/{entryId}/entry.json. When
        // journalId is unknown, scan the journal folders for a match.
        if (journalId > 0)
        {
            var p = Path.Combine(_config.JournalPath, journalId.ToString(), $"{entryId:N}", "entry.json");
            if (File.Exists(p)) return p;
        }
        else
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(_config.JournalPath))
                {
                    var p = Path.Combine(dir, $"{entryId:N}", "entry.json");
                    if (File.Exists(p)) return p;
                }
            }
            catch { }
        }

        // Legacy root layout
        var path = Path.Combine(_config.JournalPath, $"{entryId:N}.json");
        return File.Exists(path) ? path : null;
    }

    /// <summary>The research/ folder for an entry —
    /// journal-entries/{journalId}/{entryId}/research/. Scans journal folders
    /// when journalId is unknown.</summary>
    public string? ResearchDir(Guid entryId, int journalId = 0)
    {
        if (string.IsNullOrWhiteSpace(_config.JournalPath)) return null;
        if (journalId > 0)
        {
            var d = Path.Combine(_config.JournalPath, journalId.ToString(), $"{entryId:N}", "research");
            return Directory.Exists(d) ? d : null;
        }
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(_config.JournalPath))
            {
                var d = Path.Combine(dir, $"{entryId:N}", "research");
                if (Directory.Exists(d)) return d;
            }
        }
        catch { }
        return null;
    }

    /// <summary>Raw entry JSON. Decrypts when the DB says the file is
    /// encrypted (or when the content isn't JSON but decryption works).</summary>
    public string? GetEntryJson(Guid entryId, out string note)
    {
        note = "";
        var row = _db.GetEntry(entryId);
        var path = EntryFilePath(entryId, row?.JournalId ?? 0);
        if (path == null)
        {
            note = _config.JournalPath == null
                ? "journal storage path unknown (no appsettings.json found)"
                : $"file not found under {_config.JournalPath}";
            return null;
        }

        var content = File.ReadAllText(path);

        var looksEncrypted = row?.Encrypted == true || !IsJson(content);
        if (looksEncrypted && row != null)
        {
            var key = _db.GetEncryptionKey(row.OwnerId);
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    var decrypted = Sha256.Decrypt(content, key);
                    if (IsJson(decrypted))
                    {
                        note = "(decrypted with the entry owner's AES key)";
                        return decrypted;
                    }
                }
                catch (Exception ex)
                {
                    note = $"decryption failed: {ex.Message}";
                }
            }
            else
            {
                note = "entry is encrypted but no encryption key found for the owner";
            }
        }

        if (!IsJson(content))
            note = string.IsNullOrEmpty(note)
                ? "file contents are not JSON (encrypted?)"
                : note;
        return content;
    }

    private static bool IsJson(string s)
    {
        var t = s.TrimStart();
        return t.StartsWith('{') || t.StartsWith('[');
    }

    /// <summary>
    /// Files downloaded for this entry during research: images stored under
    /// {Images}/journal-entries/{entryId}/ plus media/download URLs recorded
    /// in the entry JSON modules.
    /// </summary>
    public void PrintFiles(Guid entryId)
    {
        var shown = 0;

        // 1. Downloaded media on disk — new journals/{journalId}/{entryId}/ layout
        var row = _db.GetEntry(entryId);
        if (!string.IsNullOrWhiteSpace(_config.JournalPath) && row != null)
        {
            var entryDir = Path.Combine(_config.JournalPath, row.JournalId.ToString(), $"{entryId:N}");
            foreach (var sub in new[] { "images", "files", "videos" })
            {
                var dir = Path.Combine(entryDir, sub);
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                    .OrderBy(f => f).ToList();
                Console.WriteLine($"{sub} ({dir}):");
                foreach (var f in files)
                {
                    var fi = new FileInfo(f);
                    var thumb = fi.Name.StartsWith("thumb_") || fi.Name == "thumb.jpg" ? " (thumbnail)" : "";
                    Console.WriteLine($"  {fi.Name,-50} {fi.Length,10:N0} bytes{thumb}");
                }
                shown += files.Count;
                Console.WriteLine();
            }
        }

        // 2. Media/download modules inside the entry JSON
        var json = GetEntryJson(entryId, out var note);
        if (json != null)
        {
            try
            {
                var root = JsonNode.Parse(json) as JsonObject;
                var media = new List<JsonObject>();
                CollectMediaModules(root?["modules"] as JsonArray, media);

                if (media.Count > 0)
                {
                    Console.WriteLine("Media/file modules in the entry JSON:");
                    foreach (var mod in media)
                    {
                        var type = mod["type"]?.ToString() ?? "?";
                        var url = mod["url"]?.ToString() ?? "";
                        var name = mod["originalFilename"]?.ToString()
                            ?? mod["filename"]?.ToString()
                            ?? mod["title"]?.ToString() ?? "";
                        Console.WriteLine($"  [{type}] {name}");
                        if (!string.IsNullOrEmpty(url)) Console.WriteLine($"      {url}");
                    }
                    shown += media.Count;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"warning: could not parse entry JSON: {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(note))
        {
            Console.Error.WriteLine($"note: {note}");
        }

        if (shown == 0)
            Console.WriteLine("No downloaded files or media modules found for this entry.");
    }

    private static readonly HashSet<string> MediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image", "image-gallery", "file-download", "pdf-viewer", "video-player"
    };

    private static void CollectMediaModules(JsonArray? modules, List<JsonObject> found)
    {
        if (modules == null) return;
        foreach (var node in modules)
        {
            if (node is not JsonObject mod) continue;
            var type = mod["type"]?.ToString() ?? "";
            if (MediaTypes.Contains(type)) found.Add(mod);
            if (mod["modules"] is JsonArray children) CollectMediaModules(children, found);
            if (mod["tabs"] is JsonArray tabs)
                foreach (var tab in tabs)
                    if (tab is JsonObject t && t["modules"] is JsonArray tm)
                        CollectMediaModules(tm, found);
        }
    }

    /// <summary>
    /// Journal entry ids referenced by a chat session log: every GUID that
    /// appears in an entry link plus any GUID in the log that has a matching
    /// file on disk. DB chats.JournalEntryId is added by the caller.
    /// </summary>
    public static HashSet<Guid> EntryIdsFromLog(string logText, string? journalPath)
    {
        var ids = new HashSet<Guid>();

        // Explicit entry links: /journal/{journalId}/entry/{guid}
        foreach (Match m in Regex.Matches(logText, @"/journal/\d+/entry/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})"))
            if (Guid.TryParse(m.Groups[1].Value, out var id)) ids.Add(id);

        // Any GUID in the log that corresponds to an entry file on disk
        // (legacy root file or journal-entries/{journalId}/{entryId}/entry.json)
        if (!string.IsNullOrWhiteSpace(journalPath))
        {
            foreach (Match m in Regex.Matches(logText, @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b"))
            {
                if (!Guid.TryParse(m.Value, out var g)) continue;
                if (File.Exists(Path.Combine(journalPath, $"{g:N}.json")))
                {
                    ids.Add(g);
                    continue;
                }
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(journalPath))
                        if (File.Exists(Path.Combine(dir, $"{g:N}", "entry.json")))
                        {
                            ids.Add(g);
                            break;
                        }
                }
                catch { }
            }
        }
        return ids;
    }
}
