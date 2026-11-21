# Tabs

[Home](../README.md) > [Journal](../Journal.md) > Tabs Module

`type: "tabs"` — sectioned content; each tab holds its own `modules[]`
rendered by a nested `ModuleList`. **The research-organizer module** —
research subtopics render as tabs.

## Data

```json
{
  "type": "tabs",
  "style": 1,
  "className": "side-menu",
  "tabs": [ { "id": "…", "title": "…", "modules": [ /* nested modules */ ] } ]
}
```

## Settings

| Setting | Values | Effect |
|---|---|---|
| `style` | `0` horizontal tabs / `1` side menu | Layout (side menu also sets `className`) |
| `className` | css class | Extra styling hook |
| `tabs[]` | `{id, title, modules[]}` | Nested modules use identical shapes as top-level |

## Features

- Add / inline-rename / remove tabs.
- Drag-to-reorder (hold 333ms).
- Per-tab "Add Content" dropdown offering every module type.
- Style settings modal (`tabs-settings-modal.jsx`).
- Research pipeline binds tabs to subtopic ids; duplicate tabs modules are
  healed on load/save (bound instance wins).
