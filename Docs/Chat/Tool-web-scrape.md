# web-scrape

[Home](../README.md) > [Chat](../Chat.md) > web-scrape

`WebScrapeTool` — downloads one URL and produces a structured `ResearchPage`.

## Args

`url`, `title`, `journalId`, `index` (source id).

## Pipeline

1. `Common.Article.Download` → Charlotte Router → browser DOM →
   `Html.CleanDOM` (strips scripts/styles/chrome attrs).
2. Cleaned HTML → active **HTML-parser model** (Ollama) → JSON dump
   conforming to `WebScraper.md`: `page_title`, `sections[]` with
   `heading` + `blocks[]` (`paragraph`/`list`/`table`/`blockquote`/
   `heading`), `images[]`, `comments[]`.
3. Dump is persisted as `research/{####}.json` alongside the cleaned
   `{####}.html` sidecar.
4. **Fallback** — when the parser dump is unusable/empty,
   `WebPageHelper.ContentSectionsFromDom` chunks the raw DOM into bounded
   fallback sections, filtering chrome ancestry (`nav`, `header`,
   `footer`, `aside`, `form`, `aria-hidden`, `navbox`, `sidebar`,
   `cookie`, `banner`, `mw-panel`, `vector-menu`, `sitenotice`,
   `mw-indicators`, …) so menus/footers never become content.
5. **Relevance gate** — an off-topic or junk page is marked skip and
   contributes nothing; for URL-direct runs the page's research questions
   are retired as unanswerable.
