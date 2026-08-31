[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceRoot = Join-Path $repositoryRoot 'examples\Renderer3DAnimationV2Tests\Source'
$assetRoot = Join-Path $repositoryRoot 'examples\Renderer3DAnimationV2Tests\Assets'
$labAssetRoot = Join-Path $repositoryRoot 'examples\Renderer3DAnimationLab\Assets'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Add-Padding([System.IO.BinaryWriter]$Writer) {
    while (($Writer.BaseStream.Length % 4) -ne 0) {
        $Writer.Write([byte]0)
    }
}

function New-ActorGlb([int]$BoneCount) {
    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    $views = [System.Collections.Generic.List[object]]::new()
    $accessors = [System.Collections.Generic.List[object]]::new()

    function Add-Floats([single[]]$Values, [string]$Type, [int]$Count, [int]$Target = 0) {
        Add-Padding $writer
        $offset = [int]$stream.Position
        foreach ($value in $Values) { $writer.Write([single]$value) }
        $length = [int]$stream.Position - $offset
        $view = $views.Count
        $viewValue = [ordered]@{ buffer = 0; byteOffset = $offset; byteLength = $length }
        if ($Target -ne 0) { $viewValue.target = $Target }
        $views.Add($viewValue)
        $accessor = $accessors.Count
        $accessors.Add([ordered]@{ bufferView = $view; componentType = 5126; count = $Count; type = $Type })
        return $accessor
    }

    function Add-UShorts([uint16[]]$Values, [string]$Type, [int]$Count, [int]$Target = 0, [bool]$Normalized = $false) {
        Add-Padding $writer
        $offset = [int]$stream.Position
        foreach ($value in $Values) { $writer.Write([uint16]$value) }
        $length = [int]$stream.Position - $offset
        $view = $views.Count
        $viewValue = [ordered]@{ buffer = 0; byteOffset = $offset; byteLength = $length }
        if ($Target -ne 0) { $viewValue.target = $Target }
        $views.Add($viewValue)
        $accessor = $accessors.Count
        $accessorValue = [ordered]@{ bufferView = $view; componentType = 5123; count = $Count; type = $Type }
        if ($Normalized) { $accessorValue.normalized = $true }
        $accessors.Add($accessorValue)
        return $accessor
    }

    try {
        $position = Add-Floats ([single[]]@(-0.6, 0.0, 0.0, 0.6, 0.0, 0.0, 0.0, 1.6, 0.0)) 'VEC3' 3 34962
        $normal = Add-Floats ([single[]]@(0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0)) 'VEC3' 3 34962
        $uv = Add-Floats ([single[]]@(0.0, 1.0, 1.0, 1.0, 0.5, 0.0)) 'VEC2' 3 34962
        $joints = Add-UShorts ([uint16[]]@(0, 0, 0, 0, 0, 0, 0, 0, 67, 0, 0, 0)) 'VEC4' 3 34962
        $weights = Add-UShorts ([uint16[]]@(65535, 0, 0, 0, 65535, 0, 0, 0, 65535, 0, 0, 0)) 'VEC4' 3 34962 $true
        $indices = Add-UShorts ([uint16[]]@(0, 1, 2)) 'SCALAR' 3 34963

        $matrices = [System.Collections.Generic.List[single]]::new()
        for ($bone = 0; $bone -lt $BoneCount; $bone++) {
            $matrices.AddRange([single[]]@(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1))
        }
        $inverseBind = Add-Floats $matrices.ToArray() 'MAT4' $BoneCount
        $times = Add-Floats ([single[]]@(0.0, 1.0)) 'SCALAR' 2

        $clipOutputs = @()
        $clipOutputs += Add-Floats ([single[]]@(0, 0, 0, 0, 0, 0)) 'VEC3' 2
        $clipOutputs += Add-Floats ([single[]]@(0, 0, 0, 1, 0, 0)) 'VEC3' 2
        $clipOutputs += Add-Floats ([single[]]@(0, 0, 0, 1, 0, 0, 0.38268343, 0.92387953)) 'VEC4' 2
        $clipOutputs += Add-Floats ([single[]]@(0, 0, 0, 1, -0.25881905, 0, 0, 0.96592583)) 'VEC4' 2
        $clipOutputs += Add-Floats ([single[]]@(0, 0, 0, 0, 0.25, 0)) 'VEC3' 2
        $walkYaw = Add-Floats ([single[]]@(0, 0, 0, 1, 0, 0.25881905, 0, 0.96592583)) 'VEC4' 2
        $writer.Flush()
        $binary = $stream.ToArray()

        $nodes = @()
        for ($bone = 0; $bone -lt $BoneCount; $bone++) {
            $node = [ordered]@{ name = ('Bone{0:D2}' -f $bone) }
            if ($bone -eq 0) { $node.mesh = 0; $node.skin = 0 }
            if ($bone -lt $BoneCount - 1) { $node.children = @($bone + 1) }
            if ($bone -gt 0) { $node.translation = @(0.0, 0.02, 0.0) }
            $nodes += $node
        }

        $clipNames = @('Idle', 'Walk', 'Attack', 'Hit', 'Victory')
        $clipTargets = @(1, 0, [Math]::Min(67, $BoneCount - 1), [Math]::Min(10, $BoneCount - 1), [Math]::Min(20, $BoneCount - 1))
        $clipPaths = @('translation', 'translation', 'rotation', 'rotation', 'translation')
        $animations = @()
        for ($clip = 0; $clip -lt $clipNames.Count; $clip++) {
            if ($clip -eq 1) {
                $animations += [ordered]@{
                    name = $clipNames[$clip]
                    samplers = @(
                        [ordered]@{ input = $times; output = $clipOutputs[$clip]; interpolation = 'LINEAR' },
                        [ordered]@{ input = $times; output = $walkYaw; interpolation = 'LINEAR' }
                    )
                    channels = @(
                        [ordered]@{ sampler = 0; target = [ordered]@{ node = 0; path = 'translation' } },
                        [ordered]@{ sampler = 1; target = [ordered]@{ node = 0; path = 'rotation' } }
                    )
                }
            }
            else {
                $animations += [ordered]@{
                    name = $clipNames[$clip]
                    samplers = @([ordered]@{ input = $times; output = $clipOutputs[$clip]; interpolation = 'LINEAR' })
                    channels = @([ordered]@{ sampler = 0; target = [ordered]@{ node = $clipTargets[$clip]; path = $clipPaths[$clip] } })
                }
            }
        }

        $jsonObject = [ordered]@{
            asset = [ordered]@{ version = '2.0'; generator = 'SMILE 2.0 deterministic animation-v2 fixture generator' }
            scene = 0
            scenes = @([ordered]@{ nodes = @(0) })
            nodes = $nodes
            meshes = @([ordered]@{
                name = 'AnimationActor'
                primitives = @([ordered]@{
                    attributes = [ordered]@{ POSITION = $position; NORMAL = $normal; TEXCOORD_0 = $uv; JOINTS_0 = $joints; WEIGHTS_0 = $weights }
                    indices = $indices
                    material = 0
                    mode = 4
                })
            })
            materials = @([ordered]@{
                name = 'ActorPbr'
                pbrMetallicRoughness = [ordered]@{ baseColorFactor = @(0.15, 0.35, 0.8, 1.0); metallicFactor = 0.25; roughnessFactor = 0.45 }
                emissiveFactor = @(0.02, 0.04, 0.1)
                doubleSided = $true
            })
            skins = @([ordered]@{ name = 'ActorSkin'; inverseBindMatrices = $inverseBind; skeleton = 0; joints = @(0..($BoneCount - 1)) })
            animations = $animations
            buffers = @([ordered]@{ byteLength = $binary.Length })
            bufferViews = $views.ToArray()
            accessors = $accessors.ToArray()
        }
        $json = $jsonObject | ConvertTo-Json -Depth 20 -Compress
        $jsonBytes = $utf8.GetBytes($json)
        $jsonPadding = (4 - ($jsonBytes.Length % 4)) % 4
        $binaryPadding = (4 - ($binary.Length % 4)) % 4
        $totalLength = 12 + 8 + $jsonBytes.Length + $jsonPadding + 8 + $binary.Length + $binaryPadding
        $glbStream = [System.IO.MemoryStream]::new()
        $glbWriter = [System.IO.BinaryWriter]::new($glbStream)
        try {
            $glbWriter.Write([uint32]0x46546C67)
            $glbWriter.Write([uint32]2)
            $glbWriter.Write([uint32]$totalLength)
            $glbWriter.Write([uint32]($jsonBytes.Length + $jsonPadding))
            $glbWriter.Write([uint32]0x4E4F534A)
            $glbWriter.Write($jsonBytes)
            for ($index = 0; $index -lt $jsonPadding; $index++) { $glbWriter.Write([byte]0x20) }
            $glbWriter.Write([uint32]($binary.Length + $binaryPadding))
            $glbWriter.Write([uint32]0x004E4942)
            $glbWriter.Write($binary)
            for ($index = 0; $index -lt $binaryPadding; $index++) { $glbWriter.Write([byte]0) }
            $glbWriter.Flush()
            return ,$glbStream.ToArray()
        }
        finally {
            $glbWriter.Dispose()
            $glbStream.Dispose()
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

function Publish-Bytes([string]$Path, [byte[]]$Bytes) {
    if ($Check) {
        if (-not (Test-Path -LiteralPath $Path)) { throw "Missing deterministic fixture: $Path" }
        $existing = [System.IO.File]::ReadAllBytes($Path)
        if ($existing.Length -ne $Bytes.Length) { throw "Deterministic fixture differs: $Path" }
        for ($index = 0; $index -lt $Bytes.Length; $index++) {
            if ($existing[$index] -ne $Bytes[$index]) { throw "Deterministic fixture differs: $Path" }
        }
    }
    else {
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
        [System.IO.File]::WriteAllBytes($Path, $Bytes)
    }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
    Write-Output "Verified $Path ($($Bytes.Length) bytes, SHA256 $hash)"
}

function Assert-EqualFiles([string]$Expected, [string]$Actual) {
    if (-not (Test-Path -LiteralPath $Expected) -or -not (Test-Path -LiteralPath $Actual)) {
        throw "Missing deterministic comparison input: $Expected or $Actual"
    }
    $expectedBytes = [System.IO.File]::ReadAllBytes($Expected)
    $actualBytes = [System.IO.File]::ReadAllBytes($Actual)
    if ($expectedBytes.Length -ne $actualBytes.Length) { throw "Deterministic output differs: $Actual" }
    for ($index = 0; $index -lt $expectedBytes.Length; $index++) {
        if ($expectedBytes[$index] -ne $actualBytes[$index]) { throw "Deterministic output differs: $Actual" }
    }
}

function New-MalformedSm3d([string]$Path, [string]$ChunkId, [string]$Mode) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $chunkCount = [System.BitConverter]::ToUInt32($bytes, 20)
    $chunkOffset = -1
    for ($index = 0; $index -lt $chunkCount; $index++) {
        $directory = 64 + $index * 32
        $id = $utf8.GetString($bytes, $directory, 4)
        if ($id -eq $ChunkId) { $chunkOffset = $directory; break }
    }
    if ($chunkOffset -lt 0) { throw "Chunk $ChunkId was not found in $Path" }
    if ($Mode -eq 'UnknownChunk') {
        $bytes[$chunkOffset] = [byte][char]'X'
    }
    elseif ($Mode -eq 'BadWeight') {
        $payload = [System.BitConverter]::ToUInt32($bytes, $chunkOffset + 8)
        $bytes[$payload + 8] = 254
    }
    else {
        throw "Unknown malformed fixture mode: $Mode"
    }
    $checksum = [uint32]2166136261
    for ($index = 64; $index -lt $bytes.Length; $index++) {
        $checksum = [uint32](([uint64]($checksum -bxor [uint32]$bytes[$index]) * [uint64]16777619) % [uint64]4294967296)
    }
    [System.BitConverter]::GetBytes($checksum).CopyTo($bytes, 16)
    return ,$bytes
}

$descriptorObject = [ordered]@{
    version = 1
    sampleRate = 30
    clips = [ordered]@{
        Idle = [ordered]@{ loop = $true }
        Walk = [ordered]@{
            loop = $true
            events = @(
                [ordered]@{ timeMs = 250; name = 'Footstep'; value = 1 },
                [ordered]@{ timeMs = 250; name = 'Footstep'; value = 2 }
            )
            rootMotion = [ordered]@{ node = 'Bone00'; translation = @('X', 'Z'); yaw = $true; removeFromPose = $true }
        }
        Attack = [ordered]@{ events = @([ordered]@{ timeMs = 500; name = 'SwordImpact'; value = 7 }) }
        Hit = [ordered]@{}
        Victory = [ordered]@{}
    }
    sockets = [ordered]@{
        SwordTip = [ordered]@{ node = 'Bone67'; translation = @(0.0, 0.25, 0.0) }
    }
}
$descriptorBytes = $utf8.GetBytes(($descriptorObject | ConvertTo-Json -Depth 20 -Compress) + "`n")

Publish-Bytes (Join-Path $sourceRoot 'AnimationActor68.glb') (New-ActorGlb 68)
Publish-Bytes (Join-Path $sourceRoot 'AnimationActor128.glb') (New-ActorGlb 128)
Publish-Bytes (Join-Path $sourceRoot 'AnimationActor129.glb') (New-ActorGlb 129)
Publish-Bytes (Join-Path $sourceRoot 'AnimationActor68.sm3d.json') $descriptorBytes

if (-not (Test-Path -LiteralPath $assetTool)) { throw "Build smileasset first: $assetTool" }

if (-not $Check) {
    [System.IO.Directory]::CreateDirectory($assetRoot) | Out-Null
    & $assetTool model (Join-Path $sourceRoot 'AnimationActor68.glb') --descriptor (Join-Path $sourceRoot 'AnimationActor68.sm3d.json') -o (Join-Path $assetRoot 'AnimationActor68.sm3d')
    if ($LASTEXITCODE -ne 0) { throw 'The 68-bone animation fixture conversion failed.' }
    & $assetTool model (Join-Path $sourceRoot 'AnimationActor128.glb') -o (Join-Path $assetRoot 'AnimationActor128.sm3d')
    if ($LASTEXITCODE -ne 0) { throw 'The 128-bone animation fixture conversion failed.' }
    [System.IO.Directory]::CreateDirectory($labAssetRoot) | Out-Null
    Copy-Item -LiteralPath (Join-Path $assetRoot 'AnimationActor68.sm3d') -Destination (Join-Path $labAssetRoot 'AnimationActor68.sm3d') -Force
}
else {
    $temporaryRoot = Join-Path (Join-Path $repositoryRoot 'artifacts\temp') ([System.IO.Path]::GetRandomFileName())
    [System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
    try {
        $temporary68 = Join-Path $temporaryRoot 'AnimationActor68.sm3d'
        $temporary128 = Join-Path $temporaryRoot 'AnimationActor128.sm3d'
        & $assetTool model (Join-Path $sourceRoot 'AnimationActor68.glb') --descriptor (Join-Path $sourceRoot 'AnimationActor68.sm3d.json') -o $temporary68
        if ($LASTEXITCODE -ne 0) { throw 'The 68-bone deterministic check conversion failed.' }
        & $assetTool model (Join-Path $sourceRoot 'AnimationActor128.glb') -o $temporary128
        if ($LASTEXITCODE -ne 0) { throw 'The 128-bone deterministic check conversion failed.' }
        Assert-EqualFiles (Join-Path $assetRoot 'AnimationActor68.sm3d') $temporary68
        Assert-EqualFiles (Join-Path $assetRoot 'AnimationActor128.sm3d') $temporary128
        Assert-EqualFiles (Join-Path $assetRoot 'AnimationActor68.sm3d') (Join-Path $labAssetRoot 'AnimationActor68.sm3d')
    }
    finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

$validActor = Join-Path $assetRoot 'AnimationActor68.sm3d'
Publish-Bytes (Join-Path $assetRoot 'AnimationPartialGroup.sm3d') (New-MalformedSm3d $validActor 'ROOT' 'UnknownChunk')
Publish-Bytes (Join-Path $assetRoot 'AnimationBadWeights.sm3d') (New-MalformedSm3d $validActor 'SKIN' 'BadWeight')

if (Test-Path -LiteralPath (Join-Path $assetRoot 'AnimationActor68.sm3d')) {
    Write-Output "Verified $(Join-Path $assetRoot 'AnimationActor68.sm3d')"
}
if (Test-Path -LiteralPath (Join-Path $assetRoot 'AnimationActor128.sm3d')) {
    Write-Output "Verified $(Join-Path $assetRoot 'AnimationActor128.sm3d')"
}
