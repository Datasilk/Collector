# Workers

[Home](README.md) > Workers

`Collector.Workers` — the SignalR worker framework plus the background
workers that do all long-running work.

## Framework (`SignalR/`)

- `IWorker` (`Collector.Common`) — the worker contract (`Start`, `Stop`,
  `Progress`).
- `WorkerRoutes` — static `route → worker type` registry, populated in
  `Program.cs`: `video-worker`, `youtube-channel-worker`, `chat-worker`,
  `download-worker`, `domain-worker`.
- `WorkerHub` (`/worker`) — `Call(route, method, args)` resolves the type,
  builds a DI scope, maps JSON args **by name** to method parameters, and
  hands off to `Workers.StartWorker`.
- `Workers` — static orchestration. `WorkerTask` records
  (workerId/task/CTS/scope/route/url/customId); per-user worker lists;
  `StartWorker`/`StopWorker`; `SetCustomId`/`SetRoute`/`SetUrl` +
  `GetWorkerByCustomId` for correlating spawned sub-workers;
  per-user connection registry; and the **cookie-request channel**
  (`RequestCookiesFromClientAsync` → pending `TaskCompletionSource` →
  `HandleCookieResponse` — see [Chrome Extension](Chrome-Extension.md)).
- Worker→client pushes go through `IHubContext<WorkerHub>`
  (`Workers.SetHubContext`, `SendWorkerMessage` — broadcast then filtered
  per `AppUserId`/`WorkerId`).

## ChatWorker (`ChatWorker.cs`, ~1.5k lines)

Hosts `Collector.Chat` — `SendMessage` runs the plan loop, streams
`PlanProgress`/`ChatMessage`/`ToolComplete` per step, manages memory via
`ChatContextChunks`, writes `logs/chat/{chatId}.log` + `logs/chat/details/`
in Development mode (`ChatDebugLog`). See [Chat](Chat.md).

## DownloadWorker (`DownloadWorker.cs`)

`Start(appUserId, workerId, feedId, domain, sort, ct)`:

1. **Feeds first** — checks due feeds (`Feed.checkIntervals`) before the
   queue.
2. `DownloadQueue` loop — polls for items, waits when empty.
3. Per item: validates domain + URL, applies `DownloadRules`, downloads,
   follows redirects, validates results, saves the `Articles` row,
   **extracts + queues discovered links** back into the queue, archives
   the queue item (failed downloads archive too — never reprocessed).
4. `DeleteAllArticles(domainId)` — bulk purge helper.

## DomainWorker (`DomainWorker.cs`)

- `Start(...)` — loop over unanalyzed domain rows, running
  `DomainAnalyzer.AnalyzeAsync` (LLM delegate → `OpenAI.Prompt`).
- `AnalyzeDomain(appUserId, workerId, domainId, ct)` — one-shot single
  domain analysis.

## VideoWorker (`VideoWorker.cs`, ~1k lines)

- `DownloadVideo(appUserId, workerId, url, journalId, entryId, moduleId,
  ct)` — yt-dlp download with staged progress events: `VideoRecordCreated`
  first (so the client can set up the module), download %, then **HLS
  playlist generation** (the 70–90% span), thumbnail extraction; sets the
  entry thumbnail to the video thumb when the entry has none.
- `GenerateHlsForExistingVideo(videoPath)` — backfill HLS (0–100%).
- `GetCookiesArgumentAsync` — reuses saved yt-dlp cookies or requests fresh
  ones from the user's Chrome extension (cookie channel above); saves them
  to the main cookie file for reuse.

## YoutubeChannelWorker (`YoutubeChannelWorker.cs`)

`ImportChannel(appUserId, workerId, channelUrl, journalId, tagIds,
parentEntryId, ct)` — full channel import:

1. Downloads the channel banner → image module + entry thumbnail.
2. Creates the **channel entry** — an entries-list module filtered by the
   selected tags (tags go on the module filter + each video entry, not on
   the channel entry).
3. Creates one journal entry per video (tagged), progress weighted
   `totalVideos * 2` (entry + download units).
4. Spawns `VideoWorker` downloads for each entry — **max 3 concurrent**
   (`_videoTasks` list).
5. `Stop` also stops the spawned video workers.

## YouTubeEntryCreator (service)

Shared YouTube→entry logic used by `WebContentHub.ScrapeUrl` and
`YoutubeChannelWorker`: yt-dlp metadata, URL normalization (strips
`list=`/playlist params, matches `youtube.com/watch|embed|v` + `youtu.be`),
saved-cookies check, entry + video module assembly.

## Diagnostics

`ChatDebugLog` + `logs/chat/{chatId}.log` + `logs/chat/details/` —
Development-mode only (`IHostEnvironment.IsDevelopment()`).
