[CmdletBinding()]
param(
    [string]$OutputBlend,
    [string]$OutputGlb
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$builder = Join-Path $repositoryRoot 'scripts\build-arin-v5-7-idle-checkpoint.py'
$sourceRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57'
$skinnedFbx = Join-Path $sourceRoot `
    'arin-v5.7-mixamo-sword-and-shield-idle-with-skin.fbx'
$tPoseFbx = Join-Path $sourceRoot 'arin-v5.7-mixamo-rigged-t-pose.fbx'
$animationManifest = Join-Path $sourceRoot 'arin-v5.7-animation-set.json'
$cleanGlb = Join-Path $sourceRoot 'arin-v5.7-no-equipment.cleaned.glb'
$equippedGlb = Join-Path $sourceRoot `
    'arin-v5.7-with-sword-and-shield.original.glb'
$canonicalGlb = Join-Path $sourceRoot `
    'arin-v5.7-idle-equipment-checkpoint.glb'

if ([string]::IsNullOrWhiteSpace($OutputBlend)) {
    $OutputBlend = Join-Path $repositoryRoot `
        'artifacts\temp\arin-v5.7-idle-equipment-checkpoint.blend'
}

if ([string]::IsNullOrWhiteSpace($OutputGlb)) {
    $OutputGlb = $canonicalGlb
}

foreach ($requiredFile in @(
    $blender,
    $builder,
    $skinnedFbx,
    $tPoseFbx,
    $animationManifest,
    $cleanGlb,
    $equippedGlb
)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required Arin v5.7 build input is missing: $requiredFile"
    }
}

$resolvedBlend = [IO.Path]::GetFullPath($OutputBlend)
$resolvedGlb = [IO.Path]::GetFullPath($OutputGlb)
New-Item -ItemType Directory -Force -Path `
    ([IO.Path]::GetDirectoryName($resolvedBlend)), `
    ([IO.Path]::GetDirectoryName($resolvedGlb)) | Out-Null

& $blender --background --python-exit-code 1 --python $builder -- `
    $skinnedFbx $tPoseFbx $animationManifest $cleanGlb $equippedGlb `
    $resolvedBlend $resolvedGlb
if ($LASTEXITCODE -ne 0) {
    throw 'Arin v5.7 animation checkpoint build failed.'
}

Write-Host "Built Arin v5.7 animation checkpoint: $resolvedGlb"
