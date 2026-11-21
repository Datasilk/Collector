# Articles

[Home](../README.md) > [Web Scraper](../WebScraper.md) > Articles

**Web → Articles** (`app/web/articles`, `ArticlesController`) — the
analyzed-content index built from downloaded pages.

## What it manages

- `Articles` — one row per downloaded page (title, url, word/sentence
  counts, filesize, language, flags). `Common.Articles.Merge` folds the
  `AnalyzedArticle` analysis into the row; `GetLinks` feeds discovered
  links back to the queue.
- `ArticleSentences`, `ArticleWords`, `ArticleSubjects`,
  `ArticleDates` — the granular index: every extracted sentence, word,
  matched subject, and date mention.
- `ArticleBugs` — analysis anomalies worth inspecting.

Supporting taxonomy lives under `Subjects` and `Words/` (`Words`,
`CommonWords`, `SubjectWords`) plus `Dictionary` and `Statistics`.
