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

## Ownership map

| Responsibility | Baseline owner | Refactor owner | Owned mutable state | Public operations and borrowed dependencies | Lifetime / focused proof |
|---|---|---|---|---|---|
| Startup and failure/session lifecycle | `Program.smile` | `ViewerSession.smile` plus thin `Program.smile` coordinator | Running/readiness, first error/stage, resource epoch, tab/profile and scene mode | Reset/record/capture failure; coordinator borrows renderer and subsystem owners for load/retry/switch/shutdown | One application session; direct module assertions plus native launch, reload and failure fixtures |
| Frame clocks and playback sequence | `Program.smile` plus `CharacterViewer.ClockState` | `ViewerTiming.smile` and `ViewerPlayback.smile` | Frame-rate and clamped clocks; selection, speed, pause/demo sequence | Start/advance/reset/query, clip mode and demo target; borrows current actor/profile only for duration queries | Session; direct native module assertions and playback fixture |
| Actor/inspector binding | `Program.smile` (`ViewerActorContext`, `PartyUi*`, `CalibrationOwnerProfile`) | `ViewerActors.smile` | Primary/inspected actor identity and temporary Party preview binding | Capture/use/begin/end preview; borrows actor handles and calibration owner | Scene/preview; Party isolation fixture |
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

## Symbol migration map

| Old symbol/location | Current owner/symbol | Preservation note |
|---|---|---|
| `AdvanceFrameRate`, `FrameRateElapsed`, `FrameRateFrames`, `CurrentFramesPerSecond` | `ViewerTiming.Advance`, `ViewerTiming.FrameRateState`, `ViewerTiming.FramesPerSecond` | First low-risk extraction; identical 500 ms integer sampling, directly tested |
| `PreviousTime`, `ViewerClock`, copied elapsed/drop counters | `ViewerTiming.ClockState`, `ViewerTiming.Start`, `ViewerTiming.AdvanceClock` | Identical raw sample, clamp and long-pause contract; coordinator consumes explicit animation/camera/presentation outputs |
| `SelectedClip`, speed, pause/demo counters and helper calculations | `ViewerPlayback.State`, `ResolveAnimationElapsed`, `AdjustSpeed`, `ClipMode`, `Demo*` | Playback state is explicit; actor/profile handles are borrowed for queries and never retained |
| `Ready`, `ViewerError`, first error/stage, tab/profile and resource epoch | `ViewerSession.State`, `ResetFailure`, `RecordError`, `CaptureFailure` | First-failure retention and explicit retry reset preserved; stage capture remains adjacent to the coordinator stage |
| `BaseCamera`, `Camera`, `ViewerFrame`, `CameraControls`, `SmoothZoom`, pointer remainders, calibration orbit anchor and `AutoOrbit*` | `ViewerCamera.State` with `Reset`, `Compose`, `UpdatePointerControls`, `AdvanceZoom`, `AdvanceAutoOrbit`, `UpdateResponsiveFit`, `ApplyCloseUp` and `ApplyCalibrationOrbitAnchor` | Camera interaction state now has one focused owner; `Program.smile` coordinates borrowed profile/bounds and keeps Party shot intent separate for the later Party owner |
| Calibration key banks, selection/edit/clipboard/Undo buffers, storage envelope and import baseline in `Program.smile` | `ViewerCalibration.State` plus module-private bounded workspaces in `ViewerCalibration.smile` | `Program.smile` now coordinates UI-visible operations only; profile-scoped storage, codec validation, rollback and import confirmation are direct production-module paths |
| `LoadCalibration`, `SaveCalibration`, raw `CalibrationStorage*`/`CalibrationCandidate*` arrays and in-place key-array edits | `ViewerCalibration.Load`, `Persist`, `PrepareImport`, `CommitImport`, `MoveKey`, `CommitCurrentKey`, `Undo` and focused query operations | Primary/backup recovery, rejected candidates and failed writes preserve the previous valid in-memory and stored revision; tests use disposable identities and buffers |
| Slider/timeline/repeat capture flags and ad hoc queued arrow checks in `Program.smile` | `ViewerInput.State`, `ClassifyArrow` and capture begin/finish/reset operations | Queued Ctrl state is sampled from `Key_Event_Held`; captured release is routed before outside-window return. The invalid per-frame `Window_Activate()` focus probe was removed because that operation actively foregrounds the native window rather than querying activation. |
| UI visibility and calibration panel/edit/confirmation fields in `Program.smile` | `ViewerUi.State` with explicit visibility, calibration reset, edit and confirmation transitions | Presentation state is separate from calibration banks and actor resources; pending edits and imports block actor switches without changing their owner |
| Transform-gizmo projection, pointer ownership, drag remainder and grip state globals in `Program.smile` | `ViewerGizmo.State` with reset/show-hide/begin/finish/select/projection operations | Gizmos remain opt-in; hiding ends handle capture while retaining the unsaved numeric preview for explicit Save or Cancel |
| Party participants, inspector/preview binding, turn/stage/timing, attack/reaction state and Party camera globals in `Program.smile` | `ViewerParty.State`, two explicit `ParticipantLayout` values and focused reset/formation/binding/elapsed operations | Exactly two live Party participants remain explicit; primary, companion, Dragon and inspector contexts stay distinct, and same-model fallback never aliases actor state |
| Scene VFX clock, Fire/Lightning pause flags, visual-continuity epochs, light leases and audio-cue state in `Program.smile` | `ViewerEffects.State`, `AdvanceScene`, independent Fire/Lightning toggles, continuity invalidation and shared shutdown | One scene clock advances once per frame; scene pause freezes choreography but not VFX by default, while each existing family toggle freezes only that family |
| Arena, floor/grid visibility, backdrop handles and backdrop index in `Program.smile` | `ViewerRendering.State` with reset/create/draw/apply/destroy/toggle/cycle operations | Rendering owns renderer resources and presentation selection; it borrows actor/effects snapshots and does not own gameplay, Party or calibration state |
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

- Party, effects and rendering moved into focused owners without a shared replacement
  state monolith. `Program.smile` retains ordering and coordinates borrowed handles.
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
