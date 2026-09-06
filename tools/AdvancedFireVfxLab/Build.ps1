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
