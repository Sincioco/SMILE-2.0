[CmdletBinding()]
param([string]$OutputPath = (Join-Path $PSScriptRoot 'bin\Debug\AdvancedLightningVfxLab.exe'))

$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$taskAssets = Join-Path $PSScriptRoot 'Assets\Lightning'
New-Item -ItemType Directory -Path $taskAssets -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskRoot 'TechnicalAssets\Generation3\Lightning') -File |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $taskAssets -Force }
$taskBackgrounds = Join-Path $PSScriptRoot 'Assets\Backgrounds'
New-Item -ItemType Directory -Path $taskBackgrounds -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'games\SinStarI\Assets\Sin Star - Title Screen - Background.png') `
    -Destination (Join-Path $taskBackgrounds 'SinStarLandscape.png') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'games\SinStarI\Assets\Title Screen with Logo.png') `
    -Destination (Join-Path $taskBackgrounds 'SinStarTitleWithLogo.png') -Force
& (Join-Path $taskRoot 'artifacts\compiler\smilec.exe') --project (Join-Path $PSScriptRoot 'AdvancedLightningVfxLab.smileproj') `
    --target windows-x64 --configuration Debug --graphics DirectX -o $OutputPath
if ($LASTEXITCODE -ne 0) { throw 'Advanced Lightning Lab compilation failed.' }
