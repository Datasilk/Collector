# Search Engines

[Home](README.md) > Search Engines

The `SearchEngines` table + `SearchEnginesController` drive where
`gather-research` finds candidate URLs.

## Engine types

- **Web engines** — global search endpoints iterated by priority;
  `SearchHelper.SearchEngineUrlsAsync` tries direct HTTP first, Charlotte
  headless as fallback.
- **Site engines** — a domain's own search (e.g. biorxiv's
  `keywords={search}` POST form). `gather-research` **discovers** them at
  runtime — static forms, JS endpoint analysis, and ProbeSearch (browser
  drives the site's search input with a probe term and captures the
  request it fires, Playwright backend only) — and registers them for
  reuse.

## Admin surface

**Admin → Search Engines** manages rows (enable/disable, priority,
endpoint template). `SearchEngine` entity + `SearchEnginesRepository`.
Note: discovered POST forms may embed session-bound params
(`form_build_id`) that expire — stale replay is a known limitation.
