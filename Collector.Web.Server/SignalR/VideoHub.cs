using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Common.Entities;
using Collector.Data.Interfaces;

namespace Collector.Web.Server.SignalR
{
    public class VideoHub : Hub
    {
        private readonly IJournalVideosRepository _videoRepo;
        private readonly ILogger<VideoHub> _logger;

        public VideoHub(IJournalVideosRepository videoRepo, ILogger<VideoHub> logger)
        {
            _videoRepo = videoRepo;
            _logger = logger;
            _logger.LogInformation("VideoHub instance created");
        }

        public async Task DownloadVideo(string url, int journalId, string entryId, string moduleId)
        {
            try
            {
                Console.WriteLine($"[VideoHub] DownloadVideo called: url={url}, journalId={journalId}, entryId={entryId}, moduleId={moduleId}");
                
                await Clients.Caller.SendAsync("DownloadProgress", 0, "Starting download...");

                // Detect if it's a YouTube URL
                if (!IsYouTubeUrl(url))
                {
                    Console.WriteLine("[VideoHub] URL is not a YouTube URL, aborting download.");
                    await Clients.Caller.SendAsync("DownloadError", "Only YouTube URLs are supported at this time");
                    return;
                }

                // Check if video already exists by URL
                var existingVideo = await _videoRepo.GetByUrl(url);
                Console.WriteLine(existingVideo != null
                    ? $"[VideoHub] Existing JournalVideo found for URL. Id={existingVideo.Id}, Downloaded={existingVideo.Downloaded}, Filename={existingVideo.Filename}"
                    : "[VideoHub] No existing JournalVideo found for URL. A new record will be created.");
                
                int videoId;
                string fileName;
                string relativePath;
                string title;

                if (existingVideo != null)
                {
                    _logger.LogInformation("Video already exists with ID: {VideoId}, Downloaded: {Downloaded}", existingVideo.Id, existingVideo.Downloaded);
                    Console.WriteLine($"[VideoHub] Reusing existing JournalVideo. Id={existingVideo.Id}, Downloaded={existingVideo.Downloaded}");
                    
                    videoId = existingVideo.Id;
                    fileName = existingVideo.Filename;
                    relativePath = Path.Combine(existingVideo.JournalEntryId.ToString(), fileName);
                    title = existingVideo.Title;

                    // Send video ID and path back to client immediately
                    await Clients.Caller.SendAsync("VideoRecordCreated", new 
                    { 
                        id = videoId,
                        videoPath = relativePath.Replace("\\", "/"),
                        title
                    });
                    Thread.Sleep(250); //allow React state to update (if neccessary)

                    // If already downloaded, send completion immediately
                    if (existingVideo.Downloaded)
                    {
                        Console.WriteLine("[VideoHub] Existing video already downloaded. Verifying thumbnail and returning to client.");
                        var thumbnailFileName2 = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
                        var thumbnailRelativePath2 = Path.Combine(existingVideo.JournalEntryId.ToString(), thumbnailFileName2);
                        var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath2);
                        
                        // Check if thumbnail exists, if not create it
                        string thumbnailPath = null;
                        if (File.Exists(thumbnailFullPath))
                        {
                            thumbnailPath = thumbnailRelativePath2.Replace("\\", "/");
                        }
                        else
                        {
                            // Generate thumbnail if it doesn't exist
                            var originalRelativePath = Path.Combine(existingVideo.JournalEntryId.ToString(), existingVideo.Filename);
                            var thumbnailSuccess2 = await GenerateThumbnail(originalRelativePath, thumbnailRelativePath2, existingVideo.Url);
                            if (thumbnailSuccess2)
                            {
                                thumbnailPath = thumbnailRelativePath2.Replace("\\", "/");
                            }
                        }
                        await Clients.Caller.SendAsync("DownloadProgress", 100, "Already downloaded");
                        await Clients.Caller.SendAsync("DownloadComplete", new
                        {
                            id = videoId,
                            videoPath = relativePath.Replace("\\", "/"),
                            thumbnailPath = thumbnailPath,
                            width = existingVideo.Width,
                            height = existingVideo.Height,
                            duration = existingVideo.Duration,
                            entryId = existingVideo.JournalEntryId.ToString()
                        });
                        
                        _logger.LogInformation("Video {VideoId} already downloaded, skipping download", videoId);
                        Console.WriteLine($"[VideoHub] Completed existing video flow for Id={videoId}. Skipping yt-dlp download.");
                        return;
                    }
                }
                else
                {
                    // Get original video title from yt-dlp
                    Console.WriteLine("[VideoHub] Getting original video title from yt-dlp...");
                    var originalTitle = await GetVideoTitle(url);
                    
                    // Truncate title to max 128 characters
                    title = originalTitle?.Length > 128 ? originalTitle.Substring(0, 128) : originalTitle ?? "";
                    Console.WriteLine($"[VideoHub] Original title from yt-dlp='{originalTitle}'. Truncated/normalized title='{title}'.");

                    // Generate unique filename
                    fileName = $"{Guid.NewGuid()}.mp4";
                    relativePath = Path.Combine(entryId, fileName);
                    Console.WriteLine($"[VideoHub] Generated new filename '{fileName}' and relativePath '{relativePath}'.");

                    // Save video record to database before downloading
                    var video = new JournalVideo
                    {
                        JournalId = journalId,
                        JournalEntryId = Guid.Parse(entryId),
                        ModuleId = moduleId,
                        Filename = fileName,
                        OriginalFilename = "",
                        Url = url,
                        Downloaded = false,
                        Duration = 0,
                        Width = 0,
                        Height = 0,
                        Metadata = "",
                        Title = title,
                        Description = ""
                    };

                    videoId = await _videoRepo.Add(video);
                    _logger.LogInformation("Video record created with ID: {VideoId}", videoId);
                    Console.WriteLine($"[VideoHub] Created new JournalVideo record. Id={videoId}, JournalId={journalId}, EntryId={entryId}, ModuleId={moduleId}, Url={url}");

                    // Send video ID and path back to client immediately so it can be saved
                    await Clients.Caller.SendAsync("VideoRecordCreated", new 
                    { 
                        id = videoId,
                        videoPath = relativePath.Replace("\\", "/"),
                        title
                    });
                }

                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), relativePath);
                Console.WriteLine($"[VideoHub] Video full path resolved to '{videoFullPath}'.");

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(videoFullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[VideoHub] Created video directory '{directory}'.");
                }

                // Delete any .part files in the directory (leftover from failed downloads)
                //try
                //{
                //    var partFiles = Directory.GetFiles(directory, "*.part");
                //    foreach (var partFile in partFiles)
                //    {
                //        File.Delete(partFile);
                //        _logger.LogInformation("Deleted partial download file: {PartFile}", partFile);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogWarning(ex, "Failed to delete .part files in directory: {Directory}", directory);
                //}

                await Clients.Caller.SendAsync("DownloadProgress", 0, "Downloading: " + title);
                Console.WriteLine($"[VideoHub] Starting yt-dlp download to '{videoFullPath}' for URL='{url}'.");

                // Use yt-dlp to download the video
                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" --merge-output-format mp4 --no-keep-video -o \"{videoFullPath}\" \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        await Clients.Caller.SendAsync("DownloadError", "Failed to start yt-dlp process");
                        return;
                    }

                    // Read output to track progress
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"yt-dlp output: {e.Data}");
                            _logger.LogInformation("yt-dlp: {Output}", e.Data);
                            
                            // Parse yt-dlp progress output
                            if (e.Data.Contains("%"))
                            {
                                var percentIndex = e.Data.IndexOf("%");
                                if (percentIndex > 0)
                                {
                                    var percentStart = percentIndex - 1;
                                    while (percentStart > 0 && (char.IsDigit(e.Data[percentStart - 1]) || e.Data[percentStart - 1] == '.'))
                                    {
                                        percentStart--;
                                    }
                                    if (double.TryParse(e.Data.Substring(percentStart, percentIndex - percentStart), out double progress))
                                    {
                                        if (progress < 100)
                                        {
                                            _ = Clients.Caller.SendAsync("DownloadProgress", (int)Math.Round((90f / 100f) * progress), "Downloading: " + title);
                                        }
                                        else
                                        {
                                            _ = Clients.Caller.SendAsync("DownloadProgress", 90, "Processing video & audio data for: " + title);
                                        }
                                    }
                                }
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"yt-dlp error: {e.Data}");
                            _logger.LogError("yt-dlp error: {Error}", e.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Console.WriteLine($"[VideoHub] yt-dlp exited with code {process.ExitCode}. Error='{error}'.");
                        await Clients.Caller.SendAsync("DownloadError", $"Failed to download video: {error}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("[VideoHub] yt-dlp reported successful exit code (0) on initial download.");
                    }
                }

                // Ensure the video file actually exists on disk before continuing
                if (!File.Exists(videoFullPath))
                {
                    _logger.LogWarning("yt-dlp reported success but video file not found at {Path}. Retrying once...", videoFullPath);
                    Console.WriteLine($"[VideoHub] WARNING: video file missing after initial yt-dlp. Path='{videoFullPath}'. Retrying once...");

                    // Retry the download once
                    var retryStartInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" --merge-output-format mp4 --no-keep-video -o \"{videoFullPath}\" \"{url}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var retryProcess = Process.Start(retryStartInfo))
                    {
                        if (retryProcess == null)
                        {
                            Console.WriteLine("[VideoHub] Failed to start yt-dlp retry process.");
                            await Clients.Caller.SendAsync("DownloadError", "Failed to start yt-dlp process on retry");
                            return;
                        }

                        retryProcess.BeginOutputReadLine();
                        retryProcess.BeginErrorReadLine();
                        await retryProcess.WaitForExitAsync();

                        if (retryProcess.ExitCode != 0)
                        {
                            var retryError = await retryProcess.StandardError.ReadToEndAsync();
                            Console.WriteLine($"[VideoHub] yt-dlp retry exited with code {retryProcess.ExitCode}. Error='{retryError}'.");
                            await Clients.Caller.SendAsync("DownloadError", $"Failed to download video on retry: {retryError}");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("[VideoHub] yt-dlp retry reported successful exit code (0).");
                        }
                    }

                    // Check again after retry
                    if (!File.Exists(videoFullPath))
                    {
                        _logger.LogError("yt-dlp reported success but video file still not found at {Path} after retry", videoFullPath);
                        Console.WriteLine($"[VideoHub] ERROR: video file still missing after retry. Path='{videoFullPath}'.");
                        await Clients.Caller.SendAsync("DownloadError", "Failed to download video: file not found on server after retry.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"[VideoHub] Video file found after retry at '{videoFullPath}'.");
                    }
                }

                await Clients.Caller.SendAsync("DownloadProgress", 92, "Generating preview thumbnail...");
                Console.WriteLine("[VideoHub] Generating main thumbnail...");

                // Generate main thumbnail
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
                var thumbnailRelativePath = Path.Combine(entryId, thumbnailFileName);
                var thumbnailSuccess = await GenerateThumbnail(relativePath, thumbnailRelativePath, url);
                Console.WriteLine($"[VideoHub] Main thumbnail generation result: success={thumbnailSuccess}, thumbnailRelativePath='{thumbnailRelativePath}'.");

                await Clients.Caller.SendAsync("DownloadProgress", 96, "Processing video metadata...");
                Console.WriteLine("[VideoHub] Getting video metadata with ffprobe wrapper...");

                // Get video metadata
                var (width, height, duration) = await GetVideoMetadata(relativePath);
                Console.WriteLine($"[VideoHub] Video metadata: width={width}, height={height}, duration={duration} seconds.");

                // Send completion with video data
                await Clients.Caller.SendAsync("DownloadComplete", new
                {
                    id = videoId,
                    videoPath = relativePath.Replace("\\", "/"),
                    thumbnailPath = thumbnailSuccess ? thumbnailRelativePath.Replace("\\", "/") : null,
                    width,
                    height,
                    duration
                });
                Console.WriteLine($"[VideoHub] Sent DownloadComplete to client for videoId={videoId}.");

                // Generate seek preview thumbnails
                await Clients.Caller.SendAsync("DownloadProgress", 94, "Generating seek preview thumbnails...");
                Console.WriteLine("[VideoHub] Generating seek preview thumbnails...");
                await GenerateSeekPreviewThumbnails(relativePath);

                // Update database with download completion
                await _videoRepo.UpdateDownloaded(videoId, true, fileName, duration, width, height);
                _logger.LogInformation("Video {VideoId} download completed", videoId);
                Console.WriteLine($"[VideoHub] Updated JournalVideo.Downloaded=true for Id={videoId}.");

                await Clients.Caller.SendAsync("DownloadProgress", 100, "Complete!");
                Console.WriteLine($"[VideoHub] DownloadVideo flow completed successfully for videoId={videoId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoHub] DownloadVideo error: {ex.Message}");
                Console.WriteLine($"[VideoHub] Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("DownloadError", $"Error: {ex.Message}");
            }
        }

        private bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") || url.Contains("youtu.be");
        }

        private async Task<string> GetVideoTitle(string url)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--get-title \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    var title = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    return title.Trim();
                }
            }
            catch
            {
                return "Downloaded Video";
            }
        }

        private async Task<bool> GenerateThumbnail(string videoRelativePath, string thumbnailRelativePath, string videoUrl = null, int width = 0, int height = 0, bool crop = false, int seekSeconds = 1)
        {
            var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
            var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath);

            var success = await Videos.GenerateThumbnail(videoFullPath, thumbnailFullPath, videoUrl, width, height, crop, seekSeconds, timeoutSeconds: 30);
            
            if (!success)
            {
                _logger.LogError("Failed to generate thumbnail for {VideoPath}", videoRelativePath);
            }
            else if (!crop && !string.IsNullOrEmpty(videoUrl))
            {
                // Log if we had to use fallback
                _logger.LogInformation("Successfully generated thumbnail for {VideoPath}", videoRelativePath);
            }
            
            return success;
        }



        private async Task<(int width, int height, int duration)> GetVideoMetadata(string videoRelativePath)
        {
            var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
            return await Common.Videos.GetVideoMetadata(videoFullPath);
        }

        private async Task ResizeThumbnailWithFfmpeg(string thumbnailFullPath, int width, int height)
        {
            try
            {
                var tempPath = thumbnailFullPath + ".tmp.jpg";
                var filterArgs = Common.Videos.BuildFfmpegFilterArgs(width, height, false);
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{thumbnailFullPath}\"{filterArgs} -q:v 10 \"{tempPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0 && File.Exists(tempPath))
                    {
                        File.Delete(thumbnailFullPath);
                        File.Move(tempPath, thumbnailFullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resize thumbnail");
            }
        }

        private async Task GenerateSeekPreviewThumbnails(string videoRelativePath)
        {
            try
            {
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
                
                if (!File.Exists(videoFullPath))
                {
                    _logger.LogError("Video file not found for seek preview generation: {VideoPath}", videoFullPath);
                    return;
                }

                // Get video duration
                var (width, height, duration) = await GetVideoMetadata(videoRelativePath);
                
                if (duration <= 0)
                {
                    _logger.LogWarning("Invalid video duration for seek preview generation");
                    return;
                }

                // Create preview thumbnails folder
                var videoDirectory = Path.GetDirectoryName(videoFullPath);
                var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFullPath);
                var previewFolder = Path.Combine(videoDirectory, videoFileNameWithoutExt);
                
                if (!Directory.Exists(previewFolder))
                {
                    Directory.CreateDirectory(previewFolder);
                }

                // Generate thumbnails every 10 seconds
                int successCount = 0;
                int failCount = 0;
                
                for (int second = 0; second < duration; second += 10)
                {
                    var previewFileName = $"preview_{second}.jpg";
                    var previewRelativePath = Path.Combine(Path.GetDirectoryName(videoRelativePath), videoFileNameWithoutExt, previewFileName);

                    // Ensure the full directory path for the preview thumbnail exists
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
                        failCount++;
                        continue;
                    }

                    // Use the same thumbnail generation method, but with crop enabled (no yt-dlp fallback for seek previews)
                    var success = await GenerateThumbnail(videoRelativePath, previewRelativePath, null, 160, 90, true, second);
                    
                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning("Failed to generate seek preview at {Second}s", second);
                    }
                }

                _logger.LogInformation("Generated {SuccessCount} seek preview thumbnails for video ({FailCount} failed)", successCount, failCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during seek preview thumbnail generation");
            }
        }
    }
}
