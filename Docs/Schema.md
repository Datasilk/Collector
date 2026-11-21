# Database Schema

[Home](README.md) > Database Schema

All 55 tables, grouped by subsystem (`Collector.SQL/Tables/`). Functions/
procedures follow the `Entity_Action` naming pattern
(`SELECT public."Feed_Add"(…)`).

## Users / auth (`Users/`)

`AppUsers`, `AppRoles`, `AppUserRoles`, `AppUserTokens` — identity, roles,
refresh-token store. See [Authentication](Authentication.md).

## Journal (`Journal/`)

| Table | Purpose |
|---|---|
| `Journals`, `JournalCategories`, `JournalChapters` | journal structure |
| `JournalEntries`, `JournalEntryTags`, `JournalTags` | entries + tag filters |
| `JournalEntrySnapshots` | entry version history |
| `JournalCheckLists`, `JournalCheckListItems` | checklist modules |
| `JournalFiles`, `JournalImages`, `JournalVideos` | media metadata |
| `JournalModules` | module index rows |

## Chat / research (`Chat/`)

`Chats`, `ChatHistory` (message log), `ChatContextChunks` — retrieved
context pieces for chat memory.

## Web scraper

- `Articles/` — `Articles`, `ArticleSentences`, `ArticleSubjects`,
  `ArticleWords`, `ArticleDates`, `ArticleBugs`: analyzed article index.
- `Domains/` — `Domains`, `DomainHierarchy`, `DomainLinks`,
  `DomainAddresses`, `AnalyzerRules`, `DomainTypeMatches`,
  `DownloadRules`, `DomainServices`, `DomainServiceNames`,
  `DomainCollections`, `DomainCollectionGroups`.
- `Downloads/` — `Downloads`, `DownloadQueue`.
- `Blacklist_Domains`, `Blacklist_Wildcards`, `Whitelist_Domains` — scope
  lists.
- `Subjects`, `Words/` (`Words`, `CommonWords`, `SubjectWords`) —
  subject/word taxonomy. `Dictionary`, `Statistics/` — language stats +
  research projects.
- `SearchEngines` — registered + discovered site search engines.
- `Converse`, `ConverseResources`, `ConverseSubjects` — legacy
  conversation/subject tables.

## Feeds (`Feeds/`)

`Feeds`, `FeedCategories`, `FeedsCheckedLog` — feed definitions and check
history. See [Feeds](Feeds.md).

## AI

`LLM_Models` (chat/planning models + active flag), `LLM_HtmlParsers`
(parser-model registry).
