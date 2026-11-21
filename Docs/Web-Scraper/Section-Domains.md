# Domains

[Home](../README.md) > [Web Scraper](../WebScraper.md) > Domains

**Web → Domains** (`app/web/domains`, `DomainsController`) — the domain
registry: what sites Collector knows about and how it treats them.

## What it manages

- `Domains` — one row per registered domain (https/www flags, type,
  language, paywall flags, title).
- `DomainHierarchy`, `DomainLinks`, `DomainAddresses` — the crawled link
  graph discovered by analysis.
- `AnalyzerRules` + `DomainTypeMatches` — per-domain rules and matches
  used to classify content type.
- `DownloadRules` — which discovered URLs get queued for download.
- `DomainServices`, `DomainServiceNames` — service fingerprinting (e.g.
  which platform a site runs).
- `DomainCollections`, `DomainCollectionGroups` — curated domain sets
  (seeded `DomainCollections-Countries.sql`).

## Processing

`DomainWorker` runs `DomainAnalyzer.AnalyzeAsync` over unanalyzed rows:
downloads the homepage, article-analyzes the DOM, and lets an LLM classify
type/language/paywall — see [Domains](../Domains.md) (the
`Collector.Domains` project).
