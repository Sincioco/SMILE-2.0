[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$temporaryRoot = Join-Path $repositoryRoot 'artifacts\temp\renderer3d-pbr-fixtures'
$labRoot = Join-Path $repositoryRoot 'examples\Renderer3DPbrLab\Assets'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DPbrTests\Assets'
$modelTestRoot = Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Assets'

function Write-Texture([string]$Path, [string]$Kind) {
    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new(4, 4, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    try {
        for ($y = 0; $y -lt 4; $y++) {
            for ($x = 0; $x -lt 4; $x++) {
                if ($Kind -eq 'Base') {
                    $alpha = if ((($x + $y) % 2) -eq 0) { 255 } else { 0 }
                    $color = [System.Drawing.Color]::FromArgb($alpha, 48 + $x * 48, 80 + $y * 36, 220)
                }
                elseif ($Kind -eq 'Normal') {
                    $color = [System.Drawing.Color]::FromArgb(255, 96 + $x * 20, 112 + $y * 12, 244)
                }
                elseif ($Kind -eq 'Orm') {
                    $color = [System.Drawing.Color]::FromArgb(255, 192 + $y * 16, 32 + $x * 64, $y * 80)
                }
                else {
                    $amount = if ((($x + $y) % 3) -eq 0) { 220 } else { 12 }
                    $color = [System.Drawing.Color]::FromArgb(255, 0, $amount, $amount)
                }

                $bitmap.SetPixel($x, $y, $color)
            }
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function Write-ModelSource([string]$Path, [string]$BaseColorPath) {
    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)

    try {
        foreach ($value in @(
            -1.0, -1.0, 0.0,
             1.0, -1.0, 0.0,
             1.0,  1.0, 0.0,
            -1.0,  1.0, 0.0
        )) { $writer.Write([single]$value) }

        for ($index = 0; $index -lt 4; $index++) {
            $writer.Write([single]0)
            $writer.Write([single]0)
            $writer.Write([single]1)
        }

        foreach ($value in @(0.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0)) {
            $writer.Write([single]$value)
        }

        foreach ($value in @(0, 1, 2, 0, 2, 3)) { $writer.Write([uint16]$value) }

        for ($index = 0; $index -lt 4; $index++) {
            $writer.Write([single]1)
            $writer.Write([single]0)
            $writer.Write([single]0)
            $writer.Write([single]1)
        }

        $buffer = [System.Convert]::ToBase64String($stream.ToArray())
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }

    $json = @'
{"asset":{"version":"2.0","generator":"SMILE 2.0 M2 PBR fixture"},"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"name":"PbrLab","mesh":0}],"meshes":[{"name":"PbrLabMesh","primitives":[{"attributes":{"POSITION":0,"NORMAL":1,"TEXCOORD_0":2,"TANGENT":4},"indices":3,"material":0,"mode":4},{"attributes":{"POSITION":0,"NORMAL":1,"TEXCOORD_0":2,"TANGENT":4},"indices":3,"material":1,"mode":4}]}],"materials":[{"name":"MaskedDoubleSided","doubleSided":true,"alphaMode":"MASK","alphaCutoff":0.5,"emissiveFactor":[0.15,0.5,0.7],"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1],"metallicFactor":0.1,"roughnessFactor":0.7,"baseColorTexture":{"index":0},"metallicRoughnessTexture":{"index":2}},"normalTexture":{"index":1,"scale":1},"occlusionTexture":{"index":2,"strength":1},"emissiveTexture":{"index":3}},{"name":"SmoothMetal","doubleSided":false,"alphaMode":"OPAQUE","emissiveFactor":[0,0.1,0.15],"pbrMetallicRoughness":{"baseColorFactor":[0.65,0.8,1,1],"metallicFactor":0.9,"roughnessFactor":0.18,"baseColorTexture":{"index":0},"metallicRoughnessTexture":{"index":2}},"normalTexture":{"index":1,"scale":0.65},"occlusionTexture":{"index":2,"strength":0.8},"emissiveTexture":{"index":3}}],"textures":[{"source":0},{"source":1},{"source":2},{"source":3}],"images":[{"uri":"__BASE__"},{"uri":"Assets/Textures/Pbr-normal.png"},{"uri":"Assets/Textures/Pbr-orm.png"},{"uri":"Assets/Textures/Pbr-emissive.png"}],"buffers":[{"byteLength":204,"uri":"data:application/octet-stream;base64,__BUFFER__"}],"bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":48,"target":34962},{"buffer":0,"byteOffset":48,"byteLength":48,"target":34962},{"buffer":0,"byteOffset":96,"byteLength":32,"target":34962},{"buffer":0,"byteOffset":128,"byteLength":12,"target":34963},{"buffer":0,"byteOffset":140,"byteLength":64,"target":34962}],"accessors":[{"bufferView":0,"componentType":5126,"count":4,"type":"VEC3","min":[-1,-1,0],"max":[1,1,0]},{"bufferView":1,"componentType":5126,"count":4,"type":"VEC3"},{"bufferView":2,"componentType":5126,"count":4,"type":"VEC2"},{"bufferView":3,"componentType":5123,"count":6,"type":"SCALAR","min":[0],"max":[3]},{"bufferView":4,"componentType":5126,"count":4,"type":"VEC4"}]}
'@
    $json = $json.Replace('__BASE__', $BaseColorPath).Replace('__BUFFER__', $buffer)
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}

function Assert-Or-Publish([string]$Generated, [string]$Destination) {
    if ($Check) {
        if (-not (Test-Path -LiteralPath $Destination)) {
            throw "The deterministic PBR fixture is missing: $Destination"
        }

        $generatedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Generated).Hash
        $destinationHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Destination).Hash

        if ($generatedHash -cne $destinationHash) {
            throw "The deterministic PBR fixture differs from the generator: $Destination"
        }
    }
    else {
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Destination)) | Out-Null
        [System.IO.File]::Copy($Generated, $Destination, $true)
    }

    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $Destination).Hash
    Write-Output "Verified $Destination (SHA256 $hash)"
}

if (-not (Test-Path -LiteralPath $assetTool)) {
    throw "Build smileasset before generating Renderer3D PBR fixtures: $assetTool"
}

[System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
$textureRoot = Join-Path $temporaryRoot 'Textures'
[System.IO.Directory]::CreateDirectory($textureRoot) | Out-Null
$textures = [ordered]@{
    'Pbr-base-color.png' = 'Base'
    'Pbr-normal.png' = 'Normal'
    'Pbr-orm.png' = 'Orm'
    'Pbr-emissive.png' = 'Emissive'
}

foreach ($entry in $textures.GetEnumerator()) {
    Write-Texture (Join-Path $textureRoot $entry.Key) $entry.Value
}

$validSource = Join-Path $temporaryRoot 'PbrLab.gltf'
$missingSource = Join-Path $temporaryRoot 'PbrMissingTexture.gltf'
$wrongCaseSource = Join-Path $temporaryRoot 'PbrWrongCase.gltf'
$validModel = Join-Path $temporaryRoot 'PbrLab.sm3d'
$missingModel = Join-Path $temporaryRoot 'PbrMissingTexture.sm3d'
$wrongCaseModel = Join-Path $temporaryRoot 'PbrWrongCase.sm3d'
Write-ModelSource $validSource 'Assets/Textures/Pbr-base-color.png'
Write-ModelSource $missingSource 'Assets/Textures/Pbr-missing.png'
Write-ModelSource $wrongCaseSource 'Assets/Textures/pbr-base-color.png'

foreach ($conversion in @(
    @($validSource, $validModel),
    @($missingSource, $missingModel),
    @($wrongCaseSource, $wrongCaseModel)
)) {
    & $assetTool model $conversion[0] --format-version 2 -o $conversion[1]
    if ($LASTEXITCODE -ne 0) { throw "PBR fixture conversion failed: $($conversion[0])" }
}

foreach ($root in @($labRoot, $testRoot)) {
    Assert-Or-Publish $validModel (Join-Path $root 'PbrLab.sm3d')

    foreach ($name in $textures.Keys) {
        Assert-Or-Publish (Join-Path $textureRoot $name) (Join-Path $root "Textures\$name")
    }
}

foreach ($entry in $textures.GetEnumerator()) {
    Assert-Or-Publish `
        (Join-Path $textureRoot $entry.Key) `
        (Join-Path $modelTestRoot "Textures\$($entry.Key)")
}

Assert-Or-Publish $missingModel (Join-Path $testRoot 'PbrMissingTexture.sm3d')
Assert-Or-Publish $wrongCaseModel (Join-Path $testRoot 'PbrWrongCase.sm3d')
Assert-Or-Publish (Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Assets\Humanoid.sm3d') `
    (Join-Path $testRoot 'LegacyV1.sm3d')
