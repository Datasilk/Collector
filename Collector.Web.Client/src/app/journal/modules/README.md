# Journal Modules

A journal entry's content is a JSON document stored as `{entryId}.json`:

```json
{ "id": "<entryId>", "modules": [ /* module objects */ ] }
```

Every module object shares a base shape:

```json
{ "id": "<random id>", "type": "<module type>", "manuallyAdded": true, "...module fields": "..." }
```

- `id` — unique within the entry (generated client-side, e.g. random int as string)
- `type` — registry key (`text-editor`, `image`, `tabs`, …)
- `manuallyAdded` — true when inserted by the user; modules use it to trigger
  one-time setup (auto-create a checklist, auto-open a file picker, create a
  default tab)

`modules.js` maps `type` → React component + name + icon. `module-list.jsx`
renders the `modules[]` array and passes each module these common props:

| Prop | Meaning |
|---|---|
| `module` | the module's JSON object — the single source of truth |
| `onUpdate(module)` | call with the updated module object to persist changes |
| `entryId`, `entry`, `journalId`, `journal`, `chapters` | entry context |
| `isEditable` | false in read-only view — hide all editing UI |
| `manuallyAdded` | true right after the user inserts the module |
| `tabButtons(buttons)` | push action buttons into the module header toolbar `[{icon,title,callback}]` |
| `setDeleteListener(module, fn)` | run cleanup (e.g. delete uploaded-file metadata) when the module is removed |
| `fromSnapshotId` | set when rendering a history snapshot — hydrate from `module.*`, never call APIs |

> **For AI/code generating entries**: emit modules in this shape only, keep `id`s
> unique and stable across edits, and don't emit fields a module doesn't read.

---

## `text-editor` (ckeditor.jsx)

Rich text via CKEditor InlineEditor. The workhorse for paragraphs, headings,
lists, tables, code blocks.

```json
{ "type": "text-editor", "id": "…", "html": "<p>…</p>", "userInput": "…" }
```

- `html` — the rendered content (this is what `AddEditJournalEntryTool` writes)
- `userInput` — last prompt used with the built-in AI generator
- Functionality: full inline rich-text editing, autosave → `onUpdate`, AI
  generation modal (`ckeditor/ai-generator-modal.jsx`)

## `image` (image.jsx)

Single uploaded image.

```json
{ "type": "image", "id": "…", "image": "<file>", "entryId": "<entryId>",
  "settings": { "aspectRatio": "16 / 9", "displayMode": "cover", "position": "center" } }
```

- `image` — filename stored under `journal-entries/{entryId}/`; rendered from
  `/image/journal-entries/{entryId}/{image}`. AI-generated entries may instead
  supply `{url, caption}` objects or `module.url` with a remote `https://` URL,
  which renders directly without the store path.
- `settings.aspectRatio`: `none` (renders raw `<img>`) or CSS ratio
  (`1 / 1`, `3 / 2`, `2 / 3`, `4 / 3`, `3 / 4`, `16 / 9`, `9 / 16`, `21 / 9`,
  `5 / 4`, `4 / 5`). Non-`none` renders a sized div.
- `settings.displayMode`: `contain` | `cover` | `auto` | `custom` (+ `widthMode`/
  `widthValue`, `heightMode`/`heightValue`); `position`: `top|bottom|left|right|
  center|custom` (+ `positionX`, `positionY`)
- `uploadFromClipboard` — paste-to-upload flag
- Functionality: upload button (auto-opened on insert), Replace Image modal,
  Image Display Settings modal, registers `JournalImages` metadata + delete
  listener cleanup

## `image-gallery` (image-gallery.jsx)

Multiple images as carousel or grid.

```json
{ "type": "image-gallery", "id": "…", "images": ["<file>", "…"],
  "galleryType": 0, "thumbnailsPerRow": 6, "thumbnailSize": "1:1" }
```

- `images` — array of filename strings (same folder convention as `image`;
  thumbnails are `thumb_<file>`) or `{url, caption}` objects for remote images
  (AI research entries; rendered directly, no `thumb_` derivative)
- `galleryType`: `0` carousel (main image + paged thumbnail strip) | `1` grid
- `thumbnailsPerRow` 1–12; `thumbnailSize` `1:1|3:4|4:5|16:9|4:3|5:4|9:16`
- Functionality: multi-file upload (batch, `moduleId` tag), per-image remove,
  click-to-preview modal, settings modal

## `tabs` (tabs.jsx)

Sectioned content — each tab holds its own `modules[]` rendered by a nested
`ModuleList`. **The research-organizer module.**

```json
{ "type": "tabs", "id": "…", "style": 1, "className": "side-menu",
  "tabs": [ { "id": "…", "title": "…", "modules": [ /* nested modules */ ] } ] }
```

- `style`: `0` horizontal tabs | `1` **side menu** (also sets `className`)
- `tabs[]` — `{id, title, modules[]}`; nested modules use the same shapes as
  top-level modules
- Functionality: add / inline-rename / remove tabs, drag-to-reorder (hold 333ms),
  per-tab "Add Content" dropdown offering every module type, style settings modal

## `module-list` (module-list.jsx)

Flat nested container — a column of child modules inside one module.

```json
{ "type": "module-list", "id": "…", "modules": [ /* nested modules */ ] }
```

## `checklist` (checklist.jsx + checklist-settings-modal.jsx)

API-backed task list (`JournalChecklists` API), also hydratable from module JSON.

```json
{ "type": "checklist", "id": "…", "checklistId": 123,
  "checklist": { "title": "…", "description": "…",
                 "items": [ { "id": 1, "title": "…", "status": 0, "sort": 1 } ] } }
```

- `checklistId` — DB row id; `checklist` — cached/hydrated copy (required for
  snapshots/pinned rendering; live data comes from the API)
- `items[].status`: `0` open | `1` done
- Functionality: add/edit/complete items, drag & drop reorder (sort API), create
  on first use, settings modal (title, description, **load an existing
  checklist** — rebinds `checklistId`)

## `calendar` (calendar.jsx + calendar/notes-modal.jsx)

Month grid for habit tracking / day notes.

```json
{ "type": "calendar", "id": "…",
  "crossedOffDays": ["2026-11-15"], "dayNotes": { "2026-11-15": "note" } }
```

- Dates are `YYYY-MM-DD`; clicking a day toggles the ✕, pencil icon edits the
  note (notes modal)
- Prev/next month + Today navigation (header chevrons & tabButtons)

## `video-player` (video-player.jsx)

Local upload **or** YouTube-downloaded video with HLS playback + PiP.

```json
{ "type": "video-player", "id": "…", "videoId": "…", "videoPath": "…",
  "thumbnailPath": "…", "url": "<youtube url>", "downloaded": true,
  "autoTryAgain": false, "autoUploadDroppedVideo": false }
```

- `url` — source (YouTube link → `Videos` API download via worker, progress
  streamed); `videoPath`/`thumbnailPath`/`videoId` — stored file refs
- `autoUploadDroppedVideo` — set by the drag & drop pipeline to auto-ingest a
  dropped video file
- Functionality: HLS.js playback, download-from-URL flow, cookies upload modal
  for restricted videos (`Cookies` API), picture-in-picture (`useVideoPiP`),
  progress UI, file replace

## `file-download` (file-download.jsx)

Attached file with a download button (docs, zips, anything).

```json
{ "type": "file-download", "id": "…", "filename": "<stored>", 
  "originalFilename": "report.zip", "label": "Download report",
  "fileSize": 1048576, "showFileSize": true, "style": 0 }
```

- Upload via `Files` API → `journal-entries/…`, metadata via `JournalFiles`
- `label`, `showFileSize`, `style` configured in `FileDownloadModal`

## `pdf-viewer` (pdf-viewer.jsx)

Embedded PDF via `react-pdf` (bundled `pdfjs` worker).

```json
{ "type": "pdf-viewer", "id": "…", "filename": "<stored>.pdf",
  "originalFilename": "paper.pdf" }
```

- Same upload/metadata pipeline as `file-download`; page navigation + zoom

## `custom` (custom.jsx)

Renders content sourced from **another entry** (templates/reusable layouts),
optionally multi-column.

```json
{ "type": "custom", "id": "…", "customId": "<entryId>",
  "columnsPerRow": 2, "items": [ /* per-item config */ ],
  "modules": [ /* hydrated modules */ ] }
```

- `customId` — the source entry id; `columnsPerRow` splits items into columns
- Functionality: pick a source entry (search modal), edit per-item content
  inline via nested `ModuleList`, column layout

## `entries-list` (entries-list.jsx)

A live, filtered listing of journal entries — navigation/TOC content.

```json
{ "type": "entries-list", "id": "…", "viewType": "details",
  "columns": { "created": true, "modified": true, "status": true, "chapter": true },
  "paging": { "total": 20 }, "tags": [12, 34],
  "gridColumns": 0, "aspectRatio": "5/6", "roundedCorners": true }
```

- `tags` — **tag ids filter**: only entries carrying these journal tags are
  listed (this is the TOC/filter mechanism referenced in TODO.md)
- `viewType` — `details` (default) or grid views; `columns` toggles columns;
  `paging.total` = page size; `gridColumns` 0 = auto; `aspectRatio`, 
  `roundedCorners` — grid card styling
- Functionality: search, sort (persisted to localStorage), pagination
  (`entries-list-paging`), tag picker + new-tag creation in settings modal,
  "New Entry" buttons pre-tagged with the module's `tags`

## `generate-content` (generate-content.jsx)

YouTube URL → AI-generated article content, streamed over the `/web-content`
SignalR hub — inserts generated modules around itself.

```json
{ "type": "generate-content", "id": "…", "url": "<youtube url>",
  "includeVideo": true, "includeTitle": true, "includeDescription": true,
  "includeTranscriptResearch": false, "includeCommentsResearch": false,
  "generateChapters": true, "chapterCount": 5,
  "userInstructions": "…", "cache": "…" }
```

- Flow: settings modal → validates YouTube URL → SignalR `ScrapeStatus` /
  `GenerateStatus` progress → produced modules are added to the entry
- `include*` toggles choose which generated artifacts to emit;
  `generateChapters`/`chapterCount` split long transcripts;
  `userInstructions` is a free-text steer for the generator

---

## Cross-cutting notes

- **File storage**: uploads go to `journal-entries/{entryId}/…` via `Images` /
  `Files` APIs; metadata rows via `JournalImages` / `JournalFiles`. Served back
  through `/image/…` and file endpoints.
- **Nesting**: `tabs`, `module-list`, `custom` nest `ModuleList` — child modules
  use identical shapes.
- **Editing contract**: every mutation goes through `onUpdate(updatedModule)`;
  the entry autosaves. `isEditable=false` must produce pure read-only output.
- **Settings modals**: `*-settings-modal.jsx` siblings; opened via
  `showModal`, return a full updated module object.
