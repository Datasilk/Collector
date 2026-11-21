# Chat CLI

[Home](README.md) > Chat CLI

`Collector.Chat.Cli` (`collector-chat.dll`) — drives the chat system from
the terminal over SignalR, no web UI required. The live-testing harness
(see `.agents/skills/chat-system/SKILL.md`).

## Commands

| Command | Purpose |
|---|---|
| `status` | Server/Ollama/DB connectivity probe |
| `send <msg> [--timeout N]` | Send a chat message, stream worker events. Keep the connection open — disconnect may cancel the worker |
| `logs [--limit N]` | Chat logs, newest first |
| `read <chatId>` | Print a chat log |
| `details` / `detail <chatId>` | Detail debug logs (`logs/chat/details/`) |
| `dumps` / `dump <chatId> <file>` | Content/fact dump files |
| `chats [--limit N]` | Chats from the database |
| `entries <chatId>` | Journal entries produced in a chat |
| `entry <entryId> [--raw]` | Journal entry JSON |
| `files <entryId>` | Downloaded media for an entry |
| `research <entryId> [file]` | `research.json` / per-source dumps |

## Pieces

- `CliConfig` — endpoint/DB/storage paths.
- `WorkerClient` — SignalR client to the `chat-worker` route.
- `Db` — direct PostgreSQL reads (chats, entries) for inspection commands.
- `EntryStore` — reads `entry.json`/`research.json`/dumps from the journal
  filesystem.
