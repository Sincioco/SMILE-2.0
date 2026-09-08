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
| D3 | Implementation pushed `761b7a2`; focused gates retained; all native/Chrome Viewer and preview camera observations below accepted by Sin | PrecisionCamera3D, ViewerCamera/Party/Actors/calibration/effects, Sin Star I preview, shared curve slice |
| D4 | Delivery/repairs validated and pushed `6ef99c5`; all required native/Chrome gesture/SFX and installed VS hover observations below accepted by Sin | Final compiler/runtime/VSIX 2.0.61; shared Web post shader and native debug snapshots |

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

| Retained Double/earlier VFX source/artifact (later milestones below supersede changed entries) | SHA-256 |
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
| Native Sin Star I Battle preview: same gestures/release | PASS — Sin answered the explicit slow/moderate horizontal/vertical middle orbit and left-pan check: "Yes, all pass"; the preview's intentional smooth return after release was clarified |
| Foreground Chrome Battle preview: same gestures/release | PASS — Sin selected the real Sin Star I tab and answered the explicit slow/moderate horizontal/vertical middle orbit, left-pan and smooth return check: "Yes, all pass" |
| Native Viewer Party attack/impact SFX | PASS — Sin explicitly confirmed audibility above; audio code/cues unchanged by the bounded VFX fix |
| Foreground Chrome Viewer Party SFX | PASS — Sin answered the focused foreground Chrome attack/impact audio check: "Yes, both are audible" |
| Installed VS static Quick Info: Double scalar/array | PASS — Sin hovered PositionX at line 71 and Values at line 56 in examples/DoubleTests/Program.smile and confirmed both correct Double types; tool capture also shows Dim PositionX As Double |
| Installed VS static Quick Info: record/class Double fields | PASS — Sin hovered Values in Original.Values[0] / FirstBox.Values[0] at lines 99–100 and confirmed both Double arrays |
| Installed VS paused debugger DataTip: Double array/record/class field values | PASS — paused at line 102, Sin's final corrected observation confirms Values[0] = 0.375, Original.Values[0] = 0.375 and FirstBox.Values[0] = 0.625 at lines 98–100 |
| Installed VS paused debugger DataTip: Double scalar value | PASS — after requesting the question again because of a misclick, Sin explicitly reconfirmed: "Yes, PositionX shows 0.002" at line 71 while paused |

Final hover acceptance uses the unchanged DoubleTestsFinalDebug EXE/PDB hashes above
and the current 35-payload-verified VSIX installation listed in the floor milestone.
Sin briefly reported all three array/field values as 0.375, then corrected that report:
the observed values were 0.375 / 0.375 / 0.625. His final correction supersedes the
provisional failure; no new defect or production patch is claimed from that report.
Actual tool captures `vfx-closeout-evidence/vs-double-static-scalar.jpg` and
`vs-double-paused-array.jpg` supplement his observations; they do not independently
prove all three field values. No property getter was requested or evaluated for hover.
All required D3/D4 human observations are accepted as of September 9, 2026. This checkpoint update
changes documentation only; reuse the validated builds, smoke and installed payloads.

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
- At that milestone Sin went outside and requested autonomous progress. Native/Chrome
  preview gestures and VS hover were pending; no human checks were requested while away.
  The task-owned Sin Star I preview closed normally at his request so it no longer
  obstructs the stream; Character Viewer returned to the foreground.

## Published equipment VFX milestone

`f5f4186` published the reusable equipment styles, intensity controls, stronger
blue-white hammer flame and Sin's authored Arin Defend pose before floor work.
Orin retains Original Lightning, Blue Flame and Neon Arcs; both characters have
independent weapon/shield intensity controls. Blue weapon baseline is 140, 40%
above its prior 100 and 70% of Arin's 200. Shared contour sampling follows caller-
supplied equipment outlines, not an assumed sword axis. No renderer, actor pool,
calibration identity, model or animation was replaced; Program.smile is unchanged.
Detailed owners, source fixes, native/Web tests, installation hashes and screenshots
are preserved in this same checkpoint at `f5f4186`. Their unchanged evidence is reused.

## Arena VFX reflections — published `c7f8f07`

Sin re-authorized floor VFX reflections after the equipment milestone and requested
a separate default-on toggle. Shared Graphics3D/Arena3D now accept optional IncludeVfx
(default False for existing callers). ViewerRendering explicitly enables it and owns
the session preference; InspectorCommands/Party route typed actions, Ui renders the
button, and InspectorPresentation projects state without retaining another copy.
Original floor mode and hidden floors suppress capture without losing the preference.

The existing native/Web reflection owners replay accepted transparent mesh snapshots,
committed CPU/GPU particles and ribbons after opaque geometry under the mirrored camera.
They reuse the same pose, simulation state, resources and admission; no actor update or
GPU simulation dispatch repeats. Reflection draw diagnostics stay separate from main
scene counts. Effects clip to the receiver plane and depth-test against reflected opaque
geometry. Heat distortion is excluded; main-camera soft-intersection depth is not reused
in the mirrored camera. Transparent effects retain the existing submission order.

Sin reported a white, washed-out native hammer reflection and shield/weapon outlines
appearing through legs/hands and stronger than the real equipment. Reproduced on native
Neon Idle, H -111 / V 59 / zoom 2. The shared native backdrop pass restored main-scene
depth onto the reflection target. Different target size/sample count invalidated that
attachment, removing occlusion and accumulating hidden additive glow surfaces. The
backdrop now restores the reflection target's own depth when mirrored. No Orin-specific
brightness adjustment or asset workaround was introduced. Web already restores its
reflection framebuffer correctly. Fixed native/Chrome inspection preserves metal/grip
detail and the hand/leg overlap; reflected effects use the normal floor strength.

Evidence (logs beneath `artifacts/temp`, screenshots in `vfx-closeout-evidence`):
- `vfx-reflections-final-tests.log`: clean native/Web normal/retry reflection gate,
  actual compiled Off/On/Off mesh/CPU/ribbon/GPU replay checks, unchanged simulation
  dispatch/resource generation/main counts, Viewer default/toggle/Party routing and
  58 native graphics/pointer/audio-focus checks. The backdrop depth-restore source
  guard supplements actual visual evidence; draw counters alone do not prove occlusion.
- `vfx-reflections-depth-build.log`, `vfx-reflections-depth-viewer-build.log`:
  corrected native compiler/runtime/VSIX and Viewer builds pass. Full Web remains valid
  from `vfx-reflections-viewer-build.log`; the later correction changed native code only.
  `vfx-reflections-format-check.log`: 433 tracked SMILE sources pass. Earlier build/test
  attempts exposed missing forward declarations and a required test UI argument; both
  were corrected. The first depth source-guard regex was corrected before the final pass.
- Native before: `native-reflected-hammer-washout.jpg`; matching fixed view:
  `native-floor-depth-fixed.jpg`. Enlarged fixed On/Off pair:
  `native-floor-neon-{on,off}.jpg`; blue flame: `native-floor-blue-on.jpg`.
  Foreground Chrome: `chrome-floor-neon-{on,off}.png`, `chrome-floor-blue-on.png`;
  Arin flame On/Off pair: `chrome-floor-vfx-{on,off}.png`. These are tool observations,
  not additional human acceptance. The separate human observations are recorded above.
- `vfx-reflections-depth-vsix-install.log`: final VSIX 2.0.61 installed; all 35 bundled
  payload hashes match, including the corrected native runtime. Installed root:
  `C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/iamzxrcj.q5i`.
  No user Visual Studio window was closed. Reuse this installation for documentation edits.

Final artifact SHA-256:
- Native Viewer: `5DFF54A31FCDB714DF9AE6BB4EC514C7FE426B041E68EFCCCF65CA8B082A8F0C`
- Full Web index.html: `389E7A71A5EEF5E7CE413B4921C78D45BCB61C96551E2DD7C1ABCF3F46A51CB8`
- VSIX: `B1C56E81390EEB1F8178F65FF376620F25268870CC101AF371C75CFB593DD976`
- Native runtime library: `9121BD3489CDE6F10E58D2CDD4DDA6531723F4B0FD90AAD16484C91E1EA9372C`

Both final live-save exports retain Arin's 24-key SHA and Orin's zero-key SHA listed
above. Models, animations, descriptors and Program.smile remain unchanged. Local Full
Web output is updated; no upload to sincioco.com is claimed. Sin Star I's native preview
and Chrome tab closed normally after the accepted gesture checks. No unrelated suspended work.
Sin subsequently asked to leave Sin Star I's towns, battle scenes and in-game character
viewer alone because he may redo them. No new game edits/builds are planned; only the
existing artifacts were reused for the completed preview acceptance observations.

The validated milestone was pushed normally and remote main read back as
`c7f8f07c5c760668bcfe41e3cf72dac45081fd7c`. Both save exports remained unchanged.
The reused native preview and Web game.js match the recorded B507E3A5 / DB34B6E2
artifact hashes above; no rebuild was needed for their accepted checks.
Closeout disposition: all required observations and authorized implementation/delivery
work are complete. No new runtime patch, rebuild or installation accompanies this
documentation-only acceptance update. Next action after publication: STOP; any new
feature requires a separate task decision.

## September 9 post-closeout: Dragon breath and shared fire capacity

Double acceptance above remains complete and published in `c06e462`. Sin then
reported missing Dragon breath on native and Web, requested Orin's blue flame
match Arin's intensity, and approved a twelve-emitter scene budget. These are
bounded Viewer/shared-VFX follow-ups, not a reopened Double or Sin Star I phase.

The old seven-slot FireEmitter3D pool was exhausted by Arin's four equipment
emitters, Orin's two and Dragon mouth heat. Breath/projectile admission needed an
eighth. FireEmitter3D now derives storage from MAX_EMITTERS=12; native and Web
permit 64 shared CPU particle batches so twelve four-batch fallbacks fit. The
8,192 staged-particle and 32 GPU-system ceilings are unchanged. Reflections replay
accepted submissions and allocate no emitters. Four spare Fire slots are headroom,
not proof that unbuilt characters/effects meet all resource or frame-time budgets.
ViewerDragon retries transient capacity rejection and clears its failed state on
explicit cleanup. OrinStorm's existing Blue Flame parameter is now 200 at 100%
UI, matching Arin; contour, palette, other styles, controls and shield remain intact.

Validation (logs under `artifacts/temp`):
- `dragon-fire-contract-check.log`: native/Web twelve admissions, thirteenth
  rejection, last-emitter particles, shared-pressure rollback and complete teardown.
  `dragon-fire-native-fallback.log`: same native contract with GPU shader creation
  forced to fail; all twelve use the existing CPU fallback.
- `dragon-fire-batches.log`: native/Web M6 batch/query/queue/lifecycle, 1,024-instance,
  HDR/direct-LDR checks pass with the shared 64-batch ceiling.
- `dragon-fire-viewer-contract.log`: isolated native calibration/Dragon continuity
  and Web production-actor subset pass, including all eight real scene emitters,
  forced full-pool rejection/recovery and actual recovered-breath draw submission.
  Test setup was corrected to invoke the existing Orin update and switch away/back
  for cleanup; same-tab selection intentionally does nothing. The initial multiline
  assignment lacked parentheses and was corrected before these passing runs.
  The Web fixture safely ignored an old publication manifest from a prior temporary
  application identity (SML3605); normal Viewer publication had no such warning.
- `dragon-fire-format-check.log`: all six changed SMILE sources pass. Final whitespace
  normalization changed no semantics; numeric evidence is reused. Final native/Web
  Viewer builds are in `dragon-fire-{native,web}-viewer-build-final.log`.
- Tool observations: `vfx-closeout-evidence/chrome-dragon-breath-before.png` lacks
  the plume at 1592 ms; `chrome-dragon-breath-after.png` shows it at the same pose.
  `native-dragon-breath-after.jpg` shows breath aimed at Orin at 1586 ms. Both include
  floor reflection. `native-orin-matched-flame.jpg` and `chrome-orin-matched-flame.png`
  retain hammer metal/grip detail at 100%. These are tool observations, not new human
  acceptance. Captures precede final whitespace-only normalization; Web output is
  byte-identical. Existing Double, reflection, asset and calibration evidence remains valid.
- `dragon-fire-vsix-install.log`: installed VSIX 2.0.61 and verified all 35 payload
  hashes in `Extensions/dodz1klk.udk`. The completed task-owned debug solution was
  saved under artifacts/tests and Visual Studio closed normally before installation.

Final SHA-256: native Viewer `A068B46965369F17857B7203CEC3EE8FE536880A709FEE6EB05C159B302C7E6A`;
Full Web index `9D341782D900630870B9D763C862D624421305CACCA853E59E62574EF3D2371C`;
Web game.js `5549CD3761E13E638E7A276723476920B255A7E735D3973D6335058C3B119A9B`;
VSIX `2FA2D0B5DDAE613595B299ED336550241E566A52F2040DD13896443B7F7C7347`;
native runtime `0A221352DDED29F358BDEAFE740CE85018159773F7A7D9FDC4B72A2633627A21`.
Arin's 24-key and Orin's zero-key canonical hashes remain the recorded 7A3E7BC8 /
13AE135F values. No model, descriptor, game project or Program.smile change.
Local Full Web output is current; no public-site upload is claimed.
Published normally as `a807326efb7d1c6e23cc3936da57c39a4da551c5`; remote main
was read back at that hash and the worktree was clean. At Sin's explicit request,
YouTube Live was ended and visually confirmed as "Stream Finished" / "Offline".
Meld Studio was taken offline and its "Go live" button replaced "Go offline";
the live timer and output bitrate disappeared. No stream configuration was changed.
Next action: STOP. No outstanding work in this follow-up; held features remain held.

## On Hold

Semantic-inspection CLI, Battle Scene Editor,
subject-first syntax, SMILE 1.0 changes, further Sin Star I updates and unrelated suspended features
remain deferred.

Sin explicitly released loader/splash enhancements on September 9, 2026. That
separate milestone follows [its own checkpoint](startup-presentation-checkpoint.md);
the accepted Double implementation and human observations remain closed.
