# Double implementation checkpoint

## Authority and reconciled baseline

- Package `smile-2.0-hardening-then-double`, revision 1; documents 00–07 read in order.
- Downloads archive `2026-09-08-0837-smile-2.0-hardening-then-double.zip`:
  SHA-256 `4C9D7986DF6962DFE58F7D65ED4182688DC82D142141753E1CD5610EF74132BB`.
  Identity and all 11 manifest lengths/hashes verified before safe extraction to
  `C:/Users/louie/Downloads/smile-hardening-double-verified`.
- Reconciled once: clean `main`, HEAD = fetched origin/main = reviewed
  `af37b2402ca02b50069fc6bc91eb9872200427fc`. No newer work was discarded.
- H01/H02/H03 remain closed; RF01–RF03 ordinary regressions preserved. No Doctor,
  retired H01 test, parallel agent, asset rebake, force push or user-window closure.

## State, owners and publication

| Phase | Actual state | Owners / dependencies |
|---|---|---|
| P0 | Validated and pushed `9f33ad9` before Double | ViewerUi/preview labels; existing Web reflection allocation/filter owner |
| D1 | Implemented and pushed `40c183d`; final integration repairs included below | Shared DoubleSemantics, native MasmDoubleEmitter/numeric runtime, Web Double.js, package schema 7, shared editor/debug facts |
| D2 | Validated and pushed `c310b7a` | Precision3D/PrecisionMath3D; same renderer translation unit, Scene3D lifecycle and Character3D actor pool |
| D3 | Implementation pushed `761b7a2`; focused gates pass; Sin accepted desktop and Web after testing both | PrecisionCamera3D, ViewerCamera/Party/Actors/calibration/effects, Sin Star I preview, shared curve slice |
| D4 | Complete: delivery/repairs validated and pushed `6ef99c5`; Sin accepted desktop/Web and confirmed audible native Party SFX | Final compiler/runtime/VSIX 2.0.61; shared Web post shader and native debug snapshots |

Source milestone `761b7a2fb906a5fa19b548bbd77390da0bdaf765`, checkpoint `8d946c8`,
and acceptance repairs `6ef99c58b6149b7d98263e0436b5db62cc8c2d03` were pushed
normally to origin/main and read back. The repairs below refresh their affected
payloads and preserve all earlier source work. This acceptance update changes only
the checkpoint; unchanged source/artifact, smoke and installed-payload evidence is reused.
Desktop/Web acceptance was published as `8a308f92ddb1404552e5450958000e78185257e2`.
The subsequent explicit audio confirmation below closes the last required gate.

Detailed unchanged P0/D1/D2 evidence, including historical source/artifact hashes,
remains in this same file's committed versions at `9f33ad9`, `40c183d` and `c310b7a`
(`git show <commit>:docs/implementation/double-precision-checkpoint.md`). This is
one current checkpoint, not a replacement investigation or parallel ledger.

## Structural completion and boundaries

- Double is binary64; Number remains integral. Shared binding/folding, native/Web
  checked arithmetic, eight-byte storage/calls, explicit ToDouble/ToNumber, strict
  text, Optional/named/ByRef, records/classes, diagnostics, formatter and debugger
  views are implemented. Internal GP/stack scalar transport and typed external XMM
  ABI are documented in `docs/language/double.md`. Packages rebuild as format 7.
- Precision3D mutations/queries borrow existing renderer camera, objects, sockets,
  immutable submissions, ribbon/particle staging, GPU spawn slots and light leases.
  Character3D retains one actor pool and transactional placement/rollback. The
  continuous actor position/rotation arrays are authoritative Double values.
- PrecisionCamera3D owns shared pan/orbit/zoom/framing/projection/anchors. Viewer
  and preview use their own scene sensitivity. ViewerParty evaluates actual integer
  elapsed time over unchanged 650-ms approach / 700-ms return; events, clips, IDs,
  handles, topology/noise order, integer ages/seeds/budgets and saved channels remain.
- Current-pose equipment pivots, glow, socket markers, Arin rim, Orin storm, fire
  and lightning pass fractions to the same renderer. Authored millith data and
  scales convert explicitly; culling allocation, pixel UI, density and saved
  calibration channel writes are deliberate integer boundaries. Full protocol,
  units and ownership: `docs/libraries/precision3d-boundary.md`.
- GPU float32 remains the final rendering boundary. Within 1000 world units, the
  tested upload budget is 2e-4; quarter units are exact. Near one million, spacing
  is about 0.0625. No improved z-buffer/source-animation precision or origin rebasing
  is claimed. Legacy integer APIs remain supported through their existing owners.
- The accepted Program coordinator changes by six added/five removed lines; no
  line-count refactor or replacement context. Relative to the reviewed baseline:
  33 new files = 11 focused production files, 19 test/script/project/expected-output
  files, three documents. Production additions are two semantic owners, four
  compiler/Web/debug owners, two native files and three precise SMILE modules.
  No new dependency/framework/renderer/pool; accepted coordinator size exceptions
  remain. The curve visual adds only its source and project.

## Focused evidence and repaired defects

- P0 native/Web reflection normal/retry and both label owners passed. Installed
  Chrome observed actual RGBA16F/HALF_FLOAT with LINEAR min/mag (9729), no
  OES_texture_float_linear, GL error 0. Renderability failure still fails soft.
- D1 actual native/Web teaching sample prints 0.002. Numeric/storage/call/package
  parity and 15 checked-error cases pass with zero live Class/Image/Text resources.
  Final CRT recheck: `d3-double-crt-gate.log`, numeric run
  `artifacts/temp/double/run-ab3612dbdc3d4495af32acf4b719b53a`.
- D2 shared/native/Web fixtures prove translation 0.25, yaw 0.125 degrees, camera
  FOV 55.125, near 0.125, far 10000.5, captured X unchanged after a live transform
  write, and reflection planes 7.25/-0.75. Failed camera/scale writes preserve state.
  Cubic midpoint (1,1.5,0), exact endpoints and equal elapsed partitions pass.
- D3 camera native/Web: `d3-camera-precision-gate.log`; normal/fallback Character3D
  pivot paths: `d3-character-pivot-gate.log`, `d3-character-web-fixed.log`.
  Lightning foundation: `d3-lightning-gate.log`; Viewer hardening native/Web,
  isolated calibration and 58 native graphics/pointer/audio checks:
  `d3-hardening-gate.log`. These audio assertions do not claim audibility.
- Actual `CalibrationTests.CheckFractionalPartySubmission` verifies both existing
  owned actors at 163 ms of approach and return, their captured render submissions,
  and 100+63-ms equivalence within 1e-12 before upload / 1e-6 at capture.
  Native: `d3-party-native-web-gate.log`; Web: `d3-party-web-gate.log`.
  The optional harness route is `test-viewer-calibration-native.ps1 -IncludeWebPrecision`.
- Typed ribbon/particle/GPU staging/light and invalid/in-flight writes pass native
  and Web (`d3-vfx-precision-gate.log`). Staged versus committed particle positions
  remain distinct. Focused style passes 29 changed/new sources (`d3-format-final.log`).
- Fixed during integration: Web pivot arrays needed existing-owner initialization;
  new Web branches needed complete payload destructuring; precise Party up is unit
  1.0; Double's native translation unit now matches music's DLL CRT. Fieldless
  classes use opaque debug C forward declarations. Web Number bounds now apply to
  runtime constant reads, preserving wide compile-time Enum definitions. Ordinary
  native OOP debug/source/package and Enum parity gates validate these repairs.
- Test-only corrections: the Web Party harness now updates StartupFile with its
  replacement source; current-owner assertions name the precise camera; package
  assertions expect schema 7; all template/version checks match VSIX 2.0.61.
- Desktop acceptance found a separate pre-existing Web scene-copy inversion:
  post mode 4 sampled framebuffer content with image-backdrop UVs. The same shader
  owner now uses framebuffer UVs for scene copies, preserving imported backdrop
  modes 6/7. Actual installed Chrome GPU pixels passed 3/4 before (scene copy failed) and 4/4 after
  (`scripts/prepare-web-post-orientation-check.ps1`, generated ignored HTML at
  `artifacts/web/PostOrientationCheck/index.html`). Mode 4 bottom/top are red/blue;
  HDR presentation also preserves orientation, image modes reverse rows; GL error 0.
  The repaired Viewer is visibly upright with correct floor contact and fixed backdrop.
  Normal native/Web post-processing regression: `d4-web-orientation-post-gate.log`.
  Viewer, Sin Star I and curve Web outputs rebuilt; logs `d4-web-orientation-*.log`.
- Actual VS Watch exposed uninitialized named snapshot aliases at helper entry.
  `CompilerDriver.BuildDebugSource` now gives safe source names directly to native
  parameters, with explicit struct/enum tags protecting debug type names. No alias
  assignment or extra generated-C step is required. Final artifact breakpoint at
  `examples/DoubleTests/Program.smile:124`: PositionX 0.002, Tiny 4.940656458412e-324,
  Values[0] 0.375, record Field_Values double[2] {0.375,0}, class field {0.625,0}.
  F10 reaches SMILE Check line 168 and retains values. This verifies stepping through
  SMILE source; it does not claim that helper-based F10 skips a SMILE routine call.
  Final shared suite: 319 (`d4-debug-final-shared.log`); native identifier compile/run
  yields 15; debug Double run prints 0.002/pass and zero Class/Text resources
  (`d4-debug-identifiers-final.log`, `d4-debug-final-{compile,run}.log`).

## Final automated integration and installed delivery

- The normal smoke was run with explicit `--skip-doctor` and continued only from
  failed steps after focused repairs. No identical full rerun after success:
  `d4-smoke.log` (build/shared/formatter/HDR), `d4-smoke-continue.log` (reflection
  through debug OOP), `d4-smoke-debug-continue.log` (OOP through Enum), and
  `d4-smoke-enum-continue.log` (remaining normal suites/game builds). The final
  verifier's stale version assertion was corrected; `d4-final-artifacts.log` passes.
- Final shared suite: 319 tests (`d4-shared-final.log`). Formatter integration:
  13 tests; repository style: 430 tracked files, plus the explicit new-source check.
  `d4-final-build.log` passes with no NU1503. Expected negative diagnostics count
  only where their owning gate checked exact output and cleanup.
- Final Viewer native + Full Web: `d4-viewer-final-build.log`, 46 / 35 assets,
  existing model cache hits. Final Sin Star I and curve native/Web builds:
  `d4-preview-{native,web}-final.log`, `d4-curve-{native,web}-final.log`.
- VSIX 2.0.61 installed with the repository installer `--skip-build`, without
  `/shutdownprocesses`; preflight requires VS already closed. Verification compares
  35 actual installed compiler/shared-language/runtime/template payload hashes with
  archive entries. Final acceptance rebuild/install logs:
  `d4-debug-final-build.log`, `d4-debug-final-vsix-install.log`. Installed root:
  `C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/1zp0dvff.zi5`.
  The current payload hashes are listed below.
  The source SMILE library packages remain repository-built, not newly bundled
  source libraries invented for the VSIX.
- Both live calibration exports preserve 24 Arin keys / zero Orin keys. Canonical
  JSON SHA-256: Arin `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`,
  Orin `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
  No model, descriptor, animation or canonical calibration content changed.

| Final source/artifact | SHA-256 |
|---|---|
| `src/Smile.Compiler/CompilerDriver.cs` | `EDFD361D15BA877ACDADA9CF88DAC378D88C3C6160D3F9086BF5684DF805D67A` |
| `src/Smile.Compiler/WebOutputWriter.cs` | `AD480E7BC7ED0F462D79D6065800063E4F9985C461E8D8863A5617C1396C4DA1` |
| `tools/Character3DViewer/bin/Release/Web/smile-runtime.js` | `6BD2F34B614D430E873B3F89E9931B4F4693E22ED5BF3DE9192F51D6F2EEF63F` |
| `artifacts/tests/DoubleTestsFinalDebug.exe` | `E6CF3FD3C05408D0C7CFB14AF716BC6D7CF972A16BDE284B4F38A23B33E2EC79` |
| `artifacts/tests/DoubleTestsFinalDebug.pdb` | `F669F2F0C7430751605DA031C0ED724FB8866349221FF39449CFCAEBB7F7B69E` |
| `src/Smile.Compiler/NativeDebugTypes.cs` | `E285D4496E12E4750051221633A67EDC3669438BCFFA8745701232AB9714F938` |
| `src/Smile.Compiler/WebEmitter.cs` | `6CC67498F856443CFCB9DB03BE3C46AA14DF081F583E5835FD372AD95CE02488` |
| `libraries/Smile.Simple3D/PrecisionCamera3D.smile` | `7800374693918BF83FA7A6BC7F90DA2B2430F25097A3853A6161F1B5D10FD43E` |
| `tools/Character3DViewer/CalibrationTests.smile` | `AF2992AAC1CCFE6865E0252208A5820CF91C28D8C32A8CF68D77D7426429E792` |
| `artifacts/compiler/smilec.dll` | `A548D7CD73A08DB10845EA36268A63683CC4258D939329207F46F87FABDB311F` |
| `artifacts/compiler/Smile.Language.dll` | `3E2D01D9A9AD7DEFEEBBFAD04557322955A7C4CF683F49A45E2D43681F84E6E5` |
| `artifacts/compiler/Smile.NativeRuntime.lib` | `ACE79086D0C5D9E01262B3EB9591FA04D4BA8023A5431452DBE7B3B9743CDA75` |
| `artifacts/vsix/Smile.VisualStudio.vsix` | `F23D7F9668ED5234242D503A4B0912F481926F644485EB29BE626D3E3BD68486` |
| `tools/Character3DViewer/bin/Release/Character3DViewer.exe` | `AA1EE1BDC2A91D8AEA447BADBABDBC0770CD9CF6A81893F1217AAB5187D59027` |
| `tools/Character3DViewer/bin/Release/Web/game.js` | `6EF95474B02C837C6795D6959F62E5B5E555F4CE5CDBE44201999AD9336B54F3` |
| `artifacts/games/SinStarI/SinStarI.exe` | `B507E3A5C4955C05157613996D28F40655549A7C8ACED1E31101EA3E1ABDC4C3` |
| `artifacts/web/SinStarI/game.js` | `DB34B6E2EC5309D8CB906DBE3E84284A06F9C3565BEF19E1730E96B1A84B0679` |
| `artifacts/examples/PrecisionCurve3D.exe` | `C2E01EC356F5F27C6BCDC60E36DDBE0F6367A18D8C31F38C46BF8DF489DB8E1F` |
| `artifacts/web/PrecisionCurve3D/game.js` | `74162DEA9B6FB34426F87A7F65715AF7089692A2068704BDDEA660A954308173` |
| `libraries/Smile.Simple3D/bin/Release/Smile.Simple3D.smilelib` | `6FAFC366640420ACB866BB4F181CBE13D8036E5D2068685B584FB3FE1C0DC107` |

## User acceptance and completion

Native Windows control is available through the installed computer-use tool. Sin's
Any App setting was already correct; the earlier browser-only tool limitation was
not a missing permission. No further settings toggle or Double authorization is needed.

- Final native Viewer launched through Launch.ps1 after both calibration exports;
  the old Viewer closed gracefully, with live data preserved. Actual Arin/Orin tabs,
  Party turns/dragon effects, Space pause, D toggle, timeline scrub, floor modes,
  fixed backdrop, equipment/contact, small horizontal/vertical and larger diagonal
  pans, wheel in/out, H/V camera sliders and right-click reset were exercised.
  Native curve visibly moves its cube above the floor and exits normally.
- Native Sin Star I opened Battle through its real title, rendered grounded Arin
  and reflection, accepted small/large pans, wheel in/out and R floor toggle, then
  Escape returned to the title. Task-owned preview/debug windows closed normally;
  unrelated unsaved Blender and Notepad++ work was preserved.
- Bringing the real Chrome window/tab to the foreground resolved the reported
  0–1 FPS state (observed 61, then about 102–120 FPS). No rendering performance
  repair was needed for that state. Chrome Viewer pan/zoom, H/V sliders, tabs/Party,
  Space/D, scrub and reset respond. The corrected Web scene/floor is upright.
  Chrome Sin Star I and shared curve visual evidence remains valid.
- Sin subsequently confirmed: "I've tested both the desktop and web version and
  they both look good to me". Record this as user acceptance of desktop/Web visuals
  and interaction; the pending visual acceptance is closed.
- The tooling gate is covered by shared Quick Info/editor tests and actual VS typed
  Watch/source stepping. A separate mouse-hover observation is not claimed.
- Sin also answered the focused desktop Party attack/impact SFX question:
  "Yes, the sound effects are audible". This is the human audible evidence required
  by document 07, separate from successful audio initialization and visual acceptance.

P0 and D1–D4 are complete: implementation, selected production adoption, validation,
publication, required VSIX installation and user acceptance are recorded above.
Remaining work in this package: none. Next action: STOP as instructed. Preserve the
unchanged build/test/install evidence and do not start unrelated or held features.

## On Hold

Loader/splash enhancements, semantic-inspection CLI, Battle Scene Editor,
subject-first syntax and unrelated suspended features remain deferred.
