# Dual Graphics Backends Implementation Report

Status: In progress

This report tracks the controlled implementation of the approved Direct2D milestone. Public SMILE drawing syntax and the existing native graphics export names remain unchanged.

## Phase ledger

### Phase 1 - Baseline and diagnostics

- Commit: `59181f7`
- Added QPC timing and opt-in graphics diagnostics.
- Recorded the untouched build, runtime, GDI-resource, and fullscreen-quality baseline in `direct2d-baseline.md`.
- Validation: complete smoke suite passed; live diagnostics log verified.

### Phase 2 - Backend abstraction

- Added a backend-neutral vtable covering initialization, resize, frame start, every current primitive, text, numbers, presentation, fullscreen, DPI, shutdown, name, and diagnostics.
- Moved existing GDI drawing and presentation behavior into `graphics_gdi.c`.
- Kept the compiler-facing `smile_*` exports as thin routing functions.
- Kept Windows handles and GDI types out of the common backend interface.
- Validation: complete smoke suite passed in 44.7 seconds; PaddleBall title, gameplay, and a fullscreen round trip were inspected successfully through GDI.

## Known limitations at this stage

- The only selectable backend remains GDI.
- The GDI backend intentionally retains its logical-size DIB and stretched presentation until Phase 3.
- DirectX, Direct2D, and DirectWrite are not initialized until Phases 4-6.
