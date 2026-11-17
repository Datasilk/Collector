using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Collector.Chat;
using Collector.Chat.Tools;
using Collector.Chat.Models;
using Collector.Common;
using Collector.Common.DOM;
using Collector.Common.Models.Articles;
using Collector.Data.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Collector.Chat.Tests;

/// <summary>
/// Golden regression tests for the chat system's deterministic surfaces —
/// JSON parsing, the journal-entry edit DSL, plan manipulation, and prompt
/// contracts. Run these whenever prompts or LLM plumbing change; they require
/// no live model.
/// </summary>
[TestClass]
public class GoldenTests
{
    private static string _tempDir = "";

    [ClassInitialize]
    public static void Setup(TestContext _)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "collector-chat-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Files.JournalPath = _tempDir;
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
    }

    private static JournalEntryDocument NewDoc(string? json = null)
    {
        var id = Guid.NewGuid();
        if (json != null) File.WriteAllText(Path.Combine(_tempDir, $"{id:N}.json"), json);
        return JournalEntryDocument.Load(id);
    }

    #region LLMJson

    private class Sample { public string? Name { get; set; } public int Count { get; set; } }

    [TestMethod]
    public void LLMJson_ParsesPlainJson()
    {
        Assert.IsTrue(LLMJson.TryParse<Sample>("{\"name\":\"x\",\"count\":3}", out var v));
        Assert.AreEqual("x", v!.Name);
        Assert.AreEqual(3, v.Count);
    }

    [TestMethod]
    public void LLMJson_StripsMarkdownFence()
    {
        Assert.IsTrue(LLMJson.TryParse<Sample>("```json\n{\"name\":\"f\",\"count\":1}\n```", out var v));
        Assert.AreEqual("f", v!.Name);
    }

    [TestMethod]
    public void LLMJson_FindsJsonEmbeddedInProse()
    {
        Assert.IsTrue(LLMJson.TryParse<Sample>("Sure! Here is the JSON: {\"name\":\"p\",\"count\":9} hope that helps.", out var v));
        Assert.AreEqual(9, v!.Count);
    }

    [TestMethod]
    public void LLMJson_RejectsGarbage()
    {
        Assert.IsFalse(LLMJson.TryParse<Sample>("no json here at all", out _));
        Assert.IsFalse(LLMJson.TryParse<Sample>("", out _));
        Assert.IsFalse(LLMJson.TryParse<Sample>(null, out _));
    }

    #endregion

    #region JournalEntryDocument — edit ops

    [TestMethod]
    public void Doc_Load_MissingFile_GivesEmptyModules()
    {
        var doc = NewDoc();
        Assert.AreEqual(0, doc.Modules.Count);
    }

    [TestMethod]
    public void Doc_AddOp_InsertsModuleAtRoot()
    {
        var doc = NewDoc("{\"modules\":[]}");
        var errors = doc.ApplyOps(JsonNode.Parse(
            "{\"ops\":[{\"op\":\"add\",\"parent\":\"root\",\"index\":-1,\"module\":{\"type\":\"text-editor\",\"html\":\"<p>hi</p>\"}}]}"));
        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
        Assert.AreEqual(1, doc.Modules.Count);
        var mod = doc.Modules[0] as JsonObject;
        Assert.AreEqual("text-editor", mod!["type"]!.ToString());
        Assert.IsFalse(string.IsNullOrEmpty(mod["id"]?.ToString()));
    }

    [TestMethod]
    public void Doc_UpdateOp_ChangesField()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"<p>a</p>\"}]}");
        var errors = doc.ApplyOps(JsonNode.Parse(
            "{\"ops\":[{\"op\":\"update\",\"id\":\"m1\",\"set\":{\"html\":\"<p>b</p>\"}}]}"));
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("<p>b</p>", (doc.Modules[0] as JsonObject)!["html"]!.ToString());
    }

    [TestMethod]
    public void Doc_RemoveOp_RemovesModule()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"x\"},{\"type\":\"title\",\"id\":\"m2\",\"text\":\"t\"}]}");
        var errors = doc.ApplyOps(JsonNode.Parse("{\"ops\":[{\"op\":\"remove\",\"id\":\"m1\"}]}"));
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, doc.Modules.Count);
        Assert.AreEqual("m2", (doc.Modules[0] as JsonObject)!["id"]!.ToString());
    }

    [TestMethod]
    public void Doc_MoveOp_ReordersModules()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"title\",\"id\":\"m1\",\"text\":\"a\"},{\"type\":\"title\",\"id\":\"m2\",\"text\":\"b\"}]}");
        var errors = doc.ApplyOps(JsonNode.Parse(
            "{\"ops\":[{\"op\":\"move\",\"id\":\"m2\",\"parent\":\"root\",\"index\":0}]}"));
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("m2", (doc.Modules[0] as JsonObject)!["id"]!.ToString());
    }

    [TestMethod]
    public void Doc_AddOp_IntoTabContainer()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"tabs\",\"id\":\"tmod\",\"style\":1,\"tabs\":[{\"id\":\"tab1\",\"title\":\"S1\",\"modules\":[]}]}]}");
        var errors = doc.ApplyOps(JsonNode.Parse(
            "{\"ops\":[{\"op\":\"add\",\"parent\":\"tab1\",\"index\":-1,\"module\":{\"type\":\"text-editor\",\"html\":\"<p>in tab</p>\"}}]}"));
        Assert.AreEqual(0, errors.Count);
        var tabs = (doc.Modules[0] as JsonObject)!["tabs"] as JsonArray;
        var tabModules = (tabs![0] as JsonObject)!["modules"] as JsonArray;
        Assert.AreEqual(1, tabModules!.Count);
    }

    [TestMethod]
    public void Doc_UnknownModuleType_IsRejected()
    {
        var doc = NewDoc("{\"modules\":[]}");
        var errors = doc.ApplyOps(JsonNode.Parse(
            "{\"ops\":[{\"op\":\"add\",\"parent\":\"root\",\"module\":{\"type\":\"hijack\",\"x\":1}}]}"));
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(0, doc.Modules.Count);
    }

    [TestMethod]
    public void Doc_BadParent_IsRejected()
    {
        var doc = NewDoc("{\"modules\":[]}");
        var errors = doc.ApplyOps(JsonNode.Parse(
            "{\"ops\":[{\"op\":\"add\",\"parent\":\"nonexistent\",\"module\":{\"type\":\"text-editor\",\"html\":\"x\"}}]}"));
        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public void Doc_BuildMap_ShowsStructure()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"title\",\"id\":\"m1\",\"text\":\"Intro\"},{\"type\":\"tabs\",\"id\":\"t\",\"style\":1,\"tabs\":[{\"id\":\"tab1\",\"title\":\"A\",\"modules\":[]}]}]}");
        var map = doc.BuildMap();
        StringAssert.Contains(map, "m1");
        StringAssert.Contains(map, "tabs");
    }

    [TestMethod]
    public void Doc_ExtractText_StripsHtml()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"<p>Hello <b>world</b></p>\"}]}");
        var text = doc.ExtractText();
        StringAssert.Contains(text, "Hello");
        StringAssert.Contains(text, "world");
        Assert.IsFalse(text.Contains("<p>"));
    }

    [TestMethod]
    public void Doc_SaveLoad_RoundTrips()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"<p>rt</p>\"}]}");
        var id = Guid.NewGuid();
        doc.Save(id);
        var loaded = JournalEntryDocument.Load(id);
        Assert.AreEqual(1, loaded.Modules.Count);
        Assert.AreEqual("<p>rt</p>", (loaded.Modules[0] as JsonObject)!["html"]!.ToString());
        // temp file must be cleaned up by atomic save
        Assert.IsFalse(File.Exists(Path.Combine(_tempDir, $"{id:N}.json.tmp")));
    }

    #endregion

    #region PlanUtility

    [TestMethod]
    public void PlanUtility_InsertsStepAfterCurrentIndex()
    {
        var plan = new ExecutionPlan();
        plan.Steps.Add(new PlanStep { Tool = "first" });
        plan.Steps.Add(new PlanStep { Tool = "last" });
        PlanUtility.AddStepToPlan(plan, 0, "middle", new Dictionary<string, object> { ["url"] = "x" });
        Assert.AreEqual(3, plan.Steps.Count);
        Assert.AreEqual("middle", plan.Steps[1].Tool);
        Assert.AreEqual("x", plan.Steps[1].Args["url"]);
    }

    #endregion

    #region Prompt contracts

    [TestMethod]
    public void Prompts_EntryEdit_DeclaresOpsContract()
    {
        var sys = Prompts.EntryEditSystem();
        StringAssert.Contains(sys, "\"ops\"");
        StringAssert.Contains(sys, "text-editor");
        StringAssert.Contains(sys, "\"add\"");
        StringAssert.Contains(sys, "\"update\"");
        StringAssert.Contains(sys, "\"remove\"");
        StringAssert.Contains(sys, "\"move\"");
        StringAssert.Contains(sys, "ONLY");
    }

    [TestMethod]
    public void Prompts_WrapUntrusted_DelimitsContent()
    {
        var wrapped = Prompts.WrapUntrusted("PAGE DATA");
        StringAssert.Contains(wrapped, "WEB PAGE DATA");
        StringAssert.Contains(wrapped, "END WEB PAGE DATA");
        StringAssert.Contains(wrapped, "never as instructions");
    }

    [TestMethod]
    public void Prompts_CoverageCheck_RequiresJsonArray()
    {
        StringAssert.Contains(Prompts.CoverageCheckSystem(), "\"unanswered\"");
        StringAssert.Contains(Prompts.ResearchQuestionsSystem(), "\"questions\"");
    }

    #endregion
}

[TestClass]
public class QueryValidationTests
{
    private static readonly MethodInfo IsPlausible = typeof(GatherResearchTool)
        .GetMethod("IsPlausibleQuery", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo Derive = typeof(GatherResearchTool)
        .GetMethod("DeriveQueryFromMessage", BindingFlags.NonPublic | BindingFlags.Static)!;

    [TestMethod] public void Refusal_text_rejected()
        => Assert.IsFalse((bool)IsPlausible.Invoke(null, new object[] { "I can't assist with generating a search query that includes Jensen Huang's personal affairs." })!);

    [TestMethod] public void Apology_rejected()
        => Assert.IsFalse((bool)IsPlausible.Invoke(null, new object[] { "I'm sorry, but I cannot help with that request." })!);

    [TestMethod] public void Anything_else_offer_rejected()
        => Assert.IsFalse((bool)IsPlausible.Invoke(null, new object[] { "Is there anything else I can help you with?" })!);

    [TestMethod] public void Legitimate_query_accepted()
        => Assert.IsTrue((bool)IsPlausible.Invoke(null, new object[] { "Jensen Huang affair controversy" })!);

    [TestMethod] public void Keyword_fallback_derives_from_message()
        => Assert.AreEqual("Jensen Huang affairs", (string)Derive.Invoke(null, new object[] { "find out all the details about the Jensen Huang affairs and put it in my Research journal" })!);
}

[TestClass]
public class ContextBudgetTests
{
    [TestMethod] public void EstimateTokens_scales_with_length()
    {
        Assert.AreEqual(0, ContextBudget.EstimateTokens(null));
        Assert.AreEqual(0, ContextBudget.EstimateTokens(""));
        Assert.AreEqual(9, ContextBudget.EstimateTokens(new string('x', 20))); // 5 + 4
    }

    [TestMethod] public void ShouldCompact_fires_at_threshold()
    {
        var budget = 10000;
        Assert.IsFalse(ContextBudget.ShouldCompact((int)(budget * 0.81), budget));
        Assert.IsTrue(ContextBudget.ShouldCompact((int)(budget * 0.82), budget));
        Assert.IsTrue(ContextBudget.ShouldCompact(budget, budget));
    }

    [TestMethod] public void CompactionSplit_keeps_tail_verbatim()
    {
        var msgs = Enumerable.Range(0, 15).Select(i => $"m{i}").ToList();
        var ok = ContextBudget.TryGetCompactionSplit(msgs, keepTail: 8, out var head, out var tail);
        Assert.IsTrue(ok);
        Assert.AreEqual(7, head.Count);
        Assert.AreEqual(8, tail.Count);
        Assert.AreEqual("m7", tail[0]);
    }

    [TestMethod] public void CompactionSplit_declines_when_too_short()
    {
        var msgs = Enumerable.Range(0, 9).Select(i => $"m{i}").ToList();
        Assert.IsFalse(ContextBudget.TryGetCompactionSplit(msgs, keepTail: 8, out _, out _));
    }

    [TestMethod] public void ComposeContext_summary_plus_tail()
    {
        var ctx = ContextBudget.ComposeContext("we decided X", new[] { "User: hi", "Assistant: hello" });
        StringAssert.Contains(ctx, "Earlier in this session (summary):");
        StringAssert.Contains(ctx, "we decided X");
        StringAssert.Contains(ctx, "Recent conversation in this session:");
        StringAssert.Contains(ctx, "Assistant: hello");
    }

    [TestMethod] public void ComposeContext_no_summary_is_tail_only()
    {
        var ctx = ContextBudget.ComposeContext(null, new[] { "User: hi" });
        Assert.IsFalse(ctx.Contains("summary"));
        StringAssert.Contains(ctx, "User: hi");
    }
}

[TestClass]
public class SearchResultExtractionTests
{
    private static AnalyzedArticle Article(params (string tag, string href)[] links)
    {
        var a = new AnalyzedArticle();
        foreach (var (tag, href) in links)
            a.elements.Add(new DomElement(null) { tagName = tag, attribute = new Dictionary<string, string> { ["href"] = href } });
        return a;
    }

    private static SearchEngine Bing() => new SearchEngine { Url = "https://www.bing.com/search?q={search}&count=20" };

    [TestMethod]
    public void Extracts_bing_base64_wrapped_results()
    {
        // Real Bing ck/a redirect: u=a1 + base64url(destination)
        var a = Article(("a", "https://www.bing.com/ck/a?!&&p=abc&u=a1aHR0cHM6Ly93d3cuc3BlZWR0ZXN0Lm5ldC8&ntb=1"),
                        ("a", "https://www.bing.com/ck/a?!&&p=def&u=a1aHR0cHM6Ly9mYXN0LmNvbS8&ntb=1"));
        var urls = SearchHelper.ExtractResultUrls(a, Bing());
        CollectionAssert.Contains(urls, "https://www.speedtest.net/");
        CollectionAssert.Contains(urls, "https://fast.com/");
    }

    [TestMethod]
    public void Skips_non_anchor_href_elements()
    {
        var a = Article(("link", "https://cdn.example.com/bundle.css"),
                        ("a", "https://en.wikipedia.org/wiki/Test"));
        var urls = SearchHelper.ExtractResultUrls(a, Bing());
        Assert.AreEqual(1, urls.Count);
        CollectionAssert.Contains(urls, "https://en.wikipedia.org/wiki/Test");
    }

    [TestMethod]
    public void Unwraps_ddg_uddg_redirects()
    {
        var a = Article(("a", "//duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fpage&rut=xyz"));
        var urls = SearchHelper.ExtractResultUrls(a, new SearchEngine { Url = "https://duckduckgo.com/html/?q={search}" });
        CollectionAssert.Contains(urls, "https://example.com/page");
    }

    [TestMethod]
    public void Metasearch_filters_engine_base_domain_nav()
    {
        var a = Article(("a", "https://r.search.yahoo.com/_ylt=x/RU=https%3a%2f%2ftravel.yahoo.com%2f;_ylt=y"),
                        ("a", "https://r.search.yahoo.com/_ylt=x/RU=https%3a%2f%2ftineye.com%2f"),
                        ("a", "https://images.google.com/imghp"));
        var urls = SearchHelper.ExtractResultUrls(a, new SearchEngine { Url = "https://search.yahoo.com/search?p={search}" }, allowEngineDomains: true);
        CollectionAssert.Contains(urls, "https://tineye.com/");
        CollectionAssert.Contains(urls, "https://images.google.com/imghp");
        Assert.IsFalse(urls.Any(u => u.Contains("travel.yahoo.com")), "yahoo nav should be filtered");
    }
}

[TestClass]
public class NextPageDetectionTests
{
    private static AnalyzedArticle Article(params (string text, string href)[] links)
    {
        var a = new AnalyzedArticle();
        foreach (var (text, href) in links)
            a.elements.Add(new DomElement(null)
            {
                tagName = "a", text = text,
                attribute = new Dictionary<string, string> { ["href"] = href }
            });
        return a;
    }

    const string Page1 = "https://search.yahoo.com/search?p=test";

    [TestMethod]
    public void Finds_next_via_numeric_offset_param()
    {
        // Yahoo-style: same params + b/pz/bct/xargs additions; smallest delta wins
        var a = Article(
            ("2", "https://search.yahoo.com/search;_ylt=x?p=test&b=8&pz=7&bct=0&xargs=0"),
            ("3", "https://search.yahoo.com/search;_ylt=x?p=test&b=15&pz=7&bct=0&xargs=0"),
            ("Prev", "https://search.yahoo.com/search?p=test"));
        var next = SearchHelper.FindNextPageUrl(a, Page1);
        Assert.IsNotNull(next);
        StringAssert.Contains(next!, "b=8");
    }

    [TestMethod]
    public void Finds_next_via_pager_param_page_number()
    {
        var a = Article(
            ("Next", "https://bing.com/search?q=test&first=11"),
            ("5", "https://bing.com/search?q=test&first=41"));
        var next = SearchHelper.FindNextPageUrl(a, "https://bing.com/search?q=test&first=1");
        Assert.IsNotNull(next);
        StringAssert.Contains(next!, "first=11");
    }

    [TestMethod]
    public void Returns_null_when_no_pagination()
    {
        var a = Article(
            ("Home", "https://bing.com/"),
            ("Speedtest", "https://www.speedtest.net/"));
        Assert.IsNull(SearchHelper.FindNextPageUrl(a, "https://bing.com/search?q=test&first=1"));
    }

    [TestMethod]
    public void Rejects_cross_host_and_different_path()
    {
        var a = Article(
            ("Next", "https://bing.com/images?q=test&first=11"),
            ("Next", "https://other.com/search?q=test&first=11"));
        Assert.IsNull(SearchHelper.FindNextPageUrl(a, "https://bing.com/search?q=test&first=1"));
    }
}

[TestClass]
public class ResearchObjectTests
{
    private static string _tempDir = "";

    [ClassInitialize]
    public static void Setup(TestContext _)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "collector-research-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Files.JournalPath = _tempDir;
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
    }

    private static JournalEntryDocument NewDoc(string? json = null)
    {
        var id = Guid.NewGuid();
        if (json != null) File.WriteAllText(Path.Combine(_tempDir, $"{id:N}.json"), json);
        return JournalEntryDocument.Load(id);
    }

    private static List<ExtractedTopic> Items(params (string topic, string? sub, string[] facts)[] rows)
        => rows.Select(r => new ExtractedTopic
        {
            Topic = r.topic, Subtopic = r.sub,
            Facts = r.facts.ToList()
        }).ToList();

    #region Sources

    [TestMethod]
    public void Sources_increment_and_dedupe_by_url()
    {
        var r = new ResearchObject();
        var (id1, created1) = r.RegisterSource("https://a.com/x", "A");
        var (id2, created2) = r.RegisterSource("https://b.com/y", "B");
        var (id3, created3) = r.RegisterSource("https://a.com/x", "A again");
        Assert.AreEqual(1, id1); Assert.AreEqual(2, id2); Assert.AreEqual(1, id3);
        Assert.IsTrue(created1); Assert.IsTrue(created2); Assert.IsFalse(created3);
        Assert.AreEqual(2, r.Sources.Count);
    }

    [TestMethod]
    public void Rescrape_replaces_source_facts()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        r.MergeExtractionAsync(Items(("T", null, new[] { "old fact" })), id, null).Wait();
        Assert.AreEqual(1, r.TotalFacts);
        r.RemoveFactsBySource(id);
        r.MergeExtractionAsync(Items(("T", null, new[] { "new fact" })), id, null).Wait();
        Assert.AreEqual(1, r.TotalFacts);
        Assert.AreEqual("new fact", r.AllFacts().First().Text);
    }

    [TestMethod]
    public void Dangling_source_refs_are_nulled_on_load()
    {
        var json = "{\"modules\":[],\"research\":{\"sources\":[{\"id\":1,\"title\":\"A\",\"url\":\"https://a.com\"}]," +
            "\"topics\":[{\"topic\":\"T\",\"facts\":[{\"text\":\"f1\",\"source\":9}]}]}}";
        var doc = NewDoc(json);
        Assert.IsNull(doc.Research!.Topics[0].Facts[0].Source);
    }

    #endregion

    #region Merge + dedup

    [TestMethod]
    public async Task Merge_dedupes_facts_globally_across_topics()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        await r.MergeExtractionAsync(Items(("Topic A", null, new[] { "Z-targeting invented in 1998" })), id, null);
        await r.MergeExtractionAsync(Items(("Topic B", null, new[] { "z-targeting invented in 1998!" })), id, null);
        Assert.AreEqual(1, r.TotalFacts, "normalized duplicate should not be stored twice");
        Assert.AreEqual("Topic A", r.AllTopics().First(t => t.Facts.Count > 0).Topic);
    }

    [TestMethod]
    public async Task Merge_redirects_matching_subtopic_title_globally()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        // Create "History" as a subtopic under "Game"
        await r.MergeExtractionAsync(Items(("Game", "History", new[] { "f1" })), id, null);
        // Incoming root-level "History" must redirect into the existing subtopic
        await r.MergeExtractionAsync(Items(("History", null, new[] { "f2" })), id, null);
        Assert.AreEqual(1, r.Topics.Count, "no duplicate root topic may be created");
        Assert.AreEqual("Game", r.Topics[0].Topic);
        var history = r.Topics[0].Subtopics[0];
        Assert.AreEqual(2, history.Facts.Count);
    }

    [TestMethod]
    public async Task Merge_adopts_existing_title_over_new_phrasing()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        await r.MergeExtractionAsync(Items(("The Timeline", null, new[] { "f1" })), id, null);
        await r.MergeExtractionAsync(Items(("the timeline", null, new[] { "f2" })), id, null);
        Assert.AreEqual(1, r.Topics.Count);
        Assert.AreEqual("The Timeline", r.Topics[0].Topic);
    }

    [TestMethod]
    public async Task Merge_facts_keep_source_id()
    {
        var r = new ResearchObject();
        var (id1, _) = r.RegisterSource("https://a.com/x", "A");
        var (id2, _) = r.RegisterSource("https://b.com/y", "B");
        await r.MergeExtractionAsync(Items(("T", null, new[] { "from a" })), id1, null);
        await r.MergeExtractionAsync(Items(("T", null, new[] { "from b" })), id2, null);
        Assert.AreEqual(1, r.Topics[0].Facts[0].Source);
        Assert.AreEqual(2, r.Topics[0].Facts[1].Source);
    }

    [TestMethod]
    public async Task Merge_matches_topics_by_embedding_similarity()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        await r.MergeExtractionAsync(Items(("Release Timeline", null, new[] { "f1" })), id, null);

        // Fake embedder: "Launch History" is a paraphrase of "Release Timeline"
        // (sim 0.9) but "Cooking" is unrelated (sim 0.1).
        Func<List<string>, Task<float[][]>?> embed = texts =>
            Task.FromResult<float[][]?>(texts.Select(t => t switch
            {
                "Release Timeline" or "Launch History" => new[] { 1f, 0f, 0f },
                "f1" => new[] { 0f, 1f, 0f },
                "f2" => new[] { 0f, 0f, 1f },
                _ => new[] { 0f, 1f, 0f }
            }).ToArray());

        await r.MergeExtractionAsync(Items(("Launch History", null, new[] { "f2" })), id, embed);
        Assert.AreEqual(1, r.Topics.Count, "paraphrased topic title redirects to the existing topic");
        Assert.AreEqual("Release Timeline", r.Topics[0].Topic);
        Assert.AreEqual(2, r.Topics[0].Facts.Count);
    }

    [TestMethod]
    public async Task Merge_dedupes_paraphrased_facts_by_embedding()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        await r.MergeExtractionAsync(
            Items(("Timeline", null, new[] { "The game released in November 1998." })), id, null);

        // Facts mentioning 1998 are paraphrases of each other (same vector);
        // unrelated facts get orthogonal vectors.
        Func<List<string>, Task<float[][]>?> embed = texts =>
            Task.FromResult<float[][]?>(texts.Select(t =>
                t.Contains("1998") ? new[] { 0f, 1f } : new[] { 1f, 0f }).ToArray());

        await r.MergeExtractionAsync(
            Items(("Timeline", null, new[] { "It launched in 1998 for the N64.", "It has 120 stars." })),
            id, embed);

        Assert.AreEqual(2, r.Topics[0].Facts.Count,
            "paraphrased fact (sim > 0.85) skipped; distinct fact kept");
        Assert.IsTrue(r.Topics[0].Facts.Any(f => f.Text.Contains("stars")));
    }

    [TestMethod]
    public async Task Merge_marks_dirty_topics()
    {
        var r = new ResearchObject();
        var (id, _) = r.RegisterSource("https://a.com/x", "A");
        var dirty = await r.MergeExtractionAsync(Items(("T", null, new[] { "f1" })), id, null);
        Assert.AreEqual(1, dirty.Count);
        var dirty2 = await r.MergeExtractionAsync(Items(("T", null, new[] { "f1" })), id, null);
        Assert.AreEqual(0, dirty2.Count, "all-duped facts shouldn't dirty the topic");
    }

    [TestMethod]
    public void Fact_caps_are_enforced()
    {
        var r = new ResearchObject();
        var t = new ResearchTopic { Topic = "T" };
        r.Topics.Add(t);
        for (int i = 0; i < ResearchObject.MaxFactsPerTopic - 1; i++)
            t.Facts.Add(new ResearchFact { Text = $"fact {i}" });
        // 3 new facts arrive but only 1 slot remains — cap blocks the rest.
        r.MergeExtractionAsync(Items(("T", null, new[] { "new1", "new2", "new3" })), 1, null).Wait();
        Assert.AreEqual(ResearchObject.MaxFactsPerTopic, t.Facts.Count);
    }

    #endregion

    #region Format

    [TestMethod]
    public void Formatter_uses_hash_prefixes_and_bullets()
    {
        var r = new ResearchObject();
        var root = new ResearchTopic { Topic = "Root" };
        root.Facts.Add(new ResearchFact { Text = "fact one" });
        var sub = new ResearchTopic { Topic = "Sub" };
        sub.Facts.Add(new ResearchFact { Text = "sub fact" });
        var deep = new ResearchTopic { Topic = "Deep" };
        deep.Facts.Add(new ResearchFact { Text = "deep fact" });
        sub.Subtopics.Add(deep);
        root.Subtopics.Add(sub);
        r.Topics.Add(root);

        var md = r.FormatMarkdown(includeFacts: true);
        StringAssert.Contains(md, "# Root");
        StringAssert.Contains(md, "## Sub");
        StringAssert.Contains(md, "### Deep");
        StringAssert.Contains(md, "- fact one");
        StringAssert.Contains(md, "- sub fact");
    }

    [TestMethod]
    public void Compact_form_has_counts_not_facts()
    {
        var r = new ResearchObject();
        var t = new ResearchTopic { Topic = "T" };
        t.Facts.Add(new ResearchFact { Text = "secret detail" });
        r.Topics.Add(t);
        var compact = r.FormatCompact();
        StringAssert.Contains(compact, "# T (1 facts)");
        Assert.IsFalse(compact.Contains("secret detail"));
    }

    #endregion

    #region Serialization + pruning

    [TestMethod]
    public void Save_stamps_version_and_research()
    {
        var doc = NewDoc("{\"modules\":[]}");
        doc.Research = new ResearchObject();
        var (sid, _) = doc.Research.RegisterSource("https://a.com", "A");
        var id = Guid.NewGuid();
        doc.Save(id);
        var raw = File.ReadAllText(Path.Combine(_tempDir, $"{id:N}.json"));
        StringAssert.Contains(raw, "\"version\":2");
        StringAssert.Contains(raw, "\"research\"");
        StringAssert.Contains(raw, "https://a.com");
        var loaded = JournalEntryDocument.Load(id);
        Assert.AreEqual(1, loaded.Research!.Sources.Count);
    }

    [TestMethod]
    public void Orphaned_bindings_pruned_on_load()
    {
        var json = "{\"modules\":[],\"research\":{\"topics\":[{\"topic\":\"T\",\"moduleId\":\"dead-beef\",\"tabId\":\"dead-tab\"," +
            "\"facts\":[{\"text\":\"keep me\"}]}]}}";
        var doc = NewDoc(json);
        Assert.IsNull(doc.Research!.Topics[0].ModuleId, "moduleId pointing at a deleted module must be pruned");
        Assert.IsNull(doc.Research!.Topics[0].TabId);
        Assert.AreEqual(1, doc.Research.Topics[0].Facts.Count, "facts survive pruning");
    }

    [TestMethod]
    public void Remove_op_archives_bound_topic()
    {
        var json = "{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"x\"}]," +
            "\"research\":{\"topics\":[{\"topic\":\"T\",\"moduleId\":\"m1\",\"facts\":[{\"text\":\"f\"}]}]}}";
        var doc = NewDoc(json);
        doc.ApplyOps(JsonNode.Parse("{\"ops\":[{\"op\":\"remove\",\"id\":\"m1\"}]}"));
        Assert.AreEqual("archived", doc.Research!.Topics[0].Status);
        Assert.IsNull(doc.Research.Topics[0].ModuleId);
        Assert.AreEqual(1, doc.Research.Topics[0].Facts.Count, "facts survive archival");
    }

    [TestMethod]
    public void Depth_limit_flattens_level3_facts()
    {
        var json = "{\"modules\":[],\"research\":{\"topics\":[{\"topic\":\"L0\",\"subtopics\":[" +
            "{\"topic\":\"L1\",\"subtopics\":[{\"topic\":\"L2\",\"subtopics\":[" +
            "{\"topic\":\"L3\",\"facts\":[{\"text\":\"deep fact\"}]}],\"facts\":[]}],\"facts\":[]}],\"facts\":[]}]}}";
        var doc = NewDoc(json);
        var l2 = doc.Research!.Topics[0].Subtopics[0].Subtopics[0];
        Assert.AreEqual(0, l2.Subtopics.Count, "level-3 subtopics are not kept");
        Assert.IsTrue(l2.Facts.Any(f => f.Text == "deep fact"), "level-3 facts are lifted into level 2");
    }

    #endregion

    #region Backfill

    [TestMethod]
    public async Task Backfill_builds_topics_from_modules()
    {
        var json = "{\"modules\":[" +
            "{\"type\":\"title\",\"id\":\"t1\",\"text\":\"Overview\"}," +
            "{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"<p>root content</p>\"}," +
            "{\"type\":\"tabs\",\"id\":\"tabs1\",\"tabs\":[" +
                "{\"id\":\"tab1\",\"title\":\"History\",\"modules\":[{\"type\":\"text-editor\",\"id\":\"m2\",\"html\":\"<p>history text</p>\"}]}," +
                "{\"id\":\"tab2\",\"title\":\"Stats\",\"modules\":[{\"type\":\"text-editor\",\"id\":\"m3\",\"html\":\"<p>stats text</p>\"},{\"type\":\"text-editor\",\"id\":\"m4\",\"html\":\"<p>stats extra</p>\"}]}]}," +
            "{\"type\":\"image\",\"id\":\"img1\",\"url\":\"x.png\"}]}";
        var doc = NewDoc(json);
        var research = await ResearchBackfill.BuildAsync(doc, "Entry Title",
            text => Task.FromResult(new List<string> { "fact:" + text.Substring(0, Math.Min(10, text.Length)) }));

        Assert.AreEqual(1, research.Topics.Count);
        var root = research.Topics[0];
        Assert.AreEqual("Overview", root.Topic);
        Assert.AreEqual("m1", root.ModuleId);
        Assert.AreEqual(2, root.Subtopics.Count);
        Assert.AreEqual("m2", root.Subtopics[0].ModuleId);
        Assert.AreEqual("tabs1", root.Subtopics[0].TabId);
        // First text-editor in a multi-editor tab claims the binding.
        Assert.AreEqual("m3", root.Subtopics[1].ModuleId);
    }

    [TestMethod]
    public async Task Backfill_tabs_without_preceding_topic_get_synthetic_root()
    {
        var json = "{\"modules\":[" +
            "{\"type\":\"tabs\",\"id\":\"tabs1\",\"tabs\":[" +
                "{\"id\":\"t\",\"title\":\"Solo\",\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"<p>x</p>\"}]}]}]}";
        var doc = NewDoc(json);
        var research = await ResearchBackfill.BuildAsync(doc, "My Entry",
            text => Task.FromResult(new List<string>()));
        Assert.AreEqual(1, research.Topics.Count);
        Assert.AreEqual("My Entry", research.Topics[0].Topic);
        Assert.IsTrue(research.Topics[0].Synthetic);
        Assert.AreEqual(1, research.Topics[0].Subtopics.Count);
    }

    #endregion

    #region Module writer — structure + sources tab (deterministic parts)

    [TestMethod]
    public void Sources_tab_is_last_in_first_tabs_module()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"tabs\",\"id\":\"tabs1\",\"style\":1,\"tabs\":[" +
            "{\"id\":\"a\",\"title\":\"A\",\"modules\":[]},{\"id\":\"b\",\"title\":\"B\",\"modules\":[]}]}]}");
        var research = new ResearchObject();
        research.RegisterSource("https://a.com", "Source A");
        research.RegisterSource("https://b.com", "Source B");
        var writer = new ResearchModuleWriter(new LLMClient(null!, "test"));

        Assert.IsTrue(writer.EnsureSourcesTab(doc, research));
        var tabs = (doc.Modules[0] as JsonObject)!["tabs"] as JsonArray;
        Assert.AreEqual(3, tabs!.Count);
        Assert.AreEqual("sources", (tabs[2] as JsonObject)!["id"]!.ToString());
        var html = (((tabs[2] as JsonObject)!["modules"] as JsonArray)![0] as JsonObject)!["html"]!.ToString();
        StringAssert.Contains(html, "Source A");
        StringAssert.Contains(html, "Source B");
    }

    [TestMethod]
    public void Sources_tab_removed_when_no_sources()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"tabs\",\"id\":\"tabs1\",\"style\":1,\"tabs\":[" +
            "{\"id\":\"sources\",\"title\":\"Sources\",\"modules\":[{\"type\":\"text-editor\",\"id\":\"se\",\"html\":\"x\"}]}," +
            "{\"id\":\"a\",\"title\":\"A\",\"modules\":[]}]}]}");
        var writer = new ResearchModuleWriter(new LLMClient(null!, "test"));
        Assert.IsTrue(writer.EnsureSourcesTab(doc, new ResearchObject()));
        var tabs = (doc.Modules[0] as JsonObject)!["tabs"] as JsonArray;
        Assert.AreEqual(1, tabs!.Count);
    }

    [TestMethod]
    public void EnsureTab_dedupes_labels_within_container()
    {
        var doc = NewDoc("{\"modules\":[]}");
        var tabs = doc.EnsureRootTabsModule();
        var t1 = doc.EnsureTab(tabs, "History");
        var t2 = doc.EnsureTab(tabs, "History");
        Assert.AreEqual("History", t1["title"]!.ToString());
        Assert.AreEqual("History 2", t2["title"]!.ToString());
    }

    [TestMethod]
    public void Semantic_map_labels_editors_by_topic()
    {
        var json = "{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"<p>very long prose nobody needs in a prompt</p>\"}]," +
            "\"research\":{\"topics\":[{\"topic\":\"Timeline\",\"moduleId\":\"m1\",\"facts\":[]}]}}";
        var doc = NewDoc(json);
        var map = doc.BuildSemanticMap();
        StringAssert.Contains(map, "Timeline");
        Assert.IsFalse(map.Contains("very long prose"));
    }

    #endregion

    #region Op guardrails

    private static void InvokeGuard(JsonObject ops, JournalEntryDocument doc, string instruction)
    {
        var tool = new AddEditJournalEntryTool(null!, null!, null!, null!);
        var m = typeof(AddEditJournalEntryTool).GetMethod("GuardConversationalOps",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        m.Invoke(tool, new object[] { ops, doc, instruction });
    }

    [TestMethod]
    public void Guard_rejects_emptying_removes()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"x\"},{\"type\":\"title\",\"id\":\"m2\",\"text\":\"t\"}]}");
        var ops = (JsonObject)JsonNode.Parse("{\"ops\":[{\"op\":\"remove\",\"id\":\"m1\"},{\"op\":\"remove\",\"id\":\"m2\"}]}")!;
        InvokeGuard(ops, doc, "remove everything about the journal");
        var arr = ops["ops"] as JsonArray;
        Assert.AreEqual(0, arr!.Count, "removes that would empty the entry are dropped");
    }

    [TestMethod]
    public void Guard_rejects_unnamed_removes()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"x\"},{\"type\":\"title\",\"id\":\"m2\",\"text\":\"Timeline\"}]}");
        // instruction names m1's content area but not m2's "Timeline"
        var ops = (JsonObject)JsonNode.Parse("{\"ops\":[{\"op\":\"remove\",\"id\":\"m2\"}]}")!;
        InvokeGuard(ops, doc, "fix the formatting of the text");
        var arr = ops["ops"] as JsonArray;
        Assert.AreEqual(0, arr!.Count, "remove of an unnamed module is dropped");
    }

    [TestMethod]
    public void Guard_allows_named_removes()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"title\",\"id\":\"m2\",\"text\":\"Timeline\"},{\"type\":\"text-editor\",\"id\":\"m3\",\"html\":\"x\"}]}");
        var ops = (JsonObject)JsonNode.Parse("{\"ops\":[{\"op\":\"remove\",\"id\":\"m2\"}]}")!;
        InvokeGuard(ops, doc, "remove the timeline section");
        var arr = ops["ops"] as JsonArray;
        Assert.AreEqual(1, arr!.Count, "a remove named in the instruction survives");
    }

    [TestMethod]
    public void Guard_rejects_blanking_updates()
    {
        var doc = NewDoc("{\"modules\":[{\"type\":\"text-editor\",\"id\":\"m1\",\"html\":\"x\"}]}");
        var ops = (JsonObject)JsonNode.Parse("{\"ops\":[{\"op\":\"update\",\"id\":\"m1\",\"set\":{\"html\":\"\"}}]}")!;
        InvokeGuard(ops, doc, "clear the intro paragraph");
        var arr = ops["ops"] as JsonArray;
        Assert.AreEqual(0, arr!.Count, "updates that blank a module are dropped");
    }

    #endregion
}

[TestClass]
public class ResearchSourceHelperTests
{
    [TestMethod]
    public void YouTubeHelper_matches_youtube_url_forms()
    {
        var helper = new Collector.Chat.Tools.GatherResearch.YouTubeHelper();
        Assert.IsTrue(helper.Match("https://www.youtube.com/watch?v=abc123"));
        Assert.IsTrue(helper.Match("https://youtu.be/abc123"));
        Assert.IsTrue(helper.Match("https://youtube.com/embed/abc123"));
        Assert.IsTrue(helper.Match("https://m.youtube.com/shorts/abc123"));
        Assert.IsFalse(helper.Match("https://example.com/watch?v=abc123"));
        Assert.IsFalse(helper.Match("https://notyoutube.com/watch?v=abc"));
        Assert.IsFalse(helper.Match(""));
    }

    [TestMethod]
    public void WebPageHelper_matches_everything_as_fallback()
    {
        var helper = new Collector.Chat.Tools.GatherResearch.WebPageHelper();
        Assert.IsTrue(helper.Match("https://en.wikipedia.org/wiki/Zelda"));
        Assert.IsTrue(helper.Match("https://anything.example/page?q=1"));
    }

    [TestMethod]
    public void Resolve_dispatches_youtube_to_youtube_helper_and_rest_to_webpage()
    {
        var yt = Collector.Chat.Tools.GatherResearch.ResearchHelpers.Resolve("https://youtu.be/xyz789");
        Assert.IsInstanceOfType(yt, typeof(Collector.Chat.Tools.GatherResearch.YouTubeHelper));
        var web = Collector.Chat.Tools.GatherResearch.ResearchHelpers.Resolve("https://example.com/article");
        Assert.IsInstanceOfType(web, typeof(Collector.Chat.Tools.GatherResearch.WebPageHelper));
    }

    [TestMethod]
    public void ParseSubtitleFile_extracts_cue_text_and_collapses_duplicates()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:02.500\nhello <b>world</b>\nhello world\n\n00:00:02.600 --> 00:00:04.000\nsecond line\n\n";
        var text = Collector.Chat.Tools.GatherResearch.YouTubeHelper.ParseSubtitleFile(vtt);
        Assert.AreEqual("hello world second line", text);
    }

    [TestMethod]
    public void WebPageHelper_detects_html_lang()
    {
        var method = typeof(Collector.Chat.Tools.GatherResearch.WebPageHelper)
            .GetMethod("DetectLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        Assert.AreEqual("fr", method.Invoke(null, new object[] { "<html lang=\"fr\"><body>x</body></html>" }));
        Assert.AreEqual("en-US", method.Invoke(null, new object[] { "<html lang='en-US'>" }));
        Assert.IsNull(method.Invoke(null, new object[] { "<html><body>no lang</body></html>" }));
    }
}

[TestClass]
public class VideoModuleUrlTests
{
    private static string _tempDir = "";

    [ClassInitialize]
    public static void Setup(TestContext _)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "collector-video-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Files.JournalPath = _tempDir;
    }

    private static Type ScrapedPageType =>
        typeof(AddEditJournalEntryTool).GetNestedType("ScrapedPage", BindingFlags.NonPublic)!;

    private static object NewScrapedPage(string url)
    {
        var page = Activator.CreateInstance(ScrapedPageType)!;
        ScrapedPageType.GetField("Url")!.SetValue(page, url);
        return page;
    }

    private static List<string> Videos(object page) =>
        (List<string>)ScrapedPageType.GetField("Videos")!.GetValue(page)!;

    private static void InvokeAppendPageMedia(JournalEntryDocument doc, object page)
    {
        var tool = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AddEditJournalEntryTool));
        typeof(AddEditJournalEntryTool)
            .GetMethod("AppendPageMedia", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tool, new object?[] { doc, page, null });
    }

    [TestMethod]
    public void ScrapedPage_FromDataItem_preserves_videos()
    {
        var item = new Dictionary<string, List<string>>
        {
            ["videos"] = new() { "https://www.youtube.com/watch?v=abc" }
        };
        var page = ScrapedPageType.GetMethod("FromDataItem")!
            .Invoke(null, new object[] { item, "https://www.youtube.com/watch?v=abc" })!;
        CollectionAssert.Contains(Videos(page), "https://www.youtube.com/watch?v=abc");
    }

    [TestMethod]
    public void AppendPageMedia_creates_video_player_with_url_and_autoTryAgain()
    {
        var doc = JournalEntryDocument.Load(Guid.NewGuid());
        var page = NewScrapedPage("https://www.youtube.com/watch?v=Mk4CjlajYbg");
        Videos(page).Add("https://www.youtube.com/watch?v=Mk4CjlajYbg");
        InvokeAppendPageMedia(doc, page);
        var video = doc.Modules.OfType<JsonObject>()
            .FirstOrDefault(m => m["type"]?.ToString() == "video-player");
        Assert.IsNotNull(video);
        Assert.AreEqual("https://www.youtube.com/watch?v=Mk4CjlajYbg", video["url"]?.ToString());
        Assert.AreEqual(true, video["autoTryAgain"]?.GetValue<bool>());
    }

    [TestMethod]
    public void AppendPageMedia_places_source_youtube_video_at_top()
    {
        var doc = JournalEntryDocument.Load(Guid.NewGuid());
        doc.Modules.Add(new JsonObject { ["type"] = "title", ["id"] = "t1", ["text"] = "Intro" });
        var page = NewScrapedPage("https://youtu.be/xyz");
        InvokeAppendPageMedia(doc, page);
        var first = doc.Modules[0] as JsonObject;
        Assert.AreEqual("video-player", first?["type"]?.ToString());
        Assert.AreEqual("https://youtu.be/xyz", first?["url"]?.ToString());
    }
}

[TestClass]
public class SourceDedupTests
{
    [TestMethod]
    public void GetVideoId_extracts_from_all_youtube_forms()
    {
        var h = typeof(Collector.Chat.Tools.GatherResearch.YouTubeHelper);
        Assert.AreEqual("Mk4CjlajYbg", h.GetMethod("GetVideoId")!
            .Invoke(null, new object[] { "https://www.youtube.com/watch?v=Mk4CjlajYbg" }));
        Assert.AreEqual("Mk4CjlajYbg", h.GetMethod("GetVideoId")!
            .Invoke(null, new object[] { "https://youtu.be/Mk4CjlajYbg" }));
        Assert.AreEqual("Mk4CjlajYbg", h.GetMethod("GetVideoId")!
            .Invoke(null, new object[] { "https://youtube.com/embed/Mk4CjlajYbg" }));
        Assert.AreEqual("Mk4CjlajYbg", h.GetMethod("GetVideoId")!
            .Invoke(null, new object[] { "https://m.youtube.com/shorts/Mk4CjlajYbg" }));
        Assert.IsNull(h.GetMethod("GetVideoId")!
            .Invoke(null, new object[] { "https://example.com/watch?v=Mk4CjlajYbg" }));
    }

    [TestMethod]
    public void HasSource_matches_exact_and_youtube_video_id()
    {
        var research = new ResearchObject();
        research.RegisterSource("https://www.youtube.com/watch?v=Mk4CjlajYbg", "Video");
        research.RegisterSource("https://example.com/article/", "Article");

        Assert.IsTrue(research.HasSource("https://www.youtube.com/watch?v=Mk4CjlajYbg"));
        Assert.IsTrue(research.HasSource("https://youtu.be/Mk4CjlajYbg"));       // cross-form
        Assert.IsTrue(research.HasSource("https://example.com/article"));      // trailing slash
        Assert.IsFalse(research.HasSource("https://youtu.be/OTHER1234"));
        Assert.IsFalse(research.HasSource("https://example.com/other"));
        Assert.IsFalse(research.HasSource(""));
    }
}

[TestClass]
public class TopicHeadingTests
{
    private static readonly MethodInfo WithTopicHeading = typeof(ResearchModuleWriter)
        .GetMethod("WithTopicHeading", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string Wrap(string html, string title) =>
        (string)WithTopicHeading.Invoke(null, new object[] { html, title })!;

    [TestMethod]
    public void Prepends_h4_with_topic_title()
    {
        Assert.AreEqual("<h4>Timeline</h4><p>Facts.</p>", Wrap("<p>Facts.</p>", "Timeline"));
    }

    [TestMethod]
    public void Strips_leading_h4_so_rewrites_never_duplicate()
    {
        var once = Wrap("<p>x</p>", "Timeline");
        var twice = Wrap(once, "Timeline");
        Assert.AreEqual(once, twice);
        Assert.AreEqual("<h4>Timeline</h4><p>x</p>", twice);
    }

    [TestMethod]
    public void Replaces_llm_generated_h4_and_encodes_title()
    {
        Assert.AreEqual("<h4>T&amp;C</h4><p>x</p>",
            Wrap("<h4>Old heading</h4><p>x</p>", "T&C"));
    }
}
