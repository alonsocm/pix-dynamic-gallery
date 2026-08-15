@echo off
REM Double-click entry point for the booth cabin. Forces -ExecutionPolicy Bypass so it
REM doesn't depend on whatever the "Run with PowerShell" context-menu action or the
REM machine's configured execution policy happen to do (see start-booth.ps1).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-booth.ps1"
pause
