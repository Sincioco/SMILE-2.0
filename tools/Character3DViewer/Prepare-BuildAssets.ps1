[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$arinRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57'
$arinV56Root = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin'
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
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.glb'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.glb'
    },
    @{
        Source = Join-Path $arinV56Root 'ArinV56.sm3d.json'
        Destination = Join-Path $buildAssets 'ArinV56\ArinV56.sm3d.json'
    },
    @{
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.texture-00.jpg'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.texture-00.jpg'
    },
    @{
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.texture-01.jpg'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.texture-01.jpg'
    },
    @{
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.texture-02.jpg'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.texture-02.jpg'
    },
    @{
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.texture-03.jpg'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.texture-03.jpg'
    },
    @{
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.texture-04.jpg'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.texture-04.jpg'
    },
    @{
        Source = Join-Path $arinV56Root 'arin-integrated-candidate-v5.6.texture-05.jpg'
        Destination = Join-Path $buildAssets 'ArinV56\arin-integrated-candidate-v5.6.texture-05.jpg'
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

$fireAssets = Join-Path $toolRoot 'Assets\Fire'
New-Item -ItemType Directory -Path $fireAssets -Force | Out-Null
foreach ($fireFile in @('fire-shape-atlas.png', 'smoke-shape-atlas.png', 'ember-shape.png')) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "TechnicalAssets\Generation3\Fire\$fireFile") `
        -Destination (Join-Path $fireAssets $fireFile) -Force
}
