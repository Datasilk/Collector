@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

echo.
echo ============================================
echo   Collector ^& Charlotte Setup (Docker)
echo ============================================
echo.

where docker >nul 2>&1
if errorlevel 1 (
    echo   ERROR: docker not found on PATH - install Docker Desktop first.
    pause
    exit /b 1
)

rem --- port configuration ---
set /p "PORT_FE_HTTP=React front end HTTP port [7784]: "
if "!PORT_FE_HTTP!"=="" set "PORT_FE_HTTP=7784"
set /p "PORT_FE_HTTPS=React front end HTTPS port [7783]: "
if "!PORT_FE_HTTPS!"=="" set "PORT_FE_HTTPS=7783"
set /p "PORT_BE_HTTP=Server backend HTTP port [7780]: "
if "!PORT_BE_HTTP!"=="" set "PORT_BE_HTTP=7780"
set /p "PORT_BE_HTTPS=Server backend HTTPS port [7781]: "
if "!PORT_BE_HTTPS!"=="" set "PORT_BE_HTTPS=7781"

rem --- content data folder ---
echo.
set /p "CONTENT_PATH=Collector content data folder (e.g. M:\Collector\Content): "
if "!CONTENT_PATH!"=="" (
    echo   ERROR: a content folder is required for the collector container.
    pause
    exit /b 1
)
if not exist "!CONTENT_PATH!" (
    echo   Creating !CONTENT_PATH! ...
    mkdir "!CONTENT_PATH!"
)

rem --- docker/.env (ports + content mount for the compose files) ---
if not exist "%ROOT%\docker" mkdir "%ROOT%\docker"
(
echo CONTENT_PATH=!CONTENT_PATH!
echo PORT_FE_HTTP=!PORT_FE_HTTP!
echo PORT_FE_HTTPS=!PORT_FE_HTTPS!
echo PORT_BE_HTTP=!PORT_BE_HTTP!
echo PORT_BE_HTTPS=!PORT_BE_HTTPS!
) > "%ROOT%\docker\.env"
echo   Wrote %ROOT%\docker\.env

rem --- update Collector appsettings.json (container paths + ports) ---
echo.
echo Applying configuration to Collector.Web.Server\appsettings.json ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\update-config.ps1" -Root "%ROOT%" -FeHttp !PORT_FE_HTTP! -FeHttps !PORT_FE_HTTPS! -BeHttp !PORT_BE_HTTP! -BeHttps !PORT_BE_HTTPS! -StoragePrefix "/collector-content"
if errorlevel 1 (echo   WARNING: config update script failed - check output above.)
echo.

rem --- shared docker network ---
docker network inspect collector-net >nul 2>&1
if errorlevel 1 (
    docker network create collector-net >nul
    echo   Created docker network collector-net
)

rem --- Charlotte container ---
set /p "RUN_CHARLOTTE=Run Charlotte as a Docker container? (Y/N): "
if /i "!RUN_CHARLOTTE!"=="Y" (
    if not exist "%ROOT%\Charlotte\Charlotte.dll" (
        echo   ERROR: %ROOT%\Charlotte\Charlotte.dll not found - run deploy.bat first.
    ) else (
        docker compose -f "%ROOT%\docker\charlotte.yml" up -d
        if errorlevel 1 (echo   Failed to start charlotte container.) else (echo   charlotte container started.)
    )
)
echo.

rem --- Charlotte's Web Router setup ---
set /p "SETUP_ROUTER=Set up Charlotte's Web Router? (Y/N): "
if /i not "!SETUP_ROUTER!"=="Y" goto :collector_container

set /p "ADMIN_USER=Admin account [admin]: "
if "%ADMIN_USER%"=="" set "ADMIN_USER=admin"
set /p "ADMIN_PASS=Admin password: "

set "ROUTER_DIR=%ROOT%\CharlottesWeb"
if not exist "%ROUTER_DIR%" (
    echo   ERROR: %ROUTER_DIR% not found - run deploy.bat first.
    goto :collector_container
)

set /a COUNT=0
set "INSTANCES="

:instance_loop
if %COUNT%==0 (
    rem containers reach the instance by service name on collector-net
    set /p "INSTANCE_URL=Charlotte instance URL [http://charlotte:7077]: "
    if "!INSTANCE_URL!"=="" set "INSTANCE_URL=http://charlotte:7077"
) else (
    set /p "MORE=Set up another Charlotte instance? (Y/N): "
    if /i not "!MORE!"=="Y" goto :write_config
    set /p "INSTANCE_URL=Charlotte instance URL: "
    if "!INSTANCE_URL!"=="" (
        echo   No URL entered - skipping.
        goto :instance_loop
    )
)

if "!INSTANCE_URL:~-1!"=="/" set "INSTANCE_URL=!INSTANCE_URL:~0,-1!"
echo !INSTANCE_URL! | findstr /i /c:"/Browser" >nul
if errorlevel 1 set "INSTANCE_URL=!INSTANCE_URL!/Browser"

set /a COUNT+=1
set "NNN=00!COUNT!"
set "NNN=!NNN:~-3!"
if defined INSTANCES set "INSTANCES=!INSTANCES!,"
set "INSTANCES=!INSTANCES!{ "Id": "Charlotte-!NNN!", "Url": "!INSTANCE_URL!", "ServerName": "%COMPUTERNAME%", "UsesCookies": false, "Note": "" }"
goto :instance_loop

:write_config
(
echo {
echo   "Admin": {
echo     "Username": "%ADMIN_USER%",
echo     "Pass": "%ADMIN_PASS%"
echo   },
echo   "Charlotte": {
echo     "Instances": [
echo %INSTANCES%
echo     ]
echo   }
echo }
) > "%ROUTER_DIR%\config.json"
rem Router loads config.docker.json / config.docker.prod.json in containers
copy /y "%ROUTER_DIR%\config.json" "%ROUTER_DIR%\config.docker.json" >nul
copy /y "%ROUTER_DIR%\config.json" "%ROUTER_DIR%\config.docker.prod.json" >nul
copy /y "%ROUTER_DIR%\config.json" "%ROUTER_DIR%\config.prod.json" >nul
echo   Router config written to %ROUTER_DIR%\config.json
echo.

set /p "RUN_ROUTER=Run Charlotte's Web Router as a Docker container? (Y/N): "
if /i "!RUN_ROUTER!"=="Y" (
    docker compose -f "%ROOT%\docker\charlotte-router.yml" up -d
    if errorlevel 1 (echo   Failed to start charlottesweb container.) else (echo   charlottesweb container started.)
)
echo.

rem --- Collector container ---
:collector_container
set /p "RUN_COLLECTOR=Run Collector as a Docker container? (Y/N): "
if /i "!RUN_COLLECTOR!"=="Y" (
    if not exist "%ROOT%\Collector.Web.Server\Collector.Web.Server.dll" (
        echo   ERROR: %ROOT%\Collector.Web.Server\Collector.Web.Server.dll not found - run deploy.bat first.
    ) else (
        docker compose -f "%ROOT%\docker\collector.yml" up -d
        if errorlevel 1 (echo   Failed to start collector container.) else (echo   collector container started.)
    )
)

rem --- record services for reference ---
(
echo {
echo   "Services": [ "docker compose -f docker\*.yml up -d" ]
echo }
) > "%ROOT%\config.json"

echo.
echo Done! Manage containers with:
echo   docker compose -f "%ROOT%\docker\collector.yml" logs -f / stop / down
echo   docker compose -f "%ROOT%\docker\charlotte.yml" logs -f / stop / down
echo   docker compose -f "%ROOT%\docker\charlotte-router.yml" logs -f / stop / down
echo.
pause
endlocal
