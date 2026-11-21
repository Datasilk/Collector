# Subjects

[Home](../README.md) > [Web Scraper](../WebScraper.md) > Subjects

**Web → Subjects** (`app/web/subjects`, `SubjectsController`) — the
subject taxonomy used to classify article content.

## What it manages

- `Subjects` — the taxonomy rows.
- `SubjectWords` — the words associated with each subject; the article
  analyzer scores pages by matching extracted words against them, and the
  results land in `ArticleSubjects`.
