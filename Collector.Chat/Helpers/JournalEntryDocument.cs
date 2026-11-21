using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Collector.Common;

namespace Collector.Chat.Helpers;

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
    /// Downloaded assets ("resources" key) — {filename, url, page} records for
    /// every file stored inside this entry, so modules can reference them by
    /// filename and provenance is preserved.
    /// </summary>
    public JsonArray Resources { get; }

    /// <summary>
    /// The knowledge layer ("research" key) — topics, subtopics, facts and
    /// sources kept separate from the module tree. Parsed on load; serialized
    /// back into Root on Save. Null until research has ever been recorded.
    /// </summary>
    public ResearchObject? Research { get; set; }

    private JournalEntryDocument(JsonObject root, int journalId = 0)
    {
        _journalId = journalId;
        Root = root;
        if (root["modules"] is not JsonArray modules)
        {
            modules = new JsonArray();
            root["modules"] = modules;
        }
        Modules = modules;

        if (root["resources"] is not JsonArray resources)
        {
            resources = new JsonArray();
            root["resources"] = resources;
        }
        Resources = resources;

        if (root["research"] is JsonObject researchNode)
        {
            Research = ResearchObject.FromJson(researchNode);
            // Orphan pruning — drop bindings to modules the user deleted.
            Research.PruneOrphanedBindings(id => FindModule(id) != null);
            // Stale-archive healing — archived means "module/tab gone". A
            // topic still bound to a live module or tab (restored or
            // duplicated out-of-band) is un-archived.
            foreach (var t in Research.AllTopics().Where(t => t.Archived).ToList())
            {
                var bound = new List<string?> { t.ModuleId, t.SubTopicsModuleId() };
                bound.AddRange(t.ContentItems("title")
                    .Select(i => i["moduleId"]?.ToString()));
                if (bound.Any(i => !string.IsNullOrEmpty(i) &&
                    (FindModule(i!) != null || FindTab(i!) != null)))
                    Research.SetArchived(t, false);
            }
        }

        MergeDuplicateTabsModules();
    }

    /// <summary>Two root-level tabs modules with the same tab-title set are
    /// never intentional — they come from the restore+ops race where the
    /// writer rebuilt a section the model also re-added. Keep the module
    /// whose tabs carry research bindings and the most content; drop the
    /// duplicate wholesale (its bindings die with it — orphans get pruned).
    /// Heals existing entries on load; also called before Save.</summary>
    public void MergeDuplicateTabsModules()
    {
        var tabsModules = Modules.OfType<JsonObject>()
            .Where(m => m["type"]?.ToString() == "tabs").ToList();
        var groups = tabsModules.GroupBy(m => string.Join("|",
                (m["tabs"] as JsonArray ?? new JsonArray())
                    .OfType<JsonObject>()
                    .Select(t => t["title"]?.ToString() ?? "")
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0 && g.Count() > 1);

        var remove = new HashSet<JsonObject>();
        foreach (var g in groups)
        {
            var ranked = g.OrderByDescending(m =>
            {
                var bound = 0; var content = 0;
                foreach (var t in (m["tabs"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                {
                    content += (t["modules"] as JsonArray)?.Count ?? 0;
                    var id = t["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id) && Research?.FindTopicByModuleId(id) != null) bound++;
                }
                return (bound, content);
            }).ToList();
            foreach (var m in ranked.Skip(1)) remove.Add(m);
        }
        foreach (var m in remove) Modules.Remove(m);
    }

    /// <summary>The journal this entry belongs to — set by Load; Save uses it
    /// to write the {journalId}/{entryId}/ folder layout.</summary>
    private readonly int _journalId;

    /// <summary>The journal this entry belongs to (0 when unknown).</summary>
    public int JournalId => _journalId;

    /// <summary>Relative path of the entry document inside journal storage.</summary>
    public static string EntryPath(int journalId, Guid entryId) => $"{journalId}/{entryId:N}/entry.json";

    /// <summary>Relative path of the research-layer document (research +
    /// sources + resources) — a sibling of entry.json in the same folder.</summary>
    public static string ResearchJsonPath(int journalId, Guid entryId) => $"{journalId}/{entryId:N}/research.json";

    /// <summary>Relative folder holding per-source research JSON dumps.</summary>
    public static string ResearchFolderPath(int journalId, Guid entryId) => $"{journalId}/{entryId:N}/research";

    /// <summary>Relative path of a snapshot file.</summary>
    public static string SnapshotPath(int journalId, Guid entryId, int snapshotId) => $"{journalId}/{entryId:N}/snapshots/{snapshotId}.json";

    public static JournalEntryDocument Load(Guid entryId, int journalId = 0)
    {
        string? content = null;
        if (journalId > 0)
        {
            try { content = Files.GetFile(Files.Paths.Journal, EntryPath(journalId, entryId)); }
            catch { content = null; }
        }

        if (string.IsNullOrEmpty(content))
        {
            // Legacy root layout (pre-migration or unknown journal).
            try { content = Files.GetFile(Files.Paths.Journal, $"{entryId:N}.json"); }
            catch (FileNotFoundException) { /* brand-new entry */ }
            catch { content = null; }
        }

        // Journal unknown but the file may already be migrated — scan the
        // journal folders for */{entryId}/entry.json and recover journalId.
        var resolvedJournalId = journalId;
        if (string.IsNullOrEmpty(content))
        {
            try
            {
                var journalDir = Files.GetPath(Files.Paths.Journal);
                foreach (var dir in Directory.EnumerateDirectories(journalDir))
                {
                    var candidate = Path.Combine(dir, $"{entryId:N}", "entry.json");
                    if (!File.Exists(candidate)) continue;
                    content = File.ReadAllText(candidate);
                    if (int.TryParse(Path.GetFileName(dir), out var jid))
                        resolvedJournalId = jid;
                    break;
                }
            }
            catch { content = null; }
        }

        JsonObject? root = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try { root = JsonNode.Parse(content) as JsonObject; }
            catch { root = null; }
        }

        root ??= new JsonObject { ["id"] = entryId.ToString(), ["modules"] = new JsonArray() };

        // Research layer lives in research.json alongside entry.json in the
        // new layout — merge it into Root so the in-memory shape is unchanged.
        // Embedded "research"/"resources" (legacy files) still deserialize.
        if (resolvedJournalId > 0)
        {
            string? researchJson = null;
            try { researchJson = Files.GetFile(Files.Paths.Journal, ResearchJsonPath(resolvedJournalId, entryId)); }
            catch { researchJson = null; }
            if (!string.IsNullOrWhiteSpace(researchJson))
            {
                try
                {
                    if (JsonNode.Parse(researchJson) is JsonObject rj)
                    {
                        if (rj["research"] is JsonObject r) root["research"] = r.DeepClone();
                        if (rj["resources"] is JsonArray res) root["resources"] = res.DeepClone();
                    }
                }
                catch { }
            }
        }

        return new JournalEntryDocument(root, resolvedJournalId);
    }

    /// <summary>
    /// Record a downloaded asset in the "resources" array — filename is the
    /// relative path within the content system (e.g.
    /// "journal-entries/{entryId}/photo.jpg"), url is where it was downloaded
    /// from, page is the web page it was found on. Deduped on filename+url.
    /// </summary>
    public void RegisterResource(string filename, string url, string page)
    {
        foreach (var node in Resources)
        {
            if (node is JsonObject r &&
                (r["filename"]?.ToString() ?? "") == filename &&
                (r["url"]?.ToString() ?? "") == url)
                return;
        }
        Resources.Add(new JsonObject
        {
            ["filename"] = filename,
            ["url"] = url,
            ["page"] = page
        });
    }

    public void Save(Guid entryId)
    {
        // Runtime mutations (restore + model ops) can create duplicate
        // tabs modules — collapse them before persisting.
        MergeDuplicateTabsModules();

        // Stamp the schema version + persist the research layer — it lives in
        // research.json alongside entry.json in the new folder layout, and is
        // embedded in Root for the legacy root-file layout.
        Root["version"] = 2;
        var researchDoc = BuildResearchDocument();

        if (_journalId > 0)
        {
            // New layout: research/sources/resources go to research.json —
            // they must NOT appear inside entry.json.
            Root.Remove("research");
            Root.Remove("resources");
            AtomicWrite(EntryPath(_journalId, entryId), Root.ToJsonString());
            if (researchDoc != null)
                AtomicWrite(ResearchJsonPath(_journalId, entryId), researchDoc.ToJsonString());
        }
        else
        {
            // Legacy layout — single root file keeps everything embedded.
            if (Research != null) Root["research"] = Research.ToJson();
            AtomicWrite($"{entryId:N}.json", Root.ToJsonString());
        }
    }

    /// <summary>The research.json payload: research object (topics, facts,
    /// sources) + the resources array — or null when nothing exists yet.</summary>
    private JsonObject? BuildResearchDocument()
    {
        JsonObject? doc = null;
        if (Research != null)
            (doc ??= new JsonObject())["research"] = Research.ToJson();
        if (Resources.Count > 0)
            (doc ??= new JsonObject())["resources"] = Resources.DeepClone();
        return doc;
    }

    /// <summary>Temp-file + move write under Files.Paths.Journal.</summary>
    private static void AtomicWrite(string relativePath, string content)
    {
        var tmpPath = relativePath + ".tmp";
        Files.SaveFile(Files.Paths.Journal, tmpPath, content);
        var dir = Files.GetPath(Files.Paths.Journal);
        var tmpFull = Path.Combine(dir, tmpPath);
        var finalFull = Path.Combine(dir, relativePath);
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
            // The model sometimes copies the semantic map's bracketed ids
            // ([abc123…]) into ops verbatim — unwrap them before dispatch.
            foreach (var f in new[] { "id", "parent", "after", "before" })
                if (op[f] is JsonValue v && v.ToString() is { Length: > 0 } s &&
                    s.Trim() is { } t && t.Length > 2 && t[0] == '[' && t[^1] == ']')
                    op[f] = t[1..^1];
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
        index = ResolveAnchorIndex(op, container, index, "add", parent, errors);
        if (index < 0 || index > container.Count) container.Add(module);
        else container.Insert(index, module);
    }

    /// <summary>Find a sibling's index inside a container by module id
    /// (-1 when absent) — used to resolve "after"/"before" anchors.</summary>
    private static int IndexInContainer(JsonArray container, string id)
    {
        for (var i = 0; i < container.Count; i++)
            if (container[i] is JsonObject m && m["id"]?.ToString() == id) return i;
        return -1;
    }

    /// <summary>"after"/"before" anchor ops name a sibling module id —
    /// resolved to an index at apply time, after any detach, so the
    /// model never has to do index arithmetic over a shifting array.
    /// Returns the resolved index (unchanged when no anchor is given).</summary>
    private int ResolveAnchorIndex(JsonObject op, JsonArray container, int index,
        string kind, string parent, List<string> errors)
    {
        var after = op["after"]?.ToString();
        var before = op["before"]?.ToString();
        if (string.IsNullOrEmpty(after) && string.IsNullOrEmpty(before)) return index;
        var anchor = !string.IsNullOrEmpty(after) ? after : before;
        var anchorIdx = IndexInContainer(container, anchor!);
        if (anchorIdx < 0)
        {
            errors.Add($"{kind} op: anchor \"{anchor}\" not found in parent \"{parent}\"");
            return index;
        }
        return !string.IsNullOrEmpty(after) ? anchorIdx + 1 : anchorIdx;
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

        // Section-level remove: the id may be a TAB (the semantic map
        // labels sections by tab id). RemoveTab drops the tab and prunes
        // its tabs module when it ends up empty.
        var owner = FindTabOwner(id);
        JsonObject? removedModule = null;
        if (owner != null)
        {
            var mod = owner.Value.Module;
            RemoveTab(mod, owner.Value.Tab);
            // An emptied tabs module goes down with its last tab — the
            // removal set then covers the module id and every tab it held.
            if (FindModule(mod["id"]?.ToString() ?? "") == null)
                removedModule = mod;
        }
        else
        {
            removedModule = FindModule(id);
            if (removedModule == null || !RemoveFromContainer(Modules, id))
            {
                errors.Add($"remove op: module \"{id}\" not found");
                return;
            }
        }

        // Archive bridge — a conversational remove that kills a topic-bound
        // module archives the topic (module/tab gone, facts kept). Removing a
        // whole tabs module archives every subtopic bound to it — including
        // topics bound to the tab ids INSIDE it, not just the module id.
        if (Research != null)
        {
            var removedIds = new HashSet<string> { id };
            if (removedModule?["id"]?.ToString() is string rmId) removedIds.Add(rmId);
            if (removedModule?["tabs"] is JsonArray removedTabs)
                foreach (var tn in removedTabs)
                    if (tn is JsonObject t && t["id"]?.ToString() is string tid)
                        removedIds.Add(tid);

            foreach (var t in Research.AllTopics())
            {
                // Removing a topic's tab, its sub_topics tabs module, or its
                // title item archives the topic — content is kept for re-use.
                if (removedIds.Contains(t.ModuleId ?? "") ||
                    removedIds.Contains(t.SubTopicsModuleId() ?? "") ||
                    t.ContentItems("title").Any(i => removedIds.Contains(i["moduleId"]?.ToString() ?? "")))
                {
                    Research.SetArchived(t, true);
                }
            }
        }
    }

    private void ApplyMove(JsonObject op, List<string> errors)
    {
        var id = op["id"]?.ToString();
        if (string.IsNullOrEmpty(id)) { errors.Add("move op missing \"id\""); return; }

        var after = op["after"]?.ToString();
        var before = op["before"]?.ToString();
        var anchor = !string.IsNullOrEmpty(after) ? after : before;

        // Section-level move: the semantic map labels SECTIONS by their tab
        // ids, so the model emits tab ids — not module ids — for both the
        // target and the anchor. Tabs in the same tabs module reorder inside
        // it; otherwise the owning module is the unit that moves.
        var owner = FindTabOwner(id);
        var anchorOwner = anchor != null ? FindTabOwner(anchor) : null;
        if (owner != null && anchorOwner != null &&
            owner.Value.Module["id"]?.ToString() == anchorOwner.Value.Module["id"]?.ToString())
        {
            var tabs = (JsonArray)owner.Value.Module["tabs"]!;
            tabs.Remove(owner.Value.Tab);
            var aidx = IndexInContainer(tabs, anchorOwner.Value.Tab["id"]!.ToString());
            tabs.Insert(aidx < 0 ? tabs.Count : (!string.IsNullOrEmpty(after) ? aidx + 1 : aidx),
                owner.Value.Tab);
            return;
        }
        if (owner != null || anchorOwner != null)
        {
            ApplySectionMove(op, id, owner, anchorOwner, errors);
            return;
        }

        var target = FindModule(id);
        if (target == null) { errors.Add($"move op: module \"{id}\" not found"); return; }

        var parent = op["parent"]?.ToString() ?? "root";
        var container = ResolveContainer(parent);
        if (container == null)
        {
            errors.Add($"move op: parent \"{parent}\" not found or not a container");
            return;
        }

        // Detach BEFORE resolving the anchor — indices are computed on the
        // final array so an anchor can't be invalidated by the removal.
        RemoveFromContainer(Modules, id);
        var index = op["index"]?.GetValue<int>() ?? -1;
        index = ResolveAnchorIndex(op, container, index, "move", parent, errors);
        if (index < 0 || index > container.Count) container.Add(target);
        else container.Insert(index, target);
    }

    /// <summary>Move where the target or the anchor is a SECTION (tab id).
    /// The moving unit is always a module — the tab's owning tabs module
    /// when the id names a section, the module itself otherwise — and it
    /// lands next to the anchor's owning module in whatever container the
    /// anchor actually lives in.</summary>
    private void ApplySectionMove(JsonObject op, string id,
        (JsonObject Module, JsonObject Tab)? owner,
        (JsonObject Module, JsonObject Tab)? anchorOwner,
        List<string> errors)
    {
        var after = op["after"]?.ToString();
        var before = op["before"]?.ToString();
        var wantAfter = !string.IsNullOrEmpty(after) || string.IsNullOrEmpty(before);

        var target = owner?.Module ?? FindModule(id);
        if (target == null) { errors.Add($"move op: module \"{id}\" not found"); return; }

        JsonObject? anchorModule = anchorOwner?.Module;
        if (anchorModule == null && !string.IsNullOrEmpty(after ?? before))
            anchorModule = FindModule((after ?? before)!);
        if (anchorModule != null && ReferenceEquals(anchorModule, target))
        {
            errors.Add("move op: target and anchor are the same module");
            return;
        }

        var insertContainer = anchorModule != null
            ? ImmediateContainerOf(Modules, anchorModule) ?? Modules
            : (ResolveContainer(op["parent"]?.ToString() ?? "root") ?? Modules);

        // Detach the moving module (by ITS id — `id` may be a tab) before
        // computing the landing index.
        RemoveFromContainer(Modules, target["id"]!.ToString());

        int index;
        if (anchorModule != null)
        {
            var aidx = IndexInContainer(insertContainer, anchorModule["id"]!.ToString());
            index = aidx < 0 ? insertContainer.Count : (wantAfter ? aidx + 1 : aidx);
        }
        else
        {
            index = op["index"]?.GetValue<int>() ?? -1;
            if (index < 0 || index > insertContainer.Count) index = insertContainer.Count;
        }
        insertContainer.Insert(Math.Min(index, insertContainer.Count), target);
    }

    /// <summary>The JsonArray that directly contains a module — root,
    /// a module-list's children, or a tab's modules.</summary>
    private static JsonArray? ImmediateContainerOf(JsonArray array, JsonObject module)
    {
        foreach (var node in array)
        {
            if (node is not JsonObject m) continue;
            if (ReferenceEquals(m, module)) return array;
            if (m["modules"] is JsonArray children)
            {
                var found = ImmediateContainerOf(children, module);
                if (found != null) return found;
            }
            if (m["tabs"] is JsonArray tabs)
                foreach (var tn in tabs)
                    if (tn is JsonObject t && t["modules"] is JsonArray tm)
                    {
                        var found = ImmediateContainerOf(tm, module);
                        if (found != null) return found;
                    }
        }
        return null;
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
                    if (tabNode is not JsonObject tab) continue;
                    // A tab's id is a moduleId binding target — subtopic
                    // nodes bind to it, so pruning/FIND must see it.
                    if (tab["id"]?.ToString() == id) return tab;
                    if (tab["modules"] is JsonArray tabModules)
                    {
                        var found = FindModuleIn(tabModules, id);
                        if (found != null) return found;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>Find a tab entry together with its owning tabs module —
    /// section ids in the semantic map are tab ids, so ops that name a
    /// "section" resolve to (module, tab) pairs rather than bare modules.</summary>
    public (JsonObject Module, JsonObject Tab)? FindTabOwner(string tabId)
    {
        foreach (var module in EnumerateModules())
        {
            if (module["tabs"] is not JsonArray tabs) continue;
            foreach (var node in tabs)
                if (node is JsonObject tab && tab["id"]?.ToString() == tabId)
                    return (module, tab);
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
