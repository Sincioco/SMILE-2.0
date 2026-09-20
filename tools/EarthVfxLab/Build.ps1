[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$earthRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$earthModels = Join-Path $PSScriptRoot 'BuildAssets'
$earthAssets = Join-Path $PSScriptRoot 'Assets\Earth'
$earthBackgrounds = Join-Path $PSScriptRoot 'Assets\Backgrounds'
New-Item -ItemType Directory -Path $earthModels, $earthAssets, $earthBackgrounds -Force | Out-Null
foreach ($file in @('kael-v1-animation-checkpoint.glb', 'KaelV1.sm3d.json')) {
    Copy-Item -LiteralPath (Join-Path $earthRepository "games\SinStarI\SourceAssets\Characters\Kael\KaelV1\$file") -Destination $earthModels -Force
}
Copy-Item -LiteralPath (Join-Path $earthRepository 'TechnicalAssets\Generation3\Earth\earth-rocks.glb') -Destination $earthModels -Force
Get-ChildItem -LiteralPath (Join-Path $earthRepository 'TechnicalAssets\Generation3\Earth') -File |
    Where-Object Extension -In '.png', '.wav' | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $earthAssets -Force }
& (Join-Path $earthRepository 'scripts\copy-arena-assets.ps1') -ProjectDirectory $PSScriptRoot
$earthOutput = Join-Path $PSScriptRoot "bin\$Configuration\EarthVfxLab.exe"
& (Join-Path $earthRepository 'artifacts\compiler\smilec.exe') --project (Join-Path $PSScriptRoot 'EarthVfxLab.smileproj') --target windows-x64 --configuration $Configuration --graphics DirectX -o $earthOutput
if ($LASTEXITCODE -ne 0) { throw 'Native Earth Lab compilation failed.' }
Write-Host "Built native Earth Lab: $earthOutput"
