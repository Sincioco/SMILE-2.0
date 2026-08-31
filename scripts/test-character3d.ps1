[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$assetTool = Join-Path $repositoryRoot 'artifacts\assettool\smileasset.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Character3DTests'
$testProject = Join-Path $testRoot 'Character3DTests.smileproj'
$labProject = Join-Path $repositoryRoot 'examples\Character3DLab\Character3DLab.smileproj'
$expected = Join-Path $testRoot 'expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Character3DTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Character3DTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Character3DTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\Character3DLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\Character3DLab'
$fixtureGenerator = Join-Path $repositoryRoot 'scripts\generate-renderer3d-animation-v2-fixtures.ps1'
$characterSourcePath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Character3D.smile'
$sceneSourcePath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Scene3D.smile'
$libraryProjectPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj'

if (-not (Test-Path -LiteralPath $compiler) -or -not (Test-Path -LiteralPath $assetTool)) {
    throw 'Build SMILE before running the Character3D gate.'
}

function Assert-ExactOutput([string]$Label) {
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()

    if ($actualText -cne $expectedText) {
        throw "$Label assertions failed: $actualText"
    }
}

function Invoke-NativeCharacterTest([bool]$ForcePbrFailure) {
    if ($ForcePbrFailure) {
        $env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE = '1'
    }
    else {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }

    try {
        & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
            Set-Content -LiteralPath $nativeLog -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'Character3D native test execution failed.' }

        $label = if ($ForcePbrFailure) { 'Character3D native forced-fallback' } else { 'Character3D native' }
        Assert-ExactOutput $label
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }
}

Push-Location $repositoryRoot
try {
    & $fixtureGenerator -Check

    & $assetTool inspect (Join-Path $testRoot 'Assets\AnimationArticulated.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The Character3D articulated fixture failed inspection.' }
    & $assetTool inspect (Join-Path $testRoot 'Assets\AnimationArticulatedMissingTexture.sm3d') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The Character3D missing-texture fixture failed inspection.' }

    $characterSource = Get-Content -LiteralPath $characterSourcePath -Raw
    $sceneSource = Get-Content -LiteralPath $sceneSourcePath -Raw
    $libraryProject = Get-Content -LiteralPath $libraryProjectPath -Raw

    if ($characterSource -notmatch 'CHARACTER3D_MAX_ASSETS = 16' -or
        $characterSource -notmatch 'CHARACTER3D_MAX_ACTORS = 32' -or
        $characterSource -notmatch 'Private Dim ActorParts\[32, 16\]' -or
        $characterSource -notmatch 'Public Type Actor\s+Handle As Number') {
        throw 'Character3D bounded storage or opaque actor identity changed.'
    }
    if ($sceneSource -notmatch 'Public Const QUALITY_AUTO = 0' -or
        $sceneSource -notmatch 'Public Function AssetProfileKey\(\)' -or
        $sceneSource -notmatch 'Public Function EndScene\(\)') {
        throw 'Scene3D quality, cache-profile, or balanced-frame API changed.'
    }
    if ($libraryProject.IndexOf('Scene3D.smile', [System.StringComparison]::Ordinal) -gt
        $libraryProject.IndexOf('Character3D.smile', [System.StringComparison]::Ordinal)) {
        throw 'Scene3D must precede Character3D in the source-library dependency order.'
    }

    $updateStart = $characterSource.IndexOf('Public Function Update(', [System.StringComparison]::Ordinal)
    $updateEnd = $characterSource.IndexOf('Public Function IsPlaying(', $updateStart, [System.StringComparison]::Ordinal)
    $drawStart = $characterSource.IndexOf('Public Function Draw(', [System.StringComparison]::Ordinal)
    $drawEnd = $characterSource.IndexOf('Public Function IsValid(', $drawStart, [System.StringComparison]::Ordinal)
    if ($updateStart -lt 0 -or $updateEnd -le $updateStart -or $drawStart -lt 0 -or $drawEnd -le $drawStart) {
        throw 'Character3D Update or Draw implementation was not found.'
    }
    $perFrameSource = $characterSource.Substring($updateStart, $updateEnd - $updateStart) +
        $characterSource.Substring($drawStart, $drawEnd - $drawStart)
    if ($perFrameSource -match 'LoadModel|PrepareModel|CreateModel|LoadTexture|CreateMaterial') {
        throw 'Character3D Update or Draw performs resource loading or creation.'
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Character3D native test compilation failed.' }

    Invoke-NativeCharacterTest $false
    Invoke-NativeCharacterTest $true

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Web runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Web assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 `
        --renderer3d --force-renderer3d-pbr-failure
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Web forced-fallback assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Lab Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Character3D Lab Web runtime JavaScript syntax validation failed.' }

    Write-Host 'Character3D and Scene3D native/Web cache, ownership, failure atomicity, profiles, lighting, animation, root, events, sockets, rendering, reset, lifecycle, fallback, and Lab build tests passed.'
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
