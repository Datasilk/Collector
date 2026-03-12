@echo off
setlocal enabledelayedexpansion

rem Resolve repository root (directory containing this script)
set "SCRIPT_DIR=%~dp0"
set "EXTENSIONS_DIR=%SCRIPT_DIR%\extensions"
set "PGVECTOR_DIR=%EXTENSIONS_DIR%\pgvector"

if not exist "%PGVECTOR_DIR%" (
    echo [ERROR] pgvector submodule not found at "%PGVECTOR_DIR%".
    exit /b 1
)

rem Attempt to locate PostgreSQL installation root automatically
set "PGROOT="
for %%B in ("C:\Program Files\PostgreSQL" "C:\Program Files (x86)\PostgreSQL") do (
    if exist %%~B (
        for /f "delims=" %%V in ('dir /b /ad "%%~B" ^| sort /r') do (
            if exist "%%~B\%%~V\include\server\pg_config.h" (
                set "PGROOT=%%~B\%%~V"
                goto :found_pgroot
            )
        )
    )
)

:found_pgroot
if not defined PGROOT (
    echo [ERROR] Unable to locate PostgreSQL installation. Please install PostgreSQL or set PGROOT manually.
    exit /b 1
)

echo Using PGROOT="%PGROOT%"
set "PGROOT=%PGROOT%"
set "PGSERVICE_X64="
set "PGSERVICE_X86="
set "PGSERVICE_STOPPED="

call :derive_pg_services "%PGROOT%"
call :stop_postgres_service

rem Build and install pgvector extension
pushd "%PGVECTOR_DIR%"
call nmake /F Makefile.win
if errorlevel 1 (
    echo [WARN] nmake build failed. Attempting clean and rebuild.
    call nmake /F Makefile.win clean
    if errorlevel 1 (
        echo [ERROR] nmake clean failed. Please run this script from the "x64 Native Tools Command Prompt for VS".
        call :start_postgres_service
        popd
        exit /b 1
    )

    call nmake /F Makefile.win
    if errorlevel 1 (
        echo [ERROR] nmake build failed after retry. Ensure you are using the "x64 Native Tools Command Prompt for VS" and rerun this script.
        call :start_postgres_service
        popd
        exit /b 1
    )
)

call nmake /F Makefile.win install
set "NMAKE_ERROR=%errorlevel%"
popd

if not "%NMAKE_ERROR%"=="0" (
    echo [ERROR] nmake install failed.
    call :start_postgres_service
    exit /b %NMAKE_ERROR%
)

call :start_postgres_service
echo pgvector extension built and installed successfully.
exit /b 0

:derive_pg_services
set "PGROOT_FOLDER="
for %%I in (%~1) do set "PGROOT_FOLDER=%%~nxI"

if not defined PGROOT_FOLDER goto :eof

set "PGSERVICE_X64=postgresql-x64-%PGROOT_FOLDER%"
set "PGSERVICE_X86=postgresql-%PGROOT_FOLDER%"
goto :eof

:stop_postgres_service
if defined PGSERVICE_STOPPED goto :eof

call :stop_service "%PGSERVICE_X64%"
if defined PGSERVICE_STOPPED goto :eof
call :stop_service "%PGSERVICE_X86%"
goto :eof

:stop_service
set "TARGET_SERVICE=%~1"
if "%TARGET_SERVICE%"=="" goto :eof
sc query "%TARGET_SERVICE%" >nul 2>&1 || goto :eof
for /f "delims=" %%S in ('sc query "%TARGET_SERVICE%" ^| findstr /I /C:"STATE"') do set "SERVICE_STATE=%%S"
echo %SERVICE_STATE% | findstr /I "RUNNING" >nul 2>&1 || goto :eof
echo Stopping PostgreSQL service "%TARGET_SERVICE%"...
net stop "%TARGET_SERVICE%" >nul
if errorlevel 1 (
    echo [WARN] Failed to stop PostgreSQL service "%TARGET_SERVICE%". Continuing.
) else (
    set "PGSERVICE_STOPPED=%TARGET_SERVICE%"
)
goto :eof

:start_postgres_service
if not defined PGSERVICE_STOPPED goto :eof
echo Starting PostgreSQL service "%PGSERVICE_STOPPED%"...
net start "%PGSERVICE_STOPPED%" >nul
if errorlevel 1 (
    echo [WARN] Failed to start PostgreSQL service "%PGSERVICE_STOPPED%". Please start it manually.
)
set "PGSERVICE_STOPPED="
goto :eof
