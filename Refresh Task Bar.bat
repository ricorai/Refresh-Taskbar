@echo off
rem Refresh the Windows taskbar and desktop shell.
taskkill /f /im explorer.exe >nul 2>&1
timeout /t 2 /nobreak >nul
start "" explorer.exe
exit
