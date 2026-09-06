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
$cameraSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerCamera.smile'
$playbackSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerPlayback.smile'
$calibrationSourcePath = Join-Path $repositoryRoot `
    'tools\Character3DViewer\ViewerCalibration.smile'
$calibrationEditingSourcePath = Join-Path $repositoryRoot `
    'tools\Character3DViewer\ViewerCalibrationEditing.smile'
$inputSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerInput.smile'
$uiSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerUi.smile'
$gizmoSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerGizmo.smile'
$partySourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerParty.smile'
$actorsSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerActors.smile'
$effectsSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerEffects.smile'
$viewerDragonSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerDragon.smile'
$renderingSourcePath = Join-Path $repositoryRoot 'tools\Character3DViewer\ViewerRendering.smile'
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
    $cameraSource = Get-Content -LiteralPath $cameraSourcePath -Raw
    $playbackSource = Get-Content -LiteralPath $playbackSourcePath -Raw
    $calibrationSource = Get-Content -LiteralPath $calibrationSourcePath -Raw
    $calibrationEditingSource = Get-Content -LiteralPath $calibrationEditingSourcePath -Raw
    $inputSource = Get-Content -LiteralPath $inputSourcePath -Raw
    $uiSource = Get-Content -LiteralPath $uiSourcePath -Raw
    $gizmoSource = Get-Content -LiteralPath $gizmoSourcePath -Raw
    $partySource = Get-Content -LiteralPath $partySourcePath -Raw
    $actorsSource = Get-Content -LiteralPath $actorsSourcePath -Raw
    $effectsSource = Get-Content -LiteralPath $effectsSourcePath -Raw
    $viewerDragonSource = Get-Content -LiteralPath $viewerDragonSourcePath -Raw
    $renderingSource = Get-Content -LiteralPath $renderingSourcePath -Raw
    $profileSource = Get-Content -LiteralPath $profileSourcePath -Raw
    $adapterSource = Get-Content -LiteralPath $adapterSourcePath -Raw
    # Keep architectural wiring checks here. Current behavior is executed by the
    # isolated native harness below instead of pinning obsolete labels/timers.
    foreach ($contract in @(
        'Import Smile.Simple3D.CharacterViewer As CharacterViewer',
        'Import Smile.Tools.Character3DViewerCamera As ViewerCamera',
        'Import Smile.Tools.Character3DViewerCalibration As ViewerCalibration',
        'Import Smile.Tools.Character3DViewerCalibrationEditing As ViewerCalibrationEditing',
        'Import Smile.Tools.Character3DViewerInput As ViewerInput',
        'Import Smile.Tools.Character3DViewerUi As ViewerUi',
        'Import Smile.Tools.Character3DViewerGizmo As ViewerGizmo',
        'Import Smile.Tools.Character3DViewerParty As ViewerParty',
        'Import Smile.Tools.Character3DViewerEffects As ViewerEffects',
        'Import Smile.Tools.Character3DViewerDragon As ViewerDragon',
        'Import Smile.Tools.Character3DViewerRendering As ViewerRendering',
        'ViewerCamera.AdvanceZoom(',
        'ViewerCamera.UpdatePointerControls(',
        'If Pointer_Pressed(POINTER_SECONDARY) Then',
        'Call ResetAll()',
        'Call ToggleScenePause()',
        'Call StepAnimationFrame(-1)',
        'Call StepAnimationFrame(1)',
        'Const FRAME_BUTTON_REPEAT_MILLISECONDS = 300',
        'Const CALIBRATION_MAX_CLIPS = ViewerCalibration.MAX_CLIPS',
        'Sub ToggleDragon()',
        'Sub ToggleSword()',
        'Sub ToggleShield()',
        'Sub ToggleFloorAndGrid()',
        'ViewerEffects.AdvanceScene(',
        'Call ViewerEffects.ToggleFlamePause(Effects)',
        'Call ViewerEffects.ToggleLightningPause(Effects)',
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
        'If ViewerInputState.TimelineScrubbing Then', [System.StringComparison]::Ordinal)
    $outsidePointerReturn = $viewerSource.IndexOf(
        'If Not Pointer_Inside() Then', $timelineCapture, [System.StringComparison]::Ordinal)
    Assert-True ($timelineCapture -ge 0 -and $outsidePointerReturn -gt $timelineCapture) `
        'Timeline pointer ownership must be handled before an outside-window return.'
    Assert-Contains $partySource `
        'Value.HitTarget = 1) Then' `
        'Dragon Party target ownership'
    Assert-Contains $viewerSource `
        'Call ViewerCamera.KeepAboveGround(ViewerCameraState, 0)' `
        'Solid-floor camera comfort'
    foreach ($contract in @(
        'CharacterViewer.RetainedPointerDelta(',
        'CharacterViewer.AdvanceZoom(',
        'BattleCamera.Orbit(',
        'CharacterViewer.KeepCursorAnchor(',
        'Public Sub UpdateResponsiveFit(')) {
        Assert-Contains $cameraSource $contract 'Viewer camera owner'
    }
    foreach ($contract in @(
        'Public Sub SelectClip(',
        'Public Function RuntimeClipForPresentationIndex(',
        'Public Function PresentationIndexForRuntimeClip(',
        'Public Function SelectedClipName(',
        'Public Function SelectedClipLabel(',
        'Public Function ClipLabel(',
        'Public Function FirstClipEvent(',
        'Public Function NearestClipEvent(',
        'Public Function StepFrame(',
        'Public Function SeekFirstFrame(',
        'Public Function SeekTime(',
        'Public Function StartSelectedClip(',
        'Public Function AdvanceDemo(',
        'Public Function DemoSecondsRemaining(')) {
        Assert-Contains $playbackSource $contract 'Viewer playback owner'
    }
    foreach ($contract in @(
        'Private Dim Storage[STORAGE_CAPACITY] As Number',
        'Private Dim PreviousStorage[STORAGE_CAPACITY] As Number',
        'Private Dim UndoStorage[STORAGE_CAPACITY] As Number',
        'Public Function CommitImport(',
        'Public Function Persist(',
        'Public Function Undo(',
        'Public Function ConfigureForActor(',
        'Public Function MinimumValue(',
        'Public Function MaximumValue(',
        'Public Function AdjacentKeyFrame(',
        'Public Function DeleteCurrentKeyAndPersist(',
        'Public Function MoveKeyAndPersist(',
        'Public Function PasteClipboardAndPersist(',
        'Public Function ReloadCurrentKey(',
        'Public Function ClearClipAndPersist(',
        'Public Function ClearAllAndPersist(',
        'Public Sub Evaluate(',
        'Public Function TargetValue(',
        'Public Sub SetTargetValue(',
        'Public Sub ResetTarget(',
        'Public Function ApplyWristOffsets(',
        'Public Function ApplyEquipmentCoupling(',
        'Public Function ApplyEquipmentTransforms(',
        'Public Function EquipmentGripThousandths(',
        'Public Function RestoreEquipmentGrip(')) {
        Assert-Contains $calibrationSource $contract 'Viewer calibration owner'
    }
    foreach ($contract in @(
        'Public Sub EvaluateCurrent(',
        'Public Function ApplyPose(',
        'Public Function CurrentValue(',
        'Public Function GizmoOrigin(',
        'Public Sub BeginEdit(',
        'Public Function FinishEdit(',
        'Public Function CancelEdit(',
        'Public Function SetCurrentValue(',
        'Public Function ResetTarget(',
        'Public Function ImportOrCommit(',
        'Public Function DeleteCurrentKey(',
        'Public Sub CopyCurrentKey(',
        'Public Function ReloadCurrentKey(',
        'Public Function PasteCurrentKey(',
        'Public Function ClearSelectedClip(',
        'Public Function ClearAll(',
        'Public Function SaveCurrentFrame(',
        'Public Function UndoLastChange(')) {
        Assert-Contains $calibrationEditingSource $contract 'Viewer calibration editing owner'
    }
    foreach ($contract in @(
        'Public Function ClassifyArrow(',
        'Public Function ClassifyInspectorKey(',
        'Public Sub CancelCaptures(',
        'Public Function HasPointerCapture(')) {
        Assert-Contains $inputSource $contract 'Viewer input owner'
    }
    foreach ($contract in @(
        'Import Smile.UI.Controls As UI',
        'Public Sub ResetCalibration(',
        'Public Sub BeginCalibrationEdit(',
        'Public Sub FinishCalibrationEdit(',
        'Public Function PlaybackLabel(',
        'Public Function CalibrationTargetLabel(',
        'Public Function BackgroundLabel(',
        'Public Function DragonLabel(',
        'Public Function MaterialInspectionLabel(',
        'Public Function DemoLabel(',
        'Public Function PartyRoleLabel(',
        'Public Function StatusPanelX(',
        'Public Function AnimationButtonX(',
        'Public Function GlowButtonX(',
        'Public Function TimelineWidth(',
        'Public Function TimelineFrameAtPointer(',
        'Public Function TimelineKeyframeAtPointer(',
        'Public Function LayoutTooSmall(',
        'Public Function CharacterTabsContain(',
        'Public Function CharacterTabAtPointer(',
        'Public Function TimelineContains(',
        'Public Function CalibrationGizmoButtonContains(',
        'Public Function TransferActionAtPointer(',
        'Public Function CalibrationPanelContains(',
        'Public Function InspectorBlocksScene(',
        'Public Function InspectorActionAtPointer(',
        'Public Function CalibrationPointerAction(',
        'Public Function CalibrationValueAtPointer(',
        'Public Function ApplyCalibrationSelection(',
        'Public Function UpdateCameraSliders(',
        'Public Function UpdateCalibrationSlider(',
        'Public Sub DrawMinimumSizeNotice(',
        'Public Sub DrawHeader(',
        'Public Sub DrawInspectorToolbar(',
        'Public Sub DrawDemoControl(',
        'Public Sub DrawProfileEffectControls(',
        'Public Sub DrawAnimationDetails(',
        'Public Sub DrawViewControls(',
        'Public Sub DrawFooterMessage(',
        'Public Sub DrawPauseStatus(',
        'Public Sub DrawRecoveryOverlay(',
        'Public Sub DrawCharacterStatusSummary(',
        'Public Sub DrawCameraControls(',
        'Public Sub DrawAnimationButtons(',
        'Public Sub DrawCalibrationPanel(',
        'Public Sub DrawTimeline(')) {
        Assert-Contains $uiSource $contract 'Viewer UI owner'
    }
    Assert-True (-not $viewerSource.Contains('Function LightingLabel(') -and
        -not $viewerSource.Contains('Function FlamePlaybackLabel(') -and
        -not $viewerSource.Contains('Function VfxPlaybackLabel(') -and
        -not $viewerSource.Contains('Function CalibrationTargetLabel(') -and
        -not $viewerSource.Contains('Function CalibrationAxisLabel(') -and
        -not $viewerSource.Contains('Function CalibrationTransformLabel(') -and
        -not $viewerSource.Contains('Function CalibrationUnitLabel(') -and
        -not $viewerSource.Contains('Function CalibrationResetClipLabel(') -and
        -not $viewerSource.Contains('Function CalibrationResetAllLabel(') -and
        -not $viewerSource.Contains('Function BackgroundLabel(') -and
        -not $viewerSource.Contains('Function DragonLabel(') -and
        -not $viewerSource.Contains('Function MaterialInspectionLabel(') -and
        -not $viewerSource.Contains('Function DemoLabel(') -and
        -not $viewerSource.Contains('Function DemoStatusLabel(') -and
        -not $viewerSource.Contains('Function ProductionClipStatusLabel(') -and
        -not $viewerSource.Contains('Function PartyRoleLabel(') -and
        -not $viewerSource.Contains('Function AutoOrbitLabel(')) `
        'UI presentation-label policy must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('Function StatusPanelX(') -and
        -not $viewerSource.Contains('Function AnimationButtonX(') -and
        -not $viewerSource.Contains('Function GlowButtonX(') -and
        -not $viewerSource.Contains('Function GlowButtonWidth(') -and
        -not $viewerSource.Contains('Function TimelineWidth(') -and
        -not $viewerSource.Contains('Function ViewerLayoutTooSmall(') -and
        -not $viewerSource.Contains('Sub DrawCharacterStatusSummary(') -and
        -not $viewerSource.Contains('Sub DrawCameraControls(') -and
        -not $viewerSource.Contains('Sub DrawCameraSlider(') -and
        -not $viewerSource.Contains('Sub DrawAnimationButtons(') -and
        -not $viewerSource.Contains('Sub DrawCalibrationPanel(') -and
        -not $viewerSource.Contains('Sub DrawTimeline(')) `
        'Inspector layout, status, camera, animation, calibration and timeline drawing must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('Const TIMELINE_LEFT =') -and
        -not $viewerSource.Contains('Const TIMELINE_HEIGHT =') -and
        -not $viewerSource.Contains('Const CALIBRATION_PANEL_X =') -and
        -not $viewerSource.Contains('Const CALIBRATION_PANEL_WIDTH =') -and
        -not $viewerSource.Contains('Const CALIBRATION_SLIDER_X =') -and
        -not $viewerSource.Contains('Const CALIBRATION_SLIDER_WIDTH =') -and
        -not $viewerSource.Contains('Const CALIBRATION_TARGET_SWORD =') -and
        -not $viewerSource.Contains('Const CALIBRATION_TRANSFORM_MOVE =')) `
        'Calibration and timeline geometry and selection constants must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('If PointerInRectangle(292, 96, 68, 22)') -and
        -not $viewerSource.Contains('Else If PointerInRectangle(34, 128, 92, 24)') -and
        -not $viewerSource.Contains('Else If PointerInRectangle(34, 374, 194, 24)')) `
        'Calibration panel pointer classification must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('UI.UpdateSlider(') -and
        -not $viewerSource.Contains('Import Smile.UI.Controls As UI')) `
        'Shared slider hit testing and capture must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('Function TimelineFrameFromPointer(') -and
        -not $viewerSource.Contains('Function TimelineKeyframeAtPointer(') -and
        -not $viewerSource.Contains('Function CalibrationFrameTimeMilliseconds(')) `
        'Timeline frame/key hit math and calibration frame-time conversion must use their production owners.'
    Assert-True (-not $viewerSource.Contains('Function RuntimeClipForPresentationIndex(') -and
        -not $viewerSource.Contains('Function PresentationIndexForRuntimeClip(') -and
        -not $viewerSource.Contains('Function FirstClipEvent(') -and
        -not $viewerSource.Contains('Function NearestClipEvent(') -and
        -not $viewerSource.Contains('Function SelectedClipName(') -and
        -not $viewerSource.Contains('Function SelectedClipLabel(') -and
        -not $viewerSource.Contains('Sub SeekAuthoredEvent(')) `
        'Playback mapping, event queries, labels and dead authored-event code must not return to the coordinator.'
    Assert-True (-not $viewerSource.Contains('Const ANIMATION_DETAILS_Y =') -and
        -not $viewerSource.Contains('Const ANIMATION_DETAILS_MINIMUM_HEIGHT =') -and
        -not $viewerSource.Contains('Sub DrawButton(') -and
        -not $viewerSource.Contains('"CHARACTER VIEWER NEEDS MORE ROOM"') -and
        -not $viewerSource.Contains('"Clip ms / Rate / Samples / Events"') -and
        -not $viewerSource.Contains('"Save Or Cancel The Pose Before Switching"') -and
        -not $viewerSource.Contains('"CHARACTER VIEWER RECOVERY"')) `
        'Inspector chrome, detail, footer/status and recovery drawing must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('Function PointerInRectangle(') -and
        -not $viewerSource.Contains('PointerInRectangle(') -and
        -not $viewerSource.Contains('UI.Contains(') -and
        -not $viewerSource.Contains('ViewerUi.ANIMATION_BUTTON_COLUMN_COUNT') -and
        -not $viewerSource.Contains('PanelLeft + 94, 444') -and
        -not $viewerSource.Contains('PanelLeft + 174, 444')) `
        'Character tabs, transfers, inspector actions and animation-button hit maps must remain in ViewerUi.'
    Assert-True (-not $viewerSource.Contains('Character3D.SocketName(Character') -and
        -not $viewerSource.Contains('Const CALIBRATION_MINIMUM_DEGREES =') -and
        -not $viewerSource.Contains('Const CALIBRATION_MAXIMUM_POSITION =') -and
        -not $viewerSource.Contains('Function CalibrationMinimumValue(') -and
        -not $viewerSource.Contains('Function CalibrationMaximumValue(') -and
        -not $viewerSource.Contains('Sub OpenSavedCalibrationJson(') -and
        -not $viewerSource.Contains('Sub DownloadSavedCalibrationJson(') -and
        -not $viewerSource.Contains('(Pointer_X() - ViewerUi.CALIBRATION_SLIDER_X)')) `
        'Calibration discovery, value bounds, transfer calls and slider mapping must use production owners.'
    Assert-True (-not $viewerSource.Contains('Function PrepareCalibrationImport(') -and
        -not $viewerSource.Contains('Function CommitCalibrationImport(') -and
        -not $viewerSource.Contains('Function SavedCalibrationJson(') -and
        -not $viewerSource.Contains('Sub ClearCalibrationMemory(') -and
        -not $viewerSource.Contains('Sub SaveCalibrationKeyframes(') -and
        -not $viewerSource.Contains('Function CalibrationKeyframeIndex(') -and
        -not $viewerSource.Contains('Function StoreCalibrationKeyframe(') -and
        -not $viewerSource.Contains('Sub RefreshCalibrationSavedStatus(') -and
        -not $viewerSource.Contains('Function CalibrationChannelValue(') -and
        -not $viewerSource.Contains('Sub SetCalibrationChannelValue(') -and
        -not $viewerSource.Contains('Function CurrentAnimationFrame(')) `
        'Calibration storage/key transactions and direct queries must not return as coordinator wrappers.'
    foreach ($contract in @(
        'Public Sub BeginDrag(',
        'Public Sub FinishDrag(',
        'Public Sub Hide(',
        'Public Sub UpdateProjection(',
        'Public Function AxisAtPointer(',
        'Public Function RingPointerDelta(',
        'Public Function DragValueAmount(',
        'Public Sub Draw(')) {
        Assert-Contains $gizmoSource $contract 'Viewer gizmo owner'
    }
    foreach ($contract in @(
        'Public Sub ResetChoreography(',
        'Public Function ResetDemo(',
        'Public Function ApplyEquipmentVisibility(',
        'Public Function BeginInspectorBinding(',
        'Public Function BeginInspectorSelection(',
        'Public Function EndInspectorSelection(',
        'Public Sub BeginPreview(',
        'Public Function RestorePreview(',
        'Public Sub InitializeFormation(',
        'Public Function AdvanceElapsed(',
        'Public Function ApplyFrame(',
        'Public Function AdvanceDemo(',
        'Public Function CreateCompanion(',
        'Public Function UpdateCompanion(',
        'Public Function DragonReactionRequested(',
        'Public Sub ConsumeDragonReaction(',
        'Public Function UpdateDragon(',
        'Public Function UpdateDragonOpponent(',
        'Public Sub UpdateOrinStorm(',
        'Public Function PlaceDragonInspectionParticipants(',
        'Public Sub ApplyAttackCamera(',
        'Public Function ClassifyPointer(',
        'Public Function DrawCompanion(',
        'Public Function DrawDragonOpponent(',
        'Public Sub DrawOverlay(',
        'Public Sub DestroyParticipants(')) {
        Assert-Contains $partySource $contract 'Viewer Party owner'
    }
    foreach ($contract in @(
        'Public Type Context',
        'Public Type EquipmentVisibility',
        'Public Sub ResetEquipmentVisibility(',
        'Public Sub ToggleSwordVisibility(',
        'Public Sub ToggleShieldVisibility(',
        'Public Function ApplyEquipmentVisibility(',
        'Public Function Capture(',
        'Public Sub Apply(',
        'Public Function LoadContext(',
        'Public Function LoadPrimary(',
        'Public Function PreparePrimaryPresentation(',
        'Public Function FaceToward(',
        'Public Function ValidateProfile(',
        'Public Function ArenaYawAdjustment(',
        'Public Function ApplyArenaFacing(',
        'Public Function ApplyPresentationOffset(',
        'Public Sub Destroy(')) {
        Assert-Contains $actorsSource $contract 'Viewer actor owner'
    }
    foreach ($contract in @(
        'Import Smile.Simple3D.SceneVfx3D As SceneVfx3D',
        'Public Function ShouldFreeze(',
        'Result = ExplicitlyPaused',
        'Public Function AdvanceScene(',
        'Public Sub UpdateEquipmentFire(',
        'Public Function UpdateEpicGlow(',
        'Public Function CreateBorrowedEpicGlow(',
        'Public Function UpdateBorrowedEpicGlow(',
        'Public Sub DrawEquipmentFire(',
        'Public Function DrawScene(',
        'Public Sub ResetPlaybackControls(',
        'Public Sub ResetArinAudio(',
        'Public Sub UpdateArinAudio(',
        'Public Function ShutdownShared(')) {
        Assert-Contains $effectsSource $contract 'Viewer effects owner'
    }
    foreach ($contract in @(
        'Public Type State',
        'Public Function Create(',
        'Public Function DesiredClip(',
        'Public Function Update(',
        'Public Function ClawTravel1000(',
        'Public Sub UpdateAudio(',
        'Public Sub Shutdown(')) {
        Assert-Contains $viewerDragonSource $contract 'Viewer Dragon owner'
    }
    foreach ($contract in @(
        'Public Function BeginScene(',
        'Public Sub ResetControls(',
        'Public Function ApplyLighting(',
        'Public Function CycleLighting(',
        'Public Function ResetMaterialInspection(',
        'Public Function CycleMaterialInspection(',
        'Public Function DrawFloor(',
        'Public Function DrawGrid(',
        'Public Sub DestroyBackdrops(',
        'SocketGizmos[4] As Core.Object3D',
        'Public Function CreateSocketGizmos(',
        'Public Function UpdateSocketGizmos(',
        'Public Function DrawSocketGizmos(',
        'Public Sub DestroySocketGizmos(',
        'Public Sub CycleSocketDisplay(',
        'Public Function ArenaFloorExtent(')) {
        Assert-Contains $renderingSource $contract 'Viewer rendering owner'
    }
    foreach ($contract in @(
        'Public Function ConfigureFraming(',
        'CharacterViewer.AutoFit(',
        'Public Function InitialOrbitYaw(',
        'Public Function DefaultZoomDegrees(')) {
        Assert-Contains $cameraSource $contract 'Viewer camera policy owner'
    }
    Assert-True (-not $viewerSource.Contains('Dim CalibrationStorage[') -and
        -not $viewerSource.Contains('Dim CalibrationKeyframeValues[') -and
        -not $viewerSource.Contains('Dim CalibrationUndoStorage[')) `
        'Calibration banks and transaction buffers must not return to Program.smile.'
    Assert-True (-not $viewerSource.Contains('Character3D.SetNodeRotationOffset(') -and
        -not $viewerSource.Contains('Character3D.SetPartNodeOffsetsEnabled(') -and
        -not $viewerSource.Contains('Character3D.SetPartPositionOffset(') -and
        -not $viewerSource.Contains('Character3D.SetPartPivotRotationThousandths(') -and
        -not $viewerSource.Contains('Graphics3D.SetObjectPivotRotationThousandths3D(')) `
        'Calibration application and equipment-transform implementation must remain in ViewerCalibration.'
    Assert-True (-not $viewerSource.Contains('Function EquipmentGripThousandths(') -and
        -not $viewerSource.Contains('Function RoundedGripOffset(') -and
        -not $viewerSource.Contains('Correction.X = ViewerGizmoState.GripAnchor.X')) `
        'Calibration target mapping and grip-preservation math must remain in ViewerCalibration.'
    Assert-True (-not $viewerSource.Contains('Function CurrentCalibrationValue(') -and
        -not $viewerSource.Contains('Function RestoreEquipmentGrip(') -and
        -not $viewerSource.Contains('ViewerCalibration.SetTargetValue(') -and
        -not $viewerSource.Contains('ViewerCalibration.ResetTarget(') -and
        -not $viewerSource.Contains('ViewerCalibration.EquipmentGripThousandths(') -and
        -not $viewerSource.Contains('ViewerGizmoState.GripBasePosition.')) `
        'Calibration edit-session implementation must remain in ViewerCalibrationEditing.'
    Assert-True (-not $viewerSource.Contains('ViewerCalibration.DeleteCurrentKeyAndPersist(') -and
        -not $viewerSource.Contains('ViewerCalibration.CopyKey(') -and
        -not $viewerSource.Contains('ViewerCalibration.ReloadCurrentKey(') -and
        -not $viewerSource.Contains('ViewerCalibration.PasteClipboardAndPersist(') -and
        -not $viewerSource.Contains('ViewerCalibration.ClearClipAndPersist(') -and
        -not $viewerSource.Contains('ViewerCalibration.ClearAllAndPersist(') -and
        -not $viewerSource.Contains('ViewerCalibration.CommitCurrentKey(') -and
        -not $viewerSource.Contains('ViewerCalibration.Undo(') -and
        -not $viewerSource.Contains('ViewerCalibration.ImportOrCommit(')) `
        'Calibration command transactions and pose-refresh ordering must remain in ViewerCalibrationEditing.'
    Assert-True (-not $viewerSource.Contains('Dim TimelineScrubbing As Boolean') -and
        -not $viewerSource.Contains('Dim SliderDragOwner As Number') -and
        -not $viewerSource.Contains('Dim TransformGizmoDragging As Boolean') -and
        -not $viewerSource.Contains('Dim CalibrationEditing As Boolean')) `
        'Input, UI and gizmo state must not return to Program.smile.'
    Assert-True (-not $viewerSource.Contains('ViewerInput.ClassifyArrow(') -and
        -not $viewerSource.Contains('PressedKey = KEY_SPACE') -and
        -not $viewerSource.Contains('PressedKey = KEY_B Then') -and
        -not $viewerSource.Contains('PressedKey = KEY_TAB Then')) `
        'Inspector keyboard policy and queued-arrow classification must remain in ViewerInput.'
    Assert-True (-not $viewerSource.Contains('Function TransformGizmoAxisAtPointer(') -and
        -not $viewerSource.Contains('Function CurrentTransformGizmoOrigin(') -and
        -not $viewerSource.Contains('CharacterViewer.TransformGizmoAxisPointerDelta(') -and
        -not $viewerSource.Contains('ViewerGizmo.RingPointerDelta(') -and
        -not $viewerSource.Contains('Const TRANSFORM_GIZMO_MOVE_DIVISOR =') -and
        -not $viewerSource.Contains('Const TRANSFORM_GIZMO_ROTATE_DIVISOR =') -and
        -not $viewerSource.Contains('Function TransformGizmoRingPointerDelta(') -and
        -not $viewerSource.Contains('Sub DrawTransformGizmo(') -and
        -not $viewerSource.Contains('Sub DrawTransformGizmoMove(') -and
        -not $viewerSource.Contains('Sub DrawTransformGizmoRotate(') -and
        -not $viewerSource.Contains('Sub DrawGizmoStroke(') -and
        -not $viewerSource.Contains('Sub DrawGizmoArrow(')) `
        'Transform gizmo hit testing, drag math and drawing must remain in ViewerGizmo.'
    Assert-True (-not $viewerSource.Contains('Dim PartyElapsed As Number') -and
        -not $viewerSource.Contains('Dim PartyCompanion As') -and
        -not $viewerSource.Contains('Dim SceneVfxClock As') -and
        -not $viewerSource.Contains('Dim Arena As')) `
        'Party, scene VFX and arena resource state must not return to Program.smile.'
    Assert-True (-not $viewerSource.Contains('Dim SwordVisible As Boolean') -and
        -not $viewerSource.Contains('Dim ShieldVisible As Boolean') -and
        -not $viewerSource.Contains('Character3D.SetPartVisible(')) `
        'Equipment visibility state and actor propagation must remain in the actor and Party owners.'
    Assert-True (-not $viewerSource.Contains('Sub CreatePartyCompanion(') -and
        -not $viewerSource.Contains('ViewerActors.LoadContext(') -and
        -not $viewerSource.Contains('Call UseActorContext(Companion)')) `
        'Party companion creation and calibration/glow setup must remain in ViewerParty.'
    Assert-True (-not $viewerSource.Contains('Sub InitializePartyFormation(') -and
        -not $viewerSource.Contains('Sub PreparePartyDragonTurn(') -and
        -not $viewerSource.Contains('ViewerParty.WillPrepareDragonTurn(') -and
        -not $viewerSource.Contains('ViewerParty.AdvanceChoreography(') -and
        -not $viewerSource.Contains('ViewerParty.ApplyFrame(')) `
        'Party reset/formation and per-frame demo behavior must remain in ViewerParty.'
    Assert-True (-not $viewerSource.Contains('Party.DragonReactionPending') -and
        -not $viewerSource.Contains('ViewerDragon.DesiredClip(') -and
        -not $viewerSource.Contains('ViewerDragon.Update(')) `
        'Party Dragon target, reaction and presentation update behavior must remain in ViewerParty.'
    Assert-True (-not $viewerSource.Contains('Character = DragonState.Actor') -and
        -not $viewerSource.Contains('Party.Companion = CaptureActorContext()') -and
        -not $viewerSource.Contains('ViewerDragon.CaptureInspectedActor(') -and
        -not $viewerSource.Contains('ViewerParty.BeginInspectorBinding(')) `
        'Party inspector target selection and participant capture must remain in ViewerParty.'
    Assert-True (-not $viewerSource.Contains('Function ValidateLoadedProfile(') -and
        -not $viewerSource.Contains('Character3D.LoadWithPolicy(') -and
        -not $viewerSource.Contains('Character3D.LocalBounds(') -and
        -not $viewerSource.Contains('CharacterViewer.AutoFit(') -and
        -not $viewerSource.Contains('Character3D.SetScale(') -and
        -not $viewerSource.Contains('Character3D.SetShadows(') -and
        -not $viewerSource.Contains('Function ViewerFloorWidth(') -and
        -not $viewerSource.Contains('Function ViewerFloorDepth(') -and
        -not $viewerSource.Contains('Function InitialViewerOrbitYaw(') -and
        -not $viewerSource.Contains('Function DefaultZoomDegrees(') -and
        -not $viewerSource.Contains('Sub ApplyArenaFacing(') -and
        -not $viewerSource.Contains('Function CharacterArenaYawAdjustment(')) `
        'Profile validation, arena facing, floor extent and camera defaults must remain in focused owners.'
    Assert-True (-not $viewerSource.Contains('Dim LightingIndex As Number') -and
        -not $viewerSource.Contains('Dim MaterialInspection As Number') -and
        -not $viewerSource.Contains('Sub CycleLighting(') -and
        -not $viewerSource.Contains('Sub ApplyLighting(') -and
        -not $viewerSource.Contains('Sub CycleMaterialInspection(')) `
        'Lighting and material-inspection state and behavior must remain in ViewerRendering.'
    Assert-True (-not $viewerSource.Contains('Import Smile.Tools.BattleAudio As BattleAudio') -and
        -not $viewerSource.Contains('BattleAudio.CrossedCue(') -and
        -not $viewerSource.Contains('Play Sound "Assets/Audio/arin-') -and
        -not $viewerSource.Contains('Effects.ArinAudio.Clip =') -and
        -not $viewerSource.Contains('ViewerEffects.UpdateStorm(')) `
        'Arin cue behavior and Orin storm actor routing must remain in effects and Party owners.'
    Assert-True (-not $viewerSource.Contains('Dim SocketGizmos[') -and
        -not $viewerSource.Contains('Dim SocketMarkers As') -and
        -not $viewerSource.Contains('Sub CreateSocketGizmos()') -and
        -not $viewerSource.Contains('Sub UpdateSocketGizmos()') -and
        -not $viewerSource.Contains('Function DrawSocketGizmos()') -and
        -not $viewerSource.Contains('Sub DestroySocketGizmos()') -and
        -not $viewerSource.Contains('Sub CreateStudioGrid()') -and
        -not $viewerSource.Contains('Function DrawStudioGrid()') -and
        -not $viewerSource.Contains('Sub DestroyStudioGrid()')) `
        'Socket and studio-grid state, behavior and lifecycle must remain in ViewerRendering.'
    Assert-True (-not $viewerSource.Contains('Function DrawPartyCompanion()') -and
        -not $viewerSource.Contains('Sub DestroyPartyCompanion()') -and
        -not $viewerSource.Contains('Sub DrawPartyOverlay()') -and
        -not $viewerSource.Contains('Sub DrawPartyCameraDetails(') -and
        -not $viewerSource.Contains('Sub ApplyPartyAttackCamera()') -and
        -not $viewerSource.Contains('Sub ApplyPartyFrame(') -and
        -not $viewerSource.Contains('Sub PlayPartyClip(') -and
        -not $viewerSource.Contains('Function DrawDragonOpponent(') -and
        -not $viewerSource.Contains('Function PartyHomePosition(') -and
        -not $viewerSource.Contains('Function FaceDragon(')) `
        'Party participant update, frame application, cameras, presentation and lifecycle must remain in their owners.'
    Assert-True (-not $viewerSource.Contains('Dim ShieldFire[') -and
        -not $viewerSource.Contains('Dim ShieldFirePoints[') -and
        -not $viewerSource.Contains('Dim SwordTrailPoints[') -and
        -not $viewerSource.Contains('Dim ShieldTrailPoints[') -and
        -not $viewerSource.Contains('Sub UpdateEquipmentFire()') -and
        -not $viewerSource.Contains('Function UpdateEpicGlow() As Boolean') -and
        -not $viewerSource.Contains('Sub ClearEquipmentFire()')) `
        'Equipment effects state, behavior and lifecycle must remain in ViewerEffects.'
    Assert-True (-not $viewerSource.Contains('Dim Dragon As Character3D.Actor') -and
        -not $viewerSource.Contains('Dim DragonBreath As Fire.FireEmitter') -and
        -not $viewerSource.Contains('Sub CreateDragon()') -and
        -not $viewerSource.Contains('Sub ClearDragonOwnedEffects()') -and
        -not $viewerSource.Contains('Character3D.Update(Dragon,')) `
        'Dragon actor, animation, VFX and lifecycle must remain in ViewerDragon.'
    Assert-True (-not $viewerSource.Contains('Window_Activate()')) `
        'The Viewer must not steal foreground by activating its window every frame.'
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
