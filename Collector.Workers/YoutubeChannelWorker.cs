using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Data.Interfaces;
using Collector.Workers.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Collector.Workers
{
    /// <summary>
    /// Worker that imports an entire YouTube channel:
    /// 1. Creates a "channel" journal entry containing an entries-list module filtered by the selected tags
    /// 2. Creates one journal entry per video in the channel (tagged with the selected tags)
    /// 3. Starts VideoWorker downloads for each created entry, max 3 at a time
    /// </summary>
    public class YoutubeChannelWorker : IWorker
    {
        private const int MaxConcurrentDownloads = 3;

        private readonly ILogger<YoutubeChannelWorker> _logger;
        private readonly IHubContext<WorkerHub> _hubContext;
        private readonly IJournalEntriesRepository _entriesRepository;
        private readonly IJournalVideosRepository _videoRepo;
        private readonly YouTubeEntryCreator _entryCreator;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly object _stateLock = new();
        private int _currentProgress = 0;
        private string _currentStatus = "";
        private string _title = "YouTube Channel Import";
        private int _totalVideos = 0;
        private int _createdCount = 0;
        private int _downloadedCount = 0;
        private int _inFlight = 0;
        private int _journalId = 0;
        private Guid? _channelEntryId = null;
        private string _appUserId = "";
        private readonly List<WorkerTask> _videoTasks = new();

        public YoutubeChannelWorker(
            ILogger<YoutubeChannelWorker> logger,
            IHubContext<WorkerHub> hubContext,
            IJournalEntriesRepository entriesRepository,
            IJournalVideosRepository videoRepo,
            YouTubeEntryCreator entryCreator,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _hubContext = hubContext;
            _entriesRepository = entriesRepository;
            _videoRepo = videoRepo;
            _entryCreator = entryCreator;
            _scopeFactory = scopeFactory;
        }

        public Task Stop()
        {
            // Stop any video download workers spawned by this worker
            List<WorkerTask> tasks;
            lock (_videoTasks)
            {
                tasks = _videoTasks.ToList();
            }

            foreach (var task in tasks)
            {
                try
                {
                    SignalR.Workers.StopWorker(_appUserId, task.WorkerId);
                }
                catch
                {
                }
            }

            return Task.CompletedTask;
        }

        public async Task Progress(string appUserId, Guid workerId)
        {
            int progress;
            string status;
            string title;
            int journalId;
            string? entryUrl;
            lock (_stateLock)
            {
                progress = _currentProgress;
                status = _currentStatus;
                title = _title;
                journalId = _journalId;
                entryUrl = _channelEntryId.HasValue ? $"/journal/{_journalId}/entry/{_channelEntryId}" : null;
            }

            await SendWorkerMessage(appUserId, workerId, "ChannelProgress", new { progress, status, title, journalId, entryUrl, entryId = _channelEntryId, total = _totalVideos, created = _createdCount, downloaded = _downloadedCount, inFlight = _inFlight }, CancellationToken.None);
        }

        public async Task ImportChannel(string appUserId, Guid workerId, string channelUrl, int journalId, int[] tagIds, Guid? parentEntryId, CancellationToken cancellationToken)
        {
            _appUserId = appUserId;
            _journalId = journalId;
            var tags = (tagIds ?? Array.Empty<int>()).Distinct().ToList();

            try
            {
                await SendProgress(appUserId, workerId, 0, "Fetching channel information...");

                var (channelName, videoUrls, bannerUrl) = await GetChannelVideos(channelUrl, appUserId, cancellationToken);

                if (videoUrls.Count == 0)
                {
                    lock (_stateLock) { _currentStatus = "No videos found in channel."; }
                    await SendWorkerMessage(appUserId, workerId, "ChannelError", new { message = _currentStatus }, cancellationToken);
                    return;
                }

                lock (_stateLock)
                {
                    _title = channelName;
                }

                // Download the channel banner to use in an image module and as the entry thumbnail
                var bannerModuleId = _entryCreator.GenerateModuleId();
                var banner = await DownloadChannelBanner(bannerUrl, cancellationToken);
                var bannerFileName = banner != null ? $"channel-banner{banner.Value.Extension}" : null;

                // Create the channel journal entry first: it contains an image module
                // with the channel banner above an entries-list module filtered by the
                // selected tags so it lists all imported videos
                var channelModules = new List<object>();
                if (bannerFileName != null)
                {
                    channelModules.Add(new
                    {
                        id = bannerModuleId,
                        type = "image",
                        image = bannerFileName,
                        manuallyAdded = false
                    });
                }
                channelModules.Add(new
                {
                    id = _entryCreator.GenerateModuleId(),
                    type = "entries-list",
                    viewType = "poster",
                    aspectRatio = "5/6",
                    tags,
                    manuallyAdded = false
                });

                // Tags go on the module's filter and each video entry, not on the channel entry itself
                var channelEntryId = await _entryCreator.CreateEntry(journalId, parentEntryId, channelName, channelUrl, channelModules, null);

                // Save the downloaded banner into the entry's image folder and set it as the entry thumbnail
                if (bannerFileName != null && banner != null)
                {
                    try
                    {
                        Files.SaveFileBytes(Files.Paths.Images, $"journal-entries/{channelEntryId}/{bannerFileName}", banner.Value.Bytes);
                        _entriesRepository.UpdateThumbnail(channelEntryId, $"{channelEntryId}/{bannerFileName}", bannerModuleId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set channel entry thumbnail for EntryId={EntryId}", channelEntryId);
                    }
                }
                lock (_stateLock)
                {
                    _channelEntryId = channelEntryId;
                }

                var totalVideos = videoUrls.Count;
                lock (_stateLock)
                {
                    _totalVideos = totalVideos;
                }
                var created = new List<YouTubeEntryResult>();
                var createdCount = 0;
                var downloadedCount = 0;
                var totalUnits = totalVideos * 2; // 1 unit per entry created + 1 per video downloaded

                // Create a journal entry for each video in the channel
                for (var i = 0; i < videoUrls.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await SendProgress(appUserId, workerId,
                        ProgressFor(createdCount + downloadedCount, totalUnits),
                        $"Creating journal entries... {i + 1}/{totalVideos}");

                    try
                    {
                        var result = await _entryCreator.CreateEntryFromYouTubeVideo(
                            videoUrls[i],
                            journalId,
                            channelEntryId,
                            appUserId,
                            workerId,
                            tags,
                            s => SendStatusMessage(appUserId, workerId, s),
                            s => LogErrorMessage(s),
                            cancellationToken);

                        if (result != null)
                        {
                            created.Add(result);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create journal entry for video URL {Url}", videoUrls[i]);
                    }

                    createdCount++;
                    lock (_stateLock)
                    {
                        _createdCount = createdCount;
                    }
                }

                _logger.LogInformation("Channel import created {Created} of {Total} journal entries for channel {ChannelName}", created.Count, totalVideos, channelName);

                // Download all videos for the created entries, max 3 concurrent VideoWorkers
                var running = new List<(WorkerTask Task, YouTubeEntryResult Item)>();

                foreach (var item in created)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    while (running.Count(t => t.Task.Task?.IsCompleted == false) >= MaxConcurrentDownloads)
                    {
                        await Task.WhenAny(running.Where(t => t.Task.Task != null).Select(t => t.Task.Task!));
                        downloadedCount += await FinalizeCompletedDownloads(running);
                        UpdateDownloadCounts(createdCount, downloadedCount, running.Count(t => t.Task.Task?.IsCompleted == false));
                        await SendProgress(appUserId, workerId,
                            ProgressFor(createdCount + downloadedCount, totalUnits),
                            $"Downloading & encoding videos... {downloadedCount}/{created.Count}");
                    }

                    var workerTask = StartVideoWorker(appUserId, item, journalId);
                    running.Add((workerTask, item));
                    lock (_videoTasks)
                    {
                        _videoTasks.Add(workerTask);
                    }
                    UpdateDownloadCounts(createdCount, downloadedCount, running.Count(t => t.Task.Task?.IsCompleted == false));
                    await SendProgress(appUserId, workerId,
                        ProgressFor(createdCount + downloadedCount, totalUnits),
                        $"Downloading & encoding videos... {downloadedCount}/{created.Count}");
                }

                while (running.Any(t => t.Task.Task?.IsCompleted == false))
                {
                    await Task.WhenAny(running.Where(t => t.Task.Task != null && !t.Task.Task.IsCompleted).Select(t => t.Task.Task!));
                    downloadedCount += await FinalizeCompletedDownloads(running);
                    UpdateDownloadCounts(createdCount, downloadedCount, running.Count(t => t.Task.Task?.IsCompleted == false));
                    await SendProgress(appUserId, workerId,
                        ProgressFor(createdCount + downloadedCount, totalUnits),
                        $"Downloading & encoding videos... {downloadedCount}/{created.Count}");
                }

                lock (_stateLock)
                {
                    _currentProgress = 100;
                    _currentStatus = $"Import complete - {created.Count} of {totalVideos} videos imported.";
                    _inFlight = 0;
                }
                await SendWorkerMessage(appUserId, workerId, "ChannelProgress", new { progress = _currentProgress, status = _currentStatus, title = _title, journalId = _journalId, entryUrl = $"/journal/{_journalId}/entry/{_channelEntryId}", entryId = _channelEntryId, total = _totalVideos, created = _createdCount, downloaded = _downloadedCount, inFlight = _inFlight }, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("YoutubeChannelWorker import cancelled for channel {Url}", channelUrl);
                lock (_stateLock)
                {
                    _currentProgress = 0;
                    _currentStatus = "Import cancelled.";
                }
                await SendWorkerMessage(appUserId, workerId, "ChannelError", new { message = _currentStatus }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YoutubeChannelWorker ImportChannel error for URL {Url}", channelUrl);
                lock (_stateLock)
                {
                    _currentProgress = 0;
                    _currentStatus = ex.Message;
                }
                await SendWorkerMessage(appUserId, workerId, "ChannelError", new { message = _currentStatus }, CancellationToken.None);
            }
        }

        private static int ProgressFor(int done, int total)
        {
            if (total <= 0) return 100;
            return Math.Min(100, (int)Math.Round((done / (double)total) * 100));
        }

        private void UpdateDownloadCounts(int created, int downloaded, int inFlight)
        {
            lock (_stateLock)
            {
                _createdCount = created;
                _downloadedCount = downloaded;
                _inFlight = inFlight;
            }
        }

        private async Task SendProgress(string appUserId, Guid workerId, int progress, string status)
        {
            lock (_stateLock)
            {
                _currentProgress = progress;
                _currentStatus = status;
            }
            await SendWorkerMessage(appUserId, workerId, "ChannelProgress", new { progress, status, title = _title, journalId = _journalId, entryUrl = _channelEntryId.HasValue ? $"/journal/{_journalId}/entry/{_channelEntryId}" : null, entryId = _channelEntryId, total = _totalVideos, created = _createdCount, downloaded = _downloadedCount, inFlight = _inFlight }, CancellationToken.None);
        }

        private Task SendStatusMessage(string appUserId, Guid workerId, string status)
        {
            return SendProgress(appUserId, workerId, _currentProgress, status);
        }

        private Task LogErrorMessage(string message)
        {
            _logger.LogWarning("Channel import per-video error: {Message}", message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts a real VideoWorker task for a created journal entry so the download
        /// appears as its own worker/notification. Max concurrency is managed by the caller.
        /// </summary>
        private WorkerTask StartVideoWorker(string appUserId, YouTubeEntryResult item, int journalId)
        {
            var scope = _scopeFactory.CreateScope();
            var videoWorker = scope.ServiceProvider.GetRequiredService<VideoWorker>();
            var task = SignalR.Workers.StartWorker(
                appUserId,
                videoWorker,
                "DownloadVideo",
                new object?[] { item.VideoUrl, journalId, item.EntryId.ToString(), item.VideoModuleId },
                scope);
            SignalR.Workers.SetRoute(appUserId, task.WorkerId, "video-worker");
            SignalR.Workers.SetCustomId(appUserId, task.WorkerId, item.VideoModuleId);
            SignalR.Workers.SetUrl(appUserId, task.WorkerId, $"/journal/{journalId}/entry/{item.EntryId}");
            return task;
        }

        /// <summary>
        /// For each completed video worker task, writes the downloaded video info
        /// back into the entry's module JSON (videoId, videoPath, thumbnail, etc.)
        /// since no client is listening to the worker's DownloadComplete event.
        /// Returns the number of tasks that were finalized.
        /// </summary>
        private async Task<int> FinalizeCompletedDownloads(List<(WorkerTask Task, YouTubeEntryResult Item)> running)
        {
            var finalized = 0;
            for (var i = running.Count - 1; i >= 0; i--)
            {
                var (task, item) = running[i];
                if (task.Task?.IsCompleted != true) continue;

                running.RemoveAt(i);
                finalized++;
                await UpdateEntryModuleWithVideoInfo(item.EntryId, item.VideoModuleId);
            }
            return finalized;
        }

        private async Task UpdateEntryModuleWithVideoInfo(Guid entryId, string moduleId)
        {
            try
            {
                var video = await _videoRepo.GetByModuleId(moduleId);
                if (video == null || !video.Downloaded) return;

                var filePath = $"{entryId:N}.json";
                var json = await Files.GetFileAsync(Files.Paths.Journal, filePath);
                if (string.IsNullOrWhiteSpace(json)) return;

                var root = JsonNode.Parse(json);
                var modules = root?["modules"] as JsonArray;
                if (modules == null) return;

                foreach (var module in modules)
                {
                    if (module?["id"]?.GetValue<string>() != moduleId) continue;

                    module["videoId"] = video.Id;
                    module["videoPath"] = $"{entryId}/{video.Filename}";
                    module["downloaded"] = true;

                    var thumbName = Path.GetFileNameWithoutExtension(video.Filename) + "_thumb.jpg";
                    var thumbFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), entryId.ToString(), thumbName);
                    if (File.Exists(thumbFullPath))
                    {
                        module["thumbnailPath"] = $"{entryId}/{thumbName}";
                    }

                    if (video.Width > 0) module["width"] = video.Width;
                    if (video.Height > 0) module["height"] = video.Height;
                    if (video.Duration > 0) module["duration"] = video.Duration;
                    break;
                }

                Files.SaveFile(Files.Paths.Journal, filePath, root!.ToJsonString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update module {ModuleId} in entry {EntryId} with downloaded video info", moduleId, entryId);
            }
        }

        /// <summary>
        /// Gets the channel name, all video URLs, and the banner URL via yt-dlp flat playlist dump.
        /// </summary>
        private async Task<(string ChannelName, List<string> VideoUrls, string? BannerUrl)> GetChannelVideos(string channelUrl, string appUserId, CancellationToken cancellationToken)
        {
            var cookiesArg = YouTubeEntryCreator.GetCookiesArgument(appUserId);
            var arguments = $"{cookiesArg}--js-runtimes node --flat-playlist --dump-single-json \"{channelUrl}\"";
            _logger.LogInformation("GetChannelVideos yt-dlp arguments: {Arguments}", arguments);

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

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start yt-dlp process for channel URL {Url}", channelUrl);
                return (string.Empty, new List<string>(), null);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(cancellationToken));

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                _logger.LogWarning("yt-dlp exited with code {Code} for channel URL {Url}. Error: {Error}", process.ExitCode, channelUrl, stderr);
                return (string.Empty, new List<string>(), null);
            }

            var videoUrls = new List<string>();
            var channelName = string.Empty;

            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;

            if (root.TryGetProperty("channel", out var channelProp) && channelProp.ValueKind == JsonValueKind.String)
            {
                channelName = channelProp.GetString() ?? string.Empty;
            }
            if (string.IsNullOrEmpty(channelName) && root.TryGetProperty("uploader", out var uploaderProp) && uploaderProp.ValueKind == JsonValueKind.String)
            {
                channelName = uploaderProp.GetString() ?? string.Empty;
            }
            if (string.IsNullOrEmpty(channelName) && root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
            {
                channelName = titleProp.GetString() ?? string.Empty;
            }
            if (string.IsNullOrEmpty(channelName))
            {
                channelName = "YouTube Channel";
            }

            if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in entries.EnumerateArray())
                {
                    string? videoUrl = null;
                    if (item.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                    {
                        var candidate = urlProp.GetString();
                        if (!string.IsNullOrEmpty(candidate) && candidate.StartsWith("http"))
                        {
                            videoUrl = candidate;
                        }
                    }

                    if (videoUrl == null && item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                    {
                        var id = idProp.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            videoUrl = $"https://www.youtube.com/watch?v={id}";
                        }
                    }

                    if (videoUrl != null)
                    {
                        videoUrls.Add(videoUrl);
                    }
                }
            }

            var bannerUrl = ExtractBannerUrl(root);
            if (string.IsNullOrEmpty(bannerUrl))
            {
                bannerUrl = await FetchChannelBannerUrlFromPage(channelUrl, cancellationToken);
            }

            _logger.LogInformation("Found {Count} videos in channel {ChannelName} ({Url})", videoUrls.Count, channelName, channelUrl);
            return (channelName, videoUrls, bannerUrl);
        }

        /// <summary>
        /// Picks the channel banner thumbnail from the yt-dlp playlist dump's thumbnails array.
        /// </summary>
        private static string? ExtractBannerUrl(JsonElement root)
        {
            if (!root.TryGetProperty("thumbnails", out var thumbs) || thumbs.ValueKind != JsonValueKind.Array)
                return null;

            string? fallback = null;
            var fallbackWidth = -1d;
            foreach (var thumb in thumbs.EnumerateArray())
            {
                var url = thumb.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String
                    ? urlProp.GetString()
                    : null;
                if (string.IsNullOrEmpty(url)) continue;

                var id = thumb.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
                    ? idProp.GetString() ?? string.Empty
                    : string.Empty;
                if (id.Contains("banner", StringComparison.OrdinalIgnoreCase) || url.Contains("banner", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }

                var width = thumb.TryGetProperty("width", out var wProp) && wProp.ValueKind == JsonValueKind.Number
                    ? wProp.GetDouble()
                    : 0;
                if (width > fallbackWidth)
                {
                    fallbackWidth = width;
                    fallback = url;
                }
            }
            return fallback;
        }

        /// <summary>
        /// Fallback: scrapes the channel page HTML for the banner thumbnails block.
        /// </summary>
        private async Task<string?> FetchChannelBannerUrlFromPage(string channelUrl, CancellationToken cancellationToken)
        {
            try
            {
                using var http = new HttpClient();
                var html = await http.GetStringAsync(channelUrl, cancellationToken);
                var bannerSection = Regex.Match(html, "\"banner\"\\s*:\\s*\\{\\s*\"thumbnails\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                if (bannerSection.Success)
                {
                    var urls = Regex.Matches(bannerSection.Groups[1].Value, "\"url\"\\s*:\\s*\"(https?://[^\"]+)\"");
                    if (urls.Count > 0)
                    {
                        return urls[^1].Groups[1].Value; // last thumbnail is the largest
                    }
                }
                var ogImage = Regex.Match(html, "<meta[^>]+property=\"og:image\"[^>]+content=\"([^\"]+)\"");
                return ogImage.Success ? ogImage.Groups[1].Value : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch channel page for banner URL {Url}", channelUrl);
                return null;
            }
        }

        /// <summary>
        /// Downloads the channel banner image bytes, returning null on failure.
        /// </summary>
        private async Task<(byte[] Bytes, string Extension)?> DownloadChannelBanner(string? bannerUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(bannerUrl)) return null;

            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(bannerUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length == 0) return null;

                var extension = (response.Content.Headers.ContentType?.MediaType ?? string.Empty) switch
                {
                    "image/webp" => ".webp",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    _ => ".jpg"
                };
                return (bytes, extension);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download channel banner from {Url}", bannerUrl);
                return null;
            }
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            // Broadcast to all clients; WorkerHubProvider will filter by AppUserId and WorkerId
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }
    }
}
