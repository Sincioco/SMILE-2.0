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
companion updates, Party pointer handling and overlays, equipment effects,
inspector/calibration UI, gizmo drawing and interaction, and resource lifecycle.
Passing module tests proves the extracted seams but does not make those remaining
procedures owned by the new modules.

`ViewerActors.smile` did not exist at the audit baseline. Earlier text naming it as
the current owner was an architecture target stated as a completed fact. R7.5 has now
introduced that focused owner for the actor context and actor load/update/draw/destroy
lifecycle. Inspector selection, preview orchestration and calibration/effect coupling
still remain to move. The bounded stage must not introduce a whole-application state
record or a replacement `ViewerApplication` module.

| Still implemented in `Program.smile` | Current evidence | Intended focused owner |
|---|---|---|
| Party transitions, camera application, companion lifecycle, inspector/preview binding, pointer handling and Party overlay | R7.5 moved the stage/Dragon-turn state machine, attack selection, KO/revive decisions and formation interpolation to `ViewerParty.AdvanceChoreography`; `ApplyPartyFrame` now applies its narrow actor commands. Camera application, companion lifecycle, binding, pointer handling and overlays remain in `Program.smile`. | `ViewerParty.smile`, with actor binding in a focused actor owner only if needed |
| Dragon actor lifecycle, timing, turn behavior and battle-audio coordination | `CreateDragon`, `UpdateDragon`, `AdvanceDragonTurn`, `UpdateBattleAudio` and related routines | retained `DragonPresence.smile`, `BattleAudio.smile`, and narrow Party contracts |
| Equipment Fire/glow/trails, scene VFX coordination and effect teardown | `CreateEpicGlow`, `UpdateEquipmentFire`, `UpdateSwordFire`, `UpdateShieldFire`, trail routines and clear/hide routines | `ViewerEffects.smile` plus retained focused effect modules |
| Inspector, calibration panel, timeline, camera controls, buttons and overlays | `HandleInspectorPointer`, `HandleCalibrationPanelPointer`, `DrawInspectorOverlay`, `DrawCalibrationPanel`, `DrawTimeline` and related layout/label routines | `ViewerInput.smile` and `ViewerUi.smile`, borrowing narrow subsystem operations |
| Transform-gizmo pointer/update/draw implementation | `HandleTransformGizmoPointer`, `UpdateTransformGizmoFromPointer`, `DrawTransformGizmo` and related routines | `ViewerGizmo.smile` |
| Socket resources and studio grid drawing | socket create/update/draw/destroy routines and `DrawStudioGrid` | `ViewerRendering.smile` or a focused socket-render owner |
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

## Ownership target map

| Responsibility | Baseline owner | Intended refactor owner | Owned mutable state | Public operations and borrowed dependencies | Lifetime / focused proof |
|---|---|---|---|---|---|
| Startup and failure/session lifecycle | `Program.smile` | `ViewerSession.smile` plus thin `Program.smile` coordinator | Running/readiness, first error/stage, resource epoch, tab/profile and scene mode | Reset/record/capture failure; coordinator borrows renderer and subsystem owners for load/retry/switch/shutdown | One application session; direct module assertions plus native launch, reload and failure fixtures |
| Frame clocks and playback sequence | `Program.smile` plus `CharacterViewer.ClockState` | `ViewerTiming.smile` and `ViewerPlayback.smile` | Frame-rate and clamped clocks; selection, speed, pause/demo sequence | Start/advance/reset/query, clip mode and demo target; borrows current actor/profile only for duration queries | Session; direct native module assertions and playback fixture |
| Actor/inspector binding | `Program.smile` (`ViewerActorContext`, `PartyUi*`, `CalibrationOwnerProfile`) | `ViewerActors.smile` now owns `Context` and actor load/update/draw/destroy; inspector selection and preview orchestration remain for R7.5 | Primary/inspected actor identity and temporary Party preview binding | Capture/apply/load/update/draw/destroy now moved; begin/end preview still borrows calibration owner from `Program.smile` | Scene/preview; direct context assertions plus integrated Party isolation fixture |
| Camera and transforms | `Program.smile`, `BattleCamera.smile`, shared `Interaction` | `ViewerCamera.smile` and retained `BattleCamera.smile` math | `ViewerCamera.State`: base/live camera, frame, controls, zoom target, fractional pointer remainder, orbit anchor and auto-orbit | Reset/compose/nudge/drag/advance/apply/query; borrows framing profile and current actor bounds without retaining either | Scene; direct integer-output assertions plus native/installed-Chrome controls |
| Calibration editing | `Program.smile`, `CalibrationJson.smile` | `ViewerCalibration.smile` and retained bounded JSON reader | Per-profile key banks, edit/clipboard/Undo/import workspace and selected transform | Configure/load/evaluate/edit/save/Undo/import/export/query; borrows inspected actor and storage primitives | Profile workspace; native/generated-Web isolation and malformed imports |
| File transactions and launcher synchronization | Viewer Save/Load statements, synchronizer and launcher | `ViewerCalibration.smile`, `sync-arin-v5-7-calibration.ps1`, `Launch.ps1` | Checked save baseline/pending revision; primary/backup selection | Transaction commit/recovery/watch; canonical JSON is borrowed source of truth | Save/application identity; preservation fixtures |
| Input ownership | `Program.smile` | `ViewerInput.smile` | Pointer capture, timeline/frame repeat and queued command routing | Route keyboard/pointer; borrows UI, calibration and camera operations | Frame/capture; queued-modifier and outside-window fixtures |
| UI and transform gizmo | `Program.smile` | `ViewerUi.smile` and `ViewerGizmo.smile` | Panel visibility, slider owner, opt-in gizmo drag state | Hit test/update/draw/cancel; borrows camera projection and calibration edit operations | Scene/edit; pointer exclusivity and cancel tests |
| Party choreography | `Program.smile` | `ViewerParty.smile` | Participants, turn/stage/timing, guard/hit/KO/revive, preview state and Party cameras | Create/reset/advance/draw/destroy/bind inspector; borrows actors, camera and effects | Party scene; timing, same-model isolation and inspector fixtures |
| Effects/audio | `Program.smile`, `OrinStorm.smile`, `DragonPresence.smile`, `BattleAudio.smile` | `ViewerEffects.smile` plus retained focused modules | Equipment/Dragon emitters, trails, leases, scene clocks and visual-continuity epochs | Create/update/advance-once/draw/invalidate/destroy; borrows final actor transforms | Scene; frozen-cut/skipped-cue/lease cleanup tests |
| Rendering and overlay composition | `Program.smile` | `ViewerRendering.smile` | Arena/backdrop/grid/socket render resources and transient layout | Begin/draw/end/overlay; borrows read-only snapshots from owners | Scene; native/generated-Web draw and resize checks |
| Build/publication | `Build.ps1`, `Prepare-BuildAssets.ps1`, explicit projects | same scripts with explicit module inventory | Disposable staging/publications only | Canonical preflight, compile, selected-output validation | Build; Release/Debug and Full/Low/Medium/High manifests |

Immutable `Character3D` cache entries are shared resources. Actor pose, equipment
visibility, calibration, inspector selection, effects, and scene clocks are never cache
state. Arin, Orin, Dragon, Party, and inspected-actor identities remain distinct.

## Symbol migration target map

| Old symbol/location | Intended owner/symbol | Preservation note |
|---|---|---|
| `AdvanceFrameRate`, `FrameRateElapsed`, `FrameRateFrames`, `CurrentFramesPerSecond` | `ViewerTiming.Advance`, `ViewerTiming.FrameRateState`, `ViewerTiming.FramesPerSecond` | First low-risk extraction; identical 500 ms integer sampling, directly tested |
| `PreviousTime`, `ViewerClock`, copied elapsed/drop counters | `ViewerTiming.ClockState`, `ViewerTiming.Start`, `ViewerTiming.AdvanceClock` | Identical raw sample, clamp and long-pause contract; coordinator consumes explicit animation/camera/presentation outputs |
| `ViewerParty.ActorContext`, companion actor load/update/draw/destroy mapping in `Program.smile` | `ViewerActors.Context`, `Capture`, `Apply`, `LoadContext`, `Update`, `Draw` and `Destroy` | Actor handles and per-actor inspection fields have a focused owner. Party borrows contexts; calibration and effects remain separate owners. Inspector/preview orchestration remains to move. |
| `SelectedClip`, speed, pause/demo counters and helper calculations | `ViewerPlayback.State`, `ResolveAnimationElapsed`, `AdjustSpeed`, `ClipMode`, `Demo*` | Playback state is explicit; actor/profile handles are borrowed for queries and never retained |
| `Ready`, `ViewerError`, first error/stage, tab/profile and resource epoch | `ViewerSession.State`, `ResetFailure`, `RecordError`, `CaptureFailure` | First-failure retention and explicit retry reset preserved; stage capture remains adjacent to the coordinator stage |
| `BaseCamera`, `Camera`, `ViewerFrame`, `CameraControls`, `SmoothZoom`, pointer remainders, calibration orbit anchor and `AutoOrbit*` | `ViewerCamera.State` with `Reset`, `Compose`, `UpdatePointerControls`, `AdvanceZoom`, `AdvanceAutoOrbit`, `UpdateResponsiveFit`, `ApplyCloseUp` and `ApplyCalibrationOrbitAnchor` | Camera interaction state now has one focused owner; `Program.smile` coordinates borrowed profile/bounds and keeps Party shot intent separate for the later Party owner |
| Calibration key banks, selection/edit/clipboard/Undo buffers, storage envelope and import baseline in `Program.smile` | `ViewerCalibration.State` plus module-private bounded workspaces in `ViewerCalibration.smile` | `Program.smile` now coordinates UI-visible operations only; profile-scoped storage, codec validation, rollback and import confirmation are direct production-module paths |
| `LoadCalibration`, `SaveCalibration`, raw `CalibrationStorage*`/`CalibrationCandidate*` arrays and in-place key-array edits | `ViewerCalibration.Load`, `Persist`, `PrepareImport`, `CommitImport`, `MoveKey`, `CommitCurrentKey`, `Undo` and focused query operations | Primary/backup recovery, rejected candidates and failed writes preserve the previous valid in-memory and stored revision; tests use disposable identities and buffers |
| Slider/timeline/repeat capture flags and ad hoc queued arrow checks in `Program.smile` | `ViewerInput.State`, `ClassifyArrow` and capture begin/finish/reset operations | State and low-level capture transitions moved; substantial inspector, timeline and calibration pointer routing remains for R7.5. Queued Ctrl is sampled from `Key_Event_Held`, and the invalid foreground-stealing `Window_Activate()` probe remains removed. |
| UI visibility and calibration panel/edit/confirmation fields in `Program.smile` | `ViewerUi.State` with explicit visibility, calibration reset, edit and confirmation transitions | State transitions moved; substantial layout, drawing and labels remain for R7.5. Pending edits and imports must continue to block actor switches without changing their owner. |
| Transform-gizmo projection, pointer ownership, drag remainder and grip state globals in `Program.smile` | `ViewerGizmo.State` with reset/show-hide/begin/finish/select/projection operations | State moved; pointer mathematics and drawing implementation remain for R7.5. Gizmos remain opt-in and hiding retains the unsaved numeric preview for explicit Save or Cancel. |
| Party participants, inspector/preview binding, turn/stage/timing, attack/reaction state and Party camera globals in `Program.smile` | `ViewerParty.State`, two explicit `ParticipantLayout` values and focused reset/formation/binding/elapsed operations | State and a few calculations moved; the transitions, actor lifecycle, pointer/UI and camera implementation remain for R7.5. Exactly two live Party participants remain explicit and same-model fallback must not alias actor state. |
| Scene VFX clock, Fire/Lightning pause flags, visual-continuity epochs, light leases and audio-cue state in `Program.smile` | `ViewerEffects.State`, `AdvanceScene`, independent Fire/Lightning toggles, continuity invalidation and shared shutdown | Scene-clock state moved; equipment emitters, glow, trails, update/draw and teardown implementation remain for R7.5. Scene pause continues to leave VFX running by default while each family toggle freezes only that family. |
| Arena, floor/grid visibility, backdrop handles and backdrop index in `Program.smile` | `ViewerRendering.State` with reset/create/draw/apply/destroy/toggle/cycle operations | Basic state and backdrop/floor operations moved; grid, sockets and overlay composition remain for R7.5. Rendering borrows actor/effects snapshots and does not own gameplay, Party or calibration state. |
| `BattleCamera.*` | `BattleCamera.*` | Already focused, retained |
| `BattleAudio.CueState/CrossedCue` | `BattleAudio.*` | Already focused, retained |
| `CalibrationJson.*` | `CalibrationJson.*` | Bounded reader retained; no second codec |
| `OrinStorm.*` | `OrinStorm.*` | Per-actor contexts and scene-owned Lightning retained |
| `DragonPresence.*` | `DragonPresence.*` | Frozen continuity ordering retained |

This table is updated in the same commit as each later move. Deleted routines are not
copied or left as dead wrappers.

## Test migration and navigation

| Existing proof | Production owner exercised after move | Migration status |
|---|---|---|
| `HardeningTests.smile` session/playback/clock/zoom/camera/input/UI/gizmo/Party/effects/rendering assertions | `ViewerSession`, `ViewerPlayback`, `ViewerTiming`, `ViewerCamera`, `ViewerInput`, `ViewerUi`, `ViewerGizmo`, `ViewerParty`, `ViewerEffects`, `ViewerRendering`, `BattleCamera`, shared `CharacterViewer` | New owners are included and called directly; queued modifiers, exclusive capture, opt-in hide, camera cancellation, Party formation/binding/timing, independent VFX pause and rendering resource transitions exercise production code |
| `CalibrationTests.smile` plus generated isolated project | `ViewerCalibration`, `ViewerParty`, `ViewerEffects`, `ViewerRendering` and retained `CalibrationJson` | Production owners are included directly; native/generated-Web tests prove scene pause leaves Fire and Lightning ages advancing by default, while explicit family toggles remain independent |
| `ActorIsolationTests.smile` | `OrinStorm`, `Character3D` and scene VFX ownership | Native/Web normal and forced-fallback runs retain two independent same-model actor contexts, sockets and lifecycles |
| PowerShell preservation fixtures | Launcher, synchronizer, canonical/publication validators | Direct production functions; disposable storage/processes only |

## R6 preservation and defect evidence

- Party, effects and rendering state plus selected calculations moved into focused
  owners without a shared replacement state monolith. The R7.5 audit corrects the
  earlier overstatement that their implementation had fully moved: `Program.smile`
  still contains the substantial routines listed above.
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
