# Image Gallery

[Home](../README.md) > [Journal](../Journal.md) > Image Gallery Module

`type: "image-gallery"` — multiple images as carousel or grid.

## Data

```json
{
  "type": "image-gallery",
  "images": ["<file>", "…"],
  "galleryType": 0,
  "thumbnailsPerRow": 6,
  "thumbnailSize": "1:1"
}
```

- `images` — filename strings (same folder as `image`; thumbnails are
  `thumb_<file>`) or `{url, caption}` objects for remote images (AI
  research entries — rendered directly, no `thumb_` derivative).

## Settings

| Setting | Values |
|---|---|
| `galleryType` | `0` carousel (main image + paged thumbnail strip) / `1` grid |
| `thumbnailsPerRow` | 1–12 |
| `thumbnailSize` | `1:1` / `3:4` / `4:5` / `16:9` / `4:3` / `5:4` / `9:16` |

## Features

- Multi-file batch upload (tagged with `moduleId`), per-image remove.
- Click-to-preview modal, settings modal.
