<#
.SYNOPSIS
    Builds the air-gapped Windows release zip (Windows dev machine version).
    Same output as build/build-release.sh.

.EXAMPLE
    .\build\Build-Release.ps1 -Version 0.1.0
    -> packages\SatPrep-v0.1.0-win-x64.zip (+ .sha256)
#>
param(
    [Parameter(Mandatory = $true)][string]$Version   # Release version, e.g. 0.1.0
)

$ErrorActionPreference = 'Stop'
$Root    = Split-Path -Parent $PSScriptRoot                         # Repository root
$Project = Join-Path $Root 'src\SatPrep\SatPrep.csproj'             # Web project
$Stage   = Join-Path $Root 'build\out\SatPrep'                      # Staging folder that becomes the zip
$PkgDir  = Join-Path $Root 'packages'                               # Output folder
$Zip     = Join-Path $PkgDir "SatPrep-v$Version-win-x64.zip"        # Final zip

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Write-Host '[1/4] Running tests...'
dotnet test (Join-Path $Root 'SatPrep.sln') -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Tests failed - release aborted.' }

Write-Host '[2/4] Publishing self-contained win-x64...'
if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
New-Item -ItemType Directory -Path $Stage, $PkgDir -Force | Out-Null
dotnet publish $Project -c Release -r win-x64 --self-contained -p:Version=$Version -o $Stage --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-Host '[3/4] Adding deployment helpers...'
Copy-Item (Join-Path $Root 'deploy\*.bat'), (Join-Path $Root 'deploy\*.txt'), (Join-Path $Root 'deploy\*.ps1') $Stage
Set-Content (Join-Path $Stage 'VERSION.txt') $Version
Get-ChildItem $Stage -Recurse -Include *.dll, *.exe |
    Sort-Object FullName |
    ForEach-Object { "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.FullName.Substring($Stage.Length + 1) } |
    Set-Content (Join-Path $Stage 'DLL_CHECKSUMS.txt')

Write-Host '[4/4] Zipping...'
if (Test-Path $Zip) { Remove-Item $Zip -Force }
Compress-Archive -Path $Stage -DestinationPath $Zip
"{0}  {1}" -f (Get-FileHash $Zip -Algorithm SHA256).Hash.ToLower(), (Split-Path $Zip -Leaf) | Set-Content "$Zip.sha256"

Write-Host "Done: $Zip"
