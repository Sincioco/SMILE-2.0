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
| P0 | Validated; publication is the containing commit | ViewerUi, BattleArenaPreview; existing fallback queries. Web Renderer3DReflections; existing parent HDR state | Both production label policies handle unsupported receivers; RGBA16F uses core LINEAR filtering with renderability/retry checks retained |
| D1 | Next, authorized | Smile.Language; typed native/Web emitters/runtime; packages/editor/debug | Pending distinct binary64 semantics, storage, calls and tooling on both targets |
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

## Next action

Publish validated P0, then implement D1 using documents 03 and 04 (read).
No further Double authorization is required. Complete D1–D4 before stopping.

## On Hold

Battle Scene Editor, semantic-inspection CLI, loader/splash enhancements,
subject-first syntax and unrelated suspended features remain deferred.
