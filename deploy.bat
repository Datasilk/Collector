@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo.
echo ============================================
echo   Collector Deployment
echo ============================================
echo.

rem --- load saved answers from a previous run ---
set "DEPLOY_CFG=%~dp0deploy.json"
set "SAVED_PATH="
set "SAVED_CONFIG="
if exist "%DEPLOY_CFG%" (
    for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "try { (Get-Content '%DEPLOY_CFG%' -Raw | ConvertFrom-Json).DeployPath -replace '\\+','\' } catch { '' }"`) do (
        set "SAVED_PATH=%%a"
    )
    for /f "tokens=1* delims=:" %%a in ('findstr /i "\"Config\"" "%DEPLOY_CFG%"') do (
        set "SAVED_CONFIG=%%b"
    )

    if defined SAVED_CONFIG (
        set "SAVED_CONFIG=!SAVED_CONFIG:~2!"
        set "SAVED_CONFIG=!SAVED_CONFIG:"=!"
        set "SAVED_CONFIG=!SAVED_CONFIG:,=!"
    )
)

if defined SAVED_PATH (
    set "PATH_PROMPT=Deploy path [!SAVED_PATH!]: "
) else (
    set "PATH_PROMPT=Deploy path, e.g. M:\Collector: "
)
set /p "DEPLOY_PATH=!PATH_PROMPT!"
if "!DEPLOY_PATH!"=="" set "DEPLOY_PATH=!SAVED_PATH!"
if "!DEPLOY_PATH!"=="" (
    echo No path given - aborting.
    exit /b 1
)
if "!DEPLOY_PATH:~-1!"=="\" set "DEPLOY_PATH=!DEPLOY_PATH:~0,-1!"

rem --- --playwright: signal the target's updater to docker exec the
rem --- playwright browser install in every charlotte container. No
rem --- publishing happens - just the marker drop.
for %%a in (%*) do if /i "%%a"=="--playwright" set "DO_PLAYWRIGHT=1"
if defined DO_PLAYWRIGHT (
    if not exist "%DEPLOY_PATH%\Collector.Updater\bin" (
        echo   Updater bin folder not found - nothing to signal.
        exit /b 1
    )
    type nul > "%DEPLOY_PATH%\Collector.Updater\bin\_playwright"
    echo   Signalled target to install/update Playwright in charlotte containers.
    exit /b 0
)

echo.
set "CFG_HINT=D"
if /i "!SAVED_CONFIG!"=="Release" set "CFG_HINT=R"
set /p "CFG_CHOICE=Publish (D)ebug or (R)elease build? [!CFG_HINT!]: "
if "!CFG_CHOICE!"=="" set "CFG_CHOICE=!CFG_HINT!"
if /i "!CFG_CHOICE!"=="R" (
    set "CONFIG=Release"
) else (
    set "CONFIG=Debug"
)

echo.
echo Deploying %CONFIG% to %DEPLOY_PATH%
echo.

rem --- project filter: --setup --updater --server --charlotte --router
rem --- (combinable). No args = deploy everything.
set "W_SERVER=" & set "W_CHARLOTTE=" & set "W_ROUTER=" & set "W_UPDATER=" & set "W_SETUP="
set "FILTERED="
for %%a in (%*) do (
    if /i "%%a"=="--server"    (set "W_SERVER=1"    & set "FILTERED=1")
    if /i "%%a"=="--charlotte" (set "W_CHARLOTTE=1" & set "FILTERED=1")
    if /i "%%a"=="--router"    (set "W_ROUTER=1"    & set "FILTERED=1")
    if /i "%%a"=="--updater"   (set "W_UPDATER=1"   & set "FILTERED=1")
    if /i "%%a"=="--setup"     (set "W_SETUP=1"     & set "FILTERED=1")
)
if not defined FILTERED (
    set "W_SERVER=1" & set "W_CHARLOTTE=1" & set "W_ROUTER=1" & set "W_UPDATER=1" & set "W_SETUP=1"
)

rem --- save answers now so a failed run still keeps them ---
powershell -NoProfile -Command "[IO.File]::WriteAllText('%~dp0deploy.json', (@{ DeployPath = '%DEPLOY_PATH%'; Config = '%CONFIG%' } | ConvertTo-Json))"

rem --- deploy marker: if Deploy\ already exists this target was deployed
rem --- before, so the CollectorUpdater service may be watching. Drop
rem --- .deploying; the updater stops containers/services (they lock
rem --- bind-mounted binaries over the share) then deletes it. We poll
rem --- for the deletion only when the updater is actually installed.
rem --- stop signal: drop _deploying in the updater's bin folder; the
rem --- running CollectorUpdater service sees it and stops the compose
rem --- stack + services so publish can overwrite bind-mounted binaries.
rem --- Only needed when publishing bind-mounted binaries - a pure
rem --- --setup/--updater deploy touches nothing locked by containers.
set "NEED_STOP="
if defined W_SERVER set "NEED_STOP=1"
if defined W_CHARLOTTE set "NEED_STOP=1"
if defined W_ROUTER set "NEED_STOP=1"

if not defined NEED_STOP goto :publish
if not exist "%DEPLOY_PATH%\Collector.Updater\bin" goto :publish
type nul > "%DEPLOY_PATH%\Collector.Updater\bin\_deploying"
set "MARKER_DROPPED=1"
echo   Signalled target to stop services - pausing 5s...
rem the updater restarts everything via 'setup.exe --skip' when the
rem setup.exe FILE CHANGES - so a marker deploy must always publish
rem setup.exe (last) or stopped containers/services never come back.
if not defined W_SETUP (
    set "W_SETUP=1"
    echo   setup.exe added to this deploy - it is what restarts the target.
)
timeout /t 5 /nobreak >nul

:publish

if defined W_SERVER (
    echo [server] Publishing Collector.Web.Server ^(%CONFIG%^)...
    echo           ^(builds Collector.Web.Client into wwwroot automatically^)
    dotnet publish Collector.Web.Server\Collector.Web.Server.csproj -c %CONFIG% -r win-x64 --self-contained false -o "%DEPLOY_PATH%\Collector.Web.Server"
    if errorlevel 1 goto :fail
    echo.
)

if defined W_CHARLOTTE (
    echo [charlotte] Publishing Charlotte ^(%CONFIG%^)...
    dotnet publish Charlotte\Charlotte\Charlotte.csproj -c %CONFIG% -r win-x64 --self-contained false -o "%DEPLOY_PATH%\Charlotte"
    if errorlevel 1 goto :fail
    echo.
)

if defined W_ROUTER (
    echo [router] Publishing Charlotte Router ^(%CONFIG%^)...
    dotnet publish Charlotte\Router\Charlotte.Router.csproj -c %CONFIG% -r win-x64 --self-contained false -o "%DEPLOY_PATH%\CharlottesWeb"
    if errorlevel 1 goto :fail
    echo.
)

if defined W_UPDATER (
    echo [updater] Publishing Collector.Updater ^(%CONFIG%^)...
    dotnet publish Collector.Updater\Collector.Updater.csproj -c %CONFIG% -r win-x64 --self-contained true -o "%DEPLOY_PATH%\Collector.Updater"
    if errorlevel 1 goto :fail
    echo.
)

echo Copying Deploy folder to %DEPLOY_PATH%\Deploy - overwrites existing...
xcopy /e /i /y "%~dp0Deploy" "%DEPLOY_PATH%\Deploy" >nul
if errorlevel 1 goto :fail

rem --- setup.exe LAST: convention only - everything else must be staged
rem --- before the restart trigger below fires 'setup.exe --skip'.
if defined W_SETUP (
    echo.
    echo [setup] Publishing setup.exe ^(%CONFIG%^)...
    dotnet publish Collector.Setup\Collector.Setup.csproj -c %CONFIG% -r win-x64 --self-contained true -o "%DEPLOY_PATH%"
    if errorlevel 1 goto :fail
)

rem --- publish complete: if we dropped _deploying at the start, the
rem --- target's services are stopped - signal _deployed so the updater
rem --- runs 'setup.exe --skip' and brings everything back up.
if defined MARKER_DROPPED (
    type nul > "%DEPLOY_PATH%\Collector.Updater\bin\_deployed"
    echo.
    echo   Signalled target that deploy finished - setup will restart services.
)

echo.
echo Done! Next: run setup.exe in %DEPLOY_PATH% on the target machine
echo (as Administrator for Windows services, or plainly for Docker).
exit /b 0

:fail
echo.
echo *** Deployment failed - see output above. ***
exit /b 1
