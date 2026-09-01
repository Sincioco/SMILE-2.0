[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DPostProcessingTests'
$testProject = Join-Path $testRoot 'Renderer3DPostProcessingTests.smileproj'
$labProject = Join-Path $repositoryRoot 'examples\Renderer3DPostProcessingLab\Renderer3DPostProcessingLab.smileproj'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DPostProcessingTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DPostProcessingTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DPostProcessingTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\Renderer3DPostProcessingLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DPostProcessingLab'
$expectedNormal = Join-Path $testRoot 'expected-normal.txt'
$expectedHdrFallback = Join-Path $testRoot 'expected-hdr-fallback.txt'
$expectedShadowFallback = Join-Path $testRoot 'expected-shadow-fallback.txt'

function Assert-Output([string]$ExpectedPath, [string]$ActualPath, [string]$Label) {
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()

    if ($actualText -cne $expectedText) {
        throw "$Label assertions failed: $actualText"
    }
}

function Invoke-NativeM5Test([string]$ExpectedPath, [string]$Label) {
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw "$Label execution failed." }
    Assert-Output $ExpectedPath $nativeLog $Label
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D M5 gate.'
}

Push-Location $repositoryRoot
try {
    $graphicsSource = Get-Content -LiteralPath 'libraries\Smile.Simple3D\Graphics3D.smile' -Raw
    $characterSource = Get-Content -LiteralPath 'libraries\Smile.Simple3D\Character3D.smile' -Raw
    $sceneSource = Get-Content -LiteralPath 'libraries\Smile.Simple3D\Scene3D.smile' -Raw
    $nativeHeader = Get-Content -LiteralPath 'src\Smile.NativeRuntime\graphics\graphics3d.h' -Raw
    $nativeSource = Get-Content -LiteralPath 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp' -Raw
    $webSource = Get-Content -LiteralPath 'src\Smile.Compiler\WebOutputWriter.cs' -Raw

    if ($graphicsSource -notmatch 'COMMAND_CONFIGURE_POST = 113' -or
        $graphicsSource -notmatch 'COMMAND_M5_VALUE = 117' -or
        $nativeHeader -notmatch 'SMILE_3D_CONFIGURE_POST = 113' -or
        $nativeHeader -notmatch 'SMILE_3D_M5_VALUE = 117' -or
        $nativeSource -notmatch 'SMILE_3D_MAX_FRAME_SUBMISSIONS 512' -or
        $webSource -notmatch 'new Float64Array\(512\)' -or
        $webSource -notmatch 'case 117:') {
        throw 'Renderer3D append-only M5 ABI or fixed submission capacity changed.'
    }
    if ($nativeSource -notmatch 'SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE' -or
        $nativeSource -notmatch 'SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE' -or
        $webSource -notmatch 'SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE' -or
        $webSource -notmatch 'SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE') {
        throw 'Renderer3D independent M5 fallback hooks changed.'
    }
    if ($characterSource -notmatch 'Public Function SetShadows\(' -or
        $characterSource -notmatch 'Private Function ApplyShadowTransaction\(' -or
        $sceneSource -notmatch 'Public Function RenderProfileKey\(\)' -or
        $sceneSource -notmatch 'Public Function ShadowsEffective\(\)' -or
        $sceneSource -notmatch 'Public Function HdrEffective\(\)' -or
        $sceneSource -notmatch 'Public Function BloomEffective\(\)') {
        throw 'Character3D or Scene3D M5 integration changed.'
    }

    $beginStart = $webSource.IndexOf('function renderer3DBegin', [System.StringComparison]::Ordinal)
    $endFinish = $webSource.IndexOf('function renderer3DReset', $beginStart, [System.StringComparison]::Ordinal)
    if ($beginStart -lt 0 -or $endFinish -le $beginStart) {
        throw 'Renderer3D Web M5 frame path was not found.'
    }
    $frameSource = $webSource.Substring($beginStart, $endFinish - $beginStart)
    foreach ($forbidden in @('new Float32Array', 'new Float64Array', '.push(', '.map(', 'renderer3DCompile')) {
        if ($frameSource.Contains($forbidden)) {
            throw "Renderer3D Web M5 frame path contains forbidden hot-path text: $forbidden"
        }
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 native test compilation failed.' }

    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE -ErrorAction SilentlyContinue
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE -ErrorAction SilentlyContinue
    Invoke-NativeM5Test $expectedNormal 'Renderer3D M5 native normal path'

    $env:SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE = '1'
    try {
        Invoke-NativeM5Test $expectedHdrFallback 'Renderer3D M5 native HDR fallback'
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE -ErrorAction SilentlyContinue
    }

    $env:SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE = '1'
    try {
        Invoke-NativeM5Test $expectedShadowFallback 'Renderer3D M5 native shadow fallback'
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE -ErrorAction SilentlyContinue
    }

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 Web runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expectedNormal --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 Web normal assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expectedHdrFallback --timeout 60000 `
        --renderer3d --force-renderer3d-hdr-failure
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 Web HDR fallback assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expectedShadowFallback --timeout 60000 `
        --renderer3d --force-renderer3d-shadow-failure
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D M5 Web shadow fallback assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Post Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Post Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Post Lab Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Post Lab Web runtime JavaScript syntax validation failed.' }

    Write-Host 'Renderer3D M5 native/Web queue, shadow, animation palette, HDR, MSAA, tone mapping, bloom, fallback, Character3D, Scene3D, and Post Lab tests passed.'
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE -ErrorAction SilentlyContinue
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
