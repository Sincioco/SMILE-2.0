# Party Beat Camera Editor — Desktop Checkpoint

Status (2026-09-11): the initial editor is published in `fd73f9d`; Desktop-first
scope was recorded in `26bb3a9`. The current Desktop timeline/reset enhancement is
implemented, built and validated with focused automated, visible tool and explicit
human gesture checks. The commit containing this checkpoint is the Desktop delivery
milestone. Web follow-through remains on hold.

September 13 scope update: Sin halted **all Web work**, including Mira publication,
water effects/Lab, browser checks and this timeline follow-through. Desktop is the
active target. On resumption, align the Web SM3D loader's triangle-area validation
with the cooker's/native loader's scale-independent check before publishing Tripo
Mira; its former absolute area cutoff rejects valid small details. Preserve the
outstanding Chrome camera-gesture acceptance alongside these adoption steps.

Sin also placed Studio creation, development, acceptance and next phases on hold.
The existing Studio project may retain shared source inventory, but it is not a
validation target or an authorized next phase for the native hardening below.

## Native Review Hardening (2026-09-13)

The review baseline was `c5a348c3abda773c62158fb23b82ab2b5267be37`.
Current code was reconciled without resetting to that commit. The five demonstrated
native findings are repaired in their existing owners:

- Linked cameras interpolate target, distance and a unit viewing direction over a
  deterministic great-circle path. Midpoint, near-midpoint, opposing-view and
  parallel-up cases now submit a valid native basis while exact endpoints remain.
- `ViewerBeatHeadPersistence` owns per-character current/persisted head values and
  pending/failure recovery. Camera Save/Cancel cannot erase the warning; Retry and
  Discard affect only that identity, and native X/Alt+F4 is deferred while recovery
  is unresolved.
- Head picking and dragging always use raw logical viewport coordinates. Inspector
  scrolling is applied only to visible panel controls, so a stationary held pointer
  produces zero drag delta at both zero and nonzero scroll.
- `BattleCameraTimeline.EffectiveValid` checks final rounded main/extra marker times,
  boundary and neighbor spacing, uniqueness and reachability against current action
  timing. Insert/move/resize/reset are transactional; an incompatible stored schedule
  stays on disk while playback uses the four safe main markers.
- Native compiler startup now emits accepted support/module scalar Class initializers
  exactly once, with provider/import dependencies before consumers and declaration
  order retained. Entry/local and explicit construction retain their existing paths;
  Sin Star I still constructs its Viewer session explicitly inside `Enter`.

Focused native acceptance passed: the NativeOnly Viewer gate completed 42 isolated
Arin calibration checks and 58 native graphics/pointer/audio checks; calibration
isolation preserved Arin's 24 keys and Orin's zero-key snapshot and did not touch live
storage. Local and package-backed module initializers, explicit/entry/local creation,
repeated access, failure cleanup and zero Class/Text lifetime counts passed. All 323
shared language/compiler/project tests, 46 native Text checks, 13 formatter groups and
the 462-file style gate passed. Seven Sin Star I character entries and both battle
simulations created, drew and released their actual assets. Release builds published
138 Viewer assets and 211 Sin Star I assets.

The compiler and VSIX were rebuilt at 2.0.64. `smilec.exe` SHA-256 is
`9B00239D957EF47EC6F2FBFF6089A09927AA875AA85379BDDF6B6052F40EF284`;
the VSIX SHA-256 is
`AF59896C786E92436BFF78E139C4322C8CE6D01B36908B3E0AEE019EC064229E`.
Installation is deferred because Visual Studio is actively debugging the unrelated
PMT solution; the repository installer correctly refuses a live instance. The
installed extension remains 2.0.63 until Visual Studio is closed and 2.0.64 is
installed. No held Viewer/Sin Star/Studio Web build, publication or browser
validation was performed, and no Studio build or acceptance work was resumed.

Growth review against the reviewed commit: `BattleCameraShots.smile` is 657 lines
(+115), `BattleCameraTimeline.smile` is 579 (+138), `ViewerBeatTimeline.smile` is
299 (+5), and the legacy `ViewerBeatEditor.smile` is 1,487 (+193). The new focused
head-persistence owner is 237 lines. `MasmEmitter.cs` is 3,415 lines (+50) and its
change stays at native startup emission. The camera math remains in its existing shot
owner as requested; persistent head state is not added to the editor coordinator.
No architecture threshold, baseline, exclusion, dependency or framework was changed.

## Imported Character Profile Alignment (2026-09-11)

The Head attachment added for beat framing is included in the standalone profile
inventories: Valor 15, Zara 11 and Vrax 5 sockets. Stale pre-Head counts caused the
reported Zara/Vrax recovery screen at validation stage 32; the profile correction
also covers Valor. Exact loaded-asset validation remains enabled. Canonical models,
descriptors, animation, VFX and saved camera/pose data are unchanged.

The native calibration fixture now switches through the three real imported tabs,
checks each Head socket, rejects a deliberately stale inventory and reloads Arin.
It passed along with the existing isolated calibration/persistence checks. Focused
formatting checks and Release Viewer/Studio builds passed. Desktop tool observations
confirmed Zara with her Vrax opponent, standalone Vrax and Party Vrax rendering.
The launcher preserved Arin's 24 saved pose keys and Orin's zero-key snapshot.

Artifact SHA-256: Viewer `B25F9075EFC405FD0AF7B396235D95454533B9128294D7FDF0307BB9DCF37DDE`;
Studio `FDA0854F28AA33AB74C453A113C51A608585F6E9780C3A03ADA2E04DFF2BDFD2`.
Web remains unbuilt/unpublished for this slice; its next authorized adoption must
include the corrected shared profiles together with the pending timeline changes.

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

- During Beat Preview/Edit, Beat Sequence replaces the regular Animation Timeline in
  its bottom layout with the same controls and colors and no enclosing panel. Closing
  Preview restores the Animation Timeline. Controls are 0-Frame, Previous/Next Beat
  and Previous/Next Frame; held frame buttons repeat.
- Four ordered main markers cannot be deleted. Dragging boundaries 2–4 changes
  adjacent camera durations within the fixed battle duration. Up to sixteen extra
  shots can be inserted, moved across main beats and deleted. Navigation visits all
  markers chronologically; motion/interpolation uses the current shot interval.
- **Camera timing only.** Sin explicitly reversed the earlier action-retiming request.
  Movement, animation, impacts, audio/VFX cues and gameplay counters retain the
  original action schedule. Default/unauthored cameras retain the existing battle
  policy. Authored camera intervals follow the edited camera schedule.
- Pan, orbit, zoom and keyboard orbit remain available after scrubbing; editing resumes
  from the displayed camera. The fixed Camera panel targets the active Beat camera and
  exposes View, Present, All and Fit Beat actions. Space plays/pauses the preview
  playhead. It stops at the end; another Space restarts from zero. Pausing selects the
  current camera marker. Right-click exits the editor and resets the current Party tab.
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
- Save writes the complete character sequence atomically and keeps the Beat timeline,
  selected shot, playhead and camera-editing state open. Cancel or closing Preview
  restores the prior battle. Head cuboids auto-save separately.

## Owners And Compatibility

| Owner | Responsibility |
| --- | --- |
| `BattleCameraShots.smile` | Double relative framing, shot motion/link evaluation, bounded scalar/text and independent head storage |
| `BattleCameraTimeline.smile` | Four camera weights, main/extra markers, ordering, resize and versioned sequence persistence |
| `ViewerBeatTimeline.smile` | Pointer gestures, navigation/frame repeat and bottom replacement timeline presentation |
| `ViewerBeatHeadPersistence.smile` | Per-character head current/persisted values, pending/failure state, retry and identity-scoped discard |
| `ViewerBeatEditor.smile` | Selection, per-character drafts/clipboard, Space preview, camera editing, staged resets and head controls |
| `ViewerBeatSequence.smile` | Existing actor/timing adapter, original action sampling, bookmark/pose restoration and head/body picking |
| `ViewerWorkflow.Session` | Existing input/update/draw coordination, discontinuous versus continuous visual history |
| `MasmEmitter.AllocateStack` | Shared native allocation guard-page probing for large local/call frames |
| `MasmEmitter.EmitSupportClassInitializers` | Native once-only dependency/source/declaration startup order for accepted support/module Class initializers |

`BattleCamera.Sequence.<Character>.V2` stores the timing weights and up to twenty
shots in one checksummed Save Data envelope (`SMILE-Sequence-2`). A valid V2 sequence
wins, including an intentionally saved reset with no authored cameras. Without it,
legacy `BattleCamera.<Character>.Beat1` through `Beat4` records are read; they are not
deleted or overwritten. Head keys and pose JSON remain unchanged. Native and each
Web origin own independent saves; camera import/export is outside this slice.

Default drafts retain a separate `UseDefault` flag so displaying a captured editor
view cannot turn a reset into a saved static camera. An actual camera edit adopts
that view. Save failure keeps the draft; Cancel never writes a sequence.

The Viewer project/build and hardening fixture list the current timeline and head
persistence owners. Studio's project retains those same dependencies because its
existing host imports the Viewer; this compatibility inventory is not a new Studio
phase or acceptance check. Program.smile's
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
- The focused Beat Editor save regression writes through the isolated test
  ApplicationId and verifies that Preview, edit mode, selected shot and playhead stay
  in place while the dirty flag clears. The rebuilt release Viewer launched normally;
  live user camera records were not changed for this smoke check.

Unchanged evidence from the initial editor remains valid: `fd73f9d` native/generated-
Web fractional framing, linked shots, copy/paste, head storage, action sampling and
exact bookmark restoration. Its Chrome selection, Save/reload, head resize, paste/
Cancel, Vrax/Dragon scrub, pan/reset observations are not new timeline validation.
See Git history for the original detailed checkpoint; do not repeat unaffected tests.

## Reproduced Defects Fixed In This Milestone

- Save previously cleared Preview/edit state in `ViewerBeatEditor.SaveShot`, after
  which `ViewerWorkflow.HandleBeatPointer` immediately restored the prior battle and
  closed the timeline. Save now performs persistence and saved-copy/dirty-state
  bookkeeping only. Cancel or explicitly closing Preview remains responsible for
  leaving Beat Editor and restoring battle state.
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
| `tools/Character3DViewer/ViewerBeatTimeline.smile` | `4EADC670B23C1EB0E6A563BAA0485D51A1296D6DAD177BA0F81695EE7A1426AF` |
| `tools/Character3DViewer/ViewerBeatEditor.smile` | `11ED8D2F86610B2BDEB0E0CB130057CE148027D1DAF557E5EB451AFE371E0960` |
| `tools/Character3DViewer/ViewerWorkflow.smile` | `B8A6959D138C74C6855A77593545031D1E90FFC7D7962DFF091F668514075B4B` |
| `tools/Character3DViewer/ViewerInspectorPresentation.smile` | `D22A3C101FCDF1B9282965ACEAD69CCD1FC01F54BDD7A5DAD0C3DBAD7F40AA90` |
| `tools/Character3DViewer/ViewerUi.smile` | `DCC4F98CEC9FE92D35B224E9CAD74A3E1BA4F67220B23A55A990CBBC205BF217` |
| `tools/Character3DViewer/BeatCameraTests.smile` | `E489CD2835F1188E716334AAFD1F097351EE93806716E77230250418DE2C3DBA` |
| `tools/Character3DViewer/HardeningTests.smile` | `C81FBEE28B932BDE5571CCDA8C20E5A0A1DE2B55C5D300DD34E3A0ADFE5A6D55` |
| `tools/Character3DViewer/bin/Release/Character3DViewer.exe` | `BB4302198CC13611218C419519D85FFB0189870D5120D02D3F64245E52A19C39` |
| `tools/SmileStudio/bin/Release/SmileStudio.exe` | `FDA0854F28AA33AB74C453A113C51A608585F6E9780C3A03ADA2E04DFF2BDFD2` |
| `artifacts/compiler/smilec.dll` | `1638C9E67E3796931A1AD251E19EFEB757E9C8DB508A3C2D2909BD4890E0E450` |
| `artifacts/vsix/Smile.VisualStudio.vsix` | `2537FD71D1FF9CE29D5589A074D12116582357A971B49E3BBA16C26C494EC438` |

Calibration launch/export evidence preserves Arin's 24 keys (SHA-256
`7A3E7BC823CF544FA0136920A9D0752BF7DE585C0891B9073E688B1F783B3F67`)
and Orin's zero keys (`13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`).

## Additional Desktop Viewer Controls — September 11, 2026

- The native repeat loader used during tab changes is centered over the retained
  Viewer window, clamped to the current monitor and rendered at 80-percent opacity.
- Camera controls now occupy one dedicated panel in their original lower-right
  location on all character and Party tabs. H Orbit, V Orbit, Zoom, View, Present,
  All and Fit remain together as one fixed control group.
- Right-side inspector and Beat Editor content above Camera scrolls vertically with
  the mouse wheel when it overflows. A thin scrollbar appears only while that panel
  is hovered and hides when the pointer leaves. Party input bounds stop above Camera,
  so every Camera slider and button remains interactive.
- Beat Edit now routes H Orbit, V Orbit, Zoom, View, Present, All and Fit to the active
  Beat camera. Beat Sequence occupies the normal bottom timeline position while
  Preview is open, and the animation timeline returns when Preview closes. Right-click
  closes Beat Edit and runs the current Party tab reset.
- Every Viewer panel background, including Beat Editor and the small-screen overlay,
  uses 80-percent opacity through the shared panel presentation.
- Arin, Orin, Valor and Zara now have independent 0-200 percent Weapon and Shield
  strength sliders in Character Status, using the shared H Orbit slider interaction.
- The native release Viewer rebuilt successfully and passed all 58 hardening checks.
  A 1600-by-640 live interaction exercised every Camera slider and button on Party
  Vrax, then verified inspector wheel scrolling and the hover-only scrollbar on Arin.
  The full repository build passed and VSIX 2.0.63 was reinstalled with all 35 payload
  hashes matching. The complete startup contract remains passed from the loader change.
- The native capture helper now waits for the loaded editor window instead of retaining
  the temporary startup-splash handle.
- The Beat Edit follow-through rebuilt the native Viewer and repeated the 58-check
  NativeOnly hardening gate. Regression coverage now verifies Camera panel coordinate
  actions, active Beat camera slider routing, View/Present state changes, replacement
  timeline coordinates, animation-timeline visibility, right-click current-tab reset
  routing and 80-percent panel ownership. The 13 formatter tests and the 444-file
  repository style check also pass.

## On Hold — Studio And Web Adoption Record

Resume either workstream only on Sin's direction. Studio creation, development,
acceptance and next phases remain held. Shared source edits are not a published Web feature.
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
5. Adopt and verify the shared Camera panel and four character strength-slider changes
   when this held Web work resumes. No Web artifact was rebuilt or published for the
   additional Desktop Viewer controls above.

## Next Action / Remaining

No remaining native implementation or acceptance check exists for this bounded
hardening milestone. Install the already-built VSIX 2.0.64 only after the unrelated
Visual Studio debugging session is closed. Studio and Web remain explicitly held;
resume only on Sin's direction. No unrelated feature phase is authorized.
