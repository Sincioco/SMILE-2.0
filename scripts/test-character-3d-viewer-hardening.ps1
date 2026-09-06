[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$NativeOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler = Join-Path $repositoryRoot 'artifacts\compiler\smilec.exe'
$testProject = Join-Path $repositoryRoot 'tools\Character3DViewer\HardeningTests.smileproj'
$expected = Join-Path $repositoryRoot 'tools\Character3DViewer\HardeningTests.expected.txt'
$nativeOutput = Join-Path $repositoryRoot 'artifacts\tests\Character3DViewerHardeningTests.exe'
$nativeLog = Join-Path $repositoryRoot 'artifacts\temp\Character3DViewerHardeningTests.out'
$webOutput = Join-Path $repositoryRoot 'artifacts\web\Character3DViewerHardeningTests'
$identityPath = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\paladin-prototype-asset.json'
$referencePath = Join-Path $repositoryRoot `
    'games\Dragonfall\SourceAssets\Arin\paladin-reference-images.json'
$viewerSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\Program.smile'
$profileSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\Profiles.smile'
$cookedProjectPath = Join-Path $repositoryRoot `
    'tools\Character3DViewer\Character3DViewer.smileproj'
$dragonSourcePath = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Bosses\RedDragon\RedDragonV1.0.original.glb'
$dragonPreparedPath = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Bosses\RedDragon\RedDragonV1.0.static.glb'
$dragonReportPath = Join-Path $repositoryRoot `
    'games\SinStarI\SourceAssets\Bosses\RedDragon\RedDragonV1.0.static.json'
$adapterSourcePath = Join-Path $repositoryRoot 'games\Dragonfall\DragonfallVisualActor.smile'
$preparationPath = Join-Path $repositoryRoot 'scripts\prepare-dragonfall-arin-prototype.ps1'
$pointerSourcePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\input\pointer_state.c'
$nativeRuntimePath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\runtime.c'
$webRuntimePath = Join-Path $repositoryRoot 'src\Smile.Compiler\WebOutputWriter.cs'
$syntaxPath = Join-Path $repositoryRoot 'src\Smile.Language\Syntax.cs'
$graphicsHeaderPath = Join-Path $repositoryRoot 'src\Smile.NativeRuntime\graphics\graphics3d.h'
$graphicsFacadePath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Graphics3D.smile'
$interactionPath = Join-Path $repositoryRoot 'libraries\Smile.Simple3D\Interaction.smile'
$arinBuilderPath = Join-Path $repositoryRoot 'scripts\build-arin-v5-7-idle-checkpoint.py'
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

& (Join-Path $repositoryRoot 'tools\Character3DViewer\Prepare-BuildAssets.ps1')

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
    # Keep architectural wiring checks here. Current behavior is executed by the
    # isolated native harness below instead of pinning obsolete labels/timers.
    foreach ($contract in @(
        'Import Smile.Simple3D.CharacterViewer As CharacterViewer',
        'Import Smile.Simple3D.LightPool3D As LightPool3D',
        'Import Smile.Simple3D.SceneVfx3D As SceneVfx3D',
        'Import Smile.UI.Controls As UI',
        'CharacterViewer.AutoFit(',
        'CharacterViewer.AdvanceZoom(',
        'CharacterViewer.RetainedPointerDelta(',
        'If Pointer_Pressed(POINTER_SECONDARY) Then',
        'Call ResetAll()',
        'Call ToggleScenePause()',
        'Call StepAnimationFrame(-1)',
        'Call StepAnimationFrame(1)',
        'Const FRAME_BUTTON_REPEAT_MILLISECONDS = 300',
        'Const CALIBRATION_MAX_KEYFRAMES = 256',
        'Sub ToggleDragon()',
        'Sub ToggleSword()',
        'Sub ToggleShield()',
        'Sub ToggleFloorAndGrid()',
        'SceneVfx3D.Advance(SceneVfxClock, Camera,',
        'OrinLight = LightPool3D.Acquire()',
        'Playback.ScenePaused = Not Playback.ScenePaused',
        'Const ZOOM_IN_LIMIT = -144',
        'Window_Width()',
        'Window_Height()',
        'Session.Ready = Window_Title(ViewerTitle()) And Session.Ready')) {
        Assert-Contains $viewerSource $contract 'Character 3D Viewer'
    }
    Assert-True (-not $viewerSource.Contains('IDLE_RESET_MILLISECONDS') -and
        -not $viewerSource.Contains('AdvanceIdleReset')) 'Automatic idle reset must not return.'
    Assert-True (-not $viewerSource.Contains('ClearanceRadius') -and
        -not $viewerSource.Contains('MinimumSeparation')) `
        'Party attack approaches must not be displaced by animated full-model bounds.'
    $timelineCapture = $viewerSource.IndexOf(
        'If TimelineScrubbing Then', [System.StringComparison]::Ordinal)
    $outsidePointerReturn = $viewerSource.IndexOf(
        'If Not Pointer_Inside() Then', $timelineCapture, [System.StringComparison]::Ordinal)
    Assert-True ($timelineCapture -ge 0 -and $outsidePointerReturn -gt $timelineCapture) `
        'Timeline pointer ownership must be handled before an outside-window return.'
    Assert-Contains $viewerSource `
        'PartyHitTarget = 1) Then' `
        'Dragon Party target ownership'
    Assert-Contains $viewerSource `
        'Call CharacterViewer.KeepCameraAboveGround(Camera, 0)' `
        'Solid-floor camera comfort'
    Assert-Contains $profileSource `
        'Public Function PartyAttackName(ProfileIndex As Number, AttackCycle As Number) As Text' `
        'Party attack rotation'
    & (Join-Path $PSScriptRoot 'test-arin-calibration.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Calibration persistence checks failed.' }
    & (Join-Path $PSScriptRoot 'test-viewer-calibration-native.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Viewer native behavior checks failed.' }
    Assert-True ($viewerSource.IndexOf(
        'Fill Rectangle 0, 0, 1600, 70',
        [System.StringComparison]::Ordinal) -lt 0) `
        'The hidden top panel background returned.'
    $cookedProjectSource = Get-Content -LiteralPath $cookedProjectPath -Raw
    Assert-Contains $cookedProjectSource '<ResponsiveWindow>true</ResponsiveWindow>' `
        'Character 3D Viewer project'
    Assert-Contains $cookedProjectSource `
        'BuildAssets\RedDragon\red-dragon-v1.1-animated.glb' 'Cooked Character 3D Viewer project'
    Assert-Contains $cookedProjectSource `
        'LogicalPath="Assets\Generation2\RedDragon\RedDragon.sm3d"' `
        'Cooked Character 3D Viewer project'
    $cookedProjectXml = [xml]$cookedProjectSource
    $dragonAssets = @($cookedProjectXml.SmileProject.ItemGroup.Model3DAsset | Where-Object {
        $_.LogicalPath -eq 'Assets\Generation2\RedDragon\RedDragon.sm3d'
    })
    Assert-True ($dragonAssets.Count -eq 1 -and $dragonAssets[0].Profile -ceq 'Character' -and `
        $dragonAssets[0].Descriptor -ceq 'BuildAssets\RedDragon\RedDragonV11.sm3d.json') `
        'The animated Dragon must use its own Character cooking descriptor.'
    Assert-True ((Get-FileHash -LiteralPath $dragonSourcePath -Algorithm SHA256).Hash -ceq `
        '4A90AC7BCD5E0BEA9D0747CBB3E4B3B9379E1DCE2303DBA7797F6D0E72996D88') `
        'The preserved Red Dragon GLB differs from the user-supplied source.'
    Assert-True (Test-Path -LiteralPath $dragonPreparedPath -PathType Leaf) `
        'The prepared Red Dragon static GLB is missing.'
    $dragonReport = Get-Content -LiteralPath $dragonReportPath -Raw | ConvertFrom-Json
    Assert-True ($dragonReport.meshObjects -eq 64 -and `
        $dragonReport.removedDegenerateFaces -eq 4 -and `
        $dragonReport.outputTriangles -eq 9912) `
        'The prepared Red Dragon geometry contract changed.'
    $dragonPackagePath = Join-Path $repositoryRoot 'games\SinStarI\SourceAssets\Bosses\RedDragon\RedDragonV11'
    $dragonPackage = Get-Content (Join-Path $dragonPackagePath 'red-dragon-v1.1-package.json') -Raw | ConvertFrom-Json
    Assert-True ($dragonPackage.triangles -eq 9912 -and -not $dragonPackage.geometryChanged -and `
        @($dragonPackage.bones.PSObject.Properties).Count -eq 24 -and $dragonPackage.sockets.Count -eq 6 -and `
        ($dragonPackage.clips -join ',') -ceq 'Idle,Roar,FireBreath,ClawStrike,Hit,Fireball') `
        'The Dragon preview rig, six clips and unchanged geometry contract changed.'
    Assert-True ((Get-FileHash (Join-Path $dragonPackagePath 'red-dragon-v1.1-animated.glb')).Hash -ceq `
        $dragonPackage.modelSha256) 'The animated Dragon differs from its canonical package manifest.'
    Assert-Contains $profileSource `
        'Result.AssetId = "sin-star-i.character-1.paladin"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.CandidateVersion = "v5.7"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.DisplayName = "Arin"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.PartyRole = "Paladin"' 'Viewer profile'
    Assert-Contains $profileSource 'Result.DesiredWorldHeight = 100' 'Viewer profile'
    Assert-Contains $profileSource `
        'Public Function EpicGlowAvailable(ProfileIndex As Number) As Boolean' 'Viewer profile'
    Assert-Contains $profileSource `
        'Public Function EpicGlowVisibleByDefault(ProfileIndex As Number) As Boolean' 'Viewer profile'
    Assert-Contains $profileSource 'Result.ExpectedClipCount = 9' 'Current nine-clip Viewer profile'
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
    $arinBuilder = Get-Content -LiteralPath $arinBuilderPath -Raw
    Assert-Contains $pointerSource 'wheel_remainder / units_per_step' 'Native pointer accumulator'
    Assert-Contains $nativeRuntime 'return SMILE_KEY_O;' 'Native O-key mapping'
    Assert-Contains $nativeRuntime 'return SMILE_KEY_P;' 'Native P-key mapping'
    Assert-Contains $nativeRuntime 'return SMILE_KEY_B;' 'Native B-key mapping'
    Assert-Contains $nativeRuntime 'return SMILE_KEY_CONTROL;' 'Native Control-key mapping'
    Assert-Contains $nativeRuntime 'case WM_CAPTURECHANGED:' 'Native pointer capture handling'
    Assert-Contains $nativeRuntime 'smile_pointer_reconcile_buttons(wparam);' `
        'Self-healing native pointer drag state'
    Assert-Contains $pointerSource 'state->pressed_buttons |= pressed_buttons;' `
        'Recovered native pointer press edge'
    Assert-Contains $pointerSource 'state->released_buttons |= released_buttons;' `
        'Recovered native pointer release edge'
    Assert-Contains $nativeRuntime 'if (smile_pointer.held_buttons != 0)' `
        'Normal native pointer release preservation'
    Assert-Contains $nativeRuntime 'long long smile_pointer_pressed(long long button)' `
        'Stable native pointer snapshot'
    Assert-Contains $webRuntime 'case "KeyO": return 27;' 'Web O-key mapping'
    Assert-Contains $webRuntime 'case "KeyP": return 31;' 'Web P-key mapping'
    Assert-Contains $webRuntime 'case "KeyB": return 32;' 'Web B-key mapping'
    Assert-Contains $webRuntime 'case "ControlLeft": return 33;' 'Web Control-key mapping'
    Assert-Contains $syntax '["KEY_O"] = SyntaxKind.KeyOKeyword' 'Language O-key syntax'
    Assert-Contains $syntax '["KEY_P"] = SyntaxKind.KeyPKeyword' 'Language P-key syntax'
    Assert-Contains $syntax '["KEY_B"] = SyntaxKind.KeyBKeyword' 'Language B-key syntax'
    Assert-Contains $syntax '["KEY_CONTROL"] = SyntaxKind.KeyControlKeyword' `
        'Language Control-key syntax'
    Assert-Contains $graphicsHeader 'SMILE_3D_MATERIAL_INSPECTION = 122' `
        'Renderer3D command ABI'
    Assert-Contains $graphicsHeader 'SMILE_3D_SET_CAMERA_UP = 123' `
        'Renderer3D camera-up command ABI'
    Assert-Contains $graphicsFacade 'Private Const COMMAND_SET_CAMERA_UP = 123' `
        'Simple3D camera-up command ABI'
    Assert-Contains $graphicsFacade 'Camera.UpDirection.Y = 1' 'Simple3D default camera up direction'
    Assert-Contains $interaction 'Result.UpDirection = CameraUp' 'Continuous vertical camera orbit'
    Assert-Contains $interaction `
        'Offset.X = BaseCamera.Position.X - BaseCamera.Target.X' `
        'Target-anchored camera orbit'
    Assert-Contains $interaction `
        'Result.Position.X = BaseCamera.Target.X + Controls.PanX + Orbited.X' `
        'Target-anchored camera orbit'
    Assert-Contains $arinBuilder 'LEFT_WRIST_OUTWARD_ROLL_DEGREES = 135.0' `
        'Arin shield-wrist correction'
    Assert-Contains $arinBuilder 'RIGHT_WRIST_OUTWARD_ROLL_DEGREES = -135.0' `
        'Arin sword-wrist correction'
    Assert-Contains $arinBuilder 'SWORD_ATTACHMENT_ROTATION = (0.0, 135.0, 0.0)' `
        'Arin sword attachment alignment'
    Assert-Contains $arinBuilder 'SHIELD_ATTACHMENT_ROTATION = (0.0, -45.0, 0.0)' `
        'Arin shield attachment facing correction'
    Assert-Contains $arinBuilder '"stabilizedSwordArmActions": stabilized_sword_actions' `
        'Arin equipped sword-arm stabilization report'
    Assert-Contains $arinBuilder 'load_wrist_references(t_pose_fbx, target_bones)' `
        'Arin T-pose wrist reference'
    Assert-Contains $arinBuilder `
        'normalized_wrist_bones = normalize_wrist_rotations(actions, wrist_references)' `
        'Arin per-clip wrist normalization'
    Assert-Contains $arinBuilder '"wristMaximumDeviationDegrees": wrist_deviations' `
        'Arin wrist validation report'
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

    if (-not $NativeOnly) {
        & $compiler --project $testProject --target web --configuration $Configuration `
            --output-dir $webOutput
        if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web compilation failed.' }
        & node --check (Join-Path $webOutput 'game.js')
        if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web game syntax failed.' }
        & node --check (Join-Path $webOutput 'smile-runtime.js')
        if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web runtime syntax failed.' }
        & node 'scripts\run-web-test.js' $webOutput --expected $expected --timeout 60000
        if ($LASTEXITCODE -ne 0) { throw 'Viewer hardening Web assertions failed.' }

    }


    & 'artifacts\tests\Smile.NativeGraphicsTests.exe'
    if ($LASTEXITCODE -ne 0) { throw 'Native pointer and graphics assertions failed.' }

    Write-Host ('Character 3D Viewer identity, release gate, profile auto-fit, elapsed zoom, ' +
        'self-healing pointer drags, precision wheel, O-key auto-orbit mapping, material inspection, ' +
        'socket metadata and preparation safety passed. Web checks skipped: ' + $NativeOnly)
}
finally {
    Pop-Location
}
