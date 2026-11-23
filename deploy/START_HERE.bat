@echo off
setlocal EnableExtensions
REM ===========================================================================
REM  SatPrep - quick console run (no installation)
REM  For permanent hosting use SETUP.bat in the release folder.
REM  1) Unblock files copied from USB (prevents "Could not load assembly")
REM  2) Warn if the service is already running / port 5100 is taken
REM  3) Start SatPrep.exe in this window (close the window to stop it)
REM  ASCII only, same conventions as ShiftManager's START_HERE.bat.
REM ===========================================================================
cd /d "%~dp0"

echo [1/3] Unblocking files copied from USB...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Recurse | Unblock-File" >NUL 2>&1

echo [2/3] Checking for an installed service / port 5100...
sc query SatPrep 2>NUL | find /I "RUNNING" >NUL
if not errorlevel 1 (
    echo [INFO] The SatPrep Windows Service is already running.
    echo        Open http://localhost:5100 in a browser - no need to start it here.
    pause
    exit /b
)
netstat -ano | findstr /R /C:":5100 .*LISTENING" >NUL 2>&1
if not errorlevel 1 (
    echo [WARNING] Port 5100 is already in use. Edit "Urls" in appsettings.json to change it.
    pause
)

echo [3/3] Starting SatPrep on http://localhost:5100 ...
echo Learners on the network open: http://%COMPUTERNAME%:5100
echo Keep this window open while studying.
echo.
start "" http://localhost:5100
SatPrep.exe
pause
