using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Collector.Chat;

/// <summary>
/// The incremental module writer — turns dirty research topics into modules.
/// Code owns ALL structure (root title+text-editor pairs, side-menu tabs
/// containers, nested right-aligned tabs, the Sources tab); the remote LLM
/// only produces the HTML inside each bound text-editor, and only for
/// topics whose fact set changed.
/// </summary>
public class ResearchModuleWriter
{
    private readonly LLMClient _llm;

    public ResearchModuleWriter(LLMClient llm)
    {
        _llm = llm;
    }

    /// <summary>
    /// Write every dirty, non-archived topic — one LLM call each — then
    /// enforce the empty-topic rule and refresh the Sources tab.
    /// </summary>
    public async Task WriteDirtyTopicsAsync(
        JournalEntryDocument doc,
        ResearchObject research,
        IEnumerable<ResearchTopic> dirtyTopics,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var dirty = new HashSet<ResearchTopic>(dirtyTopics ?? Enumerable.Empty<ResearchTopic>());

        foreach (var topic in research.Topics)
        {
            await WriteTopicAsync(doc, topic, parent: null, depth: 0, dirty, onProgress, cancellationToken);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Regenerate the "Sources" tab — the LAST tab of the FIRST root-level
    /// tabs module, rebuilt from research.Sources every time it runs.
    /// Returns true when a tabs module exists and the tab was maintained;
    /// false when the entry has no tabs module (caller may want the legacy
    /// root-level sources module instead).
    /// </summary>
    public bool EnsureSourcesTab(JournalEntryDocument doc, ResearchObject research)
    {
        var tabsModule = doc.FindFirstRootTabsModule();
        if (tabsModule == null) return false;
        if (tabsModule["tabs"] is not JsonArray tabs)
            tabsModule["tabs"] = tabs = new JsonArray();

        // Locate the existing sources tab by its stable id.
        JsonObject? sourcesTab = null;
        foreach (var t in tabs)
        {
            if (t is JsonObject to && to["id"]?.ToString() == "sources")
            { sourcesTab = to; break; }
        }

        if (research.Sources.Count == 0)
        {
            if (sourcesTab != null) doc.RemoveTab(tabsModule, sourcesTab);
            return true;
        }

        if (sourcesTab == null)
        {
            sourcesTab = new JsonObject
            {
                ["id"] = "sources",
                ["title"] = "Sources",
                ["modules"] = new JsonArray()
            };
            tabs.Add(sourcesTab);
        }
        else
        {
            // Always last.
            tabs.Remove(sourcesTab);
            tabs.Add(sourcesTab);
        }

        var html = "<h2>Sources</h2><ol>" +
            string.Join("", research.Sources
                .OrderBy(s => s.Id)
                .Select(s => $"<li><a href=\"{System.Net.WebUtility.HtmlEncode(s.Url)}\">" +
                    $"{System.Net.WebUtility.HtmlEncode(s.Title)}</a></li>")) +
            "</ol>";

        if (sourcesTab["modules"] is not JsonArray mods || mods.Count == 0)
        {
            sourcesTab["modules"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text-editor",
                    ["id"] = JournalEntryDocument.NewModuleId(),
                    ["manuallyAdded"] = false,
                    ["html"] = html
                }
            };
        }
        else if (mods[0] is JsonObject editor)
        {
            editor["html"] = html;
        }
        return true;
    }

    private async Task WriteTopicAsync(
        JournalEntryDocument doc,
        ResearchTopic topic,
        ResearchTopic? parent,
        int depth,
        HashSet<ResearchTopic> dirty,
        Action<string>? onProgress,
        CancellationToken cancellationToken)
    {
        if (dirty.Contains(topic) && !topic.Archived)
        {
            if (topic.Facts.Count == 0)
            {
                ApplyEmptyTopicRule(doc, topic);
            }
            else
            {
                await WriteModuleAsync(doc, topic, parent, depth, onProgress, cancellationToken);
            }
        }

        foreach (var st in topic.Subtopics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteTopicAsync(doc, st, topic, depth + 1, dirty, onProgress, cancellationToken);
        }
    }

    /// <summary>
    /// A topic with no remaining facts loses its module — stale prose must
    /// never outlive the facts that produced it.
    /// </summary>
    private void ApplyEmptyTopicRule(JournalEntryDocument doc, ResearchTopic topic)
    {
        if (!string.IsNullOrEmpty(topic.ModuleId))
        {
            var home = doc.FindTabByModuleId(topic.ModuleId);
            if (home != null)
            {
                doc.RemoveTab(home.Value.TabsModule, home.Value.Tab);
            }
            else
            {
                doc.RemoveModuleById(topic.ModuleId);
            }
            topic.ModuleId = null;
            topic.TabId = null;
        }
    }

    /// <summary>
    /// Ensure the topic's bound text-editor exists (creating the whole
    /// container chain deterministically), then write its HTML via the LLM
    /// with the module's full current text + all facts.
    /// </summary>
    private async Task WriteModuleAsync(
        JournalEntryDocument doc,
        ResearchTopic topic,
        ResearchTopic? parent,
        int depth,
        Action<string>? onProgress,
        CancellationToken cancellationToken)
    {
        var module = !string.IsNullOrEmpty(topic.ModuleId)
            ? doc.FindModule(topic.ModuleId)
            : null;

        if (module == null)
        {
            module = CreateModuleStructure(doc, topic, parent, depth);
            if (module == null) return; // structure creation failed — skip
        }

        var currentHtml = module["html"]?.ToString() ?? "";
        var user = Prompts.ModuleProseUser(topic, depth, currentHtml);
        var system = Prompts.ModuleProseSystem();

        onProgress?.Invoke($"Writing \"{topic.Topic}\"…");

        var response = await LLMJson.ParseWithRetryAsync<ModuleHtmlResponse>(
            prompt => _llm.RemoteAsync(system, prompt, json: true, cancellationToken, debugType: "module-prose"),
            user,
            r => r == null || string.IsNullOrWhiteSpace(r.Html) ? "missing \"html\"" : null,
            maxAttempts: 2);

        if (!string.IsNullOrWhiteSpace(response?.Html))
        {
            module["html"] = WithTopicHeading(response.Html, topic.Topic);
        }
    }

    /// <summary>
    /// Every generated text-editor opens with <h4>{topic}</h4> — code adds it
    /// (the prose prompt already forbids the topic name, but the LLM sees our
    /// previous h4 in the current text and may carry it over, so strip any
    /// leading h4 before prepending to keep rewrites idempotent).
    /// </summary>
    private static string WithTopicHeading(string html, string topicTitle)
    {
        var stripped = Regex.Replace(html.TrimStart(),
            @"^<h4[^>]*>.*?</h4>\s*", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return $"<h4>{System.Net.WebUtility.HtmlEncode(topicTitle)}</h4>" + stripped;
    }

    /// <summary>
    /// Deterministic structure creation — the ONLY place modules get placed:
    /// depth 0 → title + text-editor at root;
    /// depth 1 → one tab (one text-editor) inside the parent's root tabs
    /// module; depth 2 → one tab inside a nested RIGHT-aligned tabs module
    /// living in the parent subtopic's tab.
    /// </summary>
    private JsonObject? CreateModuleStructure(
        JournalEntryDocument doc,
        ResearchTopic topic,
        ResearchTopic? parent,
        int depth)
    {
        if (depth == 0)
        {
            // Root topic → title + text-editor at root level.
            doc.Modules.Add(new JsonObject
            {
                ["type"] = "title",
                ["id"] = JournalEntryDocument.NewModuleId(),
                ["manuallyAdded"] = false,
                ["text"] = topic.Topic,
                ["level"] = 2
            });
            var editor = new JsonObject
            {
                ["type"] = "text-editor",
                ["id"] = JournalEntryDocument.NewModuleId(),
                ["manuallyAdded"] = false,
                ["html"] = ""
            };
            doc.Modules.Add(editor);
            topic.ModuleId = editor["id"]!.ToString();
            return editor;
        }

        if (depth == 1)
        {
            // Subtopic → a tab inside the parent's ROOT-level tabs module.
            var tabsModule = doc.EnsureRootTabsModule(parent?.ModuleId);
            var label = ResearchObject.CleanTabLabel(topic.Tab)
                        ?? ResearchObject.FirstWords(topic.Topic, 3);
            var tab = doc.EnsureTab(tabsModule, label);
            var editor = NewEditor();
            ((JsonArray)tab["modules"]!).Add(editor);
            topic.TabId = tabsModule["id"]!.ToString();
            topic.ModuleId = editor["id"]!.ToString();
            return editor;
        }

        // depth >= 2 → nested RIGHT-aligned tabs inside the parent's own tab.
        if (parent == null || string.IsNullOrEmpty(parent.ModuleId)) return null;
        var parentHome = doc.FindTabByModuleId(parent.ModuleId);
        if (parentHome == null) return null;

        var parentTab = parentHome.Value.Tab;
        if (parentTab["modules"] is not JsonArray parentModules)
            parentTab["modules"] = parentModules = new JsonArray();

        JsonObject? nested = null;
        foreach (var m in parentModules)
        {
            if (m is JsonObject mo &&
                (mo["type"]?.ToString() ?? "") == "tabs" &&
                mo["align"]?.ToString() == "right")
            { nested = mo; break; }
        }
        if (nested == null)
        {
            nested = new JsonObject
            {
                ["type"] = "tabs",
                ["id"] = JournalEntryDocument.NewModuleId(),
                ["style"] = 1,
                ["align"] = "right",
                ["manuallyAdded"] = false,
                ["tabs"] = new JsonArray()
            };
            parentModules.Add(nested);
        }

        var childLabel = ResearchObject.CleanTabLabel(topic.Tab)
                         ?? ResearchObject.FirstWords(topic.Topic, 3);
        var childTab = doc.EnsureTab(nested, childLabel);
        var childEditor = NewEditor();
        ((JsonArray)childTab["modules"]!).Add(childEditor);
        topic.TabId = nested["id"]!.ToString();
        topic.ModuleId = childEditor["id"]!.ToString();
        return childEditor;
    }

    private static JsonObject NewEditor() => new()
    {
        ["type"] = "text-editor",
        ["id"] = JournalEntryDocument.NewModuleId(),
        ["manuallyAdded"] = false,
        ["html"] = ""
    };

    private class ModuleHtmlResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("html")]
        public string? Html { get; set; }
    }
}
