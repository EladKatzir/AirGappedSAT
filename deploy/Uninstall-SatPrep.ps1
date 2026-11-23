<#
.SYNOPSIS
    Removes SatPrep (Windows Service or IIS site) and its firewall rule.

.DESCRIPTION
    By default the install folder (with satprep.db and backups) is KEPT so no
    learner progress is lost. Use -RemoveFiles to delete everything except a
    final database backup copied to your Documents folder.

.EXAMPLE
    .\Uninstall-SatPrep.ps1
.EXAMPLE
    .\Uninstall-SatPrep.ps1 -RemoveFiles
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$InstallDir = 'C:\SatPrep',
    [switch]$RemoveFiles
)

$ErrorActionPreference = 'Stop'
$ServiceName = 'SatPrep'                                      # Windows service name

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc) {
    if ($svc.Status -ne 'Stopped') { Stop-Service -Name $ServiceName -Force; $svc.WaitForStatus('Stopped', '00:00:30') }
    sc.exe delete $ServiceName | Out-Null
    Write-Host 'Service removed.' -ForegroundColor Green
} else {
    Write-Host 'Service not found (already removed).'
}

if (Get-Module -ListAvailable -Name WebAdministration) {           # IIS mode: remove site and app pool
    Import-Module WebAdministration
    if (Test-Path 'IIS:\Sites\SatPrep') { Remove-Website -Name 'SatPrep'; Write-Host 'IIS site removed.' -ForegroundColor Green }
    if (Test-Path 'IIS:\AppPools\SatPrep') { Remove-WebAppPool -Name 'SatPrep'; Write-Host 'IIS app pool removed.' -ForegroundColor Green }
}

Get-NetFirewallRule -DisplayName 'SatPrep HTTP*' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Write-Host 'Firewall rule removed.' -ForegroundColor Green

$shortcut = Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'SatPrep.url'
if (Test-Path $shortcut) { Remove-Item $shortcut }

if ($RemoveFiles -and (Test-Path $InstallDir)) {
    $db = Join-Path $InstallDir 'satprep.db'
    if (Test-Path $db) {
        $keep = Join-Path ([Environment]::GetFolderPath('MyDocuments')) ("satprep-final-{0:yyyyMMdd-HHmmss}.db" -f (Get-Date))
        Copy-Item $db $keep
        Write-Host "Final database copy saved to $keep" -ForegroundColor Yellow
    }
    Remove-Item -LiteralPath $InstallDir -Recurse -Force
    Write-Host "Removed $InstallDir" -ForegroundColor Green
} else {
    Write-Host "Files kept in $InstallDir (database and backups preserved)."
}
