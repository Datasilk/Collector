# Generate Content

[Home](../README.md) > [Journal](../Journal.md) > Generate Content Module

`type: "generate-content"` — YouTube URL → AI-generated article content,
streamed over the `/web-content` SignalR hub — inserts generated modules
around itself.

## Data

```json
{
  "type": "generate-content",
  "url": "<youtube url>",
  "includeVideo": true,
  "includeTitle": true,
  "includeDescription": true,
  "includeTranscriptResearch": false,
  "includeCommentsResearch": false,
  "generateChapters": true,
  "chapterCount": 5,
  "userInstructions": "…",
  "cache": "…"
}
```

## Settings

| Field | Effect |
|---|---|
| `url` | The YouTube video to generate from |
| `includeVideo` / `includeTitle` / `includeDescription` | Emit those artifacts as modules |
| `includeTranscriptResearch` / `includeCommentsResearch` | Research the transcript / comments too |
| `generateChapters` + `chapterCount` | Split long transcripts into chapters |
| `userInstructions` | Free-text steer for the generator |
| `cache` | Cached generation state |

## Flow

Settings modal → validate YouTube URL → SignalR `ScrapeStatus` /
`GenerateStatus` progress events → produced modules are added to the entry.
