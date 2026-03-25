using System;
using System.Diagnostics;
using Collector.Common;
using Collector.Common.Entities;
using Collector.Data.Interfaces;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Collector.Web.Server.Workers
{
    public class VideoWorker : IWorker
    {
        private readonly IJournalVideosRepository _videoRepo;
        private readonly ILogger<VideoWorker> _logger;
        private readonly IHubContext<WorkerHub> _hubContext;
        private readonly object _stateLock = new();
        private int _currentProgress = 0;
        private string _currentStatus = "";
        private string _title = "";

        public VideoWorker(IJournalVideosRepository videoRepo, ILogger<VideoWorker> logger, IHubContext<WorkerHub> hubContext)
        {
            _videoRepo = videoRepo;
            _logger = logger;
            _hubContext = hubContext;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public async Task Progress(string appUserId, Guid workerId)
        {
            int progress;
            string status;
            string title;
            lock (_stateLock)
            {
                progress = _currentProgress;
                status = _currentStatus;
                title = _title;
            }

            await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress, status, title }, CancellationToken.None);
        }

        public async Task GenerateHlsForExistingVideo(string appUserId, Guid workerId, string videoPath, CancellationToken cancellationToken)
        {
            try
            {
                lock (_stateLock)
                {
                    _currentProgress = 0;
                    _currentStatus = "Starting HLS generation...";
                }
                await SendWorkerMessage(appUserId, workerId, "HlsGenerationProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);

                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoPath);

                if (!File.Exists(videoFullPath))
                {
                    _logger.LogError("Video file not found for HLS generation: {VideoPath}", videoPath);
                    await SendWorkerMessage(appUserId, workerId, "HlsGenerationError", new { message = "Video file not found" }, CancellationToken.None);
                    return;
                }

                var videoDirectory = Path.GetDirectoryName(videoFullPath);
                var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFullPath);
                var hlsFolder = Path.Combine(videoDirectory, videoFileNameWithoutExt, "hls");

                if (!Directory.Exists(hlsFolder))
                {
                    Directory.CreateDirectory(hlsFolder);
                }

                _logger.LogInformation("Generating HLS playlist for existing video: {VideoPath}", videoPath);

                var success = await Common.Videos.GenerateHlsPlaylist(videoFullPath, hlsFolder, timeoutSeconds: 600, onProgress: (hlsProgress, status) =>
                {
                    // For existing videos, use HLS progress directly (0-100%)
                    lock (_stateLock)
                    {
                        _currentProgress = hlsProgress;
                        _currentStatus = status;
                    }
                    SendWorkerMessage(appUserId, workerId, "HlsGenerationProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None).Wait();
                });

                if (success)
                {
                    var hlsRelativePath = Path.Combine(Path.GetDirectoryName(videoPath), videoFileNameWithoutExt, "hls");
                    _logger.LogInformation("Successfully generated HLS playlist at {HlsPath}", hlsRelativePath);
                    
                    lock (_stateLock)
                    {
                        _currentProgress = 100;
                        _currentStatus = "HLS generation complete!";
                    }
                    await SendWorkerMessage(appUserId, workerId, "HlsGenerationComplete", new { hlsPath = hlsRelativePath.Replace("\\", "/") }, CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning("Failed to generate HLS playlist for {VideoPath}", videoPath);
                    await SendWorkerMessage(appUserId, workerId, "HlsGenerationError", new { message = "Failed to generate HLS playlist" }, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during HLS generation for existing video");
                await SendWorkerMessage(appUserId, workerId, "HlsGenerationError", new { message = ex.Message }, CancellationToken.None);
            }
        }

        public async Task DownloadVideo(string appUserId, Guid workerId, string url, int journalId, string entryId, string moduleId, CancellationToken cancellationToken)
        {
            try
            {
                if (!IsYouTubeUrl(url))
                {
                    _logger.LogWarning("VideoWorker only supports YouTube URLs at this time. Url={Url}", url);
                    return;
                }

                var existingVideo = await _videoRepo.GetByUrl(url);

                int videoId;
                string fileName;
                string relativePath;
                string title;

                if (existingVideo != null)
                {
                    videoId = existingVideo.Id;
                    fileName = existingVideo.Filename;
                    relativePath = Path.Combine(existingVideo.JournalEntryId.ToString(), fileName);
                    title = existingVideo.Title;
                    lock (_stateLock)
                    {
                        _title = title;
                    }

                    if (existingVideo.Downloaded)
                    {
                        _logger.LogInformation("Video {VideoId} already downloaded, skipping download in VideoWorker", videoId);

                        // Get thumbnail path
                        var thumbRelativePath = relativePath.Replace(".mp4", "_thumb.jpg");

                        // Send VideoRecordCreated first so frontend can set up the module
                        await SendWorkerMessage(appUserId, workerId, "VideoRecordCreated", new
                        {
                            id = videoId,
                            videoPath = relativePath.Replace("\\", "/"),
                            title = title
                        }, cancellationToken);

                        // Notify client that the video is already downloaded
                        lock (_stateLock)
                        {
                            _currentProgress = 100;
                            _currentStatus = "Already downloaded";
                        }
                        await SendWorkerMessage(appUserId, workerId, "DownloadComplete", new
                        {
                            id = videoId,
                            videoPath = relativePath.Replace("\\", "/"),
                            thumbnailPath = thumbRelativePath.Replace("\\", "/"),
                            width = existingVideo.Width,
                            height = existingVideo.Height,
                            duration = existingVideo.Duration,
                            entryId = existingVideo.JournalEntryId.ToString()
                        }, cancellationToken);
                        await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = 100, status = _currentStatus }, cancellationToken);
                        return;
                    }
                }
                else
                {
                    var originalTitle = await GetVideoTitle(url, appUserId, workerId, cancellationToken);
                    title = originalTitle?.Length > 128 ? originalTitle.Substring(0, 128) : originalTitle ?? string.Empty;
                    _title = title;
                    await SendWorkerMessage(appUserId, workerId, "DownloadTitle", new { title }, cancellationToken);

                    fileName = $"{Guid.NewGuid()}.mp4";
                    relativePath = Path.Combine(entryId, fileName);

                    var video = new JournalVideo
                    {
                        JournalId = journalId,
                        JournalEntryId = Guid.Parse(entryId),
                        ModuleId = moduleId,
                        Filename = fileName,
                        OriginalFilename = string.Empty,
                        Url = url,
                        Downloaded = false,
                        Duration = 0,
                        Width = 0,
                        Height = 0,
                        Metadata = string.Empty,
                        Title = title,
                        Description = string.Empty
                    };

                    videoId = await _videoRepo.Add(video);
                    // Inform client about the new video record
                    await SendWorkerMessage(appUserId, workerId, "VideoRecordCreated", new
                    {
                        id = videoId,
                        videoPath = relativePath.Replace("\\", "/"),
                        title
                    }, cancellationToken);
                }

                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), relativePath);

                var directory = Path.GetDirectoryName(videoFullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (_stateLock)
                {
                    _currentProgress = 0;
                    _currentStatus = "Downloading video...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

                await RunYtDlpDownload(url, videoFullPath, cancellationToken, appUserId, workerId);

                if (!File.Exists(videoFullPath))
                {
                    _logger.LogError("Video file not found after yt-dlp for {Path}", videoFullPath);
                    lock (_stateLock)
                    {
                        _currentProgress = 0;
                        _currentStatus = "Video file not found on server after download.";
                    }
                    await SendWorkerMessage(appUserId, workerId, "DownloadError", new { message = _currentStatus }, cancellationToken);
                    return;
                }

                lock (_stateLock)
                {
                    _currentProgress = 90;
                    _currentStatus = "Generating preview thumbnail...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);

                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
                var thumbnailRelativePath = Path.Combine(entryId, thumbnailFileName);
                var thumbnailSuccess = await GenerateThumbnail(relativePath, thumbnailRelativePath, url, cancellationToken: cancellationToken);

                lock (_stateLock)
                {
                    _currentProgress = 70;
                    _currentStatus = "Processing video metadata...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

                var (width, height, duration) = await GetVideoMetadata(relativePath, cancellationToken);

                decimal fileSizeMb = 0;
                try
                {
                    var fileInfo = new FileInfo(videoFullPath);
                    if (fileInfo.Exists)
                    {
                        fileSizeMb = Math.Round((decimal)fileInfo.Length / (1024 * 1024), 2);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get video file size for {VideoPath}", videoFullPath);
                }

                // Generate HLS playlist for adaptive streaming (70-90%)
                var hlsPath = await GenerateHlsPlaylist(relativePath, cancellationToken, appUserId, workerId);

                lock (_stateLock)
                {
                    _currentProgress = 90;
                    _currentStatus = "Generating seek preview thumbnails...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

                await GenerateSeekPreviewThumbnails(relativePath, cancellationToken, appUserId, workerId);

                await _videoRepo.UpdateDownloaded(videoId, true, fileName, duration, width, height, fileSizeMb);
                _logger.LogInformation("Video {VideoId} download completed in VideoWorker", videoId);
                await SendWorkerMessage(appUserId, workerId, "DownloadComplete", new
                {
                    id = videoId,
                    videoPath = relativePath.Replace("\\", "/"),
                    thumbnailPath = thumbnailSuccess ? thumbnailRelativePath.Replace("\\", "/") : null,
                    hlsPath = hlsPath?.Replace("\\", "/"),
                    width,
                    height,
                    duration,
                    entryId
                }, cancellationToken);
                lock (_stateLock)
                {
                    _currentProgress = 100;
                    _currentStatus = "Complete!";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("VideoWorker download cancelled for URL {Url}", url);
                lock (_stateLock)
                {
                    _currentProgress = 0;
                    _currentStatus = "Download cancelled.";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadError", new { message = _currentStatus }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VideoWorker DownloadVideo error for URL {Url}", url);
                lock (_stateLock)
                {
                    _currentProgress = 0;
                    _currentStatus = ex.Message;
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadError", new { message = _currentStatus }, CancellationToken.None);
            }
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            // Broadcast to all clients; WorkerHubProvider will filter by AppUserId and WorkerId
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }

        private bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") || url.Contains("youtu.be");
        }

        private static readonly string CookiesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "cookies");

        /// <summary>
        /// Gets cookies for yt-dlp, returning both the argument string and whether we used an existing cookie
        /// </summary>
        private async Task<(string cookiesArg, bool usedExistingCookie)> GetCookiesArgumentAsync(string appUserId, Guid workerId, CancellationToken cancellationToken, bool forceRefresh = false)
        {
            var cookiePath = Guid.TryParse(appUserId, out var userId)
                ? Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt")
                : null;

            // Check if we have existing cookies (unless forcing refresh)
            if (!forceRefresh && cookiePath != null && File.Exists(cookiePath))
            {
                _logger.LogInformation("Using saved YouTube cookies for user {AppUserId}", appUserId);
                return ($"--cookies \"{cookiePath}\" ", true);
            }

            // Try to get cookies from client's Chrome extension
            _logger.LogInformation("Requesting YouTube cookies from client for user {AppUserId}", appUserId);
            var cookieData = await SignalR.Workers.RequestCookiesFromClientAsync(appUserId, workerId, "youtube.com", cancellationToken);

            if (!string.IsNullOrEmpty(cookieData) && cookiePath != null)
            {
                // Save cookies to main cookie file
                Directory.CreateDirectory(CookiesFolder);
                await File.WriteAllTextAsync(cookiePath, cookieData, cancellationToken);
                _logger.LogInformation("Received cookies from client, saved to {Path}", cookiePath);
                return ($"--cookies \"{cookiePath}\" ", false);
            }

            // No cookies available
            _logger.LogWarning("No cookies received from client");
            return (string.Empty, false);
        }

        private async Task<string> GetVideoTitle(string url, string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            try
            {
                var (cookiesArg, _) = await GetCookiesArgumentAsync(appUserId, workerId, cancellationToken);
                var arguments = $"{cookiesArg}--js-runtimes node --get-title \"{url}\"";
                _logger.LogInformation("GetVideoTitle yt-dlp arguments: {Arguments}", arguments);
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
                    if (process == null) return "Downloaded Video";

                    var title = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);
                    return title.Trim();
                }
            }
            catch
            {
                return "Downloaded Video";
            }
        }

        private async Task RunYtDlpDownload(string url, string videoFullPath, CancellationToken cancellationToken, string appUserId, Guid workerId)
        {
            // Check if final video already exists (fully processed)
            if (File.Exists(videoFullPath))
            {
                var fileInfo = new FileInfo(videoFullPath);
                if (fileInfo.Length > 0)
                {
                    _logger.LogInformation("Final video file already exists, skipping download: {Path}", videoFullPath);
                    lock (_stateLock)
                    {
                        _currentProgress = 70;
                        _currentStatus = "Video already processed...";
                    }
                    await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                    return;
                }
            }

            var basePath = Path.GetDirectoryName(videoFullPath)!;
            var baseFileName = Path.GetFileNameWithoutExtension(videoFullPath);
            var videoTempPath = Path.Combine(basePath, $"{baseFileName}_video.mp4");
            var audioTempPath = Path.Combine(basePath, $"{baseFileName}_audio.m4a");

            // Check if video stream already downloaded
            var videoExists = await IsFileFullyDownloaded(videoTempPath);
            var audioExists = await IsFileFullyDownloaded(audioTempPath);

            // Download video stream (0-35%) if not already downloaded
            // Use flexible format: best video, prefer mp4 but accept any
            if (!videoExists)
            {
                lock (_stateLock)
                {
                    _currentStatus = "Downloading video stream...";
                }
                await DownloadStream(url, videoTempPath, "bestvideo*[ext=mp4]/bestvideo*/bestvideo", 0, 35, cancellationToken, appUserId, workerId);
            }
            else
            {
                _logger.LogInformation("Video stream already downloaded, skipping: {Path}", videoTempPath);
                lock (_stateLock)
                {
                    _currentProgress = 35;
                    _currentStatus = "Video stream already downloaded...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
            }

            // Download audio stream (35-60%) if not already downloaded
            // Use flexible format: best audio, prefer m4a but accept any
            if (!audioExists)
            {
                lock (_stateLock)
                {
                    _currentProgress = 35;
                    _currentStatus = "Downloading audio stream...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                await DownloadStream(url, audioTempPath, "bestaudio*[ext=m4a]/bestaudio*/bestaudio", 35, 60, cancellationToken, appUserId, workerId);
            }
            else
            {
                _logger.LogInformation("Audio stream already downloaded, skipping: {Path}", audioTempPath);
                lock (_stateLock)
                {
                    _currentProgress = 60;
                    _currentStatus = "Audio stream already downloaded...";
                }
                await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
            }

            // Check if we have both files
            if (File.Exists(videoTempPath) && File.Exists(audioTempPath))
            {
                // Merge with ffmpeg (60-70%)
                await MergeWithFfmpeg(videoTempPath, audioTempPath, videoFullPath, cancellationToken, appUserId, workerId);

                // Clean up temp files
                try
                {
                    File.Delete(videoTempPath);
                    File.Delete(audioTempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temp files after merge");
                }
            }
            else if (File.Exists(videoTempPath))
            {
                // Video only (might already have audio), just rename
                File.Move(videoTempPath, videoFullPath, overwrite: true);
            }
            else
            {
                throw new InvalidOperationException("Failed to download video file");
            }
        }

        private async Task<bool> IsFileFullyDownloaded(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
                return false;

            // Check if there's a .part file (yt-dlp creates these for incomplete downloads)
            if (File.Exists(filePath + ".part"))
                return false;

            // Verify the file is a valid media file using ffprobe
            try
            {
                var duration = await GetVideoDurationSeconds(filePath);
                return duration > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task DownloadStream(string url, string outputPath, string format, int progressStart, int progressEnd, CancellationToken cancellationToken, string appUserId, Guid workerId, bool isRetry = false, bool triedFreshCookie = false)
        {
            // Get cookies - always use async version to ensure we request from extension if needed
            // On retry with fresh cookie, force refresh from extension
            var (cookiesArg, usedExistingCookie) = await GetCookiesArgumentAsync(appUserId, workerId, cancellationToken, forceRefresh: isRetry && !triedFreshCookie);

            // Update status to show downloading (especially after cookie refresh)
            var streamType = format.Contains("video") ? "video" : "audio";
            lock (_stateLock)
            {
                _currentStatus = $"Downloading {streamType} stream...";
            }
            await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);

            var startInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{cookiesArg}--js-runtimes node -f \"{format}\" -o \"{outputPath}\" \"{url}\"",
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
                    throw new InvalidOperationException("Failed to start yt-dlp process");
                }

                var progressRange = progressEnd - progressStart;
                var lastSentProgress = -1;

                process.OutputDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Console.WriteLine(e.Data);
                    try
                    {
                        if (e.Data.Contains("%"))
                        {
                            var percentIndex = e.Data.IndexOf("%", StringComparison.Ordinal);
                            if (percentIndex > 0)
                            {
                                var percentStart = percentIndex - 1;
                                while (percentStart > 0 && (char.IsDigit(e.Data[percentStart - 1]) || e.Data[percentStart - 1] == '.'))
                                {
                                    percentStart--;
                                }
                                if (double.TryParse(e.Data.Substring(percentStart, percentIndex - percentStart), out double progress))
                                {
                                    var mappedProgress = progressStart + (int)Math.Round((progressRange / 100.0) * progress);
                                    if (mappedProgress != lastSentProgress)
                                    {
                                        lastSentProgress = mappedProgress;
                                        lock (_stateLock)
                                        {
                                            _currentProgress = mappedProgress;
                                        }
                                        await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing yt-dlp progress output: {Line}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"yt-dlp error: {e.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    DeletePartFiles(outputPath);

                    // If we used an existing cookie and haven't tried a fresh one yet, get fresh cookie from extension
                    if (usedExistingCookie && !triedFreshCookie)
                    {
                        _logger.LogWarning("yt-dlp failed with code {ExitCode} using saved cookie, trying fresh cookie from extension", process.ExitCode);
                        lock (_stateLock)
                        {
                            _currentStatus = "Refreshing YouTube cookie...";
                        }
                        await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                        await DownloadStream(url, outputPath, format, progressStart, progressEnd, cancellationToken, appUserId, workerId, isRetry: true, triedFreshCookie: false);
                        return;
                    }

                    // If this is first failure without existing cookie, try getting fresh cookie
                    if (!isRetry && !usedExistingCookie)
                    {
                        _logger.LogWarning("yt-dlp failed with code {ExitCode}, trying with fresh cookie from extension", process.ExitCode);
                        lock (_stateLock)
                        {
                            _currentStatus = "Getting YouTube cookie...";
                        }
                        await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                        await DownloadStream(url, outputPath, format, progressStart, progressEnd, cancellationToken, appUserId, workerId, isRetry: true, triedFreshCookie: true);
                        return;
                    }

                    // Already tried fresh cookie, fail with helpful message
                    throw new InvalidOperationException("Failed to get a valid YouTube cookie. Log into YouTube and try again.");
                }
            }
        }

        private void DeletePartFiles(string outputPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                var fileName = Path.GetFileName(outputPath);

                if (string.IsNullOrEmpty(directory)) return;

                // Delete the exact .part file for this output
                var partFile = outputPath + ".part";
                if (File.Exists(partFile))
                {
                    File.Delete(partFile);
                    _logger.LogInformation("Deleted part file: {PartFile}", partFile);
                }

                // Also check for any .part files with similar names (yt-dlp sometimes adds format info)
                var baseFileName = Path.GetFileNameWithoutExtension(outputPath);
                var partFiles = Directory.GetFiles(directory, $"{baseFileName}*.part");
                foreach (var file in partFiles)
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted part file: {PartFile}", file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete .part files for {OutputPath}", outputPath);
            }
        }

        private async Task MergeWithFfmpeg(string videoPath, string audioPath, string outputPath, CancellationToken cancellationToken, string appUserId, Guid workerId)
        {
            var duration = await GetVideoDurationSeconds(videoPath);

            lock (_stateLock)
            {
                _currentProgress = 60;
                _currentStatus = "Merging video & audio...";
            }
            await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -b:a 256k -progress pipe:1 -y \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start ffmpeg process");
                }

                var lastSentProgress = -1;

                // ffmpeg outputs progress info to stdout when using -progress pipe:1
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Console.WriteLine($"ffmpeg: {e.Data}");
                    try
                    {
                        // Parse out_time_ms or out_time from ffmpeg progress output
                        if (e.Data.StartsWith("out_time_ms=") && duration > 0)
                        {
                            var timeMs = e.Data.Replace("out_time_ms=", "");
                            if (long.TryParse(timeMs, out long ms))
                            {
                                var currentSeconds = ms / 1000000.0; // out_time_ms is in microseconds
                                var mergeProgress = Math.Min(100, (currentSeconds / duration) * 100);
                                // Merge phase: 60-70%
                                var mappedProgress = 60 + (int)Math.Round(10.0 * mergeProgress);
                                if (mappedProgress != lastSentProgress)
                                {
                                    lastSentProgress = mappedProgress;
                                    lock (_stateLock)
                                    {
                                        _currentProgress = mappedProgress;
                                        _currentStatus = "Merging video & audio...";
                                    }
                                    await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing ffmpeg progress output: {Line}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"ffmpeg stderr: {e.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"ffmpeg merge failed with code {process.ExitCode}");
                }
            }
        }

        private async Task<double> GetVideoDurationSeconds(string videoPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null) return 0;

                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (double.TryParse(output.Trim(), out double duration))
                    {
                        return duration;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get video duration for {Path}", videoPath);
            }
            return 0;
        }

        private async Task<bool> GenerateThumbnail(string videoRelativePath, string thumbnailRelativePath, string videoUrl = null, int width = 0, int height = 0, bool crop = false, int seekSeconds = 1, CancellationToken cancellationToken = default)
        {
            var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
            var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath);

            var success = await Common.Videos.GenerateThumbnail(videoFullPath, thumbnailFullPath, videoUrl, width, height, crop, seekSeconds, timeoutSeconds: 30);

            if (!success)
            {
                _logger.LogError("Failed to generate thumbnail for {VideoPath}", videoRelativePath);
            }
            else if (!crop && !string.IsNullOrEmpty(videoUrl))
            {
                _logger.LogInformation("Successfully generated thumbnail for {VideoPath}", videoRelativePath);
            }

            return success;
        }

        private async Task<(int width, int height, int duration)> GetVideoMetadata(string videoRelativePath, CancellationToken cancellationToken)
        {
            var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
            return await Common.Videos.GetVideoMetadata(videoFullPath);
        }

        private async Task GenerateSeekPreviewThumbnails(string videoRelativePath, CancellationToken cancellationToken, string appUserId, Guid workerId)
        {
            try
            {
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);

                if (!File.Exists(videoFullPath))
                {
                    _logger.LogError("Video file not found for seek preview generation: {VideoPath}", videoFullPath);
                    return;
                }

                var (width, height, duration) = await GetVideoMetadata(videoRelativePath, cancellationToken);

                if (duration <= 0)
                {
                    _logger.LogWarning("Invalid video duration for seek preview generation");
                    return;
                }

                var videoDirectory = Path.GetDirectoryName(videoFullPath);
                var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFullPath);
                var previewFolder = Path.Combine(videoDirectory, videoFileNameWithoutExt);

                if (!Directory.Exists(previewFolder))
                {
                    Directory.CreateDirectory(previewFolder);
                }

                // Calculate total thumbnails for progress tracking (90-100%)
                var totalThumbnails = (int)Math.Ceiling(duration / 10.0);
                var thumbnailIndex = 0;

                for (int second = 0; second < duration; second += 10)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var previewFileName = $"preview_{second}.jpg";
                    var previewRelativePath = Path.Combine(Path.GetDirectoryName(videoRelativePath), videoFileNameWithoutExt, previewFileName);

                    try
                    {
                        var previewDirectory = Path.GetDirectoryName(Path.Combine(Files.GetPath(Files.Paths.Videos), previewRelativePath));
                        if (!string.IsNullOrEmpty(previewDirectory) && !Directory.Exists(previewDirectory))
                        {
                            Directory.CreateDirectory(previewDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create directory for seek preview thumbnail at {Second}s", second);
                        continue;
                    }

                    await GenerateThumbnail(videoRelativePath, previewRelativePath, null, 160, 90, true, second, cancellationToken);

                    // Update progress (90-100%)
                    thumbnailIndex++;
                    var thumbnailProgress = (double)thumbnailIndex / totalThumbnails;
                    var mappedProgress = 90 + (int)Math.Round(10.0 * thumbnailProgress);
                    lock (_stateLock)
                    {
                        _currentProgress = mappedProgress;
                    }
                    await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during seek preview thumbnail generation in VideoWorker");
            }
        }

        private async Task<string> GenerateHlsPlaylist(string videoRelativePath, CancellationToken cancellationToken, string appUserId, Guid workerId)
        {
            try
            {
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);

                if (!File.Exists(videoFullPath))
                {
                    _logger.LogError("Video file not found for HLS generation: {VideoPath}", videoFullPath);
                    return null;
                }

                var videoDirectory = Path.GetDirectoryName(videoFullPath);
                var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFullPath);
                var hlsFolder = Path.Combine(videoDirectory, videoFileNameWithoutExt, "hls");

                if (!Directory.Exists(hlsFolder))
                {
                    Directory.CreateDirectory(hlsFolder);
                }

                _logger.LogInformation("Generating HLS playlist for {VideoPath}", videoRelativePath);

                var success = await Common.Videos.GenerateHlsPlaylist(videoFullPath, hlsFolder, timeoutSeconds: 600, onProgress: (hlsProgress, status) =>
                {
                    // Map HLS progress (0-100%) to overall download progress (70-90%)
                    var mappedProgress = 70 + (int)(hlsProgress * 0.20);
                    lock (_stateLock)
                    {
                        _currentProgress = mappedProgress;
                        _currentStatus = status;
                    }
                    SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, CancellationToken.None).Wait();
                });

                if (success)
                {
                    var hlsRelativePath = Path.Combine(Path.GetDirectoryName(videoRelativePath), videoFileNameWithoutExt, "hls");
                    _logger.LogInformation("Successfully generated HLS playlist at {HlsPath}", hlsRelativePath);
                    return hlsRelativePath;
                }
                else
                {
                    _logger.LogWarning("Failed to generate HLS playlist for {VideoPath}", videoRelativePath);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during HLS playlist generation in VideoWorker");
                return null;
            }
        }
    }
}