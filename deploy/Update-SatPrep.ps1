<#
.SYNOPSIS
    Upgrades an existing SatPrep installation to the release in this folder.

.DESCRIPTION
    Thin wrapper around Install-SatPrep.ps1: checks that SatPrep is installed,
    reuses its port, then runs the installer (which backs up the database,
    replaces the program files, keeps all learner data and uploaded packs,
    applies database upgrades automatically and restarts the service).

.EXAMPLE
    .\Update-SatPrep.ps1
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param([string]$InstallDir = 'C:\SatPrep')

$ErrorActionPreference = 'Stop'
$mode = 'Service'                                                # Keep the current hosting mode
if (-not (Get-Service -Name 'SatPrep' -ErrorAction SilentlyContinue)) {
    $iis = (Get-Module -ListAvailable -Name WebAdministration) -and (Import-Module WebAdministration -PassThru) -and (Test-Path 'IIS:\Sites\SatPrep')
    if (-not $iis) { throw "SatPrep is not installed. Run SETUP.bat for a first-time installation." }
    $mode = 'IIS'
}
$port = 5100                                                     # Default if the setting cannot be read
$settings = Join-Path $InstallDir 'appsettings.json'
if (Test-Path $settings) {
    $urls = (Get-Content $settings -Raw | ConvertFrom-Json).Urls
    if ($urls -match ':(\d+)') { $port = [int]$Matches[1] }      # Keep the current port
}
& (Join-Path $PSScriptRoot 'Install-SatPrep.ps1') -Mode $mode -InstallDir $InstallDir -Port $port
