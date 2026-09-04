# M7E-G0: Approved Viewer and Thermal-Fire Hardening

Status: **G0 passed, committed, pushed and remote-verified.** Implementation commit:
`fa81d737c8de43663501067aeacb2d9ac0c03cab`; local HEAD and origin/main matched,
ahead/behind 0/0. Five unrelated untracked Tank reference PNGs were preserved.

## Updated scope: Free roam deferred

Sin subsequently deferred the separate free-roaming flaming-sword demo because
the working Character Viewer already provides the desired visual proof. G1–G5
are not started and are not claimed complete. Do not automatically resume that
follow-up package; a later user request should select the next milestone.
Arin's approved Viewer setup and all 23 saved keys remain unchanged. Future
dragon fire and other characters' ice/magic effects remain shared-resource
requirements, not authorization to implement them in this completed G0 task.

## Intake and preservation

- Repository: `D:\SMILE 2.0`, branch `main`.
- Actual starting HEAD and fetched `origin/main`: `de0fb926ed000daebb68f4efe2abe0706fbf4ac5`;
  ahead/behind 0/0. Only the user's calibration JSON was modified.
- Requested pre-work checkpoint: `a7fb138189e407f81f6dd096c0fd79738edb7ac4`;
  pushed and confirmed at the remote. It preserved eight keys, including
  SwordAttack frame 38 and Walk frame 0. The user is continuing pose refinement;
  subsequent saved values take precedence over the planning snapshot.
- Latest thermal-fire implementation commit:
  `8273d56c3942002f38e28f5bdcaef69cecb44c7a`.
- Planning baseline `67898926e0da7e93ccb78a7186b4c5e2b5a00dd3` is older.
  Its nine-key assumption must not restore a removed Walk frame or replace newer
  attack corrections. Historical nine-key JSON is preserved in Git and an
  ignored ZIP under `artifacts/temp/m7e-g0-backup`.
- The first current pre-migration JSON backup SHA-256 is
  `7B3E98AAD25FDAD45D000E84171E00DA30E6982B6BEE36BC4C13E2F51FC5913D`.
  Later live saves are not frozen by this backup.
- Model, descriptor, cooked SM3D hashes, exact clip sample bounds, and thirteen
  sockets are recorded in the canonical package's `Calibration/arin-v5.7-profile.json`.
- Initial graphics command range is 1–132; VSIX source version is 2.0.59.
  Initial native GPU capacity was eight systems, 32,768 total slots and 16,384
  slots per hardware system (8,192 CPU-reference slots per system).

Root `AGENTS.md` is authoritative. The package's contradictory SMILE 1.0 phrase
does not authorize compatibility work. Existing SMILE 2.0 behavior and ABI are
preserved. No model/rig/animation-source/texture repair is included.

## Instruction packages

Both ZIPs were found in the user's Downloads folder, sorted by modification time,
and path-validated before Markdown-only extraction under ignored
`artifacts/temp/codex-handoff`. No downloaded executable was run.

- Hardening ZIP SHA-256:
  `756467BFE0C4D8CE4B09AA752FCCF7324B6CBE69F216F567D4BC58BBB8CB231E`.
  All numbered documents read in order; ten manifest lengths/hashes verified.
- Follow-up ZIP SHA-256:
  `C9DD2F71DAFDEF2C68FFFBD532EA322B924BAA2CDB2667BE0462AF3729D7B865`.
  Safely extracted; detailed execution intentionally deferred until G0 passes.

## Baseline evidence

- `scripts/test-character-3d-viewer-hardening.ps1`: **failed**, obsolete assertion
  `Const IDLE_RESET_MILLISECONDS = 10000`. Log:
  `artifacts/temp/m7e-g0-baseline-viewer.log`.
- `scripts/test-native-thermal-fire.ps1 -SkipBuild`: passed native thermal/GPU
  recovery, Fire Lab build, native emitter execution and exact Web console parity.
  Log: `artifacts/temp/m7e-g0-baseline-fire.log`. This is a baseline using the
  existing compiler, not final rebuilt evidence.

## Implemented hardening

### Calibration identity and preservation

- Canonical schema 2, binary payload 3, profile fingerprint
  `f554574e00ec4a9f9c59eac1b3d4adaec9649c90972cc5f0dc6c92859568ba5d`.
- Exact clip names bind tracks; runtime indices are recomputed hints. Unknown
  clip tracks survive canonical normalization but are not applied to another clip.
- Strict integer/vector/Boolean/bounds/count/duplicate/profile validation.
  Per-key state contains wrist rotation XYZ, equipment rotation and movement
  XYZ, and both decoupling flags: 20 channels, not sparse deltas.
- Version-2 legacy data migrates transparently; version 1 requires an explicit
  migration option. Wrong identity, malformed JSON or binary checksums, and
  unsafe paths are rejected without overwriting a good snapshot.
- Native Save Data and PowerShell synchronization use flushed temporary files,
  atomic replacement, previous-good backups, and exact expected-hash checks for
  concurrent writes. Shared reads permit replacement while the watcher runs.
- Validate, Compare, Backup, Restore, Export, Import and Watch modes are available.
  The Viewer has one-level Undo Last Change, including first-ever save and
  selected-clip deletion. No live user key was deleted during acceptance.
- Current saved clip/frame mapping: BlockImpact 0; Defend 0; Hit 0; Idle 0;
  Run 0; SwordAttack 6, 9, 11, 16, 19, 21, 28, 30, 32, 33, 34, 35, 38;
  SwordAttack2 0, 10, 14, 17; Walk 0. Total: **23**.

### Pose, equipment and sockets

- Animation sampling, wrist correction, coupled/decoupled equipment correction,
  world transform, and VFX source sampling remain separate existing stages.
- Socket debug markers now use the same corrected equipment object as their
  flame source: SwordBase/Tip use the sword; shield fire anchors use the shield;
  anatomical sockets use the body. This changes inspection, not model sockets
  or the accepted sword/shield transforms.
- In-place rotation references the edit's stable original grip baseline.
  Six outward/inverse 45-degree cycles pass the native sub-unit drift check.
- Viewer clip selection is an explicit **cut**, not a claimed corrected-pose
  cross-fade. Saved channels interpolate within a clip; a lone key is held.
  Automatic demo boundaries retain natural world-space flame tails. Explicit
  navigation clears/reseeds them. Other profiles clear v5.7-specific state.

### Fire lifecycle

- A visual continuity epoch invalidates source history on seek, step, key jump,
  retime, edit, save/reload/reset, profile change and explicit clip selection.
  Teleport-like segment discontinuities also suppress inherited velocity.
- Scene pause and flame pause are independent. No elapsed-time catch-up is
  accumulated while flames are paused. A paused seek reseeds without advancing
  particle age; resume advances only the current elapsed slice.
- Continuous clip loops and automatic demo advancement keep existing tails.
  Source velocity is zeroed on the first sample after discontinuity.
- Optional equipment-fire failures retain the first error, destroy only the
  affected optional emitter resources, and leave the character renderer usable.
  Device recovery is covered by the retained native thermal recovery gate.

### Bounded shared resource admission

The chosen bounded alternative preserves the accepted five-layer appearance;
it does **not** implement shared simulation/render views or claim fewer systems
per effect. It raises the existing resource table limits without increasing the
global particle-slot ceiling. Generic resource exhaustion exercises shared ice/
magic pressure; no ice/magic preset is implemented in G0.

| Resource | Before | After |
| --- | ---: | ---: |
| Global GPU systems | 8 | 32 |
| Total GPU particle slots | 32,768 | 32,768 |
| CPU particle batches | 16 | 32 |
| Fire emitter handles | 4 | 6 |
| High emitter | 5 systems / 1,664 slots | unchanged |
| Medium emitter | 5 systems / 832 slots | unchanged |
| Complete CPU fallback | 4 batches / 384 slots | unchanged |

- Preflight all five GPU systems and all required slots before allocation;
  roll back partial failures. Try High, then Medium, then complete CPU fallback.
- Distinct reasons report unavailable backend, system pressure, slot pressure,
  or creation failure. Existing query/command IDs are retained; named query 60
  exposes the existing total-capacity ceiling. No new SMILE syntax is needed.
- Concurrent proof: High sword + Medium impact + two Medium torches + High
  dragon breath = **25 systems / 5,824 slots**, no unintended CPU fallback.
  Arin's existing sword and three shield edge emitters need 20 systems.
- Test 28 occupied systems, full 32,768-slot pressure, and exhausted CPU batches:
  fallback is complete or the effect is rejected; partial resources do not leak.
- Replace the fixed world box with conservative current/recent segment bounds,
  including radius, preset velocity, inherited motion and bounded tail history.
  Validate internal indices, divisors and source ranges before use.
- Actual queries/bytes/dispatches/draws are captured in the diagnostic evidence.
  GPU timings are unavailable; CPU submission/presentation observations in the
  retained native thermal test are not represented as Viewer GPU timings.

## Validation and evidence

- `scripts/test-arin-calibration.ps1`: 42 focused schema, migration, atomic I/O,
  backup/restore and preservation checks.
- `scripts/test-viewer-calibration-native.ps1`: runs actual Viewer procedures
  under an isolated application identity. Covers full-channel save/undo,
  in-place rotation, socket ownership, lifecycle generations, pause/seek/reset,
  and native Save Data plus its checksummed previous-good backup.
- `scripts/test-character-3d-viewer-hardening.ps1`: replaces obsolete automatic
  idle-reset assertions with explicit pause/reset behavior; retains camera,
  input, native and shared Web evidence.
- `scripts/test-native-thermal-fire.ps1 -SkipBuild`: native dynamics/recovery,
  capacity/fallback, exact Web console parity and Fire Lab compilation.
- `scripts/test-renderer3d-gpu-particle-common.ps1`: native/Web 32-system limit,
  total-slot query, generation and teardown checks.
- Full `scripts/smoke-test.cmd`: formatter, 295 language/compiler tests, native
  and Web graphics/soft-depth/distortion/GPU gates, games and final artifact
  verification. The final frozen-source run reached its final artifact gate
  with exit code 0. The Viewer, thermal, VFX-hardening/Effects3D and Character3D
  wrappers also completed with exit code 0 after that frozen-source run.
- Final logs: `artifacts/temp/m7e-g0-final-smoke.log`,
  `m7e-g0-final-viewer.log`, `m7e-g0-final-fire.log`; concise checked evidence will
  be retained with this report. Earlier baseline/red iteration logs are not
  substituted for the final run.
- Live inspection covers every requested pose screenshot, Walk frame 19 as a
  held correction (not a restored historical key), full profile cycle, socket
  origins, independent flame pause, timeline wheel/seek and right-click reset.
- Source-controlled evidence lives in
  `screenshots/m7e-g0-approved-viewer-fire-hardening/`; see its screenshot index.

### Model production gate

`scripts/audit-model-topology.py` imports the accepted GLB read-only in Blender
5.2.1 LTS and reports raw seams plus temporary position-welded topology. The
report flags **670 remaining boundary edges**, non-manifold/open regions,
degenerate faces, winding and skin-group review hints. Some openings are
intentional armor boundaries; the count is not a count of visible defects.
Robust self-intersection classification is explicitly unimplemented.

The canonical `Diagnostics/model-quality.json` and package manifest mark the
character **development allowed / production and release blocked**. No GLB,
descriptor, Blender source, rig, animation source, texture, or skin weight was
changed. The diagnostic PNG is a Blender unskinned boundary visualization,
not a native Viewer capture or a proposed repaired model.

### VSIX

`scripts/install-vsix.cmd` rebuilt and installed **2.0.59**, then verified the
installed assembly against the built DLL. Verified assembly SHA-256:
`461E2CB28B5BEA8DED16CAD4D32B4AF069821A04BAF45F9D8DEEED0ED7EDDF26`.
Package: `artifacts/vsix/Smile.VisualStudio.vsix`. Restart Visual Studio to load
the refreshed payload. No Visual Studio process was open at installation start.

After the final smoke rebuild, installation was refreshed once more and the
bundled compiler/runtime payloads were checked too. Built and installed hashes
match exactly in `Extensions/515cktca.pjy/Compiler`:

| Payload | Built and installed SHA-256 |
| --- | --- |
| `smilec.dll` | `C1636B2526970A335F4C34D4E5215704D7CC8CA31A6176FB50AFD6B719D9F743` |
| `Smile.NativeRuntime.lib` | `070F70C52A399A96C08515784A722CD8ED0EABF2EE1112030E9957ED100E8386` |
| `Smile.Language.dll` | `C7DABCD87E09483CACEF82DABAB9A541834CB8E580D4386204CFC7F4B5AD7AC5` |

Final install log: `artifacts/temp/m7e-g0-final-vsix-install.log`, exit code 0.
The native archive hash changes when rebuilt; no implementation source changed
between the green final gates and this installation refresh.

### Evidence and release handoff

All fourteen PNGs, the phone contact sheet, screenshot index and machine-readable
`arin-v5-7-calibration-validation.json` are included. Desktop capture returned
JPEG pixels, which were decoded and transcoded to actual PNG files, not merely
renamed. The index distinguishes native Viewer captures, a native resource-query
fixture, Blender diagnostics and the derived contact sheet.

`m7e-g0-validation-results.json` records exact final gate commands and log hashes.
The multi-Markdown delivery ZIP contains Start Here, repository-relative reports,
the canonical calibration/profile, diagnostic report, images and checked logs.
Only after the containing G0 commit is remote-verified may G1–G5 begin.

Web Viewer/editor visual parity remains deferred; shared Web regressions pass.
The new hardening visuals still benefit from Sin's manual review. The legacy
Viewer performs deliberate clip cuts, not corrected-pose cross-fades. No
free-roam, swept-trail, new character, or geometry-repair work is included here.

### Bounded live preview, 2026-09-05

Known problem being investigated: prior Viewer crashes, stale equipment trails,
and fire resource/lifecycle instability during repeated animation transitions.
Why the longer test is necessary: the supplied G0 package explicitly requires
a continuous ten-minute preview across full loops and automatic clip changes.
Stop condition: 600 seconds of observed native playback, or the first failure.
This is a one-time acceptance check, not a new permanent soak suite.

The user saved and authorized freezing the current 23 keys. The schema-1
pre-migration JSON backup hash is
`322C81349C1C08B8151F9DDA8ADB4ECFE7A200F5F8E0E143A13783B7F02267BA`.
The schema-2/storage-3 canonical hash is
`6FE2268E390D228AF4F52AF85E5358B66ACF8DE606D60C514FAC6CA0CF8B51B1`.
All 20 per-key channels compare exactly after normalization. The historical
nine-key planning payload is superseded by these later user saves.

Result: **passed**, 618.671 seconds from `2026-09-04T16:32:07.731Z` through
`2026-09-04T16:42:26.402Z`. The actual native client was approximately 1418×652
(captured window 1420×683). Demo cycled all eight clips with sword and shield
fire. No crash/recovery overlay was observed; final effect error was zero.
The live Viewer PID remained 71660 throughout. Foreground changed briefly for
the required regression windows and resource diagnostic; playback continued.

Observed samples around this interval: private memory 530,341,888–531,349,504
bytes, working set 229,056,512–230,436,864 bytes, handles 717–719. Final on-screen
FPS was 114; other checks showed roughly 110–120 when settled. These are bounded
observations under concurrent build/test work, not a benchmark or universal FPS.
CPU process time is not CPU submission time; no asynchronous Viewer GPU timer
was available and none was invented. The generic thermal gate separately logs
CPU submit-plus-present timings and explicitly marks GPU timing unavailable.

Native resource screenshot: five concurrent effects, 25/32 systems, 5,824/32,768
reserved slots, 935,440 GPU state bytes, all five backends GPU and fallback 0.
At capture: 1,775 dispatches, 1,713 draws, 130,240 cumulative upload bytes.
These are fixture counters, not Arin scene counters. The retained native test
also verifies complete teardown to zero systems, slots and CPU batches.

Generated native Viewer executable SHA-256:
`E3613B2D626C8072B9081B3AFE8EE8C281C7116CF20B3CCDE1BA1FCA76091AD4`.
Path: `tools/Character3DViewer/bin/Debug/Character3DViewer.exe`.
Final rebuilt VSIX SHA-256:
`DDB78B22D6A6A43B30231D478BA9811D300555E635A4CF3D0543F80E54CE43C0`.
