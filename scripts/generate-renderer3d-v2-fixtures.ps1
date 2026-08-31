[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DModelTests'
$sourceRoot = Join-Path $testRoot 'Source'
$assetRoot = Join-Path $testRoot 'Assets'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\renderer3d-v2-fixtures'
$glbGenerator = Join-Path $PSScriptRoot 'generate-renderer3d-glb-fixture.ps1'

function Write-UInt32([byte[]]$Bytes, [int]$Offset, [uint32]$Value) {
    $valueBytes = [System.BitConverter]::GetBytes($Value)
    [System.Buffer]::BlockCopy($valueBytes, 0, $Bytes, $Offset, 4)
}

function Update-Checksum([byte[]]$Bytes) {
    [uint32]$checksum = 2166136261

    for ($index = 64; $index -lt $Bytes.Length; $index++) {
        $product = [uint64]($checksum -bxor $Bytes[$index]) * 16777619
        $checksum = [uint32]($product -band [uint64]4294967295)
    }

    Write-UInt32 $Bytes 16 $checksum
}

function Update-V1Checksum([byte[]]$Bytes) {
    [uint32]$checksum = 2166136261

    for ($index = 32; $index -lt $Bytes.Length; $index++) {
        $product = [uint64]($checksum -bxor $Bytes[$index]) * 16777619
        $checksum = [uint32]($product -band [uint64]4294967295)
    }

    Write-UInt32 $Bytes 28 $checksum
}

function Find-ChunkOffset([byte[]]$Bytes, [string]$Id) {
    $chunkCount = [System.BitConverter]::ToUInt32($Bytes, 20)

    for ($index = 0; $index -lt $chunkCount; $index++) {
        $entry = 64 + $index * 32
        $entryId = [System.Text.Encoding]::ASCII.GetString($Bytes, $entry, 4)

        if ($entryId -ceq $Id) {
            return [int][System.BitConverter]::ToUInt32($Bytes, $entry + 8)
        }
    }

    throw "SM3D chunk '$Id' was not found."
}

function Copy-Bytes([byte[]]$Bytes) {
    $copy = [byte[]]::new($Bytes.Length)
    [System.Buffer]::BlockCopy($Bytes, 0, $copy, 0, $Bytes.Length)
    return ,$copy
}

function Publish-Fixture([string]$Path, [byte[]]$Bytes) {
    if ($Check) {
        if (-not (Test-Path -LiteralPath $Path)) {
            throw "The deterministic Renderer3D v2 fixture is missing: $Path"
        }

        $existing = [System.IO.File]::ReadAllBytes($Path)
        $matches = $existing.Length -eq $Bytes.Length

        for ($index = 0; $matches -and $index -lt $existing.Length; $index++) {
            $matches = $existing[$index] -eq $Bytes[$index]
        }

        if (-not $matches) {
            throw "The deterministic Renderer3D v2 fixture differs from the generator: $Path"
        }
    }
    else {
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
        [System.IO.File]::WriteAllBytes($Path, $Bytes)
    }

    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
    Write-Output "Verified $Path ($($Bytes.Length) bytes, SHA256 $hash)"
}

if (-not (Test-Path -LiteralPath $assetTool)) {
    throw "Build smileasset before generating Renderer3D v2 fixtures: $assetTool"
}

[System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
& $glbGenerator -Check

$m0Temporary = Join-Path $temporaryRoot 'M0TriangleV2.sm3d'
$pbrTemporary = Join-Path $temporaryRoot 'PbrTriangleV2.sm3d'
$boundarySource = Join-Path $temporaryRoot 'BoundaryMetadata.gltf'
$boundaryTemporary = Join-Path $temporaryRoot 'BoundaryMetadataV2.sm3d'

& $assetTool model (Join-Path $sourceRoot 'M0Triangle.glb') -o $m0Temporary
if ($LASTEXITCODE -ne 0) { throw 'M0 GLB conversion failed.' }

& $assetTool model (Join-Path $sourceRoot 'PbrTriangle.gltf') --format-version 2 -o $pbrTemporary
if ($LASTEXITCODE -ne 0) { throw 'PBR glTF conversion failed.' }

$boundary = Get-Content -LiteralPath (Join-Path $sourceRoot 'M0Triangle.gltf') -Raw | ConvertFrom-Json
$boundary.meshes[0].name = 'BoundaryMetadata'
$boundary.nodes[0].name = 'BoundaryMetadata'
$boundary.meshes[0].primitives = @(
    for ($index = 0; $index -lt 16; $index++) {
        [ordered]@{
            attributes = [ordered]@{ POSITION = 0; NORMAL = 1; TEXCOORD_0 = 2 }
            indices = 3
            material = $index
            mode = 4
        }
    }
)
$boundaryMaterials = @(
    for ($index = 0; $index -lt 64; $index++) {
        if ($index -lt 32) {
            $texture = $index * 4
            [ordered]@{
                name = "BoundaryMaterial$index"
                pbrMetallicRoughness = [ordered]@{
                    baseColorTexture = [ordered]@{ index = $texture }
                    metallicRoughnessTexture = [ordered]@{ index = $texture + 2 }
                }
                normalTexture = [ordered]@{ index = $texture + 1 }
                occlusionTexture = [ordered]@{ index = $texture + 2 }
                emissiveTexture = [ordered]@{ index = $texture + 3 }
            }
        }
        else {
            [ordered]@{ name = "BoundaryMaterial$index" }
        }
    }
)
$boundaryTextures = @(
    for ($index = 0; $index -lt 128; $index++) {
        [ordered]@{ source = $index }
    }
)
$boundaryImages = @(
    for ($index = 0; $index -lt 128; $index++) {
        [ordered]@{ uri = 'Assets/Textures/Boundary-{0:D3}.png' -f $index }
    }
)
$boundary.materials = $boundaryMaterials
$boundary | Add-Member -NotePropertyName textures -NotePropertyValue $boundaryTextures
$boundary | Add-Member -NotePropertyName images -NotePropertyValue $boundaryImages
$boundaryJson = $boundary | ConvertTo-Json -Depth 20 -Compress
[System.IO.File]::WriteAllText($boundarySource, $boundaryJson, [System.Text.UTF8Encoding]::new($false))

& $assetTool model $boundarySource --format-version 2 -o $boundaryTemporary
if ($LASTEXITCODE -ne 0) { throw 'Boundary metadata glTF conversion failed.' }

$m0 = [System.IO.File]::ReadAllBytes($m0Temporary)
$pbr = [System.IO.File]::ReadAllBytes($pbrTemporary)
$boundaryBytes = [System.IO.File]::ReadAllBytes($boundaryTemporary)
Publish-Fixture (Join-Path $assetRoot 'M0TriangleV2.sm3d') $m0
Publish-Fixture (Join-Path $assetRoot 'PbrTriangleV2.sm3d') $pbr
Publish-Fixture (Join-Path $assetRoot 'BoundaryMetadataV2.sm3d') $boundaryBytes

$badHeader = Copy-Bytes $m0
$badHeader[6] = 63
Publish-Fixture (Join-Path $assetRoot 'BadV2Header.sm3d') $badHeader

$badSize = Copy-Bytes $m0
Write-UInt32 $badSize 12 ([uint32]($badSize.Length + 4))
Publish-Fixture (Join-Path $assetRoot 'BadV2Size.sm3d') $badSize

$badChecksum = Copy-Bytes $m0
$badChecksum[$badChecksum.Length - 1] = $badChecksum[$badChecksum.Length - 1] -bxor 1
Publish-Fixture (Join-Path $assetRoot 'BadV2Checksum.sm3d') $badChecksum

$badDirectory = Copy-Bytes $m0
Write-UInt32 $badDirectory 28 31
Publish-Fixture (Join-Path $assetRoot 'BadV2Directory.sm3d') $badDirectory

$badRange = Copy-Bytes $m0
Write-UInt32 $badRange 72 ([uint32]($badRange.Length - 1))
Write-UInt32 $badRange 76 64
Update-Checksum $badRange
Publish-Fixture (Join-Path $assetRoot 'BadV2ChunkRange.sm3d') $badRange

$badCount = Copy-Bytes $m0
Write-UInt32 $badCount 36 2
Publish-Fixture (Join-Path $assetRoot 'BadV2Count.sm3d') $badCount

$badStride = Copy-Bytes $m0
Write-UInt32 $badStride 148 44
Update-Checksum $badStride
Publish-Fixture (Join-Path $assetRoot 'BadV2Stride.sm3d') $badStride

$missingRequired = Copy-Bytes $m0
[System.Text.Encoding]::ASCII.GetBytes('MISS').CopyTo($missingRequired, 256)
Write-UInt32 $missingRequired 260 1
Update-Checksum $missingRequired
Publish-Fixture (Join-Path $assetRoot 'MissingRequiredV2.sm3d') $missingRequired

$directoryCount = [System.BitConverter]::ToUInt32($m0, 20)
$directoryEnd = 64 + [int]$directoryCount * 32
$optional = [byte[]]::new($m0.Length + 32)
[System.Buffer]::BlockCopy($m0, 0, $optional, 0, $directoryEnd)
[System.Buffer]::BlockCopy($m0, $directoryEnd, $optional, $directoryEnd + 32, $m0.Length - $directoryEnd)
Write-UInt32 $optional 12 ([uint32]$optional.Length)
Write-UInt32 $optional 20 ([uint32]($directoryCount + 1))

for ($index = 0; $index -lt $directoryCount; $index++) {
    $entry = 64 + $index * 32
    $oldOffset = [System.BitConverter]::ToUInt32($m0, $entry + 8)
    Write-UInt32 $optional ($entry + 8) ([uint32]($oldOffset + 32))
}

$unknownEntry = $directoryEnd
[System.Text.Encoding]::ASCII.GetBytes('EXTR').CopyTo($optional, $unknownEntry)
Write-UInt32 $optional ($unknownEntry + 4) 1
Write-UInt32 $optional ($unknownEntry + 8) ([uint32]$optional.Length)
Update-Checksum $optional
Publish-Fixture (Join-Path $assetRoot 'UnknownOptionalV2.sm3d') $optional

$unknownRequired = Copy-Bytes $optional
Write-UInt32 $unknownRequired ($unknownEntry + 4) 0
Update-Checksum $unknownRequired
Publish-Fixture (Join-Path $assetRoot 'UnknownRequiredV2.sm3d') $unknownRequired

$nonPrintableNul = Copy-Bytes $optional
$nonPrintableNul[$unknownEntry] = 0
Update-Checksum $nonPrintableNul
Publish-Fixture (Join-Path $assetRoot 'NonPrintableNulV2.sm3d') $nonPrintableNul

$nonPrintableHigh = Copy-Bytes $optional
$nonPrintableHigh[$unknownEntry] = 128
Update-Checksum $nonPrintableHigh
Publish-Fixture (Join-Path $assetRoot 'NonPrintableHighV2.sm3d') $nonPrintableHigh

$nonPrintableControl = Copy-Bytes $optional
$nonPrintableControl[$unknownEntry] = 31
Update-Checksum $nonPrintableControl
Publish-Fixture (Join-Path $assetRoot 'NonPrintableControlV2.sm3d') $nonPrintableControl

$duplicateChunk = Copy-Bytes $optional
[System.Text.Encoding]::ASCII.GetBytes('STR0').CopyTo($duplicateChunk, $unknownEntry)
Update-Checksum $duplicateChunk
Publish-Fixture (Join-Path $assetRoot 'DuplicateChunkV2.sm3d') $duplicateChunk

$vertexOffset = Find-ChunkOffset $m0 'VERT'
$invalidNormal = Copy-Bytes $m0
[System.BitConverter]::GetBytes([single]2).CopyTo($invalidNormal, $vertexOffset + 20)
Update-Checksum $invalidNormal
Publish-Fixture (Join-Path $assetRoot 'InvalidNormalBasisV2.sm3d') $invalidNormal

$invalidTangent = Copy-Bytes $m0
[System.BitConverter]::GetBytes([single]2).CopyTo($invalidTangent, $vertexOffset + 24)
Update-Checksum $invalidTangent
Publish-Fixture (Join-Path $assetRoot 'InvalidTangentBasisV2.sm3d') $invalidTangent

$invalidOrthogonal = Copy-Bytes $m0
[System.BitConverter]::GetBytes([single]0).CopyTo($invalidOrthogonal, $vertexOffset + 24)
[System.BitConverter]::GetBytes([single]0).CopyTo($invalidOrthogonal, $vertexOffset + 28)
[System.BitConverter]::GetBytes([single]-1).CopyTo($invalidOrthogonal, $vertexOffset + 32)
Update-Checksum $invalidOrthogonal
Publish-Fixture (Join-Path $assetRoot 'InvalidOrthogonalBasisV2.sm3d') $invalidOrthogonal

$invalidHandedness = Copy-Bytes $m0
[System.BitConverter]::GetBytes([single]0).CopyTo($invalidHandedness, $vertexOffset + 36)
Update-Checksum $invalidHandedness
Publish-Fixture (Join-Path $assetRoot 'InvalidHandednessV2.sm3d') $invalidHandedness

$v1Source = Join-Path $assetRoot 'Humanoid.sm3d'

if (-not (Test-Path -LiteralPath $v1Source)) {
    throw "The SM3D v1 source fixture is missing: $v1Source"
}

$invalidV1 = Copy-Bytes ([System.IO.File]::ReadAllBytes($v1Source))
Write-UInt32 $invalidV1 52 1
Update-V1Checksum $invalidV1
Publish-Fixture (Join-Path $assetRoot 'InvalidStructureV1.sm3d') $invalidV1

$glb = [System.IO.File]::ReadAllBytes((Join-Path $sourceRoot 'M0Triangle.glb'))
$badGlbMagic = Copy-Bytes $glb
$badGlbMagic[0] = 0
Publish-Fixture (Join-Path $sourceRoot 'BadGlbMagic.glb') $badGlbMagic

$badGlbLength = Copy-Bytes $glb
Write-UInt32 $badGlbLength 8 ([uint32]($badGlbLength.Length + 4))
Publish-Fixture (Join-Path $sourceRoot 'BadGlbLength.glb') $badGlbLength

$badGlbVersion = Copy-Bytes $glb
Write-UInt32 $badGlbVersion 4 3
Publish-Fixture (Join-Path $sourceRoot 'BadGlbVersion.glb') $badGlbVersion

$badGlbChunk = Copy-Bytes $glb
Write-UInt32 $badGlbChunk 12 ([uint32]$badGlbChunk.Length)
Publish-Fixture (Join-Path $sourceRoot 'BadGlbChunk.glb') $badGlbChunk

$badGlbAlignment = Copy-Bytes $glb
$jsonLength = [System.BitConverter]::ToUInt32($glb, 12)
Write-UInt32 $badGlbAlignment 12 ([uint32]($jsonLength + 1))
Publish-Fixture (Join-Path $sourceRoot 'BadGlbAlignment.glb') $badGlbAlignment

$duplicateJson = Copy-Bytes $glb
$secondChunk = 20 + [int]$jsonLength
Write-UInt32 $duplicateJson ($secondChunk + 4) 0x4E4F534A
Publish-Fixture (Join-Path $sourceRoot 'BadGlbDuplicateJson.glb') $duplicateJson

$badBinReference = Copy-Bytes $glb
$glbText = [System.Text.Encoding]::ASCII.GetString($badBinReference)
$byteLengthMarker = '"byteLength":104'
$byteLengthOffset = $glbText.IndexOf($byteLengthMarker, [System.StringComparison]::Ordinal)
if ($byteLengthOffset -lt 0) { throw 'The M0 GLB buffer byteLength marker was not found.' }
$digitsOffset = $byteLengthOffset + $byteLengthMarker.Length - 3
[System.Text.Encoding]::ASCII.GetBytes('999').CopyTo($badBinReference, $digitsOffset)
Publish-Fixture (Join-Path $sourceRoot 'BadGlbBinReference.glb') $badBinReference
