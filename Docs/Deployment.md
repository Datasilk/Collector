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
4. **Publishes three projects** with `dotnet publish -r win-x64
   --self-contained false` (framework-dependent — the target needs the
   .NET 8 runtime):

   | Project | Output folder |
   |---|---|
   | `Collector.Web.Server` | `{path}\Collector.Web.Server` — the React client builds into `wwwroot` automatically |
   | `Charlotte\Charlotte` | `{path}\Charlotte` |
   | `Charlotte\Router` | `{path}\CharlottesWeb` |

5. **Copies the service scripts** — `Deploy\setup.bat`,
   `Deploy\StartServices.bat`, `Deploy\StopServices.bat` into the deploy
   path.
6. **Writes `deploy.json`** so the next run defaults to the same answers.

Any publish failure aborts with an error.

## On the target machine

After deploying, run the copied `setup.bat` **as Administrator** once:

- Optionally registers `Charlotte` as a Windows Service
  (`sc create … start= auto`).
- Optionally configures **Charlotte's Web Router**: prompts for the admin
  user/password, then loops through Charlotte instance URLs (auto-appends
  `/Browser`, assigns ids `Charlotte-001`, `Charlotte-002`, …, records
  `ServerName`, `UsesCookies: false`) and writes the Router `config.json`.
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
