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
The calibration key-transaction checkpoint then moved current-frame delete, key move,
clipboard paste, saved-key reload and clip/all clear operations together with persistence
and rollback into `ViewerCalibration`. An explicit result separates an applied mutation
from successful persistence so coordinator refresh behavior remains unchanged on failure.
The isolated fixture now calls production query/import/storage operations directly;
eleven coordinator pass-through procedures were deleted. UI confirmation, edit gesture
sequencing and cross-owner transform/VFX application remain visible in `Program.smile`.
The playback/timeline checkpoint then moved clip selection and profile/runtime mapping,
selected clip labels, clip-event queries, frame/time seeking, play-state reset, automatic
demo advancement and demo countdown into `ViewerPlayback`. Adjacent calibration-key
selection moved beside the stored key tracks in `ViewerCalibration`. Six playback query
wrappers and the unreachable authored-event seek routine were deleted. The coordinator
still shows Party preview/cancel, calibration application, effect invalidation and
timeline pointer/drag ordering without retaining the moved algorithms.
The calibrated Party-participant checkpoint then removed per-frame whole-Viewer context
swaps from companion and Dragon-inspection updates. `ViewerParty` now updates its owned
participant context directly, temporarily selects and restores only the borrowed
calibration bank, applies presentation/calibration/glow behavior through focused owners,
and reports readiness/failure stage explicitly. Dragon-inspection participant placement
and drawing moved with Party state. An `Updated` result flag preserves the difference
between a not-ready no-op and an attempted update when adapting legacy stage 71.
The calibration edit-session checkpoint then moved current-frame evaluation, current
target queries, edit begin/finish/cancel, bounded value mutation and reset, grip-anchor
capture/correction, and wrist/equipment application ordering into
`ViewerCalibrationEditing`. That coordinator borrows only the explicit calibration,
actor, UI, gizmo, camera and effect owners for the duration of an operation. The startup
coordinator retains Party-preview entry, session-failure adaptation and the user-command
ordering around persistent key transactions; it no longer implements edit or grip math.
The Party companion-creation checkpoint then moved participant load, calibration
configuration, shared-or-borrowed glow attachment, Idle playback and readiness ownership
into `ViewerParty.CreateCompanion`. It restores the borrowed calibration bank and primary
effect bindings before returning. `Program.smile` now expresses only whether the Party or
Dragon-inspection sequence creates Arin or Orin, and no longer replaces the whole primary
Viewer context to construct a participant.
The gizmo drag-geometry checkpoint then moved calibration-target socket/origin resolution
to `ViewerCalibrationEditing.GizmoOrigin`, and pointer-axis/ring projection plus retained
fractional value conversion to `ViewerGizmo.DragValueAmount`. The same gizmo-owned
remainder now handles slow move and rotation drags. `Program.smile` reads the pointer in
the established frame order and applies only the returned integer calibration delta.
The inspector-key policy checkpoint then moved editing and Party restrictions, queued
Ctrl arrow meaning, socket-selection priority, clip-count gating and general key-to-action
mapping into `ViewerInput.ClassifyInspectorKey`. `Program.smile` retains the readable
cross-owner action dispatch and the earlier backtick/gizmo priority; it no longer parses
the same inspector keys itself.
The Party demo-lifecycle checkpoint then moved choreography reset, formation construction,
initial Dragon-turn choice, equipment visibility restoration, attack-duration/preparation
policy and per-frame actor command application into `ViewerParty.ResetDemo` and
`ViewerParty.AdvanceDemo`. The main loop retains the visible Party-before-primary-update
call order, while the coordinator wrappers provide only explicit scene constants and
session-readiness adaptation.
The profile/arena-policy checkpoint then moved loaded actor/profile validation and
arena-facing selection into `ViewerActors`, character-versus-arena orbit and zoom defaults
into `ViewerCamera`, and Dragon-aware floor extents into `ViewerRendering`. Startup and
reset retain their visible ordering and pass explicit scene constants; the seven former
policy implementations no longer remain in `Program.smile`.
The Party/Dragon integration checkpoint then moved reaction timing and consumption,
inspection/Party/primary target selection, and the complete call into the Dragon owner
to `ViewerParty.UpdateDragon`. It borrows only the Party, Dragon, effects and primary-actor
owners plus explicit playback scalars. `Program.smile` retains a thin readiness adapter
at the established point in the frame order; it no longer inspects Party reaction or
target state to update the Dragon.
The Party inspector-selection checkpoint then moved companion/Dragon context selection,
Dragon clip resolution, edited companion capture and inspected-Dragon capture into
`ViewerParty.BeginInspectorSelection` and `EndInspectorSelection`. The coordinator now
applies the returned context and reconfigures the borrowed calibration owner only when
its profile changes; it no longer implements Party-owned participant transitions.
The rendering-mode checkpoint then moved lighting and material-inspection state plus
their reset/apply/cycle behavior into `ViewerRendering.State`. The load, input and reset
coordinators retain their visible ordering and adapt the returned renderer result to
session readiness; no rendering-mode state remains global in `Program.smile`.
The shared-slider checkpoint then moved camera/calibration slider geometry, hit testing,
value mapping and pointer-capture mutation into `ViewerUi.UpdateCameraSliders` and
`UpdateCalibrationSlider`. `Program.smile` no longer imports the low-level UI-controls
module; it dispatches the returned typed slider action to camera or calibration owners.
The inspector/gesture workflow checkpoint then moved frame-button repeat cancellation
and scheduling, timeline scrub release and keyframe-drag update/finish transitions into
`ViewerInput`. Pointer-to-time seeking, adjacent-key navigation, duplicate-key rejection,
drag playback, key persistence and post-move pose refresh moved into the focused
`ViewerTimelineEditing` owner. The coordinator retains Party-preview entry, runtime
pointer sampling, session-readiness adaptation and typed action dispatch.
The calibration-panel command checkpoint then moved target/axis/transform selection,
saved-key navigation, clear confirmation, key commands, Hold Grip and decouple routing
into `ViewerCalibrationControls`. The focused owner borrows only calibration, actor,
playback, UI, gizmo, camera and effect state for each action; it does not know Party,
session, rendering, Dragon or whole-application state. `Program.smile` retains pointer
sampling, Party-preview entry for the actions that require it and one explicit
operation-result adapter. Ten command-specific wrappers were deleted rather than kept
as dead aliases, and the isolated calibration fixture now invokes the production owner.
The inspector presentation-command checkpoint then moved the rendering/VFX action map
and its owner calls from the broad pointer dispatcher into `ViewerInspectorCommands`.
The focused operation borrows only `ViewerRendering.State` and `ViewerEffects.State`;
lighting/material readiness is returned explicitly, while background, floor/grid,
socket and independent VFX controls stay with their established subsystem state. It
does not know Party, playback, session, calibration, camera, Dragon or application
state. The coordinator retains one typed call plus session-readiness adaptation.
The keyboard navigation checkpoint then moved renderer-owned floor/background/socket/
grid commands and camera-owned orbit/pan/auto-orbit commands through a second bounded
`ViewerInspectorCommands` operation. `ViewerRendering.ToggleGrid` now changes grid
state beside the other rendering controls. The responsive-fit toggle, invalidated-size
sentinels and immediate recomposition moved with `ViewerCamera.State` into
`ToggleResponsiveFit`. Playback, retry, profile, Party and equipment commands remain
outside this operation; the coordinator adapts only the explicit ready result.
The inspector-overlay composition checkpoint then moved the ordered grouping of the
identity/toolbar, status/animation, effects/camera and timeline/calibration/status
presentation leaves into `ViewerInspectorPresentation`. That owner receives explicit
presentation values and borrows only the actor, calibration, input and UI state already
required by the UI-owned timeline and calibration renderers; it does not retain or know
session, Party, playback, effect, rendering, camera, Dragon or application state.
`DrawInspectorOverlay` remains a readable coordinator that gathers owner queries and
keeps Party binding, gizmo and Party overlay order visible. Dragon target-label policy
and Party remaining-time calculation moved with Party state to `ViewerParty` and are
covered by direct owner assertions.
The bounded stage must not
introduce a whole-application state record or a replacement `ViewerApplication`
module.

| Still implemented in `Program.smile` | Current evidence | Intended focused owner |
|---|---|---|
| Party transitions, companion lifecycle and inspector binding | `ViewerParty.CreateCompanion`, `ResetDemo`, `AdvanceDemo`, lower-level choreography/frame operations, `UpdateDragon`, `BeginInspectorSelection`, `EndInspectorSelection`, `ApplyAttackCamera`, participant update/placement/drawing, preview operations, pointer classification, overlay drawing and destruction own their implementations. `ViewerActors` owns actor loading/facing and `ViewerEffects` owns shared and borrowed glow resources. `Program.smile` retains cross-owner calibration reconfiguration, narrow session-readiness adaptation and pointer action dispatch. | `ViewerParty.smile`, `ViewerActors.smile`, `ViewerDragon.smile` and `ViewerEffects.smile`, with calibration-owner adaptation remaining in the coordinator |
| Inspector, calibration panel, timeline, camera controls, buttons and overlays | `ViewerCalibration` owns stored calibration evaluation, wrist-socket discovery/availability, edit bounds, key navigation/mutation/persistence transactions, wrist/equipment transform application, target/channel mapping/reset and inverse grip preservation. `ViewerCalibrationEditing` owns current evaluation/value access plus edit begin/finish/cancel, value/reset, grip correction and transform-application sequencing. `ViewerCalibrationControls` owns calibration-panel selection, navigation, clear confirmation and command routing. `ViewerInspectorCommands` owns bounded rendering/VFX and keyboard navigation action maps. `ViewerPlayback` owns clip mapping/labels/events, playback-state mutation, frame/time seeking and demo advancement. `ViewerInput` owns inspector key policy/capture fields and detailed active-gesture transitions; `ViewerTimelineEditing` owns timeline seek/drag/duplicate-key/persistence workflow; `ViewerUi` owns live label/layout policy, shared slider control execution, focused panel drawing, calibration/timeline/general inspector hit classification and raw presentation leaves. `ViewerInspectorPresentation` owns their four ordered presentation groups without retaining application state. Remaining playback/profile/Party command dispatch and Party-preview/session adaptation remain in `Program.smile`; `DrawInspectorOverlay` gathers explicit owner values and preserves cross-owner Party/gizmo ordering. | Focused calibration/playback/input/UI owners plus `ViewerInspectorCommands.smile` for bounded commands and `ViewerInspectorPresentation.smile` for explicit composition |
| Transform-gizmo command coordination | `ViewerGizmo` owns projection, hit testing, axis/ring pointer projection, retained fractional value conversion, hover state and complete axis/ring drawing. `ViewerCalibrationEditing` owns target-to-socket origin selection plus calibration edit lifecycle, bounded mutations and grip preservation. `Program.smile` retains keyboard/pointer action dispatch and timeline/capture ordering across the owners. | `ViewerInput.smile`, `ViewerGizmo.smile` and `ViewerCalibrationEditing.smile`, with cross-owner dispatch remaining in the coordinator |
| General rendering and overlay composition | Socket resources, studio-grid wrappers, lighting/material state and their apply/cycle behavior reside in `ViewerRendering`; labels, responsive geometry, status/camera/animation controls, calibration panel, timeline, inspector chrome, footer/status and recovery drawing reside in `ViewerUi`; four ordered groups reside in `ViewerInspectorPresentation`. `DrawViewerOverlay` and `DrawInspectorOverlay` retain only cross-owner scene/Party/gizmo ordering and explicit presentation-value assembly in `Program.smile`. | `ViewerRendering.smile` for scene resources and renderer modes; `ViewerUi.smile` for presentation leaves; `ViewerInspectorPresentation.smile` for bounded composition |
| Load/retry/switch/destroy orchestration mixed with resource implementation | `ViewerActors.ValidateProfile` owns loaded actor/profile validation, `ViewerActors.ApplyArenaFacing` owns Dragon/character facing policy, `ViewerCamera` owns orbit/zoom default choice and `ViewerRendering` owns arena extent policy. `LoadViewer`, `RetryViewer`, `SelectCharacterTab`, `DestroyViewerResources` and resource-stage sequencing remain in `Program.smile`. | thin `Program.smile` coordinator plus focused session/actor/render/effect owners |

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
| Calibration key-transaction move | 4,564 | 116 | From the 4,646-line/127-procedure discovery checkpoint, move/delete/paste/reload/clip-clear/all-clear mutation plus persistence/rollback moved into `ViewerCalibration`. Explicit `Applied`/`Persisted` results preserve refresh and failure behavior. The isolated fixture migrated to direct production query/import/storage operations, allowing eleven coordinator wrappers and one unused channel-count alias to be deleted. Integrated transaction, failed-write, import-confirmation and Undo tests plus static wrapper guards cover the move. |
| Playback/timeline behavior move | 4,296 | 109 | From the 4,564-line/116-procedure key-transaction checkpoint, clip selection/mapping/labels/events, frame/time seeking, play-state reset, demo advancement and demo countdown moved with `ViewerPlayback.State` into `ViewerPlayback`; adjacent saved-key navigation moved with calibration tracks into `ViewerCalibration`. Six query wrappers and the unreachable `SeekAuthoredEvent` routine were deleted. Direct state/real-actor tests and static owner/absence guards cover the move while Party preview, calibration application, VFX invalidation and timeline gesture ordering remain visible in the coordinator. |
| Calibrated Party-participant lifecycle move | 4,231 | 108 | From the 4,296-line/109-procedure playback checkpoint, calibrated companion and Dragon-inspection participant updates moved with `ViewerParty.State`; placement and Dragon-opponent drawing moved there too. Per-frame whole-Viewer context swaps were eliminated in favor of direct participant fields and a temporarily borrowed/restored calibration bank. An explicit `Updated` flag preserves no-op versus attempted-update failure capture. Direct no-op/real-actor tests, bank-restoration proof and static owner guards cover the move. |
| Calibration edit-session move | 4,084 | 107 | From the 4,231-line/108-procedure Party-participant checkpoint, current-frame evaluation, current target access, edit begin/finish/cancel, bounded set/reset, grip-anchor preservation and wrist/equipment application ordering moved to `ViewerCalibrationEditing`. `Program.smile` lost 147 lines without whitespace compression and retains only Party-preview entry, session-result adaptation and command dispatch around this owner. Direct owner smoke checks, real-actor calibration integration and static guards cover the move. |
| Party companion-creation lifecycle move | 4,046 | 106 | From the 4,084-line/107-procedure calibration-edit checkpoint, actor loading, calibration setup, shared/borrowed glow attachment, initial playback and participant readiness moved with `ViewerParty.State`. The 82-line owner operation replaces a 65-line whole-context-swap implementation; explicit startup calls add 27 readable coordinator lines, for a net 38-line reduction without compression. Isolated Party/Dragon load tests prove calibrated identities and primary-bank restoration; static guards reject the deleted wrapper and actor-load/context-swap path. |
| Gizmo origin/drag-geometry move | 3,994 | 105 | From the 4,046-line/106-procedure Party-creation checkpoint, target/socket origin selection moved to `ViewerCalibrationEditing`, while move-axis/ring projection, divisors and retained slow-drag accumulation moved with `ViewerGizmo.State`. The coordinator lost 52 lines and one helper while preserving pointer sampling and calibration-application order. Direct remainder assertions, native integration and static guards cover both owners. |
| Inspector keyboard-policy move | 3,966 | 105 | From the 3,994-line/105-procedure gizmo-geometry checkpoint, editing/Party restrictions, queued Ctrl/plain-arrow semantics, socket priority, clip gating and key-to-action mapping moved to `ViewerInput`. `HandleInspectorKeyboard` remains an explicit cross-subsystem dispatcher, so no false procedure reduction is claimed. Six direct policy assertions and static key-parsing guards cover the move. |
| Party demo reset/advance lifecycle move | 3,911 | 103 | From the 3,966-line/105-procedure input-policy checkpoint, formation/reset/initial-turn/equipment restoration and attack-duration/preparation/choreography/frame application moved with `ViewerParty.State`. The coordinator lost 55 lines and two implementation helpers while preserving the main-loop call position. Direct reset/formation assertions, integrated Party preview advancement and static guards cover the owner. |
| Profile/arena policy move | 3,843 | 96 | From the 3,911-line/103-procedure Party demo checkpoint, loaded profile validation, Orin arena-yaw selection and actor-facing behavior moved to `ViewerActors`; arena-versus-character orbit/zoom defaults moved to `ViewerCamera`; Dragon-aware floor extent policy moved to `ViewerRendering`. Seven substantial policy procedures left the coordinator without whitespace compression. Direct pure policy checks, invalid-actor checks, real-actor load integration and static absence guards cover the move. |
| Party/Dragon target and reaction move | 3,771 | 96 | From the 3,843-line/96-procedure profile/arena checkpoint, Party reaction timing/consumption, inspection/Party/primary chest-target selection and the Dragon presentation update moved with Party state into `ViewerParty.UpdateDragon`. The retained `UpdateDragon` coordinator procedure is a thin explicit readiness adapter used at the visible frame-order boundary and by the real-actor integration fixture; no false procedure reduction is claimed. Direct reaction-boundary/consumption checks, real-actor frozen-VFX/seek/cue coverage and static owner guards cover the move. |
| Party inspector selection/capture move | 3,741 | 96 | From the 3,771-line/96-procedure Party/Dragon checkpoint, companion-versus-Dragon context selection, Dragon clip lookup, edited companion capture and inspected-Dragon capture moved with Party state. `BeginPartyInspector` and `EndPartyInspector` remain thin cross-owner adapters that apply returned contexts and conditionally reconfigure calibration, so no false procedure reduction is claimed. Direct context-selection/capture checks, real Party/Dragon inspector isolation and static guards cover the move. |
| Rendering mode state/behavior move | 3,692 | 93 | From the 3,741-line/96-procedure Party inspector checkpoint, lighting index, material-inspection mode, reset policy and all native renderer calls moved together into `ViewerRendering.State`. Three implementation procedures and two globals left `Program.smile`; load preserves the previous rule that material inspection resets while lighting choice survives reload, and Reset All restores both defaults. Direct state/apply/cycle checks, native integration and static owner guards cover the move. |
| Shared slider control move | 3,677 | 93 | From the 3,692-line/93-procedure rendering-mode checkpoint, all low-level slider calls, geometry, direction/value mapping and drag-owner mutation moved into `ViewerUi`. The retained `HandleSharedSliders` procedure dispatches typed results to camera/calibration owners and evaluates calibration values only when that panel or its capture is active. `Program.smile` no longer imports `Smile.UI.Controls`; no false procedure reduction is claimed. Direct inactive-control checks, existing capture integration and static ownership guards cover the move. |
| Primary actor framing/presentation lifecycle move | 3,651 | 93 | From the 3,677-line/93-procedure slider checkpoint, the PBR-required primary load policy and scale/place/facing/shadow sequence moved into `ViewerActors`; local bounds, default-camera creation, auto-fit, Dragon inspection framing and arena-base composition moved with persistent camera state into `ViewerCamera`. The coordinator retains the established failure-stage boundaries around load, profile validation, framing and presentation. `LocalCharacterBounds` left `Program.smile`; no false procedure reduction is claimed. Invalid-actor owner checks, the real Arin/Orin/Dragon/Party load fixture and static raw-call guards cover the move. |
| Equipment visibility state/propagation move | 3,624 | 93 | From the 3,651-line/93-procedure primary-lifecycle checkpoint, weapon/shield visibility intent moved into the bounded `ViewerActors.EquipmentVisibility` record with reset/toggle/application behavior; single/Party/Dragon participant propagation moved into `ViewerParty`. `Program.smile` retains inspector-binding coordination and tells `ViewerEffects` to clear hidden attachments, but no longer owns the two visibility fields or calls the renderer-facing part API. No false procedure reduction is claimed. Direct state/no-op/invalid-actor checks, real Party and Dragon hide/restore integration, frozen-effect continuity and static ownership guards cover the move. |
| Calibration command lifecycle move | 3,561 | 93 | From the 3,624-line/93-procedure equipment checkpoint, confirmed import, key delete/copy/reload/paste, clip/all clear, Save and Undo mutation now coordinate persistence, pose refresh, edit completion and VFX invalidation inside `ViewerCalibrationEditing`. `Program.smile` keeps Party-preview entry, scene pause/demo policy and typed session-failure adaptation; its named command procedures remain as live UI/test adapters rather than duplicate implementations. No false procedure reduction is claimed. Direct no-op owner coverage, disposable primary/backup/import/Undo/failure integration and static transaction-call guards cover the move. |
| Presentation audio/storm routing move | 3,517 | 93 | From the 3,561-line/93-procedure calibration-command checkpoint, Arin attack cue timing, cue-state reset and sound submission moved with `Effects.ArinAudio` into `ViewerEffects`; companion-versus-primary Orin selection, Dragon chest targeting, KO/glow eligibility and storm dispatch moved with Party state into `ViewerParty`. `UpdateBattleAudio` and `UpdateOrinStorm` remain thin visible frame-order adapters, so no false procedure reduction is claimed. Direct reset/invalid-Dragon checks, existing continuous/skipped cue and Party/Dragon storm integration, plus static raw-call guards cover the move. |
| Transform-gizmo interaction move | 3,448 | 91 | From the 3,517-line/93-procedure presentation-routing checkpoint, gizmo keyboard classification moved to `ViewerInput`; pointer gating, toggle/cancel/update/finish/begin-axis decisions moved with retained drag conversion to `ViewerGizmo`; axis changes, edit opening, drag cancellation and calibrated value mutation moved to `ViewerCalibrationEditing`. The two implementation helpers `UpdateTransformGizmoFromPointer` and `AdjustCalibrationValue` left `Program.smile`; the retained coordinator samples runtime input, protects socket-origin evaluation and dispatches typed actions in the established order. Direct keyboard/pointer/no-op operation assertions, native/generated-Web hardening, Full Web compilation and static raw-operation guards cover the move. |
| Inspector and timeline gesture-workflow move | 3,385 | 91 | From the 3,448-line/91-procedure transform-gizmo checkpoint, frame-repeat timing/cancellation and timeline scrub/key-drag transitions moved with capture state into `ViewerInput`; pointer seeking, adjacent-key navigation, availability gating, duplicate-key rejection, drag playback, persistence and pose refresh moved into new focused `ViewerTimelineEditing`. `Program.smile` lost 63 lines without whitespace compression and retains concise Party-preview/readiness adapters. Direct repeat/scrub/drag/duplicate/no-op assertions, native/generated-Web hardening and static implementation guards cover the move. |
| Calibration-panel command-workflow move | 3,183 | 81 | From the 3,385-line/91-procedure inspector/gesture checkpoint, calibration selection, navigation, clear confirmation, saved-key commands, Hold Grip and decouple routing moved into new focused `ViewerCalibrationControls`. Ten command-specific wrappers were deleted and `Program.smile` lost 202 lines without whitespace compression. The retained 36-line panel adapter samples the pointer, enters Party preview when required and adapts the owner result to session readiness. Direct command-owner assertions, isolated real-actor calibration integration and static wrapper/implementation guards cover the move. |
| Inspector rendering/VFX command move | 3,146 | 81 | From the 3,183-line/81-procedure calibration-panel checkpoint, twelve rendering/VFX inspector actions and their state-owning calls moved into focused `ViewerInspectorCommands`. The broad dispatcher shrank from 343 to 305 lines; the retained typed call adapts renderer readiness to the session. Direct native/generated-Web command assertions and static routine-slice guards cover the move without creating shared application state. |
| Inspector keyboard-navigation/camera-fit move | 3,101 | 81 | From the 3,146-line/81-procedure presentation-command checkpoint, eleven rendering/camera keyboard actions moved through the bounded inspector command owner and responsive-fit transition/recomposition moved with camera state. The keyboard dispatcher shrank from 142 to 106 lines and the pointer dispatcher from 305 to 296 lines without compression. Direct command/no-op/sensitivity and fit-toggle assertions plus static routine-slice guards cover the move. |
| Inspector overlay-composition/Party-query move | 3,040 | 81 | From the 3,101-line/81-procedure keyboard/camera checkpoint, four ordered presentation groups moved into the new 224-line/4-procedure `ViewerInspectorPresentation` owner. `DrawInspectorOverlay` shrank from 217 to 171 lines. Dragon target-label policy and Party remaining-time calculation moved into `ViewerParty`, reducing `AnimationSecondsRemaining` from 34 to 18 lines. Direct native/generated-Web owner assertions and static routine-slice guards cover the move; no false procedure reduction is claimed. |

The separate fixed-array hardening gate did not move Viewer responsibility or change
the 3,448-line/91-procedure transform-gizmo checkpoint metrics. It added immediate
per-dimension native bounds checks, Web `ByRef` capture validation, returned-record
projection cleanup and bounded native record-helper loops, then exercised those
compiler/runtime changes with disposable native and Web fixtures. R7.5 subsequently
resumed with the inspector/gesture and calibration-panel command moves above.

Substantial implementation still in `Program.smile` after the inspector-overlay
checkpoint is intentionally explicit: startup/retry/tab-switch orchestration; Party
inspector calibration adaptation and Party pointer action dispatch; the broad inspector/
keyboard command dispatchers plus slider/gizmo priority ordering; reset orchestration;
and cross-owner scene, Party and gizmo ordering. Inspector presentation-leaf grouping
and Party-owned label/countdown policy no longer remain there. The retained calibration
adapters enter/restore Party preview, apply scene pause/demo policy, execute frame-order
transform application or adapt an explicit operation result to first-failure session
state. Calibration panel selection, navigation, clear confirmation and persistent
command dispatch no longer remain there; their implementation is in
`ViewerCalibrationControls` and the ten one-command coordinator wrappers are deleted.
The thin `UpdateDragon` adapter remains to combine
the current selected clip/playback scalars with Party, Dragon, effects and session
readiness; target/reaction selection is in `ViewerParty`, while actor update, timing,
aim, breath and audio behavior remain in `ViewerDragon`.
`UpdateBattleAudio` remains to derive current editor audibility and call the separate
Arin-effects and Dragon-audio owners. `UpdateOrinStorm` remains only as the visible
frame-order call adapter; actor/target routing is in `ViewerParty` and storm simulation
is in `ViewerEffects.UpdateStorm`.

`Program.smile` remains a documented temporary exception to the 500-line entry-point
ceiling at 3,040 lines/81 procedures. Its next concrete reductions are the 296-line
inspector command dispatcher, 102-line Party pointer dispatcher and 237-line load
orchestration. The retained 171-line `DrawInspectorOverlay` gathers explicit values and
preserves Party/gizmo ordering; its four substantial presentation groups now live in
`ViewerInspectorPresentation`, which is 224 lines/4 procedures.
`ViewerInspectorCommands` is 100 lines/2 procedures,
`ViewerCalibrationControls` is 300 lines/7 procedures,
`ViewerInput` is 389 lines/13 procedures and `ViewerTimelineEditing` is 183 lines/5
procedures, all below the applicable 500-line review threshold; no size exception or
whole-application state was introduced for this checkpoint.

## Ownership target map

| Responsibility | Baseline owner | Intended refactor owner | Owned mutable state | Public operations and borrowed dependencies | Lifetime / focused proof |
|---|---|---|---|---|---|
| Startup and failure/session lifecycle | `Program.smile` | `ViewerSession.smile` plus thin `Program.smile` coordinator | Running/readiness, first error/stage, resource epoch, tab/profile and scene mode | Reset/record/capture failure; coordinator borrows renderer and subsystem owners for load/retry/switch/shutdown | One application session; direct module assertions plus native launch, reload and failure fixtures |
| Frame clocks and playback sequence | `Program.smile` plus `CharacterViewer.ClockState` | `ViewerTiming.smile` and `ViewerPlayback.smile` | Frame-rate and clamped clocks; selection, speed, pause/demo sequence | Start/advance/reset/query, clip mapping/labels/events, frame/time seek, clip mode and demo target/countdown; borrows current actor/profile only per call | Session; direct native state assertions plus real-actor playback fixture |
| Actor/inspector binding | `Program.smile` (`ViewerActorContext`, `PartyUi*`, `CalibrationOwnerProfile`) | `ViewerActors.smile` owns `Context`, generic actor load/update/draw/destroy, loaded profile validation, arena-facing policy and shared equipment-visibility intent; `ViewerParty.smile` owns companion creation, inspector target selection/capture, participant visibility propagation and preview state/mode/restore | Primary/inspected actor identity, equipment visibility, Party participants and temporary preview binding | Participant creation combines actor, calibration and glow owners through explicit borrowed state; startup/reset pass explicit Dragon/profile constants to actor-facing policy; Party applies weapon/shield intent to explicitly borrowed actors; the coordinator applies Party-returned contexts and reconfigures calibration only when the borrowed owner changes | Scene/preview; direct context/selection/capture/preview/policy/visibility assertions plus integrated Party/Dragon load, hide/restore and isolation fixtures |
| Camera and transforms | `Program.smile`, `BattleCamera.smile`, shared `Interaction` | `ViewerCamera.smile`, bounded key routing in `ViewerInspectorCommands.smile` and retained `BattleCamera.smile` math | `ViewerCamera.State`: base/live camera, frame, persistent local actor bounds, controls, zoom target, fractional pointer remainder, orbit anchor and auto-orbit | Configure primary framing, reset/compose/nudge/drag/advance/apply/query, responsive-fit toggle/recomposition and character-versus-arena orbit/zoom default selection; borrows the current actor and framing profile during configuration without retaining either | Scene; direct integer-output/default-policy/key-routing/fit-toggle assertions plus real actor loads and native/installed-Chrome controls |
| Calibration editing | `Program.smile`, `CalibrationJson.smile` | `ViewerCalibration.smile`, `ViewerCalibrationEditing.smile`, `ViewerCalibrationControls.smile` and retained bounded JSON reader | `ViewerCalibration` owns per-profile key banks and edit/clipboard/Undo/import workspace; UI, gizmo, camera and effects retain their own interaction state | Stored operations configure/load/evaluate/apply/map/reset/preserve/save/Undo/import/export/query; the edit-session owner coordinates current evaluation/value, begin/finish/cancel, bounded set/reset, grip correction, confirmed import, key/clip/all transactions, Save/Undo, pose refresh and transform application; the controls owner dispatches panel actions through those operations using explicit borrowed states | Profile/edit workspace; direct owner checks, native/generated-Web isolation, real actor transform/grip and command integration, failed writes and malformed imports |
| File transactions and launcher synchronization | Viewer Save/Load statements, synchronizer and launcher | `ViewerCalibration.smile`, `sync-arin-v5-7-calibration.ps1`, `Launch.ps1` | Checked save baseline/pending revision; primary/backup selection | Transaction commit/recovery/watch; canonical JSON is borrowed source of truth | Save/application identity; preservation fixtures |
| Input ownership | `Program.smile` | `ViewerInput.smile`, focused `ViewerTimelineEditing.smile`, `ViewerInspectorCommands.smile`, plus `ViewerUi.smile` for concrete controls | Pointer capture, timeline/frame repeat and inspector/gizmo key classification remain input state; Viewer UI borrows the slider owner while executing a control; renderer/effect/camera state stays with those owners | Classify queued keyboard and gizmo-key policy; own repeat/scrub/key-drag transitions; execute timeline seek/drag/duplicate/persistence workflow through explicit playback/calibration/effects borrows; route bounded rendering/VFX pointer commands and rendering/camera keyboard commands through explicit owners; UI returns typed slider actions; the coordinator dispatches remaining cross-owner actions | Frame/capture; direct editing/Party/queued-modifier/socket/clip/gizmo/inactive-slider/repeat/scrub/duplicate/rendering/VFX/camera policy, outside-window fixtures and native/generated-Web integration |
| UI and transform gizmo | `Program.smile` | `ViewerUi.smile`, `ViewerGizmo.smile` and `ViewerCalibrationEditing.smile` | Panel visibility remains UI state and slider capture remains input state; opt-in projection, hover, drag and retained fractional motion remain gizmo state | UI label/geometry, shared slider execution, calibration hit/selection policy and focused panels have moved; gizmo target origin, pointer projection, gating/action classification, retained value conversion, hit testing and drawing are production-owner operations. Calibration editing owns axis/drag/value mutation. The coordinator samples runtime pointer state and dispatches typed actions | Scene/edit; direct label/geometry/panel/hit/inactive-slider assertions plus gizmo key/pointer decisions, remainder/hit-test/draw, pointer exclusivity and cancel integration |
| Party choreography | `Program.smile` | `ViewerParty.smile` | Participants, turn/stage/timing, guard/hit/KO/revive, Dragon reaction/target choice, preview state, stable shot anchors and Party cameras | Reset/advance/apply actor and Dragon commands/camera/draw/destroy/bind and restore preview; borrows explicit actor, Dragon, playback and effect snapshots | Party scene; timing, reaction boundaries/consumption, preview mode/restore, frame application, Dragon target/update integration, camera selection/continuity, same-model isolation and inspector fixtures |
| Effects/audio | `Program.smile`, `OrinStorm.smile`, `DragonPresence.smile`, `BattleAudio.smile` | `ViewerEffects.smile` plus retained focused modules; `ViewerParty.smile` selects the Orin presentation actor/target; `ViewerInspectorCommands.smile` maps inspector actions | Equipment emitters, trails, leases, scene clocks, Arin cue state and visual-continuity epochs; the Dragon owner retains its own cue state and borrows its scene light lease | Create/update/advance-once/draw/invalidate/destroy, Arin cue update/reset and Party Orin routing; inspector commands borrow effect state without copying it; borrows final actor transforms | Scene; direct control/state/inspector assertions plus frozen-cut/skipped-cue/storm/lease cleanup tests |
| Dragon actor/presentation | `Program.smile`, `DragonPresence.smile`, `BattleAudio.smile` | `ViewerDragon.smile` with retained focused presence/audio modules; `ViewerParty.smile` owns Party target/reaction integration | Dragon actor, ownership flag, clip, head aim, breath, continuity epoch, visibility and cue state; Party owns its reaction/target state | Create/update/draw/toggle/destroy; borrows the Party-selected target, shared Fire readiness/light lease and visual epoch | Scene; pure clip/travel/reaction assertions plus native frozen seek/cut/hide/resume and cue tests |
| Rendering and overlay composition | `Program.smile` | `ViewerRendering.smile` for scene resources, lighting/material modes and Dragon-aware floor extent policy; `ViewerInspectorCommands.smile` for bounded action mapping; `ViewerUi.smile` for editor leaves; `ViewerInspectorPresentation.smile` for bounded composition | `ViewerRendering.State` owns arena/backdrop/grid/socket render resources including the fixed socket-object array plus lighting/material mode; UI retains transient layout state; the presentation owner retains no state | Reset/apply/cycle renderer modes, create/update/draw/destroy scene resources and resolve explicit character/arena extents; inspector commands borrow rendering state and return readiness; presentation composition receives explicit scalar values and only the focused UI-required state borrows | Scene; direct mode/extent/socket selection/part-routing/inspector/presentation assertions plus native/generated-Web draw and resize checks |
| Build/publication | `Build.ps1`, `Prepare-BuildAssets.ps1`, explicit projects | same scripts with explicit module inventory | Disposable staging/publications only | Canonical preflight, compile, selected-output validation | Build; Release/Debug and Full/Low/Medium/High manifests |

Immutable `Character3D` cache entries are shared resources. Actor pose, equipment
visibility, calibration, inspector selection, effects, and scene clocks are never cache
state. Arin, Orin, Dragon, Party, and inspected-actor identities remain distinct.

## Symbol migration target map

| Old symbol/location | Intended owner/symbol | Preservation note |
|---|---|---|
| `AdvanceFrameRate`, `FrameRateElapsed`, `FrameRateFrames`, `CurrentFramesPerSecond` | `ViewerTiming.Advance`, `ViewerTiming.FrameRateState`, `ViewerTiming.FramesPerSecond` | First low-risk extraction; identical 500 ms integer sampling, directly tested |
| `PreviousTime`, `ViewerClock`, copied elapsed/drop counters | `ViewerTiming.ClockState`, `ViewerTiming.Start`, `ViewerTiming.AdvanceClock` | Identical raw sample, clamp and long-pause contract; coordinator consumes explicit animation/camera/presentation outputs |
| `ViewerParty.ActorContext`, companion actor load/update/draw/destroy mapping and `FaceDragon` in `Program.smile` | `ViewerActors.Context`, `Capture`, `Apply`, `LoadContext`, `Update`, `Draw`, `FaceToward` and `Destroy`; `ViewerParty.BeginInspectorSelection`/`EndInspectorSelection` own Party target transitions | Actor handles, per-actor inspection fields and generic facing have a focused owner. Party borrows/captures its participant contexts; calibration and effects remain separate owners. The coordinator only adapts returned contexts to the currently borrowed calibration owner. |
| `SelectedClip`, speed, pause/demo counters, clip mapping/labels/events, frame/time seek and play/demo sequence calculations | `ViewerPlayback.State`, `SelectClip`, `StepFrame`, `SeekFirstFrame`, `SeekTime`, `StartSelectedClip`, `AdvanceDemo`, mapping/query operations and `Demo*`; `ViewerTimelineEditing` coordinates timeline edit workflow | Playback state and behavior move together; actor/profile handles are borrowed per call and never retained. Timeline gesture state remains in `ViewerInput`; timeline seek/drag/persistence borrows the playback/calibration/effect owners; Party preview and readiness adaptation remain explicit coordinator work. |
| `Ready`, `ViewerError`, first error/stage, tab/profile and resource epoch | `ViewerSession.State`, `ResetFailure`, `RecordError`, `CaptureFailure` | First-failure retention and explicit retry reset preserved; stage capture remains adjacent to the coordinator stage |
| `BaseCamera`, `Camera`, `ViewerFrame`, `CameraControls`, `SmoothZoom`, pointer remainders, calibration orbit anchor and `AutoOrbit*` | `ViewerCamera.State` with `Reset`, `Compose`, `UpdatePointerControls`, `AdvanceZoom`, `AdvanceAutoOrbit`, `UpdateResponsiveFit`, `ApplyCloseUp` and `ApplyCalibrationOrbitAnchor` | Camera interaction state now has one focused owner; `Program.smile` coordinates borrowed profile/bounds and keeps Party shot intent separate for the later Party owner |
| Calibration key banks, selection/edit/clipboard/Undo buffers, import baseline, target mapping/reset, wrist discovery, value bounds, wrist/equipment transforms, grip preservation and edit-session routines in `Program.smile` | `ViewerCalibration.State`, module-private bounded workspaces and stored operations; `ViewerCalibrationEditing` for edit/transaction operations; `ViewerCalibrationControls.ApplyPointerAction` for panel command routing | Actor capability discovery, profile-scoped storage, key transactions, transform/grip application, codec validation, rollback, import confirmation and panel command routing are direct production-module paths. `Program.smile` retains Party-preview entry and explicit session-failure adaptation; it no longer owns edit/grip implementation or one-command panel wrappers. |
| `LoadCalibration`, `SaveCalibration`, raw `CalibrationStorage*`/`CalibrationCandidate*` arrays and in-place key-array edits | `ViewerCalibration.Load`, `Persist`, `PrepareImport`, `CommitImport`, `DeleteCurrentKeyAndPersist`, `MoveKeyAndPersist`, `PasteClipboardAndPersist`, `ReloadCurrentKey`, `ClearClipAndPersist`, `ClearAllAndPersist`, `CommitCurrentKey`, `Undo` and focused query operations | Primary/backup recovery, rejected candidates and failed writes preserve the previous valid in-memory and stored revision. An explicit transaction result distinguishes mutation from persistence. Tests call the production owner with disposable identities and buffers; the coordinator no longer exposes storage/query pass-throughs. |
| Slider/timeline/repeat capture flags, ad hoc queued arrow checks and inspector/gizmo key policy in `Program.smile` | `ViewerInput.State`, `ClassifyArrow`, `ClassifyInspectorKey`, `ClassifyGizmoKey`, `UpdateBeforeSliders`, `UpdateTimelineScrub`, `BeginFrameRepeat` and capture reset/query operations; `ViewerTimelineEditing` for seek/drag completion; `ViewerInspectorCommands` for rendering/VFX pointer and rendering/camera keyboard action dispatch | State, detailed repeat/scrub/key-drag transitions, key-to-action policy and bounded presentation/navigation command routing moved with focused production tests. Timeline pointer seeking, adjacent-key navigation, duplicate prevention, persistence and pose refresh moved out of the coordinator. General hit geometry comes from `ViewerUi`; state mutation stays with rendering/effect/camera owners. Queued Ctrl is sampled once from `Key_Event_Held` and passed explicitly, and the invalid foreground-stealing `Window_Activate()` probe remains removed. |
| UI visibility, calibration panel/edit/confirmation fields, responsive geometry, calibration/timeline/general inspector hit classification, shared camera/calibration slider controls, status/camera/animation/calibration/timeline drawing, inspector chrome/footer/status/recovery rendering and presentation labels in `Program.smile` | `ViewerUi.State` with explicit visibility, calibration reset, edit and confirmation transitions; typed `SliderUpdate` control results; narrow label/geometry/hit operations plus focused presentation renderers; `ViewerCalibrationControls` for typed panel dispatch | State transitions, label/layout policy, all current UI hit maps, slider geometry/hit/value/capture handling, calibration panel command routing and raw presentation leaves moved. `Program.smile` no longer imports the low-level UI controls module. `DrawInspectorOverlay` retains only explicit cross-owner draw ordering; `HandleInspectorPointer` retains broader named action dispatch and capture sequencing. Dead labels, unused slider calculations/constants, one-command calibration wrappers and redundant geometry/query pass-throughs were deleted. Pending edits and imports must continue to block actor switches without changing their owner. |
| Transform-gizmo target origin, keyboard policy, projection, pointer ownership, drag remainder, hit testing, ring/move pointer conversion, drawing and grip state in `Program.smile` | `ViewerInput.ClassifyGizmoKey`; `ViewerCalibrationEditing.GizmoOrigin`, `SelectGizmoAxis`, `BeginGizmoDrag`, `CancelGizmoDrag` and `AdjustGizmoValue`; plus `ViewerGizmo.State` and `ClassifyPointer` with reset/show-hide/begin/finish/select/projection/hit-test/ring-delta/retained-value/draw operations | Target/socket and calibrated mutation reside with calibration editing; key policy resides with input; pointer decisions, projection and fractional drag behavior reside with gizmo state. The coordinator retains guarded runtime input sampling, typed action dispatch, Party-preview entry and session-result adaptation only. Gizmos remain opt-in and hiding retains the unsaved numeric preview for explicit Save or Cancel. |
| Party participants, inspector/preview binding, turn/stage/timing, attack/reaction state, Party battle cameras, pointer hit map, companion creation/calibrated updates, participant placement/drawing/destruction and Party overlay implementation in `Program.smile` | `ViewerParty.State`, two explicit `ParticipantLayout` values, `CreateCompanion`, `ResetDemo`, `AdvanceDemo`, `UpdateDragon`, `BeginInspectorSelection`, `EndInspectorSelection`, choreography/preview operations, `ApplyFrame`, participant updates, placement, `ApplyAttackCamera`, `ClassifyPointer`, drawing, overlay and destruction | Participant load/calibration/glow/playback, inspector target selection/capture, reset/formation/turn preparation, per-frame choreography/commands, Dragon reaction/target integration, preview playback, calibrated updates, camera construction/continuity, pointer classification, drawing/destruction and overlay rendering moved with integration/direct tests and static ownership contracts. The coordinator retains pointer action dispatch, session-failure and calibration-owner adaptation. Exactly two live Party participants remain explicit and same-model fallback must not alias actor state. |
| Scene VFX clock, equipment Fire/glow/trails, Fire/Lightning pause flags, visual-continuity epochs, light leases and Orin-storm state in `Program.smile` | `ViewerEffects.State`, `PrepareFire`, `PrepareLightning`, `UpdateEquipmentFire`, `UpdateEpicGlow`, `UpdateStorm`, draw operations, independent toggles, continuity invalidation and shared shutdown; `ViewerInspectorCommands` for the VFX action map | State, implementation and lifecycle now move together. No duplicate wrappers or equipment-effect arrays remain in `Program.smile`; the inspector dispatcher no longer enumerates VFX commands. Scene pause continues to leave VFX running by default while each family toggle freezes only that family. |
| Arena, floor/grid visibility, backdrop handles/index, lighting/material modes, socket object array, marker resources, display selection and renderer-mode/socket lifecycle routines in `Program.smile` | `ViewerRendering.State` with arena/backdrop operations, `ResetControls`, `ApplyLighting`, `CycleLighting`, material-inspection reset/apply/cycle, `CreateSocketGizmos`, `UpdateSocketGizmos`, `DrawSocketGizmos`, `CycleSocketDisplay`, selection and destruction; `ViewerInspectorCommands` for the rendering action map | Scene-resource and renderer-mode state, implementation and lifecycle now move together. The coordinator retains original load/reset/update/draw ordering and one typed inspector result. General inspector/calibration/timeline overlay composition remains to move to the UI owner; rendering does not own gameplay, Party or calibration state. |
| `BattleCamera.*` | `BattleCamera.*` | Already focused, retained |
| `BattleAudio.CueState/CrossedCue` | `BattleAudio.*` | Already focused, retained |
| `CalibrationJson.*` | `CalibrationJson.*` | Bounded reader retained; no second codec |
| `OrinStorm.*` | `OrinStorm.*` | Per-actor contexts and scene-owned Lightning retained |
| `DragonPresence.*` | `DragonPresence.*` | Frozen continuity ordering retained |
| Dragon actor globals plus `CreateDragon`, most of `UpdateDragon`, `DrawDragon`, `ClearDragonOwnedEffects`, Dragon audio and `DestroyDragon` | `ViewerDragon.State`, `Create`, `DesiredClip`, `Update`, `Draw`, `DrawEffects`, `UpdateAudio`, `ClearOwnedEffects` and `Shutdown`; `ViewerParty.UpdateDragon` owns Party reaction/target integration | Actor lifecycle, animation timing, aim, breath, frozen continuity and audio moved together. Party reaction timing/consumption and target choice now remain with Party state; the coordinator keeps only readiness adaptation at the frame-order call site. |

This table is updated in the same commit as each later move. Deleted routines are not
copied or left as dead wrappers.

## Test migration and navigation

| Existing proof | Production owner exercised after move | Migration status |
|---|---|---|
| `HardeningTests.smile` session/playback/clock/zoom/camera/input/UI/gizmo/Party/effects/rendering assertions | `ViewerSession`, `ViewerPlayback`, `ViewerTiming`, `ViewerCamera`, `ViewerCalibrationEditing`, `ViewerCalibrationControls`, `ViewerInspectorCommands`, `ViewerInput`, `ViewerUi`, `ViewerGizmo`, `ViewerActors`, `ViewerParty`, `ViewerEffects`, `ViewerRendering`, `BattleCamera`, shared `CharacterViewer` | New owners are included and called directly; playback selection state, inspector editing/Party/queued-Ctrl/socket/clip/gizmo keyboard policy, exclusive capture, current calibration-value and inactive-edit behavior, calibration-panel preview/selection/clear/Hold Grip/navigation routing, rendering/VFX inspector no-op/readiness/toggle transitions, keyboard camera sensitivity/grid routing and responsive-fit toggling, UI presentation labels, exact responsive/calibration/timeline geometry, calibration pointer boundaries/selection and slider endpoints, timeline endpoint/key hits, tab/transfer/panel/scene/profile/animation hit-map priority, unavailable actor/socket discovery and rotation/position bounds, all inspector presentation leaves, opt-in gizmo pointer decisions/retained-drag/hide/hit-test/draw and no-op edit operations, camera cancellation, Party reset/formation/binding/timing/preview policy/frame application/labels/pointer classification/battle-camera selection, actor facing, independent VFX pause, socket display/selection/part routing and rendering resource transitions exercise production code. Static contracts keep moved playback queries/algorithms, input key parsing, calibration discovery/bounds/edit-session/origin/gizmo mutation/command routing, rendering/VFX/camera inspector routing and fit behavior, UI drawing/constants/text and all UI hit geometry, gizmo pointer/drag/draw implementation, Party reset/advance/actor-command/presentation/camera implementation and socket/grid resources out of `Program.smile`. |
| `CalibrationTests.smile` plus generated isolated project | `ViewerPlayback`, `ViewerCalibration`, `ViewerCalibrationEditing`, `ViewerCalibrationControls`, `ViewerParty`, `ViewerEffects`, `ViewerRendering` and retained `CalibrationJson` | Real-actor mapping/name/start/frame-seek tests exercise `ViewerPlayback`; adjacent-key navigation and move/delete/paste/reload/clear, failed persistence, primary/backup recovery, import confirmation and Undo exercise the calibration owners without coordinator query/storage/command wrappers. The fixture sends Undo, clear and reload through `ViewerCalibrationControls.ApplyPointerAction`; current-value and remaining edit operations use their production owners. Party and Dragon-inspection loads call `ViewerParty.CreateCompanion`; assertions prove Arin/Orin identity, calibration availability and primary-bank restoration before a real calibrated companion update. The direct no-op update check distinguishes skipped from attempted updates. Native/generated-Web tests also prove scene pause leaves Fire and Lightning ages advancing by default, while explicit family toggles remain independent. |
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
  still contains substantial inspector selection and Party command dispatch; companion
  creation/calibration/glow/playback and calibrated participant update reside in
  `ViewerParty`. Transform-gizmo hit testing, retained drag math and drawing
  reside in `ViewerGizmo`; calibration edit lifecycle, bounded mutation, transform
  application ordering and grip preservation reside in `ViewerCalibrationEditing`.
  Only cross-owner pointer/keyboard dispatch, Party-preview entry and session-result
  adaptation remain in the coordinator for those paths.
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
