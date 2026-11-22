<#
.SYNOPSIS
    Installs (or upgrades) SatPrep as a Windows Service on Windows Server 2022.

.DESCRIPTION
    Run from the extracted release folder, as Administrator (INSTALL.bat does this for you).
    Steps:
      1. Verifies file checksums (DLL_CHECKSUMS.txt) and unblocks files copied from USB
      2. Stops the existing service (upgrade) and backs up the database
      3. Copies the app to the install folder, keeping satprep.db, keys, backups and uploaded packs
      4. Sets the port in appsettings.json
      5. Creates the "SatPrep" Windows Service (automatic delayed start, restart on failure,
         runs as the low-privilege virtual account NT SERVICE\SatPrep)
         -> visible in Server Manager > Tools > Services
      6. Opens the Windows Firewall for the port
      7. Starts the service, waits for /health, prints the address learners should open

    Needs only built-in Windows tools (no NSSM, no IIS, no .NET install).

.PARAMETER InstallDir
    Where to install. Default C:\SatPrep

.PARAMETER Port
    HTTP port. Default 5100 (ShiftManager uses 5000).

.PARAMETER NoFirewall
    Skip creating the firewall rule (only this PC will reach the app).

.PARAMETER SkipVerify
    Skip checksum verification.

.EXAMPLE
    .\Install-SatPrep.ps1
.EXAMPLE
    .\Install-SatPrep.ps1 -InstallDir D:\Apps\SatPrep -Port 8080
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$InstallDir = 'C:\SatPrep',
    [ValidateRange(1024, 65535)][int]$Port = 5100,
    [switch]$NoFirewall,
    [switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'
$ServiceName = 'SatPrep'                                     # Windows service name
$DisplayName = 'SatPrep - SAT practice'                      # Name shown in Services / Server Manager
$Source      = $PSScriptRoot                                 # Extracted release folder
$Exe         = Join-Path $InstallDir 'SatPrep.exe'           # Installed executable
$Account     = "NT SERVICE\$ServiceName"                     # Virtual service account (no password to manage)
$RuleName    = "SatPrep HTTP (TCP $Port)"                    # Firewall rule display name

function Write-Step([string]$Text) { Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text)   { Write-Host "    $Text" -ForegroundColor Green }
function Write-Warn([string]$Text) { Write-Host "    $Text" -ForegroundColor Yellow }

Write-Host "SatPrep installer" -ForegroundColor White
Write-Host "  Source : $Source"
Write-Host "  Target : $InstallDir"
Write-Host "  Port   : $Port"

if (-not (Test-Path (Join-Path $Source 'SatPrep.exe'))) {
    throw "SatPrep.exe not found next to this script. Run the installer from the extracted release folder."
}

# -----------------------------------------------------------------------------
Write-Step '1/7 Verifying and unblocking files'
Get-ChildItem -LiteralPath $Source -Recurse -File | Unblock-File          # Remove the USB "downloaded" mark
Write-Ok 'Files unblocked.'
$sums = Join-Path $Source 'DLL_CHECKSUMS.txt'
if (-not $SkipVerify -and (Test-Path $sums)) {
    $bad = @()
    foreach ($line in Get-Content $sums) {
        if ($line -notmatch '^([0-9a-f]{64})\s+\*?(.+)$') { continue }
        $file = Join-Path $Source ($Matches[2] -replace '^\./', '' -replace '/', '\')
        if (-not (Test-Path -LiteralPath $file)) { $bad += "missing: $($Matches[2])"; continue }
        if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLower() -ne $Matches[1]) { $bad += "changed: $($Matches[2])" }
    }
    if ($bad.Count -gt 0) { $bad | Select-Object -First 10 | ForEach-Object { Write-Warn $_ }; throw "$($bad.Count) file(s) failed verification. Re-copy the release from USB." }
    Write-Ok 'All checksums match.'
}

# -----------------------------------------------------------------------------
Write-Step '2/7 Stopping existing service and backing up data'
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -ne 'Stopped') { Stop-Service -Name $ServiceName -Force; $existing.WaitForStatus('Stopped', '00:00:30') }
    Write-Ok 'Service stopped (upgrade).'
}
$db = Join-Path $InstallDir 'satprep.db'
if (Test-Path $db) {
    $backupDir = Join-Path $InstallDir 'backups'
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    $backup = Join-Path $backupDir ("satprep-pre-install-{0:yyyyMMdd-HHmmss}.db" -f (Get-Date))
    Copy-Item $db $backup
    Write-Ok "Database backed up to $backup"
} else {
    Write-Ok 'Fresh install (no existing database).'
}

# -----------------------------------------------------------------------------
Write-Step '3/7 Copying application files'
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
# Keep learner data and local content: database, auth keys, backups, uploaded packs
$rc = robocopy $Source $InstallDir /E /NFL /NDL /NJH /NJS /NP /R:2 /W:2 `
    /XF 'satprep.db' 'satprep.db-*' 'uploaded-*.json' /XD 'keys' 'backups' 'logs'
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }
Write-Ok "Copied to $InstallDir"

# -----------------------------------------------------------------------------
Write-Step '4/7 Configuring port'
$settingsPath = Join-Path $InstallDir 'appsettings.json'
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.Urls = "http://0.0.0.0:$Port"
$settings | ConvertTo-Json -Depth 10 | Set-Content -Path $settingsPath -Encoding UTF8
Write-Ok "Listening on port $Port"

# -----------------------------------------------------------------------------
Write-Step '5/7 Registering Windows Service'
if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    New-Service -Name $ServiceName -BinaryPathName "`"$Exe`"" -DisplayName $DisplayName `
        -Description 'Offline SAT practice platform (questions, skill mastery, daily plan, practice tests).' `
        -StartupType Automatic | Out-Null
    Write-Ok 'Service created.'
} else {
    sc.exe config $ServiceName binPath= "`"$Exe`"" | Out-Null
    Write-Ok 'Service updated.'
}
sc.exe config $ServiceName start= delayed-auto obj= $Account | Out-Null           # Start after boot, low-privilege account
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/60000 | Out-Null # Auto-restart on crash
# The virtual account needs write access to the install folder (database, keys, backups)
icacls $InstallDir /grant "${Account}:(OI)(CI)M" /T /Q | Out-Null
Write-Ok "Runs as $Account, automatic (delayed) start, restarts on failure."

# -----------------------------------------------------------------------------
Write-Step '6/7 Firewall'
if ($NoFirewall) {
    Write-Warn 'Skipped (-NoFirewall): only this computer can open SatPrep.'
} else {
    Get-NetFirewallRule -DisplayName 'SatPrep HTTP*' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $RuleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow -Profile Any | Out-Null
    Write-Ok "Inbound TCP $Port allowed."
}

# -----------------------------------------------------------------------------
Write-Step '7/7 Starting SatPrep'
Start-Service -Name $ServiceName
$healthy = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:$Port/health" -UseBasicParsing -TimeoutSec 3
        if ($r.StatusCode -eq 200) { $healthy = $true; break }
    } catch { Start-Sleep -Seconds 1 }
}
if (-not $healthy) {
    Write-Warn 'SatPrep did not answer within 60 seconds. Check Event Viewer > Windows Logs > Application (source: SatPrep).'
    exit 1
}

# Desktop shortcut for everyone on this server
$shortcut = Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'SatPrep.url'
"[InternetShortcut]`r`nURL=http://localhost:$Port/`r`n" | Set-Content -Path $shortcut -Encoding ASCII

$ips = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' }).IPAddress
Write-Host "`nSatPrep is running." -ForegroundColor Green
Write-Host "  On this server : http://localhost:$Port"
Write-Host "  On the network : http://$($env:COMPUTERNAME):$Port"
foreach ($ip in $ips) { Write-Host "                   http://${ip}:$Port" }
Write-Host "`nManage it in Server Manager > Tools > Services ('$DisplayName')."
Write-Host "Back up progress with Backup-SatPrep.ps1 (or the Content page)."
