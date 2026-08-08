# Dual Graphics Backends Manual Test Checklist

Use this checklist after `scripts\smoke-test.cmd` passes. Run the same executable once with `--graphics directx` and once with `--graphics gdi`; leave VSync enabled except for the explicit tearing comparison.

## Core visual and input checks

- [ ] Title, gameplay, scores, centered labels, and overlays render correctly.
- [ ] Rectangles, rounded rectangles, circles, and lines retain their intended geometry.
- [ ] Text remains sharp and unclipped in windowed and fullscreen modes.
- [ ] Input remains responsive after resize, minimize/restore, and fullscreen transitions.
- [ ] Audio and persistence behavior are unchanged.
- [ ] Diagnostics report the requested/selected backend, expected pacing mode, and no device-removal reason.

## Required text sample

Compile `examples\GraphicsTextSample.smile` for both backends. Capture or inspect the windowed and fullscreen forms and verify every line:

```text
ABCDEFGHIJKLMNOPQRSTUVWXYZ
abcdefghijklmnopqrstuvwxyz
0123456789
!@#$%^&*()
SCORE
COMPUTER
PLAYER 1
BEST ONE-PLAYER RALLY
```

## Size, DPI, and display matrix

Test only modes actually supported by the available hardware, and record unavailable cases instead of inferring a pass.

- [ ] 960 x 540 windowed.
- [ ] 1280 x 720 windowed.
- [ ] 1920 x 1080 fullscreen.
- [ ] 2560 x 1440 fullscreen, when available.
- [ ] 3840 x 2160 fullscreen, when available.
- [ ] DPI scales 100%, 125%, 150%, and 200%, when available.
- [ ] Refresh rates 60, 100, 120, and 144 Hz, when available.
- [ ] Move between monitors and DPI contexts, when multiple displays are available.

For every tested size, verify uniform letterboxing/pillarboxing, round circles, centered text, and a render target matching the physical client pixels.

## Transition and lifetime checks

- [ ] Continuously resize while gameplay is active.
- [ ] Minimize and restore.
- [ ] Complete 100 windowed-to-fullscreen-to-windowed cycles per backend.
- [ ] Confirm no crash, lost input, bad viewport, persistent topmost state, or text degradation.
- [ ] Compare GDI and USER object counts before and after the cycles.
- [ ] Run Paddle Ball for at least 30 minutes per backend.
- [ ] Compare starting/ending working set and GDI/USER counts.
- [ ] Confirm no progressive frame-time degradation or graphics error in diagnostics.

## Bundled game regression

- [ ] Snake: start, turn in several directions, and verify timer-driven movement.
- [ ] Falling Blocks: move, rotate, soft drop, and hard drop.
- [ ] Paddle Ball: start one-player mode, move the paddle, and complete scoring exchanges.
- [ ] Brick Breaker: start, launch, move the paddle, and hit at least one brick.

## Validation record

Record the date, display topology, available resolution/DPI/refresh modes, backend-specific results, resource counts, diagnostic log names, and any intentionally unavailable cases in `docs\implementation\direct2d-implementation-report.md`.
