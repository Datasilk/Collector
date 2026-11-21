# Development Setup

[Home](README.md) > Development Setup

How to set up a dev machine and run the stack locally. For publishing to a
development or production environment, see [Deployment](Deployment.md).

## Environment setup (dev machine)

`setup.bat` at the repo root (run elevated, in *x64 Native Tools Command
Prompt for VS*):

1. `git submodule init/update`
2. `npm install` in `Collector.Web.Client` and `Collector.SQL`
3. `postgresql/install-extensions.bat` — builds & installs pgvector into
   PostgreSQL (requires nmake; auto-installs VS Build Tools if missing)
4. Verifies `psql` on PATH
5. Sets `PGPASSWORD` (dev default; override by setting it beforehand) and
   runs `Collector.SQL\deploy.bat Collector` — creates the database if
   needed and applies the full schema + seed data, non-interactive

## Building & running

- `dotnet build` at the solution root; `Collector.Web.Server` is the host
  (API + workers + hubs). Run with `dotnet run` or the built dll with
  `--urls http://0.0.0.0:7780`.
- **Development vs Production matters**: chat logs, detail logs, and
  Swagger are Development-only. Live-testing the chat system requires
  `ASPNETCORE_ENVIRONMENT=Development`.
- React client: `npm run dev` in `Collector.Web.Client` (Vite + React 19).
- Charlotte browser + Router services run as separate processes (see
  [Charlotte](Charlotte.md)); the Router pools instances in config.
- Ollama must be running locally for AI features.

## Configuration

`appsettings.json`: `ConnectionStrings.Database` (PostgreSQL),
`Storage.Articles`/`Storage.Journal` (filesystem roots), `Auth` (JWT),
`MediaSettings`, LLM/Ollama settings. `appsettings.Development.json` is
intentionally minimal — dev overrides only.

## Database deploys

Schema deploys are separate from app deploys — see
[PostgreSQL](PostgreSQL.md) (`Collector.SQL\deploy.bat [dbname]`). — `Collector.SQL\deploy.bat [dbname]`.
