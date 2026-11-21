@echo off
cd /d "%~dp0"
set "CFG=%~dp0config.json"

echo.
echo ============================================
echo   Starting Services
echo ============================================
echo.

if not exist "%CFG%" (
    echo config.json not found - run setup.bat first.
    echo.
    pause
    exit /b 1
)

net session >nul 2>&1
if errorlevel 1 (
    echo NOTE: Not running as Administrator - service
    echo       control requires elevated rights.
    echo.
)

findstr /i "\"Charlotte\"" "%CFG%" >nul
if not errorlevel 1 (
    echo Starting Charlotte service...
    net start Charlotte
    echo.
)

findstr /i "\"CharlottesWeb\"" "%CFG%" >nul
if not errorlevel 1 (
    echo Starting CharlottesWeb service...
    net start CharlottesWeb
    echo.
)

findstr /i "\"Collector\"" "%CFG%" >nul
if not errorlevel 1 (
    echo Starting Collector service...
    net start Collector
    echo.
)

echo Done!
echo.
pause
