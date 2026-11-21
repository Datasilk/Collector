# Journal

[Home](README.md) > Journal

The journal system is Collector's content core: users keep research,
notes, media, and generated material in **journal entries**.

## Journal Categories & Journals

- **Journal Categories** group journals by subject.
- **Journals** live under a category. A journal's entries are listed and
  navigated through an **Entries List** module.

## Entries & modules

A journal is made up of **entries**. Each entry is a vertical list of
**modules** — the JSON document stored as
`journals/{journalId}/{entryId}/entry.json`:

```json
{ "id": "<entryId>", "modules": [ /* module objects */ ] }
```

Every module shares a base shape — `id`, `type`, `manuallyAdded` — plus
type-specific fields. `modules.js` in the React client maps `type` →
component + name + icon; `module-list.jsx` renders the array in order and
hands each module its object plus update callbacks.

A journal is itself *just an entry* whose content is an **Entries List**
module linking to other entries. Because entries-list + tag filters can
compose freely, users can build a hierarchy of entries — e.g. a chapter
entry listing section entries, filtered by tags.

## Creating entries

- **Manually** — pick a journal, add modules, edit inline.
- **From web content** — e.g. YouTube videos: `WebContentHub.ScrapeUrl` →
  `YouTubeEntryCreator.CreateEntryFromYouTubeVideo` fetches metadata via
  yt-dlp, normalizes the URL (strips `list=`/playlist params), and builds
  a video-player module + text-editor module (title + description).
- **By research via chat** — the chat system's `add-edit-journal-entry`
  tool merges scraped research into a structured entry: topics become
  sections, subtopics become tabs, facts become paragraphs/lists/tables,
  images become galleries.

## Module docs

One document per module type — data shape, settings, and features:

- [Entries List](Journal/Module-entries-list.md)
- [Text Editor](Journal/Module-text-editor.md)
- [Image](Journal/Module-image.md)
- [Image Gallery](Journal/Module-image-gallery.md)
- [Tabs](Journal/Module-tabs.md)
- [Video Player](Journal/Module-video-player.md)
- [Module List](Journal/Module-module-list.md)
- [Checklist](Journal/Module-checklist.md)
- [Calendar](Journal/Module-calendar.md)
- [File Download](Journal/Module-file-download.md)
- [PDF Viewer](Journal/Module-pdf-viewer.md)
- [Custom](Journal/Module-custom.md)
- [Generate Content](Journal/Module-generate-content.md)

## Database side

Journal structure rows live in PostgreSQL (`Journal/` schema folder):
`Journals` + `JournalCategories` + `JournalChapters` (grouping),
`JournalEntries` + `JournalEntryTags` + `JournalTags` (the tag-filter
mechanism entries-list uses), `JournalEntrySnapshots` (version history —
`entry.json` snapshots under `snapshots/`), `JournalCheckLists` +
`JournalCheckListItems` (checklist module), and `JournalFiles` /
`JournalImages` / `JournalVideos` (media metadata). `JournalModules` keeps
a searchable index of module content.

## Entry types

`entry-types.js` registers what "New Entry" can create:

- **journal-entry** — a blank rich-text entry (`JournalEntryModal`).
- **web-content** — content built from a web source
  (`WebContentModal` → `WebContentHub.ScrapeUrl` → `YouTubeEntryCreator`
  for YouTube: metadata + transcript + video-player module).

## Entry settings & tooling

`entry-settings-modal` (per-entry) and `journal-settings-modal`
(per-journal) hold metadata; `entry-toolbar` carries module-add, snapshot,
and settings actions. `move-entry-modal` relocates entries between
journals; `new-entry-tag` / `tags-list` manage tag assignment.

## Entry file/folder structure

Every entry is a folder on disk under its journal —
`journals/{journalId}/{entryId}/`:

```
journals/
└── {journalId}/
    └── {entryId}/
        ├── entry.json            the module tree — what the client renders
        ├── research.json         structured research (v2): topics, subtopics,
        │                         content, comments, sources, searches +
        │                         source/search indices
        ├── research/
        │   ├── {####}.json       per-source parser-model dump
        │   └── {####}.html       cleaned HTML sidecar the parser consumed
        ├── snapshots/
        │   └── {id}.json         entry snapshots (version history)
        ├── images/               downloaded images
        ├── files/                downloaded files (PDFs, docs, archives…)
        └── videos/
            └── {videoId}/
                ├── raw.{ext}     original download
                ├── thumb.jpg     thumbnail
                ├── hls/          adaptive-streaming renditions
                └── previews/     scrub-preview frames
```

Transient staging (e.g. cleaned HTML awaiting the parser) lives under
`journals/_staging/` and is moved into `research/` when the dump is saved.
The web client loads `research.json` via
`GET /api/journals/entries/{id}/research`; media paths in modules resolve
against the entry folder. See [Storing Content](Storing-Content.md) for the
wider storage model.
