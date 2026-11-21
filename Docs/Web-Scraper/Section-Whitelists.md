# Whitelists

[Home](../README.md) > [Web Scraper](../WebScraper.md) > Whitelists

**Web → Whitelists** (`app/web/whitelists`, `WhitelistsController`) —
domains explicitly in scope.

## What it manages

- `Whitelist_Domains` — the allow-list consulted during collection and
  queue filtering: whitelisted domains are always eligible to download
  regardless of other rules.
