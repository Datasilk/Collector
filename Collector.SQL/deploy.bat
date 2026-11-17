@echo off
setlocal
cd /d "%~dp0"

set "DBNAME=%~1"
if "%DBNAME%"=="" set "DBNAME=collector"

echo Generating deployment script...

call npx gulp --database "%DBNAME%"
if %ERRORLEVEL% neq 0 (
    echo.
    echo Failed to generate deploy.sql.
    exit /b %ERRORLEVEL%
)

echo Deploying database schema...

psql -h localhost -U postgres -d template1 -a -e -f deploy.sql

if %ERRORLEVEL% neq 0 (
    echo.
    echo Deployment failed.
    exit /b %ERRORLEVEL%
)

echo.
echo Deployment completed successfully.
endlocal
