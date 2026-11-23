@echo off
setlocal
REM ===========================================================================
REM  SatPrep quick setup - double-click me.
REM  Asks for Administrator rights, verifies the files, then installs SatPrep
REM  as a Windows Service (recommended) or an IIS website - your choice.
REM  Options pass through, e.g.:  SETUP.bat -Mode IIS -Port 8080
REM ===========================================================================
cd /d "%~dp0"

net session >NUL 2>&1
if errorlevel 1 (
    echo Requesting Administrator rights...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%*' -Verb RunAs"
    exit /b
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-SatPrep.ps1" %*
echo.
pause
