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
create them.

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
