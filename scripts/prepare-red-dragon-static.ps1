[CmdletBinding()]
param(
    [string]$OutputGlb,
    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$builder = Join-Path $repositoryRoot 'scripts\prepare-red-dragon-static.py'
$sourceRoot = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\RedDragon'
$sourceGlb = Join-Path $sourceRoot 'RedDragonV1.0.original.glb'

if ([string]::IsNullOrWhiteSpace($OutputGlb)) {
    $OutputGlb = Join-Path $sourceRoot 'RedDragonV1.0.static.glb'
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $sourceRoot 'RedDragonV1.0.static.json'
}

foreach ($requiredFile in @($blender, $builder, $sourceGlb)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required Red Dragon preparation input is missing: $requiredFile"
    }
}

$resolvedGlb = [IO.Path]::GetFullPath($OutputGlb)
$resolvedReport = [IO.Path]::GetFullPath($ReportPath)
New-Item -ItemType Directory -Force -Path `
    ([IO.Path]::GetDirectoryName($resolvedGlb)), `
    ([IO.Path]::GetDirectoryName($resolvedReport)) | Out-Null

& $blender --background --python $builder -- $sourceGlb $resolvedGlb $resolvedReport
if ($LASTEXITCODE -ne 0) {
    throw 'Red Dragon static asset preparation failed.'
}

Write-Host "Prepared Red Dragon static asset: $resolvedGlb"
