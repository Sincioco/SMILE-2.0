[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testProject = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewerHardeningTests.smileproj'
$expected = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewerHardeningTests.expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Character3DViewerHardeningTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Character3DViewerHardeningTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Character3DViewerHardeningTests'
$identityPath = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\paladin-prototype-asset.json'
$referencePath = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\paladin-reference-images.json'
$viewerSourcePath = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewer.smile'
$profileSourcePath = Join-Path $repositoryRoot 'games\Dragonfall\Character3DViewerProfile.smile'
$adapterSourcePath = Join-Path $repositoryRoot 'games\Dragonfall\DragonfallVisualActor.smile'
$preparationPath = Join-Path $repositoryRoot 'scripts\prepare-dragonfall-arin-prototype.ps1'
$pointerSourcePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\input\pointer_state.c'
$nativeRuntimePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\runtime.c'
$webRuntimePath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$syntaxPath = Join-Path $repositoryRoot 'src\Smile.Language\Syntax.cs'
$graphicsHeaderPath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$graphicsFacadePath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$interactionPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Interaction.smile'
$temporaryPreparation = Join-Path $repositoryRoot `
    'artifacts\temp\dragonfall-arin-prototype-preparation'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Contains([string]$Text, [string]$Expected, [string]$Label) {
    if ($Text.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Label is missing required text: $Expected"
    }
}

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'Build SMILE before running the Character 3D Viewer hardening gate.'
}

Push-Location $repositoryRoot
try {
    $identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json -Depth 30
    $references = Get-Content -LiteralPath $referencePath -Raw | ConvertFrom-Json -Depth 10
    Assert-True ($identity.assetId -ceq 'sin-star-i.character-1.paladin') `
        'The canonical Paladin asset identity changed.'
    Assert-True ($identity.characterName -ceq 'Arin') 'The official character name must be Arin.'
    Assert-True ($identity.partyRole -ceq 'Paladin') 'The party role must remain separate from the name.'
    Assert-True ($identity.prototypeAliases[0].aliasId -ceq 'dragonfall.arin-prototype') `
        'The temporary Dragonfall prototype alias changed.'
    Assert-True (-not $identity.productionReady -and -not $identity.releaseEnabled) `
        'The prototype must not become production-ready or release-enabled.'
    Assert-True ($identity.releaseVisualMode -ceq 'Classic') `
        'Dragonfall release visuals must remain Classic.'
    Assert-True ($null -eq $identity.provenance.projectOrExportId -and
        $null -eq $identity.provenance.termsOrLicenseSnapshot) `
        'Unknown provenance must remain unknown rather than inferred.'
    Assert-True (-not $references.runtimeAsset) `
        'Art-direction reference PNGs must not become runtime assets.'
    Assert-True ($identity.textureQuality.Count -eq 3) `
        'The prototype must report all three source texture semantics.'
    Assert-True (($identity.sockets | Where-Object authored).Count -eq 0) `
        'Prototype-inferred sockets must not be marked production-authored.'
    Assert-True (-not $identity.equipmentStructure.independentlySwappableSword -and
        -not $identity.equipmentStructure.independentlySwappableShield) `
        'The fused prototype mesh must not imply modular equipment.'

    $viewerSource = Get-Content -LiteralPath $viewerSourcePath -Raw
    $profileSource = Get-Content -LiteralPath $profileSourcePath -Raw
    $adapterSource = Get-Content -LiteralPath $adapterSourcePath -Raw
    foreach ($contract in @(
        'Import Smile.Simple3D.CharacterViewer As CharacterViewer',
        'CharacterViewer.AutoFit(',
        'CharacterViewer.AdvanceZoom(',
        'CharacterViewer.AdjustZoomTarget(SmoothZoom, WheelDelta, 8)',
        'Const DEFAULT_ZOOM_DEGREES = -16',
        'DEFAULT_ZOOM_DEGREES,',
        'CharacterViewer.RetainedPointerDelta(',
        'Const ORBIT_POINTER_DIVISOR = 1',
        'PressedKey = KEY_O',
        'Const IDLE_RESET_MILLISECONDS = 10000',
        'Call AdvanceIdleReset()',
        'Call RestartPresentation()',
        'Sub AdvanceAutoOrbit()',
        'Call CreateSocketGizmos()',
        'Graphics3D.SetMaterialInspection3D(',
        'Result = "11-clip candidate"',
        'Game Window "SMILE 2.0 - Character 3D Viewer" Size 1600 By 640',
        'Window_Width()',
        'Window_Height()',
        'Draw Text "Zoom / FPS"',
        'Call CreateStudioGrid()',
        'PressedKey = KEY_F',
        'PressedKey = KEY_G',
        'Sub ToggleFloorAndGrid()',
        'Opacity 80',
        'Call AdvanceFrameRate()',
        'LMB pan  •  MMB orbit  •  Wheel zoom  •  RMB reset')) {
        Assert-Contains $viewerSource $contract 'Character 3D Viewer'
    }
    Assert-True ($viewerSource.IndexOf(
        'Fill Rectangle 0, 0, 1600, 70',
        [System.StringComparison]::Ordinal) -lt 0) `
        'The hidden top panel background returned.'
    Assert-Contains (Get-Content -LiteralPath (Join-Path $repositoryRoot `
        'games\Dragonfall\Character3DViewer.smileproj') -Raw) `
        '<ResponsiveWindow>true</ResponsiveWindow>' 'Character 3D Viewer project'
    Assert-Contains $profileSource `
        'Result.AssetId = "sin-star-i.character-1.paladin.v5-4"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.DisplayName = "Arin"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.PartyRole = "Paladin"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.GroundOffset = -10' 'Viewer profile'
    Assert-Contains $profileSource 'Result = 11' 'Viewer profile'
    Assert-Contains $profileSource 'AnimationArticulated.sm3d' 'Viewer fixture profile'
    foreach ($contract in @(
        'RequestedClipNames[Slot] = ClipName',
        'ActualClipNames[Slot] = ClipName',
        'Public Function RequestedClip(',
        'Public Function ActualClip(',
        'Public Function CurrentProductionClipReady(')) {
        Assert-Contains $adapterSource $contract 'Dragonfall visual adapter'
    }

    $preparationSource = Get-Content -LiteralPath $preparationPath -Raw
    foreach ($contract in @(
        'Get-TextureImageIndex',
        '$viewOffset -gt $declaredBinaryLength',
        '[Math]::Abs($data.Stride)',
        '$pixels[$x * 4 + 2] = 255',
        'ArinPrototype.preparation-manifest.json',
        'Synthetic Arin publication failure',
        'Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force')) {
        Assert-Contains $preparationSource $contract 'Arin preparation'
    }

    $pointerSource = Get-Content -LiteralPath $pointerSourcePath -Raw
    $nativeRuntime = Get-Content -LiteralPath $nativeRuntimePath -Raw
    $webRuntime = Get-Content -LiteralPath $webRuntimePath -Raw
    $syntax = Get-Content -LiteralPath $syntaxPath -Raw
    $graphicsHeader = Get-Content -LiteralPath $graphicsHeaderPath -Raw
    $graphicsFacade = Get-Content -LiteralPath $graphicsFacadePath -Raw
    $interaction = Get-Content -LiteralPath $interactionPath -Raw
    Assert-Contains $pointerSource 'wheel_remainder / units_per_step' 'Native pointer accumulator'
    Assert-Contains $nativeRuntime 'return SMILE_KEY_O;' 'Native O-key mapping'
    Assert-Contains $nativeRuntime 'case WM_CAPTURECHANGED:' 'Native pointer capture handling'
    Assert-Contains $nativeRuntime 'if (smile_pointer.held_buttons != 0)' `
        'Normal native pointer release preservation'
    Assert-Contains $nativeRuntime 'long long smile_pointer_pressed(long long button)' `
        'Stable native pointer snapshot'
    Assert-Contains $webRuntime 'case "KeyO": return 27;' 'Web O-key mapping'
    Assert-Contains $syntax '["KEY_O"] = SyntaxKind.KeyOKeyword' 'Language O-key syntax'
    Assert-Contains $graphicsHeader 'SMILE_3D_MATERIAL_INSPECTION = 122' `
        'Renderer3D command ABI'
    Assert-Contains $graphicsHeader 'SMILE_3D_SET_CAMERA_UP = 123' `
        'Renderer3D camera-up command ABI'
    Assert-Contains $graphicsFacade 'Private Const COMMAND_SET_CAMERA_UP = 123' `
        'Simple3D camera-up command ABI'
    Assert-Contains $graphicsFacade 'Camera.UpDirection.Y = 1' 'Simple3D default camera up direction'
    Assert-Contains $interaction 'Result.UpDirection = CameraUp' 'Continuous vertical camera orbit'
    Assert-Contains $webRuntime 'renderer3DCamera.up' 'Web camera up direction'

    & 'scripts\prepare-dragonfall-arin-prototype.ps1' -Check

    Assert-True (-not (Test-Path -LiteralPath $temporaryPreparation)) `
        'Arin preparation left temporary residue.'

    & $compiler --project $testProject --target windows-x64 --configuration $Configuration `
        --graphics DirectX -o $nativeOutput
    if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening native compilation failed.' }
    & 'scripts\run-bounded-test.cmd' 60 $nativeOutput |
        Set-Content -LiteralPath $nativeLog -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening native execution failed.' }
    $expectedText = (Get-Content -LiteralPath $expected -Raw).Trim()
    $actualText = (Get-Content -LiteralPath $nativeLog -Raw).Trim()
    Assert-True ($actualText -ceq $expectedText) `
        "Viewer hardening native assertions failed: $actualText"

    & $compiler --project $testProject --target web --configuration $Configuration `
        --output-dir $webOutput
    if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web compilation failed.' }
    & node --check (Join-Path $webOutput 'game.js')
    if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web game syntax failed.' }
    & node --check (Join-Path $webOutput 'smile-runtime.js')
    if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web runtime syntax failed.' }
    & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000
    if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web assertions failed.' }

    & 'artifacts\tests\Smile.NativeGraphicsTests.exe'
    if ($LASTEXITCODE -ne 0) { throw 'Native pointer and graphics assertions failed.' }

    Write-Host ('Character 3D Viewer identity, release gate, profile auto-fit, elapsed zoom, ' +
        'retained pointer input, precision wheel, O-key auto-orbit mapping, material inspection, ' +
        'socket metadata, preparation safety, and native/Web parity tests passed.')
}
finally {
    Pop-Location
}
