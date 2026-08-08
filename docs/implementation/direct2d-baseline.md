# Direct2D Milestone Baseline

Baseline recorded on August 8, 2026 before renderer changes at commit `3d7ce03`.

## Repository state

- Branch: `main`, aligned with `origin/main`.
- Existing user work was present and left untouched: one modified requirements file plus untracked game solution files, a Falling Blocks MP3, and the approved Direct2D plan.
- Native graphics ABI: Windows x64 C calling convention with the existing `smile_game_open`, `smile_game_clear`, primitive, text, number, presentation, and window-state exports.

## Automated baseline

`scripts\smoke-test.cmd` completed successfully in 48 seconds.

- Runtime, compiler, solution, and VSIX built successfully.
- Console and language smoke checks passed.
- Structured-language and game-language diagnostics passed.
- Storage behavior passed.
- GraphicsBasics and all four game programs compiled.
- All five graphical outputs were verified as native x64 GUI executables.
- Game assets and VSIX payload were verified.
- Existing 960x540 scale math checks passed.

## Runtime baseline

- PaddleBall title screen: 14 GDI objects, 30 USER objects, 19.95 MB working set.
- PaddleBall during play: 18 GDI objects, 36 USER objects, 22.98 MB working set.
- Windowed rendering was stable and flicker-free during the observation.
- Fullscreen used the logical 960x540 DIB enlarged with `StretchBlt(COLORONCOLOR)`.
- Fullscreen text and score glyphs visibly showed the expected enlarged pixel edges.
- Movement was driven by fixed per-loop increments plus `WAIT 8 MILLISECONDS`; no presentation timing diagnostics existed.

Manual resolution, DPI, refresh-rate, long-run, monitor-move, and 100-cycle fullscreen tests remain part of the final milestone validation.

## Phase 1 stop condition

- Added a QueryPerformanceCounter frame clock without changing the rendering path.
- Preserved `TIMER()` milliseconds while moving its monotonic source to QPC.
- Added opt-in logging through `SMILE_GRAPHICS_DIAGNOSTICS=1`; release games remain quiet by default.
- A live diagnostics run reported the backend, 960x540 logical and physical sizes, viewport, 120 Hz display, requested VSync, legacy GDI pacing, FPS, and frame/draw/present timings.
- Post-change `scripts\smoke-test.cmd` passed in 44.7 seconds.
