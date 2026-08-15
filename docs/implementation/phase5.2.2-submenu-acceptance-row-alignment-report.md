# Phase 5.2.2 submenu acceptance and row alignment report

## Shipped result

Phase 5.2.2 advances `Smile.UI` to 1.1.2 without adding native menu helpers or Phase 6 behavior. The current navigator's repaired binding table is checked before leaf acceptance, and menu rows now use one bounded prepared-line layout for text, cursor, and submenu-marker placement.

## Acceptance evidence

- Commit: Included in the Phase 5.2.2 shipping commit.
- Branch: `main`.
- Smile.UI version: 1.1.2.
- Package format/hash: formatVersion 5; SHA-256 `E5D137A5406F96740D23A95534C9F345F27C05B8CC03B4945909E87E998BE9C9`.
- VSIX version/path/hash: 2.0.32.0; `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`; SHA-256 `EAC890F684E52218C756FFEBFBCD542D9743410AEDA566A51369C74AE2CA1845`.
- Installed VSIX root: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\ahbg2nv0.jwh`.
- Installed artifact match: extension, language, compiler, and native-runtime payload hashes match the final VSIX build inputs.
- Pre-fix max-depth Enter: returned `UI_EVENT_ACCEPTED` for a bound item at active depth eight.
- Pre-fix max-depth Space: returned `UI_EVENT_ACCEPTED` for the same bound item.
- Pre-fix visual layout: one-line and wrapped labels began at row Y, right markers were independently centered, and cursor offsets began at row Y.
- Bound-item guard: Right, Enter, and Space return `UI_EVENT_NONE` when a current-navigator binding cannot open safely; unbound enabled leaves still accept.
- LastAccepted protection: failed bound opens neither create nor overwrite accepted menu, index, or value state.
- Row preparation design: ellipsis, clip, and wrap prepare bounded visible line text, measured width, exact Y, line count, and overflow before drawing.
- One-line vertical formula: `RowY + Max(0, (RowDrawHeight - LineHeight) / 2)`.
- Multiline block formula: `TextBlockHeight = N * LineHeight + (N - 1) * Max(0, LineSpacing)` and `TextBlockY = RowY + Max(0, (RowDrawHeight - TextBlockHeight) / 2)`.
- Cursor centered formula: `RowY + Max(0, (RowDrawHeight - CursorHeight) / 2)`.
- Cursor offset semantics: `CursorOffsetY` applies after centering, then the destination is clamped to the row; an oversized cursor anchors at row top and remains clipped.
- Right marker baseline: exact ` >` uses prepared first-line Y.
- After-text marker baseline: exact ` >` uses prepared final visible-line Y.
- Wrapped continuation X: every prepared line uses the unchanged label X.
- System text result: one-line and two-line fixed rows center correctly with aligned markers and cursors.
- Bitmap text result: independently measured bitmap line height centers correctly with the same marker and continuation contracts.
- MenuGallery result: reusable-library-only proof covers right/after/hidden markers, system/bitmap/vector themes, cursor offsets, oversized clipping, and ellipsis/clip/wrap modes.
- Project/package tests: 80 exact `True` results from each build, with byte-for-byte logical parity.
- Web draw trace: 40 frames validate exact one-line, wrapped-system, wrapped-bitmap, marker, cursor, clipping, scrollbar, and painter-order geometry.
- Focused tests: 187 managed checks, project/package state checks, and the focused Web trace passed.
- Build: `scripts\build.cmd` passed.
- Smoke: `scripts\smoke-test.cmd` passed in 230.8 seconds with 187 managed, 39 native graphics/audio-focus, and 38 native Text checks.
- Ten-game matrix: all ten demo and no-demo variants compiled for native Windows and Web.
- DirectX: MenuGallery one-line/two-line, right/after/hidden marker, system/bitmap/vector, and cursor alignment accepted visually.
- GDI: the same reusable MenuGallery alignment and marker cases accepted visually.
- Web DPR: a real browser reported DPR 2 with a 640 by 360 CSS canvas and 1280 by 720 backing store; rendering completed with zero console warnings or errors.
- Breakpoint/F10: executable breakpoints bound in `MenuNavigator.HandleKey`, `Menu.PrepareLabel`, and `Menu.DrawFocused`; F10 remained mapped to SMILE source.
- Known limitations: row height remains application-selected and fixed; no public baseline API, automatic row height, mouse/touch input, native menu helper, or Phase 6 behavior was added.
- Uncommitted/untracked files: expected to be none after the shipping commit; generated build artifacts remain ignored.

**No manual testing is requested from Sin because DirectX, GDI, Web DPR-2, and Visual Studio debugging acceptance were completed in this run.**
