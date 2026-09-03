[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$graphicsPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$headerPath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$nativePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp'
$webPath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DDistortion'
$testProject = Join-Path $testRoot 'Renderer3DDistortionTests.smileproj'
$fallbackProject = Join-Path $testRoot 'Renderer3DDistortionFallbackTests.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$fallbackExpected = Join-Path $testRoot 'fallback-expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DDistortionTests.exe'
$fallbackNativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DDistortionFallbackTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DDistortionTests.out'
$fallbackNativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DDistortionFallbackTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DDistortionTests'
$fallbackWebOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DDistortionFallbackTests'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D distortion gate.'
}

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

function Assert-ExactOutput([string]$ActualPath, [string]$ExpectedPath, [string]$Label) {
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()
    if ($actualText -cne $expectedText) {
        throw "$Label failed: $actualText"
    }
}

Push-Location $repositoryRoot
try {
    $graphics = Get-Content -LiteralPath $graphicsPath -Raw
    $header = Get-Content -LiteralPath $headerPath -Raw
    $native = Get-Content -LiteralPath $nativePath -Raw
    $web = Get-Content -LiteralPath $webPath -Raw

    Assert-Contains $header 'SMILE_3D_DISTORTION = 126' 'Native numeric ABI'
    Assert-Contains $graphics 'Private Const COMMAND_DISTORTION = 126' 'SMILE numeric ABI'
    Assert-Contains $web 'case 126:return renderer3DDistortionCommand(a,b,c,d,e,f,g);' 'Web numeric ABI'
    Assert-Contains $native 'DXGI_FORMAT_R16G16B16A16_FLOAT' 'Native floating vector target'
    Assert-Contains $web 'gl.RGBA16F' 'Web floating vector target'
    Assert-Contains $web 'gl.RGBA8' 'Web packed-vector fallback'
    Assert-Contains $native 'SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE' 'Native fallback hook'
    Assert-Contains $web 'SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE' 'Web fallback hook'
    Assert-Contains $native 'first[0] = 5.0f' 'Native distortion composite'
    Assert-Contains $web 'renderer3DRenderDistortionPass' 'Web distortion composite'
    Assert-Contains $native 'float2(-.03,-.03),float2(.03,.03)' 'Native bounded offset'
    Assert-Contains $web 'vec2(-.03),vec2(.03)' 'Web bounded offset'
    Assert-Contains $web 'back.drawImage(renderer3DCanvas,0,0,logicalWidth,logicalHeight)' 'Renderer2D composition'

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Distortion native test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Distortion native test execution failed.' }
    Assert-ExactOutput $nativeLog $expected 'Distortion native assertions'

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Distortion Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Distortion Web game syntax check failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Distortion Web runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Distortion Web assertions failed.' }

    & $compiler --project $fallbackProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $fallbackNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Distortion fallback native compilation failed.' }
    $env:SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE = '1'
    try {
        & 'scripts\run-bounded-test.cmd' 60 $fallbackNativeOutput |
            Set-Content -LiteralPath $fallbackNativeLog -Encoding utf8
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE -ErrorAction SilentlyContinue
    }
    if ($LASTEXITCODE -ne 0) { throw 'Distortion fallback native execution failed.' }
    Assert-ExactOutput $fallbackNativeLog $fallbackExpected 'Distortion fallback native assertions'

    & $compiler --project $fallbackProject --target web --configuration $Configuration `
        --output-dir $fallbackWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Distortion fallback Web compilation failed.' }
    & node 'scripts\run-web-test.js' $fallbackWebOutput --expected $fallbackExpected --timeout 60000 `
        --renderer3d --force-renderer3d-distortion-failure
    if ($LASTEXITCODE -ne 0) { throw 'Distortion fallback Web assertions failed.' }

    Write-Host 'Renderer3D M7E-B native/Web half-, quarter-, HDR, direct-LDR, fallback, bounded-composite, and HUD-composition tests passed.'
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_DISTORTION_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
