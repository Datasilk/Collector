# Web Scraper

[Home](README.md) > Web Scraper

Collector's rule-based web collection system — the **Web** section of the
app (`app/web/`). Where the [Chat](Chat.md) pipeline *researches* topics,
the Web Scraper *collects* domains systematically: crawl → analyze →
download → index.

## How it works

```
Domain → DomainAnalyzer (links, services, content-type matches)
       → DownloadRules → DownloadQueue
       → DownloadWorker → article storage
       → analysis → Articles + sentences/subjects/words/dates indexes
```

Whitelists and blacklists bound what can be collected; subjects and the
word taxonomy (`Words`, `CommonWords`, `Dictionary`, `Statistics`) drive
the analysis that classifies article content.

## Sections

One document per side-menu section of the Web portal:

- [Domains](Web-Scraper/Section-Domains.md) — domain records, hierarchy,
  analyzer rules, collections
- [Downloads](Web-Scraper/Section-Downloads.md) — the download queue and
  what `DownloadWorker` does with it
- [Articles](Web-Scraper/Section-Articles.md) — analyzed article index
  (sentences, subjects, words, dates)
- [Subjects](Web-Scraper/Section-Subjects.md) — the subject taxonomy
- [Whitelists](Web-Scraper/Section-Whitelists.md) — always in-scope domains
- [Blacklists](Web-Scraper/Section-Blacklists.md) — excluded domains and
  wildcards
