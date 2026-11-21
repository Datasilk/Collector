using System;
using System.Collections.Generic;
using System.Linq;

namespace Collector.Chat.Helpers;

/// <summary>
/// Per-session context-budget math: token estimation, compaction threshold,
/// and the rebuilt-context shape (summary + verbatim tail + stable state).
/// Pure/static so it is covered by the golden regression suite.
/// </summary>
public static class ContextBudget
{
    /// <summary>Fraction of the window at which auto-compaction fires (~Claude Code's effectiveWindow - 13K on typical sizes).</summary>
    public const double CompactThreshold = 0.82;

    /// <summary>Fallback window when no context size is configured/known.</summary>
    public const int DefaultBudget = 8192;

    /// <summary>Token overhead assumed for system prompt + tool definitions on each call.</summary>
    public const int PromptOverheadTokens = 900;

    /// <summary>Cheap heuristic: ~4 chars per token for prose + small per-message overhead.</summary>
    public static int EstimateTokens(string? text) =>
        string.IsNullOrEmpty(text) ? 0 : (text.Length / 4) + 4;

    /// <summary>True when the assembled context has crossed the compaction threshold.</summary>
    public static bool ShouldCompact(int usedTokens, int budgetTokens) =>
        usedTokens >= (int)(budgetTokens * CompactThreshold);

    /// <summary>
    /// Split history into the tail that stays verbatim vs the head that gets
    /// compacted. Keeps at least <paramref name="keepTail"/> verbatim; returns
    /// false when there's nothing worth compacting (tail already ≈ everything).
    /// </summary>
    public static bool TryGetCompactionSplit<T>(
        IReadOnlyList<T> messages, int keepTail,
        out IReadOnlyList<T> toCompact, out IReadOnlyList<T> tail)
    {
        if (messages.Count <= keepTail + 2)
        {
            toCompact = Array.Empty<T>();
            tail = messages;
            return false;
        }
        var cut = messages.Count - keepTail;
        toCompact = messages.Take(cut).ToList();
        tail = messages.Skip(cut).ToList();
        return true;
    }

    /// <summary>
    /// Compose the rebuilt session context: prior summary (now folded with the
    /// just-compacted head), then the verbatim tail as labeled turns.
    /// </summary>
    public static string ComposeContext(string? summary, IEnumerable<string> verbatimTurns)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(summary))
            parts.Add("Earlier in this session (summary):\n" + summary.Trim());
        var turns = verbatimTurns.ToList();
        if (turns.Count > 0)
            parts.Add("Recent conversation in this session:\n" + string.Join("\n", turns));
        return string.Join("\n\n", parts);
    }
}
