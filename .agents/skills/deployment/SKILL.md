# Deployment & setup

How to publish Collector and deploy the PostgreSQL schema.

## App deploy — root `deploy.bat`

- Run `deploy.bat` UNELEVATED when target is a mapped drive — admin
  sessions don't see `net use` mappings → `MSB3191 Unable to create
  directory` even though the folder exists in Explorer. Script
  pre-checks reachability.
- Interactive: asks deploy path (e.g. `M:\Collector`) and
  (D)ebug / (R)elease build config (compile config, not runtime env — setup.exe picks ASPNETCORE_ENVIRONMENT); remembers answers in
  `deploy.json` next to the script.
- Publishes 5 projects `dotnet publish -r win-x64`:
  `Collector.Web.Server` → `{path}\Collector.Web.Server` (React client
  builds into wwwroot), `Charlotte\Charlotte` → `{path}\Charlotte`,
  `Charlotte\Router` → `{path}\CharlottesWeb`, `Collector.Updater` →
  `{path}\Collector.Updater` — all framework-dependent; `Collector.Setup`
  → `{path}\setup.exe` single-file self-contained (no runtime needed on
  the target) — published LAST, after the Deploy copy.
- Copies the whole `Deploy\` folder to `{path}\Deploy\` — compose files
  under `Deploy\docker\` + Windows service scripts (`StartServices.ps1`,
  `StopServices.ps1`, `UnregisterServices.ps1`, `StartUpdater.ps1`,
  `StopUpdater.ps1`, `UnregisterUpdater.ps1`).
- Deploy only publishes + copies — it never runs docker/sc itself
  (target may be a network drive). When `{path}\Collector.Updater\bin`
  exists it drops a `_deploying` marker there and pauses 5s; the
  target's CollectorUpdater stops containers/services and deletes it
  (that's how bind-mount locks release). After all publishes finish it
  drops `_deployed` in the same folder — the updater bounces
  containers/services (bind-mounted binaries, so `restart` loads the
  new code) and deletes it. `deploy.bat --playwright` only asks the
  deploy path then drops `_playwright` → updater `docker exec`s the
  browser install in every running charlotte container. setup.exe
  deletes stale `_deploying`/`_deployed` markers before starting the
  updater.
- On the target: run `{path}\setup.exe` — **elevated** for Windows
  services, plain for Docker. First question is host type; answers saved
  in `{path}\setup.json` — PG + router admin passwords ARE saved
  (already plaintext in appsettings/config.json) so `setup.exe --skip`
  can run non-interactively.
- `setup.exe` does config natively (no PowerShell): appsettings `Urls`,
  `Auth.Domain`/JWT, `Storage.*`, `Ollama.Url`, `ConnectionStrings.
  Database`, and `wwwroot\api-base-path.json`.
- `setup.exe` also registers+starts the `CollectorUpdater` Windows
  service (warns if unelevated): FileSystemWatcher on its own `bin\`
  folder — `_deploying` → stop containers/services + delete marker;
  `_deployed` → `compose restart` / `sc start` + delete marker;
  `_playwright` → `docker exec` browser install in each charlotte
  container + delete marker. Logs to `{path}\Collector.Updater\
  bin\logs\` (timestamped, one file per service start).
- Windows flow: `sc create` for Charlotte/CharlottesWeb/Collector, Router
  `config.json` from interactive instance URLs (`/Browser` appended),
  writes `{path}\config.json` service list for `Deploy\*Services.ps1`.
- Docker flow: Ollama + Postgres host prompts auto-translate
  `localhost`/`127.0.0.1` → `host.docker.internal` (extra_hosts mapped;
  Ollama needs `OLLAMA_HOST=0.0.0.0`, PG needs `listen_addresses` +
  pg_hba entry for the docker subnet). Generates self-signed
  `aspnetapp.pfx` into `~/.aspnet/https` (random `CertPassword` →
  `Deploy\docker\.env` + `setup.json`). Charlotte scales via
  `--scale charlotte=N` (`collector-charlotte-N` replicas), then
  `install-browsers.sh` runs via `docker exec` in each — browsers persist
  in the `playwright-browsers` volume, `--with-deps` libs re-install
  every recreate.
- All 3 ymls share project `name: collector`; compose is always invoked
  merged (`-f a -f b -f c up -d` + `restart`; binaries are bind-mounted
  so restart reloads code, no recreate needed) — per-file commands
  report the other files' containers as orphans.
- `deprecated/publish.bat` is superseded — don't use it.

## Schema deploy — `Collector.SQL\deploy.bat [dbname]`

- Regenerates `deploy.sql` via `npx gulp --database <name>` then runs it
  with `psql -h localhost -U postgres -d template1`.
- **Authentication**: no prompt if `PGPASSWORD` is set — `setup.bat` sets a
  dev default. For non-interactive runs, set `PGPASSWORD` first.
- **Database names are case-sensitive** — the app uses `Collector`
  (connection string `Database=Collector`); explicit names are preserved +
  double-quoted by `gulpfile.js`; the no-arg default is `collector`.
- **`\i` includes**: psql treats quoted/spaced paths literally, so the
  gulpfile emits `\set f 'path'` + `\i :f` pairs (needed for the space in
  `Stored Procedures`). Don't "fix" those lines.
- Schema lives in `Sequences/`, `Tables/`, `Indexes/`, `Functions/`,
  `Stored Procedures/`, `Views/`, `data/` — no migrations; `gulpfile.js`
  topologically orders tables on REFERENCES.
- Verified shape after deploy: ~55 tables, ~120 routines, ~28 sequences +
  `data/` seed (Blacklist wildcards, DomainCollections-Countries).

## Dev-machine setup — root `setup.bat`

`git submodule` → npm installs (`Collector.Web.Client`, `Collector.SQL`) →
`postgresql/install-extensions.bat` (pgvector, needs nmake/VS Build Tools)
→ verifies `psql` on PATH → `deploy.bat Collector` with `PGPASSWORD` set.

## Gotchas

- Run the server with `ASPNETCORE_ENVIRONMENT=Development` or chat/detail
  logs + Swagger don't exist.
- Detached processes: `dotnet run` children die with the parent shell —
  use `Start-Process` (PowerShell) for a surviving server.
- `dotnet <dll>` without `--urls` binds port 5000; the app expects 7780.
