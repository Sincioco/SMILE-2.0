# Phase 5.2.1 submenu UI hardening implementation report

Date: 2026-08-15

## Result

Phase 5.2.1 is complete. `Smile.UI` 1.1.1 hardens hierarchical menu rendering and state coherence while keeping the implementation in shared SMILE code. Every active level now retains its cursor, programmatic selection and disabled parent rows prune invalid descendants, scrollbars have stable optional layout and proportional thumbs, and submenu indicators support hidden, after-text, and right-aligned modes.

No compiler keyword, native menu helper, game-specific UI flow, Phase 6 feature, or deferred VSIX hover/F12/startup-project feature was added.

## Release identity

- **Commit:** This report is included in the Phase 5.2.1 commit with subject `Sin and Codex: harden hierarchical menu presentation`; use the repository history for its immutable hash.
- **Branch:** `main`.
- **Smile.UI version:** `1.1.1`.
- **Package format/hash:** `formatVersion 5`; `D:\SMILE 2.0\artifacts\libraries\Smile.UI.smilelib`; SHA-256 `0AC53A8291C71478188FC98282622E5D3253163E577966FB3F18DA23B11EAEAC`.
- **VSIX version/path/hash:** `2.0.31`; `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`; SHA-256 `A357E44C4622396D90F512E25BC4AD67ABEB6977869ADA5BDED3633A8294F73E`.

## State-coherence proof

- **Pre-fix edge-coherence result:** The isolated baseline fixture printed `TRUE FALSE FALSE FALSE`. The root selection changed programmatically, but the child, grandchild, and great-grandchild remained active even though their recorded parent edges were no longer valid.
- **Edge-coherence fix:** `MenuNavigator.RepairSlot` now validates every active edge against the current parent selection and recorded parent item, pruning that edge and all descendants plus stale accepted state at the first mismatch.
- **Programmatic selection pruning:** Covered by project and packaged-library state fixtures and by live MenuGallery key `X`; a four-level stack immediately returned to the root after its parent selection changed.
- **Disabled parent-item pruning:** Disabling the active parent item invalidates the edge and prunes the child stack without transferring ownership or corrupting selection state.
- **Cursor depth 1/2/3/4:** All four depths render one cursor per active menu in deterministic root-to-leaf painter order. The live four-level DirectX, GDI, and Chrome runs showed four simultaneous cursors.
- **Cursor text-X stability:** A fixed left cursor gutter keeps label X unchanged across focused/unfocused and selected/unselected states; the automated Web trace asserts the same X coordinate through the complete interaction.

## Scrollbar and indicator proof

- **ShowScrollbar toggle:** `FALSE` reclaims the entire scrollbar gutter. `TRUE` reserves a stable gutter whenever the style enables scrollbars, preventing label-width and marker jitter as item counts change.
- **Scrollbar proportional sizing:** The focused viewport fixture covers 4/5, 4/20, and 4/64 visible/total ratios plus all-visible and tiny-track cases. The thumb is bounded, nonzero when drawable, and never exceeds the track.
- **Scrollbar proportional position:** Top, middle, and bottom selections map proportionally to the available thumb travel, including tiny tracks, without division by zero.
- **Submenu indicator hidden:** `ShowSubmenuIndicator = FALSE` suppresses marker drawing while Right/Enter/Space navigation remains active.
- **Submenu indicator after-text:** `UI_SUBMENU_INDICATOR_AFTER_TEXT` appends the exact marker after the final visible label line and participates safely in clip, ellipsis, and wrap fitting.
- **Submenu indicator right-aligned:** `UI_SUBMENU_INDICATOR_RIGHT_ALIGNED` reserves a right marker region immediately before any scrollbar gutter.
- **Exact `" >"` marker:** The library draws the exact two-character literal ` >`; callers continue storing plain labels.
- **Long-label interactions:** Ellipsis reserves marker width, clip never draws a marker-only extra line, and wrap places an after-text marker only on the final visible label line.
- **Marker + scrollbar:** Right-aligned markers draw before the stable scrollbar gutter with deterministic label, marker, and scrollbar ordering and no overlap.
- **Viewport behavior:** Explicit tests cover overflow, all-visible, scrollbar-disabled, gutter reclamation, stable gutter reservation, and tiny track geometry on shared logical coordinates.

## Automated validation

- **Project/package state tests:** `Phase5SubmenuStateTests` and `Phase5SubmenuStateTests.Package` each printed exactly 65 `TRUE` lines with exact parity. The invalid pure-console capability fixture produced the intended consumer-located `SML3704`.
- **Web trace:** The 33-frame MenuGallery harness covered depths 1 through 4, all cursor states, top/middle/bottom scrolling, scrollbar toggling, hidden/after-text/right-aligned indicators, long-label overflow, four-to-one programmatic pruning, theme changes, viewport bounds, painter order, audio events, and final cleanup.
- **DirectX:** Live MenuGallery acceptance covered the four-level stack, four cursors, both marker placements, hidden markers with working navigation, ellipsis/wrap, scrollbar top/middle/bottom and toggle behavior, automated pruning, back navigation, and bitmap/vector themes.
- **GDI:** The explicit GDI artifact repeated the four-level cursor, marker, scrolling, long-label, pruning, navigation, and painter-order checks.
- **Web:** Chrome ran the published build at device-pixel-ratio 2 with an 839 by 472 CSS canvas and 1678 by 944 backing canvas. The complete four-level, scrollbar, marker, long-label, pruning, and theme sequence passed with no console warnings or errors. The temporary server and test tab were closed.
- **Breakpoints/F10:** Visual Studio bound in `MenuNavigator.RepairSlot`, `MenuNavigator.DrawStack`, and `Menu.DrawFocused`; F10 advanced within mapped `.smile` source. Solution Explorer displayed `Smile.UI (1.1.1)`, and completion exposed the two new style fields and both indicator constants.
- **Build:** `cmd /c scripts\build.cmd` passed in 14.1 seconds.
- **Smoke:** `cmd /c scripts\smoke-test.cmd` passed with exit code 0 in 225.3 seconds: 181 language/compiler/project/completion/timing tests, 39 native graphics/audio-focus checks, and 38 native TEXT checks passed.
- **Ten-game matrix:** All ten normal and no-demo native builds and all ten normal and no-demo Web builds passed. Prior Phase 1 through Phase 5.2 package, provider, media, UI, debugger, DirectX, GDI, and browser gates remained green.

## Installed Visual Studio artifacts

Visual Studio 2026 Enterprise instance `91f001b5` loaded the final extension after installation. The loaded DLL was held by the running IDE after `MenuGallery.slnx` and `Program.smile` opened, and the verification-only IDE session then closed cleanly.

- Loaded `Smile.VisualStudio.dll`
  - exact path: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\mc4fhwc3.xfq\Smile.VisualStudio.dll`
  - assembly/file version: `2.0.31.0`
  - SHA-256: `B2B99935E5B17EBD73CB4B6331AEBF7C972502A741C96DC5F40F1A0643060675`
- Installed root `Smile.Language.dll`
  - assembly version: `1.0.0.0`
  - SHA-256: `D745B4C5E2F2376DB8A4D7197F29D9F79876CD63D9374466E23CD98EE03C1C0B`
- Installed `Compiler\Smile.Language.dll`
  - assembly version: `1.0.0.0`
  - SHA-256: `C87C1DA866C7EC7DEEF87E4B5DF038E7483DCB7FABC502C3575F46F119944C34`
- Installed `Compiler\Smile.NativeRuntime.lib`
  - SHA-256: `58CCD8C444094D7AA864F3AB52F9729E894F7AF00D41516D240760000940741A`
- Installed `Compiler\smilec.dll`
  - assembly/file version: `1.0.0.0`
  - SHA-256: `7090E548B1DC6CF7CC09B1CB718875F0CAB88C8E84F44AF8F69E47FC388AA570`
- Installed `Compiler\smilec.exe`
  - assembly/file version: `1.0.0.0`
  - SHA-256: `D107DF1D04E7FC171CB9978B68500B527911A946A30BEB53C06BC9B70F46A477`

Every installed payload above matches its final VSIX build counterpart byte-for-byte.

## Known limitations and final tree

- **Known limitations:** Rotation and Phase 6 remain outside this milestone. Direct calls to lower-level `Menu.DrawFocused(..., FALSE)` intentionally suppress that individual menu's cursor; `MenuNavigator.DrawStack` now passes focused rendering for every active level as specified. No mandatory Phase 5.2.1 limitation remains.
- **Uncommitted/untracked files:** The pre-commit review contained only the 22 intended tracked modifications plus this report and no unrelated or untracked files. The final handoff requires and verifies a clean status after push.
- No additional user manual testing is requested; DirectX, GDI, Chrome, breakpoint, F10, package, and complete regression acceptance were completed in this run.
