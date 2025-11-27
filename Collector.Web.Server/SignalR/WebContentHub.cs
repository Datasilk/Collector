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

        public async Task<object> ScrapeUrl(string url, int journalId)
        {
            try
            {
                _logger.LogInformation($"ScrapeUrl called: url={url}, journalId={journalId}");

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
                    var metadata = await GetYouTubeMetadata(videoUrl);
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

        private async Task<(string Title, string Description)> GetYouTubeMetadata(string url)
        {
            try
            {
                // Use yt-dlp to write a JSON metadata file without downloading the video.
                // We'll create a random temp base path and let yt-dlp append .info.json.
                var tempBaseName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var expectedJsonPath = tempBaseName + ".info.json";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--skip-download --write-info-json -o \"{tempBaseName}\" \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

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

                    if (process.ExitCode != 0)
                    {
                        var err = await stderrTask;
                        _logger.LogWarning("yt-dlp exited with code {Code} for URL {Url}. Error: {Error}", process.ExitCode, url, err);
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
    }
}
