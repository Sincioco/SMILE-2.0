# Dual Graphics Backends Implementation Report

Status: Complete

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

- Commit: `47fa8a2`
- Converted Paddle Ball and Brick Breaker ball and paddle movement from per-loop increments to 1,000-unit fixed-point subpixel state.
- Added an 8 ms fixed simulation step, a 50 ms elapsed-time clamp, and a maximum of six catch-up steps per rendered frame.
- Converted Paddle Ball's player paddle to 360 pixels/second, chasing AI to 240 pixels/second, centering AI to 120 pixels/second, and initial ball velocity to approximately 300 by 180 pixels/second. These values preserve the old intended feel at roughly 60 updates/second while remaining stable at other refresh rates.
- Converted Brick Breaker's paddle to 420 pixels/second and its level-one initial ball velocity to approximately 240 by 300 pixels/second, with the existing level-based increases and paddle-contact rules preserved.
- Synchronized subpixel state whenever collision resolution snaps a ball or paddle to a logical boundary.
- Removed loop-delay pacing from all four bundled games. Snake and Falling Blocks already use `Timer()` deadlines for gameplay movement, so removing their redundant waits changes presentation/input cadence without changing their scheduled movement speed.
- Added automated fixed-point speed-consistency and elapsed-clamp checks for simulated frame sequences.
- Validation: complete smoke suite passed in 47.9 seconds with 15 managed timing/project checks and 15 native backend-selection checks; live Direct2D gameplay passed for all four games, including Paddle Ball scoring, a Brick Breaker brick hit, Snake direction changes, Falling Blocks rotation and hard drop, and fullscreen checks for both ball games. All four diagnostics logs reported DirectX, VSync, the DXGI frame-latency waitable object, and no device-removal reason.

### Phase 9 - Hardening and documentation

- Added the required text-comparison sample with uppercase, lowercase, digits, punctuation, and representative game labels, and made the smoke suite compile and verify it as a native x64 GUI executable.
- Added automated DPI-change calculations for 96, 120, 144, and 192 DPI, complementing the seven required output-size viewport cases.
- Expanded fixed-step simulation coverage to exact one-second 60, 100, 120, and 144 Hz frame sequences; ball speed remained identical in every case.
- Updated the public README and architecture guide with backend selection, fallback, VSync, frame pacing, physical-resolution rendering, compiler overrides, and diagnostics.
- Added a reusable manual checklist covering both backends, display modes, text, resize, monitor movement, fullscreen stress, lifetime/resource checks, and all four games.
- Made the native Debug runtime use the compiler-compatible static CRT so a generated SMILE executable can link while retaining the Direct3D and Direct2D debug-layer requests.
- The lifetime run exposed a transient `D2DERR_WRONG_STATE` after minimize/restore. Centralized Direct2D frame closure, ended active frames before resize/minimize, prevented nested `BeginDraw`, invalidated the common frame state on resize/fullscreen/DPI changes, and added native frame-invalidation regression checks. The corrected Release and Debug builds then completed minimize/restore and rapid fullscreen stress without another graphics error.
- Visual validation: the complete text sample remained centered, unclipped, and sharp through both DirectWrite and physical-resolution GDI in windowed mode and at 1920 x 1080 fullscreen. DirectX reported the frame-latency waitable object; GDI reported `DwmFlush` best-effort pacing.
- Transition validation: each backend completed 100 fullscreen round trips plus additional plateau cycles. After the frame-state correction, DirectX completed another 100-round-trip run with zero graphics errors, an unchanged 19 GDI objects, and 41 to 43 USER objects before settling at 40 during the lifetime run. GDI stabilized at 21 GDI and 30 USER objects in the isolated text-sample transition test. Both remained windowed, responsive, and visually correct afterward.
- Window validation: active Paddle Ball gameplay survived keyboard-driven live resize, minimize/restore, movement between both attached 1920 x 1080 displays, and a fullscreen round trip after the monitor move for both backends. Both displays reported 96 DPI.
- Debug validation: the Debug native runtime rebuilt, linked into an explicit DirectX text-sample executable, rendered correctly, survived a fullscreen round trip, and reported no device-removal reason.
- Lifetime validation: Paddle Ball ran for 31.9 minutes through DirectX and 33.0 minutes through GDI, with repeated active rematches. DirectX held 19 GDI objects, moved from 41 to 40 USER objects, changed from 62.96 to 64.84 MB working set and 65.23 to 66.70 MB private memory, and reduced its handle count from 797 to 777. GDI warmed its bounded caches from 34 to 44 GDI objects in the first ten minutes and remained at 44 through completion, moved from 37 to 35 USER objects, changed from 24.27 to 26.11 MB working set and 6.04 to 6.46 MB private memory, and reduced handles from 342 to 316. Both logs contained zero graphics errors. With both processes foregrounded in the same active-game state, the completed DirectX run measured 115.3 FPS versus 113.0 FPS for a fresh comparison process, showing no progressive frame-time degradation.
- Final game validation: the corrected Auto/DirectX build passed Snake direction changes, Falling Blocks rotation and hard drop, a scored Brick Breaker hit, and Paddle Ball gameplay; every game log selected DirectX and contained zero graphics errors.
- Final automated validation: the complete Release smoke suite passed in 47.8 seconds with 15 managed project/timing tests, 19 native selection/frame-invalidation checks, every console/runtime/diagnostic/storage regression, both graphical samples and all four games compiled as native x64 GUI executables, seven output-size viewport cases, and four DPI-scale calculation cases.

## Files changed

- Runtime architecture: `src/Smile.NativeRuntime` now contains the backend-neutral router, GDI and DirectX implementations, diagnostics, and QPC frame clock; its project links the Windows DirectX, DirectWrite, DWM, GDI, audio, and shell libraries.
- Compiler/project integration: `src/Smile.Language`, `src/Smile.Compiler`, and `src/Smile.VisualStudio` parse and carry backend/VSync settings without adding public SMILE syntax.
- Games: the four `.smileproj` files declare safe defaults; Paddle Ball and Brick Breaker use fixed-point fixed-step movement; Snake and Falling Blocks retain their timer deadlines without redundant loop delays.
- Tests and examples: managed project/timing tests, native selection/fallback tests, artifact viewport/DPI checks, the required graphics text sample, and the expanded smoke suite.
- Documentation: baseline and implementation reports, public README, architecture guide, and manual test checklist.

## Architectural decisions

- Existing `smile_*` graphics exports and the logical canvas contract remain stable. The compiler emits only one backend configuration call before game startup.
- `Auto` tries DirectX and falls back to GDI with the exact DirectX initialization failure retained for diagnostics. Explicit backend choices never silently select another backend.
- DirectX uses D3D11 BGRA resources, a two-buffer DXGI flip-discard swap chain, maximum frame latency one, Direct2D geometry, and DirectWrite text. GDI remains a maintained physical-resolution backend with bounded object caches and one-to-one presentation.
- Both backends share the uniform viewport calculations, draw at physical client resolution, and react to resize, DPI change, and borderless Alt+Enter without exposing those details to SMILE programs.
- Ball-game simulation is separated from presentation cadence through an 8 ms fixed step, fixed-point subpixel state, a 50 ms elapsed clamp, and a six-step catch-up limit.

## Approved deviations and validation boundaries

- At the user's request, the refresh-independent fixed-step treatment was extended from Paddle Ball to Brick Breaker because both have fast ball/paddle movement. Snake and Falling Blocks were inspected and needed only removal of redundant waits because their movement was already scheduled by `Timer()` deadlines.
- The plan's suggested file layout was followed by responsibility rather than by a wholesale runtime rewrite: existing Win32 window/input/audio/persistence code remains in `runtime.c`, while graphics and timing are separated into dedicated modules.
- Automated calculations cover 2560 x 1440, 3440 x 1440, 3840 x 2160, and 125/150/200 percent DPI. Live hardware validation is limited to the two attached 1920 x 1080, 96-DPI displays, which exposed 120 Hz and 60 Hz paths; higher physical resolutions, alternate DPI scales, and 100/144 Hz modes were not claimed as live passes.
- The Debug build requests Direct3D and Direct2D debug layers and falls back if the optional Direct3D SDK layer is absent. No debugger-based COM live-object report was available in this unattended run, so fullscreen plateau counts, working set/handle measurements, deterministic shutdown code, and device-removal diagnostics provide the leak evidence.
