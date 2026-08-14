# Phase 5.1 UI Hardening Report

Date: 2026-08-15

## Result

Phase 5.1 hardens the reusable `Smile.UI` library without adding native UI helpers or beginning Phase 6. Window, bitmap-font, text, menu, and dialogue styles now have bounded validation; style replacement and dialogue preparation are transactional; menu layout reflows correctly; and system/bitmap text share one multiline contract on Windows and Web.

## Pre-fix reproduction

The ignored `Phase5HPreFix` fixture reproduced all seven required defects against the reviewed Phase 5 baseline:

- an undersized window-skin source rectangle returned `TRUE` from `Window.IsStyleValid`;
- an undersized bitmap atlas returned `TRUE` from `BitmapFont.IsStyleValid`;
- `Dialogue.SetStyle` deactivated an active dialogue;
- `Menu.SetStyle` could leave the selected item outside the viewport;
- effective menu rows shrank but did not re-expand;
- system and bitmap empty/multiline measurement and drawing disagreed, and the exact multiline literal initially failed lexing;
- a 2049-scalar raw dialogue page was accepted.

The baseline Web console-parity fixture otherwise passed, isolating the defects to the Phase 5.1 contracts.

## Implemented contracts

### Validation and replacement

- `Window.IsStyleValid` validates source origin, source size, border sums, image bounds, destination borders, padding, filter, opacity, and bounded layout values with subtraction-based overflow-safe checks.
- `BitmapFont.IsStyleValid` validates atlas origin, complete row/column grid bounds, glyph geometry, filter, Unicode range overflow, surrogate ranges, and fallback code points.
- `Text.IsStyleValid`, `Menu.IsStyleValid`, and `Dialogue.IsStyleValid` validate their complete nested styles, rates, spacing, cursor/indicator geometry, and filter modes.
- Invalid `SetStyle` calls return `FALSE` without replacing retained records. Active dialogue reflow prepares scratch state before committing the candidate style.
- A stale bitmap-font handle is invalid for replacement but safely measures/draws through the documented system-text fallback.

### Menu layout

- Requested and effective visible-row counts are stored separately.
- Effective rows shrink and later re-expand after style changes.
- `SetStyle`, item insertion, enablement changes, and selection changes keep `TopIndex` clamped and the selected item visible.
- Scrollbar tracks and thumbs are bounded, including a three-pixel-high focused fixture, with no zero division.
- `MenuStyle.CursorFilterMode` makes smooth filtering the default and pixel filtering an explicit opt-in.

### Text

- System and bitmap modes split on Unicode scalar newline 10 and preserve empty lines and trailing newlines.
- Width is the widest logical line. Height is `lineCount * lineHeight + (lineCount - 1) * LineSpacing`; draw uses the same formula and applies spacing exactly once.
- Left, center, and right alignment are calculated per line.
- Nonpositive opacity draws no text. Bitmap opacity supports 1 through 100. Positive system-text opacity is intentionally fully opaque because a clean cross-target generic alpha contract is not available in this phase.
- Multiline UTF-8 literals are accepted by the shared lexer; CRLF and CR inside literals normalize to scalar newline 10.

### Dialogue

- `UI_MAX_DIALOGUE_PAGE_SCALARS` is 2048. `AddPage` measures once and rejects 2049 scalars without mutation.
- Wrapping is bounded and uses a proportional binary search for fitting chunks while preserving scalar-safe `TEXT_SLICE` behavior.
- `Start` and active `SetStyle` build scratch prepared pages before commit.
- Valid active theme changes preserve active state, raw-page identity, current prepared page, and visible scalar progress.
- Failed active reflow returns `FALSE` and preserves the old style and state.
- Long unbroken text, spaced text, spaces-only text, repeated newlines, emoji/non-BMP text, and a 32-page failed active restart are covered by focused tests.

## Packaging, capability, and ownership

- `Smile.UI` version: `1.0.1`.
- `.smilelib` format: `5`.
- Final package SHA-256: `410FCB1F0F4B80986C230E4E88E61D9FFA2DEDBFDE324BF6072137F8C28D63EC`.
- Package metadata keeps `Dialogue.Start` and `Dialogue.SetStyle` transitively game-window-capable while pure state APIs such as `Menu.SetStyle`, `Menu.VisibleRows`, and `Text.IsStyleValid` remain console-safe.
- Pure-console project/package calls to a game-capable dialogue routine each produce one consumer-located `SML3704`.
- Project and package consumers pass on native and Web; deterministic two-build package checks remain green.
- Focused fixtures create, replace, fail replacement, destroy, and unload all component-owned and app-owned values. Web diagnostics return image cache/reference counts to zero and stop media; native TEXT/image ownership and cleanup remain green in the native runtime suites.

## Performance

Focused GDI preparation timings on this machine for maximum 2048-scalar pages were:

- unbroken ASCII: 29 ms;
- spaced ASCII: 28 ms;
- Unicode emoji: 314 ms;
- maximum observed: 314 ms.

All are below the broad 5000 ms anti-pathology guard. The normal smoke suite does not use a fragile exact-time assertion.

## Automated validation

- Focused `Phase5UIStateTests`: pass for project and package consumers.
- Focused `Phase5DialogueStateTests`: pass on native and Web, including failed active `Start` rollback.
- Focused `Phase5Hardening`: pass on DirectX, GDI, Web, and project/package consumers.
- Invalid console project/package fixture: exactly one `SML3704` each.
- Shared language/compiler/project/completion/timing tests: 179 passed.
- Native graphics/audio-focus tests: 39 passed.
- Native TEXT tests: 38 passed.
- `cmd /c scripts\build.cmd`: passed; expected `NU1503` restore-skip warning for the native `.vcxproj` only.
- `cmd /c scripts\smoke-test.cmd`: passed in 221.4 seconds.
- Ten-game matrix: all ten normal and no-demo native builds and all ten normal and no-demo Web builds passed.
- High-resolution asset publication, DirectX/Direct2D, DirectWrite, GDI, Web high-DPI, audio, persistence, and package/provider regression gates passed.

## Live acceptance

- Visual Studio 2026 Enterprise instance: `91f001b5`.
- Solution Explorer refreshed immediately and displayed the `Smile.UI (1.0.1)` project reference, sources, references, and assets.
- `Window.IsStyleValid`, `Text.Draw`, `Menu.SetStyle`, and `Dialogue.SetStyle` breakpoints bound in `.smile` source. F10 remained mapped to `.smile` source.
- DirectX: MenuGallery ran through Visual Studio, opened a partially revealed dialogue, switched system to bitmap to vector while active, preserved progress, rendered multiline content, and continued normally.
- GDI: the explicit GDI MenuGallery artifact repeated active bitmap/vector theme switching and multiline rendering successfully.
- Chrome/Web: Chrome ran at device-pixel-ratio 2. The 789 by 444 CSS canvas used a 1578 by 888 backing canvas. Active system/bitmap/vector switching, multiline rendering, dialogue completion, menu scrolling, and selection visibility passed with no console warnings or errors.

## Installed Visual Studio artifacts

VSIX version, assembly version, and file version are synchronized at `2.0.29` / `2.0.29.0`.

- VSIX: `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`
  - SHA-256: `14EED4FAB194F4FC75FCB7AD4BFF52BE4FB35E354899F9E459F8A5E0C357CE3C`
- Installed root: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\rrqe0ln5.kjw`
- `Smile.VisualStudio.dll`
  - assembly/file version: `2.0.29.0`
  - SHA-256: `A75346F1CC15FC6E7635E177162C5ADEE7AC65D7AD3E1388EBAFB0F7E3C4E183`
- root `Smile.Language.dll`
  - SHA-256: `D2572A992EB627D1620D68E8BFEDC80003CC7ED4724CF8BA9FF2BF7A1C918B94`
- `Compiler\smilec.exe`
  - SHA-256: `162FA46C75ED0F5A6E4351CE72D32F4C9AA066AC53EE74365DD58B8135CBB22E`
- `Compiler\Smile.Language.dll`
  - SHA-256: `7AA63D948E27838E32B1D9E575CFBEE49E262D6490F78783B47A3E9578C8EF52`
- `Compiler\Smile.NativeRuntime.lib`
  - SHA-256: `D1A64B478BCF36F9E95387840A64107915F283D5C29D62F2ABB3FF54582A2E62`

Every installed hash matches its final repository build artifact. Visual Studio was restarted after installation and loaded the MenuGallery solution with the installed `Smile.UI (1.0.1)` integration.

## Known limitations and deferred work

- Positive system text remains fully opaque; per-draw opacity 1 through 100 is supported by bitmap text.
- Vector fallback windows keep the existing zero-opacity compatibility default rather than treating an all-zero initialized style as invisible.
- The Phase 6 recommendation for an explicit project `ApplicationId` is recorded separately and not implemented.
- No Phase 6 feature, RPG behavior, native UI helper, or speculative 3D feature was added.
