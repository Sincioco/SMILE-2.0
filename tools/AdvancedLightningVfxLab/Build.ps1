[CmdletBinding()]
param(
    [string]$OutputPath,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Native', 'Web', 'All')]
    [string]$Target = 'All'
)

$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$compiler = Join-Path $taskRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $PSScriptRoot 'AdvancedLightningVfxLab.smileproj'
$outputRoot = Join-Path $PSScriptRoot "bin\$Configuration"
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Lab: $compiler"
}
# Preserve the existing explicit native-output override used by isolated builds.
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    if (-not $PSBoundParameters.ContainsKey('Target')) { $Target = 'Native' }
    if ($Target -ne 'Native') { throw '-OutputPath is a native-only override; use -Target Native.' }
} else {
    $OutputPath = Join-Path $outputRoot 'AdvancedLightningVfxLab.exe'
}
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
if ($Target -in @('Native', 'All')) {
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($OutputPath))) | Out-Null
    & $compiler --project $project --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $OutputPath
    if ($LASTEXITCODE -ne 0) { throw 'Advanced Lightning Lab native compilation failed.' }
    Write-Host "Built Lightning Lab: $OutputPath"
}
if ($Target -in @('Web', 'All')) {
    $webOutput = Join-Path $outputRoot 'Web'
    & $compiler --project $project --target web `
        --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Advanced Lightning Lab Web compilation failed.' }
    Write-Host "Built Lightning Lab Web: $webOutput"
}
