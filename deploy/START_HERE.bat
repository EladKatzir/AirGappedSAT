@echo off
setlocal EnableExtensions
REM ===========================================================================
REM  SatPrep startup - air-gapped Windows Server 2022
REM  1) Unblock files copied from USB (prevents "Could not load assembly")
REM  2) Warn if port 5100 is taken
REM  3) Start SatPrep.exe (keep this window open)
REM  ASCII only, same conventions as ShiftManager's START_HERE.bat.
REM ===========================================================================
cd /d "%~dp0"

echo [1/3] Unblocking files copied from USB...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Recurse | Unblock-File" >NUL 2>&1

echo [2/3] Checking port 5100...
netstat -ano | findstr /R /C:":5100 .*LISTENING" >NUL 2>&1
if not errorlevel 1 (
    echo [WARNING] Port 5100 is already in use. Edit "Urls" in appsettings.json to change it.
    pause
)

echo [3/3] Starting SatPrep on http://localhost:5100 ...
echo Learners on the network open: http://%COMPUTERNAME%:5100
echo.
SatPrep.exe
pause
