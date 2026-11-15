using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Common.Models.JournalModules;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Workers.SignalR;
using Microsoft.Extensions.Logging;

namespace Collector.Workers
{
    /// <summary>
    /// Result of creating a journal entry from a YouTube video URL.
    /// </summary>
    public class YouTubeEntryResult
    {
        public Guid EntryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoModuleId { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shared logic for fetching YouTube metadata via yt-dlp and creating
    /// journal entries from YouTube video URLs. Used by WebContentHub (ScrapeUrl)
    /// and YoutubeChannelWorker.
    /// </summary>
    public class YouTubeEntryCreator
    {
        private readonly ILogger<YouTubeEntryCreator> _logger;
        private readonly IJournalEntriesRepository _entriesRepository;
        private readonly IJournalEntryTagsRepository _entryTagsRepository;
        private readonly Random _random = new();

        public YouTubeEntryCreator(
            ILogger<YouTubeEntryCreator> logger,
            IJournalEntriesRepository entriesRepository,
            IJournalEntryTagsRepository entryTagsRepository)
        {
            _logger = logger;
            _entriesRepository = entriesRepository;
            _entryTagsRepository = entryTagsRepository;
        }

        private static readonly string CookiesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "cookies");

        public static string GetCookiesArgument(string appUserId)
        {
            if (string.IsNullOrEmpty(appUserId))
                return string.Empty;

            // Check if user has saved cookies
            if (Guid.TryParse(appUserId, out var userId))
            {
                var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
                if (File.Exists(cookiePath))
                {
                    return $"--cookies \"{cookiePath}\" ";
                }
            }

            return string.Empty;
        }

        public string GenerateModuleId()
        {
            return _random.Next(1, 10000000).ToString();
        }

        public static bool IsYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // Match various YouTube URL formats:
            // - https://www.youtube.com/watch?v=VIDEO_ID
            // - https://youtu.be/VIDEO_ID
            // - https://m.youtube.com/watch?v=VIDEO_ID
            // - https://youtube.com/watch?v=VIDEO_ID
            var youtubePattern = @"^(https?://)?(www\.|m\.)?(youtube\.com/(watch\?v=|embed/|v/)|youtu\.be/)[\w-]+";
            return Regex.IsMatch(url, youtubePattern, RegexOptions.IgnoreCase);
        }

        public static string RemoveYouTubeListParameter(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            try
            {
                var uri = new Uri(url);

                // Only clean standard YouTube watch URLs; leave others untouched
                if (!uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }

                var queryDict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

                if (!queryDict.ContainsKey("list"))
                {
                    return url;
                }

                // Remove the 'list' parameter
                queryDict.Remove("list");

                // Rebuild query string without 'list'
                var newQuery = string.Join("&",
                    queryDict.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}={WebUtility.UrlEncode(v)}")));

                var builder = new UriBuilder(uri)
                {
                    Query = newQuery
                };

                return builder.Uri.ToString();
            }
            catch
            {
                // If anything goes wrong, fall back to the original URL
                return url;
            }
        }

        /// <summary>
        /// Creates a journal entry with the given modules and saves its content JSON.
        /// Optionally attaches the given tag IDs to the new entry.
        /// </summary>
        public async Task<Guid> CreateEntry(int journalId, Guid? parentEntryId, string title, string url, List<object>? modules, List<int>? tagIds)
        {
            var entry = new JournalEntry
            {
                JournalId = journalId,
                ParentEntryId = parentEntryId,
                Title = title,
                Description = string.Empty,
                Url = url
            };

            var entryId = _entriesRepository.Add(entry);
            _logger.LogInformation("Journal entry created. JournalId={JournalId}, EntryId={EntryId}, Title={Title}", journalId, entryId, title);

            if (tagIds != null && tagIds.Count > 0)
            {
                var existingTagIds = _entryTagsRepository.GetTagIdsByEntry(entryId);
                foreach (var tagId in tagIds.Distinct())
                {
                    if (!existingTagIds.Contains(tagId))
                    {
                        _entryTagsRepository.Add(new JournalEntryTag
                        {
                            TagId = tagId,
                            JournalEntryId = entryId
                        });
                    }
                }
            }

            var entryContent = new
            {
                modules = modules != null && modules.Count > 0 ? modules : null
            };

            var contentJson = JsonSerializer.Serialize(entryContent, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var filePath = $"{entryId:N}.json";
            var saveSuccess = Files.SaveFile(Files.Paths.Journal, filePath, contentJson);

            if (!saveSuccess)
            {
                _logger.LogWarning("Failed to save journal entry content JSON for EntryId={EntryId}", entryId);
            }

            await Task.CompletedTask;
            return entryId;
        }

        /// <summary>
        /// Builds the module list for a YouTube video journal entry
        /// (video-player module + text-editor module with title/description).
        /// Returns the video module id via <paramref name="videoModuleId"/>.
        /// </summary>
        public List<object> BuildYouTubeModules(string title, string description, string videoUrl, out string videoModuleId)
        {
            var modules = new List<object>();
            videoModuleId = GenerateModuleId();

            modules.Add(new VideoPlayer
            {
                Id = videoModuleId,
                Url = videoUrl,
                AutoTryAgain = true,
                Title = title
            });

            // Add a text-editor module with video title and description
            var encodedTitle = WebUtility.HtmlEncode(title ?? string.Empty);

            // Break description into paragraphs by double line breaks, and
            // convert single line breaks within a paragraph into <br/>
            var normalized = (description ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

            // Split on two or more consecutive newlines to get paragraphs
            var rawParagraphs = Regex.Split(normalized, "\n{2,}");

            var paragraphHtml = string.Join(string.Empty,
                rawParagraphs
                    .Select(p => p.Trim('\n', '\r', ' '))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p =>
                    {
                        var lines = p.Split('\n');
                        var encodedLines = lines.Select(line => WebUtility.HtmlEncode(line));
                        var innerHtml = string.Join("<br/>", encodedLines);
                        return $"<p>{innerHtml}</p>";
                    }));

            var html = $"<h4>{encodedTitle}</h4>{paragraphHtml}";

            modules.Add(new
            {
                id = GenerateModuleId(),
                type = "text-editor",
                html,
                manuallyAdded = false
            });

            return modules;
        }

        /// <summary>
        /// Creates a journal entry from a YouTube video URL: fetches metadata via yt-dlp,
        /// builds the video-player/text-editor modules, creates the entry, saves its
        /// content JSON, and attaches the given tag IDs.
        /// Returns null if YouTube metadata could not be retrieved.
        /// </summary>
        public async Task<YouTubeEntryResult?> CreateEntryFromYouTubeVideo(
            string url,
            int journalId,
            Guid? parentEntryId,
            string appUserId,
            Guid workerId,
            List<int>? tagIds,
            Func<string, Task>? sendStatus,
            Func<string, Task>? sendError,
            CancellationToken cancellationToken = default)
        {
            // Remove playlist (list) parameter from YouTube URLs so yt-dlp
            // and the stored entry refer to a single video, not a playlist
            var videoUrl = RemoveYouTubeListParameter(url);

            if (sendStatus != null) await sendStatus("Getting metadata from YouTube...");

            // Get YouTube video title & description using yt-dlp
            var metadata = await GetYouTubeMetadata(videoUrl, appUserId, workerId, sendStatus, sendError, cancellationToken);
            if (metadata.Title == null)
            {
                return null;
            }

            var title = metadata.Title;
            var description = metadata.Description;

            // Truncate title to 128 characters
            if (!string.IsNullOrEmpty(title) && title.Length > 128)
            {
                title = title.Substring(0, 128);
            }

            var modules = BuildYouTubeModules(title, description, videoUrl, out var videoModuleId);

            if (sendStatus != null) await sendStatus("Saving journal entry content...");

            var entryId = await CreateEntry(journalId, parentEntryId, title, url, modules, tagIds);

            return new YouTubeEntryResult
            {
                EntryId = entryId,
                Title = title,
                VideoModuleId = videoModuleId,
                VideoUrl = videoUrl
            };
        }

        /// <summary>
        /// Gets YouTube video metadata (title &amp; description) via yt-dlp.
        /// Retries once with fresh cookies requested from the client's Chrome extension.
        /// Returns (null, null) title on failure after sendError has been notified.
        /// </summary>
        public async Task<(string? Title, string? Description)> GetYouTubeMetadata(
            string url,
            string appUserId,
            Guid workerId,
            Func<string, Task>? sendStatus,
            Func<string, Task>? sendError,
            CancellationToken cancellationToken = default)
        {
            // First attempt with existing cookies
            var result = await TryGetYouTubeMetadata(url, appUserId, cancellationToken);
            if (!string.IsNullOrEmpty(result.Title))
            {
                return result;
            }

            // If failed, try to get fresh cookies from extension and retry
            _logger.LogInformation("First metadata attempt failed, requesting fresh cookies from extension for user {AppUserId}", appUserId);
            if (sendStatus != null) await sendStatus("Refreshing YouTube cookie...");

            var freshCookies = await SignalR.Workers.RequestCookiesFromClientAsync(appUserId, workerId, "youtube.com", cancellationToken);
            if (!string.IsNullOrEmpty(freshCookies))
            {
                // Save fresh cookies to main cookie file
                if (Guid.TryParse(appUserId, out var parsedUserId))
                {
                    var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{parsedUserId}.txt");
                    Directory.CreateDirectory(CookiesFolder);
                    await File.WriteAllTextAsync(cookiePath, freshCookies, cancellationToken);
                    _logger.LogInformation("Received fresh cookies from extension, saved to {Path}", cookiePath);
                }

                // Retry with fresh cookies
                if (sendStatus != null) await sendStatus("Retrying metadata fetch with fresh cookie...");
                result = await TryGetYouTubeMetadata(url, appUserId, cancellationToken);
                if (string.IsNullOrEmpty(result.Title))
                {
                    if (sendError != null) await sendError("Failed to retrieve YouTube metadata. Please ensure the Chrome extension for Collector is installed and try again.");
                    return (null, null);
                }
            }
            else
            {
                _logger.LogWarning("Failed to get fresh cookies from extension for user {AppUserId}", appUserId);
                if (sendError != null) await sendError("Failed to retrieve YouTube metadata. Please ensure the Chrome extension for Collector is installed and try again.");
                return (null, null);
            }

            return result;
        }

        private async Task<(string? Title, string? Description)> TryGetYouTubeMetadata(string url, string appUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use yt-dlp to write a JSON metadata file without downloading the video.
                // We'll create a random temp base path and let yt-dlp append .info.json.
                var tempBaseName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var expectedJsonPath = tempBaseName + ".info.json";
                var cookiesArg = GetCookiesArgument(appUserId);
                var arguments = $"{cookiesArg}--js-runtimes node --skip-download --write-info-json -o \"{tempBaseName}\" \"{url}\"";

                // Log cookie file path for debugging
                if (Guid.TryParse(appUserId, out var userId))
                {
                    var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
                    _logger.LogInformation("TryGetYouTubeMetadata - Cookie file path: {CookiePath}, Exists: {Exists}", cookiePath, File.Exists(cookiePath));
                }

                _logger.LogInformation("TryGetYouTubeMetadata yt-dlp arguments: {Arguments}", arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Ensure PATH is inherited so yt-dlp can find Node.js for n challenge solving
                startInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("Failed to start yt-dlp process when getting metadata for URL {Url}", url);
                        return (string.Empty, string.Empty);
                    }

                    var stderrTask = process.StandardError.ReadToEndAsync();
                    var stdoutTask = process.StandardOutput.ReadToEndAsync();

                    await Task.WhenAll(stderrTask, stdoutTask, process.WaitForExitAsync(cancellationToken));

                    var stderr = await stderrTask;
                    var stdout = await stdoutTask;

                    _logger.LogInformation("TryGetYouTubeMetadata - Exit Code: {ExitCode}", process.ExitCode);
                    _logger.LogInformation("TryGetYouTubeMetadata - STDOUT: {StdOut}", stdout);
                    _logger.LogInformation("TryGetYouTubeMetadata - STDERR: {StdErr}", stderr);

                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("yt-dlp exited with code {Code} for URL {Url}. Error: {Error}", process.ExitCode, url, stderr);
                        return (string.Empty, string.Empty);
                    }

                    if (!File.Exists(expectedJsonPath))
                    {
                        _logger.LogWarning("yt-dlp did not produce expected JSON file for URL {Url}. Expected path: {Path}", url, expectedJsonPath);
                        return (string.Empty, string.Empty);
                    }

                    try
                    {
                        var json = await File.ReadAllTextAsync(expectedJsonPath, Encoding.UTF8, cancellationToken);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            return (string.Empty, string.Empty);
                        }

                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        string title = string.Empty;
                        string description = string.Empty;

                        if (root.TryGetProperty("fulltitle", out var fullTitleProp) && fullTitleProp.ValueKind == JsonValueKind.String)
                        {
                            title = fullTitleProp.GetString() ?? string.Empty;
                        }
                        else if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                        {
                            title = titleProp.GetString() ?? string.Empty;
                        }

                        if (root.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                        {
                            description = descProp.GetString() ?? string.Empty;
                        }

                        return (title, description);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(expectedJsonPath))
                            {
                                File.Delete(expectedJsonPath);
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to delete yt-dlp metadata file {Path}", expectedJsonPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube metadata for URL {Url}", url);
                return (string.Empty, string.Empty);
            }
        }
    }
}
