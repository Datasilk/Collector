# Storing Content

[Home](README.md) > Storing Content

Collector persists downloaded/extracted content to the filesystem today;
Azure Blob is the convention for future media upload paths.

## Filesystem roots

`appsettings.json → Storage` configures two roots (`Files.ArticlesPath`,
`Files.JournalPath`, e.g. `M:\Collector\Content\...`):

- **articles/** — scraped article bodies/indexes from the Web-menu pipeline.
- **journals/** — everything journal-related (see below).

`Collector.Common/Files.cs` (`Files.GetFile/SaveFile/GetFileBytes…`) is the
access layer — it resolves `Paths.Articles`/`Paths.Journal` + relative path
to a full path; callers pass relative paths only.

## Journal entry layout

`journals/{journalId}/{entryId}/`:

| Path | Contents |
|---|---|
| `entry.json` | the module tree (rendered entry) |
| `research.json` | structured research: topics/subtopics, comments, sources, searches (v2) |
| `research/{####}.json` | per-source parser-model dump |
| `research/{####}.html` | cleaned HTML sidecar the parser consumed |
| `snapshots/{id}.json` | entry snapshots (JournalEntrySnapshots) |
| `images/` | downloaded images |
| `files/` | downloaded files (PDFs, CSVs, docs, audio…) |
| `videos/{videoId}/` | `raw.ext`, `thumb.jpg`, `hls/`, `previews/` |

`_staging/` under `journals/` holds transient files (e.g. cleaned HTML
staged before the parser runs, moved into `research/` on dump save).

## Media pipeline

`Collector.API/Services/MediaService.cs` is the convention point for
processing + uploading images/files to **Azure Blob Storage** — new
upload-heavy APIs should add a MediaService method storing under a
controller-named folder (see project rules). Image processing uses
SixLabors.ImageSharp; TinyPNG compression is available via `Images.cs`.
Video assets are produced by `VideoWorker` (raw + thumbnail + HLS +
previews).

## Database vs filesystem

Metadata/rows (chats, journal records, search engines, LLM models,
articles indexes) live in PostgreSQL; heavy payloads (entry JSON, dumps,
media) live on the filesystem. The `research.json` is served to the client
via `GET /api/journals/entries/{id}/research`.
