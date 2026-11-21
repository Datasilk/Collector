# Chat

[Home](README.md) > Chat

Collector's AI chat system: a planner running on Ollama turns the user's
message into a JSON execution plan, then executes it step by step through
registered `IChatTool`s. Ported from `Collector.Chat/README.md` and
`Collector.Chat/ResearchSystem.md`.

## Architecture

```
React client ──SignalR──> WorkerHub ("chat-worker", "SendMessage")
                              │
                        ChatWorker (Collector.Workers)
                              │  creates and populates
                  ┌───────────▼────────────┐
                  │    ChatToolRegistry    │   Collector.Chat
                  │  · BuildPlanningPrompt │
                  │  · ExecutePlan         │
                  └───────────┬────────────┘
              ┌───────────┬───┴────┬───────────────┐
        gather-research  web-scrape  clean-web-content  add-edit-journal-entry
```

`Collector.Chat` contains neither transport nor UI — `ChatWorker` invokes it
via SignalR. The client calls
`workerHub.call('chat-worker', 'SendMessage', { chatId, message, model, mode })`;
`WorkerHub` resolves the worker type through `WorkerRoutes`, creates a DI
scope, and runs the worker on a background task. Progress, messages, and
tool events stream back over the same SignalR connection.

## Execution plans

The planner returns a JSON plan (`OllamaPlanResponse` → `PlanStep`s wrapped
in an `ExecutionPlan`). For a research request it's typically a single
`gather-research` step; **tools extend the plan at runtime** —
gather-research appends `web-scrape` + `add-edit-journal-entry` pairs per
URL plus a `research-review` step, and the review can queue more
`gather-research` steps until coverage is satisfied or the page budget is
exhausted.

Shared state flows through `data`
(`List<Dictionary<string, List<string>>>`): scraped pages, research
questions, asked queries, planned pages, session-entry markers, deepening
flags.

## Research pipeline

1. **Planning** — Ollama returns the initial plan.
2. **gather-research** — generates a distilled 2–8-word query and up to 8
   research questions; searches enabled `SearchEngine` rows (direct HTTP
   first, Charlotte headless fallback); extracts SERP result URLs; filters
   relevance; queues scrape+integrate steps. Also discovers site search
   engines (static forms, JS endpoint analysis, browser probing) and
   registers them for reuse.
3. **web-scrape** — downloads a URL via `Common.Article.Download` →
   Charlotte DOM → `Html.CleanDOM` → Ollama HTML-parser model → structured
   dump (page_title, sections, blocks, images, comments) saved as
   `research/{####}.json` with the cleaned HTML sidecar. When the dump is
   unusable, `ContentSectionsFromDom` chunks the raw DOM into bounded
   fallback sections (chrome ancestry filtered). A relevance gate can skip
   off-topic pages.
4. **add-edit-journal-entry** — merges each page into the session's entry:
   topic-assign maps sections to `research.json` topics/subtopics (new
   topics created as needed, boilerplate/nav sections skipped), comments
   attach to research.comments, sources/searches indices are maintained,
   and `ResearchModuleWriter` renders dirty topics into entry modules.
   With `instruction` only, it applies conversational edits (move/add/
   remove/update ops) to the existing entry.
5. **research-review** — compares coverage to the research questions;
   retires unanswerable page-scoped questions; queues gap queries.

## Journal entry storage

`journals/{journalId}/{entryId}/`: `entry.json` (module tree),
`research.json` (topics, subtopics, comments, sources, searches — v2
schema), `research/{####}.json` + `{####}.html` per-source dumps,
`snapshots/`, `images/`, `files/`, `videos/{videoId}/`. The web client loads
`research.json` via `GET /api/journals/entries/{id}/research`.

## Built-in tools

One document per registered `IChatTool`:

- [Gather Research](Chat/Tool-gather-research.md)
- [Web Scrape](Chat/Tool-web-scrape.md)
- [Add Edit Journal Entry](Chat/Tool-add-edit-journal-entry.md)
- [Research Review](Chat/Tool-research-review.md)
- [Clean Web Content](Chat/Tool-clean-web-content.md)
- [Manage Ollama Models](Chat/Tool-manage-ollama-models.md)

## Testing

Drive live runs with the `collector-chat` CLI — see
`.agents/skills/chat-system/SKILL.md`.

## Shared data keys

State passed between plan steps via `data`
(`List<Dictionary<string, List<string>>>`) — key strings used by the tools:

- `scraped-pages` — `ResearchPage` objects queued for integration
- `research-questions` — the up-to-8 questions defining "done"
- `asked-queries` — raw + distilled queries already run (dedupes the
  coverage loop)
- `planned-pages` — URLs already queued as scrape steps (dedupe)
- `session-entry` — the journalId/entryId marker binding the run to one
  entry
- `deepening` flags — marks follow-up rounds so the review knows it's
  already iterated
- `direct-url-pages` / `skipped-pages` — URL-direct targets + relevance
  rejects, so the review retires their page-scoped questions

## Chat persistence & memory

`Chats` + `ChatHistory` store the conversation; `ChatContextChunks` store
retrieved context pieces the worker feeds the model. Development-mode
diagnostics write `logs/chat/{chatId}.log` and `logs/chat/details/*`
(plans, tool args, prompt payloads, content dumps).
