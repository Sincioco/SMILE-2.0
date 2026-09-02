[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $repositoryRoot 'games\Dragonfall'
$testProject = Join-Path $testRoot 'DragonfallArinPrototypeTests.smileproj'
$labProject = Join-Path $testRoot 'DragonfallArinPrototypeLab.smileproj'
$viewerProject = Join-Path $testRoot 'Character3DViewer.smileproj'
$expected = Join-Path $testRoot 'DragonfallArinPrototypeTests.expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\DragonfallArinPrototypeTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\DragonfallArinPrototypeTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\DragonfallArinPrototypeTests'
$labNativeOutput = Join-Path $repositoryRoot 'artifacts\examples\DragonfallArinPrototypeLab.exe'
$labWebOutput = Join-Path $repositoryRoot 'artifacts\web\DragonfallArinPrototypeLab'
$viewerNativeOutput = Join-Path $repositoryRoot 'artifacts\games\Character3DViewer.exe'
$viewerWebOutput = Join-Path $repositoryRoot 'artifacts\web\Character3DViewer'
$sourceAsset = Join-Path $testRoot 'SourceAssets\Arin\sin-star-i-character-1-paladin-tripo-v01.original.glb'
$runtimeAsset = Join-Path $testRoot 'Assets\Generation2\Arin\ArinPrototype.sm3d'
$viewerSource = Join-Path $repositoryRoot 'tools\Character3DViewer\Program.smile'
$viewerProjectSource = Join-Path $testRoot 'Character3DViewer.smileproj'
$assetToolSource = Join-Path $repositoryRoot 'src\Smile.AssetTool\Sm3dV2.cs'
$nativeRuntimeSource = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\runtime.c'
$pointerStateSource = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\input\pointer_state.c'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Build SMILE before running the Dragonfall M7B Arin prototype gate.'
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

function Invoke-NativePrototypeTest([bool]$ForcePbrFailure) {
    if ($ForcePbrFailure) {
        $env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE = '1'
    }
    else {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }

    try {
        & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
            Set-Content -LiteralPath $nativeLog -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B native test execution failed.' }

        $label = if ($ForcePbrFailure) { 'Dragonfall M7B native forced fallback' } else { 'Dragonfall M7B native' }
        Assert-ExactNativeOutput $label
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    }
}

Push-Location $repositoryRoot
try {
    $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceAsset).Hash
    if ($sourceHash -cne '0B75E3664FC2743637C9E75E86A55EBDFB8D4A4E3740AC06E593ADE1588013F6') {
        throw "The preserved Arin source GLB hash changed: $sourceHash"
    }

    $assetToolText = Get-Content -LiteralPath $assetToolSource -Raw
    Assert-Contains $assetToolText 'private const int MaximumBufferViews = 1024;' 'SM3D v2 converter'
    Assert-Contains $assetToolText 'private const int MaximumAccessors = 1024;' 'SM3D v2 converter'
    Assert-Contains $assetToolText 'MergeCompatibleParts(parts)' 'SM3D v2 converter'

    $nativeRuntimeText = Get-Content -LiteralPath $nativeRuntimeSource -Raw
    $showScreenIndex = $nativeRuntimeText.IndexOf('void smile_show_screen(void)', [System.StringComparison]::Ordinal)
    $pointerRolloverIndex = $nativeRuntimeText.IndexOf('smile_pointer_state_begin_frame(&smile_pointer);', $showScreenIndex, [System.StringComparison]::Ordinal)
    $messagePumpIndex = $nativeRuntimeText.IndexOf('smile_pump_messages();', $showScreenIndex, [System.StringComparison]::Ordinal)
    if ($showScreenIndex -lt 0 -or $pointerRolloverIndex -lt 0 -or
        $messagePumpIndex -lt 0 -or $pointerRolloverIndex -gt $messagePumpIndex) {
        throw 'Native Show Screen must roll transient pointer state before pumping next-frame messages.'
    }

    $viewerText = Get-Content -LiteralPath $viewerSource -Raw
    $viewerProjectText = Get-Content -LiteralPath $viewerProjectSource -Raw
    $pointerStateText = Get-Content -LiteralPath $pointerStateSource -Raw
    Assert-Contains $pointerStateText 'state->wheel_remainder / units_per_step' `
        'Native precision-wheel accumulator'
    foreach ($contract in @(
        'Import Smile.Simple3D.Character3D As Character3D',
        'Import Smile.Simple3D.CharacterViewer As CharacterViewer',
        'Import Smile.Simple3D.Interaction As Interaction',
        'Character3D.LoadWithPolicy(',
        'Interaction.UpdateOrbitControls(',
        'Interaction.UpdatePanZoomControls(',
        'Interaction.ApplyCameraControls(',
        'CharacterViewer.AutoFit(',
        'CharacterViewer.RetainedPointerDelta(',
        'CharacterViewer.AdvanceZoom(',
        'PressedKey = KEY_O',
        'Sub AdvanceAutoOrbit()',
        'Sub CreateSocketGizmos()',
        'Graphics3D.SetMaterialInspection3D(',
        'Sub ClampCameraControls()',
        'Sub AdvanceSmoothZoom()',
        'Sub DrawViewerOverlay()')) {
        Assert-Contains $viewerText $contract 'Character 3D Viewer'
    }
    foreach ($asset in @(
        'ArinPrototype.sm3d',
        'Arin-base-color.png',
        'Arin-normal.png',
        'Arin-orm.png',
        'AnimationArticulated.sm3d')) {
        Assert-Contains $viewerProjectText $asset 'Character 3D Viewer project'
    }

    & 'scripts\prepare-dragonfall-arin-prototype.ps1' -Check

    & 'scripts\test-renderer3d-v2-boundaries.ps1'

    & 'scripts\test-renderer3d-animation-v2-hardening.ps1' -Configuration $Configuration

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B native test compilation failed.' }

    Invoke-NativePrototypeTest $false
    Invoke-NativePrototypeTest $true

    & $compiler --project $testProject --target web --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Web test compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Web runtime JavaScript syntax validation failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 --renderer3d
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Web assertions failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000 `
        --renderer3d --force-renderer3d-pbr-failure
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Web forced-fallback assertions failed.' }

    & $compiler --project $labProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $labNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Lab native compilation failed.' }
    & $compiler --project $labProject --target web --configuration $Configuration --output-dir $labWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Lab Web compilation failed.' }
    & node --check (Join-Path $labWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Lab Web game JavaScript syntax validation failed.' }
    & node --check (Join-Path $labWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Dragonfall M7B Lab Web runtime JavaScript validation failed.' }

    & $compiler --project $viewerProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $viewerNativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Character 3D Viewer native compilation failed.' }
    & $compiler --project $viewerProject --target web --configuration $Configuration --output-dir $viewerWebOutput
    if ($LASTEXITCODE -ne 0) { throw 'Character 3D Viewer Web compilation failed.' }
    & node --check (Join-Path $viewerWebOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Character 3D Viewer Web game JavaScript validation failed.' }
    & node --check (Join-Path $viewerWebOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Character 3D Viewer Web runtime JavaScript validation failed.' }

    foreach ($asset in @(
        $runtimeAsset,
        (Join-Path $testRoot 'Assets\Generation2\Arin\Textures\Arin-base-color.png'),
        (Join-Path $testRoot 'Assets\Generation2\Arin\Textures\Arin-normal.png'),
        (Join-Path $testRoot 'Assets\Generation2\Arin\Textures\Arin-orm.png'))) {
        if (-not (Test-Path -LiteralPath $asset)) {
            throw "Expected Arin runtime asset is missing: $asset"
        }
    }

    & 'scripts\test-dragonfall-character-generation-2.ps1' -Configuration $Configuration

    Write-Host ('Dragonfall M7B Arin source preservation, deterministic conversion, 1,024-table boundary, ' +
        'compatible-part coalescing, animation hardening, native/Web PBR/fallback, Character 3D Viewer, ' +
        'M7A adapter, crowd-demo, and no-demo tests passed.')
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE -ErrorAction SilentlyContinue
    Pop-Location
}
