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

- Commit: `048942e`
- Added a backend-neutral vtable covering initialization, resize, frame start, every current primitive, text, numbers, presentation, fullscreen, DPI, shutdown, name, and diagnostics.
- Moved existing GDI drawing and presentation behavior into `graphics_gdi.c`.
- Kept the compiler-facing `smile_*` exports as thin routing functions.
- Kept Windows handles and GDI types out of the common backend interface.
- Validation: complete smoke suite passed in 44.7 seconds; PaddleBall title, gameplay, and a fullscreen round trip were inspected successfully through GDI.

### Phase 3 - Physical-resolution GDI

- Commit: `26c684d`
- Replaced the logical-size final DIB with a physical client-size back buffer that is recreated on resize.
- Added reusable uniform viewport, coordinate, size, and deterministic pixel-rounding calculations.
- Mapped every GDI primitive, number, text position, and font size before rasterization.
- Replaced final `StretchBlt` enlargement with a 1:1 `BitBlt`.
- Added bounded brush, pen, and font caches with deterministic shutdown and resize cleanup.
- Selected `CLEARTYPE_NATURAL_QUALITY`, with ClearType and antialiased font-creation fallbacks.
- Added best-effort composition pacing through `DwmFlush` when VSync is requested and DWM composition is available. Set `SMILE_GDI_DWM_FLUSH=0` to disable it for comparison.
- Validation: complete smoke suite passed in 46.7 seconds; fullscreen text and geometry were inspected at 1920x1080; GDI/USER counts remained stable at 34/35 across 20 fullscreen toggles; diagnostics reported `GDI DwmFlush best effort`.

### Phase 4 - DirectX device and swap chain

- Commit: `333bff9`
- Added a C++ DirectX backend behind the existing C-compatible backend interface.
- Added Direct3D 11 hardware-device creation with BGRA support and debug-layer fallback.
- Added a two-buffer `DXGI_SWAP_EFFECT_FLIP_DISCARD` swap chain for the HWND.
- Disabled DXGI's automatic Alt+Enter path so SMILE retains borderless fullscreen control.
- Added the frame-latency waitable-object path with maximum latency one, plus synchronized-present fallback.
- Added render-target recreation on nonzero resize and deterministic reverse-order shutdown.
- Added device-removal diagnostics for present and resize failures.
- Validation: complete smoke suite passed in 45.9 seconds; a DirectX black-frame build survived ten Alt+Enter toggles; diagnostics reported DirectX selection, VSync, 120 Hz output, and the frame-latency waitable pacing path.

### Phase 5 - Direct2D geometry

- Added a Direct2D 1.1 factory, device, device context, and DXGI-surface bitmap target on the Direct3D device.
- Added bounded solid-color brush caching and deterministic target-dependent cleanup.
- Implemented clear, filled and outlined rectangles, rounded rectangles, circles, and lines using the shared physical-resolution viewport mapping.
- Kept letterbox regions black and clipped drawing to the calculated logical viewport.
- Added Direct2D target recreation after resize and `D2DERR_RECREATE_TARGET`.
- Text and numbers intentionally remain absent until the DirectWrite phase.
- Validation: complete smoke suite passed in 43.8 seconds; the live DirectX title and gameplay geometry were inspected successfully; twenty consecutive fullscreen transitions preserved rendering; the process reported 18 GDI objects, 42 USER objects, 58.27 MB working set, and no device-removal reason after the transition test.

## Known limitations at this stage

- The only selectable backend remains GDI.
- DirectX remains a developer-only override until backend selection and fallback are implemented.
- DirectWrite text and number rendering is not initialized until Phase 6.
