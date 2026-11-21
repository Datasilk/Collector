# PDF Viewer

[Home](../README.md) > [Journal](../Journal.md) > PDF Viewer Module

`type: "pdf-viewer"` — embedded PDF via `react-pdf` (bundled `pdfjs`
worker).

## Data

```json
{ "type": "pdf-viewer", "filename": "<stored>.pdf", "originalFilename": "paper.pdf" }
```

## Features

- Same upload/metadata pipeline as `file-download` (`Files` API +
  `JournalFiles`).
- Page navigation + zoom controls.
