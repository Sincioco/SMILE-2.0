[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('Native', 'Web', 'All')]
    [string]$Target = 'All'
)

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $toolRoot 'Character3DViewer.smileproj'
$outputRoot = Join-Path $toolRoot "bin\$Configuration"

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Character Viewer/editor: $compiler"
}

& (Join-Path $toolRoot 'Prepare-BuildAssets.ps1')
if ($Target -in @('Native', 'All')) {
    $output = Join-Path $outputRoot 'Character3DViewer.exe'
    New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
    & $compiler --project $project --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $output
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor native compilation failed.'
    }
    Write-Host "Built Character Viewer/editor: $output"
}

if ($Target -in @('Web', 'All')) {
    $webOutput = Join-Path $outputRoot 'Web'
    & $compiler --project $project --target web `
        --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer/editor Web compilation failed.'
    }
    Write-Host "Built Character Viewer/editor Web: $webOutput"
}
