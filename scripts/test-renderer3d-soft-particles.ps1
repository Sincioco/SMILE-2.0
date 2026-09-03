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
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DSoftParticles'
$testProject = Join-Path $testRoot 'Renderer3DSoftParticleTests.smileproj'
$fallbackProject = Join-Path $testRoot 'Renderer3DSoftParticleFallbackTests.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$fallbackExpected = Join-Path $testRoot 'fallback-expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DSoftParticleTests.exe'
$fallbackNativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DSoftParticleFallbackTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DSoftParticleTests.out'
$fallbackNativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DSoftParticleFallbackTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DSoftParticleTests'
$fallbackWebOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DSoftParticleFallbackTests'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Renderer3D soft-particle gate.'
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

    Assert-Contains $header 'SMILE_3D_SOFT_DEPTH = 125' 'Native numeric ABI'
    Assert-Contains $graphics 'Private Const COMMAND_SOFT_DEPTH = 125' 'SMILE numeric ABI'
    Assert-Contains $web 'case 125:return renderer3DSoftDepthCommand(a,b,c,d);' 'Web numeric ABI'
    Assert-Contains $native 'Texture2DMS<float> sourceDepth:register(t0)' 'Native MSAA depth path'
    Assert-Contains $native 'depth_copy_msaa_pixel_source, "main", "ps_5_0"' 'Native MSAA shader profile'
    Assert-Contains $native 'z=min(z,sourceDepth.Load(q,7))' 'Native minimum-sample depth resolve'
    Assert-Contains $native 'DXGI_FORMAT_R32_FLOAT' 'Native linear-depth format'
    Assert-Contains $web 'gl.DEPTH_COMPONENT24' 'Web sampleable depth texture'
    Assert-Contains $web 'gl.R32F' 'Web float depth target'
    Assert-Contains $web 'packDepth' 'Web packed-depth fallback'
    Assert-Contains $native 'base.a*=saturate(distance/max(softDepth.y,.0001))' 'Native soft fade'
    Assert-Contains $web 'base.a*=clamp(distance/max(softDepthSettings.y,.0001),0.0,1.0)' 'Web soft fade'
    Assert-Contains $native 'SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE' 'Native fallback hook'
    Assert-Contains $web 'SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE' 'Web fallback hook'
    Assert-Contains $web 'back.drawImage(renderer3DCanvas,0,0,logicalWidth,logicalHeight)' 'Renderer2D composition'

    $near = 1.0
    $far = 1000.0
    $depth = 0.5
    $nativeLinear = $near * $far / ($far - $depth * ($far - $near))
    $clip = $depth * 2.0 - 1.0
    $webLinear = 2.0 * $near * $far / ($far + $near - $clip * ($far - $near))
    if ([Math]::Abs($nativeLinear - $webLinear) -gt 0.000001) {
        throw 'Native/Web normal-Z linear-depth reference vectors diverged.'
    }
    if (($near -gt $nativeLinear) -or ($nativeLinear -gt $far)) {
        throw 'Linear-depth reference escaped the camera range.'
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Soft-particle native test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Soft-particle native test execution failed.' }
    Assert-ExactOutput $nativeLog $expected 'Soft-particle native assertions'

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Soft-particle Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Soft-particle Web game syntax check failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Soft-particle Web runtime syntax check failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Soft-particle Web assertions failed.' }

    & $compiler --project $fallbackProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $fallbackNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Soft-depth fallback native compilation failed.' }
    $env:SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE = '1'
    try {
        & 'scripts\run-bounded-test.cmd' 60 $fallbackNativeOutput |
            Set-Content -LiteralPath $fallbackNativeLog -Encoding utf8
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE -ErrorAction SilentlyContinue
    }
    if ($LASTEXITCODE -ne 0) { throw 'Soft-depth fallback native execution failed.' }
    Assert-ExactOutput $fallbackNativeLog $fallbackExpected 'Soft-depth fallback native assertions'

    & $compiler --project $fallbackProject --target web --configuration $Configuration `
        --output-dir $fallbackWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Soft-depth fallback Web compilation failed.' }
    & node 'scripts\run-web-test.js' $fallbackWebOutput --expected $fallbackExpected --timeout 60000 `
        --renderer3d --force-renderer3d-soft-depth-failure
    if ($LASTEXITCODE -ne 0) { throw 'Soft-depth fallback Web assertions failed.' }

    Write-Host 'Renderer3D M7E-A native/Web MSAA, 1x, HDR, direct-LDR, soft fade, fallback, and HUD-composition tests passed.'
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_SOFT_DEPTH_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
