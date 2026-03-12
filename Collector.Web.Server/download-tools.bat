@echo off
setlocal enabledelayedexpansion

echo Checking for required video processing tools...

set "TOOLS_DIR=%~dp0"

REM Check for ffmpeg
where ffmpeg >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo ffmpeg is already in PATH
) else if exist "%TOOLS_DIR%ffmpeg.exe" (
    echo ffmpeg found in application directory
) else (
    echo Downloading ffmpeg...
    powershell -Command "$ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' -OutFile '%TOOLS_DIR%ffmpeg.zip'"
    timeout /t 2 /nobreak >nul
    if exist "%TOOLS_DIR%ffmpeg.zip" (
        echo Extracting ffmpeg...
        powershell -Command "$ProgressPreference = 'SilentlyContinue'; Expand-Archive -Path '%TOOLS_DIR%ffmpeg.zip' -DestinationPath '%TOOLS_DIR%ffmpeg-temp' -Force"
        for /d %%i in ("%TOOLS_DIR%ffmpeg-temp\*") do (
            copy "%%i\bin\ffmpeg.exe" "%TOOLS_DIR%" >nul
            copy "%%i\bin\ffprobe.exe" "%TOOLS_DIR%" >nul
        )
        rmdir /s /q "%TOOLS_DIR%ffmpeg-temp"
        del "%TOOLS_DIR%ffmpeg.zip"
        echo ffmpeg installed successfully
    ) else (
        echo Failed to download ffmpeg
    )
)

REM Check for ffprobe
where ffprobe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo ffprobe is already in PATH
) else if exist "%TOOLS_DIR%ffprobe.exe" (
    echo ffprobe found in application directory
) else (
    echo ffprobe should have been installed with ffmpeg
)

REM Check for yt-dlp and always update to latest version
where yt-dlp >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo yt-dlp is already in PATH
) else (
    if exist "%TOOLS_DIR%yt-dlp.exe" (
        echo Updating yt-dlp to latest version...
        "%TOOLS_DIR%yt-dlp.exe" --update
        if %ERRORLEVEL% EQU 0 (
            echo yt-dlp updated successfully
        ) else (
            echo yt-dlp update failed, downloading fresh copy...
            powershell -Command "$ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -OutFile '%TOOLS_DIR%yt-dlp.exe'"
        )
    ) else (
        echo Downloading yt-dlp...
        powershell -Command "$ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -OutFile '%TOOLS_DIR%yt-dlp.exe'"
        if exist "%TOOLS_DIR%yt-dlp.exe" (
            echo yt-dlp installed successfully
        ) else (
            echo Failed to download yt-dlp
        )
    )
)

REM Add tools directory to PATH for this session
set "PATH=%TOOLS_DIR%;%PATH%"

echo.
echo Tool check complete!
echo Tools location: %TOOLS_DIR%
echo.

endlocal
