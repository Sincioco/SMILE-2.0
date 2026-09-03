[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipDeterminism
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$builder = Join-Path $repositoryRoot 'scripts\build-arin-v5-5-candidate.ps1'
$builderPython = Join-Path $repositoryRoot 'scripts\build-arin-v5-5-candidate.py'
$buildManifest = Join-Path $repositoryRoot 'scripts\build-arin-v5-5-candidate.manifest.json'
$exporterPython = Join-Path $repositoryRoot 'scripts\export-arin-v5-4-viewer.py'
$roundTripValidator = Join-Path $repositoryRoot 'scripts\validate-arin-attachment-roundtrip.py'
$exportManifest = Join-Path $repositoryRoot 'scripts\export-arin-v5-5-viewer.manifest.json'
$sourceRoot = Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Characters\Paladin'
$bodySource = Join-Path $sourceRoot 'arin-t-pose-2k.original.glb'
$equipmentSource = Join-Path $sourceRoot 'paladin-equipment-2k.original.glb'
$candidateBlend = Join-Path $sourceRoot 'arin-integrated-candidate-v5.5.blend'
$dragonfallRoot = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\Arin'
$candidateGlb = Join-Path $dragonfallRoot 'arin-integrated-candidate-v5.5.glb'
$canonicalGlb = Join-Path $sourceRoot 'CombatLab\arin-integrated-candidate-v5.5.glb'
$descriptor = Join-Path $dragonfallRoot 'ArinV55.sm3d.json'
$committedSm3d = Join-Path $repositoryRoot `
    'games\Dragonfall\Assets\Generation2\ArinV55\ArinV55.sm3d'
$viewerProject = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewerCooked.smileproj'
$combatLabProject = Join-Path $repositoryRoot 'games\SinStarI\PaladinCombatLab.smileproj'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-Compiler([string[]]$Arguments, [string]$Failure) {
    & $compiler @Arguments
    if ($LASTEXITCODE -ne 0) { throw $Failure }
}

foreach ($required in @(
    $blender, $assetTool, $compiler, $builder, $builderPython, $buildManifest,
    $exporterPython, $roundTripValidator, $exportManifest, $bodySource, $equipmentSource,
    $candidateBlend, $candidateGlb, $canonicalGlb, $descriptor, $committedSm3d
)) {
    Assert-True (Test-Path -LiteralPath $required -PathType Leaf) `
        "Required Arin v5.5 gate input is missing: $required"
}

Assert-True ((Get-FileHash $bodySource -Algorithm SHA256).Hash -ceq
    'E6CC71A93738B350DEED3CB677EF41DDF88593E227B0759065CD35B6BB322885') `
    'The preserved Arin 2K GLB differs from the user-supplied source.'
Assert-True ((Get-FileHash $equipmentSource -Algorithm SHA256).Hash -ceq
    '9AD461C44E2C2EF173878EA223BE225EA67CEBC5E99B1201447321D45F753148') `
    'The preserved Paladin equipment 2K GLB differs from the user-supplied source.'
Assert-True ((Get-FileHash $candidateGlb -Algorithm SHA256).Hash -ceq
    (Get-FileHash $canonicalGlb -Algorithm SHA256).Hash) `
    'The Dragonfall Viewer alias and canonical Sin Star I v5.5 GLBs differ.'

$buildValue = Get-Content -LiteralPath $buildManifest -Raw | ConvertFrom-Json
$exportValue = Get-Content -LiteralPath $exportManifest -Raw | ConvertFrom-Json
Assert-True ($buildValue.assetId -ceq 'sin-star-i.character-1.paladin') `
    'The v5.5 build manifest changed stable identity.'
Assert-True ($buildValue.candidateVersion -ceq 'v5.5' -and
    $buildValue.baseCandidateVersion -ceq 'v5.4') 'The v5.5 candidate lineage changed.'
Assert-True ($buildValue.expectedBodyParts -eq 29 -and
    $buildValue.expectedBodyVertices -eq 6341 -and
    $buildValue.excludedBodyParts[0] -ceq 'tripo_part_3' -and
    $buildValue.equipmentTextureParts.ArinSwordGripGlove.vertices -eq 534 -and
    @($buildValue.removedAttachments).Count -eq 0) `
    'The v5.5 dedicated sword-grip hand contract changed.'
Assert-True ($buildValue.version -eq 2 -and
    @($buildValue.animationSources.PSObject.Properties).Count -eq 11) `
    'The fresh v5.5 Mixamo animation-source contract changed.'
foreach ($actionName in $buildValue.actions) {
    $animation = $buildValue.animationSources.$actionName
    Assert-True ($null -ne $animation) "Fresh Mixamo source is missing for $actionName."
    $animationPath = Join-Path $repositoryRoot $animation.source
    Assert-True (Test-Path -LiteralPath $animationPath -PathType Leaf) `
        "Fresh Mixamo source file is missing for $actionName."
    Assert-True ((Get-FileHash $animationPath -Algorithm SHA256).Hash -ceq
        $animation.sha256) "Fresh Mixamo source hash changed for $actionName."
    Assert-True ($animation.frames -gt 1 -and $animation.mixamoDescription.Length -gt 0) `
        "Fresh Mixamo source metadata is invalid for $actionName."
}
Assert-True ($exportValue.candidateVersion -ceq 'v5.5' -and
    $exportValue.actions.Count -eq 11) 'The v5.5 export action contract changed.'
Assert-True ($exportValue.version -eq 2 -and
    $exportValue.exportRestPositionArmature -eq $false -and
    $exportValue.attachmentCorrections.ArinSword.rotationDegrees[0] -eq 0 -and
    $exportValue.attachmentCorrections.ArinSwordGripGlove.rotationDegrees[0] -eq 0 -and
    $exportValue.attachmentCorrections.ArinShield.rotationDegrees[0] -eq 0) `
    'The v5.5 attachment export policy changed.'

$textureFiles = @(Get-ChildItem -LiteralPath $dragonfallRoot `
    -Filter 'arin-integrated-candidate-v5.5.texture-*.jpg' | Sort-Object Name)
Assert-True ($textureFiles.Count -eq 6) 'The v5.5 export must publish exactly six 2K sources.'
foreach ($texture in $textureFiles) {
    $image = [Drawing.Image]::FromFile($texture.FullName)
    try {
        Assert-True ($image.Width -eq 2048 -and $image.Height -eq 2048) `
            "The v5.5 source texture is not 2048x2048: $($texture.Name)"
        Assert-True ($image.RawFormat.Guid -eq [Drawing.Imaging.ImageFormat]::Jpeg.Guid) `
            "The v5.5 source texture codec is not the recorded JPEG source: $($texture.Name)"
    }
    finally {
        $image.Dispose()
    }
}

$temporaryRoot = Join-Path $repositoryRoot `
    ('artifacts\temp\paladin-v5-5-gate-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
try {
    $firstSm3d = Join-Path $temporaryRoot 'first.sm3d'
    $secondSm3d = Join-Path $temporaryRoot 'second.sm3d'
    & $assetTool model $candidateGlb --format-version 2 --descriptor $descriptor -o $firstSm3d
    if ($LASTEXITCODE -ne 0) { throw 'First v5.5 cook failed.' }
    & $assetTool model $candidateGlb --format-version 2 --descriptor $descriptor -o $secondSm3d
    if ($LASTEXITCODE -ne 0) { throw 'Second v5.5 cook failed.' }
    Assert-True ((Get-FileHash $firstSm3d -Algorithm SHA256).Hash -ceq
        (Get-FileHash $secondSm3d -Algorithm SHA256).Hash) `
        'Two clean v5.5 cooks were not byte-identical.'
    $committedInspection = & $assetTool inspect $committedSm3d | Out-String
    foreach ($requiredText in @(
        'Parts: 4', 'Vertices: 7376', 'Triangles: 10296', 'Materials: 2',
        'TextureReferences: 6', 'Bones: 42', 'Nodes: 46', 'Clips: 11',
        'Events: 8', 'Sockets: 10',
        'Assets/Generation2/ArinV55/Textures/ArinV55-m0-base-color-86d0e5baea69.png',
        'Assets/Generation2/ArinV55/Textures/ArinV55-m1-orm-8fa59370084a.png'
    )) {
        Assert-True ($committedInspection.Contains($requiredText)) `
            "The deployable committed v5.5 model is missing: $requiredText"
    }

    & $blender --background $candidateBlend --python $roundTripValidator -- $candidateGlb
    if ($LASTEXITCODE -ne 0) { throw 'Arin attachment round-trip validation failed.' }
    $inspection = & $assetTool inspect $firstSm3d | Out-String
    foreach ($requiredText in @(
        'Parts: 4', 'Vertices: 7376', 'Triangles: 10296', 'Materials: 2',
        'TextureReferences: 6', 'Bones: 42', 'Nodes: 46', 'Clips: 11',
        'Events: 8', 'Sockets: 10'
    )) {
        Assert-True ($inspection.Contains($requiredText)) `
            "The v5.5 cooked model is missing: $requiredText"
    }

    if (-not $SkipDeterminism) {
        $firstDirectory = Join-Path $temporaryRoot 'first'
        $secondDirectory = Join-Path $temporaryRoot 'second'
        New-Item -ItemType Directory -Path $firstDirectory, $secondDirectory | Out-Null
        $firstBlend = Join-Path $firstDirectory 'candidate.blend'
        $secondBlend = Join-Path $secondDirectory 'candidate.blend'
        & $builder -Publish -OutputBlend $firstBlend
        & $builder -Publish -OutputBlend $secondBlend
        $fileName = 'arin-integrated-candidate-v5.5.glb'
        $firstGlb = Join-Path $firstDirectory $fileName
        $secondGlb = Join-Path $secondDirectory $fileName
        & $blender --background $firstBlend --python $exporterPython -- $firstGlb $exportManifest
        if ($LASTEXITCODE -ne 0) { throw 'First rebuilt v5.5 export failed.' }
        & $blender --background $secondBlend --python $exporterPython -- $secondGlb $exportManifest
        if ($LASTEXITCODE -ne 0) { throw 'Second rebuilt v5.5 export failed.' }
        Assert-True ((Get-FileHash $firstGlb -Algorithm SHA256).Hash -ceq
            (Get-FileHash $secondGlb -Algorithm SHA256).Hash) `
            'Two independent v5.5 builds/exports were not byte-identical.'
        Assert-True ((Get-FileHash $firstGlb -Algorithm SHA256).Hash -ceq
            (Get-FileHash $candidateGlb -Algorithm SHA256).Hash) `
            'The committed v5.5 GLB differs from an independent clean rebuild.'
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

Invoke-Compiler @('--project', $viewerProject, '--target', 'windows-x64',
    '--configuration', $Configuration, '--graphics', 'DirectX', '-o',
    'artifacts\games\Character3DViewer-v5.5.exe') 'v5.5 Viewer native compilation failed.'
Invoke-Compiler @('--project', $viewerProject, '--target', 'web',
    '--configuration', $Configuration, '--output-dir',
    'artifacts\web\Character3DViewer-v5.5') 'v5.5 Viewer Web compilation failed.'
Invoke-Compiler @('--project', $combatLabProject, '--target', 'windows-x64',
    '--configuration', $Configuration, '--graphics', 'DirectX', '-o',
    'artifacts\games\PaladinCombatLab-v5.5.exe') 'v5.5 Combat Lab native compilation failed.'
Invoke-Compiler @('--project', $combatLabProject, '--target', 'web',
    '--configuration', $Configuration, '--output-dir',
    'artifacts\web\PaladinCombatLab-v5.5') 'v5.5 Combat Lab Web compilation failed.'

Write-Host ('Arin v5.5 2K source preservation, fresh Mixamo With Skin retarget, deterministic build/export/cook, ' +
    'material consolidation, Viewer, and Combat Lab gate passed; source textures remain JPEG.')
