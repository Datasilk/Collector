# Journal modules

How to build a new journal entry module — the complete contract and the
feature surface existing modules use. Read `Docs/Journal.md` +
`Docs/Journal/Module-*.md` for per-module docs.

## The contract

Every module is a JSON object in `entry.json`'s `modules[]`:

```json
{ "id": "<unique within entry>", "type": "<registry key>",
  "manuallyAdded": true, "…": "type-specific fields" }
```

- `id` — generated client-side, stable across edits.
- `manuallyAdded` — true when the user inserted it; **stripped from
  `module` before render** (`module-list.jsx` removes it) — use the
  `manuallyAdded` *prop* for one-time setup (auto-open a picker,
  auto-create a row).
- Emit only fields the module reads — AI-generated entries must match
  these shapes exactly.

## Steps to build one

1. **Component** — `src/app/journal/modules/{your-module}.jsx`. Signature
   like `export default function YourModule({ module, onUpdate, entryId,
   entry, journalId, journal, chapters, isEditable, manuallyAdded,
   tabButtons, setDeleteListener, fromSnapshotId })` — take the props you
   need.
2. **Register** — add `type` → `{ component, name, icon }` in
   `src/app/journal/modules.js` so it appears in the Add-Module menu.
3. **Settings modal** (optional) — `modules/{name}-settings-modal.jsx`;
   open via `showModal`, save via `onUpdate` (see below).
4. **Docs** — add `Docs/Journal/Module-{name}.md` + a TOC line in
   `Docs/Journal.md`.

## Feature checklist — what a module can implement

Pick the features your module needs; this is the full surface existing
modules use (verified across all 13 components):

### Data & persistence

- **`onUpdate(updatedModule)`** — the ONLY mutation path. Call with the
  whole updated object; `entry.jsx`'s `saveEntryContent` autosaves. Never
  mutate `module` in place.
- **`isEditable`** — when false render pure read-only output: hide every
  editing affordance (buttons, inputs, drag handles, modals).
- **`fromSnapshotId`** — set when rendering a history snapshot: hydrate
  entirely from `module.*`, **never call APIs** (checklist + custom use
  this).
- **localStorage** — persist UI prefs (entries-list stores its sort).

### APIs

- **`useSession()` → api class** — `Images` (`upload`, `uploadBatch`),
  `Files` (`upload`), `JournalImages`/`JournalFiles`/`JournalVideos`
  (metadata `add`/`delete`), `JournalChecklists` (full CRUD + sort),
  `Journals` (entry data), `Videos`/`Cookies` (video download + restricted
  cookies).
- **`apiBasePath`** (`@/helpers/endpoints.js`) — build URLs for stored
  files (`/image/journal-entries/{entryId}/{file}`).

### The hover menu (module tab)

In edit mode every module renders inside
`div.module.module-{type}.editable` with an `onMouseOver` handler that
adds a `.hover` class (`module-list.jsx` `handleMouseOver` — tracks
`hoveredModuleIdRef`/`window.moduleHovered`, also used to target drops).
Hovering reveals the **module tab** —
`.module-tab-container > .module-tab > .box > .tool-bar` — rendered only
when `isEditing && showHoverTab && module.showTab !== false`.

The tab's tool-bar always contains:

- **Add-above button** — opens the module-type dropdown (each option's
  icon comes from the registry's `icon` field).
- **Your module's buttons** — pushed via `tabButtons`.
- **Delete button** — `removeModule(module.id)`, runs registered
  `setDeleteListener` callbacks.

**Adding icons: `tabButtons`** — call it with
`[{ icon, title, callback, disabled?, hasLabel? }]`, typically from a
`useEffect` watching your relevant state:

```jsx
useEffect(() => {
    if (typeof tabButtons !== 'function') return;
    const buttons = [];
    if (isEditable) buttons.push({ icon: 'add_photo_alternate',
        title: 'Add Images', callback: handleUploadButtonClick });
    buttons.push({ icon: 'settings',
        title: 'My Settings', callback: handleShowSettings });
    tabButtons(buttons);
}, [isEditable, /* state the buttons depend on */]);
```

- `icon` — an `Icon` component name (Material Symbol names, e.g.
  `delete`, `settings`, `add_photo_alternate`, `edit`).
- `title` — the tooltip (`title` attr); also renders as a label when
  `hasLabel: true`.
- `callback` — `() => …`, e.g. `showModal(() => <MySettingsModal/>)`.
- Pushed sets are deduped (`areButtonSetsEqual` on
  icon/title/callback/disabled) and batched ~100ms — call it freely in
  effects, don't memoize a handler per render.
- Per-module filtering is by `module.id` — push only your own buttons.

Other chrome hooks:

- `module.showTab = false` — hides the hover tab entirely.
- `module.width` / `getWidthClass` — width variants; `canResize` adds
  left/right resize handles.
- `module.className` — extra classes merged onto the wrapper.
- **`setDeleteListener(module, fn)`** — register cleanup that runs when
  the module is removed (image/gallery/file modules delete their
  `JournalImages`/`JournalFiles` metadata rows + files).
- **Settings modal** — `showModal` + `*-settings-modal.jsx` (see the save
  flow below).

### File ingest (paste/drag-and-drop)

`module-list.jsx` routes dropped/pasted files to modules by type and
stashes the `File` on a window bridge for the module to pick up:

- `image/*` → `image` module, `options.uploadFromClipboard`, file in
  `window.clipboardImages[moduleId]`
- `video/*` → `video-player`, `options.autoUploadDroppedVideo`, file in
  `window.__droppedVideoFiles[moduleId]`
- `*.pdf` → `pdf-viewer`, `options.uploadFromClipboard`, file in
  `window.clipboardFileBuffer[moduleId]`
- other → `file-download`, same clipboard buffer

To opt in: accept the `options` field (`uploadFromClipboard` /
`autoUploadDroppedVideo`), read the window buffer keyed by `module.id`,
upload it, clear the slot. `manuallyAdded:false` is set by the drop so the
module doesn't run its insert-time setup.

### Interaction

- **Drag & drop reorder** — tabs (hold 333ms then drag) and checklist
  (sort API) implement their own reordering.
- **Nested `ModuleList`** — `tabs`, `module-list`, `custom` render child
  `modules[]` recursively with identical shapes.
- **SignalR** — `generate-content` streams `/web-content` progress;
  `video-player` gets download progress; live modules can subscribe to the
  hub (`useSignalR`/`hub`).
- **Debounced inputs** — only `Input` components debounce, only when they
  call an API on `onInput`; debounce the API call, not the state update;
  clear the previous timer first.

### Conventions

- All API calls through `src/api/*` classes with the `useSession()`
  context — never raw fetch.
- Named `handle*` handlers, no inline lambdas on buttons.
- `Icon` component for all icons.

## How settings modals save

`context/modal.jsx` exposes module-level bridges — `showModal`,
`hideModal`, `showMenu`, `hideMenu` — so any code can open a modal without
context. You pass a **render function**, not an element:

```jsx
showModal(() => (
  <MySettingsModal module={module} onUpdate={onUpdate} />
));
```

Two save patterns exist in the codebase:

1. **Modal owns `onUpdate`** (tabs) — the modal gets `{module, onUpdate}`,
   builds the updated object itself, calls `onUpdate`, then `hideModal()`.
2. **Modal calls an `onSave` handler** (image-gallery, entries-list) — the
   modal returns a plain `settings` object to the module's handler, which
   merges it onto `moduleRef.current`, updates local state, and calls
   `onUpdate(updatedModule)`.

Either way the module object is replaced wholesale — partial mutation of
`module` won't persist. `showMenu(component, anchorRef, {align})` renders
an anchored popover above the modal layer (tab "Add Content" dropdowns,
context menus) and flips top/bottom when clipped.
