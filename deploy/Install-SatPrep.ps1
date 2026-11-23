<#
.SYNOPSIS
    Installs (or upgrades) SatPrep on Windows Server 2022, either as a Windows
    Service (default) or as an IIS website.

.DESCRIPTION
    Run from the extracted app folder as Administrator. SETUP.bat does all of
    this for you with one double-click.

    Common steps:
      1. Verifies file checksums (DLL_CHECKSUMS.txt) and unblocks files copied from USB
      2. Stops the running app (upgrade) and backs up the database
      3. Copies the app to the install folder, keeping satprep.db, keys, backups and uploaded packs
      4. Sets the port
    -Mode Service (recommended, nothing extra to install):
      5. Registers the "SatPrep" Windows Service (delayed automatic start, restart on failure,
         low-privilege virtual account NT SERVICE\SatPrep), visible in Server Manager > Tools > Services
    -Mode IIS (the "Installing IIS" approach from the Windows Server 2022 guide):
      5. Installs the IIS role (Web Server + IIS Manager) from the Windows component store (offline)
         and the ASP.NET Core Module from the bundled Hosting Bundle, then creates the "SatPrep"
         application pool (No Managed Code, always running) and website bound to the port,
         visible in Server Manager > Tools > Internet Information Services (IIS) Manager
    Then:
      6. Opens the Windows Firewall for the port
      7. Starts SatPrep, waits for /health, and prints the address learners should open

.PARAMETER Mode
    Service (default) or IIS.

.PARAMETER InstallDir
    Where to install. Default C:\SatPrep

.PARAMETER Port
    HTTP port. Default 5100 (ShiftManager uses 5000).

.PARAMETER HostingBundle
    Path to dotnet-hosting-8.x-win.exe (IIS mode only). SETUP.bat passes the bundled copy.

.PARAMETER NoFirewall
    Skip creating the firewall rule (only this PC will reach the app).

.PARAMETER SkipVerify
    Skip checksum verification.

.EXAMPLE
    .\Install-SatPrep.ps1
.EXAMPLE
    .\Install-SatPrep.ps1 -Mode IIS -HostingBundle ..\prereqs\dotnet-hosting-8.0.34-win.exe
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [ValidateSet('Service', 'IIS')][string]$Mode = 'Service',
    [string]$InstallDir = 'C:\SatPrep',
    [ValidateRange(1024, 65535)][int]$Port = 5100,
    [string]$HostingBundle,
    [switch]$NoFirewall,
    [switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'
$Name        = 'SatPrep'                                     # Service / IIS site / app pool name
$DisplayName = 'SatPrep - SAT practice'                      # Name shown in Services
$Source      = $PSScriptRoot                                 # Extracted app folder
$Exe         = Join-Path $InstallDir 'SatPrep.exe'           # Installed executable
$RuleName    = "SatPrep HTTP (TCP $Port)"                    # Firewall rule display name

function Write-Step([string]$Text) { Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text)   { Write-Host "    $Text" -ForegroundColor Green }
function Write-Warn([string]$Text) { Write-Host "    $Text" -ForegroundColor Yellow }

Write-Host "SatPrep installer ($Mode mode)" -ForegroundColor White
Write-Host "  Source : $Source"
Write-Host "  Target : $InstallDir"
Write-Host "  Port   : $Port"

if (-not (Test-Path (Join-Path $Source 'SatPrep.exe'))) {
    throw "SatPrep.exe not found next to this script. Run the installer from the extracted app folder."
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
Write-Step '2/7 Stopping the running app and backing up data'
$svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -ne 'Stopped') { Stop-Service -Name $Name -Force; $svc.WaitForStatus('Stopped', '00:00:30'); Write-Ok 'Service stopped.' }
$iisInstalled = [bool](Get-Module -ListAvailable -Name WebAdministration)
if ($iisInstalled) {
    Import-Module WebAdministration
    if (Test-Path "IIS:\AppPools\$Name") {
        if ((Get-WebAppPoolState -Name $Name).Value -ne 'Stopped') { Stop-WebAppPool -Name $Name }
        Start-Sleep -Seconds 3                                          # Let the worker process release the files
        Write-Ok 'IIS application pool stopped.'
    }
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
robocopy $Source $InstallDir /E /NFL /NDL /NJH /NJS /NP /R:3 /W:3 `
    /XF 'satprep.db' 'satprep.db-*' 'uploaded-*.json' /XD 'keys' 'backups' 'logs' | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }
Write-Ok "Copied to $InstallDir"

# -----------------------------------------------------------------------------
Write-Step '4/7 Configuring port'
$settingsPath = Join-Path $InstallDir 'appsettings.json'
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.Urls = "http://0.0.0.0:$Port"                                     # Used in Service mode (IIS uses its binding)
$settings | ConvertTo-Json -Depth 10 | Set-Content -Path $settingsPath -Encoding UTF8
Write-Ok "Port $Port"

# -----------------------------------------------------------------------------
if ($Mode -eq 'Service') {
    Write-Step '5/7 Registering the Windows Service'
    if ($iisInstalled -and (Test-Path "IIS:\Sites\$Name")) {                 # Switching from IIS mode: remove the site
        Remove-Website -Name $Name; Remove-WebAppPool -Name $Name
        Write-Ok 'Removed the previous IIS site (switching to Service mode).'
    }
    $account = "NT SERVICE\$Name"                                            # Virtual service account (no password)
    if (-not (Get-Service -Name $Name -ErrorAction SilentlyContinue)) {
        New-Service -Name $Name -BinaryPathName "`"$Exe`"" -DisplayName $DisplayName `
            -Description 'Offline SAT practice platform (questions, skill mastery, daily plan, practice tests).' `
            -StartupType Automatic | Out-Null
        Write-Ok 'Service created.'
    } else {
        sc.exe config $Name binPath= "`"$Exe`"" | Out-Null
        Write-Ok 'Service updated.'
    }
    sc.exe config $Name start= delayed-auto obj= $account | Out-Null
    sc.exe failure $Name reset= 86400 actions= restart/5000/restart/10000/restart/60000 | Out-Null
    icacls $InstallDir /grant "${account}:(OI)(CI)M" /T /Q | Out-Null
    Write-Ok "Runs as $account, automatic (delayed) start, restarts on failure."
}
else {
    Write-Step '5/7 Setting up IIS'
    # 5a. IIS role (offline: comes from the Windows component store)
    if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
        $r = Install-WindowsFeature -Name Web-Server, Web-Mgmt-Console, Web-AppInit -ErrorAction Stop
        if ($r.RestartNeeded -eq 'Yes') { Write-Warn 'Windows reports a restart is needed to finish installing IIS. Restart, then run SETUP again.' }
    } else {
        Enable-WindowsOptionalFeature -Online -All -NoRestart -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-ManagementConsole, IIS-ApplicationInit | Out-Null
    }
    Write-Ok 'IIS role installed (Server Manager > Tools > IIS Manager).'

    # 5b. ASP.NET Core Module (lets IIS run SatPrep.exe), from the bundled Hosting Bundle
    $ancm = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
    if (-not (Test-Path $ancm)) {
        if (-not $HostingBundle -or -not (Test-Path $HostingBundle)) {
            throw 'The ASP.NET Core Hosting Bundle is needed for IIS mode. Run SETUP.bat (it passes the bundled copy) or use -HostingBundle <path>.'
        }
        Write-Ok 'Installing the ASP.NET Core Module (Hosting Bundle, offline)...'
        $p = Start-Process -FilePath $HostingBundle -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
        if ($p.ExitCode -notin 0, 3010) { throw "Hosting Bundle installer failed with exit code $($p.ExitCode)." }
        net stop was /y | Out-Null; net start w3svc | Out-Null                # Reload IIS so it sees the module
    }
    Write-Ok 'ASP.NET Core Module present.'

    # 5c. Remove the Windows Service if SatPrep previously ran in Service mode
    if (Get-Service -Name $Name -ErrorAction SilentlyContinue) { sc.exe delete $Name | Out-Null; Write-Ok 'Removed the previous Windows Service (switching to IIS mode).' }

    # 5d. Application pool + website
    Import-Module WebAdministration
    if (-not (Test-Path "IIS:\AppPools\$Name")) { New-WebAppPool -Name $Name | Out-Null }
    $pool = "IIS:\AppPools\$Name"
    Set-ItemProperty $pool -Name managedRuntimeVersion -Value ''                 # "No Managed Code" (ASP.NET Core runs its own runtime)
    Set-ItemProperty $pool -Name startMode -Value 'AlwaysRunning'                # Start with IIS, not on first request
    Set-ItemProperty $pool -Name processModel.idleTimeout -Value ([TimeSpan]::Zero) # Never shut down when idle
    Set-ItemProperty $pool -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero) # No surprise recycles mid-test
    if (Test-Path "IIS:\Sites\$Name") {
        Set-ItemProperty "IIS:\Sites\$Name" -Name physicalPath -Value $InstallDir
        Set-ItemProperty "IIS:\Sites\$Name" -Name applicationPool -Value $Name
        Get-WebBinding -Name $Name | Remove-WebBinding
        New-WebBinding -Name $Name -Protocol http -Port $Port -IPAddress '*'
    } else {
        New-Website -Name $Name -PhysicalPath $InstallDir -ApplicationPool $Name -Port $Port -IPAddress '*' | Out-Null
    }
    Set-ItemProperty "IIS:\Sites\$Name" -Name applicationDefaults.preloadEnabled -Value $true
    icacls $InstallDir /grant "IIS AppPool\${Name}:(OI)(CI)M" /T /Q | Out-Null   # Pool identity writes the database
    Write-Ok "IIS site '$Name' on port $Port, app pool '$Name' (No Managed Code, always running)."
}

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
if ($Mode -eq 'Service') { Start-Service -Name $Name }
else { Start-WebAppPool -Name $Name; Start-Website -Name $Name }
$healthy = $false
for ($i = 0; $i -lt 90; $i++) {
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:$Port/health" -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -eq 200) { $healthy = $true; break }
    } catch { Start-Sleep -Seconds 1 }
}
if (-not $healthy) {
    Write-Warn 'SatPrep did not answer within 90 seconds. The first start imports ~4,000 questions and can be slow; wait a minute and open the address below.'
    Write-Warn 'If it still fails, check Event Viewer > Windows Logs > Application.'
}

# Desktop shortcut for everyone on this server
$shortcut = Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'SatPrep.url'
"[InternetShortcut]`r`nURL=http://localhost:$Port/`r`n" | Set-Content -Path $shortcut -Encoding ASCII

$ips = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' }).IPAddress
Write-Host "`nSatPrep is installed ($Mode mode)." -ForegroundColor Green
Write-Host "  On this server : http://localhost:$Port"
Write-Host "  On the network : http://$($env:COMPUTERNAME):$Port"
foreach ($ip in $ips) { Write-Host "                   http://${ip}:$Port" }
if ($Mode -eq 'Service') { Write-Host "`nManage it in Server Manager > Tools > Services ('$DisplayName')." }
else { Write-Host "`nManage it in Server Manager > Tools > Internet Information Services (IIS) Manager > Sites > $Name." }
Write-Host 'Back up progress with Backup-SatPrep.ps1 (or the Content page).'
