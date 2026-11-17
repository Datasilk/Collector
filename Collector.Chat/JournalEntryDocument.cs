using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Collector.Common;

namespace Collector.Chat;

/// <summary>
/// In-memory model of a journal entry's {entryId}.json content file.
/// Provides the compact structural map the LLM edits against, plus a
/// validated edit-op applier (add/update/remove/move) that understands
/// nested containers (tabs[].modules, module-list .modules).
/// </summary>
public class JournalEntryDocument
{
    private static readonly HashSet<string> KnownModuleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text-editor", "title", "image", "image-gallery", "video-player",
        "pdf-viewer", "file-download", "entries-list", "tabs", "module-list",
        "checklist", "calendar", "custom", "generate-content"
    };

    public JsonObject Root { get; }
    public JsonArray Modules { get; }

    /// <summary>
    /// The knowledge layer ("research" key) — topics, subtopics, facts and
    /// sources kept separate from the module tree. Parsed on load; serialized
    /// back into Root on Save. Null until research has ever been recorded.
    /// </summary>
    public ResearchObject? Research { get; set; }

    private JournalEntryDocument(JsonObject root)
    {
        Root = root;
        if (root["modules"] is not JsonArray modules)
        {
            modules = new JsonArray();
            root["modules"] = modules;
        }
        Modules = modules;

        if (root["research"] is JsonObject researchNode)
        {
            Research = ResearchObject.FromJson(researchNode);
            // Orphan pruning — drop bindings to modules the user deleted.
            Research.PruneOrphanedBindings(id => FindModule(id) != null);
        }
    }

    public static JournalEntryDocument Load(Guid entryId)
    {
        var filePath = $"{entryId:N}.json";
        string? content = null;
        try
        {
            content = Files.GetFile(Files.Paths.Journal, filePath);
        }
        catch (FileNotFoundException)
        {
            // brand-new entry — starts as an empty module list
        }

        JsonObject? root = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try { root = JsonNode.Parse(content) as JsonObject; }
            catch { root = null; }
        }

        root ??= new JsonObject { ["id"] = entryId.ToString(), ["modules"] = new JsonArray() };
        return new JournalEntryDocument(root);
    }

    public void Save(Guid entryId)
    {
        // Stamp the schema version + persist the research layer back into the
        // root so both layers save atomically in one write.
        Root["version"] = 2;
        if (Research != null)
        {
            Root["research"] = Research.ToJson();
        }

        // Atomic write: temp file + move so a crash/cancel mid-write can't
        // leave a truncated entry JSON.
        var filePath = $"{entryId:N}.json";
        var tmpPath = $"{entryId:N}.json.tmp";
        Files.SaveFile(Files.Paths.Journal, tmpPath, Root.ToJsonString());
        var dir = Files.GetPath(Files.Paths.Journal);
        var tmpFull = Path.Combine(dir, tmpPath);
        var finalFull = Path.Combine(dir, filePath);
        if (File.Exists(finalFull)) File.Replace(tmpFull, finalFull, null);
        else File.Move(tmpFull, finalFull);
    }

    /// <summary>
    /// Compact textual map of the module tree for LLM context. Shows each
    /// module's id, type, and a short label, with nesting for containers.
    /// </summary>
    public string BuildMap()
    {
        var sb = new StringBuilder();
        if (Modules.Count == 0)
        {
            sb.AppendLine("(empty entry)");
            return sb.ToString();
        }
        AppendMap(sb, Modules, "");
        return sb.ToString();
    }

    private void AppendMap(StringBuilder sb, JsonArray modules, string indent)
    {
        foreach (var node in modules)
        {
            if (node is not JsonObject module) continue;
            var id = module["id"]?.ToString() ?? "?";
            var type = module["type"]?.ToString() ?? "?";
            sb.AppendLine($"{indent}[{id}] {type}{ModuleLabel(module)}");

            if (type.Equals("tabs", StringComparison.OrdinalIgnoreCase) && module["tabs"] is JsonArray tabs)
            {
                var style = module["style"]?.GetValue<int>() ?? 0;
                foreach (var tabNode in tabs)
                {
                    if (tabNode is not JsonObject tab) continue;
                    var tabId = tab["id"]?.ToString() ?? "?";
                    var tabTitle = tab["title"]?.ToString() ?? "";
                    sb.AppendLine($"{indent}  tab \"{tabTitle}\" (id: {tabId}){(style == 1 ? " [side-menu]" : "")}");
                    if (tab["modules"] is JsonArray tabModules)
                    {
                        AppendMap(sb, tabModules, indent + "    ");
                    }
                }
            }
            else if (module["modules"] is JsonArray children)
            {
                AppendMap(sb, children, indent + "  ");
            }
        }
    }

    private static string ModuleLabel(JsonObject module)
    {
        var type = module["type"]?.ToString() ?? "";
        switch (type)
        {
            case "text-editor":
                var html = module["html"]?.ToString() ?? "";
                var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
                text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
                return text.Length > 0 ? $" \"{Trunc(text, 40)}\"" : "";
            case "title":
                return $" \"{Trunc(module["text"]?.ToString() ?? "", 40)}\"";
            case "image":
                return $" \"{Trunc(module["originalFilename"]?.ToString() ?? module["filename"]?.ToString() ?? module["url"]?.ToString() ?? "", 40)}\"";
            case "tabs":
                return $" (style:{module["style"]?.GetValue<int>() ?? 0})";
            case "custom":
                return $" \"{Trunc(module["title"]?.ToString() ?? "", 40)}\"";
            default:
                return "";
        }
        static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";
    }

    /// <summary>
    /// Apply an LLM-produced edit batch: {"ops":[{op, ...}, ...]}.
    /// Ops are applied in order; unknown/failed ops are collected as errors
    /// and skipped so one bad op can't corrupt the entry.
    /// </summary>
    public List<string> ApplyOps(JsonNode? opsRoot)
    {
        var errors = new List<string>();
        var ops = opsRoot?["ops"] as JsonArray;
        if (ops == null)
        {
            errors.Add("Edit batch has no \"ops\" array");
            return errors;
        }

        foreach (var opNode in ops)
        {
            if (opNode is not JsonObject op) { errors.Add("Op is not an object"); continue; }
            var kind = op["op"]?.ToString() ?? "";
            try
            {
                switch (kind)
                {
                    case "add": ApplyAdd(op, errors); break;
                    case "update": ApplyUpdate(op, errors); break;
                    case "remove": ApplyRemove(op, errors); break;
                    case "move": ApplyMove(op, errors); break;
                    default: errors.Add($"Unknown op \"{kind}\""); break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Op \"{kind}\" failed: {ex.Message}");
            }
        }
        return errors;
    }

    private void ApplyAdd(JsonObject op, List<string> errors)
    {
        if (op["module"] is not JsonObject rawModule)
        {
            errors.Add("add op missing \"module\"");
            return;
        }

        var type = rawModule["type"]?.ToString() ?? "";
        if (!KnownModuleTypes.Contains(type))
        {
            errors.Add($"add op: unknown module type \"{type}\"");
            return;
        }

        // Clone — the node still belongs to the parsed ops tree and can't be
        // inserted into another parent as-is.
        var module = (JsonObject)rawModule.DeepClone();
        EnsureIds(module);

        var parent = op["parent"]?.ToString() ?? "root";
        var container = ResolveContainer(parent);
        if (container == null)
        {
            errors.Add($"add op: parent \"{parent}\" not found or not a container");
            return;
        }

        var index = op["index"]?.GetValue<int>() ?? -1;
        if (index < 0 || index > container.Count) container.Add(module);
        else container.Insert(index, module);
    }

    private void ApplyUpdate(JsonObject op, List<string> errors)
    {
        var id = op["id"]?.ToString();
        if (string.IsNullOrEmpty(id)) { errors.Add("update op missing \"id\""); return; }

        var target = FindModule(id);
        if (target == null) { errors.Add($"update op: module \"{id}\" not found"); return; }

        if (op["set"] is JsonObject changes)
        {
            foreach (var kv in changes)
            {
                if (kv.Key == "id" || kv.Key == "modules" || kv.Key == "tabs") continue;
                target[kv.Key] = kv.Value?.DeepClone();
            }
        }
    }

    /// <summary>
    /// Semantic skeleton for conversational edits and media placement — like
    /// BuildMap, but each text-editor's label is its bound topic title (from
    /// the research object) instead of truncated HTML, so the model reasons
    /// about "the Timeline section" without ingesting every paragraph.
    /// </summary>
    public string BuildSemanticMap()
    {
        var sb = new StringBuilder();
        if (Modules.Count == 0)
        {
            sb.AppendLine("(empty entry)");
            return sb.ToString();
        }
        AppendSemanticMap(sb, Modules, "");
        return sb.ToString();
    }

    private void AppendSemanticMap(StringBuilder sb, JsonArray modules, string indent)
    {
        foreach (var node in modules)
        {
            if (node is not JsonObject module) continue;
            var id = module["id"]?.ToString() ?? "?";
            var type = module["type"]?.ToString() ?? "?";
            var label = SemanticModuleLabel(module);
            sb.AppendLine($"{indent}[{id}] {type}{label}");

            if (type.Equals("tabs", StringComparison.OrdinalIgnoreCase) && module["tabs"] is JsonArray tabs)
            {
                var style = module["style"]?.GetValue<int>() ?? 0;
                var align = module["align"]?.ToString() ?? "";
                foreach (var tabNode in tabs)
                {
                    if (tabNode is not JsonObject tab) continue;
                    var tabId = tab["id"]?.ToString() ?? "?";
                    var tabTitle = tab["title"]?.ToString() ?? "";
                    sb.AppendLine($"{indent}  tab \"{tabTitle}\" (id: {tabId}){(style == 1 ? " [side-menu]" : "")}{(align == "right" ? " [right]" : "")}");
                    if (tab["modules"] is JsonArray tabModules)
                        AppendSemanticMap(sb, tabModules, indent + "    ");
                }
            }
            else if (module["modules"] is JsonArray children)
            {
                AppendSemanticMap(sb, children, indent + "  ");
            }
        }
    }

    private string SemanticModuleLabel(JsonObject module)
    {
        var type = module["type"]?.ToString() ?? "";
        if (type == "text-editor" && Research != null)
        {
            var bound = Research.FindTopicByModuleId(module["id"]?.ToString() ?? "");
            if (bound != null) return $" — topic: \"{bound.Topic}\"";
        }
        return ModuleLabel(module);
    }

    private void ApplyRemove(JsonObject op, List<string> errors)
    {
        var id = op["id"]?.ToString();
        if (string.IsNullOrEmpty(id)) { errors.Add("remove op missing \"id\""); return; }

        if (!RemoveFromContainer(Modules, id))
        {
            errors.Add($"remove op: module \"{id}\" not found");
            return;
        }

        // Archive bridge — a conversational remove that kills a topic-bound
        // module archives the topic (module/tab gone, facts kept). Removing a
        // whole tabs module archives every subtopic bound to it.
        if (Research != null)
        {
            foreach (var t in Research.AllTopics())
            {
                if (t.ModuleId == id || t.TabId == id)
                {
                    Research.SetArchived(t, true);
                    t.ModuleId = null;
                    t.TabId = null;
                }
            }
        }
    }

    private void ApplyMove(JsonObject op, List<string> errors)
    {
        var id = op["id"]?.ToString();
        if (string.IsNullOrEmpty(id)) { errors.Add("move op missing \"id\""); return; }

        var target = FindModule(id);
        if (target == null) { errors.Add($"move op: module \"{id}\" not found"); return; }

        var parent = op["parent"]?.ToString() ?? "root";
        var container = ResolveContainer(parent);
        if (container == null)
        {
            errors.Add($"move op: parent \"{parent}\" not found or not a container");
            return;
        }

        // Detach from current location, insert at index (or end)
        RemoveFromContainer(Modules, id);
        var index = op["index"]?.GetValue<int>() ?? -1;
        if (index < 0 || index > container.Count) container.Add(target);
        else container.Insert(index, target);
    }

    /// <summary>
    /// Resolve a container id to its JsonArray of modules.
    /// "root" → top-level; a module id with a "modules" array (module-list /
    /// custom) or a "tabs" entry id → that tab's modules.
    /// </summary>
    public JsonArray? ResolveContainer(string parent)
    {
        if (parent == "root" || string.IsNullOrEmpty(parent)) return Modules;

        var module = FindModule(parent);
        if (module != null)
        {
            // module-list style container
            if (module["modules"] is JsonArray children) return children;
            // tabs module → can't add directly; need a tab id
            return null;
        }

        var tab = FindTab(parent);
        if (tab != null)
        {
            if (tab["modules"] is not JsonArray tabModules)
            {
                tabModules = new JsonArray();
                tab["modules"] = tabModules;
            }
            return tabModules;
        }

        return null;
    }

    /// <summary>
    /// Find a module by id anywhere in the tree (excluding tab objects).
    /// </summary>
    public JsonObject? FindModule(string id) => FindModuleIn(Modules, id);

    private JsonObject? FindModuleIn(JsonArray modules, string id)
    {
        foreach (var node in modules)
        {
            if (node is not JsonObject module) continue;
            if (module["id"]?.ToString() == id) return module;

            if (module["modules"] is JsonArray children)
            {
                var found = FindModuleIn(children, id);
                if (found != null) return found;
            }

            if (module["tabs"] is JsonArray tabs)
            {
                foreach (var tabNode in tabs)
                {
                    if (tabNode is JsonObject tab && tab["modules"] is JsonArray tabModules)
                    {
                        var found = FindModuleIn(tabModules, id);
                        if (found != null) return found;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Find a tab entry ({id,title,modules}) inside any tabs module.
    /// </summary>
    public JsonObject? FindTab(string tabId)
    {
        foreach (var tabsModule in EnumerateModules())
        {
            if (tabsModule["tabs"] is not JsonArray tabs) continue;
            foreach (var tabNode in tabs)
            {
                if (tabNode is JsonObject tab && tab["id"]?.ToString() == tabId)
                    return tab;
            }
        }
        return null;
    }

    /// <summary>
    /// All module objects in the tree (depth-first, not including tab objects).
    /// </summary>
    public IEnumerable<JsonObject> EnumerateModules()
    {
        var stack = new Stack<JsonArray>();
        stack.Push(Modules);
        while (stack.Count > 0)
        {
            foreach (var node in stack.Pop())
            {
                if (node is not JsonObject module) continue;
                yield return module;
                if (module["modules"] is JsonArray children) stack.Push(children);
                if (module["tabs"] is JsonArray tabs)
                {
                    foreach (var tabNode in tabs)
                    {
                        if (tabNode is JsonObject tab && tab["modules"] is JsonArray tm)
                            stack.Push(tm);
                    }
                }
            }
        }
    }

    /// <summary>Remove a module anywhere in the tree. Public wrapper used by
    /// the module writer (empty-topic cleanup).</summary>
    public bool RemoveModuleById(string id) => RemoveFromContainer(Modules, id);

    /// <summary>The first root-level "tabs" module, or null.</summary>
    public JsonObject? FindFirstRootTabsModule()
    {
        foreach (var node in Modules)
        {
            if (node is JsonObject m &&
                (m["type"]?.ToString() ?? "").Equals("tabs", StringComparison.OrdinalIgnoreCase))
                return m;
        }
        return null;
    }

    /// <summary>Find or create a side-menu (style 1) tabs module at ROOT,
    /// positioned right after the given module id when possible.</summary>
    public JsonObject EnsureRootTabsModule(string? afterModuleId = null)
    {
        var tabs = FindFirstRootTabsModule();
        if (tabs != null) return tabs;

        tabs = new JsonObject
        {
            ["type"] = "tabs",
            ["id"] = NewModuleId(),
            ["style"] = 1,
            ["manuallyAdded"] = false,
            ["tabs"] = new JsonArray()
        };

        var insertAt = -1;
        if (afterModuleId != null)
        {
            for (int i = 0; i < Modules.Count; i++)
            {
                if (Modules[i] is JsonObject m && m["id"]?.ToString() == afterModuleId)
                { insertAt = i + 1; break; }
            }
        }
        if (insertAt >= 0) Modules.Insert(insertAt, tabs); else Modules.Add(tabs);
        return tabs;
    }

    /// <summary>
    /// Find or append a tab inside a tabs module. Titles must be unique
    /// within the container — colliding labels get " 2", " 3"… suffixes.
    /// Returns the tab object.
    /// </summary>
    public JsonObject EnsureTab(JsonObject tabsModule, string title)
    {
        if (tabsModule["tabs"] is not JsonArray tabs)
            tabsModule["tabs"] = tabs = new JsonArray();

        var unique = title;
        var suffix = 2;
        while (tabs.Any(t => (t as JsonObject)?["title"]?.ToString()
                .Equals(unique, StringComparison.OrdinalIgnoreCase) == true))
        {
            unique = $"{title} {suffix++}";
        }

        var tab = new JsonObject
        {
            ["id"] = "t" + Guid.NewGuid().ToString("N").Substring(0, 8),
            ["title"] = unique,
            ["modules"] = new JsonArray()
        };
        tabs.Add(tab);
        return tab;
    }

    /// <summary>The tab object (inside any tabs module) whose modules contain
    /// the given module id — used by the empty-topic rule to drop the tab.</summary>
    public (JsonObject TabsModule, JsonObject Tab)? FindTabByModuleId(string moduleId)
    {
        foreach (var tabsModule in EnumerateModules())
        {
            if ((tabsModule["type"]?.ToString() ?? "") != "tabs") continue;
            if (tabsModule["tabs"] is not JsonArray tabs) continue;
            foreach (var tabNode in tabs)
            {
                if (tabNode is not JsonObject tab) continue;
                if (tab["modules"] is not JsonArray tm) continue;
                foreach (var child in tm)
                {
                    if (child is JsonObject c && c["id"]?.ToString() == moduleId)
                        return (tabsModule, tab);
                }
            }
        }
        return null;
    }

    /// <summary>Remove a tab entry from a tabs module.</summary>
    public void RemoveTab(JsonObject tabsModule, JsonObject tab)
    {
        if (tabsModule["tabs"] is not JsonArray tabs) return;
        tabs.Remove(tab);
        // If the container is now empty, remove the tabs module itself.
        if (tabs.Count == 0 && tabsModule["id"] is JsonNode idNode)
            RemoveFromContainer(Modules, idNode.ToString());
    }

    private bool RemoveFromContainer(JsonArray modules, string id)
    {
        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] is JsonObject module)
            {
                if (module["id"]?.ToString() == id)
                {
                    modules.RemoveAt(i);
                    return true;
                }
                if (module["modules"] is JsonArray children && RemoveFromContainer(children, id)) return true;
                if (module["tabs"] is JsonArray tabs)
                {
                    foreach (var tabNode in tabs)
                    {
                        if (tabNode is JsonObject tab && tab["modules"] is JsonArray tm
                            && RemoveFromContainer(tm, id)) return true;
                    }
                }
            }
        }
        return false;
    }

    private static void EnsureIds(JsonObject module)
    {
        if (string.IsNullOrEmpty(module["id"]?.ToString()))
        {
            module["id"] = NewModuleId();
        }
        if (module["manuallyAdded"] == null) module["manuallyAdded"] = false;

        if (module["tabs"] is JsonArray tabs)
        {
            foreach (var tabNode in tabs)
            {
                if (tabNode is JsonObject tab)
                {
                    if (string.IsNullOrEmpty(tab["id"]?.ToString()))
                        tab["id"] = NewTabId();
                    if (tab["modules"] is not JsonArray tm)
                        tab["modules"] = tm = new JsonArray();
                    foreach (var child in tm)
                    {
                        if (child is JsonObject childModule) EnsureIds(childModule);
                    }
                }
            }
        }
        if (module["modules"] is JsonArray children)
        {
            foreach (var child in children)
            {
                if (child is JsonObject childModule) EnsureIds(childModule);
            }
        }
    }

    /// <summary>
    /// Extract all readable text from the entry (module html/text/title/caption
    /// fields, recursively through containers) with HTML stripped — used by the
    /// research-review tool for coverage checks.
    /// </summary>
    public string ExtractText()
    {
        var sb = new StringBuilder();
        ExtractTextFrom(Modules, sb);
        return sb.ToString();
    }

    private static void ExtractTextFrom(JsonArray modules, StringBuilder sb)
    {
        foreach (var node in modules)
        {
            if (node is not JsonObject mod) continue;
            foreach (var field in new[] { "html", "text", "title", "caption" })
            {
                var val = mod[field]?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    sb.AppendLine(StripHtml(val));
                }
            }
            if (mod["modules"] is JsonArray children) ExtractTextFrom(children, sb);
            if (mod["tabs"] is JsonArray tabs)
            {
                foreach (var tabNode in tabs)
                {
                    if (tabNode is JsonObject tab && tab["modules"] is JsonArray tm)
                        ExtractTextFrom(tm, sb);
                }
            }
        }
    }

    private static string StripHtml(string html)
        => System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");

    public static string NewModuleId() => new Random().Next(1, 10000000).ToString();
    private static string NewTabId() => "t" + Guid.NewGuid().ToString("N").Substring(0, 8);
}
