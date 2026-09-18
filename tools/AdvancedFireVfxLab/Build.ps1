[CmdletBinding()]
param(
    [string]$OutputPath,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Native', 'Web', 'All')]
    [string]$Target = 'All',
    [ValidateSet('Full', 'Low', 'Medium', 'High')]
    [string]$WebQuality = 'Full'
)

$ErrorActionPreference = 'Stop'
if ($WebQuality -ne 'Full' -and $Target -ne 'Web') {
    throw 'Optimized profiles require -Target Web; normal native/Web output is preserved.'
}
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$compiler = Join-Path $taskRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $PSScriptRoot 'AdvancedFireVfxLab.smileproj'
$outputRoot = Join-Path $PSScriptRoot "bin\$Configuration"
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Lab: $compiler"
}
# Preserve the existing explicit native-output override used by isolated builds.
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    if (-not $PSBoundParameters.ContainsKey('Target')) { $Target = 'Native' }
    if ($Target -ne 'Native') { throw '-OutputPath is a native-only override; use -Target Native.' }
} else {
    $OutputPath = Join-Path $outputRoot 'AdvancedFireVfxLab.exe'
}
$taskAssets = Join-Path $PSScriptRoot 'Assets\Fire'
New-Item -ItemType Directory -Path $taskAssets -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskRoot 'TechnicalAssets\Generation3\Fire') -Filter *.png -File |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $taskAssets -Force }
$taskBackgrounds = Join-Path $PSScriptRoot 'Assets\Backgrounds'
$modelDestination = Join-Path $PSScriptRoot 'BuildAssets'
New-Item -ItemType Directory -Path $modelDestination -Force | Out-Null
foreach ($modelFile in @('kael-v1-fire-preview.glb', 'KaelFirePreview.sm3d.json')) {
    Copy-Item -LiteralPath (Join-Path $taskRoot "games\SinStarI\SourceAssets\Characters\Kael\KaelV1\$modelFile") `
        -Destination $modelDestination -Force
}
foreach ($modelFile in @('arin-v5.7-idle-equipment-checkpoint.glb', 'ArinV57.sm3d.json')) {
    Copy-Item -LiteralPath (Join-Path $taskRoot "games\SinStarI\SourceAssets\Characters\Paladin\ArinV57\$modelFile") `
        -Destination $modelDestination -Force
}
& {
    . (Join-Path $taskRoot 'scripts\sync-arin-v5-7-calibration.ps1') -Character Arin -FunctionsOnly
    $snapshot = Read-Snapshot $snapshotPath
    $payload = Convert-SnapshotToPayload $snapshot
    $roundTrip = Convert-PayloadToSnapshot $payload
    $destination = Join-Path $PSScriptRoot 'Assets\Calibration\arin-v5.7.smkf'
    Write-AtomicBytes $destination $payload (Get-PathHash $destination)
    $metadata = [ordered]@{
        schemaVersion = 2; assetId = $roundTrip.assetId
        characterVersion = $roundTrip.characterVersion; applicationId = $roundTrip.applicationId
        dataKey = $roundTrip.dataKey; storageVersion = 3; profile = $roundTrip.profile
    } | ConvertTo-Json -Depth 8 -Compress
    $metadataPath = [IO.Path]::ChangeExtension($destination, '.metadata.json')
    Write-AtomicBytes $metadataPath ([Text.Encoding]::UTF8.GetBytes($metadata)) (Get-PathHash $metadataPath)
}
New-Item -ItemType Directory -Path $taskBackgrounds -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'games\SinStarI\Assets\Sin Star - Title Screen - Background.png') `
    -Destination (Join-Path $taskBackgrounds 'SinStarLandscape.png') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'games\SinStarI\Assets\Title Screen with Logo.png') `
    -Destination (Join-Path $taskBackgrounds 'SinStarTitleWithLogo.png') -Force
if ($Target -in @('Native', 'All')) {
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($OutputPath))) | Out-Null
    & $compiler --project $project --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $OutputPath
    if ($LASTEXITCODE -ne 0) { throw 'Advanced Fire Lab native compilation failed.' }
    Write-Host "Built Fire Lab: $OutputPath"
}
if ($Target -in @('Web', 'All')) {
    $webFolder = if ($WebQuality -eq 'Full') { 'Web' } else { "Web - Optimized $WebQuality" }
    $webOutput = Join-Path $outputRoot $webFolder
    & $compiler --project $project --target web `
        --configuration $Configuration --output-dir $webOutput --web-quality $WebQuality
    if ($LASTEXITCODE -ne 0) { throw 'Advanced Fire Lab Web compilation failed.' }
    Write-Host "Built Fire Lab Web: $webOutput"
}
