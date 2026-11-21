# Charlotte

[Home](README.md) > Charlotte

[Charlotte](https://github.com/Datasilk/Charlotte) is Collector's
headless-browser DOM extraction platform — two services plus a helper:

- **`Charlotte/Charlotte/`** — a browser *instance* service (the thing that
  actually loads pages and serializes the DOM).
- **`Charlotte/Router/`** — **Charlotte's Web Router**: an ASP.NET MVC app
  that load-balances a pool of instances and hosts an **admin dashboard
  showing real-time requests**.
- **`Charlotte/BlacklistParser/`** — parses bulk blacklist lists into
  `Blacklist_Domains` / `Blacklist_Wildcards`.

## Charlotte's Web Router

A full ASP.NET MVC app (Razor views, session state, `UseWindowsService`,
Docker-aware). Config: `config.json` / `config.docker.json` /
`config.prod.json` per environment — holds `Admin` credentials and the
`Charlotte.Instances[]` pool.

### Admin dashboard

- `Home/Index` — login form; checks `Config.Admin.Username`/`Pass`, sets
  session `admin=1`. `BaseController.CheckSecurity` guards the dashboard,
  Add/Edit instance pages.
- `Dashboard/Index` — live instance list: server name, id, url,
  `UsesCookies` flag, note, Edit links; plus a **Check URL** box.
- `/dashboardhub` (`DashboardHub`) — `UpdateConsole` streams the live log
  to connected dashboards; `CheckUrl(url, session, macros)` runs a real DOM
  request from the dashboard.
- `Log.WriteLine` — every routing decision is broadcast to all dashboard
  listeners **in real time**, prefixed with a requestId: new request,
  which instance was picked, send/receive, response size, errors,
  timeouts.
- `AddInstance` / `EditInstance` controllers + views — CRUD for the
  instance pool, persisted via `App.SaveConfig`.

### Request routing (`Common/Charlotte.GetDOM`)

`POST /GetDOM` (`GetDOMController`, args `url`, `session`, `macros`):

1. Polls `Instances.Where(UsesCookies == session).OrderBy(Started)` —
   **cookie vs non-cookie pools**, least-recently-used first; waits up to
   10s (500ms polls) for an idle instance.
2. Marks `instance.InUse = true`, POSTs `url`+`macros` form-urlencoded to
   the instance, 30s `HttpClient` timeout.
3. Logs response size, resets `InUse`, returns the DOM JSON.

`POST /ProbeSearch` (`ProbeSearchController`) — same idle-pool pick
(**non-cookie instances only**), forwards `url`+`probe` to the instance's
`/ProbeSearch`, 60s timeout (page load + input drive + waits ≈35s).

### Instance model

`ConfigCharlotteInstance` — `Id`, `Url`, `ServerName`, `UsesCookies`,
`Note`, plus runtime `InUse`/`Started` (ignored in JSON).

## Charlotte instance

Minimal API (`BrowserController`):

| Endpoint | Args | Returns |
|---|---|---|
| `POST /` | `url`, `macros`, `backend` | serialized DOM JSON |
| `POST /ProbeSearch` | `url`, `probe` | the search request the page fired |

- `CompositeBrowser` chains a primary backend with fallback;
  `?backend=cefsharp|playwright` pins one. **CefSharp** (`Chrome.cs`) is
  the embedded-Chromium backend; **Playwright** (`PlaywrightBrowser.cs`)
  is headless Chromium — the only backend that can tap the network stack,
  so **ProbeSearch requires it**: drives the page's search input with the
  probe term and captures the request the site fires (used for
  [search-engine discovery](Search-Engines.md)).
- `extractDOM.js` runs in-page: serializes elements into the indexed
  `Document` structure (`Models/DOMNodes.cs`), marking `display:none`
  nodes; `Html.DeserializeArticle` rebuilds `DomElement`s with hierarchy
  for cleaning/chrome filtering.
- `Settings.BlacklistedDomains` — per-instance domain blocklist.

## How Collector calls it

`Common.Article.DownloadViaBrowser` → `App.BrowserEndpoint` (default
`http://localhost:7007/GetDOM` — the **Router**) with a 35s timeout
(Router's inner timeout is 30s). `SearchHelper` calls the Router's
`/ProbeSearch` for engine discovery.
