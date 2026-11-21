@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

echo.
echo ============================================
echo   Collector ^& Charlotte Setup
echo ============================================
echo.

net session >nul 2>&1
if errorlevel 1 (
    echo This setup must be run with administrator privileges.
    echo Right-click setup.bat and choose "Run as administrator".
    echo.
    pause
    exit /b 1
)

rem --- Charlotte service ---
set /p "REG_CHARLOTTE=Register Charlotte as a Windows Service? (Y/N): "
if /i "!REG_CHARLOTTE!"=="Y" (
    if not exist "%ROOT%\Charlotte\Charlotte.exe" (
        echo   ERROR: %ROOT%\Charlotte\Charlotte.exe not found.
    ) else (
        sc create Charlotte binPath= "%ROOT%\Charlotte\Charlotte.exe" DisplayName= "Charlotte" start= auto
        if errorlevel 1 (echo   Failed to register Charlotte service.) else (echo   Charlotte service registered.)
    )
    sc query Charlotte >nul 2>&1
    if not errorlevel 1 set "SVC_CHARLOTTE=1"
)
echo.

rem --- Charlotte's Web Router setup ---
set /p "SETUP_ROUTER=Set up Charlotte's Web Router? (Y/N): "
if /i not "!SETUP_ROUTER!"=="Y" goto :collector_service

set /p "ADMIN_USER=Admin account [admin]: "
if "%ADMIN_USER%"=="" set "ADMIN_USER=admin"
set /p "ADMIN_PASS=Admin password: "

set "ROUTER_DIR=%ROOT%\CharlottesWeb"
if not exist "%ROUTER_DIR%" (
    echo   ERROR: %ROUTER_DIR% not found - run deploy.bat first.
    goto :collector_service
)

set /a COUNT=0
set "INSTANCES="

:instance_loop
if %COUNT%==0 (
    set /p "INSTANCE_URL=Charlotte instance URL [http://localhost:7077]: "
    if "!INSTANCE_URL!"=="" set "INSTANCE_URL=http://localhost:7077"
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
copy /y "%ROUTER_DIR%\config.json" "%ROUTER_DIR%\config.prod.json" >nul
echo   Router config written to %ROUTER_DIR%\config.json
echo.

set /p "REG_ROUTER=Register Charlotte's Web Router as a Windows Service? (Y/N): "
if /i "!REG_ROUTER!"=="Y" (
    if not exist "%ROUTER_DIR%\Charlotte.Router.exe" (
        echo   ERROR: %ROUTER_DIR%\Charlotte.Router.exe not found.
    ) else (
        sc create CharlottesWeb binPath= "%ROUTER_DIR%\Charlotte.Router.exe" DisplayName= "Charlotte's Web Router" start= auto
        if errorlevel 1 (echo   Failed to register CharlottesWeb service.) else (echo   CharlottesWeb service registered.)
    )
    sc query CharlottesWeb >nul 2>&1
    if not errorlevel 1 set "SVC_ROUTER=1"
)
echo.

rem --- Collector service ---
:collector_service

rem --- port configuration ---
echo.
set /p "PORT_FE_HTTP=React front end HTTP port [7784]: "
if "!PORT_FE_HTTP!"=="" set "PORT_FE_HTTP=7784"
set /p "PORT_FE_HTTPS=React front end HTTPS port [7783]: "
if "!PORT_FE_HTTPS!"=="" set "PORT_FE_HTTPS=7783"
set /p "PORT_BE_HTTP=Server backend HTTP port [7780]: "
if "!PORT_BE_HTTP!"=="" set "PORT_BE_HTTP=7780"
set /p "PORT_BE_HTTPS=Server backend HTTPS port [7781]: "
if "!PORT_BE_HTTPS!"=="" set "PORT_BE_HTTPS=7781"

echo.
set /p "CONTENT_PATH=Collector content data folder (e.g. M:\Collector\Content): "
if "!CONTENT_PATH!"=="" (
    echo   No path given - keeping existing storage paths.
) else (
    if not exist "!CONTENT_PATH!" (
        echo   Creating !CONTENT_PATH! ...
        mkdir "!CONTENT_PATH!"
    )
)

echo.
echo Applying configuration (ports: fe !PORT_FE_HTTP!/!PORT_FE_HTTPS! be !PORT_BE_HTTP!/!PORT_BE_HTTPS!, content: !CONTENT_PATH!)...
if defined CONTENT_PATH (set "CP_ARG=-ContentRoot '!CONTENT_PATH!'") else set "CP_ARG="
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\update-config.ps1" -Root "%ROOT%" -FeHttp !PORT_FE_HTTP! -FeHttps !PORT_FE_HTTPS! -BeHttp !PORT_BE_HTTP! -BeHttps !PORT_BE_HTTPS! !CP_ARG!
if errorlevel 1 (echo   WARNING: config update script failed - check output above.)
echo.

set /p "REG_COLLECTOR=Register Collector as a Windows Service? (Y/N): "
if /i "!REG_COLLECTOR!"=="Y" (
    if not exist "%ROOT%\Collector.Web.Server\Collector.Web.Server.exe" (
        echo   ERROR: %ROOT%\Collector.Web.Server\Collector.Web.Server.exe not found.
    ) else (
        sc create Collector binPath= "%ROOT%\Collector.Web.Server\Collector.Web.Server.exe" DisplayName= "Collector" start= auto
        if errorlevel 1 (echo   Failed to register Collector service.) else (echo   Collector service registered.)
    )
    sc query Collector >nul 2>&1
    if not errorlevel 1 set "SVC_COLLECTOR=1"
)

rem --- record registered services for Start/StopServices.bat ---
set "SERVICES="
if defined SVC_CHARLOTTE set "SERVICES="Charlotte""
if defined SVC_ROUTER (
    if defined SERVICES (set "SERVICES=!SERVICES!, "CharlottesWeb"") else set "SERVICES="CharlottesWeb""
)
if defined SVC_COLLECTOR (
    if defined SERVICES (set "SERVICES=!SERVICES!, "Collector"") else set "SERVICES="Collector""
)
(
echo {
echo   "Services": [ %SERVICES% ]
echo }
) > "%ROOT%\config.json"

echo.
if exist "%ROOT%\StartServices.bat" call "%ROOT%\StartServices.bat"

echo.
echo Thank you for your interest in this project,
echo.
echo Sincerely,
echo Mark Entingh
echo Creator/Developer of Collector ^& Charlotte
echo.
pause
endlocal
