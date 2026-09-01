[CmdletBinding()]
param(
    [switch]$Check
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

function Write-Png([byte[]]$Bytes, [string]$Path, [bool]$OpaqueRed) {
    $stream = [System.IO.MemoryStream]::new($Bytes, $false)
    $source = [System.Drawing.Bitmap]::FromStream($stream)
    try {
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
                    $pixels = [byte[]]::new([Math]::Abs($data.Stride) * $data.Height)
                    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)

                    for ($y = 0; $y -lt $data.Height; $y++) {
                        for ($x = 0; $x -lt $data.Width; $x++) {
                            $pixels[$y * $data.Stride + $x * 4 + 2] = 255
                        }
                    }

                    [System.Runtime.InteropServices.Marshal]::Copy($pixels, 0, $data.Scan0, $pixels.Length)
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
    $chunkLength = [BitConverter]::ToUInt32($bytes, $offset)
    $chunkType = [BitConverter]::ToUInt32($bytes, $offset + 4)
    $payload = $offset + 8
    if ($payload + $chunkLength -gt $bytes.Length) { throw 'Arin GLB chunk exceeds the file.' }

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

if ($null -eq $model -or $binaryStart -lt 0 -or $model.buffers.Count -ne 1) {
    throw 'Arin GLB must contain one JSON document and one buffer.'
}
if ($model.images.Count -ne 3 -or $model.textures.Count -ne 3 -or $model.materials.Count -ne 1) {
    throw 'Arin prototype must retain its one-material, three-texture profile.'
}

$declaredBinaryLength = [int]$model.buffers[0].byteLength
if ($declaredBinaryLength -gt $binaryLength) { throw 'Arin GLB buffer length exceeds its BIN chunk.' }
$binary = [byte[]]::new($declaredBinaryLength)
[Array]::Copy($bytes, $binaryStart, $binary, 0, $binary.Length)
[System.IO.File]::WriteAllBytes((Join-Path $stagePrepared 'ArinPrototype.bin'), $binary)

$textureNames = @('Arin-normal.png', 'Arin-base-color.png', 'Arin-orm.png')
$textureUris = @(
    'Assets/Generation2/Arin/Textures/Arin-normal.png',
    'Assets/Generation2/Arin/Textures/Arin-base-color.png',
    'Assets/Generation2/Arin/Textures/Arin-orm.png'
)
for ($index = 0; $index -lt $model.images.Count; $index++) {
    $image = $model.images[$index]
    if ($image.mimeType -cne 'image/jpeg') { throw "Arin image $index is not JPEG input." }
    $view = $model.bufferViews[[int]$image.bufferView]
    $viewOffset = if ($null -ne $view.byteOffset) { [int]$view.byteOffset } else { 0 }
    $imageBytes = [byte[]]::new([int]$view.byteLength)
    [Array]::Copy($binary, $viewOffset, $imageBytes, 0, $imageBytes.Length)
    Write-Png $imageBytes (Join-Path $stageTextures $textureNames[$index]) ($index -eq 2)
    $image.PSObject.Properties.Remove('bufferView')
    $image.PSObject.Properties.Remove('mimeType')
    $image | Add-Member -NotePropertyName uri -NotePropertyValue $textureUris[$index]
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

$outputs = [ordered]@{
    (Join-Path $preparedRoot 'ArinPrototype.gltf') = $preparedSource
    (Join-Path $preparedRoot 'ArinPrototype.bin') = Join-Path $stagePrepared 'ArinPrototype.bin'
    (Join-Path $runtimeRoot 'ArinPrototype.sm3d') = $stageModel
    (Join-Path $runtimeRoot 'Textures\Arin-normal.png') = Join-Path $stageTextures 'Arin-normal.png'
    (Join-Path $runtimeRoot 'Textures\Arin-base-color.png') = Join-Path $stageTextures 'Arin-base-color.png'
    (Join-Path $runtimeRoot 'Textures\Arin-orm.png') = Join-Path $stageTextures 'Arin-orm.png'
}

if ($Check) {
    foreach ($entry in $outputs.GetEnumerator()) { Assert-FileMatch $entry.Key $entry.Value }
    Write-Host 'Dragonfall Arin prototype preparation check passed.'
}
else {
    [System.IO.Directory]::CreateDirectory($preparedRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory((Join-Path $runtimeRoot 'Textures')) | Out-Null
    foreach ($entry in $outputs.GetEnumerator()) {
        [System.IO.File]::Copy($entry.Value, $entry.Key, $true)
    }
    Write-Host 'Prepared Dragonfall Arin prototype source and runtime assets.'
}

Write-Output $inspection
