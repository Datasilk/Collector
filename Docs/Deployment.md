# Deployment

[Home](README.md) > Deployment

Publishing the app to a development or production environment is done with
the root **`deploy.bat`** (`C:\Projects\Collector\deploy.bat`). Dev-machine
setup is a different thing — see [Development Setup](Development-Setup.md).

## What `deploy.bat` does

1. **Remembers your answers** — reads `deploy.json` for the previous deploy
   path and configuration, offering them as defaults on re-runs.
2. **Prompts for a deploy path** — e.g. `M:\Collector` (required; aborts
   if empty).
3. **Prompts for environment** — `(D)evelopment` → `Debug` build config,
   `(P)roduction` → `Release`.
4. **Prompts for host type** — `(W)indows services` or `(D)ocker`
   containers.
5. **Publishes three projects** with `dotnet publish -r win-x64
   --self-contained false` (framework-dependent — the target needs the
   .NET 8 runtime):

   | Project | Output folder |
   |---|---|
   | `Collector.Web.Server` | `{path}\Collector.Web.Server` — the React client builds into `wwwroot` automatically |
   | `Charlotte\Charlotte` | `{path}\Charlotte` |
   | `Charlotte\Router` | `{path}\CharlottesWeb` |

6. **Copies the setup files for the chosen host** (all overwrites via
   `copy /y`):
   - **Windows** → `setup.bat`, `StartServices.bat`, `StopServices.bat`,
     `update-config.ps1`
   - **Docker** → `setup-docker.bat`, `update-config.ps1`, and
     `docker\*.yml` (`collector.yml`, `charlotte.yml`,
     `charlotte-router.yml`) into `{path}\docker\`
7. **Writes `deploy.json`** (`DeployPath`, `Config`, `Host`) so the next
   run defaults to the same answers.

Any publish failure aborts with an error.

## On the target machine

After deploying, run the copied `setup.bat` **as Administrator** once:

- Optionally registers `Charlotte` as a Windows Service
  (`sc create … start= auto`).
- Optionally configures **Charlotte's Web Router**: prompts for the admin
  user/password, then loops through Charlotte instance URLs (auto-appends
  `/Browser`, assigns ids `Charlotte-001`, `Charlotte-002`, …, records
  `ServerName`, `UsesCookies: false`) and writes the Router `config.json`.
- **Port configuration** — prompts for four ports (defaults shown):
  React front-end HTTP `7784`, front-end HTTPS `7783`, back-end HTTP
  `7780`, back-end HTTPS `7781`. Then `update-config.ps1` applies them:
  - `Collector.Web.Serverppsettings.json` — adds/updates `"Urls":
    "http://0.0.0.0:{be-http};https://0.0.0.0:{be-https}"` (what
    `Program.cs` binds) and sets `Auth.Domain`, `JWT.ValidIssuer`,
    `JWT.ValidAudience` to `https://localhost:{fe-https}`.
  - `wwwroot` JS assets — the built bundle carries `apiBasePath()`'s
    `7780`/`7781` literally; they're patched in place to the new ports.
  - `vite.config.js` (if present under the deploy path or a sibling
    `Collector.Web.Client`) — dev ports + `^/collector` proxy target.
  When the SPA is served by the server itself (production), set the
  front-end HTTPS port equal to the back-end HTTPS port.
- **Content data folder** — prompts for the Collector content root
  (e.g. `M:\Collector\Content`), creates it if missing, then
  `update-config.ps1` rewrites `Storage.Articles`/`Storage.Journal` to
  `{root}rticles\` and `{root}\journals\` (slashes normalized,
  trailing slash stripped, JSON double-escaping handled).
- Registers the Collector service and writes the shared `config.json`.

Then use the service scripts:

- `StartServices.bat` — reads `config.json` and `net start`s the
  `Charlotte`, `CharlottesWeb`, and `Collector` services.
- `StopServices.bat` — stops them.

## Database

`deploy.bat` publishes apps only — schema deploys are separate:
`Collector.SQL\deploy.bat Collector` (see [PostgreSQL](PostgreSQL.md)).
Run it against the target's PostgreSQL before starting services.

## Re-deploys

Re-run `deploy.bat` — previous answers are offered as defaults. Stop the
services first (`StopServices.bat`) so the published binaries aren't
locked.

> `deprecated/publish.bat` is the superseded deployment path — use the
> root `deploy.bat`.

## Docker (`setup-docker.bat`)

Chose Docker at the host prompt? Run `setup-docker.bat` instead — it asks
the same questions but runs each app via `docker compose up -d` on a
shared `collector-net` network:

- Ports + content path go into `docker\.env` (compose `${VAR}`
  interpolation); the host content folder mounts to `/collector-content`
  inside the collector container — `update-config.ps1 -StoragePrefix
  /collector-content` writes container paths into `appsettings.json`.
- Router config is written to `config.docker.json` +
  `config.docker.prod.json` too — the Router loads `config.docker*.json`
  when `DOTNET_RUNNING_IN_CONTAINER` is set.
- Charlotte runs on the Playwright base image (`v1.64.0-jammy`) — CefSharp
  is Windows-only; instances are reached by service name
  (`http://charlotte:7077`).
- HTTPS inside the collector container needs a cert — mount
  `~/.aspnet/https/aspnetapp.pfx` or terminate TLS upstream (see comments
  in `docker/collector.yml`).

Manage with `docker compose -f docker\<file>.yml logs -f / stop / down`.
