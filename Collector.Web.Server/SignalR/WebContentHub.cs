using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Collector.Common;
using Collector.Common.Models;
using Collector.Common.Models.JournalModules;
using Collector.Data.Entities;
using Collector.Data.Interfaces;

namespace Collector.Web.Server.SignalR
{
    public class WebContentHub : Hub
    {
        private readonly ILogger<WebContentHub> _logger;
        private readonly IJournalEntriesRepository _entriesRepository;
        private readonly Random _random;

        public WebContentHub(ILogger<WebContentHub> logger, IJournalEntriesRepository entriesRepository)
        {
            _logger = logger;
            _entriesRepository = entriesRepository;
            _random = new Random();
            _logger.LogInformation("WebContentHub instance created");
        }

        public async Task<object> ScrapeUrl(string url, int journalId, Guid? parentEntryId = null, string appUserId = null)
        {
            try
            {
                _logger.LogInformation($"ScrapeUrl called: url={url}, journalId={journalId}, appUserId={appUserId}");

                // Notify client that scraping has started
                await Clients.Caller.SendAsync("ScrapeStatus", $"Scraping URL {url}");

                if (string.IsNullOrWhiteSpace(url))
                {
                    return new { success = false, message = "URL is required" };
                }

                var modules = new List<object>();
                string title = string.Empty;
                string description = string.Empty;

                // Determine title based on content type
                if (IsYouTubeUrl(url))
                {
                    _logger.LogInformation($"Detected YouTube URL: {url}");

                    // Remove playlist (list) parameter from YouTube URLs so yt-dlp
                    // and the stored entry refer to a single video, not a playlist
                    var videoUrl = RemoveYouTubeListParameter(url);

                    await Clients.Caller.SendAsync("ScrapeStatus", "Getting metadata from YouTube...");

                    // Get YouTube video title & description using yt-dlp
                    var metadata = await GetYouTubeMetadata(videoUrl, appUserId);
                    if (metadata.Title == null)
                    {
                        await Clients.Caller.SendAsync("OnError", "Failed to retrieve YouTube metadata. Please ensure the Chrome extension for Collector is installed and try again.");
                        return new { success = false, message = "Failed to retrieve YouTube metadata" };
                    }
                    title = metadata.Title;
                    description = metadata.Description;

                    // Truncate title to 128 characters
                    if (!string.IsNullOrEmpty(title) && title.Length > 128)
                    {
                        title = title.Substring(0, 128);
                    }

                    // Create a video player module with autoTryAgain enabled and random ID
                    modules.Add(new VideoPlayer
                    {
                        Id = GenerateRandomId(),
                        Url = videoUrl,
                        AutoTryAgain = true,
                        Title = title
                    });

                    // Add a text-editor module with video title and description
                    var encodedTitle = WebUtility.HtmlEncode(title ?? string.Empty);

                    // Break description into paragraphs by double line breaks, and
                    // convert single line breaks within a paragraph into <br/>
                    var descriptionText = description ?? string.Empty;

                    // Normalize newlines to \n
                    var normalized = descriptionText.Replace("\r\n", "\n").Replace('\r', '\n');

                    // Split on two or more consecutive newlines to get paragraphs
                    var rawParagraphs = Regex.Split(normalized, "\n{2,}");

                    var paragraphHtml = string.Join(string.Empty,
                        rawParagraphs
                            .Select(p => p.Trim('\n', '\r', ' '))
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Select(p =>
                            {
                                var lines = p.Split('\n');
                                var encodedLines = lines.Select(line => WebUtility.HtmlEncode(line));
                                var innerHtml = string.Join("<br/>", encodedLines);
                                return $"<p>{innerHtml}</p>";
                            }));

                    var html = $"<h4>{encodedTitle}</h4>{paragraphHtml}";

                    modules.Add(new
                    {
                        id = GenerateRandomId(),
                        type = "text-editor",
                        html,
                        manuallyAdded = false
                    });
                }
                else
                {
                    _logger.LogInformation($"URL is not a YouTube URL, attempting to download HTML: {url}");

                    await Clients.Caller.SendAsync("ScrapeStatus", "Downloading HTML web page...");

                    // Download article DOM JSON via Charlotte router
                    string resolvedUrl;
                    var domJson = Common.Article.Download(url, out resolvedUrl);

                    if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("file:") || domJson.StartsWith("Error"))
                    {
                        _logger.LogWarning("Failed to download or parse article DOM for URL {Url}. Result prefix: {Prefix}", url, domJson?.Substring(0, Math.Min(domJson.Length, 32)));
                    }
                    else
                    {
                        try
                        {
                            await Clients.Caller.SendAsync("ScrapeStatus", "Analyzing HTML document...");

                            var analyzedArticle = Html.DeserializeArticle(domJson);
                            Html.GetArticleInfoFromDOM(analyzedArticle);
                            title = analyzedArticle.title ?? string.Empty;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error analyzing article DOM for URL {Url}", url);
                        }
                    }
                }
                // Fallback title if we couldn't determine one
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = url;
                }

                // Create a new journal entry with the derived title
                var entry = new JournalEntry
                {
                    JournalId = journalId,
                    ParentEntryId = parentEntryId,
                    Title = title,
                    Description = string.Empty,
                    Url = url
                };

                var entryId = _entriesRepository.Add(entry);
                _logger.LogInformation("Journal entry created from web content. JournalId={JournalId}, EntryId={EntryId}, Title={Title}", journalId, entryId, title);

                // Build entry content JSON and save it to the journal files folder
                await Clients.Caller.SendAsync("ScrapeStatus", "Saving journal entry content...");

                var entryContent = new
                {
                    modules = modules.Count > 0 ? modules : null
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var contentJson = JsonSerializer.Serialize(entryContent, jsonOptions);
                var filePath = $"{entryId:N}.json";
                var saveSuccess = Files.SaveFile(Files.Paths.Journal, filePath, contentJson);

                if (!saveSuccess)
                {
                    _logger.LogWarning("Failed to save journal entry content JSON for EntryId={EntryId}", entryId);
                }

                await Clients.Caller.SendAsync("ScrapeStatus", "Completed scraping web content.");

                // Return entryId and title back to the client
                return new
                {
                    success = true,
                    entryId,
                    title
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scraping URL: {url}");
                return new { success = false, message = ex.Message };
            }
        }

        private string GenerateRandomId()
        {
            return _random.Next(1, 10000000).ToString();
        }

        private bool IsYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // Match various YouTube URL formats:
            // - https://www.youtube.com/watch?v=VIDEO_ID
            // - https://youtu.be/VIDEO_ID
            // - https://m.youtube.com/watch?v=VIDEO_ID
            // - https://youtube.com/watch?v=VIDEO_ID
            var youtubePattern = @"^(https?://)?(www\.|m\.)?(youtube\.com/(watch\?v=|embed/|v/)|youtu\.be/)[\w-]+";
            return Regex.IsMatch(url, youtubePattern, RegexOptions.IgnoreCase);
        }

        private string RemoveYouTubeListParameter(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            try
            {
                var uri = new Uri(url);

                // Only clean standard YouTube watch URLs; leave others untouched
                if (!uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }

                var queryDict = QueryHelpers.ParseQuery(uri.Query);

                if (!queryDict.ContainsKey("list"))
                {
                    return url;
                }

                // Remove the 'list' parameter
                queryDict.Remove("list");

                // Rebuild query string without 'list'
                var newQuery = string.Join("&",
                    queryDict.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}={WebUtility.UrlEncode(v)}")));

                var builder = new UriBuilder(uri)
                {
                    Query = newQuery
                };

                return builder.Uri.ToString();
            }
            catch
            {
                // If anything goes wrong, fall back to the original URL
                return url;
            }
        }

        private static readonly string CookiesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "cookies");

        private string GetCookiesArgument(string appUserId)
        {
            if (string.IsNullOrEmpty(appUserId))
                return string.Empty;

            // Check if user has saved cookies
            if (Guid.TryParse(appUserId, out var userId))
            {
                var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
                if (File.Exists(cookiePath))
                {
                    return $"--cookies \"{cookiePath}\" ";
                }
            }

            return string.Empty;
        }

        private async Task<(string Title, string Description)> GetYouTubeMetadata(string url, string appUserId)
        {
            // First attempt with existing cookies
            var result = await TryGetYouTubeMetadata(url, appUserId);
            if (!string.IsNullOrEmpty(result.Title))
            {
                return result;
            }

            // If failed, try to get fresh cookies from extension and retry
            _logger.LogInformation("First metadata attempt failed, requesting fresh cookies from extension for user {AppUserId}", appUserId);
            await Clients.Caller.SendAsync("ScrapeStatus", "Refreshing YouTube cookie...");

            var freshCookies = await Workers.RequestCookiesFromClientAsync(appUserId, Guid.Empty, "youtube.com", CancellationToken.None);
            if (!string.IsNullOrEmpty(freshCookies))
            {
                // Save fresh cookies to main cookie file
                if (Guid.TryParse(appUserId, out var parsedUserId))
                {
                    var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{parsedUserId}.txt");
                    Directory.CreateDirectory(CookiesFolder);
                    await File.WriteAllTextAsync(cookiePath, freshCookies);
                    _logger.LogInformation("Received fresh cookies from extension, saved to {Path}", cookiePath);
                }

                // Retry with fresh cookies
                await Clients.Caller.SendAsync("ScrapeStatus", "Retrying metadata fetch with fresh cookie...");
                result = await TryGetYouTubeMetadata(url, appUserId);
                if (string.IsNullOrEmpty(result.Title))
                {
                    await Clients.Caller.SendAsync("OnError", "Failed to retrieve YouTube metadata. Please ensure the Chrome extension for Collector is installed and try again.");
                    return (null, null);
                }
            }
            else
            {
                _logger.LogWarning("Failed to get fresh cookies from extension for user {AppUserId}", appUserId);
                await Clients.Caller.SendAsync("OnError", "Failed to retrieve YouTube metadata. Please ensure the Chrome extension for Collector is installed and try again.");
                return (null, null);
            }

            return result;
        }

        private async Task CheckYtDlpVersionWarning(string stderr)
        {
            if (!string.IsNullOrEmpty(stderr) && 
                (stderr.Contains("Your yt-dlp version") && stderr.Contains("is older than 90 days") ||
                 stderr.Contains("It is strongly recommended to always use the latest version")))
            {
                var warningMessage = "WARNING: yt-dlp is outdated. Please update by running download-tools.bat or 'yt-dlp --update'";
                _logger.LogWarning("yt-dlp version warning detected: {Stderr}", stderr);
                await Clients.Caller.SendAsync("GenerateError", warningMessage);
            }
        }

        private async Task<string> GetYouTubeTranscript(string url, string appUserId)
        {
            try
            {
                // Use yt-dlp to download transcript files without downloading the video
                var tempBaseName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var cookiesArg = GetCookiesArgument(appUserId);
                var arguments = $"{cookiesArg}--js-runtimes node --write-auto-subs --write-subs --skip-download -o \"{tempBaseName}\" \"{url}\"";
                _logger.LogInformation("GetYouTubeTranscript yt-dlp arguments: {Arguments}", arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Ensure PATH is inherited so yt-dlp can find Node.js for n challenge solving
                startInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("Failed to start yt-dlp process when getting transcript for URL {Url}", url);
                        return string.Empty;
                    }

                    var stderrTask = process.StandardError.ReadToEndAsync();
                    var stdoutTask = process.StandardOutput.ReadToEndAsync();

                    await Task.WhenAll(stderrTask, stdoutTask, process.WaitForExitAsync());

                    var stderr = await stderrTask;
                    await CheckYtDlpVersionWarning(stderr);

                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("yt-dlp exited with code {Code} for URL {Url}. Error: {Error}", process.ExitCode, url, stderr);
                        return string.Empty;
                    }

                    // Look for subtitle files (.vtt, .srt, .json3, etc.)
                    var directory = Path.GetDirectoryName(tempBaseName);
                    var baseFileName = Path.GetFileName(tempBaseName);
                    var subtitleFiles = Directory.GetFiles(directory, $"{baseFileName}*.vtt")
                        .Concat(Directory.GetFiles(directory, $"{baseFileName}*.srt"))
                        .ToList();

                    if (!subtitleFiles.Any())
                    {
                        _logger.LogWarning("No transcript files found for URL {Url}", url);
                        return string.Empty;
                    }

                    // Read the first available transcript file
                    var transcriptFile = subtitleFiles.First();
                    var transcriptContent = await File.ReadAllTextAsync(transcriptFile, Encoding.UTF8);

                    // Clean up transcript files
                    foreach (var file in subtitleFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to delete transcript file {Path}", file);
                        }
                    }

                    // Parse VTT/SRT format to extract just the text
                    var lines = transcriptContent.Split('\n');
                    var textLines = new List<string>();
                    bool skipNextLine = false;

                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();

                        // Skip WEBVTT header, timestamps, and empty lines
                        if (string.IsNullOrWhiteSpace(trimmedLine) ||
                            trimmedLine.StartsWith("WEBVTT") ||
                            trimmedLine.StartsWith("Kind:") ||
                            trimmedLine.StartsWith("Language:") ||
                            trimmedLine.Contains("-->") ||
                            int.TryParse(trimmedLine, out _))
                        {
                            continue;
                        }

                        // Remove HTML tags and add to text
                        var cleanedLine = System.Text.RegularExpressions.Regex.Replace(trimmedLine, "<[^>]+>", "");
                        if (!string.IsNullOrWhiteSpace(cleanedLine))
                        {
                            textLines.Add(cleanedLine);
                        }
                    }

                    return string.Join(" ", textLines);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube transcript for URL {Url}", url);
                return string.Empty;
            }
        }

        private async Task<(string Title, string Description)> TryGetYouTubeMetadata(string url, string appUserId)
        {
            try
            {
                // Use yt-dlp to write a JSON metadata file without downloading the video.
                // We'll create a random temp base path and let yt-dlp append .info.json.
                var tempBaseName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var expectedJsonPath = tempBaseName + ".info.json";
                var cookiesArg = GetCookiesArgument(appUserId);
                var arguments = $"{cookiesArg}--js-runtimes node --skip-download --write-info-json -o \"{tempBaseName}\" \"{url}\"";
                
                // Log cookie file path for debugging
                if (Guid.TryParse(appUserId, out var userId))
                {
                    var cookiePath = Path.Combine(CookiesFolder, $"cookies-youtube-{userId}.txt");
                    _logger.LogInformation("TryGetYouTubeMetadata - Cookie file path: {CookiePath}, Exists: {Exists}", cookiePath, File.Exists(cookiePath));
                }
                
                _logger.LogInformation("TryGetYouTubeMetadata yt-dlp arguments: {Arguments}", arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Ensure PATH is inherited so yt-dlp can find Node.js for n challenge solving
                startInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("Failed to start yt-dlp process when getting metadata for URL {Url}", url);
                        return (string.Empty, string.Empty);
                    }

                    var stderrTask = process.StandardError.ReadToEndAsync();
                    var stdoutTask = process.StandardOutput.ReadToEndAsync();

                    await Task.WhenAll(stderrTask, stdoutTask, process.WaitForExitAsync());

                    var stderr = await stderrTask;
                    var stdout = await stdoutTask;
                    
                    _logger.LogInformation("TryGetYouTubeMetadata - Exit Code: {ExitCode}", process.ExitCode);
                    _logger.LogInformation("TryGetYouTubeMetadata - STDOUT: {StdOut}", stdout);
                    _logger.LogInformation("TryGetYouTubeMetadata - STDERR: {StdErr}", stderr);
                    
                    await CheckYtDlpVersionWarning(stderr);

                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("yt-dlp exited with code {Code} for URL {Url}. Error: {Error}", process.ExitCode, url, stderr);
                        return (string.Empty, string.Empty);
                    }

                    if (!File.Exists(expectedJsonPath))
                    {
                        _logger.LogWarning("yt-dlp did not produce expected JSON file for URL {Url}. Expected path: {Path}", url, expectedJsonPath);
                        return (string.Empty, string.Empty);
                    }

                    try
                    {
                        var json = await File.ReadAllTextAsync(expectedJsonPath, Encoding.UTF8);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            return (string.Empty, string.Empty);
                        }

                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        string title = string.Empty;
                        string description = string.Empty;

                        if (root.TryGetProperty("fulltitle", out var fullTitleProp) && fullTitleProp.ValueKind == JsonValueKind.String)
                        {
                            title = fullTitleProp.GetString();
                        }
                        else if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                        {
                            title = titleProp.GetString();
                        }

                        if (root.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                        {
                            description = descProp.GetString();
                        }

                        return (title ?? string.Empty, description ?? string.Empty);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(expectedJsonPath))
                            {
                                File.Delete(expectedJsonPath);
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to delete yt-dlp metadata file {Path}", expectedJsonPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube metadata for URL {Url}", url);
                return (string.Empty, string.Empty);
            }
        }

        public async Task GenerateContent(string url, bool includeVideo, bool includeTitle, bool includeDescription, bool includeTranscriptResearch, bool includeCommentsResearch, string appUserId, string cacheJson = "", string userInstructions = "", bool generateChapters = true, string chapterCount = "any")
        {
            try
            {
                _logger.LogInformation($"GenerateContent called: url={url}, appUserId={appUserId}, hasCache={!string.IsNullOrEmpty(cacheJson)}");

                // Parse cache if provided
                GenerationCache cache = null;
                if (!string.IsNullOrEmpty(cacheJson))
                {
                    try
                    {
                        cache = JsonSerializer.Deserialize<GenerationCache>(cacheJson);
                        _logger.LogInformation("Resuming content generation from cache");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse cache JSON, starting fresh");
                        cache = null;
                    }
                }
                
                // Initialize or update cache with current settings
                cache = cache ?? new GenerationCache();
                cache.GenerateChapters = generateChapters;
                cache.ChapterCount = chapterCount;
                cache.UserInstructions = userInstructions;

                await Clients.Caller.SendAsync("GenerateStatus", cache != null ? "Resuming content generation..." : "Starting content generation...");
                await Clients.Caller.SendAsync("GenerateProgress", 0);

                if (string.IsNullOrWhiteSpace(url) || !IsYouTubeUrl(url))
                {
                    await Clients.Caller.SendAsync("GenerateError", "Invalid YouTube URL");
                    return;
                }

                var videoUrl = RemoveYouTubeListParameter(url);
                var totalSteps = 0;
                var currentStep = 0;

                // Calculate initial steps (we'll add chapter count later when we know it)
                if (includeVideo) totalSteps++;
                if (includeTitle || includeDescription) totalSteps++;
                if (includeTranscriptResearch) totalSteps += 2; // 1 for downloading transcript, 1 for analyzing/generating TOC
                if (includeCommentsResearch) totalSteps += 2; // 1 for downloading comments, 1 for analyzing/generating TOC

                // Skip video and title/description if we're resuming from cache with completed work
                bool skipInitialModules = cache?.CompletedChapters?.Any() == true;
                
                await Clients.Caller.SendAsync("GenerateStatus", "Getting YouTube metadata...");
                var metadata = await GetYouTubeMetadata(videoUrl, appUserId);

                if (string.IsNullOrEmpty(metadata.Title))
                {
                    await Clients.Caller.SendAsync("GenerateStatus", "Failed to retrieve YouTube metadata");
                    return;
                }

                if (includeVideo && !skipInitialModules)
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Generating video module...");

                    var videoModule = new VideoPlayer
                    {
                        Id = GenerateRandomId(),
                        Url = videoUrl,
                        AutoTryAgain = true,
                        Title = metadata.Title
                    };

                    await Clients.Caller.SendAsync("ModuleGenerated", videoModule);
                    await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                }
                else if (includeVideo && skipInitialModules)
                {
                    currentStep++;
                    _logger.LogInformation("Skipping video module generation (resuming from cache)");
                }

                if ((includeTitle || includeDescription) && !skipInitialModules)
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Generating title and description...");

                    var encodedTitle = WebUtility.HtmlEncode(metadata.Title ?? string.Empty);
                    var descriptionText = metadata.Description ?? string.Empty;
                    var normalized = descriptionText.Replace("\r\n", "\n").Replace('\r', '\n');
                    var rawParagraphs = Regex.Split(normalized, "\n{2,}");

                    var paragraphHtml = string.Join(string.Empty,
                        rawParagraphs
                            .Select(p => p.Trim('\n', '\r', ' '))
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Select(p =>
                            {
                                var lines = p.Split('\n');
                                var encodedLines = lines.Select(line => WebUtility.HtmlEncode(line));
                                var innerHtml = string.Join("<br/>", encodedLines);
                                return $"<p>{innerHtml}</p>";
                            }));

                    var html = includeTitle && includeDescription
                        ? $"<h4>{encodedTitle}</h4>{paragraphHtml}"
                        : includeTitle
                            ? $"<h4>{encodedTitle}</h4>"
                            : paragraphHtml;

                    var textEditorModule = new
                    {
                        id = GenerateRandomId(),
                        type = "text-editor",
                        html,
                        manuallyAdded = false
                    };

                    await Clients.Caller.SendAsync("ModuleGenerated", textEditorModule);
                    await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                }
                else if ((includeTitle || includeDescription) && skipInitialModules)
                {
                    currentStep++;
                    _logger.LogInformation("Skipping title/description generation (resuming from cache)");
                }

                // Prepare data for research (transcript and/or comments)
                string transcriptText = cache?.TranscriptText;
                List<YouTubeComment> commentsList = cache?.CommentsList;

                if (includeTranscriptResearch && string.IsNullOrEmpty(transcriptText))
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Downloading transcript...");

                    transcriptText = await GetYouTubeTranscript(videoUrl, appUserId);
                    if (!string.IsNullOrEmpty(transcriptText))
                    {
                        // Save transcript to cache
                        cache = cache ?? new GenerationCache();
                        cache.TranscriptText = transcriptText;
                        await Clients.Caller.SendAsync("CacheUpdate", JsonSerializer.Serialize(cache));
                        await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                    }
                    else
                    {
                        // Transcript not available - show warning but continue if comments are enabled
                        if (includeCommentsResearch)
                        {
                            await Clients.Caller.SendAsync("GenerateStatus", "Transcript not available. Continuing with comments-only research...");
                            await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                            includeTranscriptResearch = false; // Disable transcript research for the rest of the process
                        }
                        else
                        {
                            await Clients.Caller.SendAsync("GenerateError", "Failed to download transcript. The video may not have captions available.");
                            return;
                        }
                    }
                }
                else if (includeTranscriptResearch && !string.IsNullOrEmpty(transcriptText))
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Using cached transcript...");
                    await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                }

                if (includeCommentsResearch && commentsList == null)
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Downloading comments...");

                    commentsList = await GetYouTubeComments(videoUrl, appUserId, 250);

                    if (commentsList == null)
                    {
                        // Failed to retrieve comments - send error with retry option
                        await Clients.Caller.SendAsync("CommentsRetrievalFailed", "Failed to retrieve YouTube comments. Please try again.");
                        return;
                    }
                    else if (commentsList.Any())
                    {
                        // Save comments to cache
                        cache = cache ?? new GenerationCache();
                        cache.CommentsList = commentsList;
                        await Clients.Caller.SendAsync("CacheUpdate", JsonSerializer.Serialize(cache));
                        await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                    }
                    else
                    {
                        // No comments available - disable comments research and continue
                        _logger.LogInformation("No comments available for URL {Url}, continuing without comments", videoUrl);
                        await Clients.Caller.SendAsync("GenerateStatus", "No comments available. Continuing with transcript-only research...");
                        includeCommentsResearch = false;
                        await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                    }
                }
                else if (includeCommentsResearch && commentsList != null && commentsList.Any())
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Using cached comments...");
                    await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                }
                else if (includeCommentsResearch && commentsList != null && !commentsList.Any())
                {
                    // Cached empty list - disable comments research and continue
                    currentStep++;
                    includeCommentsResearch = false;
                    await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                }

                // Perform research analysis if either transcript or comments are available
                YouTubeCommentsAnalysis analysis = cache?.Analysis;
                
                _logger.LogInformation("Analysis check: hasAnalysis={HasAnalysis}, includeTranscript={IncludeTranscript}, hasTranscript={HasTranscript}, includeComments={IncludeComments}, hasComments={HasComments}", 
                    analysis != null, includeTranscriptResearch, !string.IsNullOrEmpty(transcriptText), includeCommentsResearch, commentsList != null && commentsList.Any());
                // Generate chapters if we have transcript or comments research AND generateChapters is true
                if (generateChapters && ((includeTranscriptResearch && !string.IsNullOrEmpty(transcriptText)) ||
                    (includeCommentsResearch && commentsList != null && commentsList.Any())) && analysis == null)
                {
                    currentStep++;

                    // Determine what we're analyzing
                    string analysisSource = "";
                    if (includeTranscriptResearch && includeCommentsResearch)
                    {
                        analysisSource = "transcript and comments";
                        await Clients.Caller.SendAsync("GenerateStatus", $"Analyzing transcript and comments and organizing by topic...");
                    }
                    else if (includeTranscriptResearch)
                    {
                        analysisSource = "transcript";
                        await Clients.Caller.SendAsync("GenerateStatus", $"Analyzing transcript and organizing by topic...");
                    }
                    else
                    {
                        analysisSource = "comments";
                        await Clients.Caller.SendAsync("GenerateStatus", $"Analyzing comments and organizing by topic...");
                    }

                    // Prepare content for analysis
                    string contentForAnalysis = "";

                    if (includeTranscriptResearch && !string.IsNullOrEmpty(transcriptText))
                    {
                        contentForAnalysis += $"TRANSCRIPT:\n{transcriptText}\n\n";
                    }

                    if (includeCommentsResearch && commentsList != null && commentsList.Any())
                    {
                        // Create indexed comments for AI analysis
                        var indexedComments = commentsList.Select((c, index) => new { Index = index, Text = c.Text }).ToList();
                        var commentsForAnalysis = indexedComments.Select(c => $"[{c.Index}] {c.Text}").ToList();
                        var commentsText = string.Join("\n\n", commentsForAnalysis);
                        contentForAnalysis += $"COMMENTS:\n{commentsText}";
                    }

                    // Generate structured analysis with chapters
                    var chapterCountInstruction = chapterCount != "any" && int.TryParse(chapterCount, out int requestedCount)
                        ? $"Create EXACTLY {requestedCount} chapters. "
                        : "Create as many chapters as needed to cover all distinct topics. ";
                    
                    var analysisPrompt = $"Analyze the following YouTube {analysisSource} and organize them into chapters. Be COMPREHENSIVE and SPECIFIC - create chapters for ALL topics being discussed, including:\n" +
                            $"- Broad themes and concepts\n- Specific people mentioned (by name or role)\n- Specific events, actions, or things people did\n- Specific products, tools, or technologies discussed\n" +
                            $"- Specific opinions, criticisms, or praise\n- Any other distinct subjects of discussion\n\n" +
                            $"IMPORTANT EXCLUSIONS - Do NOT create chapters about:\n" +
                            $"- Video promotion or marketing\n" +
                            $"- Timestamps or video navigation\n" +
                            $"- Criticisms about Video editing quality, production, or content delivery\n" +
                            $"- Advertisements in the video\n" +
                            $"- Criticisms about the behavior of people in the video\n" +
                            $"- Meta-commentary about the video itself\n\n" +
                            $"Focus ONLY on the substantive content topics being discussed.\n\n" +
                            (!string.IsNullOrWhiteSpace(userInstructions) ? $"USER INSTRUCTIONS:\n{userInstructions}\n\n" : "") +
                            $"TRANSLATIONS: If any comments are not in English, translate them to English and include them in the Translations array with the comment index and translation.\n\n" +
                            $"For each chapter, provide a descriptive title" +
                            (includeCommentsResearch && commentsList != null && commentsList.Any() ? " and list the INDEXES (numbers in brackets) of relevant comments" : "") + ". " +
                            chapterCountInstruction +
                            $"IMPORTANT: Do NOT use Unicode escape sequences like \\u0027. Use plain ASCII characters only. Use regular apostrophes and quotes.\n" +
                            $"Return ONLY valid JSON in this exact format:\n" +
                            $"{{\n  \"Chapters\": [\n    {{\n      \"Title\": \"Chapter title here\"" +
                            (includeCommentsResearch && commentsList != null && commentsList.Any() ? ",\n      \"CommentIndexes\": [0, 5, 12]" : "") +
                            $"\n    }}\n  ]" +
                            (includeCommentsResearch && commentsList != null && commentsList.Any() ? ",\n  \"Translations\": [\n    {{\n      \"Index\": 3,\n      \"Translation\": \"English translation here\"\n    }}\n  ]" : "") +
                            $"\n}}\n\n" +
                            $"Content:\n{contentForAnalysis}";

                    var analysisJson = await Common.LLMs.Prompt(
                        "You are a research assistant that analyzes YouTube transcripts and/or comments and organizes them into structured chapters. Be thorough and specific - " +
                        "identify ALL distinct topics, people, events, and things being discussed. Return only valid JSON with plain ASCII characters (no Unicode escape sequences). Return only the JSON, no additional text.",
                        "I will comprehensively analyze the content and return a valid JSON object with chapters for every distinct topic, person, event, or thing being discussed. I will use only plain ASCII characters and avoid Unicode escape sequences.",
                        analysisPrompt
                    );

                    // Parse the JSON response
                    try
                    {
                        analysis = JsonSerializer.Deserialize<YouTubeCommentsAnalysis>(analysisJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse chapter analysis JSON: {Json}", analysisJson);
                        await Clients.Caller.SendAsync("GenerateError", "Failed to parse AI response. Please try again.\n" + ex.Message + "\n\n" + analysisJson);
                        return;
                    }

                    if (analysis?.Chapters == null || !analysis.Chapters.Any())
                    {
                        await Clients.Caller.SendAsync("GenerateError", "No chapters were generated from the comments.");
                        return;
                    }
                    
                    // Save analysis to cache
                    cache = cache ?? new GenerationCache();
                    cache.Analysis = analysis;
                    await Clients.Caller.SendAsync("CacheUpdate", JsonSerializer.Serialize(cache));
                }
                else if (analysis != null)
                {
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", $"Using cached analysis...");
                }

                if (analysis != null)
                {

                    // Apply translations to comments
                    if (analysis.Translations != null && analysis.Translations.Any())
                    {
                        foreach (var translation in analysis.Translations)
                        {
                            if (translation.Index >= 0 && translation.Index < commentsList.Count)
                            {
                                var originalComment = commentsList[translation.Index];
                                commentsList[translation.Index] = new YouTubeComment
                                {
                                    Author = originalComment.Author,
                                    Text = translation.Translation + "\n\n(translated to English)"
                                };
                            }
                        }
                    }

                    // Update total steps now that we know the chapter count
                    // Add 1 step for each chapter + 1 step for the comments display module (if comments included)
                    totalSteps += analysis.Chapters.Count;
                    if (includeCommentsResearch && commentsList != null && commentsList.Any())
                    {
                        totalSteps += 1; // Add step for comments display module
                    }

                    // Create table of contents module with anchor links (skip if already generated)
                    if (cache?.TableOfContentsGenerated != true)
                    {
                        var tocItems = analysis.Chapters.Select((c, i) =>
                        {
                            var chapterId = $"chapter-{i + 1}";
                            var encodedTitle = WebUtility.HtmlEncode(c.Title);
                            return $"<a href='#{chapterId}'>{i + 1}. {encodedTitle}</a>";
                        }).ToList();

                        // Only add comments link if comments were included
                        if (includeCommentsResearch && commentsList != null && commentsList.Any())
                        {
                            tocItems.Add($"<a href='#youtube-comments'>{analysis.Chapters.Count + 1}. Youtube Comments</a>");
                        }
                        var tocHtml = $"<h3 id=\"table-of-contents\">Table of Contents</h3><p>{string.Join("<br/>", tocItems)}</p>";
                        var tocModule = new
                        {
                            id = GenerateRandomId(),
                            type = "text-editor",
                            html = tocHtml,
                            manuallyAdded = false
                        };
                        await Clients.Caller.SendAsync("ModuleGenerated", tocModule);
                        
                        // Mark TOC as generated in cache
                        cache = cache ?? new GenerationCache();
                        cache.TableOfContentsGenerated = true;
                        await Clients.Caller.SendAsync("CacheUpdate", JsonSerializer.Serialize(cache));
                    }
                    await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);

                    // Generate content for each chapter using transcript and/or comments
                    var chapterCounts = analysis.Chapters.Count;
                    
                    // Build list of all chapter titles for context
                    var allChapterTitles = string.Join("\n", analysis.Chapters.Select((c, idx) => $"{idx + 1}. {c.Title}"));
                    
                    for (int i = 0; i < chapterCounts; i++)
                    {
                        // Skip if this chapter was already completed
                        if (cache?.CompletedChapters?.Contains(i) == true)
                        {
                            currentStep++;
                            await Clients.Caller.SendAsync("GenerateStatus", $"Skipping completed chapter {i + 1}/{chapterCounts}: {analysis.Chapters[i].Title}...");
                            await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                            continue;
                        }
                        
                        currentStep++;
                        var chapter = analysis.Chapters[i];
                        await Clients.Caller.SendAsync("GenerateStatus", $"Generating chapter {i + 1}/{chapterCounts}: {chapter.Title}...");

                        // Build source content for chapter
                        string chapterSourceContent = "";

                        if (includeTranscriptResearch && !string.IsNullOrEmpty(transcriptText))
                        {
                            chapterSourceContent += $"TRANSCRIPT:\n{transcriptText}\n\n";
                        }

                        if (includeCommentsResearch && commentsList != null && commentsList.Any())
                        {
                            // Get relevant comments using the indexes
                            var relevantComments = (chapter.CommentIndexes ?? new List<int>())
                                .Where(idx => idx >= 0 && idx < commentsList.Count)
                                .Select(idx => commentsList[idx].Text)
                                .ToList();

                            if (relevantComments.Any())
                            {
                                var relevantCommentsText = string.Join("\n\n", relevantComments);
                                chapterSourceContent += $"RELEVANT COMMENTS:\n{relevantCommentsText}";
                            }
                        }

                        var chapterId = $"chapter-{i + 1}";
                        var sourceDescription = includeTranscriptResearch && includeCommentsResearch ? "transcript and comments" :
                                               includeTranscriptResearch ? "transcript" : "comments";
                        
                        // Build context about previous chapters
                        var chapterContext = i > 0 
                            ? $"\n\nIMPORTANT: This is Chapter {i + 1} of {chapterCounts}. Previous chapters have already covered other topics, so avoid repeating information that would have been explained in earlier chapters. Focus specifically on '{chapter.Title}' without re-explaining concepts from previous chapters."
                            : $"\n\nThis is Chapter 1 of {chapterCounts}.";
                        
                        var chapterPrompt = $"You are writing Chapter {i + 1} of a {chapterCounts}-chapter research document.\n\n" +
                            $"ALL CHAPTERS:\n{allChapterTitles}\n\n" +
                            $"CURRENT CHAPTER: Chapter {i + 1} - {chapter.Title}{chapterContext}\n\n" +
                            (!string.IsNullOrWhiteSpace(userInstructions) ? $"USER INSTRUCTIONS:\n{userInstructions}\n\n" : "") +
                            $"Write a comprehensive, well-researched chapter about '{chapter.Title}' based on the following {sourceDescription}. " +
                            $"Include multiple paragraphs with detailed analysis. Use proper HTML formatting with <p> tags for paragraphs. " +
                            $"Do not include a title at the top, only include the paragraphs of text.\n\n{chapterSourceContent}";
                        try
                        {

                            var chapterContent = await Common.LLMs.Prompt(
                                "You are a research writer creating detailed, informative content based on community discussions.",
                                "I will write a thorough, well-structured chapter with multiple paragraphs using proper HTML formatting.",
                                chapterPrompt
                            );

                            var chapterModule = new
                            {
                                id = GenerateRandomId(),
                                type = "text-editor",
                                html = $"<h4 id=\"{chapterId}\">{chapter.Title}</h4>\n" +
                                        $"<a href='#table-of-contents'>Back to Top</a>\n\n" +
                                       chapterContent,
                                manuallyAdded = false
                            };
                            await Clients.Caller.SendAsync("ModuleGenerated", chapterModule);

                            // Mark chapter as completed in cache
                            cache = cache ?? new GenerationCache();
                            cache.CompletedChapters = cache.CompletedChapters ?? new List<int>();
                            if (!cache.CompletedChapters.Contains(i))
                            {
                                cache.CompletedChapters.Add(i);
                                await Clients.Caller.SendAsync("CacheUpdate", JsonSerializer.Serialize(cache));
                            }

                            await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to retrieve chapter content");
                            await Clients.Caller.SendAsync("GenerateError", "Failed to retrieve chapter content. Please try again.\n" + ex.Message);
                            return;
                        }
                    }

                    // Generate comments display module only if comments were included
                    if (includeCommentsResearch && commentsList != null && commentsList.Any())
                    {
                        var commentsHtml = "<h3 id=\"youtube-comments\">YouTube Comments</h3>";
                        foreach (var comment in commentsList)
                        {
                            var encodedAuthor = WebUtility.HtmlEncode(comment.Author);
                            var encodedText = WebUtility.HtmlEncode(comment.Text).Replace("\n", "<br/>");
                            commentsHtml += $"<p><strong>{encodedAuthor}:</strong><br/>{encodedText}</p>";
                        }

                        var commentsModule = new
                        {
                            id = GenerateRandomId(),
                            type = "text-editor",
                            html = commentsHtml,
                            manuallyAdded = false
                        };
                        await Clients.Caller.SendAsync("ModuleGenerated", commentsModule);

                        currentStep++;
                        await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                    }
                }
                else if (!generateChapters && ((includeTranscriptResearch && !string.IsNullOrEmpty(transcriptText)) ||
                    (includeCommentsResearch && commentsList != null && commentsList.Any())))
                {
                    // Generate single text module without chapters
                    currentStep++;
                    await Clients.Caller.SendAsync("GenerateStatus", "Generating content...");
                    
                    // Prepare content for generation
                    string sourceContent = "";
                    string sourceDescription = "";
                    
                    if (includeTranscriptResearch && !string.IsNullOrEmpty(transcriptText))
                    {
                        sourceContent += $"TRANSCRIPT:\n{transcriptText}\n\n";
                        sourceDescription = "transcript";
                    }
                    
                    if (includeCommentsResearch && commentsList != null && commentsList.Any())
                    {
                        var commentsText = string.Join("\n\n", commentsList.Select(c => $"{c.Author}: {c.Text}"));
                        sourceContent += $"COMMENTS:\n{commentsText}";
                        sourceDescription = includeTranscriptResearch ? "transcript and comments" : "comments";
                    }
                    
                    var contentPrompt = $"Write comprehensive, well-researched content based on the following YouTube {sourceDescription}. " +
                        $"Include multiple paragraphs with detailed analysis. Use proper HTML formatting with <p> tags for paragraphs.\n\n" +
                        (!string.IsNullOrWhiteSpace(userInstructions) ? $"USER INSTRUCTIONS:\n{userInstructions}\n\n" : "") +
                        $"Content:\n{sourceContent}";
                    
                    try
                    {
                        var content = await Common.LLMs.Prompt(
                            "You are a research writer creating detailed, informative content based on YouTube content.",
                            "I will write thorough, well-structured content with multiple paragraphs using proper HTML formatting.",
                            contentPrompt
                        );
                        
                        var contentModule = new
                        {
                            id = GenerateRandomId(),
                            type = "text-editor",
                            html = content,
                            manuallyAdded = false
                        };
                        await Clients.Caller.SendAsync("ModuleGenerated", contentModule);
                        await Clients.Caller.SendAsync("GenerateProgress", (currentStep * 100) / totalSteps);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate content");
                        await Clients.Caller.SendAsync("GenerateError", "Failed to generate content. Please try again.\n" + ex.Message);
                        return;
                    }
                }


                // Clear cache on successful completion
                await Clients.Caller.SendAsync("CacheUpdate", "null");
                
                await Clients.Caller.SendAsync("GenerateStatus", "Content generation complete!");
                await Clients.Caller.SendAsync("GenerateProgress", 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating content: {url}");
                await Clients.Caller.SendAsync("GenerateError", ex.Message);
            }
        }

        private async Task<List<YouTubeComment>> GetYouTubeComments(string url, string appUserId, int maxComments)
        {
            try
            {
                var tempBaseName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var expectedJsonPath = tempBaseName + ".info.json";
                var cookiesArg = GetCookiesArgument(appUserId);
                var arguments = $"{cookiesArg}--js-runtimes node --skip-download --write-info-json --write-comments --extractor-args \"youtube:max_comments={maxComments}\" -o \"{tempBaseName}\" \"{url}\"";

                _logger.LogInformation("GetYouTubeComments yt-dlp arguments: {Arguments}", arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Ensure PATH is inherited so yt-dlp can find Node.js for n challenge solving
                startInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("Failed to start yt-dlp process when getting comments for URL {Url}", url);
                        return null; // Return null to indicate failure
                    }

                    var stderrTask = process.StandardError.ReadToEndAsync();
                    var stdoutTask = process.StandardOutput.ReadToEndAsync();

                    await Task.WhenAll(stderrTask, stdoutTask, process.WaitForExitAsync());

                    var stderr = await stderrTask;
                    var stdout = await stdoutTask;
                    
                    _logger.LogInformation("GetYouTubeComments yt-dlp exit code: {ExitCode}", process.ExitCode);
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        _logger.LogInformation("GetYouTubeComments yt-dlp stderr: {Stderr}", stderr);
                    }
                    
                    await CheckYtDlpVersionWarning(stderr);

                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("yt-dlp exited with code {Code} for URL {Url}. Error: {Error}", process.ExitCode, url, stderr);
                        return null; // Return null to indicate failure
                    }
                    
                    if (!File.Exists(expectedJsonPath))
                    {
                        _logger.LogWarning("yt-dlp did not create expected JSON file at {Path}", expectedJsonPath);
                        return null; // Return null to indicate failure
                    }

                    try
                    {
                        var json = await File.ReadAllTextAsync(expectedJsonPath, Encoding.UTF8);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            return new List<YouTubeComment>();
                        }

                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (!root.TryGetProperty("comments", out var commentsArray) || commentsArray.ValueKind != JsonValueKind.Array)
                        {
                            return new List<YouTubeComment>();
                        }

                        var comments = new List<YouTubeComment>();
                        var count = 0;

                        foreach (var comment in commentsArray.EnumerateArray())
                        {
                            if (count >= maxComments) break;

                            if (comment.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                            {
                                var text = textProp.GetString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    if (text.Split(' ').Length < 50) continue;
                                    if (text.Split(':').Length > 10) continue; //comment is showing all timestamps in video

                                    var author = "Unknown";
                                    if (comment.TryGetProperty("author", out var authorProp) && authorProp.ValueKind == JsonValueKind.String)
                                    {
                                        author = authorProp.GetString() ?? "Unknown";
                                    }

                                    comments.Add(new YouTubeComment
                                    {
                                        Text = text,
                                        Author = author
                                    });
                                    count++;
                                }
                            }
                        }

                        return comments;
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(expectedJsonPath))
                            {
                                File.Delete(expectedJsonPath);
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to delete yt-dlp comments file {Path}", expectedJsonPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube comments for URL {Url}", url);
                return null; // Return null to indicate failure
            }
        }
    }

    public class GenerateContentRequest
    {
        public string Url { get; set; }
        public bool IncludeVideo { get; set; }
        public bool IncludeTitle { get; set; }
        public bool IncludeDescription { get; set; }
        public bool IncludeCommentsResearch { get; set; }
    }

    public class GenerationCache
    {
        public string TranscriptText { get; set; }
        public List<YouTubeComment> CommentsList { get; set; }
        public YouTubeCommentsAnalysis Analysis { get; set; }
        public bool TableOfContentsGenerated { get; set; }
        public List<int> CompletedChapters { get; set; } = new List<int>();
        public bool GenerateChapters { get; set; } = true;
        public string ChapterCount { get; set; } = "any";
        public string UserInstructions { get; set; }
    }

    public class YouTubeCommentsAnalysis
    {
        public List<ChapterInfo> Chapters { get; set; }
        public List<CommentTranslation> Translations { get; set; }
    }

    public class ChapterInfo
    {
        public string Title { get; set; }
        public List<int> CommentIndexes { get; set; }
    }

    public class CommentTranslation
    {
        public int Index { get; set; }
        public string Translation { get; set; }
    }

    public class YouTubeComment
    {
        public string Text { get; set; }
        public string Author { get; set; }
    }
}
