using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Runtime.Versioning;

namespace Collector.Common
{
    public static class Videos
    {
        /// <summary>
        /// Generates a thumbnail for a video. Tries ffmpeg first, falls back to yt-dlp if ffmpeg fails and videoUrl is provided.
        /// </summary>
        public static async Task<bool> GenerateThumbnail(string videoFullPath, string thumbnailFullPath, string videoUrl = null, int width = 0, int height = 0, bool crop = false, int seekSeconds = 1, int timeoutSeconds = 30)
        {
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

                // Delete existing thumbnail if it exists
                if (File.Exists(thumbnailFullPath))
                {
                    File.Delete(thumbnailFullPath);
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

        #region YouTube Cookie Extraction

        private static string? _cookiesFilePath;
        private static readonly object _cookieLock = new object();

        /// <summary>
        /// Gets the path to the YouTube cookies file, extracting from Chrome if needed.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static string? GetYouTubeCookiesFilePath()
        {
            lock (_cookieLock)
            {
                if (!string.IsNullOrEmpty(_cookiesFilePath) && File.Exists(_cookiesFilePath))
                {
                    // Check if cookies file is less than 1 hour old
                    var fileInfo = new FileInfo(_cookiesFilePath);
                    if (fileInfo.LastWriteTime > DateTime.Now.AddHours(-1))
                    {
                        return _cookiesFilePath;
                    }
                }

                // Extract cookies from Chrome
                var cookiesPath = Path.Combine(Path.GetTempPath(), "youtube_cookies.txt");
                if (ExtractYouTubeCookiesFromChrome(cookiesPath))
                {
                    _cookiesFilePath = cookiesPath;
                    return _cookiesFilePath;
                }

                return null;
            }
        }

        /// <summary>
        /// Extracts YouTube cookies from Chrome and saves them in Netscape format.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static bool ExtractYouTubeCookiesFromChrome(string outputPath)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var chromeCookiesPath = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Network", "Cookies");
                
                if (!File.Exists(chromeCookiesPath))
                {
                    // Try alternative path
                    chromeCookiesPath = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cookies");
                }

                if (!File.Exists(chromeCookiesPath))
                {
                    Console.WriteLine("Chrome cookies database not found");
                    return false;
                }

                // Get the encryption key from Chrome's Local State
                var localStatePath = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Local State");
                byte[] encryptionKey = null;
                
                if (File.Exists(localStatePath))
                {
                    encryptionKey = GetChromeEncryptionKey(localStatePath);
                }

                // Copy the cookies database to a temp file (Chrome locks it)
                var tempDbPath = Path.Combine(Path.GetTempPath(), $"chrome_cookies_{Guid.NewGuid()}.db");
                File.Copy(chromeCookiesPath, tempDbPath, overwrite: true);

                try
                {
                    var cookies = new StringBuilder();
                    cookies.AppendLine("# Netscape HTTP Cookie File");
                    cookies.AppendLine("# https://curl.haxx.se/rfc/cookie_spec.html");
                    cookies.AppendLine("# This is a generated file! Do not edit.");
                    cookies.AppendLine();

                    using (var connection = new SQLiteConnection($"Data Source={tempDbPath};Version=3;Read Only=True;"))
                    {
                        connection.Open();

                        using (var command = new SQLiteCommand(
                            "SELECT host_key, path, is_secure, expires_utc, name, encrypted_value FROM cookies WHERE host_key LIKE '%youtube%' OR host_key LIKE '%google%'",
                            connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var hostKey = reader.GetString(0);
                                    var path = reader.GetString(1);
                                    var isSecure = reader.GetInt64(2) == 1;
                                    var expiresUtc = reader.GetInt64(3);
                                    var name = reader.GetString(4);
                                    var encryptedValue = (byte[])reader["encrypted_value"];

                                    var value = DecryptCookieValue(encryptedValue, encryptionKey);
                                    
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        // Convert Chrome timestamp to Unix timestamp
                                        // Chrome uses microseconds since Jan 1, 1601
                                        var unixExpires = (expiresUtc / 1000000) - 11644473600;
                                        if (unixExpires < 0) unixExpires = 0;

                                        // Netscape format: domain, tailmatch, path, secure, expires, name, value
                                        var tailMatch = hostKey.StartsWith(".") ? "TRUE" : "FALSE";
                                        var secure = isSecure ? "TRUE" : "FALSE";
                                        
                                        cookies.AppendLine($"{hostKey}\t{tailMatch}\t{path}\t{secure}\t{unixExpires}\t{name}\t{value}");
                                    }
                                }
                            }
                        }
                    }

                    File.WriteAllText(outputPath, cookies.ToString());
                    Console.WriteLine($"Extracted YouTube cookies to {outputPath}");
                    return true;
                }
                finally
                {
                    // Clean up temp database
                    try { File.Delete(tempDbPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting Chrome cookies: {ex.Message}");
                return false;
            }
        }

        [SupportedOSPlatform("windows")]
        private static byte[]? GetChromeEncryptionKey(string localStatePath)
        {
            try
            {
                var localStateJson = File.ReadAllText(localStatePath);
                
                // Find the encrypted_key in the JSON
                var keyStart = localStateJson.IndexOf("\"encrypted_key\":\"");
                if (keyStart == -1) return null;
                
                keyStart += "\"encrypted_key\":\"".Length;
                var keyEnd = localStateJson.IndexOf("\"", keyStart);
                if (keyEnd == -1) return null;

                var encryptedKeyBase64 = localStateJson.Substring(keyStart, keyEnd - keyStart);
                var encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

                // Remove "DPAPI" prefix (5 bytes)
                if (encryptedKey.Length > 5 && Encoding.ASCII.GetString(encryptedKey, 0, 5) == "DPAPI")
                {
                    var keyWithoutPrefix = new byte[encryptedKey.Length - 5];
                    Array.Copy(encryptedKey, 5, keyWithoutPrefix, 0, keyWithoutPrefix.Length);
                    
                    // Decrypt using DPAPI
                    return ProtectedData.Unprotect(keyWithoutPrefix, null, DataProtectionScope.CurrentUser);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Chrome encryption key: {ex.Message}");
                return null;
            }
        }

        [SupportedOSPlatform("windows")]
        private static string DecryptCookieValue(byte[]? encryptedValue, byte[]? key)
        {
            try
            {
                if (encryptedValue == null || encryptedValue.Length == 0)
                    return string.Empty;

                // Check if it's v10/v11 encrypted (starts with "v10" or "v11")
                if (encryptedValue.Length > 3 && encryptedValue[0] == 'v' && encryptedValue[1] == '1')
                {
                    if (key == null)
                    {
                        Console.WriteLine("No encryption key available for v10/v11 cookie");
                        return string.Empty;
                    }

                    // v10/v11 uses AES-256-GCM
                    // Format: "v10" or "v11" (3 bytes) + nonce (12 bytes) + ciphertext + tag (16 bytes)
                    var nonce = new byte[12];
                    Array.Copy(encryptedValue, 3, nonce, 0, 12);

                    var ciphertextWithTag = new byte[encryptedValue.Length - 15];
                    Array.Copy(encryptedValue, 15, ciphertextWithTag, 0, ciphertextWithTag.Length);

                    var ciphertext = new byte[ciphertextWithTag.Length - 16];
                    var tag = new byte[16];
                    Array.Copy(ciphertextWithTag, 0, ciphertext, 0, ciphertext.Length);
                    Array.Copy(ciphertextWithTag, ciphertext.Length, tag, 0, 16);

                    var plaintext = new byte[ciphertext.Length];
                    using (var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize))
                    {
                        aes.Decrypt(nonce, ciphertext, tag, plaintext);
                    }
                    return Encoding.UTF8.GetString(plaintext);
                }
                else
                {
                    // Old DPAPI encryption
                    var decrypted = ProtectedData.Unprotect(encryptedValue, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting cookie: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region HLS Adaptive Streaming

        /// <summary>
        /// Generates HLS playlist and multiple quality variants for adaptive streaming
        /// </summary>
        public static async Task<bool> GenerateHlsPlaylist(string videoFullPath, string hlsOutputDirectory, int timeoutSeconds = 300, Action<int, string> onProgress = null)
        {
            try
            {
                if (!File.Exists(videoFullPath))
                {
                    Console.WriteLine($"Video file not found: {videoFullPath}");
                    return false;
                }

                if (!Directory.Exists(hlsOutputDirectory))
                {
                    Directory.CreateDirectory(hlsOutputDirectory);
                }

                // Get video metadata to determine available qualities
                var (width, height, duration) = await GetVideoMetadata(videoFullPath);
                if (width == 0 || height == 0)
                {
                    Console.WriteLine("Failed to get video metadata");
                    return false;
                }

                // Define quality variants based on source resolution
                var variants = GetQualityVariants(width, height);
                if (variants.Count == 0)
                {
                    Console.WriteLine("No quality variants available");
                    return false;
                }

                // Generate each quality variant (dynamically divided by variant count)
                var variantPlaylists = new List<(string name, int bandwidth, int width, int height, string playlist)>();
                var variantIndex = 0;
                var progressPerVariant = 100.0 / variants.Count;
                
                foreach (var variant in variants)
                {
                    var variantDir = Path.Combine(hlsOutputDirectory, variant.name);
                    Directory.CreateDirectory(variantDir);
                    var playlistPath = Path.Combine(variantDir, "playlist.m3u8");
                    var baseProgress = variantIndex * progressPerVariant;
                    var success = await GenerateHlsVariant(videoFullPath, variantDir, variant.width, variant.height, variant.bitrate, timeoutSeconds, (variantProgress) =>
                    {
                        var overallProgress = (int)(baseProgress + (variantProgress * progressPerVariant / 100.0));
                        onProgress?.Invoke(overallProgress, $"Encoding {variant.name} ({variantProgress:F0}%)...");
                    });
                    
                    if (success)
                    {
                        variantPlaylists.Add((variant.name, variant.bitrate * 1000, variant.width, variant.height, $"{variant.name}/playlist.m3u8"));
                    }
                    
                    variantIndex++;
                }

                if (variantPlaylists.Count == 0)
                {
                    Console.WriteLine("Failed to generate any quality variants");
                    return false;
                }

                // Generate master playlist
                var masterPlaylistPath = Path.Combine(hlsOutputDirectory, "master.m3u8");
                await GenerateMasterPlaylist(masterPlaylistPath, variantPlaylists);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating HLS playlist: {ex.Message}");
                return false;
            }
        }

        private static List<(string name, int width, int height, int bitrate)> GetQualityVariants(int sourceWidth, int sourceHeight)
        {
            var variants = new List<(string name, int width, int height, int bitrate)>();

            // Define quality presets (name, width, height, bitrate in kbps)
            var presets = new[]
            {
                ("360p", 640, 360, 800),
                ("480p", 854, 480, 1400),
                ("720p", 1280, 720, 2800),
                ("1080p", 1920, 1080, 5000)
            };

            // Only include qualities at or below source resolution
            foreach (var preset in presets)
            {
                if (preset.Item2 <= sourceWidth && preset.Item3 <= sourceHeight)
                {
                    variants.Add(preset);
                }
            }

            // Always include at least one quality (lowest)
            if (variants.Count == 0 && presets.Length > 0)
            {
                variants.Add(presets[0]);
            }

            return variants;
        }

        private static async Task<bool> GenerateHlsVariant(string videoFullPath, string outputDir, int width, int height, int bitrateKbps, int timeoutSeconds, Action<double> onProgress = null)
        {
            try
            {
                Console.WriteLine($"GenerateHlsVariant called:");
                Console.WriteLine($"  videoFullPath: {videoFullPath}");
                Console.WriteLine($"  outputDir: {outputDir}");
                Console.WriteLine($"  Video exists: {File.Exists(videoFullPath)}");
                Console.WriteLine($"  Output dir exists: {Directory.Exists(outputDir)}");

                var playlistPath = Path.Combine(outputDir, "playlist.m3u8");
                var segmentPattern = Path.Combine(outputDir, "segment%03d.ts");

                Console.WriteLine($"  playlistPath: {playlistPath}");
                Console.WriteLine($"  segmentPattern: {segmentPattern}");

                // FFmpeg command for HLS generation with specific quality
                var arguments = $"-y -i \"{videoFullPath}\" " +
                    $"-vf scale={width}:{height} " +
                    $"-c:v libx264 -preset fast -crf 23 -b:v {bitrateKbps}k -maxrate {bitrateKbps * 1.5}k -bufsize {bitrateKbps * 2}k " +
                    $"-c:a aac -b:a 128k -ac 2 " +
                    $"-progress pipe:1 " +
                    $"-f hls -hls_time 6 -hls_list_size 0 -hls_segment_filename \"{segmentPattern}\" " +
                    $"\"{playlistPath}\"";

                Console.WriteLine($"FFmpeg command: ffmpeg {arguments}");

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
                        Console.WriteLine("Failed to start ffmpeg process for HLS variant");
                        return false;
                    }

                    // Get video duration for progress calculation
                    var (_, _, videoDuration) = await GetVideoMetadata(videoFullPath);

                    // Read output asynchronously and parse progress
                    var stderrTask = Task.Run(async () =>
                    {
                        var stderr = new StringBuilder();
                        var buffer = new char[4096];
                        int bytesRead;
                        while ((bytesRead = await process.StandardError.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            stderr.Append(buffer, 0, bytesRead);
                        }
                        return stderr.ToString();
                    });

                    var stdoutTask = Task.Run(async () =>
                    {
                        var stdout = new StringBuilder();
                        string line;
                        var reader = process.StandardOutput;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            stdout.AppendLine(line);
                            
                            // Parse progress from FFmpeg output
                            if (line.StartsWith("out_time_ms=") && videoDuration > 0)
                            {
                                var timeMs = line.Replace("out_time_ms=", "");
                                if (long.TryParse(timeMs, out long ms))
                                {
                                    var currentSeconds = ms / 1000000.0;
                                    var progress = Math.Min(100, (currentSeconds / videoDuration) * 100);
                                    Console.WriteLine($"  {width}x{height} encoding progress: {progress:F1}% ({currentSeconds:F1}s / {videoDuration:F1}s)");
                                    
                                    // Report progress to callback
                                    onProgress?.Invoke(progress);
                                }
                            }
                        }
                        return stdout.ToString();
                    });

                    var timeoutTask = Task.Delay(timeoutSeconds * 1000);
                    var processTask = process.WaitForExitAsync();
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Console.WriteLine($"HLS variant generation timed out after {timeoutSeconds} seconds");
                        try { process.Kill(); } catch { }
                        return false;
                    }

                    var stderr = await stderrTask;
                    var stdout = await stdoutTask;

                    Console.WriteLine($"FFmpeg exit code: {process.ExitCode}");
                    if (!string.IsNullOrEmpty(stdout))
                    {
                        Console.WriteLine($"FFmpeg stdout: {stdout}");
                    }
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        Console.WriteLine($"FFmpeg stderr: {stderr}");
                    }

                    var success = process.ExitCode == 0 && File.Exists(playlistPath);
                    if (!success)
                    {
                        Console.WriteLine($"HLS variant generation failed. Exit code: {process.ExitCode}, Playlist exists: {File.Exists(playlistPath)}");
                    }
                    else
                    {
                        Console.WriteLine($"Successfully generated HLS variant at {playlistPath}");
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during HLS variant generation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static async Task GenerateMasterPlaylist(string masterPlaylistPath, List<(string name, int bandwidth, int width, int height, string playlist)> variants)
        {
            var content = new StringBuilder();
            content.AppendLine("#EXTM3U");
            content.AppendLine("#EXT-X-VERSION:3");

            foreach (var variant in variants)
            {
                content.AppendLine($"#EXT-X-STREAM-INF:BANDWIDTH={variant.bandwidth},RESOLUTION={variant.width}x{variant.height}");
                content.AppendLine(variant.playlist);
            }

            await File.WriteAllTextAsync(masterPlaylistPath, content.ToString());
        }

        #endregion
    }
}
