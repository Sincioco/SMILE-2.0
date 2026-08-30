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
$glbFixture = Join-Path $testRoot 'Source\M0Triangle.glb'

function Convert-TestModel([string]$Name) {
    & $assetTool model (Join-Path $testRoot "Source\$Name.gltf") -o (Join-Path $testRoot "Assets\$Name.sm3d")
    if ($LASTEXITCODE -ne 0) { throw "smileasset failed for $Name." }
}

Push-Location $repositoryRoot
try {
    & $glbGenerator -OutputPath $glbFixture -Check

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
