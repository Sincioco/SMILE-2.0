[CmdletBinding()]
param(
    [string]$OutputPath,
    [string]$GltfOutputPath,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Source\M0Triangle.glb'
}

if ([string]::IsNullOrWhiteSpace($GltfOutputPath)) {
    $GltfOutputPath = Join-Path $repositoryRoot 'examples\Renderer3DModelTests\Source\M0Triangle.gltf'
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$resolvedGltfOutput = [System.IO.Path]::GetFullPath($GltfOutputPath)
$json = '{"asset":{"version":"2.0","generator":"SMILE 2.0 deterministic GLB fixture generator"},"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"name":"M0Triangle","mesh":0}],"meshes":[{"name":"M0Triangle","primitives":[{"attributes":{"POSITION":0,"NORMAL":1,"TEXCOORD_0":2},"indices":3,"material":0,"mode":4}]}],"materials":[{"name":"M0Material","pbrMetallicRoughness":{"baseColorFactor":[0.25,0.5,1,1],"metallicFactor":0.2,"roughnessFactor":0.7}}],"buffers":[{"byteLength":104}],"bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":36,"target":34962},{"buffer":0,"byteOffset":36,"byteLength":36,"target":34962},{"buffer":0,"byteOffset":72,"byteLength":24,"target":34962},{"buffer":0,"byteOffset":96,"byteLength":6,"target":34963}],"accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3","min":[-1,0,0],"max":[1,1,0]},{"bufferView":1,"componentType":5126,"count":3,"type":"VEC3"},{"bufferView":2,"componentType":5126,"count":3,"type":"VEC2"},{"bufferView":3,"componentType":5123,"count":3,"type":"SCALAR","min":[0],"max":[2]}]}'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$jsonBytes = $utf8.GetBytes($json)
$jsonPadding = (4 - ($jsonBytes.Length % 4)) % 4

$binaryStream = [System.IO.MemoryStream]::new()
$binaryWriter = [System.IO.BinaryWriter]::new($binaryStream)

try {
    foreach ($value in @(
        -1.0, 0.0, 0.0,
        1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,
        0.0, 0.0, 1.0,
        0.0, 0.0,
        1.0, 0.0,
        0.5, 1.0
    )) {
        $binaryWriter.Write([single]$value)
    }

    $binaryWriter.Write([uint16]0)
    $binaryWriter.Write([uint16]1)
    $binaryWriter.Write([uint16]2)
    $binaryWriter.Write([uint16]0)
    $binaryWriter.Flush()
    $binaryBytes = $binaryStream.ToArray()
}
finally {
    $binaryWriter.Dispose()
    $binaryStream.Dispose()
}

$totalLength = 12 + 8 + $jsonBytes.Length + $jsonPadding + 8 + $binaryBytes.Length
$glbStream = [System.IO.MemoryStream]::new()
$glbWriter = [System.IO.BinaryWriter]::new($glbStream)

try {
    $glbWriter.Write([uint32]0x46546C67)
    $glbWriter.Write([uint32]2)
    $glbWriter.Write([uint32]$totalLength)
    $glbWriter.Write([uint32]($jsonBytes.Length + $jsonPadding))
    $glbWriter.Write([uint32]0x4E4F534A)
    $glbWriter.Write($jsonBytes)

    for ($index = 0; $index -lt $jsonPadding; $index++) {
        $glbWriter.Write([byte]0x20)
    }

    $glbWriter.Write([uint32]$binaryBytes.Length)
    $glbWriter.Write([uint32]0x004E4942)
    $glbWriter.Write($binaryBytes)
    $glbWriter.Flush()
    $generatedBytes = $glbStream.ToArray()
}
finally {
    $glbWriter.Dispose()
    $glbStream.Dispose()
}

if ($generatedBytes.Length -ne $totalLength) {
    throw "Generated GLB length $($generatedBytes.Length) did not match declared length $totalLength."
}

if ($Check) {
    if (-not (Test-Path -LiteralPath $resolvedOutput)) {
        throw "The deterministic GLB fixture is missing: $resolvedOutput"
    }

    $existingBytes = [System.IO.File]::ReadAllBytes($resolvedOutput)
    $fixtureMatches = $existingBytes.Length -eq $generatedBytes.Length

    for ($index = 0; $fixtureMatches -and $index -lt $existingBytes.Length; $index++) {
        $fixtureMatches = $existingBytes[$index] -eq $generatedBytes[$index]
    }

    if (-not $fixtureMatches) {
        throw "The deterministic GLB fixture differs from the generator: $resolvedOutput"
    }
}
else {
    $outputDirectory = Split-Path -Parent $resolvedOutput
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    [System.IO.File]::WriteAllBytes($resolvedOutput, $generatedBytes)
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $resolvedOutput).Hash
$gltf = $json.Replace(
    '"buffers":[{"byteLength":104}]',
    '"buffers":[{"byteLength":104,"uri":"data:application/octet-stream;base64,' +
        [System.Convert]::ToBase64String($binaryBytes) + '"}]'
)
$gltfBytes = $utf8.GetBytes($gltf + "`n")

if ($Check) {
    if (-not (Test-Path -LiteralPath $resolvedGltfOutput)) {
        throw "The deterministic glTF fixture is missing: $resolvedGltfOutput"
    }

    $existingGltfBytes = [System.IO.File]::ReadAllBytes($resolvedGltfOutput)
    $gltfMatches = $existingGltfBytes.Length -eq $gltfBytes.Length

    for ($index = 0; $gltfMatches -and $index -lt $existingGltfBytes.Length; $index++) {
        $gltfMatches = $existingGltfBytes[$index] -eq $gltfBytes[$index]
    }

    if (-not $gltfMatches) {
        throw "The deterministic glTF fixture differs from the generator: $resolvedGltfOutput"
    }
}
else {
    $gltfOutputDirectory = Split-Path -Parent $resolvedGltfOutput
    [System.IO.Directory]::CreateDirectory($gltfOutputDirectory) | Out-Null
    [System.IO.File]::WriteAllBytes($resolvedGltfOutput, $gltfBytes)
}

$gltfHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $resolvedGltfOutput).Hash
Write-Output "Deterministic GLB fixture verified: $resolvedOutput"
Write-Output "Bytes: $($generatedBytes.Length)"
Write-Output "SHA256: $hash"
Write-Output "Equivalent glTF fixture verified: $resolvedGltfOutput"
Write-Output "Bytes: $($gltfBytes.Length)"
Write-Output "SHA256: $gltfHash"
