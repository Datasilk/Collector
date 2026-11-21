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
    for /f "tokens=1* delims=:" %%a in ('findstr /i "\"DeployPath\"" "%DEPLOY_CFG%"') do (
        set "SAVED_PATH=%%b"
    )
    for /f "tokens=1* delims=:" %%a in ('findstr /i "\"Config\"" "%DEPLOY_CFG%"') do (
        set "SAVED_CONFIG=%%b"
    )
    if defined SAVED_PATH (
        set "SAVED_PATH=!SAVED_PATH:~2!"
        set "SAVED_PATH=!SAVED_PATH:"=!"
        set "SAVED_PATH=!SAVED_PATH:,=!"
        set "SAVED_PATH=!SAVED_PATH:\\=\!"
    )
    if defined SAVED_CONFIG (
        set "SAVED_CONFIG=!SAVED_CONFIG:~2!"
        set "SAVED_CONFIG=!SAVED_CONFIG:"=!"
        set "SAVED_CONFIG=!SAVED_CONFIG:,=!"
    )
)

if defined SAVED_PATH (
    set /p "DEPLOY_PATH=Deploy path [!SAVED_PATH!]: "
    if "!DEPLOY_PATH!"=="" set "DEPLOY_PATH=!SAVED_PATH!"
) else (
    set /p "DEPLOY_PATH=Deploy path (e.g. M:\Collector): "
)
if "!DEPLOY_PATH!"=="" (
    echo No path given - aborting.
    exit /b 1
)
if "!DEPLOY_PATH:~-1!"=="\" set "DEPLOY_PATH=!DEPLOY_PATH:~0,-1!"

echo.
set "CFG_HINT=D"
if /i "!SAVED_CONFIG!"=="Release" set "CFG_HINT=P"
set /p "CFG_CHOICE=Deploy (D)evelopment or (P)roduction? [!CFG_HINT!]: "
if "!CFG_CHOICE!"=="" set "CFG_CHOICE=!CFG_HINT!"
if /i "!CFG_CHOICE!"=="P" (
    set "CONFIG=Release"
) else (
    set "CONFIG=Debug"
)

echo.
echo Deploying %CONFIG% to %DEPLOY_PATH%
echo.

echo [1/3] Publishing Collector.Web.Server (%CONFIG%)...
echo        (builds Collector.Web.Client into wwwroot automatically)
dotnet publish Collector.Web.Server\Collector.Web.Server.csproj -c %CONFIG% -r win-x64 --self-contained false -o "%DEPLOY_PATH%\Collector.Web.Server"
if errorlevel 1 goto :fail

echo.
echo [2/3] Publishing Charlotte (%CONFIG%)...
dotnet publish Charlotte\Charlotte\Charlotte.csproj -c %CONFIG% -r win-x64 --self-contained false -o "%DEPLOY_PATH%\Charlotte"
if errorlevel 1 goto :fail

echo.
echo [3/3] Publishing Charlotte Router (%CONFIG%)...
dotnet publish Charlotte\Router\Charlotte.Router.csproj -c %CONFIG% -r win-x64 --self-contained false -o "%DEPLOY_PATH%\CharlottesWeb"
if errorlevel 1 goto :fail

echo.
echo Copying setup scripts to %DEPLOY_PATH%...
copy /y "%~dp0Deploy\setup.bat" "%DEPLOY_PATH%\setup.bat" >nul
copy /y "%~dp0Deploy\StartServices.bat" "%DEPLOY_PATH%\StartServices.bat" >nul
copy /y "%~dp0Deploy\StopServices.bat" "%DEPLOY_PATH%\StopServices.bat" >nul
if errorlevel 1 goto :fail

rem --- remember these answers for the next run ---
set "JSON_PATH=!DEPLOY_PATH:\=\\!"
(
echo {
echo   "DeployPath": "!JSON_PATH!",
echo   "Config": "!CONFIG!"
echo }
) > "%~dp0deploy.json"

echo.
echo Done!
exit /b 0

:fail
echo.
echo *** Deployment failed - see output above. ***
exit /b 1
