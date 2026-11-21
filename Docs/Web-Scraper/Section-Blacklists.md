# Blacklists

[Home](../README.md) > [Web Scraper](../WebScraper.md) > Blacklists

**Web → Blacklists** (`app/web/blacklists`, `BlacklistsController`) —
domains and patterns excluded from collection.

## What it manages

- `Blacklist_Domains` — exact-domain exclusions.
- `Blacklist_Wildcards` — pattern exclusions (e.g. country-code TLD
  wildcards; `Blacklist.sql` seeds them).

`Charlotte/BlacklistParser` parses bulk list files into these tables, and
each Charlotte instance also honors `Settings.BlacklistedDomains` at the
browser level.
