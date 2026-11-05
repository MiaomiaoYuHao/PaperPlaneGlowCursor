@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Current.ps1"
set "code=%errorlevel%"
echo.
if not "%code%"=="0" echo Installation failed with exit code %code%.
pause
exit /b %code%