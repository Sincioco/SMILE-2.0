[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DModelTests'
$project = Join-Path $testRoot 'Renderer3DModelTests.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DModelTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DModelTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DModelTests'
$invalidOutput = Join-Path $repositoryRoot 'artifacts\temp\InvalidModel.sm3d'
$glbGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-glb-fixture.ps1'
$v2FixtureGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-v2-fixtures.ps1'
$v2BoundaryTests = Join-Path $repositoryRoot 'scripts\test-renderer3d-v2-boundaries.ps1'
$glbFixture = Join-Path $testRoot 'Source\M0Triangle.glb'
$m0GltfFixture = Join-Path $testRoot 'Source\M0Triangle.gltf'
$pbrFixture = Join-Path $testRoot 'Source\PbrTriangle.gltf'
$v2First = Join-Path $repositoryRoot 'artifacts\temp\M0TriangleV2-first.sm3d'
$v2Second = Join-Path $repositoryRoot 'artifacts\temp\M0TriangleV2-second.sm3d'
$v2Equivalent = Join-Path $repositoryRoot 'artifacts\temp\M0TriangleV2-equivalent.sm3d'
$pbrFirst = Join-Path $repositoryRoot 'artifacts\temp\PbrTriangleV2-first.sm3d'
$pbrSecond = Join-Path $repositoryRoot 'artifacts\temp\PbrTriangleV2-second.sm3d'

function Convert-TestModel([string]$Name) {
    & $assetTool model (Join-Path $testRoot "Source\$Name.gltf") -o (Join-Path $testRoot "Assets\$Name.sm3d")
    if ($LASTEXITCODE -ne 0) { throw "smileasset failed for $Name." }
}

function Invoke-ExpectedAssetFailure([string]$Description, [string[]]$Arguments) {
    & $assetTool @Arguments 2>$null
    if ($LASTEXITCODE -eq 0) { throw "smileasset accepted $Description." }
}

Push-Location $repositoryRoot
try {
    & $glbGenerator -OutputPath $glbFixture -Check
    & $v2FixtureGenerator -Check
    & $v2BoundaryTests

    & $assetTool model $glbFixture -o $v2First
    if ($LASTEXITCODE -ne 0) { throw 'First GLB-to-SM3D-v2 conversion failed.' }
    & $assetTool model $glbFixture -o $v2Second
    if ($LASTEXITCODE -ne 0) { throw 'Second GLB-to-SM3D-v2 conversion failed.' }
    & $assetTool model $m0GltfFixture --format-version 2 -o $v2Equivalent
    if ($LASTEXITCODE -ne 0) { throw 'Equivalent glTF-to-SM3D-v2 conversion failed.' }
    & $assetTool model $pbrFixture --format-version 2 -o $pbrFirst
    if ($LASTEXITCODE -ne 0) { throw 'First PBR glTF-to-SM3D-v2 conversion failed.' }
    & $assetTool model $pbrFixture --format-version 2 -o $pbrSecond
    if ($LASTEXITCODE -ne 0) { throw 'Second PBR glTF-to-SM3D-v2 conversion failed.' }

    $m0V2Hashes = @($v2First, $v2Second, $v2Equivalent) |
        ForEach-Object { (Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash }
    if (($m0V2Hashes | Select-Object -Unique).Count -ne 1) {
        throw 'Equivalent GLB/glTF conversion or repeated SM3D v2 conversion was not byte-identical.'
    }

    $pbrV2Hashes = @($pbrFirst, $pbrSecond) |
        ForEach-Object { (Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash }
    if (($pbrV2Hashes | Select-Object -Unique).Count -ne 1) {
        throw 'Repeated PBR SM3D v2 conversion was not byte-identical.'
    }

    $m0Inspection = (& $assetTool inspect $v2First) -join "`n"
    if ($LASTEXITCODE -ne 0 -or
        $m0Inspection -notmatch 'Version: 2' -or
        $m0Inspection -notmatch 'Vertices: 3' -or
        $m0Inspection -notmatch 'Tangents: \+0 -3') {
        throw 'SM3D v2 generated-tangent inspection did not report the expected metadata.'
    }

    $pbrInspection = (& $assetTool inspect $pbrFirst) -join "`n"
    if ($LASTEXITCODE -ne 0 -or
        $pbrInspection -notmatch 'TextureReferences: 4' -or
        $pbrInspection -notmatch 'NormalStrength 0.75' -or
        $pbrInspection -notmatch 'Alpha MASK' -or
        $pbrInspection -notmatch 'DoubleSided True') {
        throw 'SM3D v2 PBR inspection did not report the expected metadata.'
    }

    Convert-TestModel 'Humanoid'
    Convert-TestModel 'Dragon'
    $firstHashes = @(
        (Get-FileHash -Algorithm SHA256 (Join-Path $testRoot 'Assets\Humanoid.sm3d')).Hash,
        (Get-FileHash -Algorithm SHA256 (Join-Path $testRoot 'Assets\Dragon.sm3d')).Hash
    )
    Convert-TestModel 'Humanoid'
    Convert-TestModel 'Dragon'
    $secondHashes = @(
        (Get-FileHash -Algorithm SHA256 (Join-Path $testRoot 'Assets\Humanoid.sm3d')).Hash,
        (Get-FileHash -Algorithm SHA256 (Join-Path $testRoot 'Assets\Dragon.sm3d')).Hash
    )
    if (Compare-Object $firstHashes $secondHashes) { throw 'smileasset output is not deterministic.' }

    foreach ($fixture in @('InvalidIndex', 'InvalidMaterial')) {
        & $assetTool model (Join-Path $testRoot "Source\$fixture.gltf") -o $invalidOutput 2>$null
        if ($LASTEXITCODE -eq 0) { throw "smileasset accepted $fixture.gltf." }
        Invoke-ExpectedAssetFailure "$fixture.gltf as v2" @(
            'model', (Join-Path $testRoot "Source\$fixture.gltf"), '--format-version', '2', '-o', $invalidOutput
        )
    }

    foreach ($fixture in @(
        'BadGlbMagic',
        'BadGlbVersion',
        'BadGlbLength',
        'BadGlbChunk',
        'BadGlbAlignment',
        'BadGlbDuplicateJson',
        'BadGlbBinReference'
    )) {
        Invoke-ExpectedAssetFailure "$fixture.glb" @(
            'model', (Join-Path $testRoot "Source\$fixture.glb"), '-o', $invalidOutput
        )
    }

    foreach ($fixture in @(
        'BadV2Header',
        'BadV2Size',
        'BadV2Checksum',
        'BadV2Directory',
        'BadV2ChunkRange',
        'BadV2Count',
        'BadV2Stride',
        'MissingRequiredV2',
        'UnknownRequiredV2',
        'NonPrintableNulV2',
        'NonPrintableHighV2',
        'NonPrintableControlV2',
        'DuplicateChunkV2',
        'InvalidNormalBasisV2',
        'InvalidTangentBasisV2',
        'InvalidOrthogonalBasisV2',
        'InvalidHandednessV2',
        'InvalidStructureV1'
    )) {
        Invoke-ExpectedAssetFailure "$fixture.sm3d inspection" @(
            'inspect', (Join-Path $testRoot "Assets\$fixture.sm3d")
        )
    }

    & $assetTool inspect (Join-Path $testRoot 'Assets\UnknownOptionalV2.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'SM3D v2 inspection rejected an unknown optional chunk.' }

    $webSource = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs') -Raw
    $drawStart = $webSource.IndexOf('function renderer3DDraw(handle)', [System.StringComparison]::Ordinal)
    $drawEnd = $webSource.IndexOf('function renderer3DEnd()', $drawStart, [System.StringComparison]::Ordinal)

    if ($drawStart -lt 0 -or $drawEnd -le $drawStart) {
        throw 'Renderer3D Web draw function was not found for hot-path inspection.'
    }

    $drawSource = $webSource.Substring($drawStart, $drawEnd - $drawStart)

    if ($drawSource.Contains('new Float32Array') -or $drawSource.Contains('.map(') -or $drawSource.Contains('...')) {
        throw 'Renderer3D Web draw path still contains per-draw array or typed-array construction.'
    }

    $pbrJson = Get-Content -LiteralPath $pbrFixture -Raw
    $invalidSources = [ordered]@{
        'AccessorRange' = $pbrJson.Replace('"byteLength":36,"target":34962', '"byteLength":999999,"target":34962')
        'AccessorComponent' = $pbrJson.Replace('"componentType":5126', '"componentType":5123')
        'AccessorStride' = $pbrJson.Replace('"byteLength":36,"target":34962', '"byteLength":36,"byteStride":4,"target":34962')
        'TextureTraversal' = $pbrJson.Replace('Assets/Textures/Pbr-base-color.png', '../Pbr-base-color.png')
        'TextureAbsolute' = $pbrJson.Replace('Assets/Textures/Pbr-base-color.png', 'C:/Pbr-base-color.png')
        'NumericRange' = $pbrJson.Replace('"metallicFactor":0.35', '"metallicFactor":1.5')
    }

    $dataPrefix = 'data:application/octet-stream;base64,'
    $dataStart = $pbrJson.IndexOf($dataPrefix, [System.StringComparison]::Ordinal) + $dataPrefix.Length
    $dataEnd = $pbrJson.IndexOf('"', $dataStart)
    $dataText = $pbrJson.Substring($dataStart, $dataEnd - $dataStart)
    $nanBytes = [System.Convert]::FromBase64String($dataText)
    [System.BitConverter]::GetBytes([single]::NaN).CopyTo($nanBytes, 0)
    $invalidSources['NotANumber'] = $pbrJson.Replace($dataText, [System.Convert]::ToBase64String($nanBytes))
    $infinityBytes = [System.Convert]::FromBase64String($dataText)
    [System.BitConverter]::GetBytes([single]::PositiveInfinity).CopyTo($infinityBytes, 0)
    $invalidSources['Infinity'] = $pbrJson.Replace($dataText, [System.Convert]::ToBase64String($infinityBytes))

    foreach ($entry in $invalidSources.GetEnumerator()) {
        $invalidSource = Join-Path $repositoryRoot "artifacts\temp\Invalid$($entry.Key).gltf"
        [System.IO.File]::WriteAllText($invalidSource, $entry.Value, [System.Text.UTF8Encoding]::new($false))
        Invoke-ExpectedAssetFailure "$($entry.Key) glTF" @(
            'model', $invalidSource, '--format-version', '2', '-o', $invalidOutput
        )
    }

    & $compiler --project $project --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native model test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 30 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native model test execution failed.' }
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $nativeText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($nativeText -cne $expectedText) { throw "Renderer3D native model assertions failed: $nativeText" }

    & $compiler --project $project --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web model test compilation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 30000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web model assertions failed.' }

    Write-Host 'Renderer3D deterministic conversion, validation, native/Web loading, sharing, and reload tests passed.'
}
finally {
    Pop-Location
}
