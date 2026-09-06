# Character Viewer Architecture

This map describes the behavior-preserving refactor that began from reviewed commit
`5bfd4f96ee838ca1b6b255c28c4117b0e0a5ec7b`. It is a navigation and ownership
contract, not a new feature specification. `Program.smile` remains the executable
story; subsystem state must not be gathered into a replacement god object.

## Preserved frame order

The coordinator retains this dependency order:

1. Read queued keyboard input, route commands, then route UI pointer ownership before
   camera pointer handling.
2. Sample and clamp the shared frame clock. Feed raw time to frame-rate observation;
   use separate animation, camera, and presentation elapsed values.
3. Advance the sequence and Party choreography, then update the primary actor.
4. Apply presentation grounding, current-frame calibration, wrists, and equipment
   coupling.
5. Advance smooth zoom and auto-orbit; apply responsive fit, screen-space controls,
   close-up framing, and the calibration orbit anchor.
6. Place calibrated equipment and update equipment effects, companions, and Dragons.
7. Apply the current-pose Party camera and floor clearance.
8. Update audio, Orin storm state, and the once-per-scene VFX clock.
9. Update optional socket gizmos and current world bounds.
10. Begin the scene; draw floor/grid, actors, equipment glow, shared effects, optional
    gizmos, and end the scene.
11. Draw flash/UI overlays, present the frame, and test window closure.

`CaptureViewerFailure` remains immediately after the stages it identifies. The first
Viewer/renderer error is retained until explicit retry or reload.

## R7.5 responsibility-completion audit

The September 6, 2026 audit at repository commit `ed5e6bb` found that the state
extractions through R7 did not finish the corresponding implementation moves.
`Program.smile` still contains 8,319 lines and 233 procedures. In particular, it
still implements substantial Party transitions, Dragon timing and turn behavior,
companion updates, Party pointer handling and overlays, inspector/calibration UI,
gizmo drawing and interaction, and resource lifecycle.
Passing module tests proves the extracted seams but does not make those remaining
procedures owned by the new modules.

`ViewerActors.smile` did not exist at the audit baseline. Earlier text naming it as
the current owner was an architecture target stated as a completed fact. R7.5 has now
introduced that focused owner for the actor context and actor load/update/draw/destroy
lifecycle. Inspector selection and calibration/effect coupling still remain to move.
The next bounded checkpoint moved equipment Fire, glow, trails,
shield-rim behavior, Orin-storm controls, shared scene-VFX drawing and their resource
lifecycle into `ViewerEffects.smile`. The coordinator retains only the ordering and
actor-selection decisions needed to call that owner. The Dragon checkpoint then moved
the Dragon actor and its presentation lifecycle to `ViewerDragon.smile`. The current
Party presentation checkpoint moves pointer classification, companion drawing and
destruction, Party overlay rendering, camera-detail rendering and Party presentation
labels into `ViewerParty.smile`. Subsequent bounded checkpoints moved Party battle-camera
construction, preview mode/restore behavior, clip/position/facing command application,
generic actor-facing math and companion glow attachment/update into their existing
owners. Companion load/calibration switching and inspector selection still remain in
`Program.smile`. The transform-gizmo checkpoint then moved hit testing, retained
slow-drag ring math, hover selection and all axis/ring drawing into `ViewerGizmo`.
The calibration-application checkpoint then moved wrist offsets, equipment coupling,
part transforms, glow propagation and exact failure-stage results into
`ViewerCalibration`, while grounded presentation offsets moved into `ViewerActors`.
Target/channel mapping, target reset behavior and bounded inverse grip-preservation
math then moved into `ViewerCalibration`; its fixture now declares its own math
dependency instead of inheriting one from the coordinator.
The UI presentation-policy checkpoint then moved the live playback, calibration,
background, Dragon, material, demo and Party-role label implementations into
`ViewerUi`. Three unused label routines were deleted rather than relocated. Panel
layout, drawing and pointer command routing remain substantial coordinator work and
are not claimed as moved by this checkpoint.
The next UI control-drawing checkpoint moved responsive status-panel geometry,
minimum-layout policy, character-status rendering, camera sliders and animation
buttons into `ViewerUi`. Pointer hit testing consumes the same UI-owned dimensions.
Calibration-panel/timeline drawing, the overall inspector overlay and their pointer
command routing remain in `Program.smile`.
The calibration/timeline drawing checkpoint then moved both complete renderers plus
their shared raw geometry and selection constants into `ViewerUi`. They borrow only
explicit UI, calibration, input, actor and current-selection values. The overall
inspector overlay, pointer classification/dispatch and calibration edit commands
remain in `Program.smile`.
The calibration-pointer checkpoint then moved the panel's ordered hit classification,
disabled wrist Move rule and target/axis/transform selection application into
`ViewerUi`. `Program.smile` retains a readable action dispatcher because the commands
cross calibration, gizmo, effects and playback owners.
The timeline-pointer checkpoint then moved pointer-to-frame conversion and calibrated
keyframe-marker hit testing into `ViewerUi`, beside the timeline geometry and drawing
they consume. The coordinator now calls `ViewerCalibration.FrameTimeMilliseconds`
directly instead of retaining a pass-through wrapper. Seek/edit command dispatch and
the broader inspector pointer sequence remain visible in `Program.smile` because they
coordinate playback, calibration and input owners.
The inspector-presentation checkpoint then moved the minimum-size notice, header and
character tabs, toolbar/panel shell, demo and profile-specific effect controls,
animation-detail metrics, view controls, footer/pause messaging and recovery overlay
into `ViewerUi`. `DrawInspectorOverlay` remains as an ordering coordinator that gathers
explicit scalar values and invokes the focused Party, UI, timeline and calibration
renderers; it no longer implements those raw presentation leaves.
The general inspector hit-map checkpoint then moved character-tab/transfer/panel
regions, scene blocking, timeline/button priority, profile-specific effect controls
and animation-button classification into `ViewerUi`. `HandleInterfacePointer` and
`HandleInspectorPointer` retain command dispatch and capture sequencing across Party,
playback, camera, calibration, effect and session owners; the deleted rectangle wrapper
is not retained as a second geometry source.
The calibration discovery/bounds checkpoint then moved wrist-socket discovery,
availability policy and workspace configuration into `ViewerCalibration`, along with
rotation/position edit bounds. Slider-to-value conversion moved beside its geometry in
`ViewerUi`. The Open/Download and minimum/maximum pass-throughs were deleted; import,
Undo, save/edit transaction coordination remains to move in later bounded work.
The bounded stage must not
introduce a whole-application state record or a replacement `ViewerApplication`
module.

| Still implemented in `Program.smile` | Current evidence | Intended focused owner |
|---|---|---|
| Party transitions, companion lifecycle and inspector binding | `ViewerParty.AdvanceChoreography`, `ApplyFrame`, `ApplyAttackCamera`, preview operations, pointer classification, drawing and destruction now own their implementations. `ViewerActors.FaceToward` owns reusable facing and `ViewerEffects` owns borrowed companion glow creation/update. `Program.smile` retains companion load/calibration switching, inspector target selection, formation/reset commands and the narrow pointer action dispatcher. | `ViewerParty.smile`, `ViewerActors.smile` and `ViewerEffects.smile`, with cross-owner selection remaining in the coordinator |
| Inspector, calibration panel, timeline, camera controls, buttons and overlays | `ViewerCalibration` owns stored calibration evaluation, wrist-socket discovery/availability, edit bounds, wrist/equipment transform application, target/channel mapping/reset and inverse grip preservation. `ViewerUi` owns live label/layout policy, slider mapping, focused panel drawing, calibration/timeline/general inspector hit classification and raw presentation leaves. `HandleInspectorPointer`, the calibration action dispatcher and edit-session commands remain in `Program.smile` as cross-owner command/capture sequencing; `DrawInspectorOverlay` remains only as the explicit cross-owner draw coordinator. | `ViewerCalibration.smile` for stored edit transactions and `ViewerInput.smile`/`ViewerUi.smile` for interaction and presentation |
| Transform-gizmo command coordination | `ViewerGizmo` now owns projection, hit testing, retained ring-drag math, hover state and complete axis/ring drawing. `Program.smile` retains keyboard/pointer sequencing that begins, applies, cancels or commits calibration edits. | `ViewerInput.smile` and `ViewerCalibration.smile` for the remaining cross-owner command routing |
| General rendering and overlay composition | Socket resources and studio-grid wrappers reside in `ViewerRendering`; labels, responsive geometry, status/camera/animation controls, calibration panel, timeline, inspector chrome, footer/status and recovery drawing reside in `ViewerUi`. `DrawViewerOverlay` and `DrawInspectorOverlay` retain cross-owner ordering and gizmo/Party binding in `Program.smile`. | `ViewerRendering.smile` for scene resources and `ViewerUi.smile` for editor presentation |
| Load/retry/switch/destroy orchestration mixed with resource implementation | `LoadViewer`, `RetryViewer`, `SelectCharacterTab`, `DestroyViewerResources` and related routines | thin `Program.smile` coordinator plus focused session/actor/render/effect owners |

The ownership and symbol maps below describe the required destination. A row is not
completion evidence until the implementation no longer remains in `Program.smile`
and its focused production tests exercise the destination owner.

### Program metrics during responsibility completion

| Checkpoint | Lines | Procedures | Architectural result |
|---|---:|---:|---|
| R7.5 audit baseline at `ed5e6bb` | 8,319 | 233 | State seams existed, but substantial subsystem implementation remained. |
| Party state-machine move | 8,032 | 231 | Party/Dragon-turn state transitions and interpolation moved; a narrow actor command application routine remains in the coordinator. |
| Actor-context/lifecycle move | 8,033 | 231 | The new explicit actor-owner import/contract adds one coordinator line while actor context, companion load, update, draw and destruction move to `ViewerActors`; no line-count compression was used. |
| Equipment-effects behavior/lifecycle move | 7,160 | 205 | From the 8,033-line/231-procedure actor-owner checkpoint, 873 lines and 26 procedures left the coordinator. `ViewerEffects` now owns equipment Fire, shield rim, glow objects, trails, Orin-storm controls, shared scene-VFX drawing, pause/reset controls and teardown. |
| Dragon actor/presentation lifecycle move | 6,943 | 201 | From the 7,160-line/205-procedure effects checkpoint, Dragon actor creation, animation/playback timing, claw travel, head aim, breath continuity, presentation VFX, audio, drawing and destruction moved to `ViewerDragon`. The coordinator retains target and Party-reaction selection in a bounded `UpdateDragon` call. |
| Party presentation/pointer move | 6,805 | 197 | From the 6,943-line/201-procedure Dragon checkpoint, Party pointer hit-map/classification, companion drawing/destruction, full Party overlay/camera-detail rendering and acting/attack labels moved to `ViewerParty`. Focused tests call the classifier and label operations directly; hardening contracts prevent the four deleted presentation/lifecycle routines from returning. |
| Socket/grid rendering move | 6,489 | 185 | From the 6,805-line/197-procedure Party presentation checkpoint, socket fixed-array state, resource creation/failure cleanup, reference-part mapping, per-frame transforms, origin batching, drawing, selection and destruction moved to `ViewerRendering`. Three studio-grid wrappers and an unused socket-status helper were removed; frame-order calls remain directly visible in the coordinator. |
| Party battle-camera move | 6,256 | 181 | From the 6,489-line/185-procedure socket/grid checkpoint, shot eligibility, stable Dragon anchors, hero/Dragon camera construction, charge-shot selection, temporal smoothing, actor clearance and active-camera continuity moved to `ViewerParty.ApplyAttackCamera`. Three redundant Party presentation wrappers were removed; callers now use the production owner directly. |
| Party actor/preview behavior move | 6,173 | 177 | From the 6,256-line/181-procedure battle-camera checkpoint, preview mode and restore behavior plus Party clip/position/facing command application moved to `ViewerParty`; reusable actor facing moved to `ViewerActors`; borrowed companion-glow attachment and socket updates moved to `ViewerEffects`. Focused tests call preview policy, frame application and facing directly, while static contracts reject the four deleted coordinator implementations. |
| Transform-gizmo implementation move | 5,887 | 168 | From the 6,173-line/177-procedure Party actor/preview checkpoint, gizmo hit testing, retained slow-drag ring math, hover selection, move-axis arrows, rotation rings, rear-segment styling and drawing moved with state into `ViewerGizmo`. Nine implementation routines left the coordinator; focused tests call the production owner and static contracts reject their return. |
| Calibration application move | 5,752 | 167 | From the 5,887-line/168-procedure gizmo checkpoint, grounded clip presentation moved to `ViewerActors`; wrist offsets, equipment coupling, sword/shield pivot/position calibration, companion glow propagation and failure stages 71–74 moved to `ViewerCalibration`. Native calibration integration exercises the production owner against real actors and isolated storage; static contracts reject the low-level transform calls from `Program.smile`. |
| Calibration target/grip behavior move | 5,645 | 165 | From the 5,752-line/167-procedure application checkpoint, target/channel mapping, current target reads/writes, reset behavior, equipment grip sampling, inverse grip-preservation math, integer rounding and bounds rejection moved to `ViewerCalibration`. Real repeated-rotation integration calls the production grip owner; two pure helpers and the coordinator math import were removed. |
| UI presentation-policy move | 5,366 | 148 | From the 5,645-line/165-procedure calibration target/grip checkpoint, 14 live label routines left the coordinator and became 13 narrow `ViewerUi` functions; the two playback variants share one explicit verb contract. Three unused label routines were deleted rather than retained. Direct native assertions exercise every live label family and static contracts reject their return to `Program.smile`; panel drawing and pointer routing remain. |
| UI control-drawing move | 5,216 | 138 | From the 5,366-line/148-procedure label checkpoint, responsive panel/animation/glow/timeline geometry, minimum-layout policy, character-status rendering, camera-slider rendering and animation-button rendering moved to `ViewerUi`. Ten procedures left the coordinator; unused slider-progress calculations and four unused slider-owner constants were deleted. Hit testing and drawing share the owner constants/functions. Direct native geometry assertions and production draw calls plus static ownership guards cover the move. |
| Calibration/timeline drawing move | 4,975 | 136 | From the 5,216-line/138-procedure control-drawing checkpoint, the complete calibration panel and timeline renderers moved to `ViewerUi` with explicit borrowed state and values. Twenty-five raw layout/selection constants also left the coordinator so drawing and hit testing share one owner contract. Two substantial procedures and 241 coordinator lines left; unused calibration-slider knob calculations were deleted. Direct native render calls and static contracts cover the production owner. |
| Calibration pointer-classification move | 4,956 | 136 | From the 4,975-line/136-procedure panel checkpoint, ordered calibration hit boxes, the equipment-only Move rule and UI selection mutations moved to `ViewerUi`. The same coordinator procedure now receives an action and dispatches cross-owner commands, so no false procedure-count reduction is claimed. Direct boundary/selection assertions and static contracts protect the move. |
| Timeline pointer-classification move | 4,897 | 133 | From the 4,956-line/136-procedure calibration-pointer checkpoint, pointer-to-frame conversion and calibrated keyframe-marker hit testing moved beside timeline geometry in `ViewerUi`. The redundant calibration frame-time wrapper was deleted and callers now use the production calibration owner directly. Endpoint/key-marker assertions and static contracts reject all three deleted coordinator routines. |
| Inspector presentation-leaf move | 4,793 | 132 | From the 4,897-line/133-procedure timeline-pointer checkpoint, minimum-size, header/tabs, toolbar/shell, demo/effect controls, animation details, view controls, footer/pause status and recovery rendering moved into ten focused `ViewerUi` operations. The UI-only animation-detail constants moved with them and the redundant `DrawButton` wrapper was deleted. `DrawInspectorOverlay` remains as the readable cross-owner draw coordinator. Direct native renderer calls and static text/symbol guards cover every moved leaf. |
| General inspector hit-map move | 4,711 | 131 | From the 4,793-line/132-procedure presentation checkpoint, character-tab, transfer, calibration-panel, timeline, scene-blocking, toolbar, profile-effect, view and animation-button hit classification moved into `ViewerUi`. The coordinator dispatches named actions in the established priority order and retains capture/domain sequencing. The redundant rectangle wrapper and unused locals were deleted. Direct boundary/profile assertions and static geometry guards cover the move. |
| Calibration discovery/bounds move | 4,646 | 127 | From the 4,711-line/131-procedure inspector hit-map checkpoint, wrist-socket discovery, Party-character/part/socket availability policy, workspace configuration and rotation/position bounds moved into `ViewerCalibration`; calibration slider mapping moved into `ViewerUi`. Open/Download and value-bound pass-throughs were deleted. Direct unavailable-actor/bounds/endpoint assertions and static ownership guards cover the move. Import, Undo and edit transactions remain explicit below. |

Substantial implementation still in `Program.smile` after the calibration discovery/bounds checkpoint is
intentionally explicit: startup/retry/tab-switch orchestration; Party
inspector selection, companion load/calibration switching and calibrated actor update,
formation/reset commands and pointer action dispatch;
input capture sequencing and inspector/timeline action dispatch;
calibration edit command coordination and
file-transaction orchestration; playback/timeline operations;
and cross-owner overlay/gizmo composition. `UpdateDragon` remains because the coordinator
selects the explicit Party/inspection target and consumes the Party-reaction request;
actor update, timing, aim, breath and audio behavior are delegated to `ViewerDragon`.
`UpdateOrinStorm` remains because the coordinator
chooses the current borrowed actor and Dragon target; storm simulation itself now
belongs to `ViewerEffects.UpdateStorm`.

## Ownership target map

| Responsibility | Baseline owner | Intended refactor owner | Owned mutable state | Public operations and borrowed dependencies | Lifetime / focused proof |
|---|---|---|---|---|---|
| Startup and failure/session lifecycle | `Program.smile` | `ViewerSession.smile` plus thin `Program.smile` coordinator | Running/readiness, first error/stage, resource epoch, tab/profile and scene mode | Reset/record/capture failure; coordinator borrows renderer and subsystem owners for load/retry/switch/shutdown | One application session; direct module assertions plus native launch, reload and failure fixtures |
| Frame clocks and playback sequence | `Program.smile` plus `CharacterViewer.ClockState` | `ViewerTiming.smile` and `ViewerPlayback.smile` | Frame-rate and clamped clocks; selection, speed, pause/demo sequence | Start/advance/reset/query, clip mode and demo target; borrows current actor/profile only for duration queries | Session; direct native module assertions and playback fixture |
| Actor/inspector binding | `Program.smile` (`ViewerActorContext`, `PartyUi*`, `CalibrationOwnerProfile`) | `ViewerActors.smile` owns `Context`, actor load/update/draw/destroy and reusable facing; `ViewerParty.smile` owns preview state/mode/restore; inspector target and calibration-owner selection remain in the coordinator | Primary/inspected actor identity and temporary Party preview binding | Actor and preview behavior moved; cross-owner selection still borrows the calibration owner from `Program.smile` | Scene/preview; direct context/preview assertions plus integrated Party isolation fixture |
| Camera and transforms | `Program.smile`, `BattleCamera.smile`, shared `Interaction` | `ViewerCamera.smile` and retained `BattleCamera.smile` math | `ViewerCamera.State`: base/live camera, frame, controls, zoom target, fractional pointer remainder, orbit anchor and auto-orbit | Reset/compose/nudge/drag/advance/apply/query; borrows framing profile and current actor bounds without retaining either | Scene; direct integer-output assertions plus native/installed-Chrome controls |
| Calibration editing | `Program.smile`, `CalibrationJson.smile` | `ViewerCalibration.smile` and retained bounded JSON reader | Per-profile key banks, edit/clipboard/Undo/import workspace and selected transform | Configure/load/evaluate/apply transforms/map and reset targets/preserve grips/save/Undo/import/export/query; borrows inspected actor, glow objects and storage primitives | Profile workspace; native/generated-Web isolation, real actor transform/grip integration and malformed imports |
| File transactions and launcher synchronization | Viewer Save/Load statements, synchronizer and launcher | `ViewerCalibration.smile`, `sync-arin-v5-7-calibration.ps1`, `Launch.ps1` | Checked save baseline/pending revision; primary/backup selection | Transaction commit/recovery/watch; canonical JSON is borrowed source of truth | Save/application identity; preservation fixtures |
| Input ownership | `Program.smile` | `ViewerInput.smile` | Pointer capture, timeline/frame repeat and queued command routing | Route keyboard/pointer; borrows UI, calibration and camera operations | Frame/capture; queued-modifier and outside-window fixtures |
| UI and transform gizmo | `Program.smile` | `ViewerUi.smile` and `ViewerGizmo.smile` | Panel visibility, slider owner, opt-in gizmo projection/hover/drag state | UI label/geometry, calibration hit/selection policy and focused panels plus gizmo implementation have moved; coordinator still owns the overall inspector/error overlay and cross-owner pointer/edit command sequencing | Scene/edit; direct label/geometry/panel/hit assertions plus gizmo hit-test/draw, pointer exclusivity and cancel tests |
| Party choreography | `Program.smile` | `ViewerParty.smile` | Participants, turn/stage/timing, guard/hit/KO/revive, preview state, stable shot anchors and Party cameras | Reset/advance/apply actor commands/camera/draw/destroy/bind and restore preview; borrows explicit actor, playback and effect snapshots | Party scene; timing, preview mode/restore, frame application, camera selection/continuity, same-model isolation and inspector fixtures |
| Effects/audio | `Program.smile`, `OrinStorm.smile`, `DragonPresence.smile`, `BattleAudio.smile` | `ViewerEffects.smile` plus retained focused modules | Equipment emitters, trails, leases, scene clocks and visual-continuity epochs; the Dragon owner borrows its scene light lease | Create/update/advance-once/draw/invalidate/destroy; borrows final actor transforms | Scene; direct control/state assertions plus frozen-cut/skipped-cue/lease cleanup tests |
| Dragon actor/presentation | `Program.smile`, `DragonPresence.smile`, `BattleAudio.smile` | `ViewerDragon.smile` with retained focused presence/audio modules | Dragon actor, ownership flag, clip, head aim, breath, continuity epoch, visibility and cue state | Create/update/draw/toggle/destroy; borrows the coordinator-selected target, shared Fire readiness/light lease and visual epoch | Scene; pure clip/travel assertions plus native frozen seek/cut/hide/resume and cue tests |
| Rendering and overlay composition | `Program.smile` | `ViewerRendering.smile` for scene resources; `ViewerUi.smile` for editor overlays | `ViewerRendering.State` owns arena/backdrop/grid/socket render resources including the fixed socket-object array; UI retains transient layout state | Create/update/draw/destroy scene resources; overlays borrow read-only snapshots from owners | Scene; direct socket selection/part-routing assertions plus native/generated-Web draw and resize checks |
| Build/publication | `Build.ps1`, `Prepare-BuildAssets.ps1`, explicit projects | same scripts with explicit module inventory | Disposable staging/publications only | Canonical preflight, compile, selected-output validation | Build; Release/Debug and Full/Low/Medium/High manifests |

Immutable `Character3D` cache entries are shared resources. Actor pose, equipment
visibility, calibration, inspector selection, effects, and scene clocks are never cache
state. Arin, Orin, Dragon, Party, and inspected-actor identities remain distinct.

## Symbol migration target map

| Old symbol/location | Intended owner/symbol | Preservation note |
|---|---|---|
| `AdvanceFrameRate`, `FrameRateElapsed`, `FrameRateFrames`, `CurrentFramesPerSecond` | `ViewerTiming.Advance`, `ViewerTiming.FrameRateState`, `ViewerTiming.FramesPerSecond` | First low-risk extraction; identical 500 ms integer sampling, directly tested |
| `PreviousTime`, `ViewerClock`, copied elapsed/drop counters | `ViewerTiming.ClockState`, `ViewerTiming.Start`, `ViewerTiming.AdvanceClock` | Identical raw sample, clamp and long-pause contract; coordinator consumes explicit animation/camera/presentation outputs |
| `ViewerParty.ActorContext`, companion actor load/update/draw/destroy mapping and `FaceDragon` in `Program.smile` | `ViewerActors.Context`, `Capture`, `Apply`, `LoadContext`, `Update`, `Draw`, `FaceToward` and `Destroy` | Actor handles, per-actor inspection fields and generic facing have a focused owner. Party borrows contexts; calibration and effects remain separate owners. Inspector target selection remains to move. |
| `SelectedClip`, speed, pause/demo counters and helper calculations | `ViewerPlayback.State`, `ResolveAnimationElapsed`, `AdjustSpeed`, `ClipMode`, `Demo*` | Playback state is explicit; actor/profile handles are borrowed for queries and never retained |
| `Ready`, `ViewerError`, first error/stage, tab/profile and resource epoch | `ViewerSession.State`, `ResetFailure`, `RecordError`, `CaptureFailure` | First-failure retention and explicit retry reset preserved; stage capture remains adjacent to the coordinator stage |
| `BaseCamera`, `Camera`, `ViewerFrame`, `CameraControls`, `SmoothZoom`, pointer remainders, calibration orbit anchor and `AutoOrbit*` | `ViewerCamera.State` with `Reset`, `Compose`, `UpdatePointerControls`, `AdvanceZoom`, `AdvanceAutoOrbit`, `UpdateResponsiveFit`, `ApplyCloseUp` and `ApplyCalibrationOrbitAnchor` | Camera interaction state now has one focused owner; `Program.smile` coordinates borrowed profile/bounds and keeps Party shot intent separate for the later Party owner |
| Calibration key banks, selection/edit/clipboard/Undo buffers, import baseline, target mapping/reset, wrist discovery, value bounds, wrist/equipment transforms and grip preservation in `Program.smile` | `ViewerCalibration.State`, module-private bounded workspaces, `ConfigureForActor`, value bounds, target operations, `ApplyWristOffsets`, `ApplyEquipmentCoupling`, `ApplyEquipmentTransforms` and `RestoreEquipmentGrip` | `Program.smile` coordinates UI-visible edit sessions and adapts explicit application results to session failure ownership; actor capability discovery, profile-scoped storage, transform/grip application, codec validation, rollback and import confirmation are direct production-module paths |
| `LoadCalibration`, `SaveCalibration`, raw `CalibrationStorage*`/`CalibrationCandidate*` arrays and in-place key-array edits | `ViewerCalibration.Load`, `Persist`, `PrepareImport`, `CommitImport`, `MoveKey`, `CommitCurrentKey`, `Undo` and focused query operations | Primary/backup recovery, rejected candidates and failed writes preserve the previous valid in-memory and stored revision; tests use disposable identities and buffers |
| Slider/timeline/repeat capture flags and ad hoc queued arrow checks in `Program.smile` | `ViewerInput.State`, `ClassifyArrow` and capture begin/finish/reset operations | State and low-level capture transitions moved; remaining inspector/timeline action dispatch still coordinates input with playback and calibration owners. General hit geometry now comes from `ViewerUi`. Queued Ctrl is sampled from `Key_Event_Held`, and the invalid foreground-stealing `Window_Activate()` probe remains removed. |
| UI visibility, calibration panel/edit/confirmation fields, responsive geometry, calibration/timeline/general inspector hit classification, calibration slider mapping, status/camera/animation/calibration/timeline drawing, inspector chrome/footer/status/recovery rendering and presentation labels in `Program.smile` | `ViewerUi.State` with explicit visibility, calibration reset, edit and confirmation transitions; narrow label/geometry/hit operations plus focused presentation renderers | State transitions, label/layout policy, all current UI hit maps, slider mapping and raw presentation leaves moved. `DrawInspectorOverlay` retains only explicit cross-owner draw ordering; `HandleInspectorPointer` retains named action dispatch and capture sequencing. Dead labels, unused slider calculations/constants, the redundant calibration frame-time wrapper, `DrawButton` pass-through and rectangle wrapper were deleted. Pending edits and imports must continue to block actor switches without changing their owner. |
| Transform-gizmo projection, pointer ownership, drag remainder, hit testing, ring math, drawing and grip state in `Program.smile` | `ViewerGizmo.State` with reset/show-hide/begin/finish/select/projection/hit-test/ring-delta/draw operations | State and implementation moved together. The coordinator retains calibration command sequencing only. Gizmos remain opt-in and hiding retains the unsaved numeric preview for explicit Save or Cancel. |
| Party participants, inspector/preview binding, turn/stage/timing, attack/reaction state, Party battle cameras, pointer hit map, companion presentation/destruction and Party overlay implementation in `Program.smile` | `ViewerParty.State`, two explicit `ParticipantLayout` values, choreography/preview operations, `ApplyFrame`, `ApplyAttackCamera`, `ClassifyPointer`, `DrawCompanion`, `DrawOverlay` and `DestroyParticipants` | State, choreography, preview playback, actor command application, camera construction/continuity, pointer classification, drawing/destruction and overlay rendering moved with direct tests and static ownership contracts. The coordinator retains pointer action dispatch, companion load/calibration switching, formation/reset commands and inspector target selection. Exactly two live Party participants remain explicit and same-model fallback must not alias actor state. |
| Scene VFX clock, equipment Fire/glow/trails, Fire/Lightning pause flags, visual-continuity epochs, light leases and Orin-storm state in `Program.smile` | `ViewerEffects.State`, `PrepareFire`, `PrepareLightning`, `UpdateEquipmentFire`, `UpdateEpicGlow`, `UpdateStorm`, draw operations, independent toggles, continuity invalidation and shared shutdown | State, implementation and lifecycle now move together. No duplicate wrappers or equipment-effect arrays remain in `Program.smile`. Scene pause continues to leave VFX running by default while each family toggle freezes only that family. |
| Arena, floor/grid visibility, backdrop handles/index, socket object array, marker resources, display selection and socket create/update/draw/destroy routines in `Program.smile` | `ViewerRendering.State` with arena/backdrop operations plus `CreateSocketGizmos`, `UpdateSocketGizmos`, `DrawSocketGizmos`, `CycleSocketDisplay`, selection and destruction | Scene-resource state, implementation and lifecycle now move together. The coordinator retains original update/draw ordering through narrow calls. General inspector/calibration/timeline overlay composition remains to move to the UI owner; rendering does not own gameplay, Party or calibration state. |
| `BattleCamera.*` | `BattleCamera.*` | Already focused, retained |
| `BattleAudio.CueState/CrossedCue` | `BattleAudio.*` | Already focused, retained |
| `CalibrationJson.*` | `CalibrationJson.*` | Bounded reader retained; no second codec |
| `OrinStorm.*` | `OrinStorm.*` | Per-actor contexts and scene-owned Lightning retained |
| `DragonPresence.*` | `DragonPresence.*` | Frozen continuity ordering retained |
| Dragon actor globals plus `CreateDragon`, most of `UpdateDragon`, `DrawDragon`, `ClearDragonOwnedEffects`, Dragon audio and `DestroyDragon` | `ViewerDragon.State`, `Create`, `DesiredClip`, `Update`, `Draw`, `DrawEffects`, `UpdateAudio`, `ClearOwnedEffects` and `Shutdown` | Actor lifecycle, animation timing, aim, breath, frozen continuity and audio moved together. Party target/reaction choice remains visible in the coordinator. |

This table is updated in the same commit as each later move. Deleted routines are not
copied or left as dead wrappers.

## Test migration and navigation

| Existing proof | Production owner exercised after move | Migration status |
|---|---|---|
| `HardeningTests.smile` session/playback/clock/zoom/camera/input/UI/gizmo/Party/effects/rendering assertions | `ViewerSession`, `ViewerPlayback`, `ViewerTiming`, `ViewerCamera`, `ViewerInput`, `ViewerUi`, `ViewerGizmo`, `ViewerActors`, `ViewerParty`, `ViewerEffects`, `ViewerRendering`, `BattleCamera`, shared `CharacterViewer` | New owners are included and called directly; queued modifiers, exclusive capture, UI presentation labels, exact responsive/calibration/timeline geometry, calibration pointer boundaries/selection and slider endpoints, timeline endpoint/keyframe hits, tab/transfer/panel/scene/profile/animation hit-map priority, unavailable actor/socket discovery and rotation/position bounds, all inspector presentation leaves, opt-in gizmo hide/hit-test/draw, camera cancellation, Party formation/binding/timing/preview policy/frame application/labels/pointer classification/battle-camera selection, actor facing, independent VFX pause, socket display/selection/part routing and rendering resource transitions exercise production code. Static contracts keep moved calibration discovery/bounds, UI drawing/constants/text and all UI hit geometry, gizmo implementation, Party actor command application/presentation/cameras and socket/grid resources out of `Program.smile`. |
| `CalibrationTests.smile` plus generated isolated project | `ViewerCalibration`, `ViewerParty`, `ViewerEffects`, `ViewerRendering` and retained `CalibrationJson` | Production owners are included directly; native/generated-Web tests prove scene pause leaves Fire and Lightning ages advancing by default, while explicit family toggles remain independent |
| `ActorIsolationTests.smile` | `OrinStorm`, `Character3D` and scene VFX ownership | Native/Web normal and forced-fallback runs retain two independent same-model actor contexts, sockets and lifecycles |
| PowerShell preservation fixtures | Launcher, synchronizer, canonical/publication validators | Direct production functions; disposable storage/processes only |

## R6 preservation and defect evidence

- Party, effects and rendering state plus selected calculations moved into focused
  owners without a shared replacement state monolith. The R7.5 audit corrects the
  earlier overstatement that their implementation had fully moved. Equipment-effects
  and Dragon implementation has since moved. Party presentation, pointer classification
  and participant destruction now also reside in `ViewerParty`. Party cameras,
  preview behavior and frame-command application have since moved; generic facing and
  borrowed companion-glow operations reside in their actor/effects owners. `Program.smile`
  still contains the substantial companion load/calibration switching, calibrated
  companion update and inspector-selection routines listed above. Transform-gizmo
  hit testing, retained drag math and drawing now reside in `ViewerGizmo`; only
  cross-owner calibration edit commands remain in the coordinator.
- Native and generated-Web hardening pass with exact console parity. The isolated
  calibration fixture uses a random application identity and validates primary/backup
  recovery without reading or replacing live Arin/Orin storage.
- The two-Orin actor-isolation fixture passes normal and forced-fallback native/Web
  runs. Installed Chrome context loss/restoration advances the live runtime and the
  supported character-selection recovery action restores rendering without reload.
- Installed Chrome visibly retains both `Freeze Fire` and `Freeze Lightning` while the
  scene is paused. Each control independently changes to `Play` when explicitly frozen;
  both were restored to their default VFX-running state while choreography remained
  paused.
- R5 had converted the one-time native `Window_Activate()` operation into a per-frame
  focus probe. Because that operation actively foregrounds the window, Desktop stole
  focus from Chrome. R6 removes the probe and dead activation bookkeeping, and the
  hardening gate rejects its return.

Three short maintenance routes define the desired end state:

- Camera adjustment: `ViewerCamera` state and integer-control tests, then the thin
  coordinator call; no calibration, Party, or renderer-storage tour.
- Calibration save failure: `ViewerCalibration` transaction, bounded JSON codec and
  isolated storage fixture; UI consumes only the resulting status/Undo state.
- Party timing: `ViewerParty` stage transition and actor snapshots, then
  `ViewerEffects` once-per-scene advancement; rendering only consumes snapshots.
