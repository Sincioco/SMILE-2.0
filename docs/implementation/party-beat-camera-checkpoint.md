# Party Beat Camera Editor

Status (2026-09-11): the initial editor was published in `fd73f9d` and built for
Desktop/Web; focused automated and visible tool checks passed. Human camera-gesture
acceptance remains pending. The subsequent timeline enhancements below are requested,
not implemented. Sin now prioritizes Desktop only; Web follow-through is on hold.

## Scope

Party Dragon and Party Vrax share selectable combatants and four saved camera shots
per character. Dragon and Vrax are attackers with their own four beats. Copy/paste
transfers camera composition, motion and connection settings across beats and
characters. Each character owns one independently saved head cuboid (offset plus
width/height/depth), reused as attacker or target. A temporary sequence timeline
scrubs actual choreography; Save closes it, and Preview Beats opens it without
editing saved cameras. Missing shots retain the existing camera policy.

## Requested Timeline Enhancements — Desktop First

Sin's September 11 direction prioritizes velocity and Desktop implementation and
validation only. Keep using the shared owners; do not fork a separate Desktop
implementation. Do not rebuild, publish or test Web for this enhancement phase.
Record each implemented change here so Web can adopt it when Sin resumes that work.

- Match the regular Animation Timeline's appearance and interactions, including
  0-Frame, Previous Beat, Next Beat, Previous Frame and Next Frame controls.
- Drag beat boundaries to lengthen/shorten beats. The original four beats remain
  ordered and cannot be deleted.
- Insert camera shot markers between the main beats. Previous/Next navigation must
  visit both main and inserted markers. Optional marker deletion was proposed in the
  summary but has not been explicitly confirmed.
- Preserve per-character camera settings and head references for heroes and bosses.
- Keep viewport pan, zoom and orbit available in Beat Edit mode for every camera
  marker. Timeline dragging, camera dragging and head-cuboid dragging remain distinct.
- Open timing decision: does resizing a beat change only the camera schedule, or
  also retime battle movement/animation, impact events, SFX and VFX? Do not silently
  choose between these meanings. No runtime changes for this request have started.

### Deferred Web Adoption Record

Baseline Web artifact/evidence below belongs to `fd73f9d`, not these enhancements.
For each future Desktop milestone, append its commit, changed source owners, save
format/timing changes, native evidence and concrete Web work still required. Shared
source changes alone do not establish a validated or published Web implementation.

| Desktop milestone | Changed owners / compatibility | Web follow-through |
| --- | --- | --- |
| Scope recorded; no enhancement code yet | Existing four-shot saves unchanged | On hold: port/adopt new timeline controls and markers, verify save compatibility and browser input, rebuild/publish and run focused Chrome checks |

The original pending Chrome camera-gesture acceptance also remains unverified and
is deferred with this Web work. Resume from the recorded Desktop milestones rather
than repeating unaffected implementation, asset preparation or full smoke tests.

## Owners

- `BattleCameraShots.smile`: precise relative frame, shot evaluation, interpolation,
  bounded invariant text encoding and separate Save Data records for shots/heads.
- `ViewerBeatSequence.smile`: identity/selection map, timing adapter, existing actor
  lookup, choreography sampling/bookmark restoration, head query and body/bounds picking.
- `ViewerBeatEditor.smile`: draft/clipboard, shared character head definitions,
  editing/preview state, controls and projected cuboid/timeline presentation.
- `ViewerWorkflow.Session`: existing input/update/draw ordering and owner integration.
- `ViewerCamera.ComposeShot`: orbit/pan from arbitrary saved camera directions.
- `ViewerInput.ClassifyBeatKey` and `ViewerPlayback`: key classification and pause ownership.

The canonical Valor/Zara/Vrax descriptors now expose Head on their verified `head`
bones (GLB node indices 100, 49 and 125). Their package manifests record the updated
descriptor hashes. GLBs, materials, animation data and grounding remain unchanged.
Studio's project only adds the shared module inventory required by its existing
Viewer host. No compiler/runtime/VSIX payload or new Studio workspace is involved.

## Evidence

- Baseline HEAD/origin/main: `b77bc6d`; starting worktree clean.
- `artifacts/temp/beat-camera-hardening-final.log`: existing focused native gate,
  BeatCameraTests and generated-Web exact console parity passed; 58 native graphics,
  pointer and audio-focus checks passed. Shot tests cover fractional round trips,
  translated/rotated formations, motion, linked boundaries, unauthored fallback,
  cross-character copy, separate head storage and malformed numeric records.
- `artifacts/temp/beat-camera-seek-web-final.log`: actual-actor native/generated-Web
  calibration fixture passed after the final seek/restoration fixes. It checks reverse
  beat seeking, exact clip/time/mode restoration and fatal-reaction tail restoration
  without advancing the turn. The disposable Web fixture emitted SML3605 for its old
  publication identity and safely republished; final production builds passed.
- `artifacts/temp/beat-camera-formatter-tests.log`: 13 formatter integration tests
  passed. `beat-camera-format-check.log`: repository check passed for 438 tracked
  SMILE files; new modules were explicitly formatted and checked during development.
- Final native/Web publications passed: `artifacts/temp/beat-camera-build-final.log`.
  Final shared Studio native/Web build passed: `beat-camera-studio-build.log`.
- Native tool observations: right inspector, Beat 1 editing, posed actor, cuboid and
  second timeline. Normal final launcher succeeded: `beat-camera-launch-final.log`.
  After restart the native screen helper failed to capture the foreground process;
  final native gesture acceptance is unavailable to the tool and remains pending.
- Chrome tool observations at `http://127.0.0.1:8780/`: actor selection independent of
  turn, Arin camera Save/reload, head width auto-save, cross-character Paste preview
  and Cancel, Vrax attack/recovery scrubbing, Dragon's own four beats, pan and
  right-click return to the opening shot. Save closed the second timeline. Final
  head/body picking selected Orin beside Vrax without moving the camera. Chrome is
  left foreground in Dragon Beat 4 editing for the pending human gesture check.
- No human gesture acceptance has been recorded for this new feature. Earlier
  Double/Viewer confirmations are not reused as acceptance of these new controls.

## Reproduced Defects Fixed

- The panel's `Left` name collided with KEY_LEFT; PanelLeft now positions it correctly,
  verified in native and Chrome. Right-click now restores the opening draft shot.
- Scrubbing beyond short clips triggered graphics error 48. The existing actor owner
  now wraps looping clocks and clamps one-shots before seeking, on both targets.
- Preview restoration now preserves exact clips/times/modes and extended death-tail
  state. Reciprocal camera connections no longer traverse the same boundary twice.
- A click initially started pan; large boss bounds could also steal a nearby hero
  click. Click capture now suppresses pan until a drag threshold, and generic
  head/body picking precedes full actor bounds. Final Chrome checks passed.

## Source And Artifact Evidence

SHA-256 of final source bytes (under `tools/Character3DViewer`):

| Source | SHA-256 |
| --- | --- |
| BattleCameraShots.smile | `AC9270E84DA1AE1A2FD813A485845E82EAA6A555B07B2FD236B21794B4D057A4` |
| ViewerBeatSequence.smile | `7012026BAF783F8D8746C08DFDA7E215DE7EE69C654EE2D8A5F33375EC6B6EEB` |
| ViewerBeatEditor.smile | `1E0370372D7A47EAD555EF7B60225934BC1C9D857F700197B3FC79FD6A0BA7A4` |
| ViewerWorkflow.smile | `8E94BCD22C4EF2AAE0072C222EB26AE0B49B4812640D228F23775D4364FFA07A` |

Final generated artifacts (ignored build outputs; SMILE 2.0.63):

| Artifact | SHA-256 |
| --- | --- |
| Character3DViewer/bin/Release/Character3DViewer.exe | `CE7AF508707482BEEC9151CD5251DF238E67506240A06B084F86120206037499` |
| Character3DViewer/bin/Release/Web/game.js | `4464498A9A1F36D0E5BFA160EF8BB3245E57D9AC1CD13563717697BECED12E60` |
| Character3DViewer/bin/Release/Web/smile-runtime.js | `90BB2837F60140E0924E8D53F3629B3041FC1EA4420BF9EBC34E48E61C75EC59` |
| SmileStudio/bin/Release/SmileStudio.exe | `46F3973CDDC626524DA1AEC8DD382C167596DB4943FA365ADDFEEA412C124731` |

Artifact paths above are relative to `tools/`. Viewer Web compilation metadata:
2026-09-11 01:07:31 +08:00. Native launch and pre-publication calibration export
preserve Arin's 24 keys, SHA-256
`7A3E7BC823CF544FA0136920A9D0752BF7DE585C0891B9073E688B1F783B3F67`,
and Orin's zero keys, SHA-256
`13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.

## Limits

Picking uses generic head/body regions then bounds, not skinned triangle picking;
Tab disambiguates overlapping actors. Cuboid axes/offsets follow actor yaw while
their centers follow animated Head sockets. Native and each Web origin own separate
camera saves; cross-target import/export is not provided by this slice. Connections
wait for a saved neighbor. Stationary means no added orbit/dolly while the shot still
follows its two-head reference frame. Double survives to the existing Precision3D
submission boundary; GPU float limits remain as documented there. Usage and detailed
control semantics are in the Viewer README.

## Next Action / Remaining

Resolve the timing decision above before implementing dependent timeline changes.
Implement and validate the requested enhancement on Desktop, recording each change
in the deferred Web adoption record. The original Desktop slow/moderate horizontal/
vertical middle-button orbit, pan, zoom both ways and reset acceptance is still
pending; combine it with the affected Desktop interaction check when practical.
Tool pan/reset and numeric tests do not substitute for human observations. Fix any
reproduced in-scope defect through its existing owner and rerun affected checks.
Reuse unchanged build/test/pose evidence. Do not rebuild or reinstall for documentation
alone. No VSIX installation is required for this scope-recording change.

On hold: Web Beat Sequence enhancements, publication and validation, including the
outstanding Chrome gesture acceptance, until Sin directs their resumption.
