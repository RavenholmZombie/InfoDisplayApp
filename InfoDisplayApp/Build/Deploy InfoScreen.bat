@echo off
setlocal

:: Check for Administrator privileges.
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting Administrator privileges...
    powershell.exe -NoProfile -Command ^
        "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: We are now elevated.
cd /d "%~dp0"

title InfoScreen Deployment
echo ==========================================
echo          InfoScreen Deployment
echo ==========================================
echo.

powershell.exe -NoProfile -ExecutionPolicy RemoteSigned ^
    -File "%~dp0Deploy InfoScreen.ps1"

set "DEPLOY_EXIT=%ERRORLEVEL%"

echo.
echo ==========================================

if "%DEPLOY_EXIT%"=="0" (
    echo Deployment finished.
) else (
    echo Deployment exited with code %DEPLOY_EXIT%.
)

echo ==========================================
echo.
pause

exit /b %DEPLOY_EXIT%