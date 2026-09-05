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
