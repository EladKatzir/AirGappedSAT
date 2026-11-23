<#
.SYNOPSIS
    Backs up the SatPrep database (all learners' progress).

.DESCRIPTION
    Briefly stops the service so the copy is consistent, copies satprep.db to
    the destination (default: C:\SatPrep\backups), restarts the service and
    deletes backups older than -KeepDays.

    -Schedule registers a daily Windows scheduled task (02:30) that runs this
    script with the same options.

.EXAMPLE
    .\Backup-SatPrep.ps1
.EXAMPLE
    .\Backup-SatPrep.ps1 -Destination E:\SatPrepBackups      # e.g. a USB drive
.EXAMPLE
    .\Backup-SatPrep.ps1 -Schedule                           # daily automatic backup
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$InstallDir = 'C:\SatPrep',
    [string]$Destination,
    [ValidateRange(1, 3650)][int]$KeepDays = 30,
    [switch]$Schedule
)

$ErrorActionPreference = 'Stop'
$ServiceName = 'SatPrep'                                           # Windows service name
if (-not $Destination) { $Destination = Join-Path $InstallDir 'backups' }

if ($Schedule) {
    $script = Join-Path $InstallDir 'Backup-SatPrep.ps1'          # Installed copy of this script
    $taskArgs = "-NoProfile -ExecutionPolicy Bypass -File `"$script`" -InstallDir `"$InstallDir`" -Destination `"$Destination`" -KeepDays $KeepDays"
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $taskArgs
    $trigger = New-ScheduledTaskTrigger -Daily -At '02:30'
    Register-ScheduledTask -TaskName 'SatPrep daily backup' -Action $action -Trigger $trigger -User 'SYSTEM' -RunLevel Highest -Force | Out-Null
    Write-Host "Daily backup scheduled at 02:30 to $Destination (keeping $KeepDays days)." -ForegroundColor Green
    return
}

$db = Join-Path $InstallDir 'satprep.db'
if (-not (Test-Path $db)) { throw "No database found at $db" }
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$target = Join-Path $Destination ("satprep-{0:yyyyMMdd-HHmmss}.db" -f (Get-Date))

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$wasRunning = $svc -and $svc.Status -eq 'Running'                # Service mode
$poolRunning = $false                                           # IIS mode
if (Get-Module -ListAvailable -Name WebAdministration) {
    Import-Module WebAdministration
    $poolRunning = (Test-Path "IIS:\AppPools\$ServiceName") -and (Get-WebAppPoolState -Name $ServiceName).Value -eq 'Started'
}
try {
    if ($wasRunning) { Stop-Service -Name $ServiceName -Force; $svc.WaitForStatus('Stopped', '00:00:30') }
    if ($poolRunning) { Stop-WebAppPool -Name $ServiceName; Start-Sleep -Seconds 3 }
    Copy-Item $db $target
} finally {
    if ($wasRunning) { Start-Service -Name $ServiceName }       # Always bring the app back
    if ($poolRunning) { Start-WebAppPool -Name $ServiceName }
}
Write-Host "Backup written: $target" -ForegroundColor Green

Get-ChildItem $Destination -Filter 'satprep-*.db' | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) } | Remove-Item
