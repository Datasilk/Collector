# Web Server

[Home](README.md) > Web Server

`Collector.Web.Server` — the ASP.NET 8 host: API + SignalR hubs + workers
DI + SPA static files, one process.

## Service registration (`Program.cs`)

- CORS, `HttpClient`, `HttpContextAccessor`, health checks.
- SignalR with raised `MaxMessageSize` (large cache payloads).
- Controllers + `LowercaseUrls` routing; Swagger (`EnumSchemaFilter` —
  enums render by name).
- Worker types as transients: `VideoWorker`, `YoutubeChannelWorker`,
  `YouTubeEntryCreator`, `ChatWorker`, `DownloadWorker`, `DomainWorker`.
- Kestrel/IIS/`FormOptions` enlarged for uploads.

## Ollama bootstrap

`Ollama:*` config → `Collector.AI.Ollama.Config` (`Url` default
`http://localhost:11434`, `Model` default `qwen2.5:0.5b`, `UseGpu`,
`NumGpu`, `Temperature` 0.7, `AutoPullModel` true). Singleton
`OllamaApiClient` with a **15-minute** `HttpClient` timeout. If
`AutoPullModel` and Ollama is reachable, startup pulls the configured
model **and `nomic-embed-text`** when missing; on connection failure it
warns and continues (AI chat disabled until Ollama is up).

## Storage roots

`Storage:Articles` → `Files.ArticlesPath`, `Storage:Journal` →
`Files.JournalPath` (defaults: `Content/articles` +
`Content/journal-entries` under the app dir).

## Startup data load

A DI scope loads enabled `LLM_Models` rows into
`Collector.AI.OpenAI.Available` (`Loaded N enabled LLM model(s)`), plus
repositories via `DapperStartupService`.

## Pipeline

- Non-dev: `/error/server-error` + status-code re-execute + HSTS +
  `/healthcheck`. Dev: Swagger UI at `/swagger`.
- `UseCors` → custom context middleware → auth → **hubs mapped BEFORE
  static files**:
  `/text-editor`, `/video-download`, `/web-content`, `/worker`,
  `/chrome-extension`.
- Worker routes registered, `Workers.SetHubContext` wired.
- Static files with explicit `.svg` MIME mapping, controllers,
  `MapFallbackToFile("index.html")` (SPA fallback).

## Environment matters

`ASPNETCORE_ENVIRONMENT=Development` → Swagger + `logs/chat/*.log` +
`logs/chat/details/*` — required for live chat testing.
