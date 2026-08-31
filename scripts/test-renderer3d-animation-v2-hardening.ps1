[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DAnimationV2HardeningTests'
$testProject = Join-Path $testRoot 'Renderer3DAnimationV2HardeningTests.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DAnimationV2HardeningTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DAnimationV2HardeningTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DAnimationV2HardeningTests'
$fixtureGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-animation-v2-fixtures.ps1'
$runtimeSource = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'

if (-not (Test-Path -LiteralPath $compiler) -or -not (Test-Path -LiteralPath $assetTool)) {
    throw 'Build SMILE before running the Renderer3D animation-v2 hardening gate.'
}

Push-Location $repositoryRoot
try {
    & $fixtureGenerator -Check
    & $assetTool inspect (Join-Path $testRoot 'Assets\AnimationArticulated.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The articulated animation fixture failed inspection.' }

    $webSource = Get-Content -LiteralPath $runtimeSource -Raw
    $updateStart = $webSource.IndexOf('function renderer3DUpdateModelAnimator', [System.StringComparison]::Ordinal)
    $updateEnd = $webSource.IndexOf('function renderer3DTakeModelEvent', $updateStart, [System.StringComparison]::Ordinal)
    if ($updateStart -lt 0 -or $updateEnd -le $updateStart) {
        throw 'The Renderer3D Web production animator update path was not found.'
    }
    $updateSource = $webSource.Substring($updateStart, $updateEnd - $updateStart)
    if ($updateSource.Contains('new ') -or $updateSource.Contains('.subarray(') -or
        $updateSource.Contains('[...') -or $updateSource.Contains('.map(') -or $updateSource.Contains('.slice(')) {
        throw 'The Renderer3D Web production animator update path contains a per-update allocation.'
    }
    if ($webSource -notmatch 'weights=new Uint16Array') {
        throw 'The Renderer3D Web animation weights are not retained compactly.'
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native animation-v2 hardening test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native animation-v2 hardening test execution failed.' }
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $nativeText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($nativeText -cne $expectedText) {
        throw "Renderer3D native animation-v2 hardening assertions failed: $nativeText"
    }

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation-v2 hardening test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web hardening game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web hardening runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation-v2 hardening assertions failed.' }

    Write-Host 'Renderer3D animation-v2 fractional timing, irregular sampling, moving crossfade, interruption, root, event overflow, compact memory, articulated deformation, palette reuse, and lifecycle tests passed.'
}
finally {
    Pop-Location
}
