using System.Text.Json;

namespace Collector.Chat.Cli;

/// <summary>
/// Resolves everything the CLI needs from the web server's appsettings.json:
/// the Postgres connection string, storage paths, and the logs directory.
/// Server URL defaults to http://localhost:7780 (the "https" launch profile
/// also binds plain HTTP on 7780).
/// </summary>
public class CliConfig
{
    public string ServerUrl { get; set; } = "http://localhost:7780";
    public string? ConnectionString { get; set; }
    public string? JournalPath { get; set; }
    public string? ChatLogsDir { get; set; }
    public string? AppSettingsPath { get; set; }

    /// <summary>
    /// Find the server's appsettings.json. Search order: --config arg,
    /// COLLECTOR_CONFIG env var, then walk up from the working directory
    /// looking for Collector.Web.Server\appsettings.json.
    /// </summary>
    public static CliConfig Load(string? explicitPath = null, string? serverUrl = null)
    {
        var config = new CliConfig();
        var path = ResolveAppSettingsPath(explicitPath);
        config.AppSettingsPath = path;

        if (path != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;

                if (root.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("Database", out var db))
                    config.ConnectionString = db.GetString();

                if (root.TryGetProperty("Storage", out var storage))
                {
                    config.JournalPath = GetStoragePath(storage, "Journal");
                }

                // Chat debug logs live under the server's content root.
                config.ChatLogsDir = Path.Combine(
                    Path.GetDirectoryName(path)!, "logs", "chat");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"warning: could not parse {path}: {ex.Message}");
            }
        }

        if (!string.IsNullOrWhiteSpace(serverUrl))
            config.ServerUrl = serverUrl.TrimEnd('/');

        return config;
    }

    private static string? GetStoragePath(JsonElement storage, string key)
    {
        if (storage.TryGetProperty(key, out var el))
        {
            var v = el.GetString();
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        return null;
    }

    private static string? ResolveAppSettingsPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return File.Exists(explicitPath) ? Path.GetFullPath(explicitPath) : null;

        var env = Environment.GetEnvironmentVariable("COLLECTOR_CONFIG");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            return Path.GetFullPath(env);

        // Walk up from cwd looking for Collector.Web.Server\appsettings.json
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var d = dir; d != null; d = d.Parent)
        {
            // cwd may already be inside Collector.Web.Server
            var direct = Path.Combine(d.FullName, "appsettings.json");
            var nested = Path.Combine(d.FullName, "Collector.Web.Server", "appsettings.json");
            if (File.Exists(nested)) return nested;
            if (File.Exists(direct) &&
                (d.Name.Equals("Collector.Web.Server", StringComparison.OrdinalIgnoreCase) ||
                 File.Exists(Path.Combine(d.FullName, "Collector.Web.Server.csproj"))))
                return direct;
        }
        return null;
    }
}
