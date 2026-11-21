# Chat System — live testing skill

How Collector's AI chat works and how to exercise it end-to-end from the
command line. Read `Docs/Chat.md` for the full system doc; this file is the
operational playbook.

## How the chat system works

The React client calls the SignalR `WorkerHub` route `chat-worker` →
`SendMessage`. `ChatWorker` (Collector.Workers) asks Ollama for a JSON
execution plan, then runs it through `ChatToolRegistry` (Collector.Chat),
executing registered `IChatTool`s in order. For a research request the plan
starts as a single `gather-research` step; tools extend the plan at runtime:

```
gather-research   find candidate URLs (search engines, relevance filter)
web-scrape        download + extract one URL → ResearchPage (parser dump or
                  DOM fallback sections)
add-edit-journal-entry  merge one page into the entry's research.json +
                  module tree, or apply conversational edits
research-review   coverage check → queues more gather-research for gaps
clean-web-content AI relevance filter for scraped content
manage-ollama-models    list/switch/pull Ollama + HTML-parser models
```

Research artifacts persist under `journals/{journalId}/{entryId}/`:
`entry.json` (modules), `research.json` (topics/subtopics/comments/sources/
searches), `research/{####}.json` per-source parser dumps + `{####}.html`
sidecars, `snapshots/`, `images/`, `files/`, `videos/`.

## The CLI

`Collector.Chat.Cli` (`collector-chat.dll`) connects directly to the worker
endpoints — no web UI needed.

```
status                        server/Ollama/DB connectivity
send <msg> [--timeout N]      send a message, stream worker events
logs [--limit N]              chat logs, newest first
read <chatId>                 print a chat log
details / detail <chatId>     detail debug logs (logs/chat/details)
dumps / dump <chatId> <file>  content/fact dump files
chats [--limit N]             chats from the database
entries <chatId>              journal entries produced in a chat
entry <entryId> [--raw]       journal entry JSON
files <entryId>               downloaded media for an entry
research <entryId> [file]     research.json / per-source dumps
```

Run from the repo: `dotnet Collector.Chat.Cli/bin/Debug/net8.0/collector-chat.dll <cmd>`

## Running live feature tests

1. **Build & launch the server first, every time** — the CLI talks to the
   running worker. Launch in Development mode or `logs/chat/*` and
   `logs/chat/details/*` won't be written:

   ```
   cd Collector.Web.Server
   dotnet run   (or: dotnet bin/Debug/net8.0/Collector.Web.Server.dll --urls http://0.0.0.0:7780
                 with ASPNETCORE_ENVIRONMENT=Development)
   ```

2. **Pick a fresh topic.** Send a philosophy or world-history subject that
   has no existing chat log — never reuse a topic. Examples that exercised
   different paths: "Research the history of the Terracotta Army",
   "Research the history of the Library of Alexandria", or a psychology
   topic like "Research cognitive dissonance theory".

   ```
   dotnet .../collector-chat.dll send "Research <fresh topic>" --timeout 5400
   ```

   Long plans run for tens of minutes — keep the connection open or the
   worker may be cancelled on disconnect. Watch `logs/chat/{chatId}.log`
   for progress; parser calls take several minutes per page.

3. **Verify journal placement** — the topic must land in an appropriate
   existing journal or a new journal, never an unrelated one.

4. **Inspect artifacts, not just tool success.** Check `entry.json` module
   structure, `research.json` topics/subtopics/sources/searches indices,
   `research/{####}.json` dump quality, downloaded media, and the detail
   log's relevance-gate / topic-assign decisions.

5. **Record findings.** Defects go to `.agents/TASKS.md`; verified fixes move
   to `.agents/DONE-{date}.md`. Repeat the cycle until production ready.
