# Deployment

[Home](README.md) > Deployment

Publishing the app to a development or production environment is done with
the root **`deploy.bat`** (`C:\Projects\Collector\deploy.bat`). Dev-machine
setup is a different thing — see [Development Setup](Development-Setup.md).

## What `deploy.bat` does

1. **Remembers your answers** — reads `deploy.json` for the previous deploy
   path and configuration, offering them as defaults on re-runs.
2. **Prompts for a deploy path** — e.g. `M:\Collector` (required; aborts
   if empty). Run `deploy.bat` **unelevated** when the target is a mapped
   network drive — elevated sessions don't see `net use` mappings, and
   publish fails with `MSB3191 Unable to create directory` even though
   the folder exists in Explorer. (The script pre-checks reachability and
   prints this hint when the path is unreachable.)
3. **Prompts for build configuration** — `(D)ebug` or `(R)elease`. This
   is the compile config, *not* the runtime environment —
   `ASPNETCORE_ENVIRONMENT` (Development/Production) is chosen later by
   `setup.exe`.
4. **Writes `deploy.json`** (`DeployPath`, `Config`) *before*
   publishing — a failed run still saves your answers as next run's
   defaults.
5. **Publishes five projects** with `dotnet publish -r win-x64`
   (filter with `--server --charlotte --router --updater --setup`,
   combinable; no args = all):

   | Project | Output |
   |---|---|
   | `Collector.Web.Server` | `{path}\Collector.Web.Server` — framework-dependent; the React client builds into `wwwroot` automatically |
   | `Charlotte\Charlotte` | `{path}\Charlotte` — framework-dependent |
   | `Charlotte\Router` | `{path}\CharlottesWeb` — framework-dependent |
   | `Collector.Updater` | `{path}\Collector.Updater` — framework-dependent |
   | `Collector.Setup` | `{path}\setup.exe` — single-file **self-contained** exe (~67 MB), no .NET runtime needed on the target |

6. **Copies the entire `Deploy\` folder** to `{path}\Deploy\` (xcopy,
   overwrites) — compose files (`Deploy\docker\*.yml`,
   `install-browsers.sh`) plus the Windows service scripts
   (`StartServices.ps1`, `StopServices.ps1`, `UnregisterServices.ps1`,
   `StartUpdater.ps1`, `StopUpdater.ps1`, `UnregisterUpdater.ps1`).

7. **Publishes `setup.exe` last** — after all other projects *and* the
   `Deploy` copy.

8. **Drops `_deployed`** — when a `_deploying` marker was written at the
   start, deploy.bat finishes by writing `_deployed` next to it. The
   `CollectorUpdater` service (below) sees that marker and bounces the
   containers (binaries are bind-mounted — a restart loads new code).

Any publish failure aborts with an error.

## On the target machine — `setup.exe`

Run `setup.exe` in the deploy root. **As Administrator** for Windows
services (`sc create` needs it); unelevated is fine for Docker. The first
question is the host type — `(W)indows services` or `(D)ocker` containers
— and every answer is saved to `setup.json` as next run's default.

**`setup.exe --skip`** — non-interactive mode: no prompts, every answer
comes from `setup.json`. PostgreSQL and router admin passwords *are*
saved there (they're already plaintext in `appsettings.json`/`config.json`
anyway) so a skip run can rebuild both. The Collector Updater service
invokes this automatically.

### Collector Updater service

`setup.exe` registers `CollectorUpdater` (`{path}\Collector.Updater\
Collector.Updater.exe`) if missing and starts it if stopped — on either
host choice (warns instead of failing when unelevated). The service
watches its own `bin\` folder with a `FileSystemWatcher` for two
markers, logging to
`{path}\Collector.Updater\bin\logs\{yyyy-MM-dd-HH-mm}.log` (one file per
service start):

- **`_deploying`** (written by deploy.bat before publishing): stop the
  compose stack (`docker compose stop`) — or the `Collector`/`Charlotte`/
  `CharlottesWeb` services when no containers exist — then delete the
  marker so publish can overwrite bind-mounted binaries.
- **`_deployed`** (written by deploy.bat after all publishes finish):
  delete the marker, then bounce — `docker compose restart` when
  containers exist (bind-mounted binaries load the new code), or
  `sc start` for the Windows services. No `setup.exe` run.
- **`_playwright`** (written by `deploy.bat --playwright`, which only
  asks the deploy path and drops the marker): `docker exec` the
  install-browsers.sh script in every running `collector-charlotte-N`
  container, then delete the marker. Sentinels in the script make
  already-current installs a fast no-op.

`setup.exe` edits configuration natively — no PowerShell scripts:

- `Collector.Web.Server\appsettings.json` — `"Urls":
  "http://0.0.0.0:{be-http};https://0.0.0.0:{be-https}"` (what
  `Program.cs` binds); `Auth.Domain`, `JWT.ValidIssuer`,
  `JWT.ValidAudience` → `https://localhost:{fe-https}`;
  `Storage.Articles`/`Storage.Journal` → the content folder.
- `wwwroot\api-base-path.json` — the client API URLs
  (`endpoints.js` loads this file at runtime; nothing else is patched).
- Docker adds: `Ollama.Url` and the full
  `ConnectionStrings.Database` (`Host=…;Database=…;Username=…;Password=…;Keepalive=30`).

### Windows services flow

- Optionally registers `Charlotte` as a Windows Service
  (`sc create … start= auto`).
- Optionally configures **Charlotte's Web Router**: prompts for the admin
  user/password, then loops through Charlotte instance URLs (auto-appends
  `/Browser`, assigns ids `Charlotte-001`, `Charlotte-002`, …, records
  `ServerName`, `UsesCookies: false`) and writes the Router `config.json`
  (+ `config.prod.json`).
- **Port configuration** — four ports (defaults `7784`/`7783`/`7780`/`7781`).
  When the SPA is served by the server itself (production), set the
  front-end HTTPS port equal to the back-end HTTPS port.
- **Content data folder** — prompts for the Collector content root,
  creates it if missing, rewrites the Storage paths. Blank keeps existing.
- Registers the Collector service and writes `{root}\config.json`
  (`"Services": [...]` — read by the `Deploy\*Services.ps1` scripts so
  they know which services to start/stop/unregister).

Then use the service scripts under `Deploy\`:

- `Deploy\StartServices.ps1` — reads `config.json` and starts the
  `Charlotte`, `CharlottesWeb`, and `Collector` services.
- `Deploy\StopServices.ps1` — stops them.
- `Deploy\UnregisterServices.ps1` — `sc delete`s them.
- `Deploy\StartUpdater.ps1` / `Deploy\StopUpdater.ps1` — start/stop the
  `CollectorUpdater` service.
- `Deploy\UnregisterUpdater.ps1` — stops/removes the `CollectorUpdater`
  service (also resolves by display name, in case it was registered
  under a different service name).

## Database

`deploy.bat` publishes apps only — schema deploys are separate:
`Collector.SQL\deploy.bat Collector` (see [PostgreSQL](PostgreSQL.md)).
Run it against the target's PostgreSQL before starting services.

## Re-deploys

Re-run `deploy.bat` — previous answers are offered as defaults.
`deploy.bat` only publishes and copies files (the target may be a
network drive — it never runs docker/sc itself). When
`{path}\Collector.Updater\bin` exists it drops a `_deploying` marker
there and pauses 5s; the target's CollectorUpdater service sees it and
stops containers/services (they lock bind-mounted binaries over the
share). When publishing finishes it drops `_deployed` in the same
folder, and the updater bounces the containers/services to load the new
binaries. `setup.exe` also deletes stale `_deploying`/`_deployed`
markers before starting the updater service.

> `deprecated/publish.bat` is the superseded deployment path — use the
> root `deploy.bat`.

## Docker flow

Choose Docker at the `setup.exe` host prompt — it runs each app via
`docker compose up -d` on a shared `collector-net` network:

- Prompts for **Ollama URL** (default `http://localhost:11434`) and
  **PostgreSQL host/database/username/password**. `localhost` and
  `127.0.0.1` are automatically translated to `host.docker.internal`
  (the compose files map that name via `extra_hosts`; it resolves to the
  Docker host's LAN interface — so Ollama must bind `0.0.0.0` and
  Postgres needs `listen_addresses` + a `pg_hba.conf` entry for the
  docker subnet, not just loopback).
- Prompts for a **content folder** (required, created if missing) which
  mounts to `/collector-content` in the collector container;
  `Storage.*` paths are written as `/collector-content/articles` +
  `/collector-content/journals`.
- Ports + content path + cert password go into `Deploy\docker\.env`
  (passed via `--env-file`).
- **HTTPS cert** — Kestrel binds `https://0.0.0.0` inside the collector
  container and loads `/https/aspnetapp.pfx`. If
  `~/.aspnet/https/aspnetapp.pfx` doesn't exist, `setup.exe` generates a
  self-signed `CN=localhost` cert natively (`CertificateRequest`) with a
  random password (saved as `CertPassword` in `setup.json` and into
  `.env` as `CERT_PASSWORD`).
- **Charlotte scaling** — asks how many instances; starts them with
  `--scale charlotte=N` (replicas `collector-charlotte-1..N`), then runs
  `install-browsers.sh` in each via `docker exec` — browsers land in the
  persistent `playwright-browsers` volume; the `--with-deps` system libs
  re-run every recreate since the container filesystem is fresh.
- Router instance URLs are generated from the count
  (`http://collector-charlotte-N:7077/Browser`) and written to
  `config.json` + `config.docker.json` + `config.docker.prod.json` +
  `config.prod.json`.
- All three compose files share project name `collector` and are invoked
  as **one merged command** (`docker compose -f a -f b -f c up -d …`)
  followed by `restart` — app binaries are bind-mounted so a bounce
  loads new code without recreating containers. Everything is one stack
  and nothing reports orphan containers.

Manage with:
`docker compose -f Deploy\docker\collector.yml -f Deploy\docker\charlotte.yml -f Deploy\docker\charlotte-router.yml logs -f / stop / down`
