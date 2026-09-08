# Double implementation checkpoint

## Authority and reconciled baseline

- Current closeout: `smile-2.0-double-closeout-review`, revision 1; numbered documents
  00–03 read in order. Archive SHA-256
  `5E47D16DDBD3CD5F984F685A19928A13D3227CE8B62D7517D068C86E7428EDE9`;
  identity, reviewed commit and all eight manifest lengths/hashes verified before
  safe extraction under `C:/Users/louie/Downloads/smile-double-closeout-review-verified`.
  Reconciled once for closeout: clean main, HEAD = fetched origin/main =
  `d84ffdc489839ca77cda5bcba73281e366a63e1a`; reviewed `6ef99c5` is an ancestor.
  Newer `8a308f9`/`d84ffdc` change only this checkpoint. Preserve their human acceptance
  evidence; the explicit target-by-target checklist below supersedes their broad
  completion disposition. No implementation or refactor phase is restarted.
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
| D3 | Implementation pushed `761b7a2`; focused gates and general user acceptance retained; explicit camera observations below pending | PrecisionCamera3D, ViewerCamera/Party/Actors/calibration/effects, Sin Star I preview, shared curve slice |
| D4 | Delivery/repairs validated and pushed `6ef99c5`; native/Chrome SFX accepted; preview gestures and hover observations below pending | Final compiler/runtime/VSIX 2.0.61; shared Web post shader and native debug snapshots |

Source milestone `761b7a2fb906a5fa19b548bbd77390da0bdaf765`, checkpoint `8d946c8`,
and acceptance repairs `6ef99c58b6149b7d98263e0436b5db62cc8c2d03` were pushed
normally to origin/main and read back. The repairs below refresh their affected
payloads and preserve all earlier source work. The original acceptance update changed only the checkpoint. The separately authorized
Viewer VFX corrections below refresh their affected source/artifact evidence.
Desktop/Web acceptance was published as `8a308f92ddb1404552e5450958000e78185257e2`.
The subsequent explicit audio confirmation below closes the native audible-SFX gate.

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
  This was the initial Double delivery; the bounded VFX closeout installation below
  supersedes its compiler/runtime/extension payloads. Current hashes are listed below.
  The source SMILE library packages remain repository-built, not newly bundled
  source libraries invented for the VSIX.
- Both live calibration exports preserve 24 Arin keys / zero Orin keys. Canonical
  JSON SHA-256: Arin `7A3E7BC823CF544FA0136920A9D0752BF7DE585C0891B9073E688B1F783B3F67`,
  Orin `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
  Models, descriptors and animations are unchanged. Sin separately authored the new
  Defend frame-0 sword correction; it remains integral and is included in native/Web defaults.

| Current source/artifact (unchanged evidence retained) | SHA-256 |
|---|---|
| `tools/Character3DViewer/ViewerEffects.smile` | `B234833F0566389E79066822E5C4DA64D7A9399C0F0BB106D1AA960283022076` |
| `tools/Character3DViewer/OrinStorm.smile` | `05A9E46FEDFD3084552CF569CF68B1AEBC4A983BDFFED474D511394391BE64AF` |
| `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` | `9D669C000386AE7B108846A8A275F57A66B7567F2D3A3355F42198C980C18F79` |
| `src/Smile.Compiler/CompilerDriver.cs` | `EDFD361D15BA877ACDADA9CF88DAC378D88C3C6160D3F9086BF5684DF805D67A` |
| `src/Smile.Compiler/WebOutputWriter.cs` | `8FF7D5248436B627E31C02AA748EDD4F5355A4D4FC59CAB08CEE024CC787683A` |
| `tools/Character3DViewer/bin/Release/Web/smile-runtime.js` | `AF973877FE233427F5541339C649F7A4DA9AF390B46AA080E145EAA283129B15` |
| `artifacts/tests/DoubleTestsFinalDebug.exe` | `E6CF3FD3C05408D0C7CFB14AF716BC6D7CF972A16BDE284B4F38A23B33E2EC79` |
| `artifacts/tests/DoubleTestsFinalDebug.pdb` | `F669F2F0C7430751605DA031C0ED724FB8866349221FF39449CFCAEBB7F7B69E` |
| `src/Smile.Compiler/NativeDebugTypes.cs` | `E285D4496E12E4750051221633A67EDC3669438BCFFA8745701232AB9714F938` |
| `src/Smile.Compiler/WebEmitter.cs` | `6CC67498F856443CFCB9DB03BE3C46AA14DF081F583E5835FD372AD95CE02488` |
| `libraries/Smile.Simple3D/PrecisionCamera3D.smile` | `7800374693918BF83FA7A6BC7F90DA2B2430F25097A3853A6161F1B5D10FD43E` |
| `tools/Character3DViewer/CalibrationTests.smile` | `A41E6F3A9BC0833A8EF1CBCA12F8EDF6143B42896BFC657C9A0DA879516BD154` |
| `artifacts/compiler/smilec.dll` | `D27B6AA0A6FD7FC5A4AF4D6534ECEC0A8B1489E8ACCBF4B59569108174F7D111` |
| `artifacts/compiler/Smile.Language.dll` | `8E2E4B7F791E049C9AF7E72673DDEF4E99D5D3CC2801B68A96F3F2D6EB49BC63` |
| `artifacts/compiler/Smile.NativeRuntime.lib` | `4E4E4DC7223FC3006FFCDF4DC62B3F9A8C3D485F947B302691A06C247C698B69` |
| `artifacts/vsix/Smile.VisualStudio.vsix` | `8903D91A201A145D2DE80B036DB326722485FB8737D5E5AFACEA775810448C2A` |
| `tools/Character3DViewer/bin/Release/Character3DViewer.exe` | `19928FC314A89D98A41C495466E288FC0ADFB857C7A842AB899F3EC0537E8ABA` |
| `tools/Character3DViewer/bin/Release/Web/game.js` | `2A7883EA394C16F2625BB254113D72BAFDBB4CFD9011484AC2E32864E35ED520` |
| `artifacts/games/SinStarI/SinStarI.exe` | `B507E3A5C4955C05157613996D28F40655549A7C8ACED1E31101EA3E1ABDC4C3` |
| `artifacts/web/SinStarI/game.js` | `DB34B6E2EC5309D8CB906DBE3E84284A06F9C3565BEF19E1730E96B1A84B0679` |
| `artifacts/examples/PrecisionCurve3D.exe` | `C2E01EC356F5F27C6BCDC60E36DDBE0F6367A18D8C31F38C46BF8DF489DB8E1F` |
| `artifacts/web/PrecisionCurve3D/game.js` | `74162DEA9B6FB34426F87A7F65715AF7089692A2068704BDDEA660A954308173` |
| `libraries/Smile.Simple3D/bin/Release/Smile.Simple3D.smilelib` | `6FAFC366640420ACB866BB4F181CBE13D8036E5D2068685B584FB3FE1C0DC107` |

## Closeout acceptance and next action

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
  they both look good to me". Preserve this general desktop/Web acceptance; it does
  not itemize the closeout package's precise camera gestures or hover observations.
- Shared Quick Info/editor tests and actual VS typed Watch/source stepping remain
  valid evidence. They are not relabeled as mouse-hover or paused DataTip observations.
- Sin also answered the focused desktop Party attack/impact SFX question:
  "Yes, the sound effects are audible". This is the human audible evidence required
  by document 07, separate from successful audio initialization and visual acceptance.

Closeout C01: root task routing now links this checkpoint and the precise boundary,
retains the completed refactor prerequisite, and preserves held features. Documentation
only: scoped diff/link checks; no source/asset/calibration/build/install changes.

Current capability discovery: native Windows control works. Its documented drag has
no button/duration option, and the tool has no hover action or audio feed. Installed
Chrome is available. No settings change is requested; Sin will supply the unavailable
observations one short check at a time while the applicable app is foreground.

| Required observation | State / evidence source |
|---|---|
| Native Viewer: slow/moderate horizontal/vertical middle orbit and pan; release/reset | PASS — Sin answered the explicit native check: "Yes, all pass" |
| Foreground Chrome Viewer: same gestures/release/reset | PASS — Sin answered the explicit slow/moderate horizontal/vertical middle-orbit, left-pan, release and right-reset check: "Yes, all pass" |
| Native Sin Star I Battle preview: same gestures/release | NOT OBSERVED in that explicit form; pending human result |
| Foreground Chrome Battle preview: same gestures/release | NOT OBSERVED in that explicit form; pending human result |
| Native Viewer Party attack/impact SFX | PASS — Sin explicitly confirmed audibility above; audio code/cues unchanged by the bounded VFX fix |
| Foreground Chrome Viewer Party SFX | PASS — Sin answered the focused foreground Chrome attack/impact audio check: "Yes, both are audible" |
| Installed VS Double scalar/array/field hover | NOT OBSERVED — static Quick Info and paused debugger DataTip must be distinguished |

Closeout C01 published as `7b82468`; the listed unchanged application/extension
identities were verified once. Sin then requested two bounded Viewer VFX corrections:

- Orin Thor Attack/Thunder Smash releases beneath or too near the hammer. Owner:
  `OrinStorm.UpdateBattleEffects`; the release ground center previously copied `Head`,
  although `ViewerParty.UpdateOrinStorm` already supplies the real dragon Chest target.
  The owner now latches the enemy ground point at release, retaining charge sockets,
  timing, per-context ownership and the shared Lightning renderer. Source review
  verifies phase-change latching. Native and Chrome release views show ground arcs
  centered on the Dragon's feet ahead of Orin. The existing 250-unit arc radius is
  unchanged; this does not claim that every branch is entirely ahead of the actor.
- Arin's new golden sword has a sharp transverse cut above the guard in Sin's native
  and Chrome screenshots. Reproduced on native Idle at close range: Sword Fire off
  leaves the cut; Glow off removes it. Owner: `ViewerEffects`' enlarged duplicate
  equipment mesh intersects the blade. The shared default now uses identical mesh,
  palette, calibrated pose and transform for equipment surface coatings on every
  character. Only a front-culled outline explicitly requests expansion. No Arin
  identity condition, character-specific offset or new renderer was introduced.
  Surface alignment alone failed the brightness check because strict depth rejected
  coincident fragments. A flagged generic native/Web additive-mesh correction now
  reads less-or-equal depth without writing it; Web restores strict depth after draw.
  Native/Chrome close-ups retain bright gold/fire without the transverse cut, and
  the hand still occludes the grip. Chrome final view: Idle 199 ms, H -111, V 27,
  Zoom -128, approximately 120 FPS. Actual Party views retain both actors' effects.

Additional authorized equipment presentation:

- Lightning appearance is selected by `ViewerProfiles.EquipmentGlowStyle` and
  applied by `ViewerEffects.ConfigureEquipmentGlow` to primary and borrowed Party
  equipment. Its front-culled expanded outline preserves the hammer material/grip.
  The same style is tested on both real models; no Orin rendering identity branch.
  Sin explicitly accepted the native outline/material, then requested stronger
  epic glow and a trail. `LightningVfx3D.WeaponTrail` now owns the former sparse
  trail as a reusable two-edge corona with fading world-space motion particles.
  Orin supplies calibrated points and lifecycle decisions only. Native Attack
  516 ms visibly leaves a dense trail; the metal and grip remain visible.
- Arin's shield defaults to its existing ember rim plus three small LineFire
  emitters at intensity 7 each, versus the sword's 200. Native Defend frame zero
  visibly has modest flame wisps above the rim; the sword is substantially stronger.
  Ember Outline remains an option. Existing family freeze/hide controls are preserved.
- Sin explicitly confirmed the new Arin Defend frame-0 save is his and requested
  the Web update and commit. Its 24-key canonical SHA is listed above. Sword
  decoupling is enabled at that key, rotation `[5, 83, 13]`, position `[-3, 0, 0]`.
  Native/Web packaged defaults were regenerated. No test rewrote this authored
  correction. Existing browser working saves retain precedence over defaults.

Focused VFX validation and delivery:

- Native/Web calibration gate passes with Sin's new save and both glow styles on
  both models: three poses, three sockets, fractional XYZ, exact saved-byte
  round-trips/backups and actual Party fractional submissions (`vfx-epic-calibration.log`).
  The first run used stale generated defaults after Sin's live save; refreshing
  those defaults resolved the byte mismatch without changing his calibration.
  The Web fixture allows eight startup frames for its additional profile loads.
  SML3605 rejects the previous identity in its disposable reused output as designed.
- Shared Lightning foundation and real two-actor ownership/fallback tests pass on
  native/Web (`vfx-epic-lightning.log`, `vfx-epic-isolation.log`). The foundation
  submits two independent arbitrary fractional edge segments, verifies pause and
  independent destruction. The actor test now expects the requested idle corona
  to replenish particles; the separate explicit freeze count remains unchanged.
  It does not claim to automatically assert ground-discharge endpoint latching.
- Material native/Web gates pass (`vfx-closeout-materials-final.log`). Shared
  language 319-test and formatter 13-test evidence remains valid; final repository
  formatting checks 432 tracked files (`vfx-epic-style-check.log`). Earlier unchanged
  Double numeric/rendering/full-smoke evidence is reused. No Doctor was run.
- Final Viewer native/Full Web builds pass with 46/35 cached assets:
  `vfx-epic-viewer-{native,web}.log`. Launch.ps1 closes only the old Viewer normally
  and preserves both live saves. Native captures in
  `artifacts/temp/vfx-closeout-evidence/`: `native-surface-glow.jpg`,
  `native-forward-lightning.jpg`, `native-epic-lightning-trail.jpg`,
  `native-defend-shield-flames.jpg`. Canonical models/animations/descriptors unchanged.
- Compiler/runtime/VSIX build and installation for the additive depth correction:
  `vfx-closeout-final-build.log`, `vfx-closeout-vsix-install.log`. VSIX 2.0.61,
  35 installed payload hashes matched; installed root
  `C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/qxbvburr.k11`.
  Later edits affect Viewer/SMILE library sources only, which are not bundled VSIX
  payloads; the installed compiler/runtime is unchanged. No repeated installation.
- At Sin's request Chrome closed normally and restarted. Native capture temporarily
  reported a missing foreground process ID, but worked again on the final native
  Viewer. No settings toggle or permission workaround. Human audio/gesture/hover
  observations remain distinct from tool rendering checks.
- Sin went outside and requested autonomous progress. Native/Chrome preview gestures
  and VS hover remain pending; no more human checks are requested while he is away.
  The task-owned Sin Star I preview closed normally at his request so it no longer
  obstructs the stream; Character Viewer returned to the foreground.

## Current VFX extension (validated, publication milestone)

Sin requested three switchable Orin equipment styles and independent 0–200%
weapon/shield intensity controls for both characters. Original Lightning remains;
Blue Flame uses Arin's shared fire preset with a blue-white palette. Sin accepted
the blue flame appearance and then requested 40% more weapon flame: its baseline
is now 140 (70% of Arin's 200), with the separate shield baseline unchanged; Neon Arcs uses closed outer contours plus short travelling
edge arcs without star particles. The controls are session presentation settings,
not saved pose/calibration channels. Program.smile remains unchanged.

Owners: ViewerUi/InspectorCommands/InspectorPresentation for controls;
ViewerEffects and the existing per-actor OrinStorm contexts for preferences and
attachment lifetime; shared Precision3D/Character3D socket-local contour transform,
FireEmitter3D contour sampling/palette and LightningVfx3D contour/edge paths.
Native/Web expose the existing socket matrix basis without millith rounding.
Fire has seven admitted emitter slots (Arin 4, Orin 2, Dragon 1), inside unchanged
renderer particle capacities, with the existing CPU fallback. No new renderer.

Orin's eight-point head outline is canonical presentation data in
`OrinV13/OrinEquipmentContours.smile`, generated from the accepted rigid head
vertices by `scripts/update-orin-lightning-sockets.py`. It uses the existing
SwordBase socket and the normal GLB-to-SM3D Z conversion. The first visual run
exposed a missed Z conversion in the generated data; corrected at that asset
boundary, with an actual authored HammerHead-vs-transformed-point regression.
The descriptor, model, cooked asset and live calibration fingerprints stay intact.
An attempted descriptor socket addition was rejected by the calibration guard and
withdrawn before delivery; no calibration migration or asset replacement occurred.

Focused native/Web fire tests passed (`vfx-contour-fire-tests.log`). Final native
and Full Web Viewer builds pass (`vfx-native-final.log`, `vfx-web-final.log`),
including the requested 40% stronger weapon baseline. Edge regression passes on
both targets (`vfx-arcs-final.log`); actual two-actor style/forced-fallback coverage
passes (`vfx-styles-isolation-final.log`). Intensity command/bounds checks pass
native and Web (`vfx-intensity-focused.log`, `vfx-intensity-web-final.log`). One Web
harness invocation incorrectly required indexed 3D draws from the UI-only fixture;
rerunning with its normal options passed. This was a test invocation error.

The affected compiler/runtime/VSIX was rebuilt and installed once:
`vfx-contour-runtime-build.log`, `vfx-styles-vsix-install.log`; VSIX 2.0.61,
35 installed payload hashes matched at
`C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/gfvm2n5h.50a`.
Subsequent edits affect source libraries/Viewer, not those installed payloads.
Do not repeat installation for these unchanged artifacts.

Sin explicitly accepted native Arin shield flames and Orin's blue flame appearance
before the additional 40% request. The stronger native flame was tool-observed and
captured at `artifacts/temp/vfx-closeout-evidence/native-blue-white-stronger.jpg`.
Chrome recovery completed after Sin explicitly requested a full restart: all Chrome
processes exited normally, Chrome restarted, and the final local tab was brought
to the foreground. Final native/Chrome inspection exercised all three styles,
50%/100% controls, preserved material detail, and visible blue motion trails.
Chrome Arin Defend frame zero uses the authored correction and modest shield flames.
Screenshots in `artifacts/temp/vfx-closeout-evidence/`: `native-blue-final.jpg`,
`native-neon-arcs-final.jpg`, `chrome-blue-white-stronger.png`,
`chrome-neon-arcs-final.png`, `chrome-original-lightning-retained.png`,
`chrome-blue-trail-final.png`, `chrome-arin-defend-authored.png`.
These are tool observations, not new human approvals of every style. The explicit
human observations still outstanding in the table above remain pending.

Final Neon inspection exposed oversized short-arc ribbons and overlapping closed
joins. Shared LightningVfx3D now shares corner samples/wraps the closing tangent,
uses fractional narrow contour widths, and gives short arcs four distinct bends.
Its main-target query consumes the actual built endpoint index; the focused test
caught the old independently recomputed index after changing segment density.
Final native/Web regression: `vfx-edge-detail-tests.log`; native/Full Web builds:
`vfx-edge-detail-build.log`; 432-source formatting plus final changed/untracked
source check: `vfx-final-format-check.log`, `vfx-final-changed-format.log`.
The initial compile rejected integer zero at the new private Double-width boundary;
explicit Double zero corrected it. No language conversion rule was weakened.

Limits: outlines are caller-supplied ordered equipment contours, not automatic
camera-dependent silhouette extraction. Occlusion and the existing bloom/material
limits affect visible arcs and peak brightness. Equipment selection does not replace
the existing attack-discharge presets, audio or timing. The Full Web directory is
updated locally for publication; no upload to sincioco.com is claimed.

Final artifact SHA-256:
- `tools\Character3DViewer\bin\Release\Character3DViewer.exe`: `C31F9BA418BB42CB8DF8DA5DFFE5FA634C1DF57E38852A633B81187CEB36AE0A`
- `tools\Character3DViewer\bin\Release\Web\index.html`: `AD2C598021B348040573540F958FF51C46D075CCAE5B2CAB938A827A3BB92896`
- `artifacts\vsix\Smile.VisualStudio.vsix`: `16F4C5A4EB266326FDE75AFB02B4C4534EF60AD0947C320C66709502652C7E03`

Both final live-save exports retain Arin's 24-key SHA and Orin's zero-key SHA listed
above. Program.smile, model/animation/descriptor assets and calibration identities
remain unchanged. This milestone includes Sin's requested authored Arin pose.

Next action: commit/push this validated equipment-VFX and authored-pose milestone,
then implement Sin's re-authorized VFX floor reflection through the existing
renderer with a separate default-on toggle. No unrelated suspended work.

## On Hold

Loader/splash enhancements, semantic-inspection CLI, Battle Scene Editor,
subject-first syntax and unrelated suspended features remain deferred.
