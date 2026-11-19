@echo off
rem Installs (or updates) Unstuck into C:\Apps\Unstuck. This does NOT fix anything by itself - it
rem copies unstuck.ps1 there, builds the UnstuckTaskbar tray app (needs the .NET 10 SDK) and writes
rem C:\Apps\Unstuck\run.bat. To actually unstick the desktop, run unstuck.bat (or press the tray hotkey).
rem
rem   install.bat            install, or update only what changed
rem   install.bat -Force     rebuild and recopy everything
title Install Unstuck
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" %*
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" echo. & echo Install finished with errors (exit code %RC%).
if "%~1"=="" pause
exit /b %RC%
