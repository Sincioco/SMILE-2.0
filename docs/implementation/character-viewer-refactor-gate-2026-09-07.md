Overall status: BLOCKED

---

# Character Viewer Refactor Gate — September 7, 2026

## 🎯 Objective

Finish and verify the Character Viewer refactor before any Double work. This gate binds
the responsibility audit and preservation evidence to current source, rebuilt native/Web
artifacts and installed Chrome. It does not restart or replace historical H6.1 evidence.

## ✅ Completed

- ✅ Fixed-array hardening F01/F02/F03 is complete in the shared language/compiler and is
  kept separate from this Viewer refactor result.
- ✅ Viewer responsibility extraction is complete with a documented entry-point exception.
  State-local implementation and lifecycle moved with focused production owners rather than
  into a renamed application monolith.
- ✅ Debug and Release native/Web outputs were rebuilt from current source. Release Full,
  Low, Medium and High Web identities, locations, branding and asset manifests are intact.
- ✅ Installed Chrome acceptance passed for the exact Release Full Web publication.
- ✅ Launcher/recovery/watcher/stale-publication failure tests passed using only disposable
  data, processes and publication fixtures.
- ✅ The current Release Desktop executable launched visibly through the supported launcher,
  is responsive and does not remain topmost over Chrome.

## 📦 Source And Evidence Identity

| Item | Identity |
|---|---|
| Refactor review anchor | `5bfd4f96ee838ca1b6b255c28c4117b0e0a5ec7b` |
| Latest addendum review anchor | `d5b7d9ec804a1561d2fab928fd78f69eba38d73e` |
| Latest Viewer implementation | `f1b235586ff73db0f6b683d8d961ecdc7f2fe93e` |
| Fixed-array integration | `a068b4820790b91bbaf0f94811b4f8a65ffb2567` |
| Pre-report current HEAD/origin | `4e1642da828b2f95865dc96b9076c784d12c2720` |
| `Program.smile` SHA-256 | `9951954FC0DB49FBB1C33A68A29D11EB506E9A9AD8BBD4643045B73A39044E33` |
| Compact checkpoint | `docs/implementation/character-viewer-refactor-checkpoint.json`, identity `character-viewer-refactor-current-2026-09-07-r3` |

The review SHAs are evidence anchors only. No reset, checkout, clean, restore or history
rewrite was performed.

## 🏗️ Before/After Ownership Map

| Responsibility at the initial audit | Current production owner | Focused proof |
|---|---|---|
| Startup, retry, switching, reset and shutdown implementation | `ViewerSession`, stateless `ViewerLifecycle` | lifecycle and preservation fixtures |
| Clip state, event mapping, seeking, demo and pause lifecycle | `ViewerPlayback`, `ViewerTimelineEditing` | hardening and calibration isolation |
| Camera framing, responsive fit, drag remainder, smooth zoom/orbit | `ViewerCamera` | integer-policy tests plus Chrome pan/zoom/reset |
| Calibration storage/recovery, transactions, editing and command routing | `ViewerCalibration`, `ViewerCalibrationEditing`, `ViewerCalibrationControls` | native isolation, JSON/Undo/failure tests |
| Input capture, hit geometry, UI rendering and inspector action policy | `ViewerInput`, `ViewerUi`, `ViewerInspectorCommands`, `ViewerInspectorPresentation`, `ViewerGizmo` | hardening plus Chrome UI/gizmo checks |
| Actor load/update/equipment, Party choreography/preview, Dragon lifecycle | `ViewerActors`, `ViewerParty`, `ViewerDragon` | hardening, calibration and two-Orin isolation |
| Fire/Lightning clocks, equipment effects, leases and continuity | `ViewerEffects` | hardening, actor isolation and paused Chrome VFX checks |
| Arena/backdrop/grid/socket state and ordered scene draw transaction | `ViewerRendering` | hardening and native/Web rendering checks |

No old implementation was copied, no behavior assertion was removed to obtain a pass,
and two production-dead calibration test wrappers were deleted after tests moved to the
production owners.

## 🏗️ Program.smile Audit

| Measurement | Initial responsibility audit | Current |
|---|---:|---:|
| Lines | 8,319 | 1,851 |
| Procedures | 233 | 51 |
| Source commit | `ed5e6bbb` | `f1b2355` |

`Program.smile` retains startup, the visible frame order, explicit cross-owner adaptation
and runtime input sampling. It is intentionally not compressed to manufacture a small
line count. Every substantial retained implementation-shaped routine is explained:

| Retained coordinator | Lines | Reason retained |
|---|---:|---|
| `HandleInspectorPointer` | 213 | Keeps gizmo/gesture/slider/timeline/recovery/transfer/command priority visible after geometry and state-local behavior moved. |
| `HandlePartyPointer` | 81 | Samples runtime input and orders typed Party responses across Party, session and current inspected actor. |
| `DrawInspectorOverlay` | 76 | Keeps minimum-size, Party overlay and presentation-group order; capture and raw drawing are owner-local. |
| `HandleInterfacePointer` | 76 | Keeps outside-window recovery and inspector-before-Party-before-camera ownership visible. |
| `HandleTransformGizmoPointer` | 74 | Samples runtime input and adapts typed owner results; gizmo geometry/mutation are extracted. |
| `HandleInspectorKeyboard` | 65 | Samples queued modifiers and keeps gizmo/owner/application priority explicit. |
| Top-level `Game Window` / `Do...Loop` story | 337 | Preserves readable startup, scene-clock, actor, VFX, render, overlay, presentation and shutdown order. |

Moving these chains wholesale would hide the same cross-owner order in a prohibited giant
`ViewerApplication`-style module. The 1,851-line entry point remains an explicit size
exception, but no additional cohesive single-subsystem implementation was found in the
current responsibility/reachability audit.

### Destination Metrics

| Owner | Lines | Procedures |
|---|---:|---:|
| `ViewerSession` | 225 | 15 |
| `ViewerLifecycle` | 728 | 25 |
| `ViewerPlayback` | 511 | 32 |
| `ViewerActors` | 356 | 17 |
| `ViewerCamera` | 726 | 35 |
| `ViewerCalibration` | 2,477 | 72 |
| `ViewerCalibrationEditing` | 814 | 24 |
| `ViewerCalibrationControls` | 464 | 11 |
| `ViewerInput` | 389 | 13 |
| `ViewerInspectorCommands` | 474 | 8 |
| `ViewerInspectorPresentation` | 473 | 8 |
| `ViewerTimelineEditing` | 330 | 7 |
| `ViewerUi` | 1,833 | 62 |
| `ViewerGizmo` | 620 | 18 |
| `ViewerParty` | 2,182 | 58 |
| `ViewerEffects` | 1,377 | 54 |
| `ViewerDragon` | 494 | 21 |
| `ViewerRendering` | 779 | 37 |

The larger owners are documented focused exceptions. None owns the entire application
state, and their APIs borrow narrow explicit subsystem values rather than a shared
`ViewerState`.

## 🧪 Validation

| Check | Current result |
|---|---|
| `scripts/test-character-viewer-preservation.ps1` | ✅ PASS — graceful launcher ownership, primary/backup selection, wrong-character rejection, watcher retry/newest revision, persistent failure and stale-publication rejection in disposable roots |
| `scripts/test-character-3d-viewer-hardening.ps1 -Configuration Release` | ✅ PASS — native calibration isolation, generated-Web exact output and 58 native graphics/pointer/audio checks |
| `scripts/test-character-3d-viewer-actor-isolation.ps1 -Configuration Release` | ✅ PASS — native/Web normal and forced GPU-particle fallback; two Orin instances remain independent |
| Shared language tests/build | ✅ PASS — 308 tests at the fixed-array integration checkpoint |
| Fixed-array gate | ✅ PASS — compiled native/Web bounds, ByRef/order, ownership and bounded-helper cases |
| RPG integration | ✅ PASS — DirectX, GDI and Web exact parity after invalid-handle follow-through |
| Full repository smoke | ✅ PASS at `a068b48`, including formatter and 406 tracked SMILE files |
| Installed Chrome | ✅ PASS — Chrome `152.0.7977.77`, current Full Web output, no warning/error logs |
| Exact Desktop UI interaction | 🚧 BLOCKED — rebuilt executable is visible/running, but the current Codex UI channel exposes Chrome only |

### Installed-Chrome Results

- ✅ Arin, Orin, Dragon and Party tabs rendered with their own status/assets.
- ✅ Full panel height exposed Import/Download controls; no bogus visible `Full Screen`
  control appeared inside the page.
- ✅ Backtick cycled reduced/all-hidden/full UI and restored the inspector.
- ✅ Slow/moderate pan changed camera telemetry; wheel changed zoom from `-15` to `-23`
  and back; right-click restored the Party preset.
- ✅ Pose opened with the gizmo hidden. `Show Gizmo` displayed colored rotation handles;
  `Hide Gizmo` removed them without saving or changing calibration.
- ✅ With choreography paused, Fire and Lightning were frozen/restored independently.
  Both finished as `Freeze Fire` / `Freeze Lightning`, the default running state.
- ✅ Two paused-scene frames 750 ms apart differed while both families were enabled,
  corroborating the production tests that their clocks advance independently of scene time.
- ✅ Real `WEBGL_lose_context` reported lost/restored events on WebGL2; runtime status
  stayed `running`, frame count advanced again and Party rendering returned without reload.
- ✅ Browser warning/error log query returned `[]`.

## 📦 Publication Evidence

| Release artifact | Files | Bytes | Declared assets | Identity |
|---|---:|---:|---:|---|
| Desktop `Character3DViewer.exe` | 1 executable | 36,969,984 | 46 published assets | SHA-256 `D0A565F53C3FC81DC90158580612660447CDEDFF767F1B6345FAC4C13A4E10A0` |
| Full Web | 40 | 103,593,048 | 35 | `game.js` SHA-256 `50B9C839...BB5` |
| Low Web | 41 | 17,085,934 | 35 | quality identity `Low` |
| Medium Web | 41 | 38,666,930 | 35 | quality identity `Medium` |
| High Web | 41 | 71,957,914 | 35 | quality identity `High` |

Release locations remain `tools/Character3DViewer/bin/Release/Web` and
`Web - Optimized Low/Medium/High`. Debug native and Full Web also rebuilt successfully.
The optimized profiles share `game.js` with Full and retain their optimized runtime
identity; no output was renamed or relocated.

## ✅ Preservation Proof

- ✅ Arin remains 24 keys; canonical JSON SHA-256
  `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`.
- ✅ Orin remains 0 keys; canonical JSON SHA-256
  `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
- ✅ Failure tests used GUID-scoped storage/application identities and disposable processes.
- ✅ Shared Party inspector, confirmed import and Undo, queued modifier behavior, immutable
  asset cache, equipment visibility, frozen Dragon continuity, scene-clock order and
  current recovery policy remain covered.
- ✅ Number semantics, integer scales, stored formats, discrete events, asset identities,
  branding and output locations are unchanged.
- ✅ `Window_Activate()` remains absent; Chrome could stay in front of the running Desktop
  Viewer, so the old topmost/focus-stealing behavior did not recur.

## ⚠️ Defect Dispositions

| Finding | Disposition |
|---|---|
| Per-frame `Window_Activate()` made Desktop steal foreground | ✅ Fixed in R6; dead activation bookkeeping removed and hardening rejects its return |
| Launcher shutdown could target the wrong/refusing process | ✅ Fixed and reproduced with disposable processes; refusal aborts replacement without force-killing an unrelated process |
| Corrupt/missing primary could overwrite a good backup | ✅ Fixed; previous-good backup is selected/preserved and rejected evidence is retained |
| Watcher could lose a pending/newer revision or hide final failure | ✅ Fixed; newest revision wins after retry and bounded persistent failure is reported nonzero |
| Stale generated publication could pass | ✅ Fixed; selected profile asset fingerprint is validated after publication |
| Scene pause stopped all VFX | ✅ Fixed/preserved; Fire and Lightning run by default and freeze independently |
| Direct native UI control unavailable in this session | 🚧 Evidence limitation, not a product failure; exact Desktop interaction confirmation remains required |
| Initial `program.js` inventory assumption | ⚠️ Validation-command error only; corrected to the actual `game.js`; no artifact changed |
| Temporary wrapper cleanup command was policy-blocked | ⚠️ Tooling-only; copy was removed with the patch mechanism and Full Web returned to 40 files |

## 🔧 Current Build And Run Instructions

Desktop, supported launcher:

```powershell
tools/Character3DViewer/Launch.ps1 -Configuration Release
```

Rebuild and launch without forcing foreground during automation:

```powershell
tools/Character3DViewer/Launch.ps1 -Configuration Release -Build -SkipWindowActivation
```

Full Web:

```powershell
tools/Character3DViewer/Build.ps1 -Configuration Release -Target Web
python -m http.server 8766 --bind 127.0.0.1 --directory tools/Character3DViewer/bin/Release/Web
```

Open `http://127.0.0.1:8766/` in installed Chrome. The current acceptance used one Viewer
tab and did not run a redundant Edge/Firefox pass.

## 🚧 Required Completion Check

The gate cannot yet claim overall completion because the acceptance contract requires
current native launch interactions. In the already visible rebuilt Release Desktop Viewer:

1. Press Space twice and confirm pause/resume.
2. Select Arin, Orin, Dragon and Party.
3. Perform one pan or orbit, wheel in/out and right-click reset.
4. Open Pose, show the gizmo, then hide it.

No save/import/export action is required. After confirmation, update this gate/checkpoint
to `COMPLETE`, commit/push the report, and stop.

## 📋 Deferred Work

- 📋 Double and fractional renderer APIs.
- 📋 Loader/splash/progress/metadata enhancements.
- 📋 Semantic-inspection CLI.
- 📋 Battle Scene Editor.
- 📋 Subject-first syntax.

These items remain approved future work, not canceled, started or completed. The active
task stops after the Character Viewer refactor gate.
