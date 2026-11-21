# Text Editor

[Home](../README.md) > [Journal](../Journal.md) > Text Editor Module

`type: "text-editor"` — rich text via CKEditor InlineEditor. The workhorse
module: paragraphs, headings, lists, tables, code blocks. The research
pipeline writes nearly all prose as text-editor modules.

## Data

```json
{ "type": "text-editor", "html": "<p>…</p>", "userInput": "…" }
```

| Field | Meaning |
|---|---|
| `html` | Rendered content (what `add-edit-journal-entry` writes) |
| `userInput` | Last prompt used with the built-in AI generator |

## Features

- Full inline rich-text editing, autosave through `onUpdate`.
- AI generation modal (`ckeditor/ai-generator-modal`) — generate or rewrite
  content inline with a prompt.
- Read-only mode (`isEditable=false`) renders pure output.
