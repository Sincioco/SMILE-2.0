[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DAnimationV2Tests'
$testProject = Join-Path $testRoot 'Renderer3DAnimationV2Tests.smileproj'
$labProject = Join-Path $repositoryRoot 'examples\Renderer3DAnimationLab\Renderer3DAnimationLab.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DAnimationV2Tests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DAnimationV2Tests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DAnimationV2Tests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\Renderer3DAnimationLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DAnimationLab'
$fixtureGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-animation-v2-fixtures.ps1'
$runtimeSource = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'

if (-not (Test-Path -LiteralPath $compiler) -or -not (Test-Path -LiteralPath $assetTool)) {
    throw 'Build SMILE before running the Renderer3D animation-v2 gate.'
}

Push-Location $repositoryRoot
try {
    & $fixtureGenerator -Check
    & $assetTool inspect (Join-Path $testRoot 'Assets\AnimationActor68.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The 68-bone animation fixture failed inspection.' }
    & $assetTool inspect (Join-Path $testRoot 'Assets\AnimationActor128.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The 128-bone animation fixture failed inspection.' }

    foreach ($malformed in @('AnimationPartialGroup.sm3d', 'AnimationBadWeights.sm3d')) {
        $diagnostic = (& $assetTool inspect (Join-Path $testRoot "Assets\$malformed") 2>&1 | Out-String)
        if ($LASTEXITCODE -eq 0 -or $diagnostic -notmatch 'SMA1') {
            throw "Malformed SM3D fixture was not rejected deterministically: $malformed"
        }
    }

    $temporaryRoot = Join-Path (Join-Path $repositoryRoot 'artifacts\temp') ([System.IO.Path]::GetRandomFileName())
    [System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
    try {
        $rejectedOutput = Join-Path $temporaryRoot 'rejected.sm3d'
        $boneDiagnostic = (& $assetTool model (Join-Path $testRoot 'Source\AnimationActor129.glb') -o $rejectedOutput 2>&1 | Out-String)
        if ($LASTEXITCODE -eq 0 -or $boneDiagnostic -notmatch 'SMA1321') {
            throw 'The 129-bone GLB was not rejected with SMA1321.'
        }

        $badDescriptor = Join-Path $temporaryRoot 'bad-version.json'
        $descriptorText = [System.IO.File]::ReadAllText((Join-Path $testRoot 'Source\AnimationActor68.sm3d.json'))
        [System.IO.File]::WriteAllText($badDescriptor, $descriptorText.Replace('"version":1', '"version":2'))
        $descriptorDiagnostic = (& $assetTool model (Join-Path $testRoot 'Source\AnimationActor68.glb') --descriptor $badDescriptor -o $rejectedOutput 2>&1 | Out-String)
        if ($LASTEXITCODE -eq 0 -or $descriptorDiagnostic -notmatch 'SMA1331') {
            throw 'The unsupported animation descriptor version was not rejected with SMA1331.'
        }
    }
    finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }

    $webSource = Get-Content -LiteralPath $runtimeSource -Raw
    $updateStart = $webSource.IndexOf('function renderer3DUpdateModelAnimator', [System.StringComparison]::Ordinal)
    $updateEnd = $webSource.IndexOf('function renderer3DTakeModelEvent', $updateStart, [System.StringComparison]::Ordinal)
    if ($updateStart -lt 0 -or $updateEnd -le $updateStart) {
        throw 'The Renderer3D Web production animator update path was not found.'
    }
    $updateSource = $webSource.Substring($updateStart, $updateEnd - $updateStart)
    if ($updateSource.Contains('new ') -or $updateSource.Contains('.subarray(') -or
        $updateSource.Contains('[...') -or $updateSource.Contains('.map(')) {
        throw 'The Renderer3D Web production animator update path contains a per-update allocation.'
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native animation-v2 test compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput | Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D native animation-v2 test execution failed.' }
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $nativeText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    if ($nativeText -cne $expectedText) {
        throw "Renderer3D native animation-v2 assertions failed: $nativeText"
    }

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation-v2 test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation-v2 game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation-v2 runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Web animation-v2 assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Animation Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Animation Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Animation Lab Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Renderer3D Animation Lab Web runtime JavaScript syntax validation failed.' }

    Write-Host 'Renderer3D animation-v2 native/Web import, 128-bone, playback, crossfade, event, root-motion, socket, palette, lifecycle, malformed-input, and Animation Lab tests passed.'
}
finally {
    Pop-Location
}
