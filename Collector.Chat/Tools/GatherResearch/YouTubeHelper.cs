using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Collector.Chat.Tools.GatherResearch;

/// <summary>
/// YouTube extractor — a video page has no scrapable article body, so instead
/// of HTML we pull the caption track with yt-dlp (--write-subs/--write-auto-subs
/// + --write-info-json) and return the transcript as content sections plus the
/// video itself so the entry embeds a player.
/// </summary>
public class YouTubeHelper : IResearchSourceHelper
{
    private static readonly Regex YouTubePattern = new(
        @"^(https?://)?(www\.|m\.|music\.)?(youtube\.com/(watch\?[^ ]*v=|embed/|v/|shorts/)|youtu\.be/)[\w-]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string CookiesFolder =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "cookies");

    public bool Match(string url) => IsYouTubeUrl(url);

    public static bool IsYouTubeUrl(string url)
        => !string.IsNullOrWhiteSpace(url) && YouTubePattern.IsMatch(url);

    /// <summary>Extract the video id from any YouTube URL form — watch?v=,
    /// youtu.be/, embed/, v/, shorts/. Used for cross-form source dedup.</summary>
    public static string? GetVideoId(string url)
    {
        if (!IsYouTubeUrl(url)) return null;
        var m = Regex.Match(url,
            @"(?:youtu\.be/|youtube\.com/(?:embed|v|shorts)/|[?&]v=)([\w-]+)",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    public async Task<ResearchPage?> ExtractAsync(
        string url,
        Action<int, string>? onProgress = null,
        CancellationToken cancellationToken = default,
        Guid? appUserId = null,
        Func<string, CancellationToken, Task<string>>? requestCookies = null)
    {
        var tempBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var infoPath = tempBase + ".info.json";

        try
        {
            onProgress?.Invoke(30, "Fetching video details and captions from YouTube...");

            // English subs first (manual preferred, auto-captions as fallback).
            var subFile = await FetchSubsAsync(url, tempBase, "en.*,en", appUserId, cancellationToken);

            // No English captions — take whatever track exists.
            if (subFile == null)
                subFile = await FetchSubsAsync(url, tempBase, "all", appUserId, cancellationToken);

            // Both passes failed AND no info.json — the fetch itself was
            // blocked (cookie/sign-in wall). Ask the Chrome extension for
            // fresh cookies, save them, retry once — same as the video worker.
            if (subFile == null && !File.Exists(infoPath) &&
                requestCookies != null && appUserId.HasValue)
            {
                onProgress?.Invoke(35, "Refreshing YouTube cookie...");
                var fresh = await requestCookies("youtube.com", cancellationToken);
                if (!string.IsNullOrEmpty(fresh))
                {
                    Directory.CreateDirectory(CookiesFolder);
                    await File.WriteAllTextAsync(
                        Path.Combine(CookiesFolder, $"cookies-youtube-{appUserId.Value}.txt"),
                        fresh, cancellationToken);
                    onProgress?.Invoke(38, "Retrying with fresh YouTube cookie...");
                    subFile = await FetchSubsAsync(url, tempBase, "en.*,en", appUserId, cancellationToken);
                    if (subFile == null)
                        subFile = await FetchSubsAsync(url, tempBase, "all", appUserId, cancellationToken);
                }
            }

            string title = "";
            if (File.Exists(infoPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(infoPath, cancellationToken));
                    var root = doc.RootElement;
                    if (root.TryGetProperty("fulltitle", out var ft) && ft.ValueKind == JsonValueKind.String)
                        title = ft.GetString() ?? "";
                    else if (root.TryGetProperty("title", out var tp) && tp.ValueKind == JsonValueKind.String)
                        title = tp.GetString() ?? "";
                }
                catch { /* title is best-effort */ }
            }

            var transcript = subFile != null
                ? ParseSubtitleFile(await File.ReadAllTextAsync(subFile, cancellationToken))
                : "";

            if (string.IsNullOrWhiteSpace(transcript) && string.IsNullOrWhiteSpace(title))
                return null;

            var page = new ResearchPage
            {
                Url = url,
                Title = title,
                // The subtitle filename carries the track language
                // (tempBase.en.vtt / tempBase.de.vtt…) — that's the source's language.
                Language = subFile != null ? SubtitleLanguage(subFile) : null
            };
            page.Videos.Add(url);
            if (!string.IsNullOrWhiteSpace(transcript))
            {
                // Chunk the transcript into ~1800-char sections so the fact
                // extractor sees paragraphs, not one wall of text.
                var idx = 0;
                foreach (var chunk in ChunkText(transcript, 1800))
                {
                    page.ContentSections.Add(chunk);
                    page.Layout.Add($"content:{idx++}");
                }
            }
            return page;
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(tempBase) + "*"))
                    File.Delete(f);
            }
            catch { /* temp cleanup best-effort */ }
        }
    }

    /// <summary>
    /// Run yt-dlp to write subtitles + info.json into tempBase. Returns the
    /// produced subtitle file path (vtt/srt), preferring exact-language files.
    /// </summary>
    private static async Task<string?> FetchSubsAsync(
        string url, string tempBase, string subLangs, Guid? appUserId,
        CancellationToken cancellationToken)
    {
        var cookiesArg = CookiesArgument(appUserId);
        var args = $"{cookiesArg}--js-runtimes node --skip-download " +
                   $"--write-subs --write-auto-subs --sub-langs \"{subLangs}\" " +
                   $"--sub-format \"vtt/srt/best\" --write-info-json " +
                   $"-o \"{tempBase}\" \"{url}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");

        using var process = Process.Start(startInfo);
        if (process == null) return null;

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(cancellationToken));
        if (process.ExitCode != 0) return null;

        var files = Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(tempBase) + "*")
            .Where(f => f.EndsWith(".vtt", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".srt", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (files.Count == 0) return null;

        // Prefer exact "en" over "en-US"/auto variants over anything else.
        return files.FirstOrDefault(f => Path.GetFileName(f).Contains(".en.")) ?? files[0];
    }

    /// <summary>Extract the track language from a subtitle filename (".de.vtt" → "de").</summary>
    private static string? SubtitleLanguage(string subFile)
    {
        var m = Regex.Match(Path.GetFileName(subFile), @"\.([a-zA-Z]{2,3}(?:-[a-zA-Z]{2,4})?)\.(vtt|srt)$",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>Per-user YouTube cookie file when the client extension saved one.</summary>
    private static string CookiesArgument(Guid? appUserId)
    {
        if (appUserId.HasValue)
        {
            var path = Path.Combine(CookiesFolder, $"cookies-youtube-{appUserId.Value}.txt");
            if (File.Exists(path)) return $"--cookies \"{path}\" ";
        }
        return string.Empty;
    }

    /// <summary>
    /// VTT/SRT → plain text. Timing lines carry "--&gt;"; everything after them
    /// until the blank line is cue text. Tag markup is stripped and consecutive
    /// duplicate lines (auto-caption artifacts) collapse into one.
    /// </summary>
    public static string ParseSubtitleFile(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var lines = raw.Replace("\r", "").Split('\n');
        var cues = new List<string>();
        var inCue = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("-->")) { inCue = true; continue; }
            if (!inCue) continue;
            if (trimmed.Length == 0) { inCue = false; continue; }

            var text = Regex.Replace(trimmed, "<[^>]+>", "").Trim();
            if (text.Length == 0) continue;
            if (cues.Count == 0 || cues[^1] != text) cues.Add(text);
        }

        return string.Join(' ', cues);
    }

    /// <summary>Split on sentence-ish boundaries near the size limit.</summary>
    private static IEnumerable<string> ChunkText(string text, int maxChars)
    {
        var pos = 0;
        while (pos < text.Length)
        {
            var len = Math.Min(maxChars, text.Length - pos);
            if (pos + len < text.Length)
            {
                var cut = text.LastIndexOfAny(new[] { '.', '!', '?' }, pos + len - 1, Math.Min(len, 200));
                if (cut > pos) len = cut - pos + 1;
            }
            yield return text.Substring(pos, len).Trim();
            pos += len;
        }
    }
}
