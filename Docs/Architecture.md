# Architecture

[Home](README.md) > Architecture

Collector is a .NET 8 + React platform for collecting, analyzing, and
organizing web content — with an AI chat system that researches topics and
writes journal entries.

## Solution layout

```
Collector.sln
├── Collector.Web.Server    ASP.NET host: controllers, SignalR hubs, workers DI
├── Collector.Web.Client    React single-page app
├── Collector.API           API controllers (admin/user/public) + services
├── Collector.Auth          JWT auth, account flows, email, remote auth
├── Collector.Data          DAL — Dapper repositories + entities + interfaces
├── Collector.Common        Shared utilities: Article download, DOM, Files,
│                           journal paths, images, syndication
├── Collector.Domains       DomainAnalyzer — crawl/analyze domain structure
├── Collector.Chat          AI chat engine: planner + tools + research pipeline
├── Collector.Chat.Cli      collector-chat CLI — drives workers over SignalR
├── Collector.Workers       SignalR worker framework + background workers
├── Collector.AI            OpenAI client, Ollama config, Speech (TTS/STT)
├── Collector.SQL           PostgreSQL schema project (no migrations)
└── Charlotte/
    ├── Charlotte           headless-browser DOM extraction instance
    ├── Router              Charlotte Web Router — instance pool / load balancer
    └── BlacklistParser     blacklist list parser
```

## Request flow

```
React client ──HTTPS──> Collector.API controllers ──Dapper──> PostgreSQL
     │                        │
     └──SignalR──> Web.Server hubs (/worker, /web-content, /text-editor,
                   /video-download, /chrome-extension)
                          │
                   WorkerHub routes to workers (chat-worker, download-worker,
                   domain-worker, video-worker, youtube-channel-worker)
                          │
                   workers call Ollama (planning/parsing), Charlotte
                   (DOM extraction), search engines, filesystem storage
```

## Storage split

- **PostgreSQL** — relational metadata: users/auth, journals (rows, tags,
  checklists, snapshots index), chats, articles indexes, domains, downloads
  queue, LLM config, search engines, feeds.
- **Filesystem** (`Storage.Articles`/`Storage.Journal`) — heavy payloads:
  `entry.json`, `research.json`, parser dumps, media. See
  [Storing Content](Storing-Content.md).

## Conventions

- Repositories have interfaces (`Collector.Data/Interfaces`), registered in
  `DapperStartupService.cs`; enums in `Collector.Data/Enums`.
- API controllers return `Json(new ApiResponse { … })`; `message` only on
  error; try/catch around repository calls.
- Client API access goes through `src/api/*` classes with the
  `useSession()` context.
- No unit-test projects — live verification via the CLI (see
  `.agents/skills/chat-system/SKILL.md`).
