[CmdletBinding()]
param(
    [switch]$Check,
    [ValidateRange(0, 7)]
    [int]$FailAfterPublication = 0
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourcePath = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\sin-star-i-character-1-paladin-tripo-v01.original.glb'
$descriptorPath = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\Arin\ArinPrototype.sm3d.json'
$preparedRoot = Join-Path $repositoryRoot 'games\Dragonfall\SourceAssets\Arin\Prepared'
$runtimeRoot = Join-Path $repositoryRoot 'games\Dragonfall\Assets\Generation2\Arin'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\dragonfall-arin-prototype-preparation'
$expectedSourceHash = '0B75E3664FC2743637C9E75E86A55EBDFB8D4A4E3740AC06E593ADE1588013F6'
$preparationVersion = 2
$maximumImageDimension = 4096
$maximumImagePixels = 16777216

function Write-Png(
    [byte[]]$Bytes,
    [string]$Path,
    [bool]$OpaqueRed,
    [string]$Semantic
) {
    if ($Bytes.Length -lt 4 -or $Bytes[0] -ne 0xFF -or $Bytes[1] -ne 0xD8 -or
        $Bytes[$Bytes.Length - 2] -ne 0xFF -or $Bytes[$Bytes.Length - 1] -ne 0xD9) {
        throw "Arin $Semantic texture bytes do not have a complete JPEG signature."
    }

    $stream = [System.IO.MemoryStream]::new($Bytes, $false)
    $source = [System.Drawing.Bitmap]::FromStream($stream)
    try {
        if ($source.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid) {
            throw "Arin $Semantic texture MIME and decoded format disagree."
        }
        if ($source.Width -le 0 -or $source.Height -le 0 -or
            $source.Width -gt $maximumImageDimension -or
            $source.Height -gt $maximumImageDimension -or
            ([long]$source.Width * [long]$source.Height) -gt $maximumImagePixels) {
            throw "Arin $Semantic texture dimensions are outside the bounded image profile."
        }

        $bitmap = [System.Drawing.Bitmap]::new(
            $source.Width,
            $source.Height,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
        )
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.DrawImageUnscaled($source, 0, 0)
            }
            finally {
                $graphics.Dispose()
            }

            if ($OpaqueRed) {
                $rectangle = [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height)
                $data = $bitmap.LockBits(
                    $rectangle,
                    [System.Drawing.Imaging.ImageLockMode]::ReadWrite,
                    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
                )
                try {
                    $rowBytes = [Math]::Abs($data.Stride)
                    $pixels = [byte[]]::new($rowBytes)

                    for ($y = 0; $y -lt $data.Height; $y++) {
                        $rowPointer = [IntPtr]::Add($data.Scan0, $y * $data.Stride)
                        [System.Runtime.InteropServices.Marshal]::Copy(
                            $rowPointer,
                            $pixels,
                            0,
                            $rowBytes
                        )

                        for ($x = 0; $x -lt $data.Width; $x++) {
                            $pixels[$x * 4 + 2] = 255
                        }

                        [System.Runtime.InteropServices.Marshal]::Copy(
                            $pixels,
                            0,
                            $rowPointer,
                            $rowBytes
                        )
                    }
                }
                finally {
                    $bitmap.UnlockBits($data)
                }
            }

            $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $bitmap.Dispose()
        }
    }
    finally {
        $source.Dispose()
        $stream.Dispose()
    }
}

function Assert-FileMatch([string]$Expected, [string]$Actual) {
    if (-not (Test-Path -LiteralPath $Expected -PathType Leaf)) {
        throw "Prepared Arin prototype output is missing: $Expected"
    }

    $expectedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Expected).Hash
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Actual).Hash
    if ($expectedHash -cne $actualHash) {
        throw "Prepared Arin prototype output drifted: $Expected"
    }
}

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Arin prototype source is missing: $sourcePath"
}
if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
    throw "Arin prototype descriptor is missing: $descriptorPath"
}
if (-not (Test-Path -LiteralPath $assetTool -PathType Leaf)) {
    throw "SMILE AssetTool is missing. Run scripts\build.cmd first."
}

$sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourcePath).Hash
if ($sourceHash -cne $expectedSourceHash) {
    throw "Arin prototype source hash changed. Expected $expectedSourceHash, found $sourceHash."
}

Add-Type -AssemblyName System.Drawing.Common

$resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
$resolvedRepositoryRoot = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar
if (-not $resolvedTemporaryRoot.StartsWith($resolvedRepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Arin preparation directory escaped the repository.'
}
if (Test-Path -LiteralPath $resolvedTemporaryRoot) {
    Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
}

$stagePrepared = Join-Path $resolvedTemporaryRoot 'Prepared'
$stageRuntime = Join-Path $resolvedTemporaryRoot 'Runtime'
$stageTextures = Join-Path $stageRuntime 'Textures'
$stageBackup = Join-Path $resolvedTemporaryRoot 'Backup'

try {

[System.IO.Directory]::CreateDirectory($stagePrepared) | Out-Null
[System.IO.Directory]::CreateDirectory($stageTextures) | Out-Null

$bytes = [System.IO.File]::ReadAllBytes($sourcePath)
if ($bytes.Length -lt 20 -or [BitConverter]::ToUInt32($bytes, 0) -ne 0x46546C67 -or
    [BitConverter]::ToUInt32($bytes, 4) -ne 2 -or
    [BitConverter]::ToUInt32($bytes, 8) -ne $bytes.Length) {
    throw 'Arin prototype source is not a canonical GLB 2.0 file.'
}

$offset = 12
$model = $null
$binaryStart = -1
$binaryLength = 0
while ($offset -lt $bytes.Length) {
    if ($offset -gt $bytes.Length - 8) {
        throw 'Arin GLB ends inside a chunk header.'
    }

    $chunkLength = [long][BitConverter]::ToUInt32($bytes, $offset)
    $chunkType = [BitConverter]::ToUInt32($bytes, $offset + 4)
    $payload = [long]$offset + 8L
    if ($chunkLength -gt $bytes.Length - $payload) {
        throw 'Arin GLB chunk exceeds the file.'
    }

    if ($chunkType -eq 0x4E4F534A) {
        if ($null -ne $model) { throw 'Arin GLB contains duplicate JSON chunks.' }
        $json = [System.Text.Encoding]::UTF8.GetString($bytes, $payload, $chunkLength).
            TrimEnd([char]0x20, [char]0)
        $model = $json | ConvertFrom-Json -Depth 100
    }
    elseif ($chunkType -eq 0x004E4942) {
        if ($binaryStart -ge 0) { throw 'Arin GLB contains duplicate BIN chunks.' }
        $binaryStart = $payload
        $binaryLength = $chunkLength
    }

    $offset = $payload + $chunkLength
}

if ($offset -ne $bytes.Length) {
    throw 'Arin GLB chunk table does not end at the declared file boundary.'
}

if ($null -eq $model -or $binaryStart -lt 0 -or $model.buffers.Count -ne 1) {
    throw 'Arin GLB must contain one JSON document and one buffer.'
}
if ($model.images.Count -ne 3 -or $model.textures.Count -ne 3 -or $model.materials.Count -ne 1) {
    throw 'Arin prototype must retain its one-material, three-texture profile.'
}

$declaredBinaryLength = [long]$model.buffers[0].byteLength
if ($declaredBinaryLength -lt 0 -or
    $declaredBinaryLength -gt [int]::MaxValue -or
    $declaredBinaryLength -gt $binaryLength) {
    throw 'Arin GLB buffer length exceeds its bounded BIN chunk.'
}
$binary = [byte[]]::new($declaredBinaryLength)
[Array]::Copy($bytes, $binaryStart, $binary, 0, $binary.Length)
[System.IO.File]::WriteAllBytes((Join-Path $stagePrepared 'ArinPrototype.bin'), $binary)

function Get-TextureImageIndex([object]$TextureInfo, [string]$Semantic) {
    if ($null -eq $TextureInfo -or $null -eq $TextureInfo.index) {
        throw "Arin material is missing its $Semantic texture binding."
    }

    $textureIndex = [long]$TextureInfo.index
    if ($textureIndex -lt 0 -or $textureIndex -ge $model.textures.Count) {
        throw "Arin $Semantic texture index is outside the texture table."
    }

    $texture = $model.textures[[int]$textureIndex]
    if ($null -eq $texture.source) {
        throw "Arin $Semantic texture has no image source."
    }

    $imageIndex = [long]$texture.source
    if ($imageIndex -lt 0 -or $imageIndex -ge $model.images.Count) {
        throw "Arin $Semantic image index is outside the image table."
    }

    return [int]$imageIndex
}

$material = $model.materials[0]
$baseColorImage = Get-TextureImageIndex `
    $material.pbrMetallicRoughness.baseColorTexture 'base-color'
$normalImage = Get-TextureImageIndex $material.normalTexture 'normal'
$metallicRoughnessImage = Get-TextureImageIndex `
    $material.pbrMetallicRoughness.metallicRoughnessTexture 'metallic-roughness'
$occlusionImage = $metallicRoughnessImage
if ($null -ne $material.occlusionTexture) {
    $occlusionImage = Get-TextureImageIndex $material.occlusionTexture 'occlusion'
}
if ($occlusionImage -ne $metallicRoughnessImage) {
    throw 'Arin prototype requires one shared occlusion/roughness/metallic image.'
}

$semanticImageIndexes = @($normalImage, $baseColorImage, $metallicRoughnessImage)
if (($semanticImageIndexes | Sort-Object -Unique).Count -ne 3) {
    throw 'Arin texture semantics must resolve to three distinct images.'
}

$textureRecords = @(
    [ordered]@{
        semantic = 'normal'
        imageIndex = $normalImage
        fileName = 'Arin-normal.png'
        uri = 'Assets/Generation2/Arin/Textures/Arin-normal.png'
        opaqueRed = $false
    },
    [ordered]@{
        semantic = 'base-color'
        imageIndex = $baseColorImage
        fileName = 'Arin-base-color.png'
        uri = 'Assets/Generation2/Arin/Textures/Arin-base-color.png'
        opaqueRed = $false
    },
    [ordered]@{
        semantic = 'orm'
        imageIndex = $metallicRoughnessImage
        fileName = 'Arin-orm.png'
        uri = 'Assets/Generation2/Arin/Textures/Arin-orm.png'
        opaqueRed = $true
    }
)

foreach ($record in $textureRecords) {
    $image = $model.images[$record.imageIndex]
    if ($image.mimeType -cne 'image/jpeg') {
        throw "Arin $($record.semantic) image is not declared as JPEG input."
    }
    if ($null -eq $image.bufferView) {
        throw "Arin $($record.semantic) image has no buffer view."
    }

    $viewIndex = [long]$image.bufferView
    if ($viewIndex -lt 0 -or $viewIndex -ge $model.bufferViews.Count) {
        throw "Arin $($record.semantic) buffer-view index is outside the table."
    }

    $view = $model.bufferViews[[int]$viewIndex]
    $viewBuffer = if ($null -ne $view.buffer) { [long]$view.buffer } else { 0L }
    $viewOffset = if ($null -ne $view.byteOffset) { [long]$view.byteOffset } else { 0L }
    $viewLength = if ($null -ne $view.byteLength) { [long]$view.byteLength } else { -1L }
    if ($viewBuffer -ne 0 -or $viewOffset -lt 0 -or $viewLength -le 0 -or
        $viewOffset -gt $declaredBinaryLength -or
        $viewLength -gt $declaredBinaryLength - $viewOffset -or
        $viewLength -gt [int]::MaxValue) {
        throw "Arin $($record.semantic) buffer view exceeds the declared binary buffer."
    }

    $imageBytes = [byte[]]::new([int]$viewLength)
    [Array]::Copy($binary, $viewOffset, $imageBytes, 0, $imageBytes.Length)
    Write-Png `
        $imageBytes `
        (Join-Path $stageTextures $record.fileName) `
        $record.opaqueRed `
        $record.semantic
    $image.PSObject.Properties.Remove('bufferView')
    $image.PSObject.Properties.Remove('mimeType')
    $image | Add-Member -NotePropertyName uri -NotePropertyValue $record.uri
}

$model.buffers[0] | Add-Member -NotePropertyName uri -NotePropertyValue 'ArinPrototype.bin'
$preparedSource = Join-Path $stagePrepared 'ArinPrototype.gltf'
[System.IO.File]::WriteAllText(
    $preparedSource,
    ($model | ConvertTo-Json -Depth 100 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)

$stageModel = Join-Path $stageRuntime 'ArinPrototype.sm3d'
& $assetTool model $preparedSource --format-version 2 --descriptor $descriptorPath -o $stageModel
if ($LASTEXITCODE -ne 0) { throw 'Arin prototype conversion failed.' }
$inspection = (& $assetTool inspect $stageModel) -join "`n"
if ($LASTEXITCODE -ne 0 -or
    $inspection -notmatch 'Parts: 1' -or
    $inspection -notmatch 'Vertices: 6631' -or
    $inspection -notmatch 'Triangles: 9974' -or
    $inspection -notmatch 'Bones: 41' -or
    $inspection -notmatch 'Clips: 3' -or
    $inspection -notmatch 'Events: 4' -or
    $inspection -notmatch 'Sockets: 6') {
    throw "Arin prototype inspection did not match its accepted profile.`n$inspection"
}

$manifestTextures = @()
foreach ($record in $textureRecords) {
    $texturePath = Join-Path $stageTextures $record.fileName
    $decoded = [System.Drawing.Image]::FromFile($texturePath)
    try {
        $manifestTextures += [ordered]@{
            semantic = $record.semantic
            imageIndex = $record.imageIndex
            width = $decoded.Width
            height = $decoded.Height
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $texturePath).Hash
            output = "Textures/$($record.fileName)"
        }
    }
    finally {
        $decoded.Dispose()
    }
}

$assetToolVersion = (Get-Item -LiteralPath $assetTool).VersionInfo.FileVersion
if ([string]::IsNullOrWhiteSpace($assetToolVersion)) { $assetToolVersion = 'repository-build' }
$manifest = [ordered]@{
    version = 1
    preparationVersion = $preparationVersion
    assetId = 'sin-star-i.character-1.paladin'
    characterName = 'Arin'
    partyRole = 'Paladin'
    source = [ordered]@{
        path = 'sin-star-i-character-1-paladin-tripo-v01.original.glb'
        sha256 = $sourceHash
        bytes = $bytes.Length
    }
    descriptor = [ordered]@{
        path = 'ArinPrototype.sm3d.json'
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $descriptorPath).Hash
    }
    tool = [ordered]@{
        path = 'artifacts/assettool/smileasset.exe'
        fileVersion = $assetToolVersion
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $assetTool).Hash
    }
    prepared = [ordered]@{
        gltfSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $preparedSource).Hash
        binarySha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath `
            (Join-Path $stagePrepared 'ArinPrototype.bin')).Hash
        modelSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $stageModel).Hash
    }
    textures = $manifestTextures
    ormPolicy = 'Set occlusion red to 255; preserve roughness green, metallic blue, and alpha.'
    sourceOcclusionBinding = if ($null -eq $material.occlusionTexture) {
        'Absent; published red channel is neutral white.'
    }
    else {
        'Shared with metallic-roughness texture.'
    }
    sourceQualityNotice = 'PNG publication changes the container; it cannot restore precision lost to source JPEG compression.'
}
$stageManifest = Join-Path $stagePrepared 'ArinPrototype.preparation-manifest.json'
[System.IO.File]::WriteAllText(
    $stageManifest,
    ($manifest | ConvertTo-Json -Depth 20),
    [System.Text.UTF8Encoding]::new($false)
)

$outputs = [ordered]@{
    (Join-Path $preparedRoot 'ArinPrototype.gltf') = $preparedSource
    (Join-Path $preparedRoot 'ArinPrototype.bin') = Join-Path $stagePrepared 'ArinPrototype.bin'
    (Join-Path $preparedRoot 'ArinPrototype.preparation-manifest.json') = $stageManifest
    (Join-Path $runtimeRoot 'ArinPrototype.sm3d') = $stageModel
    (Join-Path $runtimeRoot 'Textures\Arin-normal.png') = Join-Path $stageTextures 'Arin-normal.png'
    (Join-Path $runtimeRoot 'Textures\Arin-base-color.png') = Join-Path $stageTextures 'Arin-base-color.png'
    (Join-Path $runtimeRoot 'Textures\Arin-orm.png') = Join-Path $stageTextures 'Arin-orm.png'
}

foreach ($entry in $outputs.GetEnumerator()) {
    $resolvedOutput = [System.IO.Path]::GetFullPath($entry.Key)
    if (-not $resolvedOutput.StartsWith(
            $resolvedRepositoryRoot,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Arin preparation output escaped the repository: $resolvedOutput"
    }
}

if ($Check) {
    foreach ($entry in $outputs.GetEnumerator()) {
        Assert-FileMatch $entry.Key $entry.Value
    }

    Write-Host 'Dragonfall Arin prototype preparation check passed.'
}
else {
    [System.IO.Directory]::CreateDirectory($stageBackup) | Out-Null
    $publication = [System.Collections.Generic.List[object]]::new()
    $publicationCount = 0
    try {
        foreach ($entry in $outputs.GetEnumerator()) {
            $target = [System.IO.Path]::GetFullPath($entry.Key)
            $targetDirectory = [System.IO.Path]::GetDirectoryName($target)
            $existed = Test-Path -LiteralPath $target -PathType Leaf
            $backup = Join-Path $stageBackup ("{0:D2}.backup" -f $publicationCount)
            if ($existed) {
                [System.IO.File]::Copy($target, $backup, $true)
            }

            $publication.Add([pscustomobject]@{
                Target = $target
                Existed = $existed
                Backup = $backup
            })
            [System.IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
            [System.IO.File]::Copy($entry.Value, $target, $true)
            $publicationCount++
            if ($FailAfterPublication -eq $publicationCount) {
                throw "Synthetic Arin publication failure after output $publicationCount."
            }
        }
    }
    catch {
        for ($index = $publication.Count - 1; $index -ge 0; $index--) {
            $published = $publication[$index]
            if ($published.Existed) {
                [System.IO.File]::Copy($published.Backup, $published.Target, $true)
            }
            elseif (Test-Path -LiteralPath $published.Target -PathType Leaf) {
                Remove-Item -LiteralPath $published.Target -Force
            }
        }

        throw
    }

    Write-Host 'Prepared Dragonfall Arin prototype source and runtime assets.'
}

Write-Output $inspection
}
finally {
    if (Test-Path -LiteralPath $resolvedTemporaryRoot) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
