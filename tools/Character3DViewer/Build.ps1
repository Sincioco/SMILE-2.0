[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$project = Join-Path $toolRoot 'Character3DViewer.smileproj'
$output = Join-Path $toolRoot 'bin\Character3DViewer.exe'

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw "Build SMILE before compiling the Character Viewer/editor: $compiler"
}

& (Join-Path $toolRoot 'Prepare-BuildAssets.ps1')
New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($output)) | Out-Null
& $compiler --project $project --target windows-x64 `
    --configuration $Configuration --graphics DirectX -o $output

if ($LASTEXITCODE -ne 0) {
    throw 'Character Viewer/editor native compilation failed.'
}

Write-Host "Built Character Viewer/editor: $output"
