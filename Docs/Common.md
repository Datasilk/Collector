# Common

[Home](README.md) > Common

`Collector.Common` — shared utilities used by the API, workers, and chat
pipeline.

## Content pipeline

- `Article.cs` — the download chokepoint: `Download(url)` → Charlotte
  Router browser fetch or direct HTTP, charset-aware decode (UTF-8-first,
  BOM/meta sniff, Latin-1 fallback), `CleanUrl` normalization + mojibake
  repair.
- `Articles.cs`, `AnalyzedArticle.cs` — article analysis model +
  word/sentence/subject extraction.
- `Html.cs` — `CleanDOM` (strip scripts/styles/chrome attributes),
  `DeserializeArticle` (Charlotte DOM JSON → `DomElement` hierarchy),
  section extraction.
- `DOM.cs` + `Models/DOMNodes.cs` — the indexed `Document`/`DomElement`
  model Charlotte produces (dictionary-indexed tags/attrs/classes).
- `Files.cs` — `Files.Paths` (`Articles`, `Journal`) storage root access.
- `JournalPaths.cs`, `Models/JournalModule.cs`,
  `Models/JournalModules/` — journal file layout + module shapes.
- `Images.cs`, `Videos.cs`, `Syndication.cs` — image ops (ImageSharp +
  TinyPNG), video helpers, RSS/feed syndication.
- `IWorker.cs` — the worker contract implemented by all workers.

## Entities / enums

`Entities/` mirrors scraper-side records (Domain, Download, AnalyzerRule,
Blacklist, Feed, Subject, Word…); `Enums/` shared enum types (QueueStatus,
FeedDocType, DomainType…). `Extensions/` — strings/objects/web helpers.
`Encryption/Sha256.cs` — password/token hashing.
