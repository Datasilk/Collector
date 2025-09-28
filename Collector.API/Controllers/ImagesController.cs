using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
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
    }
}
