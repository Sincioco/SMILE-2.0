# H6.1 Hardening and Web Parity Ledger

Scope: W0-W6 from the user-supplied H6.1 package. No Battle Scene Editor E0-E12
work is authorized or started. Historical H6 `PASS-NATIVE` remains historical;
H6.1 is **IN-PROGRESS**, with real-browser acceptance still outstanding.

Browser scope update from Sin after intake: use the installed Chrome and Edge,
with browser tests visible on-screen. These are two Chromium-family browsers,
not independent rendering engines. Firefox is no longer an acceptance target
under this explicit user revision; its incomplete diagnostic runs are retained
as historical observations, not claimed as passes. The original package's G10
Firefox requirement must be shown as superseded in the final gate, not silently
removed. Edge extension connection was verified by opening and reading an
Example Domain tab on 2026-09-05. No browser permission settings were changed.

## Baseline and preservation

Reviewed baseline: `902a7022c895bf97010d979ea578fc5361cdcbf4`. Actual branch is
`main`; no reset/rebase was performed. Package identity/hash-checked intake and
full current Arin/Orin package copies are retained under ignored
`artifacts/temp/codex-handoff/2026-09-05-smile-2.0-h6-1-hardening-and-web-parity/`.
The `preservation-start-902a7022` copies retain the pre-edit assets and calibration.

At intake Arin retained 23 keys and snapshot SHA-256
`1747367DD5E411D8230AB5159DE1309F221867C8DE6745661DA1396EAE6DB867`.
Orin retains zero saved keys, its distinct storage key and clip identities; the
explicit Jump Attack asset migration updates only its asset hashes/fingerprint.
The historical zero-key snapshot is not restored over a newer save.

## Validated milestones and current work

| Milestone | Commit | Actual commands/results | Remaining work |
| --- | --- | --- | --- |
| W1 safe camera math and urgent Orin shot fix | `5c2036afb4435fb7375d6515f15491746bdf5560` | Native/shared generated-Web reference tests passed; attack camera no longer follows unbounded animated-model bounds. | Real-browser camera evidence remains required. |
| W2 scene-owned comfort and frozen ownership | `b8fce49701738fcab3c45d7d5cdb343e1e4a9b33` | `scripts/test-character-3d-viewer-actor-isolation.ps1`: native active two-Orin/GPU and forced fallback plus generated-Web exact-output checks passed. | Real-browser integrated effects observations remain required. |
| User-reported Viewer regressions | `0768860e59e60c6231e0992f23b34a3a62c48483` | `tools/Character3DViewer/Build.ps1 -Configuration Release`; `scripts/test-character-3d-viewer-hardening.ps1` **without** `-NativeOnly`: PASS, including seeded native calibration/tab loads, generated-Web hardening and 58 native graphics/input/audio checks. `scripts/test-character-3d-viewer-actor-isolation.ps1`: PASS after repaired Orin asset. Scoped formatter/style checks passed. | Direct off-window mouse scrub and comprehensive camera interaction evidence still required. |
| W3/W4 Web renderer and calibration publication | `519e780cc723cc6beb514edeb1278370abd8cc19` (pushed) | Compiler/VSIX build and installation, focused native/VM tests and visible Edge Viewer/Labs checks below passed. | MSAA, remaining browser workflows and final W6 acceptance remain. |

## Viewer regression checkpoint

- Jump Attack: surgical GLB Root translation replacement grounds sample 37 through
  71 at the measured Idle sole height (3/1000 model units), preserving samples
  0-36 and other clip/mesh/material/skin data. A broad rebuild changed accepted
  Death data and was rejected; only the surgical repair is retained.
- Repaired Orin GLB SHA-256:
  `3100F1ACB2E9B1F3776E8A94C113BB23C0A90A7141590A0936958991CC99DC0C`.
  Cooked SM3D: `1741564CDBD4F3ADB305AACD1AAD295B373672D34E1490C103601E8BD4BF8DAF`.
  Current zero-key snapshot: `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
- Fixed stale Orin runtime fingerprint after migration. The old test's empty-save
  fixture missed companion rejection. It now seeds isolated copies of current
  canonical Arin/Orin saves and loads Arin, Orin, Dragon and Party.
- All tabs retain above-solid-floor camera placement; grid-only permits underside
  inspection. Timeline scrubbing captures outside-window motion/release exclusively.
- Party rotates Arin's two and Orin's three attacks. Boss-first/extra guard beats
  and one latched dragon aim/hit/KO target improve presentation consistency.
  No live combat damage, MP, rewards or game-save mutations are introduced.
- Native `tools/Character3DViewer/bin/Character3DViewer.exe` relaunched through
  `Launch.ps1`; agent observed populated Dragon and Party scenes. Sin then explicitly
  reported: “I can visually see all 4 tabs work again.” This confirms tab loading,
  not blanket artistic approval or completion of the browser gate.

The Viewer regression commit exported both calibrations and was pushed. Web
compiler work was kept separate until actual browser validation below.

## Web renderer and packaged calibration checkpoint — September 5–6

Source parent: `0768860e59e60c6231e0992f23b34a3a62c48483`, branch `main`.
Implementation SHA: `519e780cc723cc6beb514edeb1278370abd8cc19`; this is not W6 acceptance.

- Ported existing backdrop, animator node-offset, object pivot/cull and independent
  equipment-offset dispatch through WebGL2. Shared models do not own mutable edits.
- Ported thermal force/turbulence/evolution/bounds/render setters, GPU soft-depth
  and heat composition. Real-browser failures found and fixed: reserved GLSL
  identifier, transform-feedback buffer binding order, backdrop orientation and
  implementation-dependent linear shadow comparison filtering.
- Edge exposed Lightning Ultra silently falling back to Basic because Web capped
  all systems at 8,192. GPU/Auto now match native's 16,384 limit; explicit CPU and
  shared 32,768 limits remain bounded and have rejection tests.
- Sin reported absent Arin corrections in Web. Normal project publication now
  includes validated SMKF defaults from current canonical JSON, using the existing
  serializer. The shared Viewer loads defaults only with no working/legacy save.
  There is no model bake, live-save replacement or browser-to-repository write.
- Fresh backups: ignored `artifacts/temp/codex-handoff/h6-1-web-defaults-20260905/`.
  Arin remains 23 keys at the unchanged hash above; Orin remains zero at its current
  migrated hash above. Default payload hashes are Arin
  `9DD73F6BFCCF3A9F3AB9E10860D6DECD42CE917F99B6D37C2FB69EA076656508`, Orin
  `4CCBE1D6B5E60964D6D9AF2802978970D4293C44F874B60AC0B44A8C86341819`.

### Actual focused evidence

| Command or observation | Result |
| --- | --- |
| `scripts/test-viewer-calibration-native.ps1` | PASS: both defaults match every canonical payload byte; a saved edited track wins over defaults; seeded four-tab loading, save and previous-good backup checks pass in isolated application storage. |
| `scripts/test-character-3d-viewer-hardening.ps1` without `-NativeOnly` | PASS: 42 calibration checks, updated native calibration fixture, generated-Web hardening, 58 native graphics/pointer/audio checks. |
| `scripts/test-renderer3d-gpu-particle-webgl2.ps1` | PASS: real generated runtime in VM/GL double, 16,384 GPU admission, CPU/scene-capacity rejection, forced shader/attribute fallback. Not browser rendering proof. |
| `scripts/test-lightning-vfx-foundation.ps1` | PASS native and generated-Web exact output. |
| `scripts/test-renderer3d-post-processing.ps1` | PASS native and generated-Web normal/HDR/shadow fallback. Its historical MSAA label is not proof of Web multisampling; Web still reports one sample. |
| `scripts/format-smile-style.ps1 -Files ... -FormatLongIf` | Four changed SMILE files passed transaction preflight; no formatting changes needed. Initial CLI comma-list invocation was rejected without edits, then corrected to a PowerShell array. |
| Visible Edge 146.0.3856.62, aligned by Sin with the Viewer | All four tabs and both Labs rendered; warnings/errors returned empty from browser diagnostics. Fire High showed GPU, five systems and 399,360 GPU bytes; paused fire stayed visible. The Fire toggle is emission enable/disable, not immediate visibility removal. |
| Edge after calibration publication | Arin Attack displayed 13 keys, green ticks and saved channel values. The isolated Web fixture printed exactly `Viewer calibration isolation passed`. No user artistic approval is claimed. |
| Edge after Lightning capacity fix | Backend 2, pool 16,384, 3,932,160 GPU bytes, active sparks and no console/shader warnings. |
| `tools/Character3DViewer/Launch.ps1 -Build -SkipWindowActivation` | Built and launched current `bin/Character3DViewer.exe`, PID 51508. Both live saves retained; watchers started. Supported Windows control restored foreground and confirmed populated Party rendering. |
| `scripts/install-vsix.cmd` | Full build and install PASS, VSIX 2.0.59, instance `91f001b5`; no Visual Studio editing process was open. |

Installed extension DLL SHA-256:
`B5137FFCC66B52D5BB9EBBC1DECAA462D2A872B46265B2B6AD5A7150A95C89D6`.
VSIX `artifacts/vsix/Smile.VisualStudio.vsix`:
`EC966348A29A272E20576F51B68D7D7321CB89F450BD8EADEF00247AF8D40E95`.
Installed/built compiler, shared language and native runtime payload hashes matched:

- `smilec.dll`: `EE0F5C4AF1B2AEAC58F9DE8F727CBE86020A7CAF747BAFD1A01B8C8193F11865`.
- `Smile.Language.dll`: `D1C0CF65EF2A4A665B3DE570EC4C9F4BF2D1AB62DAE3DDC9BAFDE490B81D996A`.
- `Smile.NativeRuntime.lib`: `4BE4122889512FC6BBE4E39B3767470FCFA217278CF3D00A9899A70C5F2C4599`.

GitHub's current open-issues endpoint returned `[]` on September 6 (Taipei).
This does not close repository-discovered issues. Initial PowerShell array wrapping
misreported an empty response as one container; raw JSON was checked explicitly.

### Remaining work / next actions

Web scene MSAA, focused-canvas keyboard/shortcut ownership, accessible fullscreen
and audio activation, strict current-snapshot import/download and storage recovery,
complete context/resize/interaction evidence, refreshed Chrome checks, wider normal
smoke, complete issue/parity reconciliation, portable deployment package, final
H6.1 report/gate and final VSIX refresh if payload changes. No E0 work has started.
Earlier Chrome/Firefox diagnostics are not current-final-browser acceptance. The
native Viewer was left in the foreground after the visible Edge checks.

## September 6: rotation, timeline pause and current Death pose

Source parent: `519e780cc723cc6beb514edeb1278370abd8cc19`, branch `main`.
This checkpoint is not final W6 acceptance and introduces no Double or E0 work.

- Web Euler signs were reversed relative to native. Corrected the shared Web
  matrix builder for object, animator-node and world-pivot transforms. No model,
  profile, pose constants or SMILE numeric semantics were changed.
- Added actual Character3D socket tests against shared Math3D for axis/combined
  rotations, node offsets, world pivots, scale and translation. Before rebuilding
  the compiler, native passed but the old Web runtime produced 30 mismatches;
  after the fix `scripts/test-character3d.ps1` passed native and generated-Web
  normal/forced-PBR-fallback runs and Lab builds.
- Canvas-only context-menu suppression preserves the secondary pointer action.
  Actual Edge right-click reset the Viewer without opening a browser menu.
  `run-web-test.js ... --mobile-controls` passed with the published
  `Phase3ATextGame` fixture. An initial invocation on the responsive Viewer was
  the wrong fixture (640 versus expected 480 logical center); it was not a
  product regression and was rerun with the smoke suite's specified fixture.
- Timeline navigation now pauses before seeking/stepping, even with no keys.
  `scripts/test-viewer-calibration-native.ps1` passed all five directions and
  resume in isolated storage. All five buttons were also exercised in visible
  Edge after resume; the pause banner and fixed target frame were observed.
  Rebuilt native Viewer 0-Frame and Space resume were checked directly.
- Captured frame zero of all nine Arin animations on both targets at the same
  yaw/pitch/zoom using the 23-key baseline. Comparison images were being assembled
  when Sin explicitly stopped further comparison, saying Arin looked good on
  Web. Existing captures are retained under `screenshots/h6-1-arin-frame-zero`;
  no claim of a completed pixel-by-pixel comparison or blanket approval is made.
- Sin saved a new Death frame-0 key during this work. Both characters were
  exported again; Arin now has **24** keys, snapshot SHA-256
  `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`.
  Orin remains zero keys at `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
  Prior keys remain unchanged. The old 23-key snapshot was not restored.
  Native and Web publication carries Arin payload
  `CEA45992C925D6DC049548671D9FD0EE706DD9D6A460659C03A338F765047037`.
  Actual Edge next/previous-key navigation reached the newly saved Death key.
- `Launch.ps1 -Build -SkipWindowActivation` built and relaunched native
  `tools/Character3DViewer/bin/Character3DViewer.exe`, PID 26520, SHA-256
  `F31B88D073945E0949C4D82BA0BB42F885E37DF4FF3A3E51737C6F49CFD4C824`.
  The Viewer was restored to foreground and resumed after the direct pause check.
- Scoped SMILE formatter check passed. `scripts/install-vsix.cmd` rebuilt and
  installed VSIX 2.0.59 in instance `91f001b5`; installed DLL hash
  `A599BE4303527B8B159717F54FB515FB011E924590A9D62D7E837708E3CDB3A5`.
  VSIX SHA-256: `B15F5F754CDD0DEAF875ABAB35ECC5B44994C3CF79F8E44F051B0376A003FDCF`.
  Installed compiler/language/native-library hashes match the VSIX's staged
  `artifacts/compiler` payload: `DB8CCB1D67F732603D8521887F166D1DB285A4D39A46CEC396859437BBE75EC0`,
  `28D6CAB5F7AB136E2CC971D222BEDE894780BE4BD256C66B0D85F7A3F19FBBB2`, and
  `D9A7907397F90148C8E9218F1EEEE2661117CD921A774813D0EC52873EA7A871`.
  The build script recompiles the native library during its later solution build;
  that separate `artifacts/runtime` output is not byte-identical to its earlier
  staged copy and is not claimed to be the installed payload.

Next: strict current-snapshot JSON transfer, browser input/storage recovery,
remaining render/interaction/context evidence and ordinary smoke. The Web
filename still shows the native path at this checkpoint; no JSON download or
Web-to-native authoring round-trip is claimed implemented yet.

## September 6: saved JSON download and generic UTF-8 transfer

Source parent: `6b62f3b28b998f45cdbfbd1992f89a68fb062043`, branch `main`.
This is a coherent W12 download milestone, not completed W5/W6 or final acceptance.

- Added shared typed `File_Export(FileName, Contents) As Boolean` and
  `File_Import() As Text`, native user-selected UTF-8 dialogs and browser download/
  file-picker implementations. No new framework, numeric type or scene format.
  Content is bounded to 8 MiB; filenames are bounded and not arbitrary paths.
  Native export uses a flushed temporary file and replaces only the chosen path;
  browser success means request initiated, not proof of disk persistence.
- The Viewer filename no longer displays a native drive path. Native click keeps
  Explorer reveal; Web click downloads schema-2 JSON from the saved SMKF buffer.
  Unsaved temporary pose edits are not encoded or persisted by a download.
  Publication generates identity-only metadata from the canonical validator;
  both metadata files are declared project assets, bringing publication to 46.
- Initial Arin export failed the native fixture because its saved-record order
  differs from model clip indices (Death was appended). Fixed by exact clip-name
  lookup, preserving indices as hints. No character data was edited to pass.
- `dotnet run --project src/Smile.Tests/Smile.Tests.csproj -c Release --no-restore`:
  PASS, 296 checks including shared transfer signatures/types and both emitters.
- `scripts/test-viewer-calibration-native.ps1`: PASS, both exports normalize to
  current canonical JSON and round-trip through native payload serialization.
  `scripts/test-character-3d-viewer-hardening.ps1` without `-NativeOnly`: PASS,
  including 42 calibration checks, seeded four-tab isolation, generated-Web
  hardening and 58 native graphics/pointer/audio checks.
- `examples/TextFileTransferBasics.smile` compiled native and Web. An initial
  draft used unavailable `KEY_I`; switched to supported `KEY_O` without expanding
  keyboard syntax. Scoped formatter and `git diff --check` passed.
- `node scripts/run-web-test.js artifacts/web/h6-1/TextFileTransferBasics
  --file-transfer`: PASS. Covers UTF-8/BOM, bounds, invalid names, denied activation,
  cancellation, duplicate picker exclusion and shutdown URL/listener cleanup.
  These DOM/VM checks are not actual browser selection evidence.
- Actual Edge download: `Downloads/arin-v5.7-pose-calibration.json`, 6,596 bytes,
  SHA-256 `168E229660185EA5611519232B694FC83564F75B73EAC76D5F4EEAA500B3BBF1`.
  It normalizes to all 24 canonical keys. The native sample's real Open and Save As
  dialogs reproduced those bytes in `artifacts/tests/native-transfer-roundtrip.json`.
  Native cancellation preserved sample contents; the sample never loaded Viewer
  Save Data or applied a calibration. The native Viewer was restored afterward.
- Browser sample opened a chooser, but automated `setFiles` returned `Not allowed`.
  No bypass was attempted. Live browser file selection/import is still unverified;
  the disposable sample tab was closed. This is a tool limitation, not a passed test.
- Export/Compare for Arin and Orin both passed again. Arin remains 24 keys at
  `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`;
  Orin remains zero at `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
  Canonical GLB, descriptor and profile identities are unchanged.
- `scripts/install-vsix.cmd`: PASS, installed 2.0.59 in instance `91f001b5`, folder
  `yko5a5kh.4uf`, DLL SHA-256
  `A0EEBB8E582EE18E411A44B852F65A7766FE52021BB306294A30BB2930595B08`.
  VSIX SHA-256: `CD8F501F9A9C8395C88C9669649DF5F5019DC97143758B4DA25A8D57F5BBB21E`.
  Installed compiler/shared-language/native-library match staged payload hashes:
  `80B1FE55B6AB6C2621B47F9AB69A0F493CBAB8F1EF2C873BF82E371D78AB2649`,
  `C0671420B1B5743EBA36026023FD99C833FE1B9CA401C8C532C07F4E27B6D704`,
  `800BB11558DF77DEA45F624CB32A68DF0E634C03F723DB0348FDFD549EDA618A`.
- `CalibrationTransferTests` generated from the actual Viewer procedures passes
  exact native/Web console parity, including both complete JSON snapshots. The
  native harness initially omitted Print's final newline in its log; corrected
  log preservation. The existing `--renderer3d` option also demands a presented
  scene, which this state-only fixture does not provide. Added the explicit
  `--renderer3d-state` option to supply the GL double for model state without
  asserting visual presentation; existing render assertions remain unchanged.
- Rebuilt native with `Launch.ps1 -Build -SkipWindowActivation`, after a normal
  UI close and confirmation that the old process exited. PID 33292, executable
  SHA-256 `54A7C2CB05D6049E0576BC2C66924988CF67108C502BEE7AD8B0376379DF6E80`.
  Party rendered after launch; Arin's native filename label was observed and the
  Viewer was left in the foreground. Both live saves retained precedence.
- Published `artifacts/web/h6-1/Character3DViewer` from the current project with
  46 assets. Republished `Phase3ATextGame` with the current runtime and reran
  `--mobile-controls`: PASS. No remote website deployment was performed by Codex.

Next: strict atomic in-Viewer JSON import and W14 storage recovery, then remaining
MSAA/input/lifecycle/browser evidence, ordinary smoke, deployment ZIP and final
H6.1 gate. No claim of completed Web authoring round-trip, final PASS or E0 work.

## September 6: W14 checked storage and transactional calibration checkpoint

Source parent: `a375c83500726970487a5ea0e266e471438d52f3`, branch `main`.
That parent is the pushed JSON-download milestone above. Fetch before this
checkpoint found HEAD/origin/main aligned (0 ahead / 0 behind). Only this
milestone's changes are staged; current model/calibration data remain unchanged.

- Added optional `Status <writable Number target>` to existing Save Data/Load Data
  in the shared parser/AST/semantics/module lowering and both emitters. Status is
  contextual, so existing identifiers still compile. Shared DATA_STATUS constants
  distinguish success, missing, recovered, invalid, unavailable, corrupt and
  destination-too-small. The language reference includes the exact contract.
  Strict statements still fail on corruption; checked failures preserve the
  destination and set Count to zero. Native Count/Status assignment order matches
  Web, including writable array/ByRef targets.
- Checked native/Web loads can read verified `.bak` envelopes for missing/corrupt
  primaries without rewriting them. A subsequent checked save preserves the good
  backup when replacing a corrupt primary. Web writes persistent storage before
  updating memory; denied/quota failures cannot turn an unsaved candidate into
  the runtime's saved copy. Native temporary cleanup only removes files created
  by that operation. No cross-tab/process merge or locking is claimed.
- Viewer Save/Undo is transactional. Failed Save Frame preserves the temporary
  preview for retry/cancel but restores the saved key track/JSON. Failed Undo keeps
  the undo entry. Failed loads block writes rather than falling back to defaults
  over unknown data. Visible failure/recovery status uses the existing HUD.
- `dotnet run --project src/Smile.Tests/Smile.Tests.csproj -c Release --no-restore`:
  PASS, 297 checks. Test drafts initially omitted explicit scalar types in a strict
  module fixture; corrected the fixture, not the language contract.
- `scripts/test-smile-formatter.ps1`: PASS, 13 focused checks. Repository-wide
  `format-smile-style.ps1 -Check -FormatLongIf`: PASS, 382 tracked sources; scoped
  check of all five changed/new Viewer/example sources also passed.
- `scripts/test-data-status.ps1`: PASS with real filesystem sharing denials,
  missing/corrupt/oversized primary and backup, unchanged destination on failure,
  previous-good preservation, successful recovery/save and no leaked temporary
  files. Latest disposable application:
  `smile.tests.data-status.run-fc659e123f6246e28c27819e3519075d`.
  Only an exact disposable primary file was deleted to test missing-primary
  recovery; test evidence and its unrelated-to-user application folder are retained.
- `node scripts/run-web-test.js artifacts/web/h6-1/DataStatusBasics --data-status`:
  PASS, disposable VM storage denial/quota/corruption/backup/fresh-runtime tests.
  This includes backup-write versus primary-write failure, strict legacy rejection,
  corrupt/oversized data, and cache atomicity. Not actual private-mode/quota UI proof.
- `scripts/test-character-3d-viewer-hardening.ps1` (Web not skipped): PASS, 42
  calibration checks, actual native four-tab/edit/Save/Undo isolation, generated-Web
  hardening console parity and 58 native graphics/pointer/audio checks.
  Latest native isolation app: `smile.tests.viewer-calibration.run-ec721b1e01084d1a8d5703bf3f69700a`.
  The isolated native test uses a directory at one probe filename to force failure;
  no live native save is corrupted/locked/deleted by the fixture.
- Published `CalibrationStorageTests` from the preceding isolated Viewer harness
  and ran `--renderer3d-state --deny-data-key "Viewer Denied Storage Probe"
  --native-output artifacts/tests/ViewerCalibrationIsolation/native.out --timeout
  60000`: PASS, exact native/Web output including both full canonical JSON exports
  and failed Viewer Save/Undo assertions. This is state/console evidence, not GPU
  visual or real browser denial evidence. The fixture's per-run app identity is
  intentionally disposable.
- Existing `DataKeyIdentity` native/Web exact console checks and
  `test-phase4-data-envelope.ps1` passed; strict corrupt load still exits 2 with
  a diagnostic. Teaching example `DataStatusBasics`, both Read/Write probes and
  the language-reference snippet compile for native/Web. The teaching example
  executes with Saved, Loaded bytes 2, rejected-invalid-byte True and preserved True.
- Actual Chrome and Edge at `http://127.0.0.1:8765` use separate test-only
  `smile.tests.web-data-status.run-september-six` storage. Both showed initial
  missing `1,0,99,98`, successful write `0`, and persisted `0,2,17,23` after refresh
  and closing/reopening the test tab. Browser creation initially selected a
  background tab (Chrome's window was minimized); brought each test tab visibly
  forward and verified its rendered output, with foreground write/read checks.
  This proves tab reopen, not termination/restart of the entire browser process.
  Closed only the disposable tabs and returned the native Viewer to the foreground.
- Both character Export/Compare checks pass: Arin 24 keys including Death frame 0,
  JSON `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`;
  Orin 0 keys, JSON `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
  No model, descriptor, profile identity or accepted calibration bytes changed.
- Native rebuilt/relaunched through `Launch.ps1 -Build -SkipWindowActivation`
  after normal UI close and confirmed process exit. New PID 47188; executable
  `tools/Character3DViewer/bin/Character3DViewer.exe` SHA-256
  `E891B76E3E31ECD51E971B90BB6260DAB36FAE2FA570192BB737B3BC4F8700B0`.
  Published current Web Viewer to `artifacts/web/h6-1/Character3DViewer`, 46 assets.
  Observed the rebuilt native Party scene playing with both heroes and the Dragon,
  without a recovery/error overlay; left the Viewer in the foreground for the stream.
- **VSIX installation BLOCKED, not passed.** `scripts/install-vsix.cmd` rebuilt
  the package but failed removing an old locked `Smile.Language.dll` under
  `Extensions/yinvwrss.gto`. A read-only UI inspection found unsaved `Web.config*`
  in Visual Studio; it was not closed/discarded. Asked Sin to save and close it.
  Built VSIX SHA-256:
  `70488DEE46F87554052E2D1664922215071B643D79D988E7EE393D471EB7A51B`.
  Built staged compiler/language/native-library hashes (NOT installed verification):
  `1D0CD2FADB665F44299B3929B0F4D0B3E8BE28EB4AC92907CDA884988BA06784`,
  `1351433D0CD54CDEE1E08BE5124F5A9D28DA5A670FE268EFB3B92E6E0247903E`,
  `0070E3F4B1588D39D6174D8E235BC13F4B0D6CE1A7CB63B40B038A71F48CFD0D`.

Next action requiring Sin: save/close Visual Studio, then rerun the repository
installer and hash verification. This checkpoint does not complete H6.1/W14.
Remaining work includes validated in-Viewer JSON import and its real selection
proof, Viewer-origin save/reload evidence, MSAA, canvas keyboard/fullscreen,
audio/lifecycle/mobile emulation, remaining multi-actor live evidence, wider smoke,
deployment ZIP/manifest and the final readiness gate. No E0–E12 or Double work.

## September 6: VSIX blocker cleared and normal tool output layout

Parent/source checkpoint: `2cb6c523a43bb71e8f113b93bf426b1f43db1f1e` (pushed).
Resumption found a clean `main`, fetched `origin/main`, and confirmed 0/0 divergence.
Visual Studio was no longer running. `scripts/install-vsix.cmd` then completed:
the repository verifier removed the proven orphan `Extensions/yinvwrss.gto` and
installed version 2.0.59 under `Extensions/yzuphacw.t3f`. No user document was
saved, closed or discarded by Codex. The old orphan's assemblies are replaceable
from the built package; unrelated extensions were not removed.

All five corresponding installed entries were compared directly against the
built ZIP's entry streams (not against a later rebuilt native archive):

| VSIX entry | Installed/package SHA-256 match |
| --- | --- |
| `Smile.VisualStudio.dll` | `812E60D391499A2419DC984B5FC0993B74146ABBF252AA988169BAE86F93B838` |
| `Smile.Language.dll` | `4D7CFBB8589F80D35EF5623CE2BC936BB4B74FD4EDD02695032A915FD9B8971B` |
| `Compiler/Smile.Language.dll` | `0012711C3DFBBC4E26B8D8C21C3BAE18A0E1025947822168056823796CAB9B41` |
| `Compiler/smilec.dll` | `7BE7A9E0B4FD04947BA7743300524BF116F6CD6F16D8DAE54FB6A83FF6E3FCEF` |
| `Compiler/Smile.NativeRuntime.lib` | `EE5EBF52F1A0B0EAA34511213ECBD0A07357A98F2D962A10AD8443980754FD77` |

VSIX: `artifacts/vsix/Smile.VisualStudio.vsix`, SHA-256
`4AC624A3783BB9E788F7D7AFE8208D892D27AB2C0B6DD27776E6BB7B02045271`.
The build-layout changes below do not change the VSIX payload or language syntax.

Sin requested normal configuration-local builds for the Viewer and both Labs,
and asked to launch the Web Labs and provide all three publishable locations.

- All three `Build.ps1` scripts default to `-Configuration Release -Target All`;
  Native precedes Web. Debug and target-only builds are selectable. Native output
  is `tools/<Tool>/bin/<Configuration>/<Tool>.exe`; Web is the adjacent `Web/`.
  Existing Lab `-OutputPath` overrides remain native-only. The focused native
  thermal script explicitly requests its existing Debug/native fixture.
- Viewer `Launch.ps1` defaults to Release and honors `-Configuration Debug`.
  Its `-Build` rebuilds that native configuration, not another executable.
  Missing/custom-build overrides are rejected before export/closure/launch.
  Asset, application, storage, and character identities are unchanged.
- Executed each tool's `Build.ps1 -Configuration Release` and `-Configuration Debug`:
  all twelve native/Web outputs PASS. Each output has its normal asset manifest;
  every native/Web asset pair matches by SHA-256 (Viewer 46, Fire 6, Lightning 5).
  All five Web bootstrap files exist in each publication. PowerShell parsing and
  `git diff --check` pass. No broad soak or full smoke was needed for path-only work.
- The old flat Viewer was closed normally through supported Windows control
  after re-observing overlapping input. `Launch.ps1 -SkipWindowActivation` launched
  PID 55044 from `bin/Release/Character3DViewer.exe`; the Party scene rendered.
  Both synchronizers kept their live working copies. The exact old flat executable
  was moved recoverably to ignored
  `artifacts/temp/codex-handoff/viewer-layout-legacy-2cb6c52/Character3DViewer.exe`
  after verifying its previous hash and that it was no longer running. Old test
  publications, Debug outputs, assets and other user files were not deleted.
- Release executable SHA-256: Viewer
  `C66EC9541E8E0794DB6BC4EF9FA39E09DFDC90A28B572C70EF3E6E4CD82B7F16`, Fire
  `DCD4E44443E6A517011B540682CF9D0D346F96C9E1AC3EEAA0A3E57CA92D0000`, Lightning
  `39F61D59127D244A41116D06AD1DADB03DE4CF0E14E4F2DC4C5FC9C8EBCBA82E`.
- Served the three Release/Web folders with `python -m http.server` bound to
  `127.0.0.1`, ports 8766 (Viewer), 8767 (Fire), 8768 (Lightning); all returned HTTP
  200. Existing port 8765 was left alone. Chrome rendered both requested Labs:
  Fire High GPU/five systems/399360 bytes/error 0; Lightning GPU/backend 2,
  pool 16384/3932160 bytes and visible bolts. Web AA still reports 1 and remains
  an open H6.1 item. Edge's connection was unavailable at this check; these new
  launch observations are Chrome, not claimed as new Edge acceptance.
- Export/Compare preserves Arin's current 24 keys including Death frame 0 and
  Orin's current zero-key snapshot at the unchanged W14 hashes. Added exact
  pre-import backups under ignored `h6-1-json-import-preservation/Arin` and `Orin`.
  The synchronizer now verifies both existing Release/Debug cooked mirrors.
  `scripts/test-arin-calibration.ps1`: 42 PASS in isolated storage; Orin `-Mode
  Validate` also passed with its existing profile fingerprint.

The three README files identify the complete publishable `Web/` folders and
explain origin-scoped storage: a new port does not inherit or erase previous
browser saves. No remote website publication was performed. The Labs' live tabs
are user-requested outputs, not a claim that every H6.1 browser test has passed.
Next: resume strict atomic JSON import and the remaining W0-W6 work listed above.
The stream must remain live until all approved work genuinely completes.

## September 6: W12/W14 snapshot import implementation (in validation)

Source parent: `909fd6acf8eb80e370a88ea4f024583faffaa33e`, main/origin/main
aligned and clean before this work. This entry is a checkpoint, not a final gate.

- Added the bounded, tool-local `CalibrationJson.smile` reader using existing
  SMILE syntax and Character3D metadata. It produces a candidate SMKF payload but
  does not mutate actors or storage. The Viewer validates first, asks for a second
  Replace Keys confirmation, checks the saved baseline, and uses the existing
  save/rollback/Undo transaction. It pauses movement and refuses to discard an
  active unsaved edit. Profile changes cancel pending confirmation.
- Strict checks include exact metadata, named/unique clips, complete 20-channel
  keys, integer/vector/flag/frame bounds, saved-frame references, counts, duplicate
  and unknown fields, malformed/trailing data and the 8 MiB input bound. Printable
  ASCII identity strings allow equivalent JSON escapes. Current schema only;
  rejected working storage is not silently replaced or migrated.
- The first draft compile exposed unsupported pre-test loop syntax and reserved
  identifier names; corrected the source to existing Do/Loop Until syntax. No
  language extension was added. An oversized *test-local* fixture array exceeded
  the native stack; moved that fixture buffer to shared test storage. Subsequent
  failed byte-order assertions were corrected to preserve the packaged-byte check
  before import and compare the full normalized post-Undo snapshots by name.
- `scripts/test-viewer-calibration-native.ps1`: PASS, including 42 malformed JSON
  cases, both current canonical snapshots, reordered properties/clips/index hints,
  cross-character rejection, oversized input, validation without writes, changed
  baseline rejection, actual denied persistence and successful replacement/Undo.
  Both final JSON snapshots match canonical normalization and native serialization;
  native primary and backup checksum/profile checks pass. Latest disposable app:
  `smile.tests.viewer-calibration.run-a3e73f19f1084e589f24e3a80aed922b`.
- Published that same isolated project to
  `artifacts/web/h6-1/CalibrationImportTests`; `node scripts/run-web-test.js
  artifacts/web/h6-1/CalibrationImportTests --renderer3d-state --deny-data-key
  "Viewer Denied Storage Probe" --native-output
  artifacts/tests/ViewerCalibrationIsolation/native.out --timeout 60000`: PASS,
  exact console parity. This is VM/state evidence, not real file-selection proof.
- Exported both live snapshots unchanged and made exact backups at
  `artifacts/temp/codex-handoff/h6-1-json-import-resume-909fd6a/{Arin,Orin}`.
  Arin JSON remains `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`;
  Orin remains `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
- Built a separate interactive native/Web Viewer with disposable application
  `smile.tests.viewer-import-ui.run-582f047e9ad44dd29175fbeb5a15be5b`. Native opened
  and the Import JSON button showed the real UTF-8 picker. Overlapping keyboard
  input was observed; requested an idle-input interval before continuing UI tests.
  No native or browser file-selection acceptance is claimed at this checkpoint.

Next: complete actual native/browser import, refresh/reopen and Undo checks;
rebuild normal Viewer outputs, finish milestone validation and commit/push. The
remaining MSAA, input/fullscreen, audio/lifecycle/mobile, integrated multi-actor,
wider smoke, deployment ZIP and final H6.1 gate remain unfinished. No E0 or Double.

## September 6: resumed Party inspector, transfer UI and publication cleanup

Current source parent is `1747003`, main/origin/main 0/0 at resumption. This is
ongoing work, not a gate pass. Sin requested automatic resumption after input,
completion of existing prerequisites first, then a separate reusable Visual
Studio **Web - Optimized** platform and builds of all three tools. Keep normal
Web full fidelity; the lossless-only constraint still applies. That new platform
is authorized but has not been implemented. No Battle Scene Editor or Double.

- Strict transfer UI now uses Import Key Frames and Download Key Frames to the
  left of the JSON filename. Earlier actual isolated native picker selection and
  replacement succeeded. Actual Chrome downloaded the canonical 24-key snapshot,
  imported it, deliberately replaced only disposable browser test storage with
  an empty snapshot, and Undo restored all 24 keys. Downloaded restored data was
  compared with all canonical channels and identities. Distinct imported-state
  refresh/reopen evidence remains pending. No live native keys were replaced.
- Party now binds the shared inspector temporarily to its active actor; scene
  advancement stays outside the UI binding. Hero pose storage remains separate;
  Dragon exposes timeline controls only. Native isolated tests passed before the
  final Enter-key/layout/gizmo follow-ups; those changes are being retested.
- White headers were built for all three tools. One native Viewer was relaunched
  and its Party 0-Frame pause and Arin Pose panel were observed. The local Web
  Viewer displayed the shared Party timeline. These are bounded observations,
  not final native/Web UI acceptance.
- Both live exports still match the preserved Arin 24-key and Orin zero-key
  hashes recorded above. The new build script removes only obsolete Web
  diagnostic assets through the existing managed publisher; native diagnostics
  and canonical textures remain intact. Profile and its shortcut are disabled
  in the generated Web policy, not removed from Desktop.
- Before cleanup, Release/Web contains 51 files / 145,107,194 bytes. A recoverable
  complete copy is `artifacts/temp/codex-handoff/viewer-web-before-profile-cleanup-1747003.zip`,
  SHA-256 `2BD74794D03412007040A615B6A1EDB3714FFC62778CE1D27C6A826182F7D1C6`.
  No texture transcoding or resizing has been performed.

Next: compile/fix the final Party/gizmo changes, focused native and generated-Web
tests, visible UI validation, managed publication/hash comparison, documentation
and coherent milestone commit/push; then resume the outstanding H6.1 checks.

### Follow-up validation and Blender-reference correction

- `test-viewer-calibration-native.ps1` passed with disposable identity
  `smile.tests.viewer-calibration.run-21fddd25144f4b3e8ecd03c295f5ba62`, including
  Party Enter confirming a gizmo preview and then saving the correct active hero
  without resetting the battle. The same generated test project ran on Web with
  `--renderer3d-state --deny-data-key "Viewer Denied Storage Probe"` and exact
  `--native-output` parity. Logs: `artifacts/temp/h6-1-party-gizmo-{native,web-test}.log`.
- Release and Debug Viewer Native/Web builds passed. Release/Web is now 39 files,
  102,106,251 bytes versus 145,107,194 before cleanup (about 41.0 MiB saved).
  Twelve obsolete diagnostic assets were removed from generated output; all 21
  retained PNG hashes are unchanged. All 51 pre-cleanup ZIP entries were checked
  against their baseline hashes. Native still publishes its 46 assets; Web 34.
- The native hardening wrapper passed including generated-Web console parity,
  calibration isolation and 58 graphics/pointer/audio checks. Actual native Party
  Dragon 0-Frame and drag-scrub remained paused with unchanged camera diagnostics;
  Dragon Pose was disabled. Arin and Orin individual tabs loaded afterward.
  A native Arin Run frame-0 ring preview changed wrist X from 68 to 90 degrees;
  Escape restored 68 without Save. No user aesthetic acceptance is claimed.
- Sin rejected the thick ring appearance as jagged and asked for a closer Blender
  match. Inspected installed Blender 5.2.1's default cube Move/Rotate tools without
  opening or editing character files, then minimized Blender and foregrounded the
  Viewer at Sin's request. Premature whole-world rounding in shared ring projection
  was identified. The new camera-relative thousandths projection passes 128-point
  analytic native/Web checks to within one pixel, in the normal hardening wrapper
  (`artifacts/temp/h6-1-smooth-gizmo-hardening.log`). No Double was added.
- Follow-up styling now uses 128 ring segments, approximately constant screen
  size, round joins and subdued rear segments. This newer styling is NOT visually
  validated yet. Outer-ring view rotation, plane handles and full Blender behavior
  must not be claimed from the current axis-only implementation.
- Sin is using desktop mouse/keyboard. No UI inputs, activation, relaunches or
  interactive tests until Sin finishes. Background source work and compile-only
  checks continue. Latest source compiles to a separate test executable without
  replacing the running Viewer; completion is checked separately.

Open: finish gizmo/transfer/Party visible validation and documentation, milestone
commit/push, remaining H6.1 gate evidence, then the newly authorized lossless Web
Optimized platform and all three optimized publications. No final gate update yet.

### W11 focused keyboard ownership — 2026-09-06 continuation

- Sin finished using the keyboard/mouse and subsequently opened the Web Viewer
  himself. An attempted Windows browser observation was stopped by the Computer
  Use tool because it could not determine the browser URL confidently; an earlier
  attempt reported physical Escape. Sin attributes stray Escape to his KVM.
  No safety stop was disabled or bypassed, and neither attempt proves browser
  acceptance. Background edits/VM checks resumed after his follow-up.
- Shared generated Web input now records keys only on the focused canvas or
  console. Browser modifier shortcuts, function keys, text-input targets,
  composition and Shift+Tab remain unclaimed. Ctrl+Left/Right still support Viewer
  frame navigation; Control alone is held state rather than a queued action.
  Repeat does not create duplicate queued actions. Surface blur clears keyboard
  state without releasing independent virtual-controller owners.
- Added a generated, keyboard-focus-revealed Full Screen button, reached through
  Shift+Tab from the canvas. It uses the existing user-gesture fullscreen path,
  follows actual browser state and is hidden when unsupported. Alt+Enter remains
  available. Initial console/graphics focus transitions preserve external input
  focus. No new SMILE syntax or Double type was added.
- `dotnet publish src/Smile.Compiler/Smile.Compiler.csproj -c Release -r win-x64
  --self-contained false -o artifacts/compiler` passed, followed by compilation
  of `examples/Phase3ATextGame/Phase3ATextGame.smileproj` to
  `artifacts/web/h6-1/KeyboardFocus`. `run-web-test.js` passed `--mobile-controls`,
  `--file-transfer`, `--data-status` and dynamic Draw Text parity. These tests run
  generated JavaScript in the repository VM host, not an actual browser.
  Compiler/build/input logs: `artifacts/temp/h6-1-keyboard-focus-*.log`.
- The latest smoother Viewer source had already compiled successfully to
  `artifacts/tests/CharacterViewerSmoothGizmo.exe`; it has not yet been launched.
  W11 remains IN-PROGRESS pending actual Chrome/Edge checks and VSIX refresh.
  The running Viewer and its saved calibration were not replaced in this step.

### Loader, quality-tier authorization and user Mac observation — 2026-09-06

- Sin requested an immediate Web startup loader with the program title, large
  centered official logo, progress below it, creator credits and the Snake
  tutorial copyright/footer. Links must open a new tab. The repository link is
  retained separately. The unchanged canonical logo is now under `assets/branding`;
  Sin explicitly authorized a compressed Web loader derivative.
- Sin superseded the earlier lossless-only optimized-profile constraint: keep
  normal Web full fidelity and later add Web - Optimized Low, Medium and High.
  Low prioritizes small functionality-test downloads, Medium balances size and
  fidelity, High retains more detail while still being compressed. Nine optimized
  publications (three tools times three tiers) are pending after existing work.
  No canonical/native textures may be degraded. No measured savings claimed yet.
- Sin reports that the deployed Character Viewer looks good on his Mac and gives
  it a visual pass. This is user-observed acceptance of that deployed version,
  not an agent-run Mac test or acceptance of unbuilt loader/gizmo changes.
- Sin reports long frozen tab transitions in Chrome on PC and Mac. Source review
  confirms `SelectCharacterTab` tears down scene resources and calls `LoadViewer`;
  Web model loading uses `cache: "no-store"` and zero-reference image entries are
  discarded. Repeated visits therefore recreate resources and can redownload
  models. This is a recorded performance issue, not fixed by a startup overlay.
  Actual deployed timings and an ownership-safe caching fix remain pending.

### Shared Desktop/Web tab cache and branded loader validation — 2026-09-06

Source parent remains `1747003bddfb1188921084ac60bc3fc235232b54`; main and
origin/main were 0/0 at this checkpoint. Existing dirty work is preserved. This
entry supersedes the preceding cache-fix-pending note, not the unfinished H6.1 gate.

- Sin explicitly requested the same optimization on Desktop. The new shared
  `Character3D.SetUnusedAssetCacheLimit` API defaults to zero; the Viewer opts into
  three unused immutable assets. Tab switches still destroy per-instance actor,
  animator/pose and scene VFX state. Compatible geometry, prepared materials,
  textures and animation source data are reused, with bounded eviction, a single
  admission retry after unused eviction, shutdown cleanup and epoch invalidation.
  Quality is selected before loading so first/subsequent tabs use the same key.
- Both targets present a loading notice before teardown. Web additionally keeps
  encoded model/image downloads in a page-local 128 MiB/256-entry LRU. This does
  not retain combat state, saves or decoded owners. Invalid decoded images/models
  are removed, failed downloads are retryable, shutdown clears retained bytes,
  and late requests cannot refill a shut-down cache. First visits still incur
  loading; repeated switches are not promised to be instant.
- All three tool projects use the optional shared `WebLoadingAuthor` and
  `WebLoadingLogo` project metadata. The generated startup page has the title,
  prominent centered logo, activity bar, actual file readiness/pending detail,
  creator credits and the requested Snake footer/new-tab links. The activity bar
  is deliberately indeterminate, not a fabricated whole-download percentage.
  Script/runtime failures show recovery information. Native publication ignores
  the Web-only logo. No SMILE grammar or Double type was added.
- Canonical branding PNG is unchanged: 1,747,311 bytes, SHA-256
  `43D695C36FAB50849ADD26330E2D857F18C60BFBA91AF0EAA0D02127E0009AC9`.
  The authorized 768x512 Web derivative is 427,320 bytes, SHA-256
  `494F2B7A8476EA58702DEF84105ADEB52D77BCE3AA209A85A5507ECF5371A374`.
  No character textures were resized or recompressed.
- Corrected `Launch.ps1` to match the repository executable, including renamed
  running compiler outputs, rather than a title that can also match Chrome.
  Relaunch did not close the browser. One native Viewer (PID 44004 at observation)
  is running; the native Viewer was restored to the foreground after Web checks.

Actual validation (logs under ignored `artifacts/temp`):

| Command/check | Result/evidence |
| --- | --- |
| `dotnet publish src/Smile.Compiler/Smile.Compiler.csproj -c Release -r win-x64 --self-contained false -o artifacts/compiler` | PASS; `h6-1-loader-compiler-publish.log`. |
| `dotnet run --project src/Smile.Tests/Smile.Tests.csproj -c Release` | 299 PASS on final runtime source; `h6-1-loader-cache-final-language-tests.log`. Synthetic negative-test diagnostics are expected. |
| `scripts/test-smile-formatter.ps1` | 13 PASS; `h6-1-loader-formatter-tests.log`. |
| `scripts/format-smile-style.ps1 -Check -FormatLongIf` | Final 385-file check PASS; `h6-1-loader-cache-final-format.log`. |
| `scripts/test-character3d.ps1` | Native and generated-Web normal/PBR-fallback PASS; `h6-1-tab-cache-character3d.log`. Reuse keeps the model handle but creates a fresh animator, frame zero and clean pose; limit/profile eviction and shutdown also pass. |
| `tools/Character3DViewer/Build.ps1 -Configuration Release -Target All` | PASS; `h6-1-loader-cache-viewer-build.log`; native executable and Release/Web with 35 published assets. |
| Fire and Lightning `Build.ps1 -Configuration Release -Target Web` | PASS; `h6-1-loader-{fire,lightning}-build.log`. |
| `node scripts/run-web-test.js artifacts/web/h6-1/KeyboardFocus --startup-loading` | PASS; actual generated-runtime VM activity/cache/decode-failure/late-completion/entry-eviction tests, `h6-1-loader-cache-final-startup.log`. |
| Same fixed-canvas fixture with `--mobile-controls` and `--data-status` (separate invocations) | PASS; `h6-1-loader-cache-final-input.log` and `h6-1-loader-cache-final-web-tests.log`. A combined flags invocation selects Data tests only; it is not counted as all modes. |
| `node scripts/run-web-test.js tools/Character3DViewer/bin/Release/Web --file-transfer` | PASS; `h6-1-loader-cache-final-transfer.log`. |
| Native visible Party → Arin → Orin → Dragon → Party | All scenes rendered, no recovery error; Party demo resumed. This is functional evidence, not a stopwatch benchmark. |
| Visible Chrome four-tab cycle on current local build | All scenes rendered and Party demo continued; browser warning/error log query returned `[]`. No quantitative speedup or new Mac check claimed. |
| Visible Chrome startup/footer | Logo/activity/credits/footer rendered. A disposable local server delayed one model by six seconds to keep the unmodified loader visible. Clicking GitHub opened a separate `https://github.com/Sincioco` tab. Popup closed, Viewer returned to normal `127.0.0.1:8766`, temporary server stopped. Mail-handler/social services were not each opened. |
| `scripts/install-vsix.cmd` | Rebuilt/installed/verified 2.0.59; `h6-1-loader-cache-vsix-install.log`. Installed extension DLL SHA-256 `B8A9A3A623528A4331144379C84A612C4BCB855B3DA9AB492974C444A3CCAEAE`; artifact `artifacts/vsix/Smile.VisualStudio.vsix` SHA-256 `8F93BC1EC1BD7D31021E22BFCE73A222C9B9516A064A6C947064354AA1841D39`. |
| Installed compiler/language payload | Matches built `smilec.dll` SHA-256 `90D7D9CCBD782035A3273DB127F2E46B9ED5B06162DC71907768980949E323B2` and `Smile.Language.dll` `026D9DA0C4E6244B759BE9D608EDA6EC761E03518B57613385CBCD9F5FD38475`. |
| `scripts/test-character-3d-viewer-hardening.ps1` | PASS on final Viewer/cache source, including isolated calibration, exact generated-Web hardening console and 58 native graphics/input/audio checks; `h6-1-loader-cache-final-viewer-hardening.log`. |
| Current isolated Viewer native/Web snapshot fixture | Native PASS, then Web compile and `run-web-test.js artifacts/web/h6-1/CalibrationImportTests --renderer3d-state --deny-data-key "Viewer Denied Storage Probe" --native-output artifacts/tests/ViewerCalibrationIsolation/native.out --frames 16 --timeout 60000` PASS, exact console; `h6-1-loader-cache-final-calibration{,-web}.log`. |

An initial `--mobile-controls` run against the responsive Viewer fixture failed
its fixed-960-canvas expectation (480 versus 640). Recompiling the intended fixed
`Phase3ATextGame` fixture and running that mode passed; this is a fixture-selection
correction, not a claimed pointer bug fix. Draft compile errors from reserved
`Count` and guessed drawing syntax were corrected to existing supported SMILE
syntax before the successful builds above.

The final isolated snapshot fixture now remaps its optional Web logo path when
copying the project. Its first Web run used the VM's default three-frame limit;
new tab-loading notices reached that limit before the final success line. Both
JSON outputs matched exactly, and rerunning with the bounded 16-frame allowance
completed all assertions and matched the native console. Reusing the disposable
output directory with a new ApplicationId emitted SML3605 (old publication
manifest identity ignored); it did not alter live Viewer publication or storage.

Both live calibration exports still match the Arin 24-key and Orin zero-key JSON
hashes recorded earlier. No historical checkpoint was restored and no calibration
was changed by tab checks. The user-reported Mac visual pass applies only to his
deployed build. No website upload has been performed by Codex.

Unfinished: commit/push validated milestones, distinct imported-state refresh/
reopen, latest gizmo interaction/remaining native and real Chrome/Edge parity
checks, W10 MSAA, audio/lifecycle/mobile and integrated same-model VFX evidence,
wider normal smoke, final report/gate/portable evidence package; then the nine
Web Optimized Low/Medium/High outputs. H6.1 remains incomplete. No E0 or Double.

### Final-task request — public README showcase

Sin requests a complete root README rewrite as the final task after the remaining
implementation and validation work. Replace the technical-manual presentation
with a concise, professional, high-level visual showcase for casual visitors,
developers, prospective employers, collaborators, business partners and investors.
Use genuine Viewer/VFX screenshots for the opening visual hook and recent
accomplishments; link detailed technical guidance separately. Briefly explain
SMILE's full name, mission and continued evolution. Describe the planned Battle
Scene Editor and declarative scene-authoring direction as future work, without
introducing another version name or presenting proposed syntax as supported.
Do not mark this final task complete before the earlier approved work is done.

### Viewer integration and opt-in gizmo — 2026-09-06

The shared runtime/cache/loader milestone is committed and pushed as
`89755fe9a44404d3d9d1867c256d10a841f29dd0` on main. This next Viewer milestone
adopts the shared cache and loader, preserves the Party demo behind a shared
active-actor inspector, and includes the strict schema-2 keyframe import/export
workflow documented above. Native diagnostic profiles remain available; normal
Web publication excludes those historical diagnostic assets. All three tools
use white headers. The launcher identifies only the tool's own executable.

Sin requests that transform handles be opt-in, with precise numeric editing
available without them. `Show Gizmo` / `Hide Gizmo` is in the Pose Calibration
panel on both targets. Fresh calibration initialization and full reset hide it.
Hidden handles are neither drawn nor hit-tested, and R/E/G cannot initiate an
invisible drag. Hiding ends drag capture but preserves the current unsaved
preview; it does not save, cancel, or resume playback. Numeric controls and
X/Y/Z selection remain usable. No JSON schema or saved key values change.

Actual additional checks:

- `scripts/test-viewer-calibration-native.ps1`: PASS, disposable identity
  `smile.tests.viewer-calibration.run-f6ab16e854d74d68ba44270d92eada47`;
  `artifacts/temp/h6-1-gizmo-visibility-native.log`. Assertions cover hidden
  startup/reset, keyboard and pointer guards, numeric editing, explicit show,
  hide-during-preview without saving, exact Cancel restoration, and each Party
  hero explicitly enabling its own gizmo.
- Generated isolated Web fixture and the bounded 16-frame exact-console parity
  command above: PASS; `artifacts/temp/h6-1-gizmo-visibility-web.log`.
- `tools/Character3DViewer/Build.ps1 -Configuration Release -Target All`: PASS;
  `artifacts/temp/h6-1-gizmo-visibility-build.log`. Release native executable and
  Release/Web are current; Debug has not been rebuilt for this latest toggle.
- Visible native: launched the current Release through Launch.ps1, exactly one
  Viewer at observation (PID 64604). Arin's panel opened with no gizmo; Show
  revealed the rings, Hide removed them. Keyboard-initiated R plus pointer
  travel changed Run frame-zero wrist X from 68 to the bounded 180 preview;
  Hide preserved it and Cancel restored 68, without saving.
- Visible Chrome, current `127.0.0.1:8766` publication: panel default hidden,
  Show/Hide both rendered correctly. With handles hidden, the numeric slider
  changed Defend frame-zero wrist X from 55 to 78; Cancel restored 55. No Save
  or import replacement was performed during these checks.

The pointer handler now applies final release-frame travel before finishing an
existing drag, and releases capture when press/release arrive in one frame.
Fast automated native mouse drags still did not prove numeric rotation: short
gestures selected a ring without changing its value; one larger gesture panned
the camera. Event coalescing/endpoint-only delivery is a possible explanation,
not a proven runtime diagnosis. Keyboard-initiated rotation did change the
displayed value. Physical ring dragging remains an explicit manual limitation
for the larger gizmo acceptance; full Blender equivalence is not claimed.

### W10 — Web scene MSAA — 2026-09-06

Source milestone: `78e00f496e47f95e7bcfb504bf578da44dd5381e`, following
`0fd79938ae8073191bfae18097c6039a7db69157` on main. W10 is FIXED-VERIFIED;
this does not complete H6.1. The root README remains queued as the final task.

The existing WebGL2 renderer now chooses supported 4x/2x/1x scene targets by
intersecting color/depth capabilities and checking actual sample counts and
framebuffer completeness. Partial targets are released before a lower-sample
retry. Color/depth resolves provide the opaque snapshot; heat composition copies
color back without tone mapping while retaining multisample depth, followed by
transparent effects and final resolve. Queries report actual samples, resolves
and allocated bytes. AA alone preserves the immediate submission path rather
than imposing the deferred submission limit. Existing target bundles own reuse,
resize, replacement and context-reset lifetimes. No new SMILE syntax was added.

Validation commands/results (logs under `artifacts/temp`, generated/ignored):

| Command/check | Result and log |
|---|---|
| Compiler publish | PASS; `h6-1-msaa-compiler-build.log` |
| `scripts/test-renderer3d-post-processing-hardening.ps1` | PASS including base native/Web normal, HDR and shadow fallbacks; `h6-1-msaa-final-post-hardening.log` |
| `node scripts/run-web-test.js artifacts/web/Renderer3DPostProcessingTests --renderer3d-msaa --expected examples/Renderer3DPostProcessingTests/expected-normal.txt --timeout 60000` | PASS exact console plus GL-double capability, resolve/depth, reuse, resize, fallback, context recreation and cleanup assertions; `h6-1-msaa-capability-tests.log` |
| `scripts/test-renderer3d-distortion.ps1` | PASS native/Web HDR, LDR, half/quarter quality and fallback; `h6-1-msaa-distortion-tests.log` |
| `scripts/test-renderer3d-soft-particles.ps1` | PASS native/Web MSAA, 1x, soft fade and fallback; `h6-1-msaa-soft-tests.log` |
| Targeted SMILE formatter check | PASS one changed fixture; `h6-1-msaa-formatter.log` |
| Release Web builds of Viewer, Fire and Lightning | PASS; `h6-1-msaa-{viewer,fire,lightning}-build.log` |
| `scripts/install-vsix.cmd` | PASS rebuild/install/verification; `h6-1-msaa-vsix-install.log` |
| `git diff --check` | PASS |

Three old hardening assertions were updated to match already-existing source:
shared cull helper, two bounded GPU allocation/first-dispatch error probes, and
whitespace-split native byte accounting. Those are stale test expectations, not
new runtime fixes. No visual tolerance was loosened to conceal a regression.

Real visible browser observations, separate from the GL double:

- Chrome at `http://127.0.0.1:8767/`: Fire GPU backend, High quality, scene AA
  samples 4 and error 0 in HDR and LDR with depth/heat. After the final immediate-
  path correction, reloaded and disabled HDR/depth/heat: fire still rendered,
  samples 4 and error 0; captured warnings/errors were empty. Reloaded defaults.
- Edge at the same Fire origin: HDR/LDR with depth/heat, GPU/High, samples 4,
  error 0 and no captured warnings/errors. This preceded the final AA-only queue
  correction; the depth/heat path was unchanged by that correction.
- Edge at `http://127.0.0.1:8768/`: GPU Lightning/Ultra rendered. Maximizing then
  restoring the browser changed the viewport from approximately 1415x901 to
  1912x901 and back, with effects still visible and no captured warnings/errors.
- Edge at `http://127.0.0.1:8766/`: branded startup loader, Party then Arin, Orin,
  Dragon and Party all rendered without a recovery overlay or captured errors.
- Full Screen control changed its label/pressed state, but the DOM observation
  still reported no fullscreen element. This is NOT actual fullscreen acceptance;
  W11 remains open for that evidence. Window resize above is independent evidence.

Chrome and Edge share Chromium. These observations do not claim independent-
engine, Firefox, Safari, physical mobile-device, performance benchmark, real
context-loss injection or user visual approval. Sin's explicit Chrome/Edge choice
supersedes the package's Firefox requirement. No website upload was performed.
Exactly one native Viewer remained running and was restored to the foreground.

VSIX 2.0.59: `artifacts/vsix/Smile.VisualStudio.vsix`, SHA256
`25E01235B784F7AB3DB128CBC3E17986AFB4E505F3C4A0BCAA98DB6C4489C440`.
Installed under
`C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/m2b0la0n.j3x`.
Installed and artifact compiler SHA256 both
`E3054DE95B12F38950CE0155E45C265DAE2C10AB94E36128592E4F089D7130EF`.
Installed extension DLL SHA256
`5C25A63893B8865386F6736BEB24BDBD62717323E631FC8C6EE977DA13626B28`.

Native generated fixtures include
`artifacts/tests/Renderer3DPostProcessingTests.exe`,
`artifacts/tests/Renderer3DDistortionFallbackTests.exe`,
`artifacts/tests/Renderer3DSoftParticleFallbackTests.exe` and
`artifacts/examples/Renderer3DPostProcessingLab.exe`. The native Viewer did not
need a rebuild for this Web-only runtime change. The three current Web outputs
remain each tool's `bin/Release/Web` directory.

Both canonical exports remained unchanged: Arin 24 keys, SHA256
`C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`;
Orin zero keys, SHA256
`13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
Logs: `h6-1-msaa-arin-export.log` and `h6-1-msaa-orin-export.log`.
No calibration Save/import or historical restore occurred in this milestone.

Next: remaining integrated native/actual-browser lifecycle, same-model effects,
calibration persistence and interaction proof; normal smoke; final H6.1 reports,
gate and portable evidence package. Then finish nine optimized Web tier outputs
and finally the requested visual README rewrite. No E0 or Double work.

### Follow-up request — truthful Web loader progress, after README

Sin explicitly adds one task after the README rewrite: replace the ambiguous
animated loading indicator with an overall progress bar and a secondary bar for
the current asset download. Show real byte percentages when totals are known;
where a server does not provide a reliable length, show received bytes and an
honestly labeled completed-item measure rather than inventing a percentage or
ETA. Preserve the approved prominent logo, program name and working footer links.
Reconcile the existing shared loader/fetch pipeline before choosing the smallest
implementation, and validate the three tools and affected VSIX payload.

This later instruction changes the final ordering: existing hardening and Web
Optimized work first, visual README rewrite next, then loader progress last.
It does not authorize starting E0 or Double, and streaming shutdown still waits
until every approved task is actually complete.

The loader follow-up also includes compile-time metadata below the author credit:
the actual build date/time, an explicit time-zone offset, and the authoritative
SMILE/VSIX product version. The user's October 31 example is formatting guidance,
not a literal date or evidence of the build machine's geographic location. Do not
substitute page-open time or infer a physical city from its UTC offset.

Sin also authorizes native branding. The current HTML loader is Web-only;
**Flag:** a reusable startup splash for native graphical programs is additional
runtime work, not an existing supported loader feature. Implement a branded
splash with the official logo, credits/footer and build
metadata, overlapping real startup loading where practical and retaining useful
status if startup takes longer. Keep console programs and ordinary graphical
input/focus behavior intact. Inspect existing native startup/asset preparation
before selecting the smallest shared implementation. This stays in the final
loader milestone after README, including native validation and VSIX refresh.

Sin subsequently sets the permanent minimum to **one second**, on both native
Desktop and Web and for all SMILE programs/tools, even with fast/cached startup.
This supersedes the earlier two-second native suggestion. Measure actual visible
presentation and overlap loading; do not add an unconditional second after assets
finish. The root AGENTS.md records this as a pending implementation requirement,
not a claim that current executables already meet it. Loader work remains after
README; no new startup code has been implemented while recording these requests.

Sin additionally requires reusable library/runtime presentation with mandatory
compiler/runtime inclusion: no optional import/call and no standard source-level
or build-setting bypass. Explain the boundary honestly: editable compiler sources
and generated Web files cannot be made tamper-proof. No DRM, obfuscation or new
framework is implied by this branding requirement.

### F02/F04 — real same-model correction and effect evidence — 2026-09-06

Implementation/test milestone: `cf84ac8c09468368bf77629becbe83fdf940279d`,
on top of `ee2a951` (W10 evidence) and `78e00f4` (Web MSAA). Existing production
ownership fixes are retained; this closes their remaining focused Orin evidence
gap, not all H6.1 requirements.

The test previously varied actor yaw but did not prove actual independent wrist
corrections in the combined live scene. It now resolves the right wrist from the
HandRight socket and applies different local rotations to the two animators.
Each hammer-head socket moves while the other actor's corrected socket stays
exactly unchanged. The current profile supplies Orin's standing baseline. No
fixture correction is saved. Both actors retain distinct clips/times/speeds,
transforms, storm contexts, styles and scene-issued light leases.

It now presents real draw frames after frozen weapon hide, resume, scene Off,
and destruction of the first context. Assertions prove the first trail/light
clear while the second survives, no resumed thunder is replayed, scene Off
survives reversed frozen staging without moving the camera, and stale light
leases cannot disable replacement/current owners. Existing bounded effect
admission and complete actor/effect/GPU-system cleanup assertions remain active.
The visible last frame identifies the actual GPU versus CPU fallback trail path.

- `scripts/test-character-3d-viewer-actor-isolation.ps1`: PASS native Auto and
  forced shader-failure paths; both generated-Web exact-console paths PASS.
  Log `artifacts/temp/h6-1-live-actor-isolation.log`; native outputs
  `Character3DViewerActorIsolationTests.out` and
  `Character3DViewerActorIsolationFallbackTests.out` under `artifacts/temp`.
- `scripts/format-smile-style.ps1 -Check -FormatLongIf -Files tools/Character3DViewer/ActorIsolationTests.smile`:
  PASS; `git diff --check` PASS.
- Actual visible Edge 146.0.3856.62 and Chrome 152.0.7977.77, Windows 11 Pro
  10.0.26200: normal and `fallback.html` at
  `http://127.0.0.1:8765/Character3DViewerActorIsolationTests/` all print exactly
  `Character Viewer two-Orin isolation tests passed`. Normal pages show GPU;
  fallback pages show CPU Fallback. The last rendered frame shows the remaining
  actor and lightning after first-context release. Captured warning/error logs
  are empty in all four runs. These are real browser draws, not the GL double.
- The script creates ignored `fallback.html` beside the test publication. It
  injects only the existing GPU shader-failure test flag before runtime startup;
  normal production/index pages are unchanged. It is a deliberate capability
  fallback, not a claim that real hardware failed spontaneously.
- Both local browser tabs were returned to `http://127.0.0.1:8766/`; the one native
  Viewer was restored to foreground. No screenshot files are claimed: the live
  tool images and these explicit observations are the evidence.

Final generated fixture SHA256 values:

| Artifact | SHA256 |
|---|---|
| `artifacts/tests/Character3DViewerActorIsolationTests.exe` | `6D26694AED1D2C77F8DFD3B791197BAF4526328E9671C6322E851068C65E3912` |
| `artifacts/web/Character3DViewerActorIsolationTests/game.js` | `3AB4EA9843A0787A3C82D2F1EA2ADAC3495A87468FF4138EB57ED7B5AA9F95D0` |
| `artifacts/web/Character3DViewerActorIsolationTests/smile-runtime.js` | `F034E3439FB2D937D58A3CF37260B65EBE1FA8F24C748394DA3EEC6096C9C408` |
| `artifacts/web/Character3DViewerActorIsolationTests/fallback.html` | `D413AF92AD74E98A10CCD2119F8E965D3F2C6F38A1C9A4B8B778CCC9E7B4D4A0` |

The first expanded Web run stopped at the test runner's old three-frame limit
before printing a result; the five presented lifecycle frames now receive an
explicit eight-frame allowance. No assertion or expected output was weakened.
A draft loop spelling was rejected by SML2001 and corrected to the existing
`Loop Until True` syntax before final builds; no language feature was added.

Before committing, both exports remained Arin 24 keys and Orin zero keys with the
same SHA256 values recorded above; logs `h6-1-live-actors-{arin,orin}-export.log`.
Canonical model/descriptor/calibration data are unchanged. This fixture/docs
milestone changes no VSIX payload; the W10 installed VSIX remains current.

Next actions: remaining Fire frozen lifecycle, actual Viewer interaction,
imported-state persistence, focus/audio/mobile/context recovery and normal smoke;
finish truthful H6.1 reports/gate/package, then Web Optimized tiers, README, and
the newly requested mandatory loader/splash improvements, in that order.
