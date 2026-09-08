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
| D3 | Implemented; native/Web focused gates pass; full interaction acceptance pending | PrecisionCamera3D, ViewerCamera/Party/Actors/calibration/effects, Sin Star I preview, shared curve slice |
| D4 | Automated integration and installation validated; publication pending | Final compiler/runtime/VSIX 2.0.61, native/Web deliveries; manual acceptance remains open |

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
  32 new files = 11 focused production files, 18 test/script/project/expected-output
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
  archive entries (`d4-vsix-install.log`). Installed root:
  `C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/0ka1rcc4.qlq`.
  The source SMILE library packages remain repository-built, not newly bundled
  source libraries invented for the VSIX.
- Both live calibration exports preserve 24 Arin keys / zero Orin keys. Canonical
  JSON SHA-256: Arin `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`,
  Orin `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
  No model, descriptor, animation or canonical calibration content changed.

| Final source/artifact | SHA-256 |
|---|---|
| `src/Smile.Compiler/NativeDebugTypes.cs` | `E285D4496E12E4750051221633A67EDC3669438BCFFA8745701232AB9714F938` |
| `src/Smile.Compiler/WebEmitter.cs` | `6CC67498F856443CFCB9DB03BE3C46AA14DF081F583E5835FD372AD95CE02488` |
| `libraries/Smile.Simple3D/PrecisionCamera3D.smile` | `7800374693918BF83FA7A6BC7F90DA2B2430F25097A3853A6161F1B5D10FD43E` |
| `tools/Character3DViewer/CalibrationTests.smile` | `AF2992AAC1CCFE6865E0252208A5820CF91C28D8C32A8CF68D77D7426429E792` |
| `artifacts/compiler/smilec.dll` | `C2F13EC000B421A2D74F021E5FBA5C502BDD23F4399C7522F9D670C8043D0F6F` |
| `artifacts/compiler/Smile.Language.dll` | `82E562298FE9763944A139F18D8E998C6F87CAE666D42D6F4124AC8BA91D0206` |
| `artifacts/compiler/Smile.NativeRuntime.lib` | `49F1917075B7945B78E77180EAA150DE993B9B6C46A232DFAF059E12AAC86CEE` |
| `artifacts/vsix/Smile.VisualStudio.vsix` | `7E8E5C016E5F6C77649F9D828B0E3B56CC90721B3FDB521239C98B11D4D2BD38` |
| `tools/Character3DViewer/bin/Release/Character3DViewer.exe` | `AA1EE1BDC2A91D8AEA447BADBABDBC0770CD9CF6A81893F1217AAB5187D59027` |
| `tools/Character3DViewer/bin/Release/Web/game.js` | `6EF95474B02C837C6795D6959F62E5B5E555F4CE5CDBE44201999AD9336B54F3` |
| `artifacts/games/SinStarI/SinStarI.exe` | `B507E3A5C4955C05157613996D28F40655549A7C8ACED1E31101EA3E1ABDC4C3` |
| `artifacts/web/SinStarI/game.js` | `DB34B6E2EC5309D8CB906DBE3E84284A06F9C3565BEF19E1730E96B1A84B0679` |
| `artifacts/examples/PrecisionCurve3D.exe` | `C2E01EC356F5F27C6BCDC60E36DDBE0F6367A18D8C31F38C46BF8DF489DB8E1F` |
| `artifacts/web/PrecisionCurve3D/game.js` | `74162DEA9B6FB34426F87A7F65715AF7089692A2068704BDDEA660A954308173` |
| `libraries/Smile.Simple3D/bin/Release/Smile.Simple3D.smilelib` | `6FAFC366640420ACB866BB4F181CBE13D8036E5D2068685B584FB3FE1C0DC107` |

## Open acceptance and next action

Chrome visibly renders the Viewer/Party/Arin, attached effects, backdrop and floor;
the Demo toggle works. The shared curve renders its moving cube/floor. Sin Star I
opens Battle from its real title menu, responds to pan/wheel zoom and R switches
Reflective to Original. These observations are partial, not a complete manual pass.

The Viewer reports 0–1 FPS and some brief input is missed. A disposable probe
observed ~1000-ms RAF gaps despite visible/focused state and three long tasks
(max 559 ms including startup), with no browser warning/error logs. Callback
resolver timing is not total rendering cost. Scheduling/foreground behavior is
suspected, not proven. Probe removed; scope/result: `d3-frame-probe-scope.txt`.

Native app APIs are disabled in the current tool session. Sin granted desktop
control permission; enabling Settings > Computer use > Any App was requested.
The pre-existing native Viewer (PID 29908, started 07:30) was preserved; its running
image is not claimed to be the new build. Next: obtain native/foreground control,
preserve any unsaved preview, launch the final Viewer through Launch.ps1, and finish
one combined slow/moderate horizontal/vertical orbit and pan, zoom in/out, reset,
keyboard/tab/Party approach/return, pause/scrub, attachment/ground/floor/backdrop and
audible SFX check on native/Chrome. Complete the native curve visual and installed
VS Double breakpoint/hover check. Do not claim full acceptance while these remain.
Reuse passing automated/install evidence unless a source/dependency actually changes.
Publish this validated implementation/checkpoint, then finish only these open checks;
no further Double authorization is needed and no unrelated feature should start.

## On Hold

Loader/splash enhancements, semantic-inspection CLI, Battle Scene Editor,
subject-first syntax and unrelated suspended features remain deferred.
