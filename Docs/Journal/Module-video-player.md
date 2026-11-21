# Video Player

[Home](../README.md) > [Journal](../Journal.md) > Video Player Module

`type: "video-player"` — local upload **or** YouTube-downloaded video with
HLS playback and picture-in-picture.

## Data

```json
{
  "type": "video-player",
  "videoId": "…",
  "videoPath": "…",
  "thumbnailPath": "…",
  "url": "<youtube url>",
  "downloaded": true,
  "autoTryAgain": false,
  "autoUploadDroppedVideo": false
}
```

## Settings / flags

| Field | Effect |
|---|---|
| `url` | Source — a YouTube link triggers `Videos` API download via `VideoWorker` (progress streamed) |
| `videoPath` / `thumbnailPath` / `videoId` | Stored file refs under `videos/{videoId}/` |
| `downloaded` | Video is stored locally vs remote |
| `autoTryAgain` | Retry a failed download automatically |
| `autoUploadDroppedVideo` | Drag & drop pipeline set this — auto-ingest the dropped file |

## Features

- HLS.js playback, download-from-URL flow, progress UI.
- Cookies upload modal for restricted videos (`Cookies` API).
- Picture-in-picture (`useVideoPiP`), file replace.
