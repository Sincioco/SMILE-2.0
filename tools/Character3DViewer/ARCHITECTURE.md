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
| Startup and failure/session lifecycle | `Program.smile` | `ViewerSession.smile` plus thin `Program.smile` coordinator | Running/readiness, first error/stage, resource epoch, tab/profile and scene mode | Load/retry/switch/shutdown; borrows renderer and subsystem owners | One application session; native launch, reload and failure fixtures |
| Frame clocks and playback sequence | `Program.smile` plus `CharacterViewer.ClockState` | `ViewerTiming.smile`, then `ViewerPlayback.smile` | Frame-rate sample; selection, speed, pause/demo sequence | Advance/reset/query; borrows current actor and profile | Session; direct native/Web module assertions and playback fixture |
| Actor/inspector binding | `Program.smile` (`ViewerActorContext`, `PartyUi*`, `CalibrationOwnerProfile`) | `ViewerActors.smile` | Primary/inspected actor identity and temporary Party preview binding | Capture/use/begin/end preview; borrows actor handles and calibration owner | Scene/preview; Party isolation fixture |
| Camera and transforms | `Program.smile`, `BattleCamera.smile`, shared `Interaction` | `ViewerCamera.smile` and retained `BattleCamera.smile` math | Base/live camera, controls, zoom target, fractional pointer remainder, orbit anchor, auto-orbit | Reset/nudge/drag/advance/apply/query; borrows framing and current actor bounds | Scene; scripted integer outputs plus native/Chrome controls |
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
| `HardeningTests.smile` clock/zoom/camera assertions | `ViewerTiming`, `BattleCamera`, shared `CharacterViewer` | `ViewerTiming` now included and called directly; remaining checks migrate with owners |
| `CalibrationTests.smile` plus generated isolated project | Calibration, actor binding, Party/effects owners | Temporarily still uses bounded startup assembly; final project will include production modules directly |
| `ActorIsolationTests.smile` | `OrinStorm`, `Character3D`, scene VFX ownership | Already direct; retain explicit source list |
| PowerShell preservation fixtures | Launcher, synchronizer, canonical/publication validators | Direct production functions; disposable storage/processes only |

Three short maintenance routes define the desired end state:

- Camera adjustment: `ViewerCamera` state and integer-control tests, then the thin
  coordinator call; no calibration, Party, or renderer-storage tour.
- Calibration save failure: `ViewerCalibration` transaction, bounded JSON codec and
  isolated storage fixture; UI consumes only the resulting status/Undo state.
- Party timing: `ViewerParty` stage transition and actor snapshots, then
  `ViewerEffects` once-per-scene advancement; rendering only consumes snapshots.

