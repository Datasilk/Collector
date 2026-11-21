# add-edit-journal-entry

[Home](../README.md) > [Chat](../Chat.md) > add-edit-journal-entry

`AddEditJournalEntryTool` — merges a scraped `ResearchPage` into the
session's journal entry, or applies conversational edits.

## Two modes

- **Integrate** (scrape step feeding it): topic-assign prompt maps the
  dump's sections to `research.json` topics/subtopics — existing topics
  matched by id, new ones created, boilerplate sections (`References:`,
  `Related Posts`, footer/nav labels) skipped. Content lands as
  `research.json` `content[]` (with `source` index), `sources[]` +
  `searches[]` indices updated, comments attached (normalized-body
  dedupe), and `ResearchModuleWriter` renders dirty topics into
  `entry.json` modules (title text-editor, image-gallery, body blocks,
  tabs for subtopics).
- **Edit** (`instruction` arg only): the model emits move/add/remove/
  update ops applied to the existing module tree and research topics.

## Args

`instruction` (edit ops), plus the scraped page supplied by the preceding
`web-scrape` step. `journalId`/`title` control placement.

## Artifacts

`entry.json`, `research.json` (v2), `research/{####}.{json,html}`,
`snapshots/`, downloaded `images/`/`files/`/`videos/`.
