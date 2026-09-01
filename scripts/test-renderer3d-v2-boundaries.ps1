[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\renderer3d-v2-boundaries'
$testSource = Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Source'
$maximumVertices = 65536
$maximumIndices = 196611

function Invoke-ExpectedFailure([string]$Description, [string[]]$Arguments) {
    & $assetTool @Arguments 2>$null
    if ($LASTEXITCODE -eq 0) { throw "smileasset accepted $Description." }
}

function Invoke-ExpectedDiagnostic([string]$Description, [string]$Expected, [string[]]$Arguments) {
    $output = (& $assetTool @Arguments 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0) { throw "smileasset accepted $Description." }
    if (-not $output.Contains($Expected, [System.StringComparison]::Ordinal)) {
        throw "smileasset returned the wrong diagnostic for $Description.`n$output"
    }
}

[System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
$binaryPath = Join-Path $temporaryRoot 'BoundaryGeometry.bin'
$binaryStream = [System.IO.File]::Create($binaryPath)
$writer = [System.IO.BinaryWriter]::new($binaryStream)

try {
    for ($index = 0; $index -lt $maximumVertices; $index++) {
        if ($index -eq 1) { $position = @(1.0, 0.0, 0.0) }
        elseif ($index -eq 2) { $position = @(0.0, 1.0, 0.0) }
        else { $position = @(-1.0, 0.0, 0.0) }
        foreach ($value in $position) { $writer.Write([single]$value) }
    }

    for ($index = 0; $index -lt $maximumVertices; $index++) {
        $writer.Write([single]0)
        $writer.Write([single]0)
        $writer.Write([single]1)
    }

    for ($index = 0; $index -lt $maximumVertices; $index++) {
        if ($index -eq 1) { $uv = @(1.0, 0.0) }
        elseif ($index -eq 2) { $uv = @(0.5, 1.0) }
        else { $uv = @(0.0, 0.0) }
        foreach ($value in $uv) { $writer.Write([single]$value) }
    }

    for ($index = 0; $index -lt $maximumVertices; $index++) {
        $writer.Write([single]1)
        $writer.Write([single]0)
        $writer.Write([single]0)
        $writer.Write([single]1)
    }

    for ($index = 0; $index -lt $maximumIndices; $index++) {
        $writer.Write([uint32]($index % 3))
    }
}
finally {
    $writer.Dispose()
    $binaryStream.Dispose()
}

$positionOffset = 0
$normalOffset = $maximumVertices * 12
$uvOffset = $normalOffset + $maximumVertices * 12
$tangentOffset = $uvOffset + $maximumVertices * 8
$indexOffset = $tangentOffset + $maximumVertices * 16
$binaryLength = $indexOffset + $maximumIndices * 4
$views = @(
    [ordered]@{ buffer = 0; byteOffset = $positionOffset; byteLength = $maximumVertices * 12 }
    [ordered]@{ buffer = 0; byteOffset = $normalOffset; byteLength = $maximumVertices * 12 }
    [ordered]@{ buffer = 0; byteOffset = $uvOffset; byteLength = $maximumVertices * 8 }
    [ordered]@{ buffer = 0; byteOffset = $tangentOffset; byteLength = $maximumVertices * 16 }
    [ordered]@{ buffer = 0; byteOffset = $indexOffset; byteLength = $maximumIndices * 4 }
)
$accessors = @()

function Add-AccessorSet([int]$VertexCount, [int]$IndexCount) {
    $script:accessors += [ordered]@{ bufferView = 0; componentType = 5126; count = $VertexCount; type = 'VEC3' }
    $script:accessors += [ordered]@{ bufferView = 1; componentType = 5126; count = $VertexCount; type = 'VEC3' }
    $script:accessors += [ordered]@{ bufferView = 2; componentType = 5126; count = $VertexCount; type = 'VEC2' }
    $script:accessors += [ordered]@{ bufferView = 3; componentType = 5126; count = $VertexCount; type = 'VEC4' }
    $script:accessors += [ordered]@{ bufferView = 4; componentType = 5125; count = $IndexCount; type = 'SCALAR' }
}

Add-AccessorSet 65535 196608
Add-AccessorSet 65534 196605
Add-AccessorSet 3 3
$primitives = @(
    for ($index = 0; $index -lt 3; $index++) {
        $accessor = $index * 5
        [ordered]@{
            attributes = [ordered]@{
                POSITION = $accessor
                NORMAL = $accessor + 1
                TEXCOORD_0 = $accessor + 2
                TANGENT = $accessor + 3
            }
            indices = $accessor + 4
            material = 0
            mode = 4
        }
    }
)
$model = [ordered]@{
    asset = [ordered]@{ version = '2.0'; generator = 'SMILE 2.0 M1 boundary fixture generator' }
    scene = 0
    scenes = @([ordered]@{ nodes = @(0) })
    nodes = @([ordered]@{ name = 'BoundaryGeometry'; mesh = 0 })
    meshes = @([ordered]@{ name = 'BoundaryGeometry'; primitives = $primitives })
    materials = @([ordered]@{ name = 'BoundaryMaterial' })
    buffers = @([ordered]@{ byteLength = $binaryLength; uri = 'BoundaryGeometry.bin' })
    bufferViews = $views
    accessors = $accessors
}
$sourcePath = Join-Path $temporaryRoot 'BoundaryGeometry.gltf'
$outputPath = Join-Path $temporaryRoot 'BoundaryGeometry.sm3d'
$json = $model | ConvertTo-Json -Depth 20 -Compress
[System.IO.File]::WriteAllText($sourcePath, $json, [System.Text.UTF8Encoding]::new($false))

& $assetTool model $sourcePath --format-version 2 -o $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Exact SM3D v2 geometry-boundary conversion failed.' }
$inspection = (& $assetTool inspect $outputPath) -join "`n"
if ($LASTEXITCODE -ne 0 -or
    $inspection -notmatch 'Parts: 3' -or
    $inspection -notmatch 'Vertices: 131072' -or
    $inspection -notmatch 'Indices: 393216') {
    throw 'Exact SM3D v2 geometry-boundary inspection failed.'
}

$vertexOverflow = $json.Replace('"count":65535,"type":"VEC3"', '"count":65536,"type":"VEC3"')
$vertexOverflow = $vertexOverflow.Replace('"count":65535,"type":"VEC2"', '"count":65536,"type":"VEC2"')
$vertexOverflow = $vertexOverflow.Replace('"count":65535,"type":"VEC4"', '"count":65536,"type":"VEC4"')
$vertexOverflowPath = Join-Path $temporaryRoot 'VertexOverflow.gltf'
[System.IO.File]::WriteAllText($vertexOverflowPath, $vertexOverflow, [System.Text.UTF8Encoding]::new($false))
Invoke-ExpectedFailure '65,536 vertices in one part' @(
    'model', $vertexOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$indexOverflow = $json.Replace('"count":196608,"type":"SCALAR"', '"count":196611,"type":"SCALAR"')
$indexOverflowPath = Join-Path $temporaryRoot 'IndexOverflow.gltf'
[System.IO.File]::WriteAllText($indexOverflowPath, $indexOverflow, [System.Text.UTF8Encoding]::new($false))
Invoke-ExpectedFailure '196,611 indices in one part' @(
    'model', $indexOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$m0 = Get-Content -LiteralPath (Join-Path $testSource 'M0Triangle.gltf') -Raw | ConvertFrom-Json
$primitive = $m0.meshes[0].primitives[0]
$m0.meshes[0].primitives = @(for ($index = 0; $index -lt 17; $index++) { $primitive })
$compatiblePartsPath = Join-Path $temporaryRoot 'CompatibleParts.gltf'
[System.IO.File]::WriteAllText(
    $compatiblePartsPath,
    ($m0 | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
& $assetTool model $compatiblePartsPath --format-version 2 -o (Join-Path $temporaryRoot 'CompatibleParts.sm3d')
if ($LASTEXITCODE -ne 0) { throw 'Compatible source-part coalescing failed.' }
$compatibleInspection = (& $assetTool inspect (Join-Path $temporaryRoot 'CompatibleParts.sm3d')) -join "`n"
if ($LASTEXITCODE -ne 0 -or $compatibleInspection -notmatch 'Parts: 1') {
    throw 'Compatible source parts did not coalesce to one bounded runtime part.'
}

$m0 = Get-Content -LiteralPath (Join-Path $testSource 'M0Triangle.gltf') -Raw | ConvertFrom-Json
$primitive = $m0.meshes[0].primitives[0]
$m0.materials = @(for ($index = 0; $index -lt 17; $index++) {
    [ordered]@{ name = "Part Material $index" }
})
$m0.meshes[0].primitives = @(for ($index = 0; $index -lt 17; $index++) {
    [ordered]@{
        attributes = $primitive.attributes
        indices = $primitive.indices
        material = $index
        mode = 4
    }
})
$partOverflowPath = Join-Path $temporaryRoot 'PartOverflow.gltf'
[System.IO.File]::WriteAllText(
    $partOverflowPath,
    ($m0 | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
Invoke-ExpectedFailure '17 parts' @(
    'model', $partOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$metadataSource = Join-Path $repositoryRoot 'artifacts\temp\renderer3d-v2-fixtures\BoundaryMetadata.gltf'
$metadata = Get-Content -LiteralPath $metadataSource -Raw | ConvertFrom-Json
$metadata.materials += [ordered]@{ name = 'MaterialOverflow' }
$materialOverflowPath = Join-Path $temporaryRoot 'MaterialOverflow.gltf'
[System.IO.File]::WriteAllText(
    $materialOverflowPath,
    ($metadata | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
Invoke-ExpectedFailure '65 materials' @(
    'model', $materialOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$metadata = Get-Content -LiteralPath $metadataSource -Raw | ConvertFrom-Json
$metadata.images += [ordered]@{ uri = 'Assets/Textures/Boundary-overflow.png' }
$metadata.textures += [ordered]@{ source = 128 }
$metadata.materials[32] = [ordered]@{
    name = 'TextureOverflow'
    pbrMetallicRoughness = [ordered]@{ baseColorTexture = [ordered]@{ index = 128 } }
}
$textureOverflowPath = Join-Path $temporaryRoot 'TextureOverflow.gltf'
[System.IO.File]::WriteAllText(
    $textureOverflowPath,
    ($metadata | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
Invoke-ExpectedFailure '129 texture references' @(
    'model', $textureOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$tableBoundary = Get-Content -LiteralPath (Join-Path $testSource 'M0Triangle.gltf') -Raw | ConvertFrom-Json
$baseView = $tableBoundary.bufferViews[0]
$baseAccessor = $tableBoundary.accessors[0]
$tableBoundary.bufferViews = @($tableBoundary.bufferViews) + @(
    for ($index = $tableBoundary.bufferViews.Count; $index -lt 1024; $index++) { $baseView }
)
$tableBoundary.accessors = @($tableBoundary.accessors) + @(
    for ($index = $tableBoundary.accessors.Count; $index -lt 1024; $index++) { $baseAccessor }
)
$tableBoundaryPath = Join-Path $temporaryRoot 'TableBoundary.gltf'
[System.IO.File]::WriteAllText(
    $tableBoundaryPath,
    ($tableBoundary | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
& $assetTool model $tableBoundaryPath --format-version 2 -o (Join-Path $temporaryRoot 'TableBoundary.sm3d')
if ($LASTEXITCODE -ne 0) { throw 'Exact 1,024 bufferView/accessor conversion failed.' }

$bufferViewOverflow = Get-Content -LiteralPath $tableBoundaryPath -Raw | ConvertFrom-Json
$bufferViewOverflow.bufferViews = @($bufferViewOverflow.bufferViews) + @($baseView)
$bufferViewOverflowPath = Join-Path $temporaryRoot 'BufferViewOverflow.gltf'
[System.IO.File]::WriteAllText(
    $bufferViewOverflowPath,
    ($bufferViewOverflow | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
Invoke-ExpectedDiagnostic '1,025 bufferViews' 'SMA1140: bufferViews must be an array of at most 1024 entries.' @(
    'model', $bufferViewOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$accessorOverflow = Get-Content -LiteralPath $tableBoundaryPath -Raw | ConvertFrom-Json
$accessorOverflow.accessors = @($accessorOverflow.accessors) + @($baseAccessor)
$accessorOverflowPath = Join-Path $temporaryRoot 'AccessorOverflow.gltf'
[System.IO.File]::WriteAllText(
    $accessorOverflowPath,
    ($accessorOverflow | ConvertTo-Json -Depth 20 -Compress),
    [System.Text.UTF8Encoding]::new($false)
)
Invoke-ExpectedDiagnostic '1,025 accessors' 'SMA1116: glTF requires 1 to 1024 accessors.' @(
    'model', $accessorOverflowPath, '--format-version', '2', '-o', (Join-Path $temporaryRoot 'invalid.sm3d')
)

$oversizedPath = Join-Path $temporaryRoot 'Oversized.sm3d'
[System.IO.File]::WriteAllBytes($oversizedPath, [byte[]]::new(16 * 1024 * 1024 + 1))
Invoke-ExpectedFailure 'a model file over 16 MiB' @('inspect', $oversizedPath)

Write-Host 'Renderer3D SM3D v2 exact and over-limit boundary tests passed.'
