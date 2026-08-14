# Phase 5.2 reusable submenu navigation implementation report

## Result

Phase 5.2 is complete. `Smile.UI` 1.1.0 now provides a reusable SMILE-authored `Smile.UI.MenuNavigator` over application-owned `Smile.UI.Menu` handles. The reviewed MenuGallery prototype was not used as the navigator design source: its manual depth, positions, routing, and labels were removed, and the sample was rewritten only after the pure project/package state fixture passed.

No compiler keyword, native submenu helper, game-specific menu flow, or Phase 6 feature was added.

## Menu foundation

- Core adds submenu-opened/closed events, ellipsis/clip/wrap modes, automatic/right/left placement directions, and the fixed navigator, depth, binding, item-scalar, and item-line capacities.
- `MenuStyle` adds `ItemTextOverflowMode` and `ItemTextMaxLines`.
- Labels accept at most 256 Unicode scalars. A 256-emoji label succeeds; the 257th scalar is rejected without mutating the menu.
- Ellipsis uses scalar-safe binary fitting and the ASCII suffix `...`. The permanent Web proof observed bounded labels ending in `...`.
- Clip draws only the bounded first logical line. Wrap honors explicit newlines, prefers spaces, hard-breaks long words, permits at most four public lines, respects row height, and ellipsizes the final overflow line. The Web proof observed the exact wrapped first line `LOCALIZATION AND`.
- Every row reserves the same left cursor gutter. The active cursor is clamped left of the text, and automated Web traces proved identical root-label X values across selection states.
- Bound rows draw the exact automatic literal ` >` in a reserved right region using the row's normal, selected, or disabled text style. Callers store plain labels.
- Menu adds generation/revision-safe submenu marker state, `ResetSelection`, `Bounds`, `SetPosition`, `SelectedRowRect`, `DrawFocused`, and revision queries. `Draw` remains the compatibility focused wrapper.

## Navigator

- Capacity: 8 navigator handles, depth 8, and 128 bindings per navigator.
- Handles are generation-safe. The navigator owns only its style, bindings, stack, and last-accepted state; it never destroys a Menu.
- `MenuNavigatorStyle` contains a logical viewport, nonnegative viewport padding, horizontal gap, and auto/right/left direction preference.
- Bind, replace, unbind, clear, shared-child, and reset/preserve-child-selection policies pass.
- Self-links, reachable cycles, active duplicates, a ninth active level, a 129th binding, oversized menus, and invalid style changes reject safely.
- Item revisions prevent an index from silently rebinding after `ClearItems`/repopulation. Stale child, parent, root, and navigator handles prune or fail safely.
- Right, Enter, and Space open an enabled bound submenu. Left and Escape close exactly one level. Escape at the root returns cancel; Left at the root and Right on a leaf are no-ops. Enter/Space on a leaf accept and populate the last-accepted menu/index/value queries.
- Layout is transactional. It clamps the root, aligns a child to the selected parent row, tries the preferred side and fallback, clamps vertically, and uses bounded overlap when neither complete side fits. Every successful layout keeps every complete menu inside the inner viewport.
- Exact focused placement proof: root clamp `(430, 200)`, left fallback child `(242, 210)`, preferred-left child X `112`, with transactional invalid-width rejection.
- Exact four-level MenuGallery Web trace: `(320,250)`, `(578,258)`, `(250,286)`, `(548,298)` in deterministic root-to-leaf painter order.
- `DrawActive` draws only the current menu. `DrawStack` draws root through current, retaining ancestor path fills while focusing only the top menu. Both relayout first.

## MenuGallery rewrite

The sample now uses `MenuNavigator.HandleKey`, `LastAcceptedValue`, `Relayout`, and `DrawStack`. It has no `MenuDepth`, manual active-menu routing, manual placement, or embedded `>` labels. The root plus three submenu levels demonstrate:

- Right/Enter/Space open and Left/Escape back behavior;
- a shared child and a disabled submenu binding;
- reset and preserve selection policies;
- scalar-safe ellipsis and two-line wrapping;
- right placement, left fallback, bottom adjustment, and narrow-space overlap;
- system, bitmap, and vector theme switching without losing the hierarchy;
- app-owned move, confirm, and cancel sound playback;
- leaf acceptance and app-owned dialogue response.

## Focused and permanent tests

- `src\Smile.Tests`: 181 language/compiler/project/completion/timing checks passed.
- `Phase5SubmenuStateTests`: 52 project-reference checks printed `TRUE`.
- `Phase5SubmenuStateTests.Package`: the same 52 packaged-library checks printed `TRUE` with exact line parity.
- `InvalidPhase5Submenus\ConsoleDrawStack`: exactly one consumer-located `Program.smile(7,20)` `SML3704` for `Smile.UI.MenuNavigator.DrawStack`.
- `Phase5SubmenuViewport`: DirectX, GDI, and Web builds passed; the Web program executed.
- MenuGallery project/package native and Web builds passed.
- The 18-frame Web harness exercised Right, Enter, Space, Left, Escape, leaf acceptance, system/bitmap/vector themes, all four active levels, exact markers, stable label X, bounded text, placement/painter order, clipping, high-DPI backing, event WAVs, and final resource/audio cleanup.

## Full regression

- `cmd /c scripts\build.cmd`: passed.
- `cmd /c scripts\smoke-test.cmd`: passed in 230.4 seconds.
- Native runtime: 39 graphics/audio-focus and 38 TEXT checks passed.
- Ten-game matrix: all ten normal and no-demo native builds plus all ten normal and no-demo Web builds passed.
- All prior module, library, typed text, record, IMAGE/media, clipping, persistence, audio, asset-publication, UI, debugger-artifact, native-x64-GUI, and package/provider gates remained green.

## Live acceptance

- Visual Studio 2026 Enterprise instance `91f001b5` displayed the installed `Smile.UI (1.1.0)` project reference and `MenuNavigator.smile` immediately in Solution Explorer.
- A breakpoint bound at `Program.smile` line 267 on `MenuNavigator.HandleKey`. F10 stepped into the referenced library at `MenuNavigator.smile` line 719, proving project-reference breakpoint and source stepping behavior.
- DirectX: the release MenuGallery opened all four levels, showed exactly one top cursor, automatic markers, ellipsis/wrap, left fallback/overlap, back navigation, and live vector theme switching.
- GDI: the explicit release GDI artifact repeated the four-level stack, marker, cursor, ellipsis, placement, overlap, and painter-order result.
- Chrome/Web: the published build opened at `http://127.0.0.1:8765/`, navigated to the four-level stack, backed to two levels, switched to vector rendering, and reported no browser console warnings or errors. The temporary local server was stopped afterward.

## Package and installed Visual Studio artifacts

- Smile.UI package: `D:\SMILE 2.0\artifacts\libraries\Smile.UI.smilelib`
  - version/format: `1.1.0` / `formatVersion 5`
  - SHA-256: `BEF471358C0A6AD17B33C00A4D94FA72A2918969812DD38A7F4769EA431FBC9D`
- VSIX: `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`
  - version: `2.0.30`
  - SHA-256: `32C20191E19109B5073C61556A04D835FD5B00EED907F44B4FBBC9BDA4ECC16E`
- Installed root: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\a2b3gwxe.khk`
- Loaded `Smile.VisualStudio.dll`
  - exact path: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\a2b3gwxe.khk\Smile.VisualStudio.dll`
  - assembly/file version: `2.0.30.0`
  - SHA-256: `EBD47A26CEA15B3BDA34763BF8A8558F77C99752373F1243B1E60973061440DF`
- Loaded root `Smile.Language.dll`
  - exact path: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\a2b3gwxe.khk\Smile.Language.dll`
  - assembly version: `1.0.0.0`
  - SHA-256: `B3387BA54EEE2F67D1D6A9659EDDC5E8E48D123E37F36E16344080D8784990ED`
- Installed compiler: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\a2b3gwxe.khk\Compiler\smilec.exe`
  - SHA-256: `8DCD19BDECA8137C5096343C31B4455EBC1079938D03A9E428153CE18C122234`
- Installed compiler language model: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\a2b3gwxe.khk\Compiler\Smile.Language.dll`
  - assembly version: `1.0.0.0`
  - SHA-256: `397700FDEEC5EFC285BA7D7FD13715F9D31BAA55BF02395FEC623B9310AEA183`
- Installed native runtime: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\a2b3gwxe.khk\Compiler\Smile.NativeRuntime.lib`
  - SHA-256: `CF0AD8D6E446ED51B966300D3152476E7152A0B95A946539A409A391AB422D26`

The installed extension and language assemblies were exclusively locked after Visual Studio loaded the MenuGallery solution. Every installed artifact hash above matches its final built counterpart byte-for-byte.

## Known limitations

- Positive system-text opacity remains fully opaque because generic screen text still has no alpha parameter; bitmap text retains per-draw opacity.
- Rotation and all Phase 6 features remain outside this milestone.
- No additional user manual testing is requested; the required DirectX, GDI, Chrome, breakpoint, and F10 live checks were completed.
