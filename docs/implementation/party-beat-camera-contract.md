# Party Beat Camera Editor contract

Return to the [current entry point](party-beat-camera-checkpoint.md) for open
reports and native validation. The [September archive](archive/party-beat-camera-2026-09.md)
preserves delivery evidence; its old versions, host names and holds are historical.

## Editing and resets

The editor uses the existing Party renderer, actors and choreography. Each
character identity has its own sequence and saved head cuboid; camera copy/paste
can cross identities without merging their persistence. `ViewerBeatSequence.Name`
currently recognizes Arin, Orin, Dragon, Valor, Zara, Vrax, Mira and Kael. This
identity inventory is distinct from a particular Party formation.

- Beat Sequence replaces the regular Animation Timeline in its bottom layout
  while Preview/Edit is open, using the same controls and colors. Closing Preview
  restores the Animation Timeline. 0-Frame, Previous/Next Beat and Previous/Next
  Frame navigate it; held frame buttons repeat.
- Four ordered main markers cannot be deleted. Boundaries 2–4 resize adjacent
  camera intervals within the fixed action duration. At most sixteen extra shots
  can be inserted, moved across main beats and deleted. Navigation visits all
  markers chronologically; motion/interpolation uses the current shot interval.
- **Camera timing only:** movement, animation, impacts, audio/VFX cues and gameplay
  counters retain the original action schedule. Unauthored/default cameras keep
  battle policy; authored cameras follow the edited camera schedule.
- Pan, orbit, zoom and keyboard orbit remain available after scrubbing; editing
  resumes from the displayed camera. The fixed Camera panel targets that Beat
  camera with View, Present, All and Fit Beat actions.
- Space plays/pauses. Playback stops at the end; another Space restarts from zero.
  Pausing selects the current marker. Right-click exits Beat Editor and resets
  the current Party tab.
- Preview samples existing action poses, mutes live battle audio and restores
  turn state on close. Continuous samples retain equipment trails; discontinuous
  seeks clear stale trails. Independent Fire/Lightning freeze controls remain.

| Action | Draft changes | Preserved |
| --- | --- | --- |
| Reset Beat | Containing main beat's built-in camera, motion and connection | Timing and extra markers |
| Reset All Beats | Four default cameras and timing; removes extra shots | Head cuboids and other identities |
| Reset All Beat Lengths | Four default camera durations | Compositions, motion/connections; extra shots keep relative positions inside each main beat |
| Save | Atomically writes complete character sequence | Open editor, selected shot, playhead and camera-editing state |
| Cancel / close Preview | Restores the prior battle without writing a sequence | Saved sequence and independently saved head cuboids |

All resets require Save. A save failure retains the draft. Default drafts carry
`UseDefault`; merely displaying a captured editor view cannot turn a reset into
an authored static camera. A real camera edit adopts that view.

## Owners and compatibility

Source files are in [tools/Character3DViewer](../../tools/Character3DViewer/ARCHITECTURE.md).

| Owner | Responsibility |
| --- | --- |
| [BattleCameraShots](../../tools/Character3DViewer/BattleCameraShots.smile) | Double relative two-head framing, shot motion/link evaluation, checked scalar/text and head serialization |
| [BattleCameraTimeline](../../tools/Character3DViewer/BattleCameraTimeline.smile) | Four camera weights, main/extra markers, ordering, resize and versioned sequence persistence |
| [ViewerBeatTimeline](../../tools/Character3DViewer/ViewerBeatTimeline.smile) | Pointer gestures, navigation/frame repeat and replacement timeline |
| [ViewerBeatHeadPersistence](../../tools/Character3DViewer/ViewerBeatHeadPersistence.smile) | Per-identity current/persisted head values, pending/failure state, retry and discard |
| [ViewerBeatEditor](../../tools/Character3DViewer/ViewerBeatEditor.smile) | Selection, identity drafts/clipboard, Space preview, camera editing, staged resets and head controls |
| [ViewerBeatSequence](../../tools/Character3DViewer/ViewerBeatSequence.smile) | Existing actor/timing adapter, action sampling, bookmark/pose restoration and head/body picking |
| [ViewerWorkflow.Session](../../tools/Character3DViewer/ViewerWorkflow.smile) | Input/update/draw coordination and continuous/discontinuous visual history |

`BattleCamera.Sequence.<Character>.V2` contains timing weights and up to twenty
shots in one checksummed Save Data envelope (`SMILE-Sequence-2`). A valid V2
sequence wins, including an intentionally saved reset with no authored cameras.
Without it, legacy `BattleCamera.<Character>.Beat1` through `Beat4` records are read
without deleting or overwriting them. Head keys and pose JSON remain separate.
Native application storage and each Web origin have independent saves; camera
import/export is outside this editor slice.

`EffectiveValid` checks final rounded main/extra times, boundaries, neighbor
spacing, uniqueness and reachability against current action timing. Insert,
move, resize and reset are transactional. An incompatible stored schedule stays
on disk while playback uses the four safe main markers.

Newly captured/edited shots use `SMILE-Shot-2`. Their longitudinal value controls
horizontal placement, with its vertical contribution clamped between the two
heads. Bounded version-1 shots retain their original math; extreme version-1
shots use the built-in camera until edited/recaptured as version 2. Linked shots
interpolate target, distance and unit viewing direction on a deterministic
great-circle path, including opposing-view and parallel-up cases, preserving
exact endpoints. These rules do not alter sequence timing or actor animation.

Head picking/dragging uses raw logical viewport coordinates; inspector scrolling
affects visible panel controls only. Head-save warnings survive camera Save/Cancel.
Retry/Discard applies only to that identity; native close is deferred while the
head persistence owner still reports unresolved recovery.

## Validation boundaries

The [native hardening runner](../../scripts/test-character-3d-viewer-hardening.ps1)
and [calibration fixture](../../scripts/test-viewer-calibration-native.ps1) include
the current owners. [BeatCameraTests](../../tools/Character3DViewer/BeatCameraTests.smile)
and [HardeningTests](../../tools/Character3DViewer/HardeningTests.smile) cover
camera-only timing, framing, saved definitions, preview and restoration.

Use isolated storage; never replace accepted Viewer-exported character JSON with
an old backup or application-data envelope. Existing compiler startup/stack
regressions and old artifact hashes remain in the archive as their own evidence,
not a requirement to rebuild or install an old VSIX for ordinary camera edits.
The separate Studio concept is abandoned and Web work remains paused. Consult
[AGENTS.md](../../AGENTS.md) and the [Studio entry point](../../tools/Character3DViewer/README.md)
for current host/build/calibration policy.
