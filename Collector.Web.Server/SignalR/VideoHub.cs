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
                Console.WriteLine($"DownloadVideo called: url={url}, journalId={journalId}, entryId={entryId}, moduleId={moduleId}");
                
                await Clients.Caller.SendAsync("DownloadProgress", 0, "Starting download...");

                // Detect if it's a YouTube URL
                if (!IsYouTubeUrl(url))
                {
                    await Clients.Caller.SendAsync("DownloadError", "Only YouTube URLs are supported at this time");
                    return;
                }

                // Check if video already exists by URL
                var existingVideo = await _videoRepo.GetByUrl(url);
                
                int videoId;
                string fileName;
                string relativePath;
                string title;

                if (existingVideo != null)
                {
                    _logger.LogInformation("Video already exists with ID: {VideoId}, Downloaded: {Downloaded}", existingVideo.Id, existingVideo.Downloaded);
                    
                    videoId = existingVideo.Id;
                    fileName = existingVideo.Filename;
                    relativePath = Path.Combine(entryId, fileName);
                    title = existingVideo.Title;

                    // Send video ID and path back to client immediately
                    await Clients.Caller.SendAsync("VideoRecordCreated", new 
                    { 
                        id = videoId,
                        videoPath = relativePath.Replace("\\", "/"),
                        title,
                        exists = true
                    });

                    // If already downloaded, send completion immediately
                    if (existingVideo.Downloaded)
                    {
                        var thumbnailFileName2 = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
                        var thumbnailRelativePath2 = Path.Combine(entryId, thumbnailFileName2);
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
                            var thumbnailSuccess2 = await GenerateThumbnail(originalRelativePath, thumbnailRelativePath2);
                            if (thumbnailSuccess2)
                            {
                                thumbnailPath = thumbnailRelativePath2.Replace("\\", "/");
                            }
                        }
                        
                        await Clients.Caller.SendAsync("DownloadComplete", new
                        {
                            id = videoId,
                            videoPath = relativePath.Replace("\\", "/"),
                            thumbnailPath = thumbnailPath,
                            width = existingVideo.Width,
                            height = existingVideo.Height,
                            duration = existingVideo.Duration
                        });
                        
                        _logger.LogInformation("Video {VideoId} already downloaded, skipping download", videoId);
                        return;
                    }
                }
                else
                {
                    // Get original video title from yt-dlp
                    var originalTitle = await GetVideoTitle(url);
                    
                    // Truncate title to max 128 characters
                    title = originalTitle?.Length > 128 ? originalTitle.Substring(0, 128) : originalTitle ?? "";

                    // Generate unique filename
                    fileName = $"{Guid.NewGuid()}.mp4";
                    relativePath = Path.Combine(entryId, fileName);

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

                    // Send video ID and path back to client immediately so it can be saved
                    await Clients.Caller.SendAsync("VideoRecordCreated", new 
                    { 
                        id = videoId,
                        videoPath = relativePath.Replace("\\", "/"),
                        title
                    });
                }

                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), relativePath);

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(videoFullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
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
                                            _ = Clients.Caller.SendAsync("DownloadProgress", 100, "Processing video data for: " + title);
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
                        await Clients.Caller.SendAsync("DownloadError", $"Failed to download video: {error}");
                        return;
                    }
                }

                await Clients.Caller.SendAsync("DownloadProgress", 92, "Generating thumbnail...");

                // Generate thumbnail
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
                var thumbnailRelativePath = Path.Combine(entryId, thumbnailFileName);
                var thumbnailSuccess = await GenerateThumbnail(relativePath, thumbnailRelativePath);

                await Clients.Caller.SendAsync("DownloadProgress", 95, "Processing video metadata...");

                // Get video metadata
                var (width, height, duration) = await GetVideoMetadata(relativePath);

                // Update database with download completion
                await _videoRepo.UpdateDownloaded(videoId, true, fileName, duration, width, height);
                _logger.LogInformation("Video {VideoId} download completed", videoId);

                await Clients.Caller.SendAsync("DownloadProgress", 100, "Complete!");

                // Send completion with video data
                await Clients.Caller.SendAsync("DownloadComplete", new
                {
                    id = videoId,
                    videoPath = relativePath.Replace("\\", "/"),
                    thumbnailPath = thumbnailSuccess ? thumbnailRelativePath.Replace("\\", "/") : null,
                    width = width,
                    height = height,
                    duration = duration
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DownloadVideo error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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

        private async Task<bool> GenerateThumbnail(string videoRelativePath, string thumbnailRelativePath)
        {
            try
            {
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
                var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath);

                var directory = Path.GetDirectoryName(thumbnailFullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-ss 00:00:01 -i \"{videoFullPath}\" -vframes 1 -q:v 1 \"{thumbnailFullPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0 && File.Exists(thumbnailFullPath);
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<(int width, int height, int duration)> GetVideoMetadata(string videoRelativePath)
        {
            try
            {
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,duration -of csv=p=0 \"{videoFullPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var parts = output.Trim().Split(',');
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[0], out int width);
                            int.TryParse(parts[1], out int height);
                            double.TryParse(parts.Length > 2 ? parts[2] : "0", out double durationSeconds);
                            return (width, height, (int)durationSeconds);
                        }
                    }
                }
            }
            catch
            {
            }

            return (0, 0, 0);
        }
    }
}
