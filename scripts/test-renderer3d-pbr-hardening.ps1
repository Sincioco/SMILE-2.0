[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DPbrHardeningTests'
$testProject = Join-Path $testRoot 'Renderer3DPbrHardeningTests.smileproj'
$expectedNormal = Join-Path $testRoot 'expected-normal.txt'
$expectedForced = Join-Path $testRoot 'expected-forced-failure.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DPbrHardeningTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DPbrHardeningTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DPbrHardeningTests'
$fixtureGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-pbr-fixtures.ps1'
$runtimeSource = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'

function Assert-Output([string]$ExpectedPath, [string]$ActualPath, [string]$Label) {
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()

    if ($actualText -cne $expectedText) {
        throw "$Label assertions failed: $actualText"
    }
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D PBR hardening gate.'
}

Push-Location $repositoryRoot
try {
    & $fixtureGenerator -Check

    $webSource = Get-Content -LiteralPath $runtimeSource -Raw
    $drawStart = $webSource.IndexOf('function renderer3DDrawPbr', [System.StringComparison]::Ordinal)
    $drawEnd = $webSource.IndexOf('function renderer3DBegin', $drawStart, [System.StringComparison]::Ordinal)

    if ($drawStart -lt 0 -or $drawEnd -le $drawStart) {
        throw 'The Renderer3D Web PBR draw path was not found.'
    }

    $drawSource = $webSource.Substring($drawStart, $drawEnd - $drawStart)

    foreach ($forbidden in @('new Float32Array', '.map(', '...', 'generateMipmap', 'renderer3DCompile')) {
        if ($drawSource.Contains($forbidden)) {
            throw "The Renderer3D Web PBR draw path contains forbidden hot-path text: $forbidden"
        }
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native PBR hardening compilation failed.' }

    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native PBR hardening execution failed.' }
    Assert-Output $expectedNormal $nativeLog 'Renderer3D native PBR hardening'

    $env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE = '1'
    try {
        & 'scripts\run-bounded-test.cmd' 60 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'Renderer3D forced native PBR failure execution failed.' }
        Assert-Output $expectedForced $nativeLog 'Renderer3D forced native PBR failure'
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR hardening compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR hardening game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR hardening runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expectedNormal --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web PBR hardening assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expectedForced --timeout 60000 --renderer3d `
        --force-renderer3d-pbr-failure
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D forced Web PBR failure assertions failed.' }

    Write-Host 'Renderer3D PBR hardening native/Web failure, fallback, ownership, transform, skinning, and lifecycle tests passed.'
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
