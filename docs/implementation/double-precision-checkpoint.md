# Double precision implementation checkpoint

## Authority and baseline

- Package: `smile-2.0-hardening-then-double`, revision 1; documents 00–07.
- Archive: `C:\Users\louie\Downloads\2026-09-08-0837-smile-2.0-hardening-then-double.zip`.
  SHA-256: `4C9D7986DF6962DFE58F7D65ED4182688DC82D142141753E1CD5610EF74132BB`.
  Identity, reviewed commit and all 11 payload hashes verified before safe extraction to
  `C:\Users\louie\Downloads\smile-hardening-double-verified` (fresh destination).
- Starting HEAD and fetched origin/main: `af37b2402ca02b50069fc6bc91eb9872200427fc`,
  branch `main`, clean worktree, no divergence or newer work to reconcile.
- H01/H02/H03 closure and paired evidence read; closed. RF01–RF03 and the accepted
  Viewer coordinator remain preserved. No Doctor or retired test run/recreated.

## Current state and owners

| Phase | State | Owner and dependency | Structural completion |
|---|---|---|---|
| P0 | Completed and pushed, `9f33ad9` | ViewerUi, BattleArenaPreview; existing fallback queries. Web Renderer3DReflections; existing parent HDR state | Both production label policies handle unsupported receivers; RGBA16F uses core LINEAR filtering with renderability/retry checks retained |
| D1 | Validated; publication is the containing commit | Smile.Language/DoubleSemantics; MasmDoubleEmitter/numeric runtime; WebEmitter/Double.js; package schema 7; shared completion and native debug types | Distinct binary64 arithmetic/storage/calls/errors, packages and tooling on both targets; 317 shared tests; actual native, Web and Chrome execution |
| D2 | Pending D1 | Existing renderer and actor ownership; fractional typed bridge/shared math | Pending actual fractional submission/query path |
| D3 | Pending D2 | Viewer camera/Party, Sin Star I preview, shared curve slice | Pending selective adoption without rounding to old integer arguments |
| D4 | Pending D3 | Integration, documentation, publication and installed VSIX verification | Pending final delivery |

## P0 evidence

- Compiler: `dotnet publish src/Smile.Compiler/Smile.Compiler.csproj -c Release -r win-x64 --self-contained false -o artifacts/compiler` passed.
- Focused formatting: `scripts/format-smile-style.ps1 -Files` on the three changed
  SMILE files with `-FormatLongIf`, zero changes required; `git diff --check` passed.
- `scripts/test-renderer3d-reflections.ps1`: native normal/retry and generated-Web
  normal/retry passed, including production unsupported-receiver matte fallback,
  translated planes and LDR/HDR/LDR transitions. The generated-Web normal run also
  observed production RGBA16F/HALF_FLOAT allocation with LINEAR min/mag filters in
  a context providing EXT_color_buffer_float but no OES_texture_float_linear.
- Its Viewer subgate initially exposed an invalid new Draw Text continuation;
  corrected using a named label value. `scripts/test-character-3d-viewer-hardening.ps1`
  then passed independently on native/Web, with direct On/Off fallback-6 assertions
  against both real label owners, isolated calibration preservation/round trips,
  and 58 native graphics/pointer/audio checks. No need to repeat the unchanged
  reflection subgates after the presentation-only syntax correction.
- Installed Chrome ran the generated reflection fixture to its exact passing result.
  An ignored copy under `artifacts/temp/p0-chrome` masked only float-linear support
  and read the actual reflection framebuffer: format 2 (RGBA16F), 700x525,
  min/mag 9729 (LINEAR), floatLinear false, GL error 0, RGB peak 0.72216796875.
  No warning/error console logs. Screenshot obtained through Chrome accessibility
  capture after the first screenshot request timed out. This fixture's RGB peak
  does not independently repeat the historical >1 HDR probe; that unchanged
  rendering evidence remains in `reflective-battle-floor.md`.
- Validation corrections: the first formatter CLI list was passed as one path;
  corrected to a PowerShell array. The first new filter assertion also caught
  unrelated HDR textures; corrected to observe the production reflection caller.
  Neither failed attempt is counted as passing evidence.

| Source/artifact | SHA-256 at P0 validation |
|---|---|
| `src/Smile.Compiler/WebRuntime/Renderer3DReflections.js` | `764313F715DC11BDC250619699994DAB6F81FB8F0EA3BF58651F0BE78464A095` |
| `tools/Character3DViewer/ViewerUi.smile` | `28F8EF7F07CA0B0397B3502EC00712AC77CF628FEC29446402176B4A66658D8E` |
| `games/SinStarI/BattleArenaPreview.smile` | `A142EB3FAF38BB8A4CD9FE5D3E85AD107CBA9CA93B15D44B64FE9E2B973A1612` |
| `artifacts/compiler/smilec.dll` | `EFF5E720173DE0824221E3EC3D5AFE2CF50B63BD7D7D9B88B4130982FAA86267` |
| `artifacts/web/Renderer3DReflectionTests/smile-runtime.js` | `20F2417749D984C420877E8F01F553DBE75A2361D5F9EAF4E223C24256267EE0` |

Local logs: `artifacts/temp/p0-compiler-build.log`, `p0-reflections.log`,
`p0-viewer.log`. Production assets, calibration JSON, live storage, IDs and
`Program.smile` were not changed. VSIX rebuild/install is reserved for the integrated
D4 payload boundary; no current Double or installation completion is claimed.

## D1 evidence and limits

- Contract and support matrix: `docs/language/double.md`. Token/type tags are appended;
  Number and Enum keep their integral rules. Native internal eight-byte call payloads
  use typed floating adapters at external ABI boundaries; Web uses the same runtime.
- `scripts/test-double.ps1` passed native/generated-Web success, source-project and
  package-file parity, deterministic package hashes, 15 explicit numeric/text failure
  cases and target-specific bounds. Failure cases carry a Class with nested Double
  array/Image state and local/global Text; all native live counts return zero and
  Web ownership diagnostics pass. Earlier side-effect output is preserved and no
  post-failure write/output occurs. Logs: `artifacts/temp/d1-focused-final.log`,
  detailed run `artifacts/temp/double/run-3344955aaaf64ee5b3f0cc88c23ce78f`.
- Later Print acceptance, duplicate-zero Select diagnostics, Optional Quick Info
  display and contextual intrinsic-name inference corrections were validated by
  317 shared tests (`d1-shared-tests.log`) and the rebuilt native/generated-Web
  fixture (`artifacts/tests/DoubleTests.exe`, `artifacts/web/DoubleTests`). The source
  fixture prints `0.002` and `Double tests passed`. Unchanged failure/package
  evidence above is reused; no redundant full-smoke/VSIX installation was run.
- Actual `--debug --keep-temp` compilation and execution pass; generated C/PDB
  retains Double scalars, arrays and nested aggregate views with source paths.
  Shared tests check typed views, navigation, Optional signed-zero signatures and
  Double return temporaries. This is compiled debug evidence, not a claimed manual
  Visual Studio breakpoint session. Integrated VSIX rebuild/install remains D4.
- Installed Chrome tab 35136093 ran the actual generated program at local port
  8771. DOM output verified `0.002\nDouble tests passed\n`; earlier AX screenshot
  obtained. A later automatic screenshot timed out; DOM execution was still verified.
- Formatter: all 13 focused integration tests passed; repository-wide
  `-Check -FormatLongIf` passed for 423 tracked files, plus explicit formatting of
  the three new Double sources. Logs: `d1-formatter-tests.log`, `d1-repository-style.log`.
- Corrected defects: native MINSD/MAXSD tie lowering lost negative zero despite
  `/fp:strict`; explicit tie operand order fixes it. Also fixed Print type admission,
  signed-zero/adjacent-value Select duplicate identity, new contextual-name inference,
  Optional signature display and Web folded Number constant range checking.
  Initial harness invocations needed `--project`/`--target library`, a Type-owned
  Image field and a parenthesized PowerShell case name; failed attempts are not passes.
- No canonical assets, live saves, calibration, Viewer coordinator or closed H/RF
  investigation was changed. D2–D4 and final installation/publication remain pending.

| D1 source/artifact | SHA-256 at validation |
|---|---|
| `src/Smile.Language/DoubleSemantics.cs` | `08BBB3A81D7191C5C41C51744C544AF8930E4A214105420F5A55C01E1C01733F` |
| `src/Smile.NativeRuntime/numeric/double.cpp` | `281D02ACE6C9EFD96B9473E0944F32FA9EE8094607DE72DC0B6FC52C8D22BEC6` |
| `artifacts/compiler/smilec.dll` | `2E6FDF0C01D8B382B0C033C70FE0722EC484A4E6EEAD0A503F33E74D796FF44D` |
| `artifacts/compiler/Smile.NativeRuntime.lib` | `913697963ADD07FF3D62FE4812B3A68D15CDB75F909AEB957D1346682DD0D971` |
| `artifacts/tests/DoubleTests.exe` | `DDBE8E83CD029D970CC18C196F7002FC863A5E71E016698FB04F0E97FD723C8A` |
| `artifacts/web/DoubleTests/game.js` | `103B99C1D166A7BD1788646E3B42ACB303CD6880FE18B5BA54D79F6251DA2A15` |
| `examples/DoubleTests/bin/Debug/Smile.Double.Proof.smilelib` | `B8AE5586E9682C290770B415D17EFE15F6A9D1A81E8D550F2FCA2653DA4BA282` |

## Next action

Publish validated D1, then read document 05 and implement the typed fractional
renderer/actor/query bridge and shared math through the existing owners.
No further Double authorization is required. Complete D1–D4 before stopping.

## On Hold

Battle Scene Editor, semantic-inspection CLI, loader/splash enhancements,
subject-first syntax and unrelated suspended features remain deferred.
