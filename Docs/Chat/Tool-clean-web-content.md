# clean-web-content

[Home](../README.md) > [Chat](../Chat.md) > clean-web-content

`CleanWebContentTool` — Ollama-based relevance filtering of scraped
content.

## Behavior

- Scores scraped/candidate pages against the research topic; off-topic
  results (ambiguous keyword hits, generic institutional homepages, nav
  chrome) are rejected before integration.
- Used both as a pre-scrape gate on SERP candidates and as a cleaner on
  extracted content when the dump is noisy.
