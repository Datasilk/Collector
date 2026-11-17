using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Collector.Chat;

/// <summary>
/// The "research" object inside a journal entry's JSON — the knowledge layer
/// kept separate from "modules" (the presentation layer). Sources carry
/// provenance; topics/subtopics organize extracted facts; moduleId/tabId
/// bind topics to the modules that render them. Deterministic code owns
/// merging, dedup and placement — the LLM only supplies facts and prose.
/// </summary>
public class ResearchObject
{
    public const int MaxTopicDepth = 2; // root topic → subtopic → level-2 subtopic
    public const int MaxFactsPerTopic = 30;
    public const int MaxFactsGlobal = 200;

    public List<ResearchSource> Sources { get; set; } = new();
    public List<ResearchTopic> Topics { get; set; } = new();

    #region Sources

    /// <summary>
    /// Resolve an existing source id by URL or register a new one
    /// (ids are incremental from 1 and never reused).
    /// Returns the id and whether a NEW source row was created.
    /// </summary>
    public (int Id, bool Created) RegisterSource(string url, string title)
    {
        url = (url ?? "").Trim();
        title = (title ?? "").Trim();
        var existing = Sources.FirstOrDefault(s =>
            s.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.Title) && !string.IsNullOrWhiteSpace(title))
                existing.Title = title;
            return (existing.Id, false);
        }

        var nextId = (Sources.Count == 0 ? 0 : Sources.Max(s => s.Id)) + 1;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : url;
        }
        Sources.Add(new ResearchSource { Id = nextId, Title = title, Url = url });
        return (nextId, true);
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

    /// <summary>Delete every fact whose source id matches — used before
    /// re-merging a re-scraped page (replace semantics).</summary>
    public void RemoveFactsBySource(int sourceId)
    {
        foreach (var t in AllTopics())
            t.Facts.RemoveAll(f => f.Source == sourceId);
    }

    /// <summary>Null out fact source refs that no longer resolve.</summary>
    public void FixDanglingSourceRefs()
    {
        var ids = Sources.Select(s => s.Id).ToHashSet();
        foreach (var t in AllTopics())
        {
            foreach (var f in t.Facts)
            {
                if (f.Source.HasValue && !ids.Contains(f.Source.Value))
                    f.Source = null;
            }
        }
    }

    #endregion

    #region Topics

    /// <summary>All topics in the tree, depth-first (roots, then children).</summary>
    public IEnumerable<ResearchTopic> AllTopics()
    {
        foreach (var t in Topics)
        {
            yield return t;
            foreach (var st in t.AllDescendants())
                yield return st;
        }
    }

    public IEnumerable<ResearchFact> AllFacts()
        => AllTopics().SelectMany(t => t.Facts);

    public int TotalFacts => AllFacts().Count();

    /// <summary>
    /// Global topic lookup — an incoming title is matched against EVERY topic
    /// in the tree so duplicates can't appear at any level. Match order:
    /// exact (case-insensitive) → normalized → (embedding handled by the
    /// async merger). Returns the best match or null.
    /// </summary>
    public ResearchTopic? FindTopic(string title)
    {
        var norm = Normalize(title);
        foreach (var t in AllTopics())
        {
            if (t.Topic.Equals(title, StringComparison.OrdinalIgnoreCase))
                return t;
        }
        foreach (var t in AllTopics())
        {
            if (Normalize(t.Topic) == norm)
                return t;
        }
        return null;
    }

    /// <summary>Find the topic bound to a given module id (text-editor) or
    /// tabs-module id (tabId).</summary>
    public ResearchTopic? FindTopicByModuleId(string moduleId)
        => AllTopics().FirstOrDefault(t => t.ModuleId == moduleId || t.TabId == moduleId);

    /// <summary>
    /// Third-tier topic match — embedding similarity on titles. Same
    /// threshold infra as fact dedup (&gt;0.85); ambiguous ties go to the
    /// deeper (more specific) topic. Returns null when embeddings are
    /// unavailable or nothing clears the bar.
    /// </summary>
    private async Task<ResearchTopic?> FindTopicByEmbeddingAsync(
        string title, Func<List<string>, Task<float[][]>?>? embed)
    {
        if (embed == null) return null;
        var candidates = AllTopics().ToList();
        if (candidates.Count == 0) return null;
        try
        {
            var texts = candidates.Select(t => t.Topic).Concat(new[] { title }).ToList();
            var vectors = await embed(texts);
            if (vectors == null || vectors.Length != texts.Count) return null;
            var needle = vectors[^1];
            ResearchTopic? best = null;
            var bestSim = 0.85f;
            for (int i = 0; i < candidates.Count; i++)
            {
                var sim = Cosine(needle, vectors[i]);
                // On a tie prefer the deeper node — the more specific topic.
                if (sim > bestSim || (Math.Abs(sim - bestSim) < 0.001f && best != null && candidates[i].Depth > best.Depth))
                { best = candidates[i]; bestSim = sim; }
            }
            return best;
        }
        catch { return null; }
    }

    /// <summary>Mark a topic and (optionally) its subtopics archived/active.</summary>
    public void SetArchived(ResearchTopic topic, bool archived)
    {
        topic.Status = archived ? "archived" : null;
    }

    /// <summary>Compact title list for LLM context — "# Title" / "## Sub" /
    /// "### Sub-sub" headings only, no facts (the shared formatter with
    /// facts disabled).</summary>
    public string FormatMarkdown(bool includeFacts = false, int factBudget = int.MaxValue)
        => ResearchFormatter.Format(Topics, Sources, includeFacts, factBudget);

    /// <summary>Compact form for conversational-edit prompts: topic titles
    /// with fact counts, no fact text (budget).</summary>
    public string FormatCompact()
    {
        var sb = new StringBuilder();
        foreach (var t in Topics)
        {
            sb.AppendLine($"# {t.Topic} ({t.Facts.Count} facts){(t.Status == "archived" ? " [archived]" : "")}");
            foreach (var st in t.Subtopics)
            {
                sb.AppendLine($"## {st.Topic} ({st.Facts.Count} facts){(st.Status == "archived" ? " [archived]" : "")}");
                foreach (var s2 in st.Subtopics)
                    sb.AppendLine($"### {s2.Topic} ({s2.Facts.Count} facts){(s2.Status == "archived" ? " [archived]" : "")}");
            }
        }
        return sb.ToString();
    }

    #endregion

    #region Merge

    /// <summary>
    /// Merge a flat extraction [{topic, tab?, subtopic?, facts[]}] into the
    /// tree. Global fuzzy topic matching prevents duplicates at any level;
    /// incoming matches adopt the EXISTING title/tab (identity stability).
    /// Facts are deduped globally — normalized string first, then (when an
    /// embedder is provided) cosine similarity on batch embeddings:
    /// sim > 0.85 → skip (paraphrase); below → append (contradictions kept).
    /// Topics touched are returned as dirty for the module writer.
    /// </summary>
    public async Task<List<ResearchTopic>> MergeExtractionAsync(
        List<ExtractedTopic> items,
        int sourceId,
        Func<List<string>, Task<float[][]>? >? embed = null)
    {
        var dirty = new List<ResearchTopic>();

        foreach (var item in items ?? new List<ExtractedTopic>())
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Topic)) continue;
            var facts = (item.Facts ?? new List<string>())
                .Select(f => (f ?? "").Trim())
                .Where(f => f.Length > 0)
                .ToList();
            if (facts.Count == 0 && string.IsNullOrWhiteSpace(item.Subtopic)) continue;

            // Resolve (or create) the parent topic — global match first so an
            // incoming root-level title matching an existing SUBtopic redirects
            // there rather than duplicating it at root.
            ResearchTopic? target;
            var matched = FindTopic(item.Topic)
                          ?? await FindTopicByEmbeddingAsync(item.Topic, embed);
            if (matched != null)
            {
                target = matched;
            }
            else
            {
                target = new ResearchTopic { Topic = item.Topic.Trim() };
                Topics.Add(target);
            }

            // Subtopic level — again matched globally.
            if (!string.IsNullOrWhiteSpace(item.Subtopic))
            {
                var subMatched = FindTopic(item.Subtopic)
                                 ?? await FindTopicByEmbeddingAsync(item.Subtopic, embed);
                if (subMatched != null && subMatched != target)
                {
                    target = subMatched;
                }
                else if (subMatched == null)
                {
                    var sub = new ResearchTopic
                    {
                        Topic = item.Subtopic.Trim(),
                        Tab = CleanTabLabel(item.Tab) ?? FirstWords(item.Subtopic, 3)
                    };
                    // Place under the resolved parent when the parent is a
                    // root topic; otherwise hang it at root (a subtopic title
                    // can only exist inside a parent we know).
                    if (Topics.Contains(target)) target.Subtopics.Add(sub);
                    else Topics.Add(sub);
                    target = sub;
                }
                // subMatched == target: the "subtopic" name matched its own
                // parent — facts just land on the parent.
            }

            var added = await MergeFactsAsync(target, facts, sourceId, embed);
            if (added > 0 && !dirty.Contains(target)) dirty.Add(target);
            if (added == 0 && target.Facts.Count > 0 && facts.Count > 0 && !dirty.Contains(target))
            {
                // Facts were extracted but all deduped away — no dirty flag.
            }
        }

        return dirty;
    }

    /// <summary>
    /// Append facts to a topic after global dedup. Returns the count added.
    /// </summary>
    private async Task<int> MergeFactsAsync(
        ResearchTopic target,
        List<string> facts,
        int sourceId,
        Func<List<string>, Task<float[][]>?>? embed)
    {
        var added = 0;

        // Tier 1 — normalized string dedup across the WHOLE entry.
        var existingNorm = AllFacts()
            .Select(f => Normalize(f.Text))
            .ToHashSet(StringComparer.Ordinal);

        var candidates = new List<string>();
        foreach (var f in facts)
        {
            var n = Normalize(f);
            if (n.Length == 0) continue;
            if (existingNorm.Contains(n)) continue;      // exact paraphrase-free dup
            if (candidates.Any(c => Normalize(c) == n)) continue; // dup within batch
            candidates.Add(f);
        }
        if (candidates.Count == 0) return 0;

        // Tier 2 — semantic dedup via batch embeddings (in-memory only).
        var existingTexts = AllFacts().Select(f => f.Text).ToList();
        if (embed != null && existingTexts.Count > 0)
        {
            try
            {
                var all = existingTexts.Concat(candidates).ToList();
                var vectors = await embed(all);
                if (vectors != null && vectors.Length == all.Count)
                {
                    var kept = new List<string>();
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var v = vectors[existingTexts.Count + i];
                        var maxSim = 0f;
                        for (int j = 0; j < existingTexts.Count; j++)
                            maxSim = Math.Max(maxSim, Cosine(v, vectors[j]));
                        // also compare against already-kept new candidates
                        for (int k = 0; k < kept.Count; k++)
                        {
                            var kv = vectors[existingTexts.Count + candidates.IndexOf(kept[k])];
                            maxSim = Math.Max(maxSim, Cosine(v, kv));
                        }
                        if (maxSim <= 0.85f) kept.Add(candidates[i]);
                    }
                    candidates = kept;
                }
            }
            catch { /* embedding unavailable → string dedup only */ }
        }

        foreach (var f in candidates)
        {
            if (target.Facts.Count >= MaxFactsPerTopic) break;
            if (TotalFacts >= MaxFactsGlobal) break;
            target.Facts.Add(new ResearchFact { Text = f, Source = sourceId });
            added++;
        }
        return added;
    }

    #endregion

    #region Serialization

    public JsonObject ToJson()
    {
        var obj = new JsonObject();
        if (Sources.Count > 0)
        {
            var arr = new JsonArray();
            foreach (var s in Sources)
                arr.Add(new JsonObject { ["id"] = s.Id, ["title"] = s.Title, ["url"] = s.Url });
            obj["sources"] = arr;
        }
        var topics = new JsonArray();
        foreach (var t in Topics) topics.Add(t.ToJson());
        obj["topics"] = topics;
        return obj;
    }

    /// <summary>Parse a "research" JsonObject. Tolerates malformed content —
    /// bad rows are skipped, depth > MaxTopicDepth is flattened by dropping
    /// the deepest subtopics (facts are re-homed to the level-2 parent).</summary>
    public static ResearchObject FromJson(JsonObject? node)
    {
        var research = new ResearchObject();
        if (node == null) return research;

        if (node["sources"] is JsonArray sources)
        {
            foreach (var s in sources)
            {
                if (s is not JsonObject so) continue;
                var id = so["id"]?.GetValue<int>() ?? 0;
                var url = so["url"]?.ToString() ?? "";
                if (id <= 0 || string.IsNullOrWhiteSpace(url)) continue;
                research.Sources.Add(new ResearchSource
                {
                    Id = id,
                    Title = so["title"]?.ToString() ?? "",
                    Url = url
                });
            }
        }

        if (node["topics"] is JsonArray topics)
        {
            foreach (var t in topics)
            {
                var topic = ResearchTopic.FromJson(t as JsonObject, depth: 0);
                if (topic != null) research.Topics.Add(topic);
            }
        }

        research.FixDanglingSourceRefs();
        return research;
    }

    /// <summary>Drop moduleId/tabId bindings that point at modules no longer
    /// present (user deleted them). Topics + facts are preserved.</summary>
    public void PruneOrphanedBindings(Func<string, bool> moduleExists)
    {
        foreach (var t in AllTopics())
        {
            if (t.ModuleId != null && !moduleExists(t.ModuleId)) t.ModuleId = null;
            if (t.TabId != null && !moduleExists(t.TabId)) t.TabId = null;
        }
    }

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

    private static float Cosine(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || a.Length != b.Length) return 0f;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        if (na == 0 || nb == 0) return 0f;
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }

    /// <summary>Tab labels must be ≤ 3 words — trim or fall back to null.</summary>
    public static string? CleanTabLabel(string? label)
    {
        var s = (label ?? "").Trim();
        if (s.Length == 0) return null;
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 3 ? s : string.Join(' ', words.Take(3));
    }

    public static string FirstWords(string s, int n)
        => string.Join(' ', (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(n));

    #endregion
}

public class ResearchSource
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}

public class ResearchFact
{
    public string Text { get; set; } = "";
    public int? Source { get; set; }

    public JsonObject ToJson()
    {
        var o = new JsonObject { ["text"] = Text };
        if (Source.HasValue) o["source"] = Source.Value;
        return o;
    }

    public static ResearchFact? FromJson(JsonObject? node)
    {
        var text = node?["text"]?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return null;
        int? source = null;
        if (node!["source"] is JsonValue sv && sv.TryGetValue<int>(out var sid)) source = sid;
        return new ResearchFact { Text = text, Source = source };
    }
}

public class ResearchTopic
{
    public string Topic { get; set; } = "";
    public string? Tab { get; set; }
    public string? TabId { get; set; }       // id of the PARENT tabs module
    public string? ModuleId { get; set; }    // id of the bound text-editor
    public string? Status { get; set; }      // only ever "archived"
    /// <summary>True for the backfill-created placeholder root named after the
    /// entry — renamed in lockstep when the entry title changes.</summary>
    public bool Synthetic { get; set; }
    public List<ResearchFact> Facts { get; set; } = new();
    public List<ResearchTopic> Subtopics { get; set; } = new();

    public bool Archived => Status == "archived";

    public IEnumerable<ResearchTopic> AllDescendants()
    {
        foreach (var st in Subtopics)
        {
            yield return st;
            foreach (var d in st.AllDescendants()) yield return d;
        }
    }

    /// <summary>Depth of this node inside the tree (0 = root). Set on parse.</summary>
    public int Depth { get; internal set; }

    public JsonObject ToJson()
    {
        var o = new JsonObject { ["topic"] = Topic };
        if (!string.IsNullOrEmpty(Tab)) o["tab"] = Tab;
        if (!string.IsNullOrEmpty(TabId)) o["tabId"] = TabId;
        if (!string.IsNullOrEmpty(ModuleId)) o["moduleId"] = ModuleId;
        if (Status == "archived") o["status"] = "archived";
        if (Synthetic) o["synthetic"] = true;
        var facts = new JsonArray();
        foreach (var f in Facts) facts.Add(f.ToJson());
        o["facts"] = facts;
        if (Subtopics.Count > 0)
        {
            var subs = new JsonArray();
            foreach (var st in Subtopics) subs.Add(st.ToJson());
            o["subtopics"] = subs;
        }
        return o;
    }

    public static ResearchTopic? FromJson(JsonObject? node, int depth)
    {
        var title = node?["topic"]?.ToString();
        if (string.IsNullOrWhiteSpace(title)) return null;
        var t = new ResearchTopic
        {
            Topic = title,
            Tab = node!["tab"]?.ToString(),
            TabId = node!["tabId"]?.ToString(),
            ModuleId = node!["moduleId"]?.ToString(),
            Status = node!["status"]?.ToString() == "archived" ? "archived" : null,
            Synthetic = node!["synthetic"]?.GetValue<bool>() == true,
            Depth = depth
        };
        if (node!["facts"] is JsonArray facts)
        {
            foreach (var f in facts)
            {
                var fact = ResearchFact.FromJson(f as JsonObject);
                if (fact != null) t.Facts.Add(fact);
            }
        }
        if (depth < ResearchObject.MaxTopicDepth && node!["subtopics"] is JsonArray subs)
        {
            foreach (var s in subs)
            {
                var st = FromJson(s as JsonObject, depth + 1);
                if (st != null) t.Subtopics.Add(st);
            }
        }
        else if (node!["subtopics"] is JsonArray tooDeep)
        {
            // Depth violation — flatten: lift the deepest facts into this node.
            foreach (var s in tooDeep)
            {
                var st = FromJson(s as JsonObject, 0);
                if (st != null)
                {
                    t.Facts.AddRange(st.Facts);
                    foreach (var deeper in st.Subtopics) t.Facts.AddRange(deeper.Facts);
                }
            }
        }
        return t;
    }
}

/// <summary>One item of the LLM's flat categorization output.</summary>
public class ExtractedTopic
{
    public string Topic { get; set; } = "";
    public string? Tab { get; set; }
    public string? Subtopic { get; set; }
    public List<string> Facts { get; set; } = new();
}

/// <summary>
/// The ONE shared formatter for topics+facts sent to any LLM: topics use
/// `#` prefixes scaled to depth (`#` root, `##` subtopic, `###` level-2),
/// facts are `- ` bullet lists beneath their topic.
/// </summary>
public static class ResearchFormatter
{
    public static string Format(
        List<ResearchTopic> topics,
        List<ResearchSource>? sources = null,
        bool includeFacts = true,
        int factBudget = int.MaxValue)
    {
        var sb = new StringBuilder();
        var budget = factBudget;
        foreach (var t in topics) AppendTopic(sb, t, "#", includeFacts, ref budget);
        return sb.ToString();
    }

    /// <summary>Format a single topic (at its natural depth prefix) with its
    /// facts — the per-topic block the module writer feeds the prose LLM.</summary>
    public static string FormatTopic(ResearchTopic topic, int depth)
    {
        var prefix = new string('#', Math.Clamp(depth + 1, 1, 3));
        var sb = new StringBuilder();
        var budget = int.MaxValue;
        AppendTopic(sb, topic, prefix, includeFacts: true, ref budget, recurseChildren: false);
        return sb.ToString();
    }

    private static void AppendTopic(
        StringBuilder sb, ResearchTopic t, string prefix,
        bool includeFacts, ref int budget, bool recurseChildren = true)
    {
        sb.AppendLine($"{prefix} {t.Topic}{(t.Status == "archived" ? " [archived]" : "")}");
        if (includeFacts)
        {
            foreach (var f in t.Facts)
            {
                if (budget-- <= 0) { sb.AppendLine("- …(facts truncated)"); break; }
                sb.AppendLine($"- {f.Text}");
            }
        }
        if (recurseChildren)
        {
            var childPrefix = prefix.Length >= 3 ? "###" : prefix + "#";
            foreach (var st in t.Subtopics)
                AppendTopic(sb, st, childPrefix, includeFacts, ref budget, recurseChildren);
        }
    }
}
