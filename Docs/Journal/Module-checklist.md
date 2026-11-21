# Checklist

[Home](../README.md) > [Journal](../Journal.md) > Checklist Module

`type: "checklist"` — an API-backed task list (`JournalChecklists` API),
also hydratable from module JSON.

## Data

```json
{
  "type": "checklist",
  "checklistId": 123,
  "checklist": {
    "title": "…",
    "description": "…",
    "items": [ { "id": 1, "title": "…", "status": 0, "sort": 1 } ]
  }
}
```

## Settings / fields

| Field | Effect |
|---|---|
| `checklistId` | DB row id — live data comes from the API |
| `checklist` | Cached/hydrated copy (required for snapshots/pinned rendering) |
| `items[].status` | `0` open / `1` done |
| `items[].sort` | Reorder weight |

## Features

- Add / edit / complete items, drag & drop reorder (sort API).
- Auto-creates the checklist row on first use.
- Settings modal (`checklist-settings-modal.jsx`): title, description, and
  **load an existing checklist** — rebinds `checklistId` to a different
  shared checklist.
