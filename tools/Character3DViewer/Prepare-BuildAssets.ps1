[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$arinRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57'
$paladinRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin'
$dragonRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Bosses\RedDragon'
$technicalRoot = Join-Path $repositoryRoot `
    'games\SinStarI\TechnicalAssets\Generation2'
$buildAssets = Join-Path $toolRoot 'BuildAssets'
$copies = @(
    @{
        Source = Join-Path $arinRoot 'arin-v5.7-idle-equipment-checkpoint.glb'
        Destination = Join-Path $buildAssets 'ArinV57\arin-v5.7-idle-equipment-checkpoint.glb'
    },
    @{
        Source = Join-Path $arinRoot 'ArinV57.sm3d.json'
        Destination = Join-Path $buildAssets 'ArinV57\ArinV57.sm3d.json'
    },
    @{
        Source = Join-Path $paladinRoot 'sin-star-i-character-1-paladin-tripo-v01.original.glb'
        Destination = Join-Path $buildAssets `
            'ArinPrototype\sin-star-i-character-1-paladin-tripo-v01.original.glb'
    },
    @{
        Source = Join-Path $paladinRoot 'ArinPrototype.sm3d.json'
        Destination = Join-Path $buildAssets 'ArinPrototype\ArinPrototype.sm3d.json'
    },
    @{
        Source = Join-Path $dragonRoot 'RedDragonV1.0.static.glb'
        Destination = Join-Path $buildAssets 'RedDragon\RedDragonV1.0.static.glb'
    },
    @{
        Source = Join-Path $technicalRoot 'AnimationArticulated.sm3d'
        Destination = Join-Path $toolRoot `
            'TechnicalAssets\Generation2\AnimationArticulated.sm3d'
    },
    @{
        Source = Join-Path $technicalRoot 'VfxAtlas.png'
        Destination = Join-Path $toolRoot 'TechnicalAssets\Generation2\VfxAtlas.png'
    }
)

foreach ($copy in $copies) {
    if (-not (Test-Path -LiteralPath $copy.Source -PathType Leaf)) {
        throw "Character Viewer build asset is missing: $($copy.Source)"
    }

    New-Item -ItemType Directory -Force -Path `
        ([IO.Path]::GetDirectoryName($copy.Destination)) | Out-Null
    Copy-Item -LiteralPath $copy.Source -Destination $copy.Destination -Force
}

Write-Host "Prepared Character Viewer cooking inputs from Sin Star I: $buildAssets"
