@echo off
setlocal
REM ===========================================================================
REM  SatPrep installer launcher
REM  Installs / upgrades SatPrep as the "SatPrep" Windows Service.
REM  Asks for Administrator rights automatically.
REM  Options are passed through, e.g.:  INSTALL.bat -Port 8080
REM ===========================================================================
cd /d "%~dp0"

net session >NUL 2>&1
if errorlevel 1 (
    echo Requesting Administrator rights...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%*' -Verb RunAs"
    exit /b
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-SatPrep.ps1" %*
echo.
pause
