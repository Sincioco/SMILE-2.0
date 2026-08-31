[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$sourceRoot = Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Source'
$assetRoot = Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Assets'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\renderer3d-m11-hardening'
$validSource = Join-Path $sourceRoot 'M0Triangle.gltf'
$pbrSource = Join-Path $sourceRoot 'PbrTriangle.gltf'
$validOutput = Join-Path $temporaryRoot 'Valid.sm3d'
$invalidOutput = Join-Path $temporaryRoot 'Invalid.sm3d'
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Copy-Model([string]$Path) {
    return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
}

function Write-Model([object]$Model, [string]$Path) {
    $json = $Model | ConvertTo-Json -Depth 32 -Compress
    [System.IO.File]::WriteAllText($Path, $json, $utf8)
}

function Invoke-Asset([string[]]$Arguments) {
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new($assetTool)
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $outputText = $process.StandardOutput.ReadToEnd()
    $errorText = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    $process.Dispose()
    return [pscustomobject]@{ ExitCode = $exitCode; Output = $outputText; Error = $errorText }
}

function Assert-Rejected([string]$Name, [object]$Model, [string]$ExpectedText) {
    $source = Join-Path $temporaryRoot "$Name.gltf"
    Write-Model $Model $source
    $result = Invoke-Asset @('model', $source, '--format-version', '2', '-o', $invalidOutput)

    if ($result.ExitCode -ne 2 -or -not $result.Error.StartsWith('error SMA', [System.StringComparison]::Ordinal) -or
        ($ExpectedText -and $result.Error.IndexOf($ExpectedText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0)) {
        throw "$Name did not produce the expected controlled rejection. Exit=$($result.ExitCode) Error=$($result.Error)"
    }
}

function Remove-Property([object]$Value, [string]$Name) {
    $Value.PSObject.Properties.Remove($Name)
}

if (-not (Test-Path -LiteralPath $assetTool)) {
    throw "Build smileasset before running M1.1 hardening tests: $assetTool"
}

[System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null

$scene = Copy-Model $validSource
$assigned = $scene.meshes[0].primitives[0]
$unassigned = $assigned | ConvertTo-Json -Depth 16 -Compress | ConvertFrom-Json
Remove-Property $unassigned 'material'
$scene.meshes[0].primitives = @($assigned, $unassigned)
$scene.scenes = @(
    [pscustomobject]@{ name = 'UnreachableScene'; nodes = @(0) },
    [pscustomobject]@{ name = 'ActiveScene'; nodes = @(1, 3) }
)
$scene.scene = 1
$scene.nodes = @(
    [pscustomobject]@{ name = 'Unreachable'; mesh = 0; translation = @(100, 0, 0) },
    [pscustomobject]@{ name = 'Parent'; translation = @(2, 0, 0); children = @(2) },
    [pscustomobject]@{ name = 'Reflected'; mesh = 0; scale = @(-1, 2, 1) },
    [pscustomobject]@{ name = 'Rotated'; mesh = 0; translation = @(0, 3, 0); rotation = @(0, 0, 1, 0) }
)
$sceneSource = Join-Path $temporaryRoot 'SceneSemantics.gltf'
$sceneFirst = Join-Path $temporaryRoot 'SceneSemantics-first.sm3d'
$sceneSecond = Join-Path $temporaryRoot 'SceneSemantics-second.sm3d'
Write-Model $scene $sceneSource

& $assetTool model $sceneSource --format-version 2 -o $sceneFirst | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Scene-semantics conversion failed.' }
& $assetTool model $sceneSource --format-version 2 -o $sceneSecond | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Repeated scene-semantics conversion failed.' }

if ((Get-FileHash -Algorithm SHA256 -LiteralPath $sceneFirst).Hash -cne
    (Get-FileHash -Algorithm SHA256 -LiteralPath $sceneSecond).Hash) {
    throw 'Scene traversal and transform baking are not byte deterministic.'
}

$inspection = (& $assetTool inspect $sceneFirst) -join "`n"

if ($LASTEXITCODE -ne 0 -or $inspection -notmatch 'Name: ActiveScene' -or
    $inspection -notmatch 'Parts: 4' -or $inspection -notmatch 'Materials: 2' -or
    $inspection -notmatch 'Bounds: -1,0,0 \| 3,3,0' -or
    $inspection -notmatch 'Material 1: Default') {
    throw "Scene traversal, transform, repeated-instance, unreachable-mesh, or implicit-material inspection failed:`n$inspection"
}

$unassignedOnly = Copy-Model $validSource
Remove-Property $unassignedOnly 'materials'
Remove-Property $unassignedOnly.meshes[0].primitives[0] 'material'
$unassignedSource = Join-Path $temporaryRoot 'AllUnassigned.gltf'
Write-Model $unassignedOnly $unassignedSource
& $assetTool model $unassignedSource --format-version 2 -o $validOutput | Out-Null
$unassignedInspection = (& $assetTool inspect $validOutput) -join "`n"

if ($LASTEXITCODE -ne 0 -or $unassignedInspection -notmatch 'Materials: 1' -or
    $unassignedInspection -notmatch 'Material 0: Default') {
    throw 'All-unassigned implicit default material conversion failed.'
}

$materialCapacity = Copy-Model $validSource
$materialCapacity.materials = @(for ($index = 0; $index -lt 64; $index++) { [pscustomobject]@{ name = "Material$index" } })
Remove-Property $materialCapacity.meshes[0].primitives[0] 'material'
Assert-Rejected 'ImplicitMaterialCapacity' $materialCapacity 'implicit material'

$skin = Copy-Model $validSource
$skin | Add-Member -NotePropertyName skins -NotePropertyValue @([pscustomobject]@{})
Assert-Rejected 'Skin' $skin 'skins'

$animation = Copy-Model $validSource
$animation | Add-Member -NotePropertyName animations -NotePropertyValue @([pscustomobject]@{})
Assert-Rejected 'Animation' $animation 'animations'

$joints = Copy-Model $validSource
$joints.meshes[0].primitives[0].attributes | Add-Member -NotePropertyName JOINTS_0 -NotePropertyValue 0
Assert-Rejected 'Joints' $joints 'JOINTS_0'

$weights = Copy-Model $validSource
$weights.meshes[0].primitives[0].attributes | Add-Member -NotePropertyName WEIGHTS_0 -NotePropertyValue 0
Assert-Rejected 'Weights' $weights 'WEIGHTS_0'

$morph = Copy-Model $validSource
$morph.meshes[0].primitives[0] | Add-Member -NotePropertyName targets -NotePropertyValue @([pscustomobject]@{ POSITION = 0 })
Assert-Rejected 'MorphTarget' $morph 'morph targets'

$meshWeights = Copy-Model $validSource
$meshWeights.meshes[0] | Add-Member -NotePropertyName weights -NotePropertyValue @(0)
Assert-Rejected 'MeshWeights' $meshWeights 'mesh weights'

$nodeWeights = Copy-Model $validSource
$nodeWeights.nodes[0] | Add-Member -NotePropertyName weights -NotePropertyValue @(0)
Assert-Rejected 'NodeWeights' $nodeWeights 'node morph weights'

$requiredExtension = Copy-Model $validSource
$requiredExtension | Add-Member -NotePropertyName extensionsRequired -NotePropertyValue @('KHR_draco_mesh_compression')
Assert-Rejected 'RequiredExtension' $requiredExtension 'KHR_draco_mesh_compression'

$compressed = Copy-Model $validSource
$compressed.meshes[0].primitives[0] | Add-Member -NotePropertyName extensions `
    -NotePropertyValue ([pscustomobject]@{ KHR_draco_mesh_compression = [pscustomobject]@{} })
Assert-Rejected 'CompressedGeometry' $compressed 'KHR_draco_mesh_compression'

$textureTransform = Copy-Model $pbrSource
$textureTransform.materials[0].pbrMetallicRoughness.baseColorTexture | Add-Member -NotePropertyName extensions `
    -NotePropertyValue ([pscustomobject]@{ KHR_texture_transform = [pscustomobject]@{ offset = @(0, 0) } })
Assert-Rejected 'TextureTransform' $textureTransform 'KHR_texture_transform'

$secondUv = Copy-Model $pbrSource
$secondUv.materials[0].pbrMetallicRoughness.baseColorTexture | Add-Member -NotePropertyName texCoord -NotePropertyValue 1
Assert-Rejected 'TextureCoordinate' $secondUv 'texture coordinate set 1'

$sampler = Copy-Model $pbrSource
$sampler | Add-Member -NotePropertyName samplers -NotePropertyValue @([pscustomobject]@{ wrapS = 33071 })
$sampler.textures[0] | Add-Member -NotePropertyName sampler -NotePropertyValue 0
Assert-Rejected 'Sampler' $sampler 'sampler is not representable'

$embeddedImage = Copy-Model $pbrSource
Remove-Property $embeddedImage.images[0] 'uri'
$embeddedImage.images[0] | Add-Member -NotePropertyName bufferView -NotePropertyValue 0
$embeddedImage.images[0] | Add-Member -NotePropertyName mimeType -NotePropertyValue 'image/png'
Assert-Rejected 'EmbeddedImage' $embeddedImage 'embedded image bytes'

$cycle = Copy-Model $validSource
$cycle.nodes[0] | Add-Member -NotePropertyName children -NotePropertyValue @(0)
Assert-Rejected 'NodeCycle' $cycle 'cycle'

$invalidChild = Copy-Model $validSource
$invalidChild.nodes[0] | Add-Member -NotePropertyName children -NotePropertyValue @(99)
Assert-Rejected 'InvalidChild' $invalidChild 'node index'

$singular = Copy-Model $validSource
$singular.nodes[0] | Add-Member -NotePropertyName scale -NotePropertyValue @(1, 0, 1)
Assert-Rejected 'SingularTransform' $singular 'singular'

$matrixTrs = Copy-Model $validSource
$matrixTrs.nodes[0] | Add-Member -NotePropertyName matrix -NotePropertyValue @(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
$matrixTrs.nodes[0] | Add-Member -NotePropertyName translation -NotePropertyValue @(1, 0, 0)
Assert-Rejected 'MatrixAndTrs' $matrixTrs 'combine matrix and TRS'

$hugeAccessor = Copy-Model $validSource
$hugeAccessor.accessors[0].count = 2147483647
Assert-Rejected 'HugeAccessor' $hugeAccessor 'accessor count'

$wrongKind = Copy-Model $validSource
$wrongKind.accessors[0].count = 'three'
Assert-Rejected 'WrongValueKind' $wrongKind 'malformed value'

$declaredBuffer = Copy-Model $validSource
$declaredBuffer.buffers[0].byteLength = 4
Assert-Rejected 'DeclaredBufferLength' $declaredBuffer 'declared buffer length'

$misaligned = Copy-Model $validSource
$misaligned.bufferViews[0].byteOffset = 1
$misaligned.bufferViews[0].byteLength = 35
Assert-Rejected 'AccessorAlignment' $misaligned 'accessor range or stride'

$invalidBase64 = Copy-Model $validSource
$invalidBase64.buffers[0].uri = 'data:application/octet-stream;base64,%%%'
Assert-Rejected 'InvalidBase64' $invalidBase64 'malformed value'

$missingBuffer = Copy-Model $validSource
$missingBuffer.buffers[0].uri = 'Missing.bin'
Assert-Rejected 'MissingExternalBuffer' $missingBuffer ''

& $assetTool model $validSource --format-version 2 -o $validOutput | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Atomic-publication setup conversion failed.' }
$priorHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $validOutput).Hash
$lock = [System.IO.File]::Open($validOutput, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read,
    [System.IO.FileShare]::None)

try {
    $lockedResult = Invoke-Asset @('model', $validSource, '--format-version', '2', '-o', $validOutput)
}
finally {
    $lock.Dispose()
}

if ($lockedResult.ExitCode -ne 2 -or (Get-FileHash -Algorithm SHA256 -LiteralPath $validOutput).Hash -cne $priorHash -or
    @(Get-ChildItem -LiteralPath $temporaryRoot -Filter 'Valid.sm3d.tmp-*').Count -ne 0) {
    throw 'Atomic publication did not preserve the prior output or clean temporary residue.'
}

$samePathResult = Invoke-Asset @('model', $validSource, '--format-version', '2', '-o', $validSource)
if ($samePathResult.ExitCode -ne 2) { throw 'AssetTool accepted identical input and output paths.' }

$oversizedGltf = Join-Path $temporaryRoot 'Oversized.gltf'
$stream = [System.IO.File]::Open($oversizedGltf, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)
try { $stream.SetLength(4MB + 1) } finally { $stream.Dispose() }
$oversizedGltfResult = Invoke-Asset @('model', $oversizedGltf, '--format-version', '2', '-o', $invalidOutput)
if ($oversizedGltfResult.ExitCode -ne 2 -or $oversizedGltfResult.Error -notmatch 'SMA1242') {
    throw 'Oversized textual glTF was not rejected before reading.'
}

$oversizedSm3d = Join-Path $temporaryRoot 'Oversized.sm3d'
$stream = [System.IO.File]::Open($oversizedSm3d, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)
try { $stream.SetLength(16MB + 1) } finally { $stream.Dispose() }
$oversizedInspectResult = Invoke-Asset @('inspect', $oversizedSm3d)
if ($oversizedInspectResult.ExitCode -ne 2 -or $oversizedInspectResult.Error -notmatch 'SMA1200') {
    throw 'Oversized SM3D inspect target was not rejected before reading.'
}

Write-Host 'Renderer3D M1.1 scene, material, unsupported-feature, input-safety, and atomic-publication tests passed.'
