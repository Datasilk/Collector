# Research System

How Collector turns a chat message like "research the philosophy of Stoicism" into a
structured journal entry. The pipeline lives in `Collector.Chat` and is orchestrated
by an `ExecutionPlan` — a list of tool steps the planner generates and tools can
extend at runtime.

## Pipeline overview

```
user message
   │
   ▼
ChatWorker.SendMessage ──► planner (Ollama, JSON plan)
   │
   ▼
ExecutionPlan steps, executed in order, each step a chat tool:
   │
   ├─► gather-research      find candidate URLs on the web
   ├─► web-scrape           download + extract one URL → ResearchPage
   ├─► add-edit-journal-entry  merge one page's content into the entry
   └─► research-review      coverage check → queue more searches for gaps
```

`gather-research` doesn't collect pages itself — it inserts
`web-scrape` → `add-edit-journal-entry` step pairs into the plan after itself,
plus a `research-review` step behind them. The review may queue more
`gather-research` steps, so the plan grows dynamically until coverage is
satisfied or the page budget is exhausted.

Shared state passes between steps through `data`
(`List<Dictionary<string, List<string>>>`): scraped pages, research questions,
asked queries, planned pages, session-entry markers, and deepening flags.

## Step 1 — Planning

`ChatWorker` asks Ollama for a JSON plan. For a research request the plan is
typically a single `gather-research` step (optionally with a `title`,
`journalId`, or `url` arg). Everything else is appended by the tools as the run
proceeds.

## Step 2 — gather-research (`GatherResearchTool`)

Finds up to `maxResults` (capped at 10) candidate page URLs:

1. **Query.** Uses a `query` arg if present (research-review follow-ups),
   otherwise generates a 2–8 word keyword query via Ollama
   (`GenerateSearchQueryAsync`). Natural-language questions are distilled to
   keywords first (`DistillToKeywords` → `DeriveQueryFromMessage`) because
   engines fixate on single words in long questions and return junk.
   The raw question and distilled form both land in `asked-queries` so the
   coverage loop never re-asks them.
2. **Research questions.** On the first research step it generates up to 8
   explicit research questions (`GenerateResearchQuestionsAsync`) that define
   "done" for the run; stored as `research-questions` for the coverage check.
3. **Engine search.** Iterates enabled `SearchEngine` rows (priority order)
   through `SearchHelper.SearchEngineUrlsAsync`. Each engine is tried
   *direct HTTP fetch first, Charlotte headless browser as fallback* — plain
   HTTP avoids bot-detection pages served to headless Chrome.
   `ExtractResultUrls` pulls result links out of the parsed SERP, unwraps
   redirect wrappers (Bing `ck/a`, DDG `uddg=`, Google `/url?q=`), and rejects
   self-domain links, sponsored/ad URLs, search/social domains, and a junk-host
   list (uservoice, asset CDNs, engine infrastructure).
   (`SearchHelper` lives in `Collector.Chat/Helpers/`.)
4. **Dedupe.** `FilterByDomain` keeps one URL per domain. Then candidates are
   dropped if the entry already lists them as a research source **or** they
   were already planned this run (failed scrapes never become sources, so
   without this they'd be re-queued every wave).
5. **Plan expansion.** For each surviving URL a `web-scrape` +
   `add-edit-journal-entry` pair is appended, followed by `research-review`.
   Chosen URLs are recorded in `planned-pages` (the denominator for the
   "collected N of M pages" tally).

URL-direct mode: if the request names a specific URL, that page is downloaded
directly and no engine search happens.

## Step 3 — web-scrape (`WebScrapeTool`)

Converts one URL into a `ResearchPage` (title, headers, content sections,
images, videos, files, links, language):

- `ResearchHelpers.Resolve(url)` picks a site-specific `IResearchSourceHelper`
  — currently `YouTubeHelper` (caption transcripts) with `WebPageHelper` as the
  match-everything fallback.
- `WebPageHelper` downloads through `Common.Article.Download`: HTTP HEAD/GET
  with redirect/SSL handling, falling back to the **Charlotte** headless
  browser (`POST http://localhost:7007/GetDOM`, served by Charlotte Router →
  Charlotte/CefSharp) for JS-rendered pages. Non-HTML responses return as
  `file:{contentType}` and become downloadable research items rather than pages.
- The DOM JSON is deserialized by `Html.DeserializeArticle` into a flat
  `DomElement` list (open tags, `#text` nodes and generated closing tags
  with index/parent/childIndexes bookkeeping). `GetArticleInfoFromDOM`
  extracts title/meta, and `SearchHelper.DiscoverSearchEngines` scans the
  RAW elements for site-search forms (registration must see the unmodified
  DOM — cleaning strips forms).
- **`Html.CleanDOM`** produces a NEW element list (the input is untouched,
  so the raw DOM stays available for discovery) that:

  1. **Prunes empty leaves upward** — any element with no children and no
     text that isn't `<img>` is deleted; each emptied parent is deleted in
     turn until an ancestor still holds a child or text.
  2. **Collapses same-tag single-child chains** — `div > div > div` with
     nothing but the innermost content collapses upward into one div.
  3. **Strips every attribute** except `href`, `src` and `alt` (classes,
     ids, styles, data-*, aria-* all go).

  `FormatHtml(CleanDOM(elements))` yields the cleaned markup (live runs:
  ~200K chars → ~96K). No deterministic section classification remains —
  the parser dump is the page's ONLY content source.
- Pages already in the entry's sources are skipped entirely.
- **HTML parser model** (`LLM_HtmlParsers` — managed under Admin → Ollama:
  HTML Parser Models): when an active parser model exists, `WebPageHelper`
  sends the **cleaned** HTML to `HtmlParser.ParseAsync`, which:

  - **Pre-strips** `<script|style|svg|noscript|iframe|form|select|template>`
    subtrees (most of a page's bytes, none of its content — Wikipedia is ~4 MB
    raw), then caps input at `MaxHtmlChars` (380K chars ≈ 95K tokens).
  - Prompts in Jina's trained JSON-extraction shape — a single user turn,
    no system message: `Extract the main content from the given HTML and
    organize it into a logical, well-structured JSON object based on the
    data found on the page.` When the chat has a research subject, the
    instruction adds `Include only content that relates to the research
    subject below — omit navigation, ads, related-article links, and any
    text that does not relate to this subject.` followed by
    `Research subject: {userMessage}`. The HTML rides in an ````html`
    block, then `The JSON schema is as follows:` + the WebScraper.md
    schema in a ````json` block.
  - Sends Jina's own harness options (`ParserOptions`): `num_ctx 131072`
    (Ollama's 4K default would truncate the prompt to ~5% of the page —
    this was the root cause of "compacted" dumps), `num_predict 16384`,
    `temperature 0`, `top_k 1`, `repeat_penalty 1.13`.
  - Bounds the wait with `Task.WhenAny` abandonment (10 min) — OllamaSharp
    doesn't reliably cancel an in-flight generate. Parse failures log
    message + stack to the chat detail log and degrade to no content.
  - The cleaned HTML is staged under `journals/_staging/{guid}.html`
    BEFORE the parser call (`rawhtml` key in shared data) so it exists
    even if the model hangs — integration moves it beside the dump as
    `research/{####}.html`.
  - Every image URL the dump names (hero_image + sections[].images +
    root images) is downloaded and measured; only files whose largest
    dimension clears 640px are kept on `page.Images`.

  The result is carried on the page as `ParsedJson` (`parsed` key in shared
  data) and persisted under `research/{####}.json` beside the entry.

Extracted pages are stored in `data` as `web-scrape` items for the next step.

## Step 4 — add-edit-journal-entry (`AddEditJournalEntryTool`)

Integrates one scraped page into the target entry:

1. **Entry resolution.** Session marker → embedding-similarity search over
   existing entries (`maxDistance 0.3`) → `IsSameSubjectAsync` LLM verification
   (embeddings can't tell "Scientific Revolution" from "French Revolution" —
   a wrong-subject merge would corrupt a real entry) → create new. For
   creation, an LLM picks the best journal (`SelectJournalWithOllamaAsync`)
   unless a `journalId` arg was given; title and description are generated
   from the page content if absent.
2. **Relevance gate.** `IsPageRelevantAsync` checks the page against subject
   vocabulary built from the entry title, research questions, and existing
   topic names. ≥2 term hits accept; a single hit gets an LLM second opinion;
   zero hits are skipped without registering a source — SERP noise can't
   pollute the entry. Institutional homepages (bare root/landing URLs like
   mdpls.org) face a higher deterministic bar — a named-subject hit, or 3+
   generic hits when the subject has no proper nouns — since ambiguous
   keyword matches on menu boilerplate otherwise fool the LLM pass.
3. **Source + dump persistence.** `research.SyncSearches` mirrors this run's
   engine queries; `RegisterSource` dedupes the page URL and returns its
   index. Re-scraped sources replace their previous content —
   `RemoveContentBySource` drops every content item bound to the old index
   (structural title items survive) and their rendered modules are removed.
   The parser JSON is saved to `research/{####}.json` (four-digit increment)
   with the staged raw HTML moved beside it as `research/{####}.html` — the
   source row gets `"file": "000N.json"` and `"search": <index>`.
4. **Verbatim content copy — no fact extraction.** `ParseSections` walks the
   dump and copies its content directly into topic `content` arrays: the page
   `summary` + `hero_image` lead an unheaded section, `sections[]` contribute
   `blocks` (paragraph/bullet_list/numbered_list/table/image) plus per-section
   `images` and `tables`, root-level `tables`/`images` form a trailing
   section, and `comments` preserve their reply tree. A schema-shaped
   response (`{"type","properties"}` — the model echoing its output schema)
   is detected and yields no content. When a dump exists it is the source of
   truth — chunked `ContentSections` text is only used for pages with no
   parser output at all.
5. **Topic assignment.** `AssignTopicsAsync` sends the section headings +
   150-char previews to the local model with the existing topic tree, and
   gets back per-section `{topic, subtopic, tab}` — or `"skip"` for
   navigation/category/boilerplate sections. `IsBoilerplateSection`
   additionally drops reference/see-also/external-links-style headings
   outright. A deterministic post-pass (`DedupeAssignedTopics`) then
   ungarbles repeated-word titles ("Scope and Scope") and merges
   near-duplicate titles — content-word subsets like "Content" vs
   "Organization and Content" — onto existing topics (existing wins) or
   the longer assigned name. Sections then land via
   `GetOrCreateTopic`/`GetOrCreateSubTopic`
   (subtopics render as tabs, `tab` ≤3 words). Blocks are copied verbatim —
   only `type`, `moduleId`, and `source` are stamped; list/table items are
   flattened (`FlattenItems` hoists nested objects, keeps `{text,url}`
   link objects) and `AppendContent` dedupes paragraphs by normalized text,
   list items against all stored text, tables by caption+headers signature,
   and images by URL. Section images and unattached page images become an
   `image-gallery` right after the topic's title block.
6. **Module composition.** `ResearchModuleWriter.WriteDirtyTopicsAsync`
   renders only topics the page actually touched: `title`→`<h4>` editor,
   `paragraph`→`<p>` editor, lists→`<ul>/<ol>` (`{text,url}` items render as
   `<a href>` anchors), `table`→HTML table, `image`/`image-gallery`→media
   modules, `sub_topics`→`tabs` module (side-menu style; each subtopic's
   `moduleId` is its tab's `id`). `FindModuleIn` counts tab ids — without
   that, load-time `PruneOrphanedBindings` stripped subtopic bindings and
   spawned duplicate empty tabs; `RepairDuplicateTabs` heals the old damage
   by folding same-title orphans into the bound tab. `EnsureSourcesModule`
   keeps the Sources editor pinned last.
7. **Media.** Only images verified ≥640px in **either** dimension may land
   in the entry — every URL the dump declares is downloaded +
   dimension-checked (`Images.ImageMaxDimension`, bounded per page). UI
   chrome is rejected first: `ChromeImageUrl` drops SVG/icon-class assets
   and avatar/profile/logo/sprite URL paths before download, and the
   post-download floor adds an 8 KB minimum plus an svg/icon media-type
   rejection so chatbot icons and author headshots can't land in
   galleries. `AppendPageMedia` then deterministically places videos (2) and
   files (3) as modules beside the topic the page merged into. User-requested
   images go through `AcquireRequestedImagesAsync`, which downloads real
   files into `journals/{journalId}/{entryId}/images/`.
8. **Provenance + save.** `EnsureSourcesModule` maintains a single
   text-editor pinned to the very bottom of the main module array:
   `<h4>Sources</h4>` plus a `<ul>` bullet list of `<a href>` links titled
   by page, rebuilt in place from `research.Sources` on every save.
   `doc.Save` writes `research.json` + `entry.json` atomically (temp file +
   replace; optionally AES-encrypted). A snapshot of the pre-edit state is
   taken once per run for rollback.
9. **Instruction edits.** When `instruction` args (not a URL) arrive, the same
   tool applies conversational edits — LLM edit ops validated by
   `GuardConversationalOps`/`ApplyInstructionEditsAsync`, archived-topic
   restore, media placement — instead of research integration.

## Step 5 — research-review (`ResearchReviewTool`)

Advisory coverage check after each collection wave:

- Loads the entry text (7k-char budget) and asks the local model which
  `research-questions` remain unanswered.
- **Gap follow-ups.** Unanswered questions not already in `asked-queries` get
  new `gather-research` steps (max 4 per review). Two guards stop runaway:
  questions already asked are never re-queued, and a hard page budget
  (`MaxPlannedPages = 25`) halts follow-ups regardless of remaining gaps.
- **Answerability.** Questions that were searched but stay uncovered get a
  one-time "can these sources even answer this?" judgment; unanswerable ones
  are permanently excluded from coverage.
- **Deepening pass.** Once per run, topics with thin content (<3 text items,
  or judged too generic by `IsDetailedAsync`) each get a targeted search of up
  to 3 pages (15 total), tracked via `deepened-topics`/`deepening-done`,
  subject to the same page budget.
- **Pure URL-direct runs skip deepening + follow-ups entirely.** When every
  asked query is a user-named URL, the request is a targeted page collection;
  expanding coverage would research around the named pages and balloon one
  scrape into dozens of unrelated sources.

## The ResearchObject (knowledge layer)

`ResearchObject` is the entry's structured knowledge — a *content-structured*
document: topics hold ordered, typed content blocks copied verbatim from the
per-source parser dumps. There is no fact extraction and no prose
regeneration — the page's own text is the data.

```jsonc
research.json → {
  "topics": [
    {
      "title": "…",
      "content": [
        { "type": "title",         "moduleId": "…", "text": "…" },
        { "type": "image-gallery", "moduleId": "…", "source": 0,
          "items": [{ "url": "…", "alt_text": "…", "caption": "…" }] },
        { "type": "paragraph",     "moduleId": "…", "source": 0, "text": "…" },
        { "type": "bullet_list",   "moduleId": "…", "source": 0,
          "items": ["…", { "text": "…", "url": "…" }] },
        { "type": "table",         "moduleId": "…", "source": 0,
          "caption": "…", "headers": ["…"], "rows": [["…"]] },
        { "type": "sub_topics",    "moduleId": "…",
          "items": [{ "title": "…", "tab": "…", "moduleId": "…",
                      "content": [ /* same shape, nested */ ] }] }
      ]
    }
  ],
  "comments": { "moduleId": "…", "items": [ {username, text, source, replies[]} ] },
  "sources":  { "moduleId": "…", "items": [ {title, url, file?, search?} ] },
  "searches": [ { "query": "…", "url": "…", "timestamp": "…" } ]
}
```

- **Provenance is index-based.** `source` on a content item indexes
  `sources.items`; `search` on a source indexes `searches`. `FixDanglingSourceRefs`
  clears refs that fall out of range.
- **List items** are strings, except `{text,url}` link objects which render
  as `<a href>` anchors. `NormalizeListItems`/`FlattenItems` coerce object or
  nested-block items on load and on copy.
- **Subtopics** live inside a parent's `sub_topics` content item and are
  themselves `ResearchTopic` nodes (title + content). `MaxTopicDepth = 2`.
- **Dedupe** in `AppendContent`: paragraphs by normalized text anywhere in
  the tree, list items against all stored text, tables by caption+headers
  signature, images by URL.
- **Bindings.** Content items and subtopic nodes carry `moduleId` of the
  module/tab that renders them. `PruneOrphanedBindings` clears ids whose
  module was deleted (tab ids count — `FindModuleIn` descends into
  `tabs[].id`/`tabs[].modules[]`).
- **Legacy read.** Old fact-oriented research loads through compat conversion:
  `facts[]`→paragraph items, `subtopics[]`→`sub_topics` items, source IDs→
  array indexes.
- `FormatMarkdown`/`FormatCompact` render the tree for review/plan prompts.

`JournalEntryDocument` is the entry file set, stored under
`journals/{journalId}/{entryId}/`:

- `entry.json` — `modules` tree, `version`, `css`; atomic save, optional AES
  encryption, `ExtractText()` for the coverage check.
- `research.json` — the knowledge layer above + `resources`.
- `research/{####}.json` — per-source parser dumps; `research/{####}.html` —
  the raw HTML the parser consumed (sidecar for verifying extraction).
  The matching source row gets `"file": "000N.json"`.
- `snapshots/{id}.json` — pre-edit snapshots.
- `images/`, `files/` — downloaded media; `videos/{videoId}/` holds
  `raw.mp4`, `thumb.jpg`, `hls/`, `previews/`.
- `journals/_staging/` — raw-HTML staging before a dump number is assigned.

`ResearchBackfill.BuildAsync` constructs a `ResearchObject` for entries that
pre-date the research layer by turning existing text-editor modules into
paragraph content items bound to their source module ids.

## External dependencies

| System | Role |
|---|---|
| Ollama (`LLMClient`) | planning, queries, questions, topic assignment, coverage/depth/answerability/relevance checks, same-subject verification, journal selection |
| Ollama parser model (`LLM_HtmlParsers`, `HtmlParser`) | HTML→Markdown conversion; deterministic `MarkdownToDumpJson` builds the structured dump |
| Search engines (DB `SearchEngines` + `SearchHelper`) | result-page fetching, link extraction, engine discovery from SERP forms/scripts |
| Charlotte Router (`:7007`) + Charlotte (`:7077`, CefSharp) | JS-rendered page download, `GetDOM` JSON contract consumed by `Html.DeserializeArticle` |
| `Collector.Common.Article` / `Files` / `Images` | shared download pipeline, media storage under `journals/{journalId}/{entryId}/{images,files,videos}/` |
| PostgreSQL (`Collector.Data`) | entries, journals, search engines, domains, snapshots |

## Failure handling

- Failed scrapes are skipped; the review tally reports "collected N of M pages".
- Coverage review never throws — it completes with "skipped" on error.
- Relevance-gated pages are skipped with a chat message, no source recorded.
- Snapshots preserve pre-run state; `GuardConversationalOps` validates LLM
  edit ops before application.

## Project layout

- `Tools/` — `IChatTool` implementations only (`GatherResearchTool`,
  `WebScrapeTool`, `AddEditJournalEntryTool`, `ResearchReviewTool`, …).
- `Tools/<ToolName>/` — helper classes specific to one tool
  (`Tools/GatherResearch/` holds `WebPageHelper`, `YouTubeHelper`,
  `ResearchPage`, the `IResearchSourceHelper` registry).
- `Helpers/` — shared helpers used across tools (`SearchHelper`, `LLMClient`,
  `LLMJson`, `Prompts`, `ContextBudget`, `JournalEntryDocument`,
  `ResearchObject`, `ResearchModuleWriter`, `ResearchBackfill`,
  `ChatDetailLog`).
- Root — `IChatTool` (the tool contract) and `ChatToolRegistry`.

## Debugging

Two dev-only log layers exist under `Collector.Web.Server/logs/chat/`:

- **Event log** (`{chatId}.log`) — every SignalR payload sent to the client:
  progress, raw Ollama request/response, tool steps, chat messages.
- **Detail log** (`details/{chatId}.log`) — `ChatDetailLog`, a debugging
  trace of how the run actually unfolded: the user message, plan starts and
  step-count changes, each tool call, search-engine URLs, the SERP link list
  as `[{title,url,relevant}]`, per-page content dumps
  (`details/{chatId}/NNN.txt`), parser-input reduction lines (`parser input
  cleaned/trimmed`), schema-shaped/unusable dump notices, image keep/reject
  counts, topics/subtopics added to the research object, and every function
  entry inside the tools (`fn {tool}.{method}`, never arguments).

`ChatDetailLog` is ambient — `ChatWorker` assigns `ChatDetailLog.Current`
(AsyncLocal) before executing a plan, so tools and helpers log via static
one-liners with no signature changes. Disabled outside Development and fully
swallows I/O failures.

## Testing

`Collector.Chat.Cli` drives the loop end-to-end: `send` posts a research
prompt over SignalR to `chat-worker` (`http://localhost:7780/worker`), then
`detail`/`dumps`/`dump` inspect the detail log and its dump files, while
`logs`/`read`/`entries`/`entry`/`files` cover event logs, entry JSON, and
downloaded media. Requires `Collector.Web.Server` (Development), Charlotte,
and Charlotte Router all running.
