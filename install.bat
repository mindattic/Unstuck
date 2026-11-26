@echo off
rem Installs (or updates) Unstuck into R:\MindAttic\Apps\Unstuck\Debug and \Release: run.ps1's 1) Redeploy. This does
rem NOT fix anything by itself - it builds the UnstuckTaskbar tray app (needs the .NET 10 SDK) and puts unstuck.ps1,
rem unstuck.bat, logo.ico and commands\ beside it. To actually unstick the desktop, run unstuck.bat (or press the tray
rem hotkey). run.bat opens the full menu (Redeploy, Run Debug, Run Release).
title Install Unstuck
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" -Redeploy %*
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" echo. & echo Install finished with errors (exit code %RC%).
if "%~1"=="" pause
exit /b %RC%
