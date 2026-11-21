# Downloads

[Home](../README.md) > [Web Scraper](../WebScraper.md) > Downloads

**Web → Downloads** (`app/web/downloads`, `DownloadsController`) — the
collection queue: URLs waiting to be fetched and what's been fetched.

## What it manages

- `DownloadQueue` — pending URLs, each with a `QueueStatus` and the domain
  rule that queued it.
- `Downloads` — completed/failed download records.

## Processing

`DownloadWorker.Start(feedId, domain, sort, ct)`:

1. Checks due **feeds** first (`Feed.checkIntervals`).
2. Loops the queue — waits when empty.
3. Per item: validates domain + URL, applies `DownloadRules`, downloads,
   follows redirects, validates the result, saves the `Articles` row,
   extracts discovered links back into the queue, archives the item
   (failures archive too — nothing is reprocessed).
