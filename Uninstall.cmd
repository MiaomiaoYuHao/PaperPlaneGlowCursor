@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Restore-Previous.ps1"
set "code=%errorlevel%"
echo.
if not "%code%"=="0" echo Uninstall failed with exit code %code%.
pause
exit /b %code%