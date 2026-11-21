using System.Text.Json;
using System.Text.Json.Nodes;
using Collector.Chat.Cli;

/// <summary>
/// collector-chat — CLI test harness for the Collector chat system.
/// Talks to the web server's WorkerHub (/worker) over SignalR exactly the
/// way the React client does, and inspects chat logs + journal entry files
/// produced by research runs.
/// </summary>
return await Run(args);

static async Task<int> Run(string[] argv)
{
    var args = argv.ToList();
    if (args.Count == 0 || args.Contains("--help") || args.Contains("-h") || args[0] == "help")
    {
        PrintHelp();
        return args.Count == 0 ? 1 : 0;
    }

    var command = args[0].ToLowerInvariant();
    args.RemoveAt(0);

    var server = Opt(args, "--server");
    var config = CliConfig.Load(Opt(args, "--config"), server);
    var db = new Db(config.ConnectionString);
    var entries = new EntryStore(config, db);

    try
    {
        switch (command)
        {
            case "status": return Status(config, db);
            case "logs": return Logs(config, Int(args, "--limit") ?? 25);
            case "read": return ReadLog(config, Arg(args, 0, "chat-id"));
            case "details": return Details(config, Int(args, "--limit") ?? 25);
            case "detail": return ReadDetail(config, Arg(args, 0, "chat-id"));
            case "dumps": return Dumps(config, Arg(args, 0, "chat-id"));
            case "dump": return Dump(config, Arg(args, 0, "chat-id"), Arg(args, 1, "file"));
            case "chats": return Chats(db, Int(args, "--limit") ?? 25);
            case "entries": return Entries(config, db, Arg(args, 0, "chat-id"));
            case "entry": return Entry(entries, Arg(args, 0, "entry-id"), args.Contains("--raw"));
            case "files": return Files(entries, Arg(args, 0, "entry-id"));
            case "research": return Research(entries, db, Arg(args, 0, "entry-id"), Arg(args, 1, "file"));
            case "send": return await Send(config, db, args);
            default:
                Console.Error.WriteLine($"unknown command: {command}\n");
                PrintHelp();
                return 1;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
        return 1;
    }
}

// ---------- commands ----------

static int Status(CliConfig config, Db db)
{
    var ok = true;

    // server
    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var resp = http.PostAsync($"{config.ServerUrl}/worker/negotiate?negotiateVersion=1", null).Result;
        Console.WriteLine($"server   {config.ServerUrl}  ->  {(int)resp.StatusCode} {resp.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"server   {config.ServerUrl}  ->  unreachable ({ex.Message})");
        ok = false;
    }

    // ollama
    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var resp = http.GetAsync("http://localhost:11434/api/tags").Result;
        Console.WriteLine($"ollama   http://localhost:11434  ->  {(int)resp.StatusCode} {resp.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ollama   http://localhost:11434  ->  unreachable ({ex.Message})");
    }

    // database
    Console.WriteLine(db.Available
        ? $"database -> {(db.Ping() ? "connected" : "UNREACHABLE")}"
        : "database -> no connection string (appsettings.json not found)");
    if (db.Available && !db.Ping()) ok = false;

    Console.WriteLine($"appsettings: {config.AppSettingsPath ?? "not found"}");
    Console.WriteLine($"chat logs:   {config.ChatLogsDir ?? "unknown"}");
    Console.WriteLine($"journal dir: {config.JournalPath ?? "unknown"}");
    return ok ? 0 : 1;
}

static int Logs(CliConfig config, int limit)
{
    if (string.IsNullOrWhiteSpace(config.ChatLogsDir) || !Directory.Exists(config.ChatLogsDir))
    {
        Console.Error.WriteLine("chat log directory not found — run the server in Development mode first");
        return 1;
    }

    var files = new DirectoryInfo(config.ChatLogsDir).GetFiles("*.log")
        .OrderByDescending(f => f.LastWriteTime).Take(limit).ToList();

    Console.WriteLine($"{files.Count} chat log(s), newest first ({config.ChatLogsDir}):\n");
    foreach (var f in files)
        Console.WriteLine($"  {f.LastWriteTime:yyyy-MM-dd HH:mm:ss}  {Path.GetFileNameWithoutExtension(f.Name)}  {f.Length,8:N0} bytes");
    return 0;
}

static int ReadLog(CliConfig config, string chatId)
{
    var path = LogPath(config, chatId);
    if (path == null)
    {
        Console.Error.WriteLine($"no chat log for {chatId}");
        return 1;
    }
    Console.WriteLine($"# {path}\n");
    Console.Write(File.ReadAllText(path));
    return 0;
}

static string? DetailsDir(CliConfig config)
{
    if (string.IsNullOrWhiteSpace(config.ChatLogsDir)) return null;
    var dir = Path.Combine(config.ChatLogsDir, "details");
    return Directory.Exists(dir) ? dir : null;
}

static int Details(CliConfig config, int limit)
{
    var dir = DetailsDir(config);
    if (dir == null)
    {
        Console.Error.WriteLine("no detail logs yet — they are written by a Development-mode server");
        return 1;
    }

    var files = new DirectoryInfo(dir).GetFiles("*.log")
        .OrderByDescending(f => f.LastWriteTime).Take(limit).ToList();

    Console.WriteLine($"{files.Count} detail log(s), newest first ({dir}):\n");
    foreach (var f in files)
        Console.WriteLine($"  {f.LastWriteTime:yyyy-MM-dd HH:mm:ss}  {Path.GetFileNameWithoutExtension(f.Name)}  {f.Length,8:N0} bytes");
    return 0;
}

static int ReadDetail(CliConfig config, string chatId)
{
    var dir = DetailsDir(config);
    var name = chatId.EndsWith(".log") ? chatId : chatId + ".log";
    var path = dir != null ? Path.Combine(dir, name) : null;
    if (path == null || !File.Exists(path))
    {
        Console.Error.WriteLine($"no detail log for {chatId}");
        return 1;
    }
    Console.WriteLine($"# {path}\n");
    Console.Write(File.ReadAllText(path));
    return 0;
}

static int Dumps(CliConfig config, string chatId)
{
    var dir = DetailsDir(config);
    var sessionDir = dir != null ? Path.Combine(dir, chatId) : null;
    if (sessionDir == null || !Directory.Exists(sessionDir))
    {
        Console.Error.WriteLine($"no dump directory for {chatId}");
        return 1;
    }

    var files = new DirectoryInfo(sessionDir).GetFiles("*.txt").OrderBy(f => f.Name).ToList();
    Console.WriteLine($"{files.Count} dump file(s) for {chatId} ({sessionDir}):\n");
    foreach (var f in files)
        Console.WriteLine($"  {f.Name}  {f.Length,10:N0} bytes");
    return 0;
}

static int Dump(CliConfig config, string chatId, string file)
{
    var dir = DetailsDir(config);
    var sessionDir = dir != null ? Path.Combine(dir, chatId) : null;
    var path = sessionDir != null ? Path.Combine(sessionDir, file.EndsWith(".txt") ? file : file + ".txt") : null;
    if (path == null || !File.Exists(path))
    {
        Console.Error.WriteLine($"no dump file '{file}' for {chatId}");
        return 1;
    }
    Console.WriteLine($"# {path}\n");
    Console.Write(File.ReadAllText(path));
    return 0;
}

static int Chats(Db db, int limit)
{
    var rows = db.ListChats(limit);
    if (rows.Count == 0)
    {
        Console.WriteLine("no chats found (or database unreachable)");
        return 1;
    }
    Console.WriteLine($"{rows.Count} chat(s), newest first:\n");
    foreach (var c in rows)
        Console.WriteLine($"  {c.Modified:yyyy-MM-dd HH:mm}  {c.Id}  {(c.JournalEntryId.HasValue ? $"entry={c.JournalEntryId}  " : "")}{c.Title}");
    return 0;
}

static int Entries(CliConfig config, Db db, string chatIdStr)
{
    if (!Guid.TryParse(chatIdStr, out var chatId))
    {
        Console.Error.WriteLine("entries requires a chat id (guid)");
        return 1;
    }

    var ids = new HashSet<Guid>();

    var path = LogPath(config, chatId.ToString());
    if (path != null)
        ids.UnionWith(EntryStore.EntryIdsFromLog(File.ReadAllText(path), config.JournalPath));

    var chat = db.GetChat(chatId);
    if (chat?.JournalEntryId is Guid linked && linked != Guid.Empty)
        ids.Add(linked);

    if (ids.Count == 0)
    {
        Console.WriteLine("no journal entries found for this chat");
        if (path == null) Console.WriteLine("(no chat log either — was this session run in Development mode?)");
        return 1;
    }

    Console.WriteLine($"{ids.Count} journal entr{(ids.Count == 1 ? "y" : "ies")} touched by chat {chatId}:\n");
    foreach (var id in ids)
    {
        var row = db.GetEntry(id);
        if (row != null)
            Console.WriteLine($"  {id}  \"{row.Title}\"  journal={row.JournalId} \"{row.JournalTitle}\"  modified={row.Modified:yyyy-MM-dd HH:mm}{(row.Encrypted ? "  [encrypted]" : "")}");
        else
            Console.WriteLine($"  {id}  (metadata unavailable)");
    }
    return 0;
}

static int Entry(EntryStore entries, string entryIdStr, bool raw)
{
    if (!Guid.TryParse(entryIdStr, out var entryId))
    {
        Console.Error.WriteLine("entry requires a journal entry id (guid)");
        return 1;
    }

    var json = entries.GetEntryJson(entryId, out var note);
    if (json == null)
    {
        Console.Error.WriteLine($"could not read entry: {note}");
        return 1;
    }

    if (!string.IsNullOrEmpty(note))
        Console.WriteLine($"({note})\n");

    if (!raw)
    {
        try
        {
            var node = JsonNode.Parse(json);
            Console.WriteLine(node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch { /* fall through to raw print */ }
    }
    Console.WriteLine(json);
    return 0;
}

static int Files(EntryStore entries, string entryIdStr)
{
    if (!Guid.TryParse(entryIdStr, out var entryId))
    {
        Console.Error.WriteLine("files requires a journal entry id (guid)");
        return 1;
    }
    entries.PrintFiles(entryId);
    return 0;
}

/// <summary>
/// research <entry-id> [file] — list or print the parser-model JSON dumps in
/// journal-entries/{journalId}/{entryId}/research/ (0001.json, 0002.json…).
/// </summary>
static int Research(EntryStore entries, Db db, string entryIdStr, string? file)
{
    if (!Guid.TryParse(entryIdStr, out var entryId))
    {
        Console.Error.WriteLine("research requires a journal entry id (guid)");
        return 1;
    }

    var dir = entries.ResearchDir(entryId, db.GetEntry(entryId)?.JournalId ?? 0);
    if (dir == null)
    {
        Console.WriteLine("no research folder found for this entry");
        return 1;
    }

    var files = Directory.GetFiles(dir, "*.json").OrderBy(f => f).ToList();
    if (string.IsNullOrWhiteSpace(file))
    {
        Console.WriteLine($"{files.Count} research file(s) in {dir}:\n");
        foreach (var f in files)
            Console.WriteLine($"  {Path.GetFileName(f),-12} {new FileInfo(f).Length,10:N0} bytes");
        return 0;
    }

    var target = files.FirstOrDefault(f =>
        Path.GetFileName(f).Equals(file, StringComparison.OrdinalIgnoreCase) ||
        Path.GetFileName(f).Equals(file.TrimStart('0').PadLeft(4, '0') + ".json", StringComparison.OrdinalIgnoreCase));
    if (target == null)
    {
        Console.Error.WriteLine($"research file '{file}' not found — {files.Count} file(s) exist");
        return 1;
    }
    Console.WriteLine(File.ReadAllText(target));
    return 0;
}


static async Task<int> Send(CliConfig config, Db db, List<string> args)
{
    var message = string.Join(' ', Positional(args));
    if (string.IsNullOrWhiteSpace(message))
    {
        Console.Error.WriteLine("send requires a message");
        return 1;
    }

    Guid? chatId = Guid.TryParse(Opt(args, "--chat-id"), out var c) ? c : null;
    Guid? journalEntryId = Guid.TryParse(Opt(args, "--journal-entry-id"), out var j) ? j : null;
    var model = Opt(args, "--model") ?? "collector-chat-cli";
    var mode = Opt(args, "--mode") ?? "agent";
    var timeout = Int(args, "--timeout") ?? 900;
    var verbose = args.Contains("--raw") || args.Contains("--verbose");

    var user = Opt(args, "--user") ?? db.ResolveDefaultUser()?.ToString();
    if (string.IsNullOrWhiteSpace(user) || !Guid.TryParse(user, out _))
    {
        Console.Error.WriteLine("could not resolve an appUserId — pass --user <guid>");
        return 1;
    }

    await using var client = new WorkerClient(config.ServerUrl, user);
    Console.WriteLine($"connecting to {config.ServerUrl}/worker as {user}…");
    await client.ConnectAsync();

    var resultChatId = await client.SendMessageAsync(
        message, chatId, journalEntryId, model, mode, timeout, verbose);

    Console.WriteLine();
    if (resultChatId.HasValue)
        Console.WriteLine($"chatId: {resultChatId}");
    if (client.EntryIds.Count > 0)
        Console.WriteLine($"journal entries: {string.Join(", ", client.EntryIds)}");
    if (!client.EntryIds.Any() && resultChatId.HasValue)
        Console.WriteLine("(no journal entries referenced in this run)");
    return 0;
}

// ---------- helpers ----------

static string? LogPath(CliConfig config, string chatId)
{
    if (string.IsNullOrWhiteSpace(config.ChatLogsDir)) return null;
    var name = chatId.EndsWith(".log") ? chatId : chatId + ".log";
    var path = Path.Combine(config.ChatLogsDir, name);
    return File.Exists(path) ? path : null;
}

static string? Opt(List<string> args, string name)
{
    var i = args.IndexOf(name);
    return i >= 0 && i + 1 < args.Count ? args[i + 1] : null;
}

static int? Int(List<string> args, string name)
    => int.TryParse(Opt(args, name), out var v) ? v : null;

static string Arg(List<string> args, int index, string name)
{
    var positional = Positional(args);
    if (positional.Count <= index)
        throw new ArgumentException($"missing required argument: {name}");
    return positional[index];
}

static bool IsValueOption(string arg) => arg switch
{
    "--server" or "--user" or "--config" or "--chat-id" or "--journal-entry-id"
        or "--model" or "--mode" or "--timeout" or "--limit" => true,
    _ => false
};

static List<string> Positional(List<string> args)
{
    var list = new List<string>();
    for (var i = 0; i < args.Count; i++)
    {
        if (args[i].StartsWith("--"))
        {
            if (IsValueOption(args[i])) i++; // skip the option's value too
            continue;
        }
        list.Add(args[i]);
    }
    return list;
}

static void PrintHelp()
{
    Console.WriteLine("""
collector-chat — CLI test harness for the Collector chat system

Usage: collector-chat <command> [args] [options]

Commands:
  status                       Check server, Ollama and database connectivity
  send <message>               Send a chat message and stream worker events
      --chat-id <guid>         Continue an existing chat
      --journal-entry-id <g>   Work on a specific journal entry
      --model <name>           Model label saved on history rows (default: collector-chat-cli)
      --mode <ask|agent>       Chat mode (default: agent)
      --timeout <sec>          Max wait for a terminal event (default: 900)
      --raw / --verbose        Print every event payload (incl. raw Ollama I/O)
  logs [--limit N]             List chat logs, newest first (default 25)
  read <chatId>                Print a chat log
  details [--limit N]          List detail debug logs (logs/chat/details)
  detail <chatId>              Print a detail debug log
  dumps <chatId>               List content/fact dump files for a chat
  dump <chatId> <file>         Print a dump file (e.g. 001.txt, 001-facts.txt)
  chats [--limit N]            List chats from the database, newest first
  entries <chatId>             List journal entries produced in a chat session
  entry <entryId> [--raw]      Print a journal entry's JSON (decrypts if needed)
  files <entryId>              List files/images downloaded for an entry
  research <entryId> [file]    List or print research JSON dumps

Global options:
  --server <url>               Server base URL (default http://localhost:7780)
  --user <guid>                appUserId (default: owner of the most recent chat)
  --config <path>              Path to the server's appsettings.json
                               (or set COLLECTOR_CONFIG)

Examples:
  collector-chat status
  collector-chat send "Research the history of Stoicism and build a journal entry"
  collector-chat logs
  collector-chat entries 0bb991f4-3a07-44cc-b0af-ec3eefbdbd80
  collector-chat entry 002d7c4e-66cb-408b-b064-6d9aacb924c3
  collector-chat files 002d7c4e-66cb-408b-b064-6d9aacb924c3
""");
}
