using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Collector.Common
{
    public static class Videos
    {
        /// <summary>
        /// Generates a thumbnail for a video. Tries ffmpeg first, falls back to yt-dlp if ffmpeg fails and videoUrl is provided.
        /// </summary>
        public static async Task<bool> GenerateThumbnail(string videoFullPath, string thumbnailFullPath, string videoUrl = null, int width = 0, int height = 0, bool crop = false, int seekSeconds = 1, int timeoutSeconds = 30)
        {
            // Check if thumbnail already exists
            if (File.Exists(thumbnailFullPath))
            {
                return true;
            }

            // Try ffmpeg first
            var ffmpegSuccess = await GenerateThumbnailWithFfmpeg(videoFullPath, thumbnailFullPath, width, height, crop, seekSeconds, timeoutSeconds);
            
            if (ffmpegSuccess)
            {
                return true;
            }

            // If ffmpeg failed and we have a video URL, try yt-dlp as fallback
            if (!string.IsNullOrEmpty(videoUrl))
            {
                Console.WriteLine("ffmpeg failed, attempting yt-dlp fallback...");
                return await GenerateThumbnailWithYtDlp(videoUrl, thumbnailFullPath, width, height, timeoutSeconds);
            }

            return false;
        }

        private static async Task<bool> GenerateThumbnailWithFfmpeg(string videoFullPath, string thumbnailFullPath, int width = 0, int height = 0, bool crop = false, int seekSeconds = 1, int timeoutSeconds = 30)
        {
            try
            {
                if (!File.Exists(videoFullPath))
                {
                    Console.WriteLine($"Video file not found: {videoFullPath}");
                    return false;
                }

                var directory = Path.GetDirectoryName(thumbnailFullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Build ffmpeg arguments with optional scaling and cropping
                var filterArgs = BuildFfmpegFilterArgs(width, height, crop);
                var arguments = $"-ss {seekSeconds} -i \"{videoFullPath}\" -vframes 1{filterArgs} -q:v {(crop ? "10" : "1")} \"{thumbnailFullPath}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Failed to start ffmpeg process");
                        return false;
                    }

                    // Add timeout
                    var timeoutTask = Task.Delay(timeoutSeconds * 1000);
                    var processTask = process.WaitForExitAsync();
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Console.WriteLine($"ffmpeg thumbnail generation timed out after {timeoutSeconds} seconds");
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        return false;
                    }

                    var success = process.ExitCode == 0 && File.Exists(thumbnailFullPath);
                    if (!success)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Console.WriteLine($"ffmpeg thumbnail generation failed: {error}");
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ffmpeg thumbnail generation: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> GenerateThumbnailWithYtDlp(string videoUrl, string thumbnailFullPath, int width = 0, int height = 0, int timeoutSeconds = 5)
        {
            try
            {
                var directory = Path.GetDirectoryName(thumbnailFullPath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Use yt-dlp to download the thumbnail
                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--write-thumbnail --skip-download --convert-thumbnails jpg -o \"{thumbnailFullPath}\" \"{videoUrl}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Failed to start yt-dlp process for thumbnail");
                        return false;
                    }

                    // Add timeout
                    var timeoutTask = Task.Delay(timeoutSeconds * 1000);
                    var processTask = process.WaitForExitAsync();
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Console.WriteLine($"yt-dlp thumbnail download timed out after {timeoutSeconds} seconds");
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        return false;
                    }

                    // yt-dlp creates files with .jpg extension, but may add extra suffixes
                    // Look for any .jpg file that was just created
                    var jpgFiles = Directory.GetFiles(directory, "*.jpg")
                        .Where(f => Path.GetFileName(f).StartsWith(Path.GetFileNameWithoutExtension(thumbnailFullPath)))
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .FirstOrDefault();

                    if (jpgFiles != null && File.Exists(jpgFiles))
                    {
                        // Rename to the expected filename if needed
                        if (jpgFiles != thumbnailFullPath)
                        {
                            File.Move(jpgFiles, thumbnailFullPath, overwrite: true);
                        }
                        
                        // Optionally resize if dimensions are specified
                        if (width > 0 && height > 0 && File.Exists(thumbnailFullPath))
                        {
                            // Use ffmpeg to resize the downloaded thumbnail
                            var filterArgs = BuildFfmpegFilterArgs(width, height, false);
                            var tempPath = thumbnailFullPath + ".temp.jpg";
                            var resizeArgs = $"-i \"{thumbnailFullPath}\" {filterArgs} -q:v 2 \"{tempPath}\"";
                            
                            var resizeStartInfo = new ProcessStartInfo
                            {
                                FileName = "ffmpeg",
                                Arguments = resizeArgs,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (var resizeProcess = Process.Start(resizeStartInfo))
                            {
                                await resizeProcess.WaitForExitAsync();
                                if (resizeProcess.ExitCode == 0 && File.Exists(tempPath))
                                {
                                    File.Move(tempPath, thumbnailFullPath, overwrite: true);
                                }
                            }
                        }
                        
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during yt-dlp thumbnail generation: {ex.Message}");
                return false;
            }
        }

        public static string BuildFfmpegFilterArgs(int width, int height, bool crop)
        {
            if (width <= 0 || height <= 0)
            {
                return ""; // No filtering
            }

            if (crop)
            {
                // Scale to cover the target size, then crop to exact dimensions
                return $" -vf \"scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height}\"";
            }
            else
            {
                // Scale to fit within dimensions, maintaining aspect ratio
                return $" -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease\"";
            }
        }

        public static async Task<(int width, int height, int duration)> GetVideoMetadata(string videoFullPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,duration -of csv=p=0 \"{videoFullPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
                {
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();
                    int parsedWidth = 0;
                    int parsedHeight = 0;
                    double parsedDuration = 0;
                    bool sawError = false;

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null) return;
                        Console.WriteLine(e.Data);
                        stdout.AppendLine(e.Data);

                        // Success line is expected to be CSV: width,height[,duration]
                        var line = e.Data.Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            var parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                int.TryParse(parts[0], out parsedWidth);
                                int.TryParse(parts[1], out parsedHeight);
                                if (parts.Length > 2)
                                {
                                    double.TryParse(parts[2], out parsedDuration);
                                }
                            }
                        }
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null) return;
                        Console.WriteLine(e.Data);
                        stderr.AppendLine(e.Data);
                        if (e.Data.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sawError = true;
                        }
                    };

                    if (!process.Start())
                    {
                        return (0, 0, 0);
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Timeout safeguard to avoid hangs due to any unexpected pipe issues
                    var timeoutTask = Task.Delay(15000);
                    var waitTask = process.WaitForExitAsync();
                    var completed = await Task.WhenAny(waitTask, timeoutTask);
                    if (completed == timeoutTask)
                    {
                        try { process.Kill(true); } catch { }
                    }
                    else
                    {
                        await waitTask; // ensure fully exited
                    }

                    if (!sawError && parsedWidth > 0 && parsedHeight > 0)
                    {
                        return (parsedWidth, parsedHeight, (int)parsedDuration);
                    }

                    // If process exited with non-zero or no parse, log stderr for diagnostics
                    if (stderr.Length > 0)
                    {
                        Console.WriteLine(stderr.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting video metadata: {ex.Message}");
            }

            return (0, 0, 0);
        }
    }
}
