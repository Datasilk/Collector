@echo off
setlocal

set "npm_config_yes=true"

chcp 65001 >nul

set "ICON_SETUP=⚙"
set "ICON_GIT=🔧"
set "ICON_NPM=📦"
set "ICON_DB=🗄"
set "ICON_SQL=🧾"
set "ICON_DONE=✅"
set "ICON_ERROR=❌"
set "ICON_WARN=⚠"
set "NATIVE_TOOLS_READY="

call :ensureAdmin
if errorlevel 1 exit /b 1

call :ensureNativeTools
if errorlevel 1 exit /b 1

for /f %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"

call :echoGreen "%ICON_SETUP% Setting up Collector for development environment..."
call :echoGreen "%ICON_GIT% Installing necessary git submodules..."
git submodule init
git submodule update --init
 
call :runNpmInstall "Collector.Web.Client" "%ICON_NPM% Installing necessary npm packages for React app..."
call :runNpmInstall "Collector.SQL" "%ICON_NPM% Installing necessary npm packages for Collector.SQL..."

pushd Collector.SQL

call :echoGreen "%ICON_DB% Installing PostgreSQL extensions..."
pushd postgresql
call install-extensions.bat
if errorlevel 1 exit /b %errorlevel%
popd

call :echoGreen "%ICON_DB% Checking for PostgreSQL client tools..."
where psql
if errorlevel 1 (
  echo ERROR: psql was not found on PATH. Install PostgreSQL client tools and/or add psql.exe to PATH.
  exit /b 1
)

call :echoGreen "%ICON_DB% Ensuring PostgreSQL database \"Collector\" exists..."
psql -v ON_ERROR_STOP=1 -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='Collector'" | findstr /c:"1" >nul
if errorlevel 1 (
  psql -v ON_ERROR_STOP=1 -U postgres -d postgres -c "CREATE DATABASE \"Collector\";"
  if errorlevel 1 exit /b %errorlevel%
)

call :echoGreen "%ICON_SQL% Executing postgresql-tables.sql against database \"Collector\"..."
psql -v ON_ERROR_STOP=1 -U postgres -d Collector -f postgresql/postgresql-tables.sql
if errorlevel 1 exit /b %errorlevel%

popd

call :echoGreen "%ICON_DONE% Setup complete!"

:runNpmInstall
set "TARGET_DIR=%~1"
set "STATUS_MSG=%~2"
call :echoGreen "%STATUS_MSG%"
pushd "%TARGET_DIR%"
call npm install --loglevel=error --no-progress >nul 2>&1
set "NPM_RESULT=%errorlevel%"
popd
if not "%NPM_RESULT%"=="0" (
  call :echoRed "%ICON_ERROR% npm install failed in %TARGET_DIR%. Use npm install inside that folder for details."
  exit /b %NPM_RESULT%
)
exit /b 0

:echoGreen
if not defined ESC (
  echo %~1
  exit /b 0
)
echo %ESC%[32m%~1%ESC%[0m
exit /b 0

:echoRed
if not defined ESC (
  echo %~1
  exit /b 0
)
echo %ESC%[31m%~1%ESC%[0m
exit /b 0

:ensureAdmin
net session >nul 2>&1
if errorlevel 1 (
  echo "%ICON_WARN% Administrator privileges are required. Please rerun setup.bat from an elevated command prompt."
  pause
  exit /b 1
)
exit /b 0

:ensureNativeTools
if defined NATIVE_TOOLS_READY exit /b 0

nmake /? >nul 2>&1
if not errorlevel 1 (
  set "NATIVE_TOOLS_READY=1"
  exit /b 0
)

echo "%ICON_WARN% nmake not found. Please run this script from the 'x64 Native Tools Command Prompt for VS' (run as Administrator)."
pause
exit /b 1