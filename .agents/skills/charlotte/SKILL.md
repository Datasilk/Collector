# Charlotte operations

Running and debugging the DOM-extraction stack.

## Processes

- **Instance** (`Charlotte/Charlotte`): `POST /` `{url, macros, backend}` →
  DOM JSON; `POST /ProbeSearch` `{url, probe}` → the search request the
  page fired. `?backend=cefsharp|playwright` pins a backend; default
  `CompositeBrowser` tries primary then fallback.
- **Router** (`Charlotte/Router`): MVC app on its own port. `POST /GetDOM`
  is the API Collector calls (`url`, `session`, `macros`).
  `App.BrowserEndpoint` default `http://localhost:7007/GetDOM`; Collector's
  call timeout is 35s (Router's inner is 30s, ProbeSearch 60s).

## Debugging a bad scrape

1. Open the Router dashboard → log in (config `Admin` creds) — instance
   pool status, `UsesCookies` flags, live request log.
2. `/dashboardhub` `CheckUrl(url, session, macros)` — run the exact scrape
   live and watch the `Log.WriteLine` lines: instance picked,
   send/receive, response size, errors.
3. Suspect bot-detection/CAPTCHA → the plain-HTTP path or a different
   instance may do better; suspect JS-rendered content → pin
   `backend=playwright`.
4. Empty DOM → check `Settings.BlacklistedDomains` on the instance and the
   DB blacklist tables.

## Instance pool mechanics

`UsesCookies` splits the pool; `OrderBy(Started)` picks least-recently-used
idle instance; `InUse` marks it busy; 10s wait for an idle one. Manage
instances via the dashboard's Add/Edit pages or the `Deploy\setup.bat`
loop — don't hand-edit `config.json` on a running Router.

## ProbeSearch

Playwright backend only — it taps the network stack: drives the page's
search input with a probe term and captures the outgoing request. This is
how `gather-research` discovers JS-driven site search engines.
