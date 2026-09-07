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
    Assert-Contains $nativeSource 'smile_front_cull_raster_state3d' `
        'Native reflection winding reversal'
    Assert-Contains $webOwner 'renderer3DReflection.pass = true' `
        'Web reflected-view pass'
    Assert-Contains $webWriter `
        'gl.cullFace(front!==renderer3DReflection.pass?gl.FRONT:gl.BACK)' `
        'Web reflection winding reversal'
    Assert-Contains $nativeOwner 'longest > 2048' `
        'Native bounded reflection target'
    Assert-Contains $webOwner 'longest > 2048' `
        'Web bounded reflection target'
    Assert-Contains $nativeOwner 'failed_revision' `
        'Native failed-allocation retry cache'
    Assert-Contains $webOwner 'failedRevision' `
        'Web failed-allocation retry cache'

    Assert-Contains $nativeSource `
        'smile_reflections_softness_percent() / 100.0f' `
        'Native reflection softness'
    Assert-Contains $webOwner 'renderer3DReflection.softness / 100' `
        'Web reflection softness'
    Assert-Contains $nativeSource 'reflectionTexture.Sample' `
        'Native bounded reflection filtering'
    Assert-Contains $webWriter 'texture(reflectionTexture' `
        'Web bounded reflection filtering'
    Assert-Contains $nativeSource 'smile_3d_draw_backdrop(context, target, &viewport)' `
        'Native screen-fixed backdrop capture'
    Assert-Contains $webOwner 'renderer3DPostPass(renderer3DReflection.framebuffer' `
        'Web screen-fixed backdrop capture'

    Assert-Contains $viewerUi 'Floor Reflections: On' 'Viewer reflection label'
    Assert-Contains $viewerUi 'Floor Reflections: Off' 'Viewer reflection label'
    Assert-Contains $viewerUi 'Floor Reflections: Unavailable' `
        'Viewer fallback label'
    Assert-Contains $viewerParty 'Result.Consumed = True' `
        'Party reflection control ownership'
    Assert-Contains $gamePreview 'Import Smile.Simple3D.Arena3D As Arena3D' `
        'Sin Star I shared arena reuse'
    Assert-Contains $gamePreview 'Import Smile.Simple3D.Interaction As Interaction' `
        'Sin Star I shared camera reuse'
    Assert-Contains $gamePreview 'Character3D.PlayMode(' `
        'Sin Star I live animated actor'
    Assert-Contains $gamePreview 'Character3D.SetUnusedAssetCacheLimit(0)' `
        'Sin Star I renderer-reset re-entry synchronization'
    if ($gamePreview.IndexOf('Character3DViewer',
            [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw 'Sin Star I preview imports or copies a Character Viewer owner.'
    }

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
        --timeout 60000 --renderer3d
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

    Write-Host ('Renderer3D native/Web reflection state, eligibility, ownership, ' +
        'fallback, retry, Viewer, and Sin Star I integration tests passed.')
}
finally {
    Remove-Item Env:SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE `
        -ErrorAction SilentlyContinue
    Pop-Location
}
