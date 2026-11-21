using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Collector.Chat.Helpers;

/// <summary>
/// Backfill — build a research object for an entry that pre-dates it.
/// Root text-editors become topics; text-editors inside tabs become
/// subtopics of the nearest preceding root topic (or a synthetic root
/// named after the entry when tabs come first). Module bindings keep the
/// real module ids so later updates land in place. Fact extraction goes
/// through the same LLM path as page merges — supplied as a delegate.
/// </summary>
public static class ResearchBackfill
{
    /// <summary>Cap on per-module fact extraction calls during backfill.</summary>
    private const int MaxExtractionCalls = 20;

    public static async Task<ResearchObject> BuildAsync(
        JournalEntryDocument doc,
        string entryTitle,
        Func<string, Task<List<string>>> extractFacts)
    {
        var research = new ResearchObject();
        ResearchTopic? lastRootTopic = null;
        string? lastTitle = null;
        var extractionCalls = 0;

        // Each extracted fact line becomes a paragraph content item bound to
        // the module it was read from (moduleId reuse keeps the binding real).
        void AddParagraphs(ResearchTopic topic, JsonObject module, IEnumerable<string> facts)
        {
            var moduleId = module["id"]?.ToString();
            var first = true;
            foreach (var f in facts)
            {
                var p = research.ContentItem("paragraph");
                if (first && !string.IsNullOrWhiteSpace(moduleId))
                {
                    p["moduleId"] = moduleId;
                    first = false;
                }
                p["text"] = f;
                topic.Content.Add(p);
            }
        }

        foreach (var node in doc.Modules)
        {
            if (node is not JsonObject module) continue;
            var type = module["type"]?.ToString() ?? "";

            if (type == "title")
            {
                lastTitle = module["text"]?.ToString();
                continue;
            }

            if (type == "text-editor")
            {
                var text = StripHtml(module["html"]?.ToString() ?? "");
                var fallbackName = ResearchObject.FirstWords(text, 6).Trim();
                var topic = research.GetOrCreateTopic(
                    !string.IsNullOrWhiteSpace(lastTitle)
                        ? lastTitle!
                        : (fallbackName.Length > 0 ? fallbackName : entryTitle));
                lastRootTopic = topic;
                if (extractionCalls < MaxExtractionCalls && text.Length > 0)
                {
                    extractionCalls++;
                    AddParagraphs(topic, module, await extractFacts(text));
                }
                continue;
            }

            if (type == "tabs" && module["tabs"] is JsonArray tabs)
            {
                var tabsId = module["id"]?.ToString();
                foreach (var tabNode in tabs)
                {
                    if (tabNode is not JsonObject tab) continue;
                    var editors = (tab["modules"] as JsonArray ?? new JsonArray())
                        .OfType<JsonObject>()
                        .Where(m => (m["type"]?.ToString() ?? "") == "text-editor")
                        .ToList();
                    if (editors.Count == 0) continue;

                    // Tabs with no preceding root topic hang off a synthetic
                    // root named after the entry.
                    if (lastRootTopic == null)
                    {
                        lastRootTopic = research.GetOrCreateTopic(entryTitle);
                        lastRootTopic.Synthetic = true;
                    }

                    var tabTitle = tab["title"]?.ToString() ?? "Section";
                    var combined = string.Join("\n\n",
                        editors.Select(e => StripHtml(e["html"]?.ToString() ?? ""))
                               .Where(t => t.Length > 0));

                    var sub = research.GetOrCreateSubTopic(lastRootTopic, tabTitle,
                        ResearchObject.CleanTabLabel(tabTitle));
                    if (tab["id"]?.ToString() is { Length: > 0 } tabId)
                        sub.ModuleId = tabId;
                    if (!string.IsNullOrWhiteSpace(tabsId))
                    {
                        foreach (var c in lastRootTopic.Content)
                            if (c is JsonObject ci && ci["type"]?.ToString() == "sub_topics")
                                ci["moduleId"] = tabsId;
                    }
                    if (extractionCalls < MaxExtractionCalls && combined.Length > 0)
                    {
                        extractionCalls++;
                        AddParagraphs(sub, editors[0], await extractFacts(combined));
                    }
                }
            }
            // Non-text modules get no topic — they're presentation.
        }

        return research;
    }

    private static string StripHtml(string html)
        => Regex.Replace(html ?? "", "<[^>]+>", " ").Trim();
}
