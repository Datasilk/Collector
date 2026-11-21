# Image

[Home](../README.md) > [Journal](../Journal.md) > Image Module

`type: "image"` — a single uploaded (or remote) image.

## Data

```json
{
  "type": "image",
  "image": "<file>",
  "entryId": "<entryId>",
  "settings": { "aspectRatio": "16 / 9", "displayMode": "cover", "position": "center" }
}
```

- `image` — filename under `journal-entries/{entryId}/`, rendered from
  `/image/journal-entries/{entryId}/{image}`. AI-generated entries may use
  `{url, caption}` / `module.url` remote `https://` images instead.
- `uploadFromClipboard` — paste-to-upload flag.

## Display settings

| Setting | Values |
|---|---|
| `settings.aspectRatio` | `none` (raw `<img>`) or CSS ratio: `1/1`, `3/2`, `2/3`, `4/3`, `3/4`, `16/9`, `9/16`, `21/9`, `5/4`, `4/5` |
| `settings.displayMode` | `contain` / `cover` / `auto` / `custom` (+ `widthMode`/`widthValue`, `heightMode`/`heightValue`) |
| `settings.position` | `top` / `bottom` / `left` / `right` / `center` / `custom` (+ `positionX`/`positionY`) |

## Features

- Upload button (auto-opens on insert), Replace Image modal, Image Display
  Settings modal.
- Registers `JournalImages` metadata + a delete listener so removing the
  module cleans up the file.
