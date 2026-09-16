[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$waterRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$modelDestination = Join-Path $PSScriptRoot 'BuildAssets'
New-Item -ItemType Directory -Path $modelDestination -Force | Out-Null
$backgroundDestination = Join-Path $PSScriptRoot 'Assets\Backgrounds'
New-Item -ItemType Directory -Path $backgroundDestination -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $waterRepository 'games\SinStarI\Assets\Sin Star - Title Screen - Background.png') `
    -Destination (Join-Path $backgroundDestination 'SinStarLandscape.png') -Force
Copy-Item -LiteralPath (Join-Path $waterRepository 'games\SinStarI\Assets\Title Screen with Logo.png') `
    -Destination (Join-Path $backgroundDestination 'SinStarTitleWithLogo.png') -Force
foreach ($modelFile in @('mira-animation-checkpoint.glb', 'Mira.sm3d.json')) {
    Copy-Item -LiteralPath (Join-Path $waterRepository "games\SinStarI\SourceAssets\Characters\Healer\MiraTripoV1\$modelFile") `
        -Destination $modelDestination -Force
}
foreach ($family in @('Water', 'Lightning')) {
    $destination = Join-Path $PSScriptRoot "Assets\$family"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $waterRepository "TechnicalAssets\Generation3\$family") -File |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Force }
}
$audioDestination = Join-Path $PSScriptRoot 'Assets\Audio'
New-Item -ItemType Directory -Path $audioDestination -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $waterRepository 'games\SinStarI\SourceAssets\Characters\Healer\MiraTripoV1\Audio') -Filter '*.wav' -File |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $audioDestination -Force }
$output = Join-Path $PSScriptRoot "bin\$Configuration\WaterVfxLab.exe"
& (Join-Path $waterRepository 'artifacts\compiler\smilec.exe') --project (Join-Path $PSScriptRoot 'WaterVfxLab.smileproj') `
    --target windows-x64 --configuration $Configuration --graphics DirectX -o $output
if ($LASTEXITCODE -ne 0) { throw 'Native Water Lab compilation failed.' }
Write-Host "Built native Water Lab: $output"
