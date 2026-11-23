<#
.SYNOPSIS
    One-step SatPrep setup for the air-gapped Windows Server (called by SETUP.bat).

.DESCRIPTION
    Run from the "release" folder copied from USB. It:
      1. Re-joins any large files that were split for GitHub (*.001, *.002, ...)
      2. Verifies every file against SHA256SUMS.txt
      3. Extracts the SatPrep app zip
      4. Asks how to host it (unless -Mode is given):
           [1] Windows Service  - recommended, nothing else to install
           [2] IIS website      - installs IIS + the ASP.NET Core Module (bundled, offline)
      5. Runs Install-SatPrep.ps1 from the extracted app

.PARAMETER Mode
    Service or IIS. If omitted you are asked (default: Service).

.PARAMETER Port
    HTTP port (default 5100).

.PARAMETER InstallDir
    Install folder (default C:\SatPrep).
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [ValidateSet('', 'Service', 'IIS')][string]$Mode = '',
    [ValidateRange(1024, 65535)][int]$Port = 5100,
    [string]$InstallDir = 'C:\SatPrep'
)

$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot                                           # The release folder

function Write-Step([string]$Text) { Write-Host "`n### $Text" -ForegroundColor Magenta }

Write-Host '=====================================================' -ForegroundColor White
Write-Host '              SatPrep  -  quick setup' -ForegroundColor White
Write-Host '=====================================================' -ForegroundColor White

# -----------------------------------------------------------------------------
Write-Step 'A. Unblocking and re-joining files'
Get-ChildItem -LiteralPath $Here -Recurse -File | Unblock-File
foreach ($part in Get-ChildItem -LiteralPath $Here -Recurse -Filter '*.001') {
    $target = $part.FullName.Substring(0, $part.FullName.Length - 4)                   # name without .001
    if (Test-Path $target) { continue }
    $parts = Get-ChildItem -LiteralPath $part.DirectoryName -Filter ((Split-Path $target -Leaf) + '.*') |
        Where-Object { $_.Name -match '\.\d{3}$' } | Sort-Object Name
    $out = [System.IO.File]::Create($target)
    try { foreach ($p in $parts) { $in = [System.IO.File]::OpenRead($p.FullName); try { $in.CopyTo($out) } finally { $in.Dispose() } } }
    finally { $out.Dispose() }
    Write-Host "    joined $(Split-Path $target -Leaf) from $($parts.Count) parts"
}

# -----------------------------------------------------------------------------
Write-Step 'B. Verifying files'
$sums = Join-Path $Here 'SHA256SUMS.txt'
foreach ($line in Get-Content $sums) {
    if ($line -notmatch '^([0-9a-f]{64})\s+\*?(.+)$') { continue }
    $file = Join-Path $Here ($Matches[2] -replace '/', '\')
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing $($Matches[2]). Copy the whole release folder again." }
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLower() -ne $Matches[1]) { throw "$($Matches[2]) is damaged (checksum mismatch). Copy it again." }
    Write-Host "    OK  $($Matches[2])"
}

# -----------------------------------------------------------------------------
Write-Step 'C. Extracting SatPrep'
$zip = Get-ChildItem -LiteralPath $Here -Filter 'SatPrep-v*-win-x64.zip' | Sort-Object Name | Select-Object -Last 1
if (-not $zip) { throw 'SatPrep-v*-win-x64.zip not found in the release folder.' }
$work = Join-Path $env:TEMP ('SatPrep-setup-' + [IO.Path]::GetFileNameWithoutExtension($zip.Name))
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
Expand-Archive -LiteralPath $zip.FullName -DestinationPath $work
$app = Join-Path $work 'SatPrep'
Write-Host "    extracted $($zip.Name)"

# -----------------------------------------------------------------------------
if (-not $Mode) {
    Write-Step 'D. How should SatPrep be hosted?'
    Write-Host '    [1] Windows Service  (recommended - simplest, nothing extra to install)'
    Write-Host '    [2] IIS website      (installs the IIS role + ASP.NET Core Module, managed in IIS Manager)'
    $answer = Read-Host '    Choose 1 or 2 [1]'
    $Mode = if ($answer -eq '2') { 'IIS' } else { 'Service' }
}
$bundle = Get-ChildItem -LiteralPath (Join-Path $Here 'prereqs') -Filter 'dotnet-hosting-*-win.exe' -ErrorAction SilentlyContinue | Select-Object -First 1

Write-Step "E. Installing ($Mode mode)"
$installArgs = @{ Mode = $Mode; Port = $Port; InstallDir = $InstallDir }
if ($bundle) { $installArgs.HostingBundle = $bundle.FullName }
& (Join-Path $app 'Install-SatPrep.ps1') @installArgs

# Keep the management scripts next to the installed app for later use
Copy-Item (Join-Path $app '*.ps1') $InstallDir -Force
Write-Host "`nSetup finished. Management scripts (Backup / Update / Uninstall) are in $InstallDir." -ForegroundColor Green
