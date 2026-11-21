using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Collector.Chat.Helpers;

/// <summary>
/// The incremental module writer — turns research content items into journal
/// modules. Every content item carries a pre-assigned moduleId; the writer
/// materializes any item whose module doesn't exist yet and leaves existing
/// modules untouched (user edits preserved). Content items map 1:1:
///
///   title         → text-editor "&lt;h4&gt;text&lt;/h4&gt;"
///   paragraph     → text-editor "&lt;p&gt;text&lt;/p&gt;"
///   bullet_list   → text-editor "&lt;ul&gt;&lt;li&gt;…"
///   numbered_list → text-editor "&lt;ol&gt;&lt;li&gt;…"
///   table         → text-editor "&lt;table&gt;…"
///   image         → image module {url, caption, alt}
///   image-gallery → image-gallery module {images:[{url,caption}]}
///   sub_topics    → tabs module (side menu, style:1); each item → one tab
///                   whose modules render the item's own content recursively
///
/// Comments render nowhere yet (tracked in research only); Sources keeps its
/// own pinned-last text-editor.
/// </summary>
public class ResearchModuleWriter
{
    /// <summary>Render every dirty topic (and any dirty nested subtopics) into
    /// the entry's modules, then pin the Sources module last.</summary>
    public Task WriteDirtyTopicsAsync(
        JournalEntryDocument doc,
        ResearchObject research,
        IEnumerable<ResearchTopic> dirtyTopics,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("journal-writer", "WriteDirtyTopicsAsync");
        var dirty = dirtyTopics?.Select(t => t.Node).ToList()
                    ?? new List<JsonObject>();
        bool IsDirty(ResearchTopic t) => dirty.Any(d => ReferenceEquals(d, t.Node));

        foreach (var topic in research.Topics)
            RenderTopic(doc, doc.Modules, topic, IsDirty(topic), onProgress);

        RemoveOrphanedGalleries(doc.Modules, BoundModuleIds(research));
        EnsureSourcesModule(doc, research);
        return Task.CompletedTask;
    }

    /// <summary>Every moduleId referenced by the research tree — content
    /// items, sub_topics containers, plus the sources/comments anchors.</summary>
    private static HashSet<string> BoundModuleIds(ResearchObject research)
    {
        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in research.AllContentItems())
        {
            var id = item["moduleId"]?.ToString();
            if (!string.IsNullOrEmpty(id)) bound.Add(id);
        }
        if (!string.IsNullOrEmpty(research.SourcesModuleId)) bound.Add(research.SourcesModuleId);
        if (!string.IsNullOrEmpty(research.CommentsModuleId)) bound.Add(research.CommentsModuleId);
        return bound;
    }

    /// <summary>Gallery content merged away (or deleted) leaves its rendered
    /// module behind — RenderTopic only inserts. Sweep auto-generated
    /// image-gallery modules no content item still binds to. Manually added
    /// galleries have no research binding and are never touched.</summary>
    private static void RemoveOrphanedGalleries(JsonArray container, HashSet<string> bound)
    {
        for (var i = container.Count - 1; i >= 0; i--)
        {
            if (container[i] is not JsonObject m) continue;
            if (m["type"]?.ToString() == "image-gallery" &&
                m["manuallyAdded"]?.GetValue<bool>() != true &&
                !bound.Contains(m["id"]?.ToString() ?? ""))
            {
                container.RemoveAt(i);
                continue;
            }
            if (m["modules"] is JsonArray inner) RemoveOrphanedGalleries(inner, bound);
            if (m["tabs"] is JsonArray tabs)
                foreach (var t in tabs)
                    if (t is JsonObject to && to["modules"] is JsonArray tm)
                        RemoveOrphanedGalleries(tm, bound);
        }
    }

    /// <summary>Walk a topic's content in order; create modules for items that
    /// lack them, tracking the insertion point so a topic's chain stays
    /// contiguous. When the topic itself isn't dirty we still descend into
    /// its sub_topics — a nested item may be the dirty one.</summary>
    private void RenderTopic(
        JournalEntryDocument doc,
        JsonArray container,
        ResearchTopic topic,
        bool topicDirty,
        Action<string>? onProgress)
    {
        // Anchor: the last in-container module already rendered for this
        // topic; new items insert right after it.
        var insertAt = -1;
        for (int i = 0; i < container.Count; i++)
        {
            if (container[i] is JsonObject m &&
                topic.ContentItems().ContainsId(m["id"]?.ToString()))
                insertAt = i;
        }

        void Insert(JsonObject module)
        {
            if (insertAt >= 0 && insertAt < container.Count)
            {
                container.Insert(insertAt + 1, module);
                insertAt++;
            }
            else
            {
                container.Add(module);
                insertAt = container.Count - 1;
            }
        }

        foreach (var node in topic.Content)
        {
            if (node is not JsonObject item) continue;
            var type = item["type"]?.ToString() ?? "";
            var moduleId = item["moduleId"]?.ToString();
            var isSubContainer = type == "sub_topics";

            if (!isSubContainer && doc.FindModule(moduleId ?? "") is JsonObject existing)
            {
                // Module exists — advance the anchor if it sits in this container.
                for (int i = 0; i < container.Count; i++)
                    if (container[i] is JsonObject m && m["id"]?.ToString() == moduleId)
                        insertAt = i;
                // Dirty topic: refresh gallery images so merged/pruned items
                // propagate to the already-rendered module.
                if (topicDirty && type == "image-gallery")
                    existing["images"] = NewGallery(moduleId, item)["images"]!.DeepClone();
                continue;
            }

            if (!topicDirty && !isSubContainer) continue; // clean topic: nothing new to add

            switch (type)
            {
                case "title":
                case "paragraph":
                    if (topicDirty) Insert(NewEditor(moduleId, WrapBlock(type, item["text"]?.ToString())));
                    break;
                case "bullet_list":
                case "numbered_list":
                    if (topicDirty) Insert(NewEditor(moduleId, WrapList(type, item["items"] as JsonArray)));
                    break;
                case "table":
                    if (topicDirty) Insert(NewEditor(moduleId, WrapTable(item)));
                    break;
                case "image":
                    if (topicDirty) Insert(NewImage(moduleId, item));
                    break;
                case "image-gallery":
                    if (topicDirty) Insert(NewGallery(moduleId, item));
                    break;
                case "sub_topics":
                {
                    // Ensure the tabs module exists (create if missing), then
                    // render each subtopic item into its tab.
                    var tabs = doc.FindModule(moduleId ?? "");
                    if (tabs == null)
                    {
                        tabs = new JsonObject
                        {
                            ["type"] = "tabs",
                            ["id"] = moduleId ?? ResearchObject.NewModuleId(),
                            ["style"] = 1, // side menu
                            ["manuallyAdded"] = false,
                            ["tabs"] = new JsonArray()
                        };
                        item["moduleId"] = tabs["id"]!.ToString();
                        Insert(tabs);
                    }
                    if (tabs["tabs"] is not JsonArray tabArr)
                        tabs["tabs"] = tabArr = new JsonArray();

                    if (item["items"] is JsonArray subs)
                    {
                        foreach (var sn in subs)
                        {
                            if (sn is not JsonObject sobj) continue;
                            var sub = new ResearchTopic(sobj, isSubtopic: true);
                            if (sub.Archived)
                            {
                                // Archived = no live module. A tab still
                                // carrying this sub's moduleId means the flag
                                // is stale (restored out-of-band) — clear it
                                // rather than spawn a duplicate tab.
                                var bound = sub.ModuleId ?? "";
                                var live = tabArr.OfType<JsonObject>().Any(
                                    t => t["id"]?.ToString() == bound);
                                if (!live) continue;
                                sub.Status = null;
                            }
                            var tab = EnsureTab(tabArr, sub);
                            // Nested render is always "dirty" — creation is
                            // idempotent since existing modules skip early.
                            RenderTopic(doc, (JsonArray)tab["modules"]!, sub,
                                topicDirty: true, onProgress);
                        }
                    }
                    break;
                }
            }
        }
    }

    /// <summary>Find the tab for a subtopic item (by its moduleId == tab id)
    /// or create it. Tab title = item.tab ?? item.title.</summary>
    private static JsonObject EnsureTab(JsonArray tabs, ResearchTopic sub)
    {
        var tabId = sub.ModuleId ?? "";
        foreach (var tn in tabs)
            if (tn is JsonObject t && t["id"]?.ToString() == tabId)
            {
                if (t["modules"] is not JsonArray) t["modules"] = new JsonArray();
                RepairDuplicateTabs(tabs, t);
                return t;
            }
        var tab = new JsonObject
        {
            ["id"] = string.IsNullOrWhiteSpace(tabId) ? ResearchObject.NewModuleId() : tabId,
            ["title"] = sub.Tab ?? sub.Topic,
            ["modules"] = new JsonArray()
        };
        if (string.IsNullOrWhiteSpace(tabId)) sub.ModuleId = tab["id"]!.ToString();
        tabs.Add(tab);
        RepairDuplicateTabs(tabs, tab);
        return tab;
    }

    /// <summary>Earlier builds stripped subtopic moduleIds on load, leaving a
    /// same-title empty tab beside the real one. When the bound tab is empty
    /// and a same-title sibling has the content, move it over and drop the
    /// orphan.</summary>
    private static void RepairDuplicateTabs(JsonArray tabs, JsonObject bound)
    {
        var title = bound["title"]?.ToString();
        if (string.IsNullOrWhiteSpace(title)) return;
        var boundModules = bound["modules"] as JsonArray;
        for (var i = tabs.Count - 1; i >= 0; i--)
        {
            if (tabs[i] is not JsonObject other || ReferenceEquals(other, bound)) continue;
            if (!string.Equals(other["title"]?.ToString(), title, StringComparison.OrdinalIgnoreCase)) continue;
            var otherModules = other["modules"] as JsonArray;
            if ((boundModules?.Count ?? 0) == 0 && (otherModules?.Count ?? 0) > 0 && boundModules != null)
                while (otherModules!.Count > 0) { var m = otherModules[0]; otherModules.RemoveAt(0); boundModules.Add(m); }
            // Drop only genuinely-empty duplicates — a tab that still has
            // modules may be legitimate content we can't safely merge.
            if ((otherModules?.Count ?? 0) == 0) tabs.RemoveAt(i);
        }
    }

    #region Module builders

    private static JsonObject NewEditor(string? id, string html) => new()
    {
        ["type"] = "text-editor",
        ["id"] = string.IsNullOrWhiteSpace(id) ? ResearchObject.NewModuleId() : id,
        ["manuallyAdded"] = false,
        ["html"] = html
    };

    private static string WrapBlock(string type, string? text)
    {
        var t = System.Net.WebUtility.HtmlEncode(text ?? "");
        return type == "title" ? $"<h4>{t}</h4>" : $"<p>{t}</p>";
    }

    private static string WrapList(string type, JsonArray? items)
    {
        var tag = type == "numbered_list" ? "ol" : "ul";
        var lis = string.Join("", (items ?? new JsonArray()).Select(RenderListItem));
        return $"<{tag}>{lis}</{tag}>";
    }

    /// <summary>One list item → HTML. A {text,url} object becomes an anchor
    /// link when the url is non-null; anything else renders its text.</summary>
    private static string RenderListItem(JsonNode? node)
    {
        var text = System.Net.WebUtility.HtmlEncode(ResearchObject.ItemText(node));
        var url = node is JsonObject o
            ? (o["url"]?.ToString() ?? o["href"]?.ToString() ?? o["link"]?.ToString() ?? "")
            : "";
        return string.IsNullOrWhiteSpace(url)
            ? $"<li>{text}</li>"
            : $"<li><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">{text}</a></li>";
    }

    private static string WrapTable(JsonObject item)
    {
        var sb = new System.Text.StringBuilder();
        if (item["caption"]?.ToString() is { Length: > 0 } cap)
            sb.Append($"<p><strong>{System.Net.WebUtility.HtmlEncode(cap)}</strong></p>");
        sb.Append("<table>");
        if (item["headers"] is JsonArray headers && headers.Count > 0)
        {
            sb.Append("<thead><tr>");
            foreach (var h in headers)
                sb.Append($"<th>{System.Net.WebUtility.HtmlEncode(h?.ToString() ?? "")}</th>");
            sb.Append("</tr></thead>");
        }
        sb.Append("<tbody>");
        if (item["rows"] is JsonArray rows)
            foreach (var r in rows)
            {
                sb.Append("<tr>");
                if (r is JsonArray cells)
                    foreach (var c in cells)
                        sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(c?.ToString() ?? "")}</td>");
                sb.Append("</tr>");
            }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static JsonObject NewImage(string? id, JsonObject item) => new()
    {
        ["type"] = "image",
        ["id"] = string.IsNullOrWhiteSpace(id) ? ResearchObject.NewModuleId() : id,
        ["manuallyAdded"] = false,
        ["url"] = item["url"]?.ToString() ?? "",
        ["caption"] = item["caption"]?.ToString() ?? "",
        ["alt"] = item["alt_text"]?.ToString() ?? ""
    };

    private static JsonObject NewGallery(string? id, JsonObject item)
    {
        var images = new JsonArray();
        if (item["items"] is JsonArray arr)
            foreach (var i in arr)
                if (i is JsonObject io && !string.IsNullOrWhiteSpace(io["url"]?.ToString()))
                    images.Add(new JsonObject
                    {
                        ["url"] = io["url"]!.ToString(),
                        ["caption"] = io["caption"]?.ToString() ?? "",
                        ["alt"] = io["alt_text"]?.ToString() ?? ""
                    });
        return new JsonObject
        {
            ["type"] = "image-gallery",
            ["id"] = string.IsNullOrWhiteSpace(id) ? ResearchObject.NewModuleId() : id,
            ["manuallyAdded"] = false,
            ["images"] = images
        };
    }

    #endregion

    #region Sources module

    /// <summary>
    /// Maintain the "Sources" text-editor module as the LAST module of the
    /// entry's main module array — &lt;h4&gt;Sources&lt;/h4&gt; over a bullet
    /// list of source links (anchor text = page title). Rebuilt in place from
    /// research.Sources (plus any extra scraped-page sources) on every save,
    /// so new sources append to the bottom of the existing list. Migrates
    /// entries away from the legacy sources-tab layout.
    /// </summary>
    public void EnsureSourcesModule(
        JournalEntryDocument doc,
        ResearchObject? research,
        IEnumerable<(string Title, string Url)>? extraSources = null)
    {
        ChatDetailLog.Fn("journal-writer", "EnsureSourcesModule");
        // Legacy cleanup: the sources list used to live in a "sources" tab
        // inside the first root tabs module — remove it so it never
        // duplicates the root-level module.
        var tabsModule = doc.FindFirstRootTabsModule();
        if (tabsModule?["tabs"] is JsonArray tabs)
        {
            for (int i = tabs.Count - 1; i >= 0; i--)
            {
                if (tabs[i] is JsonObject to && to["id"]?.ToString() == "sources")
                    doc.RemoveTab(tabsModule, to);
            }
        }

        // Union tracked research sources with any extra scraped pages,
        // preserving order.
        var sources = new List<(string Title, string Url)>();
        if (research != null)
            sources.AddRange(research.Sources.Select(s => (s.Title, s.Url)));
        if (extraSources != null)
            foreach (var s in extraSources)
                if (!string.IsNullOrWhiteSpace(s.Url) &&
                    sources.All(x => !x.Url.Equals(s.Url, StringComparison.OrdinalIgnoreCase)))
                    sources.Add(s);

        // Locate the existing sources module — a root-level text-editor
        // marked with data-research-sources or headed <hN>Sources</hN>.
        JsonObject? sourcesModule = null;
        foreach (var node in doc.Modules)
        {
            if (node is JsonObject mod &&
                mod["type"]?.ToString() == "text-editor" &&
                IsSourcesHtml(mod["html"]?.ToString() ?? ""))
            { sourcesModule = mod; break; }
        }

        if (sources.Count == 0)
        {
            if (sourcesModule != null) doc.Modules.Remove(sourcesModule);
            if (research != null) research.SourcesModuleId = null;
            return;
        }

        var html = "<h4 data-research-sources=\"true\">Sources</h4><ul>" +
            string.Join("", sources.Select(s =>
                $"<li><a href=\"{System.Net.WebUtility.HtmlEncode(s.Url)}\">" +
                $"{System.Net.WebUtility.HtmlEncode(s.Title)}</a></li>")) +
            "</ul>";

        if (sourcesModule == null)
        {
            sourcesModule = new JsonObject
            {
                ["type"] = "text-editor",
                ["id"] = JournalEntryDocument.NewModuleId(),
                ["manuallyAdded"] = false
            };
            doc.Modules.Add(sourcesModule);
        }
        sourcesModule["html"] = html;
        if (research != null) research.SourcesModuleId = sourcesModule["id"]?.ToString();

        // Always the very last module of the main array.
        if (doc.Modules.IndexOf(sourcesModule) != doc.Modules.Count - 1)
        {
            doc.Modules.Remove(sourcesModule);
            doc.Modules.Add(sourcesModule);
        }
    }

    /// <summary>
    /// Move an existing sources module back to the end of the main array —
    /// called after ops/media paths that can append modules below it.
    /// </summary>
    public void PinSourcesLast(JournalEntryDocument doc)
    {
        foreach (var node in doc.Modules)
        {
            if (node is JsonObject mod &&
                mod["type"]?.ToString() == "text-editor" &&
                IsSourcesHtml(mod["html"]?.ToString() ?? ""))
            {
                if (doc.Modules.IndexOf(mod) != doc.Modules.Count - 1)
                {
                    doc.Modules.Remove(mod);
                    doc.Modules.Add(mod);
                }
                return; // only the first sources module matters
            }
        }
    }

    private static bool IsSourcesHtml(string html)
        => html.Contains("data-research-sources")
           || Regex.IsMatch(html, @"^\s*<h[1-6][^>]*>\s*Sources\s*</h[1-6]>",
               RegexOptions.IgnoreCase);

    #endregion

}

internal static class ContentItemLookup
{
    /// <summary>True when any content item (this topic, not nested) carries
    /// this moduleId.</summary>
    public static bool ContainsId(this IEnumerable<JsonObject> items, string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (var item in items)
            if (item["moduleId"]?.ToString() == id) return true;
        return false;
    }

    /// <summary>All direct content items of a topic (typed access for the
    /// writer).</summary>
    public static IEnumerable<JsonObject> ContentItems(this ResearchTopic topic)
    {
        foreach (var n in topic.Content)
            if (n is JsonObject o) yield return o;
    }
}
