# Web Client

[Home](README.md) > Web Client

`Collector.Web.Client` — Vite + React 19 SPA (`npm run dev`).

## Structure

```
src/
├── api/        API wrapper classes — account/admin/public/user subfolders
├── app/        page areas: account, admin, feeds, home, journal, web
├── components/ shared UI (navigation, forms, ui/)
├── context/    session context (useSession)
├── helpers/    client helpers
├── routes/     route definitions
└── styles/     dark.css / light.css themes
```

## Conventions

- API access: `src/api/*` classes receive the `useSession()` context:
  `const { getById, filter } = Journals(session);`
- Buttons: pre-styled; `icon` class for icon-only, `cancel` for Cancel;
  toolbar buttons go in `.tool-bar` (`.right-side` for header bars); modal
  buttons in `.buttons`; named `handle*` functions, never inline lambdas.
- Filters: a single `filter` state object; filter fields inside
  `div.filters.tool-bar`; only `Input` debounces, only when it calls an
  API — debounce the API call, not the state update; clear the previous
  timer first.
- Icons: `Icon` component in `src/components/ui/icon.jsx` only.

## Key areas

- **journal/** — the journal UI: entry editor, module registry
  (`modules.js`), `module-list.jsx`, entry types (`journal-entry`,
  `web-content`), tags, snapshots, chapters.
- **web/** — [Web Scraper](WebScraper.md): articles, blacklists, domains,
  downloads, subjects, whitelists.
- **admin/** — LLMs, Ollama models, HTML parser models, users.
- **feeds/** — feed categories/items.
- **account/** — login/activate/reset.
