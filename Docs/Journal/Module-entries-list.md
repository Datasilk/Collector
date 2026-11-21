# Entries List

[Home](../README.md) > [Journal](../Journal.md) > Entries List Module

`type: "entries-list"` — a live, filtered listing of journal entries. This
is the navigation/TOC module: a journal is itself an entry whose content is
an entries-list, so nested entries-list modules + tag filters build entry
hierarchies.

## Data

```json
{
  "type": "entries-list",
  "viewType": "details",
  "columns": { "created": true, "modified": true, "status": true, "chapter": true },
  "paging": { "total": 20 },
  "tags": [12, 34],
  "gridColumns": 0,
  "aspectRatio": "5/6",
  "roundedCorners": true
}
```

## Settings

| Setting | Values | Effect |
|---|---|---|
| `viewType` | `details` / `cards` / `poster` | List layout — details table, card grid, or poster wall |
| `columns` | per-column toggles | Details view: show/hide Created, Modified, Status, Chapter |
| `paging.total` | page size | Entries per page |
| `tags` | tag id array | Only entries carrying these journal tags are listed — the TOC/hierarchy filter |
| `gridColumns` | `0` = auto, else N | Fixed column count for cards/poster grids |
| `aspectRatio` | e.g. `5/6`, `1/1`, `16/9` | Card/poster thumbnail ratio |
| `roundedCorners` | bool | Card/poster corner styling |

## Features

- Search box, sortable columns (sort persisted to localStorage).
- Pagination bar (`entries-list-paging`).
- Settings modal: tag picker + **create new tag**; "New Entry" buttons are
  pre-tagged with the module's `tags` so new entries land in the list.
