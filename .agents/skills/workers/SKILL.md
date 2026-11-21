# Workers

How to add or modify a background worker.

## The contract

- Implement `IWorker` (`Collector.Common`) — `Start`, `Stop`, `Progress`
  methods invoked by name over SignalR.
- Register the route in `Collector.Web.Server/Program.cs`:
  `WorkerRoutes.Register<MyWorker>("my-worker")`.
- Register the type as transient in `Program.cs` service registration.
- Invoke from the client: `workerHub.call('my-worker', 'MethodName',
  { arg1, arg2 })` — JSON args map **by name** to method parameters.

## Lifecycle

`WorkerHub.Call` → `Workers.StartWorker` creates a DI scope, runs the
method on a background task, tracks it as a `WorkerTask`
(workerId/CTS/scope/route/url/customId). `StopWorker` cancels via the CTS —
honor `cancellationToken` in long loops. `SetCustomId`/`SetRoute`/`SetUrl`
+ `GetWorkerByCustomId` correlate spawned sub-workers (see
`YoutubeChannelWorker` spawning `VideoWorker`s, max 3 concurrent).

## Pushing events to clients

Inject `IHubContext<WorkerHub>` and send events — clients filter by
`AppUserId` + `WorkerId`. Existing event names: `PlanProgress`,
`ChatMessage`, `ToolComplete`, `ScrapeStatus`, `GenerateStatus`,
`VideoRecordCreated` + progress events.

## Cookies for restricted downloads

`Workers.RequestCookiesFromClientAsync(appUserId, …)` → signals the user's
Chrome extension via `/chrome-extension` hub → `HandleCookieResponse`
resolves the pending request. Check `HasConnectedExtensions` first; fall
back to saved cookie files (see `VideoWorker.GetCookiesArgumentAsync`).

## Diagnostics

`ChatDebugLog` + `logs/chat/{chatId}.log` + `logs/chat/details/` —
Development-mode only.
