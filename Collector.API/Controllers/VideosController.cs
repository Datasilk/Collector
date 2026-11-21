using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Collector.API.Models;
using Collector.Common;
using Collector.Common.Entities;
using Collector.Data.Interfaces;

namespace Collector.API.Controllers
{
    public class VideosController : ApiController
    {
        private readonly IJournalVideosRepository _videoRepo;

        public VideosController(IJournalVideosRepository videoRepo)
        {
            _videoRepo = videoRepo;
        }
        [HttpGet("/video/thumb/{*path}")]
        public IActionResult GetThumbnail(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }

            try
            {
                // Get the thumbnail file as bytes (journal root first, legacy root fallback)
                var thumbnailBytes = System.IO.File.ReadAllBytes(Files.ResolveExisting(path));
                
                // Return as JPEG image
                return File(thumbnailBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return NotFound($"Thumbnail not found: {path}");
            }
        }

        [HttpGet("/video/preview/{*path}")]
        public async Task<IActionResult> GetSeekPreview(string path)
        {
            // path shape: "{videoDir}/{second}" where videoDir is the journal-relative
            // videos/{videoId} dir, or the legacy "{entryId}/{fileName}/{second}" form.
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }

            try
            {
                var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2 || !int.TryParse(segments[^1], out var second))
                {
                    return NotFound();
                }
                // Strip an optional trailing video filename: clients send
                // "{videoDir}/{second}" or the full "{videoPath}/{second}".
                var videoExts = new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi", ".wmv", ".flv", ".mkv", ".m4v" };
                var hasFileSegment = videoExts.Any(e => segments[^2].EndsWith(e, StringComparison.OrdinalIgnoreCase));
                var parentPath = string.Join('/', segments[..^1]);
                var videoDirPath = hasFileSegment ? string.Join('/', segments[..^2]) : parentPath;

                // Round down to nearest 10 seconds
                var roundedSecond = (int)Math.Floor(second / 10.0) * 10;
                var previewFileName = $"preview_{roundedSecond}.jpg";

                string previewPath;
                string videoPath;
                if (JournalPaths.IsJournalRelative(videoDirPath))
                {
                    // New layout: {journalId}/{entryId}/videos/{videoId}/previews/…
                    var videoDir = JournalPaths.Canonicalize(videoDirPath);
                    previewPath = $"{videoDir}/previews/{previewFileName}";
                    var videoDirFull = Path.Combine(Files.GetPath(Files.Paths.Journal), videoDir);
                    var rawFile = Directory.Exists(videoDirFull)
                        ? Directory.GetFiles(videoDirFull, "raw.*").FirstOrDefault()
                        : null;
                    if (rawFile == null)
                    {
                        return NotFound("Video file not found");
                    }
                    videoPath = $"{videoDir}/{Path.GetFileName(rawFile)}";
                }
                else
                {
                    // Legacy layout: {entryId}/{fileName}/{second} resolved under the journal root
                    var videoFileName = hasFileSegment ? segments[^2] : null;
                    if (videoFileName == null) return NotFound();
                    var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFileName);
                    previewPath = Path.Combine(videoDirPath, videoFileNameWithoutExt, previewFileName);
                    videoPath = Path.Combine(videoDirPath, videoFileName);
                }

                var previewFullPath = Path.Combine(
                    Files.GetPath(Files.Paths.Journal),
                    JournalPaths.Canonicalize(previewPath));

                // Check if thumbnail already exists
                if (!System.IO.File.Exists(previewFullPath))
                {
                    // Ensure the full directory path exists before generating the thumbnail
                    try
                    {
                        var previewDirectory = Path.GetDirectoryName(previewFullPath);
                        if (!string.IsNullOrEmpty(previewDirectory) && !Directory.Exists(previewDirectory))
                        {
                            Directory.CreateDirectory(previewDirectory);
                        }
                    }
                    catch (Exception dirEx)
                    {
                        return StatusCode(500, $"Error creating preview directory: {dirEx.Message}");
                    }

                    var videoFullPath = Path.Combine(
                        Files.GetPath(Files.Paths.Journal),
                        JournalPaths.Canonicalize(videoPath));

                    if (!System.IO.File.Exists(videoFullPath))
                    {
                        return NotFound("Video file not found");
                    }

                    // Generate the thumbnail on-demand
                    var success = await Common.Videos.GenerateThumbnail(
                        videoFullPath,
                        previewFullPath,
                        videoUrl: null, // No yt-dlp fallback for seek previews
                        width: 160,
                        height: 90,
                        crop: true,
                        seekSeconds: roundedSecond,
                        timeoutSeconds: 10
                    );

                    if (!success)
                    {
                        return NotFound("Failed to generate preview thumbnail");
                    }
                }

                // Get the preview thumbnail file as bytes
                var previewBytes = System.IO.File.ReadAllBytes(previewFullPath);
                
                // Return as JPEG image with cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=31536000"); // Cache for 1 year
                return File(previewBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating preview: {ex.Message}");
            }
        }

        [HttpGet("/video/{id:int}")]
        public async Task<IActionResult> StreamVideo(int id)
        {
            try
            {
                // Get video metadata from database
                var video = await _videoRepo.GetById(id);
                if (video == null)
                {
                    return NotFound("Video not found");
                }

                // Build file path — new journal layout first, legacy videos root fallback
                var videoStem = JournalPaths.VideoIdFromFileName(video.Filename);
                var videoPath = JournalPaths.VideoPath(video.JournalId, video.JournalEntryId, videoStem, Path.GetExtension(video.Filename));
                var videoFullPath = Files.ResolveExisting(videoPath);
                if (!System.IO.File.Exists(videoFullPath))
                {
                    videoPath = Path.Combine(video.JournalEntryId.ToString(), video.Filename);
                    videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Journal), videoPath);
                }
                var changed = false;

                // Ensure FileSizeMb is populated
                if (video.FileSizeMb == 0)
                {
                    try
                    {
                        var fileInfo = new FileInfo(videoFullPath);
                        if (fileInfo.Exists)
                        {
                            video.FileSizeMb = Math.Round((decimal)fileInfo.Length / (1024 * 1024), 2);
                            changed = true;
                        }
                    }
                    catch
                    {
                        // Ignore file size errors here; streaming will still proceed if file exists
                    }
                }

                // Ensure width, height, and duration are populated
                if (video.Width == 0 || video.Height == 0 || video.Duration == 0)
                {
                    var (width, height, duration) = await GetVideoMetadata(videoPath);

                    if (width > 0 && height > 0)
                    {
                        video.Width = width;
                        video.Height = height;
                        changed = true;
                    }

                    if (duration > 0)
                    {
                        video.Duration = duration;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await _videoRepo.Update(video);
                }

                if (!System.IO.File.Exists(videoFullPath))
                {
                    return NotFound("Video file not found");
                }

                // Determine content type
                var ext = Path.GetExtension(video.Filename)?.ToLowerInvariant();
                var contentType = ext switch
                {
                    ".mp4" => "video/mp4",
                    ".webm" => "video/webm",
                    ".ogg" => "video/ogg",
                    ".mov" => "video/quicktime",
                    ".avi" => "video/x-msvideo",
                    ".wmv" => "video/x-ms-wmv",
                    ".flv" => "video/x-flv",
                    ".mkv" => "video/x-matroska",
                    ".m4v" => "video/x-m4v",
                    _ => "application/octet-stream"
                };

                // Use PhysicalFile with EnableRangeProcessing for proper streaming
                return PhysicalFile(videoFullPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return NotFound($"Error streaming video: {ex.Message}");
            }
        }

        [HttpGet("/video/{*path}")]
        public IActionResult GetVideo(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }
            
            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length < 1)
            {
                return NotFound();
            }
            
            try
            {
                // Get the full file path (journal root first, legacy root fallback)
                var videoFullPath = Files.ResolveExisting(path);

                if (!System.IO.File.Exists(videoFullPath))
                {
                    return NotFound("Video file not found");
                }
                
                // Determine the correct content type from the file extension
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                var contentType = ext switch
                {
                    ".mp4" => "video/mp4",
                    ".webm" => "video/webm",
                    ".ogg" => "video/ogg",
                    ".mov" => "video/quicktime",
                    ".avi" => "video/x-msvideo",
                    ".wmv" => "video/x-ms-wmv",
                    ".flv" => "video/x-flv",
                    ".mkv" => "video/x-matroska",
                    ".m4v" => "video/x-m4v",
                    _ => "application/octet-stream"
                };
                
                // Use PhysicalFile with EnableRangeProcessing for proper streaming
                return PhysicalFile(videoFullPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return NotFound($"Video not found: {path}");
            }
        }

        [Authorize]
        [HttpPost("/video-upload/{*path}")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> UploadVideo(string path)
        {
            // Get file from request directly
            var file = Request.Form.Files.FirstOrDefault();
            
            if (string.IsNullOrEmpty(path))
            {
                return Json(new ApiResponse { success = false, message = "Path is required" });
            }

            // Parse path: journalId/entryId/moduleId
            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length != 3)
            {
                return Json(new ApiResponse { success = false, message = "Invalid path format. Expected: journalId/entryId/moduleId" });
            }

            if (!int.TryParse(pathParts[0], out int journalId))
            {
                return Json(new ApiResponse { success = false, message = "Invalid journal ID" });
            }

            string entryId = pathParts[1];
            string moduleId = pathParts[2];

            if (file == null || file.Length == 0)
            {
                return Json(new ApiResponse { success = false, message = "File is required" });
            }

            try
            {
                // Get file extension
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var videoStem = JournalPaths.VideoIdFromFileName(fileName);
                var relativePath = JournalPaths.VideoPath(journalId, entryId, videoStem, fileExtension);
                
                // Save video file by streaming directly to disk (optimized for large files)
                using (var stream = file.OpenReadStream())
                {
                    var success = await Files.SaveFileStreamAsync(Files.Paths.Journal, relativePath, stream);
                    if (!success)
                    {
                        return Json(new ApiResponse { success = false, message = "Failed to save video" });
                    }
                }

                // Generate thumbnail
                var thumbnailRelativePath = JournalPaths.VideoThumbPath(journalId, entryId, videoStem);
                var thumbnailSuccess = await GenerateThumbnail(relativePath, thumbnailRelativePath);

                // Get video metadata using ffprobe
                var (width, height, duration) = await GetVideoMetadata(relativePath);

                // Save video metadata to database
                var video = new JournalVideo
                {
                    JournalId = journalId,
                    JournalEntryId = Guid.Parse(entryId),
                    ModuleId = moduleId,
                    Filename = fileName,
                    OriginalFilename = file.FileName,
                    Duration = duration,
                    Width = width,
                    Height = height,
                    Metadata = "",
                    Title = "",
                    Description = ""
                };

                var videoId = await _videoRepo.Add(video);

                return Json(new ApiResponse 
                { 
                    success = true, 
                    data = new 
                    { 
                        id = videoId,
                        videoPath = relativePath.Replace("\\", "/"),
                        thumbnailPath = thumbnailSuccess ? thumbnailRelativePath.Replace("\\", "/") : null,
                        width = width,
                        height = height,
                        duration = duration
                    } 
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        private async Task<bool> GenerateThumbnail(string videoRelativePath, string thumbnailRelativePath, int? seekPosition = null)
        {
            try
            {
                var videoFullPath = Files.ResolveExisting(videoRelativePath);
                var thumbnailFullPath = Files.ResolveExisting(thumbnailRelativePath);
                
                // Use provided seek position, or calculate random position between 1-21% of video duration
                int seekSeconds;
                if (seekPosition.HasValue)
                {
                    seekSeconds = seekPosition.Value;
                }
                else
                {
                    var (_, _, duration) = await GetVideoMetadata(videoRelativePath);
                    seekSeconds = duration > 0 ? (int)(duration * (0.01 + (new Random().NextDouble() * 0.2))) : 5;
                }
                
                // Use the unified thumbnail generation method (no yt-dlp fallback for uploaded videos)
                return await Common.Videos.GenerateThumbnail(
                    videoFullPath,
                    thumbnailFullPath,
                    videoUrl: null,
                    width: 0,
                    height: 0,
                    crop: false,
                    seekSeconds: seekSeconds,
                    timeoutSeconds: 30
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
                return false;
            }
        }

        private async Task<(int width, int height, int duration)> GetVideoMetadata(string videoRelativePath)
        {
            try
            {
                var videoFullPath = Files.ResolveExisting(videoRelativePath);

                // Use ffprobe to get video metadata
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting video metadata: {ex.Message}");
            }

            return (0, 0, 0);
        }

        [Authorize]
        [HttpGet("/video/generate-thumbnail/{id}")]
        public async Task<IActionResult> GenerateThumbnailForVideo(int id, [FromQuery] int? seekPosition = null)
        {
            try
            {
                var video = await _videoRepo.GetById(id);
                if (video == null)
                {
                    return Json(new ApiResponse { success = false, message = "Video not found" });
                }

                var videoStem = JournalPaths.VideoIdFromFileName(video.Filename);
                var videoPath = JournalPaths.VideoPath(video.JournalId, video.JournalEntryId, videoStem, Path.GetExtension(video.Filename));
                if (!System.IO.File.Exists(Files.ResolveExisting(videoPath)))
                {
                    videoPath = Path.Combine(video.JournalEntryId.ToString(), video.Filename);
                }
                var thumbnailRelativePath = JournalPaths.VideoThumbPath(video.JournalId, video.JournalEntryId, videoStem);

                var success = await GenerateThumbnail(videoPath, thumbnailRelativePath, seekPosition);
                if (!success)
                {
                    return Json(new ApiResponse { success = false, message = "Failed to generate thumbnail" });
                }

                return Json(new ApiResponse
                {
                    success = true,
                    data = new { thumbnailPath = thumbnailRelativePath.Replace("\\", "/") }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("/video-delete")]
        public async Task<IActionResult> DeleteVideo([FromBody] DeleteVideoModel model)
        {
            try
            {
                // Get video metadata to find the files
                var video = await _videoRepo.GetByModuleId(model.ModuleId);
                
                // Delete files from disk if requested
                if (model.DeleteFiles && video != null)
                {
                
                // Delete from database
                    var success = _videoRepo.DeleteByModuleId(model.EntryId, model.ModuleId);
                    var videoStem = JournalPaths.VideoIdFromFileName(video.Filename);

                    // New layout: remove the whole videos/{videoId} directory
                    var videoDirFull = Path.Combine(Files.GetPath(Files.Paths.Journal),
                        JournalPaths.VideoDir(video.JournalId, video.JournalEntryId, videoStem));
                    if (Directory.Exists(videoDirFull))
                    {
                        Directory.Delete(videoDirFull, true);
                    }

                }
                
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("/video/generate-hls/{id}")]
        public async Task<IActionResult> GenerateHlsPlaylist(int id)
        {
            try
            {
                var video = await _videoRepo.GetById(id);
                if (video == null)
                {
                    return Json(new ApiResponse { success = false, message = "Video not found" });
                }

                var videoStem = JournalPaths.VideoIdFromFileName(video.Filename);
                var videoPath = JournalPaths.VideoPath(video.JournalId, video.JournalEntryId, videoStem, Path.GetExtension(video.Filename));
                var videoFullPath = Files.ResolveExisting(videoPath);
                if (!System.IO.File.Exists(videoFullPath))
                {
                    videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Journal),
                        Path.Combine(video.JournalEntryId.ToString(), video.Filename));
                }

                if (!System.IO.File.Exists(videoFullPath))
                {
                    return Json(new ApiResponse { success = false, message = "Video file not found" });
                }

                // Create HLS directory structure under the video's own folder
                var hlsDir = JournalPaths.VideoHlsDir(video.JournalId, video.JournalEntryId, videoStem);
                var hlsFullPath = Path.Combine(Files.GetPath(Files.Paths.Journal), hlsDir);

                // Generate HLS playlist and variants
                var success = await Common.Videos.GenerateHlsPlaylist(videoFullPath, hlsFullPath, timeoutSeconds: 600);

                if (!success)
                {
                    return Json(new ApiResponse { success = false, message = "Failed to generate HLS playlist" });
                }

                return Json(new ApiResponse
                {
                    success = true,
                    data = new { hlsPath = hlsDir.Replace("\\", "/") }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        [HttpGet("/video/hls/{*path}")]
        public IActionResult GetHlsContent(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }

            try
            {
                var fullPath = Files.ResolveExisting(path);

                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("HLS content not found");
                }

                var ext = Path.GetExtension(path)?.ToLowerInvariant();
                var contentType = ext switch
                {
                    ".m3u8" => "application/vnd.apple.mpegurl",
                    ".ts" => "video/mp2t",
                    _ => "application/octet-stream"
                };

                // Add CORS headers for HLS streaming
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                Response.Headers.Add("Access-Control-Allow-Headers", "Range");

                return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return NotFound($"HLS content not found: {path}");
            }
        }
    }
}
