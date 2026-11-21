# Deployment & setup

How to publish Collector and deploy the PostgreSQL schema.

## App deploy — root `deploy.bat`

- Interactive: asks deploy path (e.g. `M:\Collector`) and
  (D)evelopment=`Debug` / (P)roduction=`Release`; remembers answers in
  `deploy.json` next to the script.
- Publishes 3 projects `dotnet publish -r win-x64 --self-contained false`:
  `Collector.Web.Server` → `{path}\Collector.Web.Server` (React client
  builds into wwwroot), `Charlotte\Charlotte` → `{path}\Charlotte`,
  `Charlotte\Router` → `{path}\CharlottesWeb`.
- Copies `Deploy\setup.bat`, `StartServices.bat`, `StopServices.bat` to the
  deploy path. On the target: run `setup.bat` **elevated** (registers
  `Charlotte`/`CharlottesWeb`/`Collector` Windows services, writes Router
  `config.json` with admin creds + instance URLs — `/Browser` is appended
  automatically), then `StartServices.bat`.
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
