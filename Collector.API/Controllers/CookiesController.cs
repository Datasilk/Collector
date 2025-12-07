using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Collector.API.Models;

namespace Collector.API.Controllers
{
    [Authorize]
    public class CookiesController : ApiController
    {
        private static readonly string CookiesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "cookies");

        /// <summary>
        /// Check if YouTube cookies exist for the current user
        /// </summary>
        [HttpGet("youtube-cookies-exist")]
        public IActionResult YouTubeCookiesExist()
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new ApiResponse { message = "User not authenticated" });
                }

                var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
                var exists = System.IO.File.Exists(cookiePath);

                return Json(new ApiResponse { success = true, data = new { exists } });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { message = ex.Message });
            }
        }

        /// <summary>
        /// Upload Chrome cookies file and extract YouTube cookies
        /// </summary>
        [HttpPost("upload-cookies")]
        public async Task<IActionResult> UploadCookies(IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new ApiResponse { message = "User not authenticated" });
                }

                if (file == null || file.Length == 0)
                {
                    return Json(new ApiResponse { message = "No file uploaded" });
                }

                // Ensure cookies folder exists
                if (!Directory.Exists(CookiesFolder))
                {
                    Directory.CreateDirectory(CookiesFolder);
                }

                // Save the uploaded cookies file
                var uploadedCookiePath = Path.Combine(CookiesFolder, $"cookies-{userId}.txt");
                using (var stream = new FileStream(uploadedCookiePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Extract YouTube-specific cookies
                var youtubeCookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
                ExtractYouTubeCookies(uploadedCookiePath, youtubeCookiePath);

                return Json(new ApiResponse { success = true, data = new { message = "Cookies uploaded successfully" } });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete user's cookies files
        /// </summary>
        [HttpPost("delete-cookies")]
        public IActionResult DeleteCookies()
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new ApiResponse { message = "User not authenticated" });
                }

                var uploadedCookiePath = Path.Combine(CookiesFolder, $"cookies-{userId}.txt");
                var youtubeCookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");

                if (System.IO.File.Exists(uploadedCookiePath))
                {
                    System.IO.File.Delete(uploadedCookiePath);
                }

                if (System.IO.File.Exists(youtubeCookiePath))
                {
                    System.IO.File.Delete(youtubeCookiePath);
                }

                return Json(new ApiResponse { success = true });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { message = ex.Message });
            }
        }

        /// <summary>
        /// Get the path to the YouTube cookies file for a user (used by VideoWorker)
        /// </summary>
        public static string? GetYouTubeCookiesPath(Guid userId)
        {
            var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
            return System.IO.File.Exists(cookiePath) ? cookiePath : null;
        }

        /// <summary>
        /// Extract YouTube and Google cookies from a Netscape format cookies file
        /// </summary>
        private void ExtractYouTubeCookies(string sourcePath, string destPath)
        {
            var lines = System.IO.File.ReadAllLines(sourcePath);
            var youtubeLines = new List<string>();

            foreach (var line in lines)
            {
                // Keep comment lines
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    youtubeLines.Add(line);
                    continue;
                }

                // Check if this is a YouTube or Google cookie
                var lowerLine = line.ToLower();
                if (lowerLine.Contains("youtube") || lowerLine.Contains("google"))
                {
                    youtubeLines.Add(line);
                }
            }

            System.IO.File.WriteAllLines(destPath, youtubeLines);
        }
    }
}
