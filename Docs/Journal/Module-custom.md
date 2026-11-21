# Custom Module

[Home](../README.md) > [Journal](../Journal.md) > Custom Module

`type: "custom"` — renders content sourced from **another entry**
(templates/reusable layouts), optionally multi-column.

## Data

```json
{
  "type": "custom",
  "customId": "<entryId>",
  "columnsPerRow": 2,
  "items": [ /* per-item config */ ],
  "modules": [ /* hydrated modules */ ]
}
```

## Settings

| Field | Effect |
|---|---|
| `customId` | The source entry to embed |
| `columnsPerRow` | Split items into N columns |
| `items` | Per-item configuration |
| `modules` | Hydrated child modules (nested `ModuleList` shapes) |

## Features

- Pick a source entry (search modal), edit per-item content inline via
  nested `ModuleList`.
- Column layout for reusable template content.
