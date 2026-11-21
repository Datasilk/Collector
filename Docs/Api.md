# API

[Home](README.md) > API

`Collector.API` — REST controllers consumed by the React client.

## Conventions

- Every controller inherits `ApiController`.
- Every response is `Json(new ApiResponse { … })`; `message` is used
  **only** for errors.
- `POST` for model-bodied calls; `GET` for no/simple parameters.
- Repository calls wrapped in try/catch → `ApiResponse.message = ex.Message`.
- Admin controllers live in `Controllers/Admin/`; user/manager and public
  under `Controllers/`.

## Controller inventory

| Area | Controllers | Notes |
|---|---|---|
| Account/session | `Session`, `Users` | |
| Journal | `Journals`, `JournalCheckLists`, `JournalSnapshots`, `JournalTags` | |
| Files/media | `Files`, `Images`, `Videos` | uploads, stored paths, `JournalFiles`/`JournalImages`/`JournalVideos` metadata |
| Web scraper | `Articles`, `Blacklists`, `Domains`, `Downloads`, `Subjects`, `Whitelists` | see [Web Scraper](WebScraper.md) |
| Feeds | `Feeds` | see [Feeds](Feeds.md) |
| AI | `Chats`, `LLMs`, `LLMModels`, `LLMHtmlParsers`, `Ollama`, `Languages` | |
| Web content | `Cookies`, `SearchEngines` | cookie store for restricted downloads; registered/discovered engines |

## Services

- `ApiAppService` / `ApiStartupService` — DI bootstrap.
- `EmailService` — transactional email (auth flows).
- `MediaService` — media processing + Azure Blob uploads (new upload APIs
  add a method here; folder per controller name).
