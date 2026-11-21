# Notifications

[Home](README.md) > Notifications

## SignalR pushes (in-app)

Real-time events flow server→client over the hubs registered in
`Program.cs`:

| Hub | Route | Events |
|---|---|---|
| `WorkerHub` | `/worker` | `PlanProgress`, `ChatMessage`, `ToolComplete`, step/step-status updates — the chat + worker progress channel (CLI listens here too) |
| `WebContentHub` | `/web-content` | `ScrapeStatus` / `GenerateStatus` for web-content entries and the generate-content module |
| `TextEditorHub` | `/text-editor` | live text-editor module updates |
| `VideoHub` | `/video-download` | video download progress |
| `ChromeExtensionHub` | `/chrome-extension` | cookie requests/responses (server→extension) |

A worker emitting progress is effectively emitting an in-app notification;
the React client renders them in the session/chat UI.

## Email

`Collector.API/Services/EmailService` + `Collector.Auth`'s
`AuthEmailService` — transactional mail: account activation, password
reset, one-time login (`EmailSettings` config).
