# Collector.Chat

.NET 8 library implementing the **core of Collector's AI chat system**: a planner
running on [Ollama](https://ollama.com) (via OllamaSharp) that turns the user's
message into a **JSON execution plan**, plus an engine that executes that plan step
by step through **registered tools** (`IChatTool`).

This project **contains neither the transport layer nor the UI**. It is consumed by
`ChatWorker` (in `Collector.Workers`), which is itself triggered via SignalR by the
React client. See "End-to-end flow" below.

---

## Architecture overview

```
React client ──SignalR──> WorkerHub ("chat-worker", "SendMessage")
                                │
                          ChatWorker (Collector.Workers/ChatWorker.cs)
                                │  creates and populates
                    ┌───────────▼────────────┐
                    │    ChatToolRegistry    │   this project
                    │  · BuildPlanningPrompt │
                    │  · ExecutePlan         │
                    └───────────┬────────────┘
                ┌───────────┬───┴────┬───────────────────┐
          gather-research  web-scrape  clean-web-content  add-edit-journal-entry
```

| File | Role |
|---|---|
| `IChatTool.cs` | Tool contract + callback delegates + `PlanUtility` |
| `ChatToolRegistry.cs` | Tool registry/lookup, planning prompt generation, plan execution loop |
| `Models/ToolModels.cs` | JSON models: `OllamaPlanResponse`, `PlanStep`, `ExecutionPlan`, `ToolDefinition`, etc. |
| `Tools/GatherResearchTool.cs` | Web research (the only tool exposed to the planner) |
| `Tools/WebScrapeTool.cs` | HTML page download + structured extraction |
| `Tools/CleanWebContentTool.cs` | Ollama-based relevance filtering of scraped content |
| `Tools/AddEditJournalEntryTool.cs` | Creates/updates journal entries from cleaned content |

---

## End-to-end flow

### 1. Trigger

The client calls `workerHub.call('chat-worker', 'SendMessage', { chatId, message, model, mode })`.
`WorkerHub.Call` resolves `ChatWorker` through the `"chat-worker"` route (registered in
`Collector.Web.Server/Program.cs`), creates a DI scope and fires `SendMessage` on a
`Task.Run` via `Workers.StartWorker` — JSON arguments are mapped **by name** to the
method parameters.

### 2. `ChatWorker.SendMessage` (orchestrator)

1. **Validation** — if a `chatId` was provided, verifies the chat exists and belongs to the user.
2. **Active model** — reads `OllamaModelsRepository.GetActive()`; falls back to
   `AI.Ollama.Config.Model`.
3. **RAG context** — embeds the message with `nomic-embed-text`, fetches up to 20
   chunks from `IChatsRepository.GetContext` (vector distance ≤ 0.3) and asks Ollama
   for a **summary** of that context. On failure, proceeds without context.
4. **Planning** — builds the prompt via `ChatToolRegistry.BuildPlanningPrompt` and calls
   `GenerateAsync` with `Format = "json"`. If the model is missing from the local
   Ollama (HTTP 404), the worker attempts `PullModelAsync` (reporting progress to the
   client) and retries the call.
5. **Parse** — deserializes into `OllamaPlanResponse`. JSON failure → raw response
   becomes a direct answer.
6. **Chat creation** — creates the chat (title = first 50 chars of the message) if new,
   and saves the user message to `ChatHistory` (role 0).
7. **Dispatch** — `plan.IsPlan` (steps > 0) → plan execution; otherwise → direct answer.

### 3. Planning phase (`BuildPlanningPrompt`)

The prompt instructs the model to reply with **only** JSON in this shape:

```json
{
  "message": "What I will do / the answer",
  "steps": [
    { "tool": "gather-research", "args": { "context": "...", "maxResults": "5" } }
  ]
}
```

- `steps` present → it's a **plan**; absent → direct answer in `message`.
- The tool catalog embedded in the prompt lists **only** tools with
  `AvailableToOllama == true` (currently just `gather-research`), each with its
  description and parameters (`key: type`). The remaining tools can only enter the
  plan through dynamic insertion (see below).
- The summarized RAG context and the user message close out the prompt.

> `BuildToolsContext()` also serializes full tool definitions (`parameters` +
> `response` per tool) in camelCase — available for consumers that want the catalog
> in structured form.

### 4. Plan execution (`ExecutePlan`)

The loop runs **sequentially** over `plan.Steps`, forwarding callbacks to each
`tool.Run`:

- `onStepProgress(percent, msg)` — each tool's percentage is normalized into its
  slice of the plan (`[k/N]`), so the client's progress bar advances 0→100 across
  all steps.
- `onToolError` / `onToolComplete` — errors **do not abort** the plan: the step is
  reported and the loop continues. A step with an unknown `tool` is also skipped
  with an error.
- `totalSteps` is re-read every iteration — tools can **add steps to the plan at
  runtime** via `PlanUtility.AddStepToPlan` / `AddStepsToPlan` (inserted right after
  the current step).

`ChatWorker` converts these callbacks into SignalR `WorkerProgress` events
(`PlanProgress`, `ToolError`, `ToolComplete`, `ChatMessage`, …) — see "Events".

### 5. Dynamic plan expansion

`gather-research` is the trigger for the full pipeline. Once it discovers `N` URLs, it
inserts into the plan, **immediately after itself**:

```
gather-research → web-scrape ×N → clean-web-content → add-edit-journal-entry
```

This is why `web-scrape`, `clean-web-content` and `add-edit-journal-entry` have
`AvailableToOllama == false`: the planner only needs to know "do research"; the
expansion is deterministic and done by the tool's own code.

### 6. Data bus between tools

All tools share the `data` list
(`List<Dictionary<string, List<string>>>`), where **each item is a document**
identified by its `type` field:

| `type` | Produced by | Fields |
|---|---|---|
| `web-scrape` | `WebScrapeTool` | `url`, `title`, `headers`, `content`, `links`, `images`, `layout` |
| `content` | `CleanWebContentTool` | `headers`, `content`, `images`, `layout`, `sourceUrl`, `sourceTitle` |

`layout` is an ordered list of `"header:i" | "content:i" | "image:i"` references
preserving the document's original order — consumers rehydrate the indices into each
list. Content under 20 chars and `script`/`style`/comment elements are discarded
during extraction.

### 7. Persistence and memory

- **Messages**: `ChatHistory` (role 0 user, 1 assistant) via `IChatsRepository`.
- **Facts (RAG)**: `ExtractAndStoreFactsAsync` asks Ollama for a JSON list of facts
  from the turn (and, during plans, also from scraped text via `onSaveChatHistory`),
  embeds each fact with `nomic-embed-text` and stores it via `StoreContext` —
  skipping duplicates through `HasSimilarContext` (similarity < 0.1).
- **Journal entries**: `JournalEntry` in the database + content file
  `Files.Paths.Journal/{entryId:N}.json` in the form `{ id, modules: [...] }`, with
  `title`, `text-editor` (HTML) and `image` modules rebuilt in `layout` order.

---

## Tool catalog

| ToolKey | Ollama | Description | Main parameters |
|---|---|---|---|
| `gather-research` | ✅ | Generates a query via Ollama, searches Google (Bing/DDG commented out), filters URLs and expands the plan | `context`, `maxResults` (def. 5) |
| `web-scrape` | — | Downloads a page via `Charlotte.Download`, pushes analyzed DOM (`Html.*`) into the data bus | `url` |
| `clean-web-content` | — | Asks Ollama (`{"relevant": bool}`) about each header/section, associates images with the relevant section | `userMessage` |
| `add-edit-journal-entry` | — | Dedupes by title similarity (embedding, dist ≤ 0.3), picks category/journal via Ollama (creating new ones if needed), builds modules and saves | `title`, `journalId`, `description`, `content`, `updateTitle` |

Behavior notes:

- **`GatherResearchTool`** filters ads, internal search-engine links, social networks
  and repeated domains, strips tracking parameters (`utm_*`, `gclid`…) and dedupes by
  host. The query is generated by the model (2–8 words) with fallback to the raw
  message.
- **`CleanWebContentTool`** evaluates each `layout` item against the user's question +
  RAG context + page title, using the previous relevant header as context. Evaluation
  failure → assumes relevant (fails open, doesn't lose content).
- **`AddEditJournalEntryTool`** generates title/description via Ollama when absent,
  searches for a similar entry by title embedding (updates and **appends** modules if
  found; `updateTitle` controls renaming + re-embedding), otherwise picks
  category → journal with two JSON prompts (`category-selection`,
  `journal-selection`) and creates the entry. The resulting `entryId` is written to
  `plan.Steps[i].Result["entryId"]` for subsequent steps.

---

## `IChatTool` contract

```csharp
Task Run(string userMessage, string ragContext,
         List<Dictionary<string, List<string>>> data,
         OnProgress onProgress, OnError onError, OnComplete onComplete,
         Guid appUserId,
         ExecutionPlan? plan = null, int currentStepIndex = -1,
         OnRawRequest? onRawRequest = null, OnRawResponse? onRawResponse = null,
         OnSaveChatHistory? onSaveChatHistory = null, SendMessage? sendMessage = null);
```

Properties: `ToolKey` (key used in plans, kebab-case), `Description`,
`Parameters`, `ResponseFields` (metadata serialized into the model context) and
`AvailableToOllama` (visibility in the planning prompt).

Delegates (`IChatTool.cs`):

| Delegate | Use |
|---|---|
| `OnProgress(percent, msg)` | 0–100 progress within the step |
| `OnError(msg, ex?)` | Recoverable step error |
| `OnComplete(msg)` | Step completion |
| `OnRawRequest/OnRawResponse(type, payload)` | Debug: raw Ollama prompt/response (Development only) |
| `OnSaveChatHistory(context)` | Inject fetched content (e.g. scraped text) into fact extraction |
| `SendMessage(msg, linkUrl?, linkText?)` | Message with optional link for the chat window |

---

## SignalR events emitted by `ChatWorker`

All through `WorkerProgress(appUserId, workerId, eventName, payload)`:

`Planning` · `ChatCreated` · `AssistantPlan` · `PlanStarted` · `PlanProgress`
(with `replace: true`) · `ToolError` · `ToolComplete` · `PlanCompleted` ·
`ChatMessage` · `ChatResponse` · `ChatError` · `Debug` /
`OllamaRequest` / `OllamaResponse` (these last three Development only).

At the end of a plan, the worker also generates a **final summary** with Ollama and
saves it as the assistant message.

---

## Adding a new tool

1. Create `Tools/MyTool.cs` implementing `IChatTool`.
2. Register it in `ChatWorker`: `_toolRegistry.RegisterTool(new MyTool(...))`.
3. If it should be callable by the planner, expose `AvailableToOllama => true` with a
   clear `ToolKey` + `Parameters`/`Description` (the model decides based on those
   alone).
4. If it's an internal pipeline step, keep `AvailableToOllama => false` and insert it
   via `PlanUtility` from a trigger tool.
5. Read args from `plan.Steps[currentStepIndex].Args` (values arrive as `object`;
   convert with `ToString()`/`TryParse`), report progress/errors through the
   callbacks and publish results to the `data` bus or to `Step.Result`.

---

## Dependencies

- `OllamaSharp` 5.4.18 — local Ollama HTTP client (`GenerateAsync`, `EmbedAsync`,
  `PullModelAsync`).
- `Collector.Data` — Dapper repositories (chats, journals, entries, categories,
  ollama-models).
- `Collector.Common` — `Charlotte.Download` (page download/DOM), `Html.*`
  (article analysis), `Files` (content persistence), `AI.Ollama.Config`.
- `Microsoft.AspNetCore.App` (framework reference).

Ollama models used: the **active model** (database/config) for planning, relevance,
selections and summaries; **`nomic-embed-text`** hardcoded for all embeddings.

## Known caveats and limitations

- The planner requires well-formed JSON; malformed responses degrade to direct
  answers (no plan executed).
- Search currently uses **Google only** — `bing`/`duckduckgo` are commented out in
  `GatherResearchTool`.
- `PlanStep.Args` carries values as untyped `object` — always convert.
- Cross-step progress normalization assumes the step count **at the time**; dynamic
  expansions rescale on the next step.
- `WebScrapeTool` depends on the `Charlotte` router to download pages; failures
  (`"Error"`/`"file:"`) end the step with an error.
