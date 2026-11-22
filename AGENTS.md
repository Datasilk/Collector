# Working With This Repository

## Task tracking

We track work in `.agents/` at the repo root:

- **`.agents/GOALS.md`** — the current goals we are trying to accomplish.
  Update it when the direction changes.
- **`.agents/TASKS.md`** — the immediate task list. Findings from live test
  runs land here as pending items; implement top-down.
- **`.agents/DONE-{yyyy}-{mm}-{dd}.md`** — completed work. When a task has
  been developed, tested, and verified, move it out of `TASKS.md` into the
  dated DONE file for that day (e.g. `DONE-2026-11-21.md`).

## Testing

We don't do unit testing — there are no Test projects in this repo, don't
create them. Testing is live end-to-end runs (see the chat-system skill
for the collector-chat CLI playbook). Before running tests, make sure
the full stack is up:

- **Collector.Web.Server** — running and listening on port 7780, in
  Development mode (`ASPNETCORE_ENVIRONMENT=Development`) so chat detail
  logs are written. `dotnet run` on the server project also launches the
  Vite dev server for the React app automatically via SpaProxy
  (`Collector.Web.Client.esproj` → `StartupCommand: npm run dev`). When
  the built dll is run directly instead, start the client separately:
  `npm run dev` in `Collector.Web.Client/` — the user may be browsing
  the app while tests run, so keep it up.
- **Charlotte Web Router** — `http://localhost:7007/GetDOM`
  (`Collector.Common.Article.BrowserEndpoint` default).
- **Charlotte browser instance** — `http://localhost:7077/Browser`
  (the deployed `M:\Collector\Charlotte` build carries the full CefSharp
  runtime; a plain local build is missing the natives).

Scrapes fail silently to "Failed to download or parse content" when the
Charlotte services aren't running — check them first on scrape errors.

## Documentation

Comprehensive documentation lives in `Docs/` and must be kept up to date as
systems change. `Docs/README.md` is the table of contents — add entries when
you add docs.

## Skills

Agent skills live under `.agents/skills/` — one folder per skill containing
a `SKILL.md`. Current skills:

- `.agents/skills/chat-system/` — how the chat/research system works and how
  to drive it end-to-end with the `collector-chat` CLI for live feature
  testing.
- `.agents/skills/deployment/` — `deploy.bat` publish flow, Windows services,
  `Collector.SQL` schema deploys (non-interactive gotchas included).
- `.agents/skills/docs/` — Docs/ conventions: TOC, breadcrumbs, naming.
- `.agents/skills/workers/` — adding/modifying SignalR workers, client
  events, cookie requests.
- `.agents/skills/journal-modules/` — adding module types: JSON contract,
  props, storage.
- `.agents/skills/charlotte/` — running/debugging instances + Web Router,
  dashboard, ProbeSearch.
