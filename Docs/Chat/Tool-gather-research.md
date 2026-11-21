# gather-research

[Home](../README.md) > [Chat](../Chat.md) > gather-research

`GatherResearchTool` — the only tool exposed to the planner. Everything
else is queued by tools as the plan executes.

## Args

`query` (optional), `title`, `journalId`, `url`, `maxResults` (cap 10).

## Behavior

1. **Query.** Uses `query` if given (research-review follow-ups carry it);
   otherwise generates a 2–8-word keyword query via
   `GenerateSearchQueryAsync` / `DistillToKeywords` — natural-language
   questions are distilled because engines fixate on single words in long
   questions. A step with an explicit `query` is a search step: URLs in the
   original user message do NOT hijack it (prevents re-download loops).
2. **Research questions.** First research step generates up to 8 questions
   defining "done"; stored as `research-questions`.
3. **Engine search.** Iterates enabled `SearchEngine` rows by priority via
   `SearchHelper.SearchEngineUrlsAsync` — direct HTTP first, Charlotte
   headless fallback (plain HTTP dodges bot-detection). `ExtractResultUrls`
   pulls links, unwraps redirectors, drops file types and junk domains.
4. **Relevance filter.** Candidate pages are scored/gated before scraping
   (see `clean-web-content`); off-topic pages are skipped, not scraped.
5. **Plan extension.** Inserts `web-scrape` + `add-edit-journal-entry`
   pairs per URL, then a `research-review` step — the plan grows
   dynamically.
6. **Search-engine discovery.** `ProbeSearch`-style discovery registers a
   site's own search form/endpoint (static forms, JS endpoint analysis,
   browser probing) as a `SearchEngine` row for future reuse.

URL-direct requests (`url` arg or URLs in the user message) bypass search —
the page itself becomes the research target and its questions are
page-scoped.
