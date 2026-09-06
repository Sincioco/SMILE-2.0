[CmdletBinding()]
param(
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
$arinRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin\ArinV57'
$arinV56Root = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin'
$paladinRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Paladin'
$orinRoot = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Characters\Tank\OrinV13'
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
        Source = Join-Path $orinRoot 'orin-v1.3-animation-checkpoint.glb'
        Destination = Join-Path $buildAssets 'OrinV13\orin-v1.3-animation-checkpoint.glb'
    },
    @{
        Source = Join-Path $orinRoot 'OrinV13.sm3d.json'
        Destination = Join-Path $buildAssets 'OrinV13\OrinV13.sm3d.json'
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
        Source = Join-Path $dragonRoot 'RedDragonV11\red-dragon-v1.1-animated.glb'
        Destination = Join-Path $buildAssets 'RedDragon\red-dragon-v1.1-animated.glb'
    },
    @{
        Source = Join-Path $dragonRoot 'RedDragonV11\RedDragonV11.sm3d.json'
        Destination = Join-Path $buildAssets 'RedDragon\RedDragonV11.sm3d.json'
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
}

$audioRoots = @((Join-Path $arinRoot 'Audio'), (Join-Path $dragonRoot 'RedDragonV11\Audio'))
$fireFiles = @('fire-shape-atlas.png', 'smoke-shape-atlas.png', 'ember-shape.png')
$lightningFiles = @('lightning-ribbon.png', 'lightning-spark.png', 'thunder.wav')

foreach ($audioRoot in $audioRoots) {
    if (-not (Test-Path -LiteralPath $audioRoot -PathType Container) -or
        @(Get-ChildItem -LiteralPath $audioRoot -Filter '*.wav' -File).Count -eq 0) {
        throw "Character Viewer audio inputs are missing: $audioRoot"
    }
}

foreach ($fireFile in $fireFiles) {
    $source = Join-Path $repositoryRoot "TechnicalAssets\Generation3\Fire\$fireFile"

    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Character Viewer fire input is missing: $source"
    }
}

foreach ($lightningFile in $lightningFiles) {
    $source = Join-Path $repositoryRoot "TechnicalAssets\Generation3\Lightning\$lightningFile"

    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Character Viewer lightning input is missing: $source"
    }
}

foreach ($characterName in @('Arin', 'Orin')) {
    & {
        param($CharacterName, $Synchronizer)
        . $Synchronizer -Character $CharacterName -FunctionsOnly
        Assert-CanonicalProfileAssets
        $snapshot = Read-Snapshot $snapshotPath
        $payload = Convert-SnapshotToPayload $snapshot
        $roundTrip = Convert-PayloadToSnapshot $payload
        $appliedKeys = @($snapshot.clips | Where-Object index -ge 0 |
            ForEach-Object { $_.keyframes }).Count

        if ($roundTrip.totalKeyframes -ne $appliedKeys) {
            throw 'Packaged calibration lost resolved keyframes during serialization.'
        }
    } $characterName (Join-Path $repositoryRoot 'scripts\sync-arin-v5-7-calibration.ps1')
}

if ($ValidateOnly) {
    Write-Host 'Character Viewer build prerequisites and canonical identities passed preflight.'
    return
}

foreach ($copy in $copies) {

    New-Item -ItemType Directory -Force -Path `
        ([IO.Path]::GetDirectoryName($copy.Destination)) | Out-Null
    Copy-Item -LiteralPath $copy.Source -Destination $copy.Destination -Force
}

Write-Host "Prepared Character Viewer cooking inputs from Sin Star I: $buildAssets"

# Publish current canonical corrections as read-only first-run defaults. Reuse
# the authoritative serializer; never copy live private SMD4 envelopes or import
# a historical snapshot over a saved working copy during a build.
foreach ($characterName in @('Arin', 'Orin')) {
    & {
        param($CharacterName, $CalibrationDirectory, $Synchronizer)
        . $Synchronizer -Character $CharacterName -FunctionsOnly
        $snapshot = Read-Snapshot $snapshotPath
        $payload = Convert-SnapshotToPayload $snapshot
        $roundTrip = Convert-PayloadToSnapshot $payload
        $appliedKeys = @($snapshot.clips | Where-Object index -ge 0 |
            ForEach-Object { $_.keyframes }).Count
        if ($roundTrip.totalKeyframes -ne $appliedKeys) {
            throw 'Packaged calibration lost resolved keyframes during serialization.'
        }
        $fileName = if ($CharacterName -eq 'Arin') { 'arin-v5.7.smkf' } else { 'orin-v1.3.smkf' }
        $destination = Join-Path $CalibrationDirectory $fileName
        Write-AtomicBytes $destination $payload (Get-PathHash $destination)
        # Identity-only JSON for the shared Viewer serializer. Values are read
        # from its current saved buffer, never copied from this publication seed.
        $metadata = [ordered]@{
            schemaVersion = 2; assetId = $roundTrip.assetId
            characterVersion = $roundTrip.characterVersion; applicationId = $roundTrip.applicationId
            dataKey = $roundTrip.dataKey; storageVersion = 3; profile = $roundTrip.profile
        } | ConvertTo-Json -Depth 8 -Compress
        $metadataPath = [IO.Path]::ChangeExtension($destination, '.metadata.json')
        Write-AtomicBytes $metadataPath ([Text.Encoding]::UTF8.GetBytes($metadata)) (Get-PathHash $metadataPath)
        Write-Host "Packaged $CharacterName calibration: $appliedKeys keys; SHA-256 $(Get-PathHash $destination)"
    } $characterName (Join-Path $toolRoot 'Assets\Calibration') `
        (Join-Path $repositoryRoot 'scripts\sync-arin-v5-7-calibration.ps1')
}

$audioAssets = Join-Path $toolRoot 'Assets\Audio'
New-Item -ItemType Directory -Path $audioAssets -Force | Out-Null
foreach ($audioRoot in $audioRoots) {
    foreach ($audioFile in Get-ChildItem -LiteralPath $audioRoot -Filter '*.wav') {
        Copy-Item -LiteralPath $audioFile.FullName -Destination $audioAssets -Force
    }
}

$fireAssets = Join-Path $toolRoot 'Assets\Fire'
New-Item -ItemType Directory -Path $fireAssets -Force | Out-Null
foreach ($fireFile in $fireFiles) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "TechnicalAssets\Generation3\Fire\$fireFile") `
        -Destination (Join-Path $fireAssets $fireFile) -Force
}

$lightningAssets = Join-Path $toolRoot 'Assets\Lightning'
New-Item -ItemType Directory -Path $lightningAssets -Force | Out-Null
foreach ($lightningFile in $lightningFiles) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "TechnicalAssets\Generation3\Lightning\$lightningFile") `
        -Destination (Join-Path $lightningAssets $lightningFile) -Force
}
