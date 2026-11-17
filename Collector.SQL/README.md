# Collector.SQL

PostgreSQL schema and deployment scripts for Collector. The database was
fully migrated from Microsoft SQL Server — everything here is PostgreSQL.

## Layout

- `Tables/` — `CREATE TABLE IF NOT EXISTS` scripts (organized by domain)
- `Sequences/` — `CREATE SEQUENCE` scripts (deployed first — tables use
  `nextval()` column defaults)
- `Indexes/` — `CREATE INDEX` scripts
- `Functions/` — `CREATE OR REPLACE FUNCTION` scripts
- `Stored Procedures/` — procedure scripts
- `Views/` — view scripts (optional folder)
- `data/` — seed data (`Blacklist.sql`, `DomainCollections-Countries.sql`)
- `extensions/` — git submodules for PostgreSQL extensions (`pgvector`)
- `deploy/` — legacy SQL Server deploy scripts (reference only, not deployed)

## Deploying the schema

`deploy.bat` generates `deploy.sql` and runs it against the local server:

```bat
deploy.bat            :: deploys to database "collector"
deploy.bat mydb       :: deploys to a different database name
```

It runs `npx gulp --database <name>` which compiles `deploy.sql` from the
folders above, then executes it with
`psql -h localhost -U postgres -d template1 -a -e -f deploy.sql`. The script
creates the database if it does not exist, then applies every object.

Objects are deployed in dependency order: `Sequences` → `Tables` (topologically
sorted on `REFERENCES`) → `Indexes` → `Functions` → `Stored Procedures` →
`Views` → `data`.

**Requirements**: PostgreSQL's `bin` folder on `PATH` (so `psql` works) and
Node.js with the `gulp` devDependency installed (`npm install` in this
folder).

Other gulp tasks:

```bat
npx gulp update-deploy   :: regenerate deploy.sql without deploying
npx gulp watch           :: regenerate deploy.sql when any .sql file changes
```

## PostgreSQL extensions (pgvector)

The `extensions/pgvector` submodule contains the pgvector project. To build
and install it into your PostgreSQL installation:

**Requirements**

1. **PostgreSQL** (native Windows installer) — the script looks for the
   latest version under `C:\Program Files\PostgreSQL` or
   `C:\Program Files (x86)\PostgreSQL`. If installed elsewhere, set `PGROOT`
   in `install-extensions.bat` manually.
2. **Microsoft Visual Studio** with **C++ build tools** (`nmake`).
3. An elevated **x64 Native Tools Command Prompt for Visual Studio** (needed
   for the MSVC build environment and to stop/start the PostgreSQL service).

**Install**

```bat
cd C:\Projects\Collector\Collector.SQL
install-extensions.bat
```

The script locates your PostgreSQL installation (`PGROOT`), builds pgvector
with `nmake /F Makefile.win`, and runs `nmake /F Makefile.win install` to
copy the extension artifacts into PostgreSQL.
