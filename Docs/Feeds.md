# Feeds

[Home](README.md) > Feeds

RSS/web feed monitoring — the **Feeds** area of the client (`app/feeds/`),
backed by `FeedsController` + `FeedsRepository` (stored functions like
`Feed_Add`, `Feed_GetInfo`, `Feed_Checked`, `FeedCheckedLog_Add`).

## Model

- `Feed` — `{feedId, domainId, doctype, title, url, checkIntervals,
  lastChecked, filter, category/categoryId}`.
- `FeedCategory` — user-facing grouping.
- `FeedWithLog` — feed + last check log (link count, check date).
- `checkIntervals` — minutes between checks (default 720 = 12h); `filter`
  scopes which links are collected.
- `FeedDocType` enum distinguishes feed item types.

`Common/Syndication.cs` parses RSS/Atom; checked links land in
`FeedsCheckedLog` and feed the downloads/articles pipeline.
