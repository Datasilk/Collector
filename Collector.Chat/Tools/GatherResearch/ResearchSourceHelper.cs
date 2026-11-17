using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Collector.Chat.Tools.GatherResearch;

/// <summary>
/// A site-specific research extractor. Each helper decides for itself whether
/// it handles a URL via <see cref="Match"/> (regex, domain check, whatever the
/// site needs) and knows how to pull content from it (YouTube transcripts,
/// generic article pages, …). <see cref="ResearchHelpers.Resolve"/> returns
/// the first helper whose <see cref="Match"/> is true — the fallback helper
/// matches everything and stays last.
/// </summary>
public interface IResearchSourceHelper
{
    /// <summary>Does this helper handle the given URL?</summary>
    bool Match(string url);

    /// <summary>
    /// Extract the page's content. Returns null when extraction fails —
    /// the caller reports the error, helpers shouldn't. appUserId lets
    /// helpers use per-user credentials (e.g. saved YouTube cookies), and
    /// requestCookies(domain, ct) lets them ask the client's Chrome
    /// extension for fresh cookies and retry once when a fetch is blocked.
    /// </summary>
    Task<ResearchPage?> ExtractAsync(
        string url,
        Action<int, string>? onProgress = null,
        CancellationToken cancellationToken = default,
        Guid? appUserId = null,
        Func<string, CancellationToken, Task<string>>? requestCookies = null);
}

/// <summary>
/// Ordered helper registry — first <see cref="IResearchSourceHelper.Match"/>
/// wins; <see cref="WebPageHelper"/> matches everything and must stay last.
/// </summary>
public static class ResearchHelpers
{
    private static readonly List<IResearchSourceHelper> All = new()
    {
        new YouTubeHelper(),
        new WebPageHelper()   // fallback — Match always true, keep LAST
    };

    public static IResearchSourceHelper Resolve(string url)
        => All.First(h => h.Match(url));
}
