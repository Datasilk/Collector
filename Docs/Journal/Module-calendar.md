# Calendar

[Home](../README.md) > [Journal](../Journal.md) > Calendar Module

`type: "calendar"` — month grid for habit tracking / day notes.

## Data

```json
{
  "type": "calendar",
  "crossedOffDays": ["2026-11-15"],
  "dayNotes": { "2026-11-15": "note" }
}
```

## Features

- Click a day to toggle the ✕ (habit cross-off); pencil icon edits the
  day's note (`calendar/notes-modal.jsx`).
- Prev/next month + Today navigation (header chevrons & `tabButtons`).
- Dates are `YYYY-MM-DD` strings in the module JSON.
