using System.Text;
using System.Text.Json.Nodes;

namespace Collector.Chat.Helpers;

/// <summary>
/// The structured research tree for a journal entry, serialized into
/// research.json:
///
///   topics[]   — { title, content[] } where content items are typed blocks
///                ({type, moduleId, source, ...}) copied from per-page parser
///                dumps: title, paragraph, bullet_list, numbered_list, table,
///                image, image-gallery, sub_topics.
///   comments   — { moduleId, items[] } hierarchical comments w/ replies.
///   sources    — { moduleId, items[] } web pages; index into items is the
///                "source" property on every content item.
///   searches   — { query, url, timestamp } engine queries; a source's
///                "search" property indexes into this array.
///
/// Topic nodes are thin wrappers over their JsonObject so nested subtopic
/// items ({tab,title,moduleId,content[]}) serialize identically.
/// </summary>
public class ResearchObject
{
    public List<ResearchTopic> Topics { get; } = new();
    public List<ResearchSource> Sources { get; } = new();
    public List<ResearchSearch> Searches { get; } = new();
    public List<ResearchComment> Comments { get; } = new();
    public string? SourcesModuleId { get; set; }
    public string? CommentsModuleId { get; set; }

    #region Searches

    /// <summary>Append run-shared engine queries — the shared list is
    /// append-only, so this just extends with whatever is new. Indexes stay
    /// aligned with the values gather recorded on sources.</summary>
    public void SyncSearches(IReadOnlyList<string> queries)
    {
        while (Searches.Count < queries.Count)
            Searches.Add(new ResearchSearch { Query = queries[Searches.Count] });
    }

    #endregion

    #region Sources

    /// <summary>Resolve an existing source by URL or append a new one.
    /// Returns the index into sources.items plus whether it was created.</summary>
    public (int Index, bool Created) RegisterSource(string url, string title, int? searchIndex = null)
    {
        url = (url ?? "").Trim();
        title = (title ?? "").Trim();
        for (int i = 0; i < Sources.Count; i++)
        {
            var s = Sources[i];
            if (!s.Url.Equals(url, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(s.Title) && !string.IsNullOrWhiteSpace(title))
                s.Title = title;
            if (s.SearchIndex == null && searchIndex != null)
                s.SearchIndex = searchIndex;
            return (i, false);
        }

        if (string.IsNullOrWhiteSpace(title))
            title = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : url;
        Sources.Add(new ResearchSource { Title = title, Url = url, SearchIndex = searchIndex });
        return (Sources.Count - 1, true);
    }

    /// <summary>Is this URL already a recorded source? Exact match plus
    /// YouTube video-id comparison so watch?v=X and youtu.be/X dedupe.</summary>
    public bool HasSource(string url)
    {
        url = (url ?? "").Trim();
        if (url.Length == 0) return false;
        var candidate = Tools.GatherResearch.YouTubeHelper.GetVideoId(url);
        var normalized = url.TrimEnd('/');
        foreach (var s in Sources)
        {
            var stored = (s.Url ?? "").Trim();
            if (stored.TrimEnd('/').Equals(normalized, StringComparison.OrdinalIgnoreCase))
                return true;
            if (candidate != null &&
                string.Equals(candidate, Tools.GatherResearch.YouTubeHelper.GetVideoId(stored),
                    StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>Index of a recorded source by URL, or -1.</summary>
    public int SourceIndexOf(string url)
    {
        url = (url ?? "").Trim();
        for (int i = 0; i < Sources.Count; i++)
            if (Sources[i].Url.Equals(url, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    /// <summary>Remove every content item tagged with this source index —
    /// used before re-merging a re-scraped page (replace semantics).
    /// Returns the moduleIds of removed items so the caller can delete
    /// their rendered modules.</summary>
    public List<string> RemoveContentBySource(int sourceIndex)
    {
        var removed = new List<string>();
        foreach (var topic in AllTopics())
            RemoveBySource(topic.Content, sourceIndex, removed);
        return removed;
    }

    private static void RemoveBySource(JsonArray content, int sourceIndex, List<string> removed)
    {
        for (int i = content.Count - 1; i >= 0; i--)
        {
            if (content[i] is not JsonObject item) { content.RemoveAt(i); continue; }
            var type = item["type"]?.ToString() ?? "";

            // Nested containers: recurse first.
            if (type == "sub_topics" && item["items"] is JsonArray subs)
            {
                for (int s = subs.Count - 1; s >= 0; s--)
                {
                    if (subs[s] is JsonObject st &&
                        st["content"] is JsonArray stc)
                        RemoveBySource(stc, sourceIndex, removed);
                    if (subs[s] is JsonObject stc2 &&
                        (stc2["content"] is not JsonArray c2 || c2.Count == 0))
                    {
                        // Collect the emptied subtopic's tab id so its
                        // rendered tab goes down with it.
                        if (stc2["moduleId"]?.ToString() is { Length: > 0 } smid)
                            removed.Add(smid);
                        subs.RemoveAt(s); // empty subtopic after cleanup
                    }
                }
            }
            if (type == "image-gallery" && item["items"] is JsonArray imgs)
            {
                for (int g = imgs.Count - 1; g >= 0; g--)
                    if (imgs[g] is JsonObject gi &&
                        gi["source"] is JsonValue gv && gv.TryGetValue<int>(out var gsv) && gsv == sourceIndex)
                        imgs.RemoveAt(g);
                if (imgs.Count > 0) continue; // gallery survives on other sources' images
            }

            // Title items are structural — a re-scrape never strips a
            // topic's own heading (its module stays bound either way).
            if (type != "title" &&
                item["source"] is JsonValue sv && sv.TryGetValue<int>(out var idx) && idx == sourceIndex)
            {
                if (item["moduleId"]?.ToString() is { Length: > 0 } mid) removed.Add(mid);
                content.RemoveAt(i);
                continue;
            }
            // Container items with no surviving children go too.
            if (type == "sub_topics" && item["items"] is JsonArray rest && rest.Count == 0)
            {
                if (item["moduleId"]?.ToString() is { Length: > 0 } mid) removed.Add(mid);
                content.RemoveAt(i);
            }
            if (type == "image-gallery" && item["items"] is JsonArray rest2 && rest2.Count == 0)
            {
                if (item["moduleId"]?.ToString() is { Length: > 0 } mid) removed.Add(mid);
                content.RemoveAt(i);
            }
        }
    }

    /// <summary>Null out source refs that no longer resolve (index out of range).</summary>
    public void FixDanglingSourceRefs()
    {
        bool Valid(JsonNode? n) =>
            n is not JsonValue v || !v.TryGetValue<int>(out var i) || (i >= 0 && i < Sources.Count);
        foreach (var item in AllContentItems())
            if (!Valid(item["source"])) item["source"] = null;
        foreach (var c in Comments)
            if (c.Source.HasValue && (c.Source < 0 || c.Source >= Sources.Count)) c.Source = null;
    }

    #endregion

    #region Topics

    /// <summary>All topics depth-first (roots then nested subtopic items).</summary>
    public IEnumerable<ResearchTopic> AllTopics()
    {
        foreach (var t in Topics)
        {
            yield return t;
            foreach (var d in t.AllDescendants()) yield return d;
        }
    }

    /// <summary>Every content item in the tree, in document order.</summary>
    public IEnumerable<JsonObject> AllContentItems()
    {
        foreach (var t in AllTopics())
            foreach (var n in t.Content)
                if (n is JsonObject o) yield return o;
    }

    /// <summary>Count of factual text blocks (paragraphs + list items) — the
    /// "is this topic thin" signal the coverage review uses.</summary>
    public static int TextItemCount(ResearchTopic topic)
    {
        var n = 0;
        foreach (var node in topic.Content)
        {
            if (node is not JsonObject item) continue;
            switch (item["type"]?.ToString())
            {
                case "paragraph": n++; break;
                case "bullet_list":
                case "numbered_list":
                    n += (item["items"] as JsonArray)?.Count ?? 0; break;
            }
        }
        return n;
    }

    /// <summary>All paragraph + list-item text across the tree — the global
    /// dedupe surface.</summary>
    public IEnumerable<string> AllText()
    {
        foreach (var t in AllTopics())
            foreach (var s in t.Texts()) yield return s;
    }

    /// <summary>Find a topic (or nested subtopic) by normalized title.</summary>
    public ResearchTopic? FindTopic(string title)
    {
        var want = Normalize(title);
        if (want.Length == 0) return null;
        foreach (var t in AllTopics())
            if (Normalize(t.Topic) == want) return t;
        return null;
    }

    /// <summary>Find or create a root topic by title. A newly created topic
    /// opens with the spec'd title content item.</summary>
    public ResearchTopic GetOrCreateTopic(string title, int? source = null)
    {
        var found = FindTopic(title);
        if (found != null) return found;
        var topic = new ResearchTopic(title);
        Topics.Add(topic);
        var titleItem = ContentItem("title", source);
        titleItem["text"] = title;
        topic.Content.Add(titleItem);
        return topic;
    }

    /// <summary>Find or create a subtopic under a parent. Subtopics live
    /// inside the parent's "sub_topics" content item.</summary>
    public ResearchTopic GetOrCreateSubTopic(ResearchTopic parent, string title, string? tab = null)
    {
        foreach (var st in parent.SubTopics())
            if (Normalize(st.Topic) == Normalize(title)) return st;
        var sub = new ResearchTopic(title, isSubtopic: true)
        {
            Tab = CleanTabLabel(tab) ?? FirstWords(title, 3)
        };
        sub.ModuleId = NewModuleId();
        var titleItem = ContentItem("title", null);
        titleItem["text"] = title;
        sub.Content.Add(titleItem);
        parent.SubTopicsContainerItems().Add(sub.Node);
        return sub;
    }

    /// <summary>Append a content item to a topic with global dedupe:
    /// paragraph → whole-text match anywhere in the tree skips it;
    /// lists → item lines already present anywhere are filtered;
    /// table → identical caption+headers signature skips;
    /// image/image-gallery → url dedupe inside the topic.
    /// Returns the appended item, or null when fully deduped.</summary>
    public JsonObject? AppendContent(ResearchTopic topic, JsonObject item, int? source = null)
    {
        if (source.HasValue) item["source"] = source.Value;
        var type = item["type"]?.ToString() ?? "";

        switch (type)
        {
            case "paragraph":
            {
                var n = Normalize(item["text"]?.ToString());
                if (n.Length == 0) return null;
                if (AllText().Any(t => Normalize(t) == n)) return null;
                break;
            }
            case "bullet_list":
            case "numbered_list":
            {
                if (item["items"] is not JsonArray arr) return null;
                var existing = AllText().Select(Normalize).ToHashSet();
                for (int i = arr.Count - 1; i >= 0; i--)
                    if (string.IsNullOrWhiteSpace(ItemText(arr[i])) ||
                        existing.Contains(Normalize(ItemText(arr[i]!))))
                        arr.RemoveAt(i);
                if (arr.Count == 0) return null;
                break;
            }
            case "table":
            {
                var sig = Normalize(item["caption"]?.ToString()) + "|" +
                          Normalize(string.Join('|',
                              (item["headers"] as JsonArray ?? new JsonArray())
                                  .Select(h => h?.ToString())));
                foreach (var other in topic.ContentItems("table"))
                {
                    var osig = Normalize(other["caption"]?.ToString()) + "|" +
                               Normalize(string.Join('|',
                                   (other["headers"] as JsonArray ?? new JsonArray())
                                       .Select(h => h?.ToString())));
                    if (osig == sig) return null;
                }
                break;
            }
            case "image":
            case "image-gallery":
            {
                var urls = item["type"]?.ToString() == "image"
                    ? new[] { item["url"]?.ToString() }
                    : (item["items"] as JsonArray ?? new JsonArray())
                        .Select(i => i is JsonObject io ? io["url"]?.ToString() : null)
                        .ToArray();
                var known = new HashSet<string>(
                    AllContentItems()
                        .Where(c => (c["type"]?.ToString() ?? "") is "image" or "image-gallery")
                        .SelectMany(c => c["type"]?.ToString() == "image"
                            ? new[] { c["url"]?.ToString() }
                            : (c["items"] as JsonArray ?? new JsonArray())
                                .Select(i => i is JsonObject io ? io["url"]?.ToString() : null))
                        .Where(u => !string.IsNullOrWhiteSpace(u))!,
                    StringComparer.OrdinalIgnoreCase);
                if (type == "image")
                {
                    if (urls[0] == null || known.Contains(urls[0]!)) return null;
                }
                else
                {
                    var arr = (JsonArray)item["items"]!;
                    for (int i = arr.Count - 1; i >= 0; i--)
                        if (arr[i] is JsonObject io &&
                            (io["url"] == null || known.Contains(io["url"]!.ToString())))
                            arr.RemoveAt(i);
                    if (arr.Count == 0) return null;
                }
                break;
            }
        }

        topic.Content.Add(item);
        return item;
    }

    /// <summary>Gallery helper — appends an image-gallery content item after
    /// the topic's title block, deduped by URL. When the topic already has a
    /// gallery, new images merge into its items array instead of stacking a
    /// second gallery. Returns the item or null.</summary>
    public JsonObject? AppendGallery(ResearchTopic topic, IEnumerable<(string Url, string? Alt, string? Caption)> images, int? source)
    {
        var list = images.Where(i => !string.IsNullOrWhiteSpace(i.Url)).ToList();
        if (list.Count == 0) return null;

        // Reuse the topic's existing gallery — the research object keeps one
        // gallery per topic so images accumulate in its items array.
        var existing = topic.Content
            .OfType<JsonObject>()
            .FirstOrDefault(c => c["type"]?.ToString() == "image-gallery");
        if (existing != null)
        {
            if (existing["items"] is not JsonArray items)
            {
                items = new JsonArray();
                existing["items"] = items;
            }
            var known = new HashSet<string>(
                items.Select(i => i is JsonObject io ? io["url"]?.ToString() : i?.ToString())
                     .Where(u => !string.IsNullOrWhiteSpace(u))!,
                StringComparer.OrdinalIgnoreCase);
            var added = false;
            foreach (var (url, alt, caption) in list)
            {
                if (!known.Add(url)) continue;
                var gi = new JsonObject { ["url"] = url };
                if (source.HasValue) gi["source"] = source.Value;
                if (!string.IsNullOrWhiteSpace(alt)) gi["alt_text"] = alt;
                if (!string.IsNullOrWhiteSpace(caption)) gi["caption"] = caption;
                items.Add(gi);
                added = true;
            }
            return added ? existing : null;
        }

        var gallery = ContentItem("image-gallery", source);
        var arr = new JsonArray();
        foreach (var (url, alt, caption) in list)
        {
            var gi = new JsonObject { ["url"] = url };
            if (source.HasValue) gi["source"] = source.Value;
            if (!string.IsNullOrWhiteSpace(alt)) gi["alt_text"] = alt;
            if (!string.IsNullOrWhiteSpace(caption)) gi["caption"] = caption;
            arr.Add(gi);
        }
        gallery["items"] = arr;

        // Dedupe against everything already stored.
        var filtered = AppendContent(topic, gallery, source);
        if (filtered == null) return null;

        // Spec order: title first, gallery immediately after.
        var idx = topic.Content.IndexOf(filtered);
        if (idx > 1)
        {
            topic.Content.RemoveAt(idx);
            topic.Content.Insert(1, filtered);
        }
        return filtered;
    }

    /// <summary>A fresh module id for content items / subtopic nodes.</summary>
    public static string NewModuleId() => Guid.NewGuid().ToString("N");

    /// <summary>Content item scaffold: {type, moduleId, source?}.</summary>
    public JsonObject ContentItem(string type, int? source = null)
    {
        var o = new JsonObject { ["type"] = type, ["moduleId"] = NewModuleId() };
        if (source.HasValue) o["source"] = source.Value;
        return o;
    }

    /// <summary>The topic or subtopic owning a moduleId — checks the node
    /// itself and every content item.</summary>
    public ResearchTopic? FindTopicByModuleId(string? moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId)) return null;
        foreach (var t in AllTopics())
        {
            if (t.ModuleId == moduleId) return t;
            foreach (var n in t.Content)
                if (n is JsonObject item && item["moduleId"]?.ToString() == moduleId) return t;
        }
        return null;
    }

    /// <summary>The content item bound to a module id (any depth).</summary>
    public JsonObject? FindContentItem(string? moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId)) return null;
        foreach (var t in AllTopics())
            foreach (var n in t.Content)
                if (n is JsonObject item && item["moduleId"]?.ToString() == moduleId) return item;
        return null;
    }

    public void SetArchived(ResearchTopic topic, bool archived)
        => topic.Status = archived ? "archived" : null;

    /// <summary>Drop moduleId bindings that point at modules no longer in the
    /// entry (user deleted them). Content itself is preserved.</summary>
    public void PruneOrphanedBindings(Func<string, bool> moduleExists)
    {
        foreach (var t in AllTopics())
        {
            if (t.ModuleId != null && !moduleExists(t.ModuleId)) t.ModuleId = null;
            foreach (var n in t.Content)
                if (n is JsonObject item &&
                    item["moduleId"]?.ToString() is { Length: > 0 } mid &&
                    !moduleExists(mid))
                    item.Remove("moduleId");
        }
    }

    #endregion

    #region Prompt presentation

    /// <summary>Markdown-ish rendering for review/plan prompts.</summary>
    public string FormatMarkdown()
    {
        var sb = new StringBuilder();
        foreach (var t in Topics) RenderTopic(sb, t, 0);
        return sb.ToString();
    }

    /// <summary>Compact single-line topic summary.</summary>
    public string FormatCompact()
        => string.Join(" | ", Topics.Select(t =>
            $"{t.Topic}({TextItemCount(t)} items"
            + (t.SubTopics().Any() ? ", subs: " + string.Join(",", t.SubTopics().Select(s => s.Topic)) : "")
            + ")"));

    private static void RenderTopic(StringBuilder sb, ResearchTopic t, int depth)
    {
        var indent = new string(' ', depth * 2);
        sb.Append(indent).Append("# ").AppendLine(t.Topic);
        foreach (var s in t.Texts())
            sb.Append(indent).Append("- ").AppendLine(s);
        foreach (var st in t.SubTopics())
            RenderTopic(sb, st, depth + 1);
    }

    #endregion

    #region Serialization

    /// <summary>Bump when load-time healing semantics change — files below
    /// this version flag HealedOnLoad so the entry re-renders once.</summary>
    public const int SchemaVersion = 2;

    public JsonObject ToJson()
    {
        var obj = new JsonObject { ["v"] = SchemaVersion };

        var topics = new JsonArray();
        foreach (var t in Topics) topics.Add(t.Node.DeepClone());
        obj["topics"] = topics;

        var comments = new JsonObject();
        if (!string.IsNullOrWhiteSpace(CommentsModuleId)) comments["moduleId"] = CommentsModuleId;
        var ci = new JsonArray();
        foreach (var c in Comments) ci.Add(c.ToJson());
        comments["items"] = ci;
        obj["comments"] = comments;

        var sources = new JsonObject();
        if (!string.IsNullOrWhiteSpace(SourcesModuleId)) sources["moduleId"] = SourcesModuleId;
        var si = new JsonArray();
        foreach (var s in Sources) si.Add(s.ToJson());
        sources["items"] = si;
        obj["sources"] = sources;

        var searches = new JsonArray();
        foreach (var s in Searches) searches.Add(s.ToJson());
        obj["searches"] = searches;

        return obj;
    }

    /// <summary>Parse a research JsonObject — current shape plus a legacy
    /// conversion: {topic,tab,facts[],subtopics[]} topics become
    /// title+paragraph content; flat sources[]/search[] arrays become the
    /// indexed items; flat facts' integer source ids map onto array indexes.</summary>
    public static ResearchObject FromJson(JsonObject? node)
    {
        var research = new ResearchObject();
        if (node == null) return research;

        // --- sources: new {moduleId,items[]} or legacy flat array ---
        var idToIndex = new Dictionary<int, int>();
        if (node["sources"] is JsonObject sobj)
        {
            research.SourcesModuleId = sobj["moduleId"]?.ToString();
            if (sobj["items"] is JsonArray items)
                foreach (var s in items)
                    if (s is JsonObject so && !string.IsNullOrWhiteSpace(so["url"]?.ToString()))
                        research.Sources.Add(ResearchSource.FromJson(so));
        }
        else if (node["sources"] is JsonArray sarr)
        {
            foreach (var s in sarr)
            {
                if (s is not JsonObject so || string.IsNullOrWhiteSpace(so["url"]?.ToString())) continue;
                var src = ResearchSource.FromJson(so);
                if (so["id"] is JsonValue idv && idv.TryGetValue<int>(out var oldId))
                    idToIndex[oldId] = research.Sources.Count;
                research.Sources.Add(src);
            }
        }

        // --- searches: new [{query,url,timestamp}] or legacy [{id,query}] ---
        var searchIdToIndex = new Dictionary<int, int>();
        var searchesNode = node["searches"] ?? node["search"];
        if (searchesNode is JsonArray qarr)
        {
            foreach (var q in qarr)
            {
                if (q is not JsonObject qo || string.IsNullOrWhiteSpace(qo["query"]?.ToString())) continue;
                if (qo["id"] is JsonValue idv && idv.TryGetValue<int>(out var oldId))
                    searchIdToIndex[oldId] = research.Searches.Count;
                research.Searches.Add(ResearchSearch.FromJson(qo));
            }
        }

        // Remap stored indexes: legacy used id values, new uses array index.
        foreach (var s in research.Sources)
            if (s.SearchIndex.HasValue && searchIdToIndex.Count > 0)
                s.SearchIndex = searchIdToIndex.TryGetValue(s.SearchIndex.Value, out var mapped)
                    ? mapped : null;

        // --- comments ---
        if (node["comments"] is JsonObject cobj)
        {
            research.CommentsModuleId = cobj["moduleId"]?.ToString();
            if (cobj["items"] is JsonArray items)
                foreach (var c in items)
                {
                    var rc = ResearchComment.FromJson(c as JsonObject);
                    if (rc != null) research.Comments.Add(rc);
                }
        }

        // --- topics ---
        if (node["topics"] is JsonArray topics)
        {
            foreach (var t in topics)
            {
                var topic = ResearchTopic.FromJson(t as JsonObject, research, idToIndex);
                if (topic != null) research.Topics.Add(topic);
            }
        }

        // Unversioned/stale files may still carry pre-heal rendered modules —
        // flag so the caller re-renders every topic once, then the version
        // stamp on save suppresses future passes.
        var storedVersion = node["v"] is JsonValue vv && vv.TryGetValue<int>(out var sv) ? sv : 0;

        research.FixDanglingSourceRefs();
        research.NormalizeListItems();
        research.ConsolidateGalleries();
        if (storedVersion < SchemaVersion) research.HealedOnLoad = true;
        return research;
    }

    /// <summary>Set when load-time healing changed the tree — callers expand
    /// their dirty set so rendered modules get rebuilt (e.g. stacked gallery
    /// modules collapse back into one).</summary>
    public bool HealedOnLoad { get; private set; }

    /// <summary>One gallery per topic — earlier runs stacked a separate
    /// image-gallery item per section. Merge every gallery after the first
    /// into the first (url-deduped, positional order kept) and drop it.</summary>
    public void ConsolidateGalleries()
    {
        foreach (var topic in AllTopics())
        {
            JsonObject? keep = null;
            for (var i = 0; i < topic.Content.Count; i++)
            {
                if (topic.Content[i] is not JsonObject c ||
                    c["type"]?.ToString() != "image-gallery") continue;
                if (keep == null) { keep = c; continue; }
                HealedOnLoad = true;

                if (keep["items"] is not JsonArray ki)
                {
                    ki = new JsonArray();
                    keep["items"] = ki;
                }
                var known = new HashSet<string>(
                    ki.Select(x => x is JsonObject io ? io["url"]?.ToString() : x?.ToString())
                      .Where(u => !string.IsNullOrWhiteSpace(u))!,
                    StringComparer.OrdinalIgnoreCase);
                if (c["items"] is JsonArray ci)
                    foreach (var img in ci)
                    {
                        var u = img is JsonObject io ? io["url"]?.ToString() : img?.ToString();
                        if (!string.IsNullOrWhiteSpace(u) && known.Add(u))
                            ki.Add(img?.DeepClone());
                    }
                topic.Content.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>research.json list items are plain strings — earlier dumps
    /// embedded objects ({"text":…} or nested blocks). Flatten to strings,
    /// hoisting nested item text so nothing is lost.</summary>
    public void NormalizeListItems()
    {
        foreach (var item in AllContentItems())
        {
            var type = item["type"]?.ToString();
            if (type is "bullet_list" or "numbered_list")
                item["items"] = FlattenItems(item["items"]);
            else if (type == "table")
            {
                item["headers"] = FlattenItems(item["headers"]);
                if (item["rows"] is JsonArray rows)
                    for (var i = 0; i < rows.Count; i++)
                        rows[i] = FlattenItems(rows[i]);
            }
        }
        // Drop empty parser table placeholders ({headers:[],rows:[]}).
        foreach (var t in AllTopics())
            for (var i = t.Content.Count - 1; i >= 0; i--)
                if (t.Content[i] is JsonObject c && c["type"]?.ToString() == "table"
                    && (c["headers"] as JsonArray)?.Count == 0
                    && (c["rows"] as JsonArray)?.Count == 0)
                    t.Content.RemoveAt(i);
    }

    /// <summary>Flatten an items/row array: strings pass through; objects
    /// carrying a non-null url keep a normalized {text,url} shape (rendered
    /// as anchor links downstream); objects without a url yield their text;
    /// nested item arrays hoist their elements after it.</summary>
    public static JsonArray FlattenItems(JsonNode? items)
    {
        var flat = new JsonArray();
        if (items is not JsonArray arr) return flat;
        foreach (var el in arr)
        {
            if (el is JsonObject o)
            {
                var text = o["text"]?.ToString() ?? o["item"]?.ToString()
                    ?? o["name"]?.ToString() ?? o["label"]?.ToString() ?? "";
                var url = o["url"]?.ToString() ?? o["href"]?.ToString() ?? o["link"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(url))
                    flat.Add(new JsonObject { ["text"] = text, ["url"] = url });
                else if (!string.IsNullOrWhiteSpace(text))
                    flat.Add(text);
                if (o["items"] is JsonArray nested)
                    foreach (var s in FlattenItems(nested)) flat.Add(s?.DeepClone());
            }
            else if (!string.IsNullOrWhiteSpace(el?.ToString()))
                flat.Add(el!.DeepClone());
        }
        return flat;
    }

    /// <summary>Display text for a list/table item — objects use their text
    /// field; everything else ToString().</summary>
    public static string ItemText(JsonNode? item)
        => item is JsonObject o
            ? (o["text"]?.ToString() ?? o["item"]?.ToString() ?? o["name"]?.ToString() ?? "")
            : item?.ToString() ?? "";

    #endregion

    #region Helpers

    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var lower = s.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower)
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Tab labels must be ≤ 3 words — trim or fall back to null.
    /// Rejects site-name tails ("History | Macks Lifesciences"), question
    /// fragments ("how works"), leading stopwords, repeated words and
    /// dangling conjunctions so the LLM's sloppier labels fall back to
    /// FirstWords(topic, 3) instead of rendering as-is.</summary>
    public static string? CleanTabLabel(string? label)
    {
        var s = (label ?? "").Trim();
        if (s.Length == 0) return null;
        // "X | Site Name" — pipe/dash tails are the site name, not the label.
        var cut = s.IndexOfAny(new[] { '|', '–', '—', '»', '·' });
        if (cut > 0) s = s.Substring(0, cut).Trim();
        if (s.Length == 0) return null;
        // Question fragments and leading function words are never labels.
        if (System.Text.RegularExpressions.Regex.IsMatch(s,
                @"^(?:how|what|why|when|where|whom?|which|the|a|an|and|or|but|in|on|of|to|for|is|are|was|were|do|does|did)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return null;
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kept = new List<string>();
        foreach (var w in words)
            if (kept.Count == 0 || !string.Equals(kept[kept.Count - 1], w, StringComparison.OrdinalIgnoreCase))
                kept.Add(w); // collapse "Scope Scope" → "Scope"
        while (kept.Count > 0 &&
               DanglingWords.Contains(kept[kept.Count - 1]))
            kept.RemoveAt(kept.Count - 1); // "Scope and" → "Scope"
        if (kept.Count == 0) return null;
        return string.Join(' ', kept.Take(3));
    }

    private static readonly HashSet<string> DanglingWords =
        new(StringComparer.OrdinalIgnoreCase)
        { "and", "or", "of", "the", "a", "an", "in", "on", "to", "for", "with", "by", "-" };

    public static string FirstWords(string s, int n)
        => string.Join(' ', (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(n));

    private static float Cosine(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || a.Length != b.Length) return 0f;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        if (na == 0 || nb == 0) return 0f;
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }

    #endregion
}

public class ResearchSource
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    /// <summary>Index into research.searches — the engine query that produced
    /// this source.</summary>
    public int? SearchIndex { get; set; }
    /// <summary>Filename of the parser-model research JSON stored under
    /// journals/{journalId}/{entryId}/research/ (e.g. "0001.json").</summary>
    public string? File { get; set; }

    public JsonObject ToJson()
    {
        var o = new JsonObject { ["title"] = Title, ["url"] = Url };
        if (SearchIndex.HasValue) o["search"] = SearchIndex.Value;
        if (!string.IsNullOrWhiteSpace(File)) o["file"] = File;
        return o;
    }

    public static ResearchSource FromJson(JsonObject so)
    {
        int? search = null;
        var idx = so["search"] ?? so["searchIndex"];
        if (idx is JsonValue v && v.TryGetValue<int>(out var i)) search = i;
        return new ResearchSource
        {
            Title = so["title"]?.ToString() ?? "",
            Url = so["url"]?.ToString() ?? "",
            SearchIndex = search,
            File = so["file"]?.ToString()
        };
    }
}

public class ResearchSearch
{
    public string Query { get; set; } = "";
    public string? Url { get; set; }
    public string? Timestamp { get; set; }

    public JsonObject ToJson()
    {
        var o = new JsonObject { ["query"] = Query };
        if (!string.IsNullOrWhiteSpace(Url)) o["url"] = Url;
        if (!string.IsNullOrWhiteSpace(Timestamp)) o["timestamp"] = Timestamp;
        return o;
    }

    public static ResearchSearch FromJson(JsonObject qo) => new()
    {
        Query = qo["query"]?.ToString() ?? "",
        Url = qo["url"]?.ToString(),
        Timestamp = qo["timestamp"]?.ToString()
    };
}

public class ResearchComment
{
    public string Username { get; set; } = "";
    public int? Source { get; set; }
    public string Text { get; set; } = "";
    public string? Timestamp { get; set; }
    public List<ResearchComment> Replies { get; } = new();

    public JsonObject ToJson()
    {
        var o = new JsonObject
        {
            ["username"] = Username,
            ["comment"] = Text
        };
        if (Source.HasValue) o["source"] = Source.Value;
        if (!string.IsNullOrWhiteSpace(Timestamp)) o["timestamp"] = Timestamp;
        var r = new JsonArray();
        foreach (var reply in Replies) r.Add(reply.ToJson());
        o["replies"] = r;
        return o;
    }

    public static ResearchComment? FromJson(JsonObject? node)
    {
        if (node == null) return null;
        var text = node["comment"]?.ToString() ?? node["text"]?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return null;
        int? source = null;
        if (node["source"] is JsonValue sv && sv.TryGetValue<int>(out var s)) source = s;
        var rc = new ResearchComment
        {
            Username = node["username"]?.ToString() ?? node["author"]?.ToString() ?? "",
            Source = source,
            Text = text,
            Timestamp = node["timestamp"]?.ToString()
        };
        if (node["replies"] is JsonArray replies)
            foreach (var r in replies)
            {
                var child = FromJson(r as JsonObject);
                if (child != null) rc.Replies.Add(child);
            }
        return rc;
    }
}

/// <summary>
/// A topic (or nested subtopic) node — wraps its JsonObject so the in-memory
/// model IS the serialized one. Root topics serialize {title,content[]};
/// subtopic items serialize {tab,title,moduleId,content[]}.
/// </summary>
public class ResearchTopic
{
    public JsonObject Node { get; }

    public ResearchTopic(string title, bool isSubtopic = false)
    {
        Node = new JsonObject { ["title"] = title, ["content"] = new JsonArray() };
        IsSubtopic = isSubtopic;
    }

    /// <summary>Wrap an existing topic/subtopic JSON node.</summary>
    public ResearchTopic(JsonObject node, bool isSubtopic = false)
    {
        Node = node;
        IsSubtopic = isSubtopic;
        Node["title"] ??= "";
        if (Node["content"] is not JsonArray) Node["content"] = new JsonArray();
    }

    public bool IsSubtopic { get; }
    public string Topic
    {
        get => Node["title"]?.ToString() ?? "";
        set => Node["title"] = value;
    }
    public string? Tab
    {
        get => Node["tab"]?.ToString();
        set { if (string.IsNullOrWhiteSpace(value)) Node.Remove("tab"); else Node["tab"] = value; }
    }
    public string? ModuleId
    {
        get => Node["moduleId"]?.ToString();
        set { if (string.IsNullOrWhiteSpace(value)) Node.Remove("moduleId"); else Node["moduleId"] = value; }
    }
    public string? Status
    {
        get => Node["status"]?.ToString();
        set { if (string.IsNullOrWhiteSpace(value)) Node.Remove("status"); else Node["status"] = value; }
    }
    public bool Archived => Status == "archived";
    public bool Synthetic
    {
        get => Node["synthetic"]?.GetValue<bool>() ?? false;
        set { if (value) Node["synthetic"] = true; else Node.Remove("synthetic"); }
    }

    public JsonArray Content => (JsonArray)Node["content"]!;

    /// <summary>Subtopics = the items of the single "sub_topics" content item.</summary>
    public IEnumerable<ResearchTopic> SubTopics()
    {
        foreach (var n in Content)
        {
            if (n is not JsonObject item || item["type"]?.ToString() != "sub_topics") continue;
            if (item["items"] is not JsonArray items) continue;
            foreach (var s in items)
                if (s is JsonObject so) yield return new ResearchTopic(so, isSubtopic: true);
        }
    }

    /// <summary>The items array of the sub_topics content item, created on
    /// demand with its own moduleId.</summary>
    public JsonArray SubTopicsContainerItems()
    {
        foreach (var n in Content)
            if (n is JsonObject item && item["type"]?.ToString() == "sub_topics" &&
                item["items"] is JsonArray arr)
                return arr;
        var container = new JsonObject
        {
            ["type"] = "sub_topics",
            ["moduleId"] = ResearchObject.NewModuleId(),
            ["items"] = new JsonArray()
        };
        Content.Add(container);
        return (JsonArray)container["items"]!;
    }

    /// <summary>The moduleId of the sub_topics container item, if present.</summary>
    public string? SubTopicsModuleId()
    {
        foreach (var n in Content)
            if (n is JsonObject item && item["type"]?.ToString() == "sub_topics")
                return item["moduleId"]?.ToString();
        return null;
    }

    public IEnumerable<ResearchTopic> AllDescendants()
    {
        foreach (var st in SubTopics())
        {
            yield return st;
            foreach (var d in st.AllDescendants()) yield return d;
        }
    }

    /// <summary>Content items of a given type in this topic (not nested).</summary>
    public IEnumerable<JsonObject> ContentItems(string type)
    {
        foreach (var n in Content)
            if (n is JsonObject item && item["type"]?.ToString() == type) yield return item;
    }

    /// <summary>All text payload in this topic — paragraphs and list items.</summary>
    public IEnumerable<string> Texts()
    {
        foreach (var n in Content)
        {
            if (n is not JsonObject item) continue;
            switch (item["type"]?.ToString())
            {
                case "paragraph":
                    if (item["text"]?.ToString() is { Length: > 0 } p) yield return p;
                    break;
                case "bullet_list":
                case "numbered_list":
                    if (item["items"] is JsonArray arr)
                        foreach (var x in arr)
                            if (ResearchObject.ItemText(x) is { Length: > 0 } li) yield return li;
                    break;
            }
        }
    }

    /// <summary>Parse one topic node — current {title,content} or legacy
    /// {topic,tab,facts[],subtopics[]}. Legacy facts become paragraph items;
    /// legacy subtopics become a sub_topics container.</summary>
    public static ResearchTopic? FromJson(JsonObject? node, ResearchObject owner,
        Dictionary<int, int>? sourceIdMap = null)
    {
        if (node == null) return null;

        int? Remap(JsonNode? s)
        {
            if (s is not JsonValue v || !v.TryGetValue<int>(out var i)) return null;
            if (sourceIdMap != null && sourceIdMap.Count > 0)
                return sourceIdMap.TryGetValue(i, out var mapped) ? mapped : null;
            return i;
        }

        // New shape
        if (node["content"] is JsonArray || node["title"] != null)
        {
            var t = new ResearchTopic(node, isSubtopic: false);
            foreach (var item in t.Content)
                if (item is JsonObject io && io["type"]?.ToString() == "sub_topics" &&
                    io["items"] is JsonArray)
                    break; // structure is already nested correctly
            return t;
        }

        // Legacy shape: {topic, tab, moduleId, tabId, facts[], subtopics[], status, synthetic}
        var title = node["topic"]?.ToString();
        if (string.IsNullOrWhiteSpace(title)) return null;
        var topic = new ResearchTopic(title);
        if (node["status"]?.ToString() == "archived") topic.Status = "archived";
        if (node["synthetic"]?.GetValue<bool>() == true) topic.Synthetic = true;

        var titleItem = new JsonObject
        {
            ["type"] = "title",
            ["moduleId"] = node["moduleId"]?.ToString() ?? ResearchObject.NewModuleId(),
            ["text"] = title
        };
        topic.Content.Add(titleItem);

        if (node["facts"] is JsonArray facts)
        {
            foreach (var f in facts)
            {
                var text = f is JsonObject fo ? fo["text"]?.ToString() : f?.ToString();
                if (string.IsNullOrWhiteSpace(text)) continue;
                var p = new JsonObject
                {
                    ["type"] = "paragraph",
                    ["moduleId"] = ResearchObject.NewModuleId(),
                    ["text"] = text
                };
                var src = Remap((f as JsonObject)?["source"]);
                if (src.HasValue) p["source"] = src.Value;
                topic.Content.Add(p);
            }
        }

        if (node["subtopics"] is JsonArray subs && subs.Count > 0)
        {
            var container = topic.SubTopicsContainerItems();
            if (node["tabId"]?.ToString() is { Length: > 0 } tabId)
                foreach (var c in topic.Content)
                    if (c is JsonObject ci && ci["type"]?.ToString() == "sub_topics")
                        ci["moduleId"] = tabId;
            foreach (var s in subs)
            {
                var sub = FromJson(s as JsonObject, owner, sourceIdMap);
                if (sub == null) continue;
                sub.Node["tab"] = (s as JsonObject)?["tab"]?.ToString()
                    ?? CleanTabLabelStatic(sub.Topic);
                container.Add(sub.Node);
            }
            if (container.Count == 0)
                for (int i = topic.Content.Count - 1; i >= 0; i--)
                    if (topic.Content[i] is JsonObject ci && ci["type"]?.ToString() == "sub_topics")
                        topic.Content.RemoveAt(i);
        }
        return topic;
    }

    private static string CleanTabLabelStatic(string title)
        => ResearchObject.CleanTabLabel(title) ?? ResearchObject.FirstWords(title, 3);
}
