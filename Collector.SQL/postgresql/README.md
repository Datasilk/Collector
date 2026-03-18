# PostgreSQL Extensions

This folder contains scripts and utilities for building optional PostgreSQL extensions used by the Collector project.

## Requirements

Before running any build scripts, make sure you have the following installed locally:

1. **PostgreSQL** (native Windows installer). The script will attempt to find the latest version under:
   - `C:\Program Files\PostgreSQL`
   - `C:\Program Files (x86)\PostgreSQL`
   - **Important**: Add the PostgreSQL `bin` folder to your system `PATH` environment variable (e.g., `C:\Program Files\PostgreSQL\18\bin`). This ensures `psql` and other PostgreSQL tools are accessible from the command line.

2. **Microsoft Visual Studio** with **C++ build tools**.
   - The script uses `nmake`, so C++ support (MSVC toolchain) must be installed.

3. **x64 Native Tools Command Prompt for Visual Studio (Run as Administrator)**.
   - Run `install-extensions.bat` from the elevated x64 prompt so the MSVC build environment (compiler, `nmake`, etc.) is available on `PATH` and has permission to stop/start the PostgreSQL service.

## Installing pgvector

The `extensions/pgvector` submodule contains the pgvector project. To build and install it into your PostgreSQL installation:

1. Open the **x64 Native Tools Command Prompt for Visual Studio** as Administrator.
2. Navigate to this folder, e.g.
   ```bat
   cd C:\Projects\Collector\Collector.SQL\postgresql
   ```
3. Run the installer script:
   ```bat
   install-extensions.bat
   ```

The script will:
- Locate your PostgreSQL installation (`PGROOT`).
- Build pgvector using `nmake /F Makefile.win`.
- Run `nmake /F Makefile.win install` to copy the extension artifacts into PostgreSQL.

If PostgreSQL is installed in a non-standard location, edit `install-extensions.bat` and set the `PGROOT` variable manually before running the script.
