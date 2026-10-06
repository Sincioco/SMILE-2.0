[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testRoot = Join-Path $repository 'artifacts\tests\model3d-part-limit'
$assetTool = Join-Path $repository 'artifacts\compiler\smileasset.dll'
$null = New-Item -ItemType Directory -Force -Path $testRoot

# Regression for Arin v5.8 exceeding the old per-part vertex/index ceilings.
# Repeated valid triangles keep the boundary fixture small and deterministic.
foreach ($count in @(100000, 100001)) {
    $name = "Limit$count"
    $binaryPath = Join-Path $testRoot "$name.bin"
    $writer = [IO.BinaryWriter]::new([IO.File]::Create($binaryPath))
    try {
        for ($index = 0; $index -lt $count; $index++) {
            $x = [single][int]($index % 3 -eq 1)
            $y = [single][int]($index % 3 -eq 2)
            foreach ($value in @($x, $y, 0, 0, 0, 1, $x, $y, 1, 0, 0, 1)) {
                $writer.Write([single]$value)
            }
        }
        for ($index = 0; $index -lt 300000; $index++) {
            $writer.Write([uint32]($count - 3 + $index % 3))
        }
    } finally { $writer.Dispose() }
    $model = @{
        asset = @{ version = '2.0' }; scene = 0
        scenes = @(@{ nodes = @(0) }); nodes = @(@{ mesh = 0 })
        meshes = @(@{ primitives = @(@{
            attributes = @{ POSITION = 0; NORMAL = 1; TEXCOORD_0 = 2; TANGENT = 4 }
            indices = 3; mode = 4
        }) })
        buffers = @(@{ uri = "$name.bin"; byteLength = $count * 48 + 1200000 })
        bufferViews = @(
            @{ buffer = 0; byteOffset = 0; byteLength = $count * 48; byteStride = 48; target = 34962 },
            @{ buffer = 0; byteOffset = $count * 48; byteLength = 1200000; target = 34963 }
        )
        accessors = @(
            @{ bufferView = 0; byteOffset = 0; componentType = 5126; count = $count; type = 'VEC3' },
            @{ bufferView = 0; byteOffset = 12; componentType = 5126; count = $count; type = 'VEC3' },
            @{ bufferView = 0; byteOffset = 24; componentType = 5126; count = $count; type = 'VEC2' },
            @{ bufferView = 1; byteOffset = 0; componentType = 5125; count = 300000; type = 'SCALAR' },
            @{ bufferView = 0; byteOffset = 32; componentType = 5126; count = $count; type = 'VEC4' }
        )
    }
    $source = Join-Path $testRoot "$name.gltf"
    $output = Join-Path $testRoot "$name.sm3d"
    $model | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $source
    $log = (& dotnet $assetTool model $source --format-version 2 -o $output 2>&1) -join "`n"
    $exitCode = $LASTEXITCODE
    Set-Content -LiteralPath (Join-Path $testRoot "$name.log") -Value $log
    if ($count -eq 100000) {
        if ($exitCode -ne 0) { throw "100,000 vertices / 300,000 indices rejected: $log" }
        & dotnet $assetTool inspect $output | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Boundary output failed inspection.' }
    } elseif ($exitCode -eq 0 -or $log -notmatch 'SMA1276') {
        throw "Expected rejection above 100,000 vertices: $log"
    }
}
Write-Host 'Model part boundary PASS: 100000 accepted; 100001 rejected; 300000 indices retained.'
