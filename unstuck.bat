@echo off
rem Unstuck - recover a desktop that stopped responding to clicks. Double-click this, or run it from cmd.
rem It runs the recovery right now; nothing to install.
rem
rem   unstuck.bat             default recovery: release stuck keys/buttons, kill hung or runaway apps
rem   unstuck.bat check       report only, change nothing
rem   unstuck.bat explorer    also force-restart Explorer
rem   unstuck.bat hooks       also restart PowerToys (its mouse hooks can eat clicks)
rem   unstuck.bat overlays    also close overlay apps (Overwolf, Game Bar, NVIDIA, Steam, Discord)
rem   unstuck.bat all         overlays + explorer + hooks
rem
rem Options combine, e.g.  unstuck.bat overlays explorer
title Unstuck
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0unstuck.ps1" %*
set "RC=%ERRORLEVEL%"
rem Started by double-click (no arguments): keep the window open so the report can be read.
if "%~1"=="" pause
exit /b %RC%
