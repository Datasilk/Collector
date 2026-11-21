# File Download

[Home](../README.md) > [Journal](../Journal.md) > File Download Module

`type: "file-download"` — an attached file with a download button (docs,
zips, anything).

## Data

```json
{
  "type": "file-download",
  "filename": "<stored>",
  "originalFilename": "report.zip",
  "label": "Download report",
  "fileSize": 1048576,
  "showFileSize": true,
  "style": 0
}
```

## Settings

| Field | Effect |
|---|---|
| `label` | Button text (defaults to the filename) |
| `showFileSize` | Show the size next to the label |
| `style` | Visual variant |
| `filename` / `originalFilename` | Stored name + display name |

## Features

- Upload via `Files` API → `journal-entries/{entryId}/`; metadata via
  `JournalFiles`.
- `FileDownloadModal` configures label/size/style.
