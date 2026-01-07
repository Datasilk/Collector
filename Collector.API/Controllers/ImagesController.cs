using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Collector.API.Models;
using Collector.Common;
using Collector.API.Services;

namespace Collector.API.Controllers
{
    public class ImagesController : ApiController
    {
        [HttpGet("/image/{*path}")]
        public IActionResult GetImage(string path)
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
                // Get the image file as bytes
                var imageBytes = Files.GetFileBytes(Files.Paths.Images, path);
                
                // Determine the correct content type from the file extension
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                var contentType = ext switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".svg" => "image/svg+xml",
                    ".bmp" => "image/bmp",
                    ".ico" => "image/x-icon",
                    ".tif" => "image/tiff",
                    ".tiff" => "image/tiff",
                    ".avif" => "image/avif",
                    ".heic" => "image/heic",
                    ".heif" => "image/heif",
                    _ => "application/octet-stream"
                };
                
                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                return NotFound($"Image not found: {path}");
            }
        }

        [Authorize]
        [HttpPost("/image-upload/{*path}")]
        public async Task<IActionResult> UploadImage(string path, [FromForm] IFormFile file)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Json(new ApiResponse { success = false, message = "Path is required" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new ApiResponse { success = false, message = "File is required" });
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    var success = Files.SaveFileBytes(Files.Paths.Images, path, fileBytes);
                if (!success)
                {
                    return Json(new ApiResponse { success = false, message = "Failed to save image" });
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
        [HttpPost("/image-upload/batch/{*folder}")]
        public async Task<IActionResult> UploadImagesBatch(string folder, [FromForm] List<IFormFile> files, [FromQuery] string moduleId = null)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return Json(new ApiResponse { success = false, message = "Path is required" });
            }

            if (files == null || files.Count == 0)
            {
                return Json(new ApiResponse { success = false, message = "At least one file is required" });
            }

            try
            {
                var normalizedFolder = NormalizeImagePath(folder);
                if (string.IsNullOrEmpty(normalizedFolder))
                {
                    return Json(new ApiResponse { success = false, message = "Invalid path" });
                }

                var savedFiles = new List<string>();
                var failedFiles = new List<string>();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var sanitizedModuleId = SanitizeFileNameSegment(moduleId);
                var index = 0;

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0)
                    {
                        failedFiles.Add(file?.FileName ?? "unknown");
                        continue;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();
                        var extension = Path.GetExtension(file.FileName);
                        if (string.IsNullOrWhiteSpace(extension))
                        {
                            extension = ".png";
                        }

                        var generatedName = $"{timestamp}-{(sanitizedModuleId ?? "img")}-{index}{extension}";
                        var relativePath = Path.Combine(normalizedFolder, generatedName).Replace("\\", "/");
                        var success = Files.SaveFileBytes(Files.Paths.Images, relativePath, fileBytes);

                        if (success)
                        {
                            savedFiles.Add(generatedName);
                            SaveThumbnail(relativePath, fileBytes);
                        }
                        else
                        {
                            failedFiles.Add(file.FileName ?? generatedName);
                        }
                    }

                    index++;
                }

                if (failedFiles.Count > 0)
                {
                    return Json(new ApiResponse
                    {
                        success = savedFiles.Count > 0,
                        message = "Failed to save one or more images",
                        data = new { saved = savedFiles, failed = failedFiles }
                    });
                }

                return Json(new ApiResponse
                {
                    success = true,
                    data = new { saved = savedFiles }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { success = false, message = ex.Message });
            }
        }

        private string NormalizeImagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var sanitized = path.Replace("\\", "/");
            if (sanitized.Contains(".."))
            {
                return null;
            }

            return sanitized.TrimStart('/');
        }
        private string SanitizeFileNameSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar.ToString(), string.Empty);
            }

            return value.Trim();
        }

        private void SaveThumbnail(string relativePath, byte[] originalBytes)
        {
            var thumbnailBytes = Images.CreateThumbnail(originalBytes, 640, 80);
            var thumbnailPath = BuildThumbnailPath(relativePath);
            Files.SaveFileBytes(Files.Paths.Images, thumbnailPath, thumbnailBytes);
        }

        private string BuildThumbnailPath(string relativePath)
        {
            var directory = Path.GetDirectoryName(relativePath)?.Replace("\\", "/");
            var fileName = Path.GetFileName(relativePath);
            var thumbnailFileName = $"thumb_{fileName}";
            return string.IsNullOrEmpty(directory) ? thumbnailFileName : $"{directory}/{thumbnailFileName}";
        }
    }
}
