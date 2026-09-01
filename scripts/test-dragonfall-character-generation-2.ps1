[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $repositoryRoot 'games\Dragonfall'
$testProject = Join-Path $testRoot 'DragonfallVisualAdapterTests.smileproj'
$labProject = Join-Path $testRoot 'DragonfallVisualAdapterLab.smileproj'
$expected = Join-Path $testRoot 'DragonfallVisualAdapterTests.expected.txt'
$adapterSource = Join-Path $testRoot 'DragonfallVisualActor.smile'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\DragonfallVisualAdapterTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\DragonfallVisualAdapterTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\DragonfallVisualAdapterTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\DragonfallVisualAdapterLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\DragonfallVisualAdapterLab'
$dragonfallGate = Join-Path $repositoryRoot 'scripts\test-dragonfall.ps1'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Dragonfall M7A gate.'
}

function Assert-Contains([string]$Text, [string]$ExpectedText, [string]$Label) {
    if ($Text.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $ExpectedText"
    }
}

function Assert-ExactNativeOutput([string]$Label) {
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()

    if ($actualText -cne $expectedText) {
        throw "$Label assertions failed: $actualText"
    }
}

function Invoke-NativeAdapterTest([bool]$ForcePbrFailure) {
    if ($ForcePbrFailure) {
        $env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE = '1'
    }
    else {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }

    try {
        & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
            Set-Content -LiteralPath $nativeLog -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A native test execution failed.' }

        $label = if ($ForcePbrFailure) { 'Dragonfall M7A native forced fallback' } else { 'Dragonfall M7A native' }
        Assert-ExactNativeOutput $label
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }
}

Push-Location $repositoryRoot
try {
    $adapter = Get-Content -LiteralPath $adapterSource -Raw

    foreach ($contract in @(
        'Public Const RELEASE_MODE = MODE_CLASSIC',
        'Public Function Create(ByRef Options As CreateOptions) As Actor',
        'Public Function Update(ByRef Value As Actor, ElapsedMilliseconds As Number) As Boolean',
        'Public Function Draw(Value As Actor) As Boolean',
        'Public Sub Destroy(ByRef Value As Actor)',
        'Public Function PlayState(',
        'Public Function SocketPosition(Value As Actor, Anchor As Number) As Core.Vector3',
        'Public Function SpawnEventEffect(',
        'Public Function WorldBounds(Value As Actor) As Character3D.Bounds3D',
        'Public Function PrimaryInteropHandle(Value As Actor) As Number')) {
        Assert-Contains $adapter $contract 'Dragonfall visual adapter'
    }

    foreach ($projectName in @('Dragonfall.smileproj', 'Dragonfall-NoDemo.smileproj')) {
        $projectText = Get-Content -LiteralPath (Join-Path $testRoot $projectName) -Raw
        Assert-Contains $projectText '<SmileSource Include="DragonfallVisualActor.smile" />' $projectName
    }

    $fixturePairs = @(
        @(
            (Join-Path $repositoryRoot 'examples\Character3DTests\Assets\AnimationArticulated.sm3d'),
            (Join-Path $testRoot 'TechnicalAssets\Generation2\AnimationArticulated.sm3d')
        ),
        @(
            (Join-Path $repositoryRoot 'examples\Character3DTests\Assets\AnimationArticulatedMissingTexture.sm3d'),
            (Join-Path $testRoot 'TechnicalAssets\Generation2\AnimationArticulatedMissingTexture.sm3d')
        ),
        @(
            (Join-Path $repositoryRoot 'examples\Renderer3DVfxLab\Assets\VfxAtlas.png'),
            (Join-Path $testRoot 'TechnicalAssets\Generation2\VfxAtlas.png')
        )
    )

    foreach ($pair in $fixturePairs) {
        $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $pair[0]).Hash
        $dragonfallHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $pair[1]).Hash

        if ($sourceHash -cne $dragonfallHash) {
            throw "Dragonfall technical fixture drifted from its repository-owned source: $($pair[1])"
        }
    }

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A native test compilation failed.' }

    Invoke-NativeAdapterTest $false
    Invoke-NativeAdapterTest $true

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Web runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Web assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 `
        --renderer3d --force-renderer3d-pbr-failure
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Web forced-fallback assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Lab Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7A Lab Web runtime JavaScript syntax validation failed.' }

    & $dragonfallGate
    if ($LASTEXITCODE -ne 0) { throw 'Existing Dragonfall gate failed from the M7A gate.' }

    Write-Host ('Dragonfall M7A Classic/Character3D adapter, mixed draw, state/clip, anchor/socket, ' +
        'event/Effects3D, bounds, atomic fallback, 100-restart, native/Web, crowd-demo, and no-demo tests passed.')
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
