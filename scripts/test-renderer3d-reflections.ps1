[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testRoot = Join-Path $repositoryRoot 'examples\Renderer3DReflectionTests'
$testProject = Join-Path $testRoot 'Renderer3DReflectionTests.smileproj'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Renderer3DReflectionTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Renderer3DReflectionTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Renderer3DReflectionTests'
$expectedNormal = Join-Path $testRoot 'expected-normal.txt'
$expectedRetry = Join-Path $testRoot 'expected-retry.txt'

function Assert-Contains([string]$Text, [string]$Expected, [string]$Label) {
    if ($Text.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $Expected"
    }
}

function Assert-Near(
    [double]$Actual,
    [double]$Expected,
    [double]$Tolerance,
    [string]$Label
) {
    if ([Math]::Abs($Actual - $Expected) -gt $Tolerance) {
        throw "$Label expected $Expected but received $Actual."
    }
}

function Assert-Output([string]$ExpectedPath, [string]$ActualPath, [string]$Label) {
    $expectedText = (Get-Content -LiteralPath $ExpectedPath -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $ActualPath -Raw).Trim()

    if ($actualText -cne $expectedText) {
        throw "$Label assertions failed: $actualText"
    }
}

function Invoke-NativeReflectionTest(
    [string]$ExpectedPath,
    [string]$Label
) {
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw "$Label execution failed." }

    Assert-Output $ExpectedPath $nativeLog $Label
}

function Mirror-Y([double]$Value, [double]$FloorHeight) {
    return 2.0 * $FloorHeight - $Value
}

function Backdrop-SourceY([double]$DisplayY, [double]$ReceiverSeam) {
    $span = [Math]::Max(1.0 - $ReceiverSeam, 0.0001)
    return [Math]::Max(0.0,
        [Math]::Min($ReceiverSeam,
            $ReceiverSeam * (1.0 - $DisplayY) / $span))
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'Build SMILE before running the Renderer3D reflection gate.'
}

Push-Location $repositoryRoot
try {
    $graphicsSource = Get-Content -LiteralPath `
        'libraries\Smile.Simple3D\Graphics3D.smile' -Raw
    $arenaSource = Get-Content -LiteralPath `
        'libraries\Smile.Simple3D\Arena3D.smile' -Raw
    $nativeHeader = Get-Content -LiteralPath `
        'src\Smile.NativeRuntime\graphics\graphics3d.h' -Raw
    $nativeSource = Get-Content -LiteralPath `
        'src\Smile.NativeRuntime\graphics\graphics3d_directx.cpp' -Raw
    $nativeOwner = Get-Content -LiteralPath `
        'src\Smile.NativeRuntime\graphics\graphics3d_reflections.cpp' -Raw
    $webWriter = Get-Content -LiteralPath `
        'src\Smile.Compiler\WebOutputWriter.cs' -Raw
    $webOwner = Get-Content -LiteralPath `
        'src\Smile.Compiler\WebRuntime\Renderer3DReflections.js' -Raw
    $viewerUi = Get-Content -LiteralPath `
        'tools\Character3DViewer\ViewerUi.smile' -Raw
    $viewerParty = Get-Content -LiteralPath `
        'tools\Character3DViewer\ViewerParty.smile' -Raw
    $gamePreview = Get-Content -LiteralPath `
        'games\SinStarI\BattleArenaPreview.smile' -Raw

    Assert-Contains $nativeHeader 'SMILE_3D_CONFIGURE_REFLECTIONS = 133' `
        'Native append-only reflection ABI'
    Assert-Contains $nativeHeader 'SMILE_3D_SET_OBJECT_REFLECTION_MODE = 134' `
        'Native append-only reflection ABI'
    Assert-Contains $nativeHeader 'SMILE_3D_REFLECTION_VALUE = 135' `
        'Native append-only reflection ABI'
    Assert-Contains $graphicsSource 'Private Const COMMAND_CONFIGURE_REFLECTIONS = 133' `
        'SMILE reflection ABI'
    Assert-Contains $graphicsSource 'REFLECTION_QUERY_EFFECTIVE_FLOOR_HEIGHT = 18' `
        'SMILE effective reflection-plane diagnostic'
    Assert-Contains $graphicsSource 'REFLECTION_QUERY_TARGET_FORMAT = 19' `
        'SMILE reflection-format diagnostic'
    Assert-Contains $webWriter 'case 133:return renderer3DReflectionConfigure(a,b,c,d,e,f);' `
        'Web reflection ABI'
    Assert-Contains $webWriter 'case 134:object=renderer3DObjects.get(a);' `
        'Web reflection eligibility ABI'
    Assert-Contains $webWriter 'case 135:return renderer3DReflectionValue(a);' `
        'Web reflection query ABI'

    Assert-Contains $arenaSource 'Optional Reflective As Boolean = False' `
        'Matte generic-arena default'
    Assert-Contains $arenaSource 'OBJECT_REFLECTION_RECEIVER' `
        'Shared arena receiver role'
    Assert-Contains $arenaSource 'OBJECT_REFLECTION_EXCLUDED' `
        'Shared arena grid exclusion'
    Assert-Contains $nativeSource 'submission->object.reflection_mode != 1' `
        'Native immutable eligibility filter'
    Assert-Contains $nativeSource 'submission->object.reflection_mode == 2' `
        'Native receiver selection'
    Assert-Contains $nativeSource 'smile_3d_submission_is_opaque(submission)' `
        'Native opaque and masked reflection filter'
    Assert-Contains $webWriter `
        'object.reflectionMode=source.reflectionMode===undefined?1:source.reflectionMode;' `
        'Web reflection eligibility snapshot'
    Assert-Contains $webOwner 'object.kind !== renderer3DSubmissionObject' `
        'Web VFX and non-object exclusion'
    Assert-Contains $webOwner 'renderer3DSubmissionIsOpaque(object)' `
        'Web opaque and masked reflection filter'

    Assert-Contains $nativeSource 'smile_reflection_pass3d = 1' `
        'Native reflected-view pass'
    Assert-Contains $nativeSource 'reflection.m[5] = -1.0f' `
        'Native improper plane-reflection transform'
    Assert-Contains $nativeSource 'smile_3d_multiply(reflection, result)' `
        'Native row-vector reflection/view composition'
    Assert-Contains $nativeSource 'smile_front_cull_raster_state3d' `
        'Native reflection winding reversal'
    Assert-Contains $webOwner 'renderer3DReflection.pass = true' `
        'Web reflected-view pass'
    Assert-Contains $webWriter `
        'gl.cullFace(front===renderer3DReflection.pass?gl.FRONT:gl.BACK)' `
        'Web Direct3D-parity reflection winding reversal'
    Assert-Contains $webWriter 'output[4]=-columnY0' `
        'Web improper plane-reflection transform'
    Assert-Contains $webOwner 'renderer3DReflectionResolveReceiver()' `
        'Web immutable receiver-plane resolution'
    Assert-Contains $nativeSource 'smile_3d_resolve_reflection_receiver' `
        'Native immutable receiver-plane resolution'
    Assert-Contains $nativeOwner 'longest > 2048' `
        'Native bounded reflection target'
    Assert-Contains $webOwner 'longest > 2048' `
        'Web bounded reflection target'
    Assert-Contains $nativeOwner 'failed_revision' `
        'Native failed-allocation retry cache'
    Assert-Contains $webOwner 'failedRevision' `
        'Web failed-allocation retry cache'
    Assert-Contains $webOwner 'format === 2 ? gl.RGBA16F : gl.RGBA8' `
        'Web effective HDR reflection format'
    Assert-Contains $webOwner 'renderer3DReflection.failedFormat === format' `
        'Web format-aware failed-allocation cache'
    Assert-Contains $webOwner 'format === 2 ? 12 : 8' `
        'Web reflection target byte accounting'

    Assert-Contains $nativeSource `
        'smile_reflections_softness_percent() / 100.0f' `
        'Native reflection softness'
    Assert-Contains $webOwner 'renderer3DReflection.softness / 100' `
        'Web reflection softness'
    Assert-Contains $nativeSource 'reflectionTexture.Sample' `
        'Native bounded reflection filtering'
    Assert-Contains $webWriter 'texture(reflectionTexture' `
        'Web bounded reflection filtering'
    Assert-Contains $nativeSource `
        'smile_3d_receiver_backdrop_seam(receiver)' `
        'Native receiver-bounded backdrop capture'
    Assert-Contains $webOwner 'renderer3DPostPass(renderer3DReflection.framebuffer' `
        'Web screen-fixed backdrop capture'
    Assert-Contains $webOwner 'renderer3DReflectionBackdropSeam(receiver)' `
        'Web receiver-bounded backdrop capture'
    Assert-Contains $webOwner '0, 0, 1, backdropSeam, 0);' `
        'Web receiver seam delivery'
    Assert-Contains $nativeSource 'backdropUv' `
        'Native reflected backdrop orientation'
    Assert-Contains $webWriter 'backdropUv' `
        'Web reflected backdrop orientation'
    Assert-Contains $nativeSource 'seam*(1-uv.y)' `
        'Native visible-backdrop sampling bound'
    Assert-Contains $nativeSource 'mirrored && receiver_seam <= 0.0f' `
        'Native fully occluded backdrop suppression'
    Assert-Contains $webWriter 'seam*(1.0-displayY)' `
        'Web visible-backdrop sampling bound'
    Assert-Contains $webOwner 'else if (backdropSeam > 0)' `
        'Web fully occluded backdrop suppression'
    Assert-Contains $nativeSource 'smile_3d_set_object_raster(context, object, 1);' `
        'Native legacy simple-material grid culling'
    Assert-Contains $webWriter 'renderer3DApplyCull(object,true);' `
        'Web legacy simple-material grid culling'

    Assert-Contains $viewerUi 'Battle Floor: Reflective' 'Viewer reflection label'
    Assert-Contains $viewerUi 'Battle Floor: Original' 'Viewer reflection label'
    Assert-Contains $viewerUi 'Battle Floor: Unavailable' `
        'Viewer fallback label'
    Assert-Contains $viewerUi 'Private Const ANIMATION_DETAILS_Y = 530' `
        'Viewer reflection control and animation-details separation'
    Assert-Contains $viewerUi 'Private Const ANIMATION_DETAILS_MINIMUM_HEIGHT = 780' `
        'Viewer compact-height animation-details suppression'
    Assert-Contains $viewerParty 'Result.Consumed = True' `
        'Party reflection control ownership'
    Assert-Contains $gamePreview 'Import Smile.Simple3D.Arena3D As Arena3D' `
        'Sin Star I shared arena reuse'
    Assert-Contains $gamePreview 'Import Smile.Simple3D.PrecisionCamera3D As Interaction' `
        'Sin Star I shared camera reuse'
    Assert-Contains $gamePreview 'Character3D.PlayMode(' `
        'Sin Star I live animated actor'
    Assert-Contains $gamePreview 'Character3D.SetUnusedAssetCacheLimit(0)' `
        'Sin Star I renderer-reset re-entry synchronization'
    if ($gamePreview.IndexOf('Character3DViewer',
            [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw 'Sin Star I preview imports or copies a Character Viewer owner.'
    }

    # Supplemental scalar sanity only; compiled production-path diagnostics below
    # provide the discriminating view/projection and receiver-plane regression.
    $floorHeight = 7.0
    $pointY = 31.0
    $mirroredY = Mirror-Y $pointY $floorHeight
    Assert-Near (Mirror-Y $mirroredY $floorHeight) $pointY 0.0000001 `
        'Mirror involution'
    Assert-Near (Mirror-Y $floorHeight $floorHeight) $floorHeight 0.0000001 `
        'Plane point remains fixed'
    Assert-Near ($mirroredY - $floorHeight) (-1.0 * ($pointY - $floorHeight)) `
        0.0000001 'Signed distance reversal'
    Assert-Near ([Math]::Abs($pointY - $floorHeight)) `
        ([Math]::Abs($mirroredY - $floorHeight)) 0.0000001 `
        'Receiver-point distance symmetry'
    $receiverSeam = 0.42
    Assert-Near (Backdrop-SourceY $receiverSeam $receiverSeam) `
        $receiverSeam 0.0000001 'Backdrop seam continuity'
    Assert-Near (Backdrop-SourceY 1.0 $receiverSeam) 0.0 0.0000001 `
        'Backdrop bottom samples the visible image top'
    if ((Backdrop-SourceY 0.75 $receiverSeam) -gt $receiverSeam) {
        throw 'Reflected backdrop sampled a floor-covered source row.'
    }

    & $compiler --project $testProject --target windows-x64 `
        --configuration $Configuration --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D reflection native test compilation failed.'
    }

    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE `
        -ErrorAction SilentlyContinue
    Invoke-NativeReflectionTest $expectedNormal `
        'Renderer3D reflection native normal path'

    $env:SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE = 'once'
    try {
        Invoke-NativeReflectionTest $expectedRetry `
            'Renderer3D reflection native retry path'
    }
    finally {
        Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE `
            -ErrorAction SilentlyContinue
    }

    & $compiler --project $testProject --target web `
        --configuration $Configuration --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D reflection Web test compilation failed.'
    }

    & node.exe --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D reflection Web game JavaScript syntax validation failed.'
    }
    & node.exe --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D reflection Web runtime JavaScript syntax validation failed.'
    }
    & node.exe 'scripts\run-web-test.js' $webOutput --expected $expectedNormal `
        --timeout 60000 --renderer3d --reflection-half-float-filter
    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D reflection Web normal assertions failed.'
    }
    & node.exe 'scripts\run-web-test.js' $webOutput --expected $expectedRetry `
        --timeout 60000 --renderer3d --force-renderer3d-reflection-failure-once
    if ($LASTEXITCODE -ne 0) {
        throw 'Renderer3D reflection Web retry assertions failed.'
    }

    $powerShell7 = (Get-Command pwsh.exe -ErrorAction Stop).Source
    & $powerShell7 -NoProfile -ExecutionPolicy Bypass -File `
        'scripts\test-character-3d-viewer-hardening.ps1' `
        -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw 'Character Viewer reflection integration assertions failed.'
    }

    Write-Host ('Renderer3D native/Web reflection projection, receiver plane, HDR target, ' +
        'fallback, retry, Viewer, and Sin Star I integration tests passed.')
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE `
        -ErrorAction SilentlyContinue
    Pop-Location
}
