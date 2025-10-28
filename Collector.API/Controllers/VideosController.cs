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
                
                // Save video file
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    
                    var success = Files.SaveFileBytes(Files.Paths.Videos, relativePath, fileBytes);
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

        private async Task<bool> GenerateThumbnail(string videoRelativePath, string thumbnailRelativePath)
        {
            try
            {
                var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
                var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath);
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(thumbnailFullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Use ffmpeg to generate thumbnail at 1 second mark with highest quality
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
                    return process.ExitCode == 0 && System.IO.File.Exists(thumbnailFullPath);
                }
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
