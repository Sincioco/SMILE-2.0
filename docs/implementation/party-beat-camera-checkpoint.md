# Party Beat Camera Editor — Desktop Checkpoint

Status (2026-09-11): the initial editor is published in `fd73f9d`; Desktop-first
scope was recorded in `26bb3a9`. The current Desktop timeline/reset enhancement is
implemented, built and validated with focused automated, visible tool and explicit
human gesture checks. The commit containing this checkpoint is the Desktop delivery
milestone. Web follow-through remains on hold.

## Camera Frame Hardening Addendum (2026-09-11)

Desktop playback reproduced an authored Beat 3 camera rising far above the fight as
Vrax entered his hit animation, then cutting to the fixed Beat 4 composition. This was
not interpolation across the Beat 3/4 boundary: Beat 3 was a saved Orbit Right shot
with a Cut connection. Its version-1 eye was about nineteen attacker-to-target lengths
behind the attacker. The old frame used that same unbounded longitudinal value against
the animated vertical head delta, amplifying an ordinary Vrax head drop into the large
camera rise. Beat 4 then correctly applied its explicit cut to the built-in fixed shot.

Shot frame version 2 keeps the authored longitudinal value for horizontal placement
but clamps only its vertical contribution to the span between the two heads. Newly
captured and edited shots use `SMILE-Shot-2`. Existing version-1 shots within a bounded
longitudinal margin keep their original frame math. Extreme version-1 shots, including
the reproduced Beat 3 data, use the live built-in camera until editing recaptures them
as version 2. Sequence envelopes, timing, connections, actor animation and head-cuboid
storage remain unchanged.

Focused validation passed after the change. The NativeOnly Viewer hardening run
completed all 42 isolated calibration checks and 58 native graphics, pointer-input
and audio-focus checks, including the new far-camera moving-head and versioned legacy
regressions. Release builds succeeded for both `Character3DViewer.exe` (110 assets)
and the shared `SmileStudio.exe` consumer (111 assets). In a fresh rebuilt Viewer,
the previously unsafe interval reported camera Y 85 at frames 1158 and 1200 while
Vrax's animated head changed height; Beat 4 reported Y 85 at frame 1238. The prior
reproduction rose from Y 472 to Y 1337 before that boundary. The validation draft
was cancelled, so no user camera sequence was saved or deleted.

## Current Contract

Party Dragon and Party Vrax use the existing renderer, actors and choreography.
Heroes, Dragon and Vrax each own four main camera shots. Selection, relative two-head
framing, per-character saved head cuboids and cross-character camera copy/paste remain.

- The second timeline matches the regular Animation Timeline's controls and colors:
  0-Frame, Previous/Next Beat, Previous/Next Frame; held frame buttons repeat.
- Four ordered main markers cannot be deleted. Dragging boundaries 2–4 changes
  adjacent camera durations within the fixed battle duration. Up to sixteen extra
  shots can be inserted, moved across main beats and deleted. Navigation visits all
  markers chronologically; motion/interpolation uses the current shot interval.
- **Camera timing only.** Sin explicitly reversed the earlier action-retiming request.
  Movement, animation, impacts, audio/VFX cues and gameplay counters retain the
  original action schedule. Default/unauthored cameras retain the existing battle
  policy. Authored camera intervals follow the edited camera schedule.
- Pan, orbit, zoom and keyboard orbit remain available after scrubbing; editing resumes
  from the displayed camera. Space plays/pauses the preview playhead. It stops at the
  end; another Space restarts from zero. Pausing selects the current camera marker.
- Preview samples existing action poses, with live battle audio muted and turn state
  restored on closing. Continuous samples retain equipment VFX trails; discontinuous
  seeks clear stale trails. Existing independent Fire/Lightning freeze controls remain.
- Reset Beat restores the containing main beat's built-in camera/motion/connection,
  keeping timing and extra markers. Reset All Beats restores the four default cameras
  and timing, removing extra shots from the draft. Both require Save; Cancel preserves
  saved settings. Head cuboids and other characters are untouched.
- Reset All Beat Lengths restores only the four default camera durations; camera
  compositions, motion/connections and extra shots remain. Extra shots retain their
  relative positions within each main beat. This timing-only reset also waits for Save.
- Save writes the complete character sequence atomically and closes the second
  timeline. Preview Beats can show it without editing. Head cuboids auto-save separately.

## Owners And Compatibility

| Owner | Responsibility |
| --- | --- |
| `BattleCameraShots.smile` | Double relative framing, shot motion/link evaluation, bounded scalar/text and independent head storage |
| `BattleCameraTimeline.smile` | Four camera weights, main/extra markers, ordering, resize and versioned sequence persistence |
| `ViewerBeatTimeline.smile` | Pointer gestures, navigation/frame repeat and second timeline presentation |
| `ViewerBeatEditor.smile` | Selection, per-character drafts/clipboard, Space preview, camera editing, staged resets and head controls |
| `ViewerBeatSequence.smile` | Existing actor/timing adapter, original action sampling, bookmark/pose restoration and head/body picking |
| `ViewerWorkflow.Session` | Existing input/update/draw coordination, discontinuous versus continuous visual history |
| `MasmEmitter.AllocateStack` | Shared native allocation guard-page probing for large local/call frames |

`BattleCamera.Sequence.<Character>.V2` stores the timing weights and up to twenty
shots in one checksummed Save Data envelope (`SMILE-Sequence-2`). A valid V2 sequence
wins, including an intentionally saved reset with no authored cameras. Without it,
legacy `BattleCamera.<Character>.Beat1` through `Beat4` records are read; they are not
deleted or overwritten. Head keys and pose JSON remain unchanged. Native and each
Web origin own independent saves; camera import/export is outside this slice.

Default drafts retain a separate `UseDefault` flag so displaying a captured editor
view cannot turn a reset into a saved static camera. An actual camera edit adopts
that view. Save failure keeps the draft; Cancel never writes a sequence.

The Viewer project/build and hardening fixture list the two new modules. Studio's
project lists those same dependencies because its existing host imports the Viewer;
its native build is a shared-consumer check, not a new Studio phase. Program.smile's
coordinator, shared Number/Double rules, assets and live calibration are preserved.

## Validation And Observations

- Baseline HEAD/origin/main `26bb3a9`, starting worktree clean. No reset, force push,
  historical Doctor/RF replay, asset rebake or calibration migration.
- `artifacts/temp/beat-timeline-native-tests.log`: NativeOnly hardening fixture passed,
  including camera-only resize, ordered extra markers, serialization/actual Save Data,
  malformed-record preservation, frame stepping, resume-from-view, Space pause/play,
  reset draft isolation, timing-only reset and default fallback serialization; 58 native graphics,
  pointer and audio-focus checks passed. Web fixture execution skipped.
- `beat-timeline-compiler-tests.log`: 323 language/compiler/project/completion/timing
  tests passed. Negative-test synthetic failures in the log are expected assertions.
- `beat-timeline-formatter-tests.log`: 13 formatter integration tests passed.
  Changed/new SMILE sources were explicitly formatted and checked. The repository
  check passed for 442 tracked files; the two new modules passed an explicit check.
- `beat-timeline-build.log`: Desktop Viewer rebuilt successfully, 110 assets reused/
  published. `beat-timeline-studio-build.log`: existing Studio native host built,
  111 assets. No new Viewer/Studio Web build or publication was performed.
- `beat-timeline-vsix-build.log` and `beat-timeline-vsix-install.log`: VSIX 2.0.63
  rebuilt and installed while Visual Studio was already closed. Installer verified
  all 35 compiler/language/library/template payload hashes against the built package.
  No user window was force-closed. Further Viewer-only edits need no reinstall.
- Native tool observations: stable launch after compiler fix; timeline controls and
  Reset Beat/Reset All Beats visible; frame step advances sequence time and sampled
  pose; an extra shot appears and is editable; Space advances the sequence. Reset
  Beat restored its camera while retaining modified timing. Reset All restored the
  four default camera boundaries. Cancel closed the timeline and retained the saved
  Beat 1. No reset was written to the user's live camera records during tool tests.
- Final build observations: Reset All Beat Lengths is visible and invokes the
  timing-only reset. Space advanced the playhead with equipment flames/trails visible,
  and a second Space paused at 634 ms with Beat 3 selected. The end/replay behavior
  was also observed. Test drafts were cancelled; the pre-existing saved camera and
  independently saved head reference survived the normal restart.
- Sin explicitly confirmed that dragging the Beat 2 marker moves it and it stays
  there. The automatic drag that moved only the playhead was a tool-delivery limitation;
  earlier screenshot-ID helper failures recovered with fresh observations.
- Sin explicitly confirmed slow/moderate horizontal/vertical MMB orbit, left pan,
  wheel zoom both ways and right-click reset: all smooth and stopping cleanly. These
  are new Beat Edit observations, not reused Double confirmations. His independently
  saved Arin head adjustment during the check was preserved.

Unchanged evidence from the initial editor remains valid: `fd73f9d` native/generated-
Web fractional framing, linked shots, copy/paste, head storage, action sampling and
exact bookmark restoration. Its Chrome selection, Save/reload, head resize, paste/
Cancel, Vrax/Dragon scrub, pan/reset observations are not new timeline validation.
See Git history for the original detailed checkpoint; do not repeat unaffected tests.

## Reproduced Defects Fixed In This Milestone

- Larger sequence/editor records exposed a native access violation during local
  initialization: the generated frame subtracted 131184 bytes without touching the
  intervening Windows guard pages. Diagnostic object/fault-offset inspection located
  the write. `AllocateStack` probes each page before variable-sized frame allocation,
  preserving incoming integer/XMM arguments. A native regression initializes two
  8192-element Number/Double arrays and checks a fractional argument. The real Viewer
  now launches and remains running. No assembly rewriting or retired test was used.
- Continuous preview initially invalidated equipment visual history every frame,
  repeatedly clearing fire trails. Only discontinuous seeks now invalidate it.
- Pause/end state now reports the preview state, selects the active shot after crossing
  a boundary and avoids a stale global Space instruction. Extra shot labels are integral.

## Source And Artifact Evidence

SHA-256, current Desktop source/artifact bytes:

| Source / artifact | SHA-256 |
| --- | --- |
| `src/Smile.Compiler/MasmEmitter.cs` | `1D7D0B267E8183ABA6876052721B4F41381D280DE2F872E9F22FE88C9BCF6B93` |
| `tools/Character3DViewer/BattleCameraTimeline.smile` | `A1188859B189B4A6D7D47B186ABABC4151D3DF1DE5C56BB281BD7BE249CA8D83` |
| `tools/Character3DViewer/ViewerBeatTimeline.smile` | `7BE7B93E045A24202C56D37891B67FFF3B525A102F35686164C56985FF98EE13` |
| `tools/Character3DViewer/ViewerBeatEditor.smile` | `232042A14A654928BBBF358FE17CB21AF34BA03A9F6421B225D0F81BA72F90D1` |
| `tools/Character3DViewer/ViewerWorkflow.smile` | `113CB4347C4F62F7BDE927B3FF87365C67F1B675CA6412FAA56B9BEA1707C24E` |
| `tools/Character3DViewer/bin/Release/Character3DViewer.exe` | `59F43108F1C19F7059B88F8F3B2610AC0D0AED8F861B5D33AC53D6A9445179E7` |
| `tools/SmileStudio/bin/Release/SmileStudio.exe` | `CECB3476325C5C692324CE469F103DBAD696D2E7BEBEE916F110CB39C751522E` |
| `artifacts/compiler/smilec.dll` | `F01DF217C455A7FC17426737B4723077B8D75D03A2E37D94E5BF237C5FE50189` |
| `artifacts/vsix/Smile.VisualStudio.vsix` | `561660E2B7B47251CE67654A357AA2885CA1E8D510EBDC397867DBC60B9FD5CA` |

Calibration launch/export evidence preserves Arin's 24 keys (SHA-256
`7A3E7BC823CF544FA0136920A9D0752BF7DE585C0891B9073E688B1F783B3F67`)
and Orin's zero keys (`13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`).

## On Hold — Web Adoption Record

Resume only on Sin's direction. Shared source edits are not a published Web feature.
The existing Web artifact remains the initial editor from `fd73f9d` (game.js SHA-256
`4464498A9A1F36D0E5BFA160EF8BB3245E57D9AC1CD13563717697BECED12E60`).

1. Adopt the two modules and editor/workflow changes above through the existing
   shared project inventory; no separate Web camera/editor implementation.
2. Run the generated-Web fixture for camera-only timing, extra shots, Space, resets,
   V2 serialization, legacy fallback and unchanged action/pose restoration.
3. Rebuild/publish Viewer and the affected Studio shared consumer; preserve each
   origin's saves. Native stack probing has no Web code-generation dependency.
4. In Chrome check marker drag, insert/delete/navigation/repeat, Space, Save/Cancel,
   legacy and V2 reload, and the outstanding slow/moderate MMB orbit, pan, zoom and
   reset. Reuse unchanged numeric/rendering/assets evidence; no routine Edge pass.

## Next Action / Remaining

No remaining Desktop implementation or acceptance check for this bounded milestone.
Native gesture acceptance is recorded above. The remaining task is the explicitly
held Web adoption/validation record; resume only on Sin's direction. Reuse the current
native, numeric, asset and VSIX evidence. No unrelated feature phase is authorized.
