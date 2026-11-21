# Domains

[Home](README.md) > Domains

`Collector.Domains/DomainAnalyzer.cs` — the single-domain analysis core,
shared by `DomainWorker` (batch loop) and the chat research pipeline
(inline analysis when a new domain is registered during research).

## `AnalyzeAsync` pipeline

1. Builds the homepage URL from the domain row
   (`http{https?'s'}://{www?'www.'}{domain}`).
2. Downloads it through the shared `Common.Article` downloader.
3. Deserializes + article-analyzes the DOM.
4. Sends it to an LLM for classification — injected as an `LlmCall`
   delegate `(system, user) → text` so each caller picks its endpoint
   (worker → `OpenAI.Prompt`, chat → `LLMClient`).
5. Persists type / language / paywall / title into `Domains` — preserving
   existing values for any field the LLM returned empty.

## `Result` / `Status`

`Result`: `DomainId`, `Domain`, `Type`/`Type2` (`DomainType` — e.g.
unused/article/video), `Language`, `RequiresSubscription`,
`HasFreeContent`, `IsEmpty`, `Failure`. Every step is best-effort —
failures map to `Status` values rather than throwing; cancellation is
honored inside the download + LLM call.

## Schema (`Domains/` folder)

- `Domains`, `DomainHierarchy`, `DomainLinks`, `DomainAddresses` — the
  domain graph.
- `AnalyzerRules`, `DomainTypeMatches` — analysis rules + matches.
- `DownloadRules` — which discovered URLs become queue items.
- `DomainServices`, `DomainServiceNames` — service fingerprinting.
- `DomainCollections`, `DomainCollectionGroups` — curated sets
  (`DomainCollections-Countries.sql` seed).

`DomainWorker` runs `AnalyzeAsync` over unanalyzed domain rows
(`AnalyzeDomain` for a single row). Client UI: **Web → Domains**
(`app/web/domains`, `DomainsController`).
