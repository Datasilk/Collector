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
                // Get the thumbnail file as bytes
                var thumbnailBytes = Files.GetFileBytes(Files.Paths.Videos, path);
                
                // Return as JPEG image
                return File(thumbnailBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return NotFound($"Thumbnail not found: {path}");
            }
        }

        [HttpGet("/video/preview/{entryId}/{videoFileName}/{second}")]
        public async Task<IActionResult> GetSeekPreview(string entryId, string videoFileName, int second)
        {
            if (string.IsNullOrEmpty(entryId) || string.IsNullOrEmpty(videoFileName))
            {
                return NotFound();
            }

            try
            {
                // Round down to nearest 10 seconds
                var roundedSecond = (int)Math.Floor(second / 10.0) * 10;
                
                // Build paths (videoFileName includes extension)
                var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFileName);
                var previewFileName = $"preview_{roundedSecond}.jpg";
                var previewPath = Path.Combine(entryId, videoFileNameWithoutExt, previewFileName);
                var previewFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), previewPath);
                
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

                    // Build video file path (videoFileName includes extension)
                    var videoPath = Path.Combine(entryId, videoFileName);
                    var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoPath);
                    
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
                var previewBytes = Files.GetFileBytes(Files.Paths.Videos, previewPath);
                
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

                // Build file path
                var videoPath = Path.Combine(video.JournalEntryId.ToString(), video.Filename);
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoPath);
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
                // Get the full file path
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), path);
                
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
                var relativePath = Path.Combine(entryId, fileName);
                
                // Save video file by streaming directly to disk (optimized for large files)
                using (var stream = file.OpenReadStream())
                {
                    var success = await Files.SaveFileStreamAsync(Files.Paths.Videos, relativePath, stream);
                    if (!success)
                    {
                        return Json(new ApiResponse { success = false, message = "Failed to save video" });
                    }
                }

                // Generate thumbnail
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
                var thumbnailRelativePath = Path.Combine(entryId, thumbnailFileName);
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
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
                var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath);
                
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
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);

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

                var videoPath = Path.Combine(video.JournalEntryId.ToString(), video.Filename);
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(video.Filename)}_thumb.jpg";
                var thumbnailRelativePath = Path.Combine(video.JournalEntryId.ToString(), thumbnailFileName);

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
                    var videoPath = Path.Combine(model.EntryId.ToString(), video.Filename);
                    var thumbnailPath = Path.Combine(model.EntryId.ToString(), 
                        $"{Path.GetFileNameWithoutExtension(video.Filename)}_thumb.jpg");
                    
                    // Delete video file
                    Files.DeleteFile(Files.Paths.Videos, videoPath);
                    
                    // Delete thumbnail file
                    Files.DeleteFile(Files.Paths.Videos, thumbnailPath);
                }
                
                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }
    }
}
