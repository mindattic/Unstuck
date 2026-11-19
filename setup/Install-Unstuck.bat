@echo off
rem One-click installer for Unstuck. Double-click it: it downloads the latest release from GitHub and installs it
rem into C:\Apps\Unstuck (no admin, no .NET needed), adds a desktop shortcut and starts the tray app (Ctrl+Alt+Break).
rem If Install-Unstuck.ps1 sits next to this file it is used; otherwise the current one is fetched from GitHub.
title Install Unstuck
if exist "%~dp0Install-Unstuck.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Unstuck.ps1" %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "& ([scriptblock]::Create((Invoke-RestMethod 'https://raw.githubusercontent.com/mindattic/Unstuck/main/setup/Install-Unstuck.ps1'))) %*"
)
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" echo. & echo Install failed (exit code %RC%). Check your internet connection and try again.
pause
exit /b %RC%
