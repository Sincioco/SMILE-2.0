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

- Commit: `2527cf9`
- Added a Direct2D 1.1 factory, device, device context, and DXGI-surface bitmap target on the Direct3D device.
- Added bounded solid-color brush caching and deterministic target-dependent cleanup.
- Implemented clear, filled and outlined rectangles, rounded rectangles, circles, and lines using the shared physical-resolution viewport mapping.
- Kept letterbox regions black and clipped drawing to the calculated logical viewport.
- Added Direct2D target recreation after resize and `D2DERR_RECREATE_TARGET`.
- Text and numbers intentionally remain absent until the DirectWrite phase.
- Validation: complete smoke suite passed in 43.8 seconds; the live DirectX title and gameplay geometry were inspected successfully; twenty consecutive fullscreen transitions preserved rendering; the process reported 18 GDI objects, 42 USER objects, 58.27 MB working set, and no device-removal reason after the transition test.

### Phase 6 - DirectWrite text and numbers

- Commit: `dff11e4`
- Added a shared DirectWrite factory and bounded final-resolution Consolas Bold text-format cache.
- Converted SMILE UTF-8 text to Unicode and created a fresh text layout for each distinct string draw.
- Measured centered text using final-resolution layout metrics and routed numbers through the same text path.
- Selected grayscale DirectWrite text antialiasing for stable results on the DXGI surface.
- Kept DirectWrite factory and formats device-independent while retaining Direct2D target-dependent recreation on resize.
- Validation: complete smoke suite passed in 45.7 seconds; live title, centered labels, dynamic scores, and the game-over panel rendered correctly; fullscreen text remained sharp at 1920x1080; twenty consecutive fullscreen transitions completed with 18 GDI objects, 40 USER objects, and 62.4 MB working set.

### Phase 7 - Project selection and fallback

- Commit: `85350e3`
- Added shared `.smileproj` parsing for `<GraphicsBackend>Auto|GDI|DirectX</GraphicsBackend>` and `<VSync>true|false</VSync>`, with required defaults and clear invalid-value diagnostics.
- Passed project settings from the Visual Studio project system through `smilec` into a stable native `smile_graphics_configure` call emitted before game startup.
- Added simple `--graphics` and `--vsync` CLI overrides without introducing a new command framework.
- Made `Auto` try DirectX first, release partial DirectX state after failure, retain the failure reason, and continue through GDI; explicit DirectX never falls back, and explicit GDI never initializes DirectX.
- Added legal VSync-off behavior by detecting DXGI tearing support and applying swap-chain and present flags only when supported.
- Added eleven managed project-option tests and fifteen native selection/fallback checks to the smoke suite.
- Added explicit Auto/VSync defaults to the four bundled game projects and the Visual Studio game template.
- Validation: complete smoke suite passed in 45.1 seconds with both new test executables; default Auto selected DirectX with no fallback; explicit GDI reported GDI pacing; explicit DirectX with VSync off reported legal low-latency tearing presentation; an invalid CLI backend returned `SML5007`; live DirectX title, gameplay controls, and fullscreen rendering passed for Snake, Falling Blocks, and Brick Breaker with no device-removal reason.

### Phase 8 - Refresh-independent game timing

- Converted Paddle Ball and Brick Breaker ball and paddle movement from per-loop increments to 1,000-unit fixed-point subpixel state.
- Added an 8 ms fixed simulation step, a 50 ms elapsed-time clamp, and a maximum of six catch-up steps per rendered frame.
- Converted Paddle Ball's player paddle to 360 pixels/second, chasing AI to 240 pixels/second, centering AI to 120 pixels/second, and initial ball velocity to approximately 300 by 180 pixels/second. These values preserve the old intended feel at roughly 60 updates/second while remaining stable at other refresh rates.
- Converted Brick Breaker's paddle to 420 pixels/second and its level-one initial ball velocity to approximately 240 by 300 pixels/second, with the existing level-based increases and paddle-contact rules preserved.
- Synchronized subpixel state whenever collision resolution snaps a ball or paddle to a logical boundary.
- Removed loop-delay pacing from all four bundled games. Snake and Falling Blocks already use `TIMER()` deadlines for gameplay movement, so removing their redundant waits changes presentation/input cadence without changing their scheduled movement speed.
- Added automated fixed-point speed-consistency and elapsed-clamp checks for simulated frame sequences.
- Validation: complete smoke suite passed in 47.9 seconds with 15 managed timing/project checks and 15 native backend-selection checks; live Direct2D gameplay passed for all four games, including Paddle Ball scoring, a Brick Breaker brick hit, Snake direction changes, Falling Blocks rotation and hard drop, and fullscreen checks for both ball games. All four diagnostics logs reported DirectX, VSync, the DXGI frame-latency waitable object, and no device-removal reason.

## Known limitations at this stage

- Extended visual, resize, resource-lifetime, and long-run validation remains for the final hardening phase.
