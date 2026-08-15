# SMILE 2.0 — Dual Graphics Backends, Frame Pacing, and Resolution-Independent Rendering Plan

**Document type:** Codex implementation plan  
**Project:** SMILE 2.0 / SinBASIC  
**Date:** August 8, 2026  
**Primary platform for this milestone:** Windows  
**Status:** Approved direction; implement in controlled stages

---

## 1. Purpose

SMILE 2.0 can currently create simple graphical games using the Windows Graphics Device Interface, or GDI. The existing implementation proves that the language, compiler, native runtime, window management, keyboard input, drawing commands, sound, fullscreen switching, and game packaging can work together.

The current renderer is an early proof of concept. It has two visible weaknesses:

1. Fast-moving objects can appear uneven, torn, stretched, or temporarily oblong.
2. Text that looks acceptable in the normal 960×540 window becomes pixelated when the completed low-resolution frame is enlarged to fullscreen.

This plan modernizes the Windows game renderer while preserving SMILE’s beginner-friendly programming model.

The required result is:

- A backend-neutral SMILE graphics interface.
- A maintained and improved GDI compatibility backend.
- A modern DirectX backend.
- DirectX selected automatically when available.
- VSync and predictable presentation by default.
- Rendering performed at the current output resolution.
- Text rendered at the final display resolution instead of enlarged from a low-resolution bitmap.
- Stable elapsed-time-aware movement in PaddleBall.
- No DirectX- or GDI-specific syntax in ordinary SMILE programs.
- No requirement for a child or beginner to understand resolutions, DPI, scaling factors, swap chains, frame queues, or graphics devices.

---

## 2. Codex Operating Instructions

Before changing code, Codex must:

1. Read this entire document.
2. Inspect the current `main` branch.
3. Locate and review at minimum:
   - `games/PaddleBall/Program.smile`
   - `games/PaddleBall/PaddleBall.smileproj`
   - `src/Smile.NativeRuntime/runtime.c`
   - The native-runtime build configuration.
   - The compiler emitter code that calls the native graphics functions.
   - Existing tests and smoke-test scripts.
4. Record the current build and test results before editing.
5. Confirm the current external runtime function names and calling convention.
6. Implement this work incrementally rather than replacing the complete runtime in one step.

Codex must not:

- Invent new public SMILE syntax without a separate approved language specification.
- Remove GDI after DirectX works.
- expose DirectX or GDI objects through the public SMILE API.
- Force existing SMILE games to contain renderer-specific code.
- Replace the existing logical coordinate model with physical screen coordinates.
- Depend on third-party graphics packages for the Windows backend.
- Make PaddleBall’s speed depend on a hard-coded `Wait` duration.
- Claim that changing `COLORONCOLOR` to `HALFTONE` alone fixes fullscreen text.
- Combine all changes into one large, difficult-to-review commit.

Each implementation stage must leave the repository building and the existing test suite passing before Codex proceeds to the next stage.

---

## 3. Confirmed Current-State Findings

Codex must verify these findings against the repository before implementation and update this section in the implementation report if the code has changed.

### 3.1 PaddleBall logical canvas

PaddleBall uses a logical game area of:

```text
960 × 540
```

The game source draws all objects in this logical coordinate system.

### 3.2 PaddleBall movement

The current game advances the ball by a fixed amount once per loop:

```smile
BallX = BallX + BallVX
BallY = BallY + BallVY
```

The loop ends with:

```smile
Show Screen
Wait 8 Milliseconds
```

This means physical game speed depends on how often the loop executes. `Wait 8 Milliseconds` requests a wait, but it does not guarantee that every displayed frame arrives exactly eight milliseconds after the previous frame.

### 3.3 Current GDI back buffer

The native runtime creates a 32-bit GDI DIB section using the logical game dimensions. Rectangles, rounded rectangles, circles, lines, text, and numbers are drawn into that logical-resolution memory DC.

### 3.4 Current text path

The current runtime creates a GDI font and uses `TextOutW` against the logical back buffer. The letters have therefore already become low-resolution pixels before fullscreen scaling occurs.

### 3.5 Current presentation path

`Show Screen` obtains a destination DC and enlarges the logical back buffer with `StretchBlt` using `COLORONCOLOR`.

At 1920×1080, a 960×540 frame is enlarged by approximately 2× in both dimensions.

At 3840×2160, the same frame is enlarged by approximately 4× in both dimensions.

The current fullscreen path therefore enlarges the pixels of already-rasterized text rather than asking the font renderer to create more detailed glyphs at the larger resolution.

### 3.6 Current synchronization limitation

The current GDI `GetDC` → `StretchBlt` → `ReleaseDC` path does not provide a DXGI swap chain, a flip-model presentation queue, a synchronization interval, or a frame-latency waitable object.

The RTX 5090 is not the likely performance limitation for PaddleBall. The important limitations are presentation synchronization, frame pacing, fixed-per-loop movement, low-resolution rasterization, and scaling quality.

---

## 4. Approved Architectural Decisions

### 4.1 Keep SMILE drawing commands backend-neutral

Existing code must continue to work:

```smile
Clear BLACK
Fill Rectangle 100, 100, 200, 50, BLUE
Fill Circle BallX, BallY, BallRadius, WHITE
Draw Text "Score" At 40, 25 Size 16 Color YELLOW
Show Screen
```

Do not create public statements such as:

```smile
DIRECTX Fill Circle ...
GDI Draw Text ...
```

### 4.2 Support three backend selections

The `.smileproj` format will support:

```xml
<GraphicsBackend>Auto</GraphicsBackend>
<VSync>true</VSync>
```

Allowed `GraphicsBackend` values:

| Value | Required behavior |
|---|---|
| `Auto` | Try DirectX first. Fall back to GDI if DirectX initialization fails. Record the fallback reason. |
| `GDI` | Explicitly use the improved GDI backend. |
| `DirectX` | Require DirectX. Report an actionable error if initialization fails. |

When the property is omitted, the default is:

```text
Auto
```

When `VSync` is omitted, the default is:

```text
true
```

### 4.3 Use the following DirectX stack

The Windows DirectX backend will use:

- Direct3D 11 for the graphics device and GPU resources.
- DXGI for the swap chain and presentation.
- Direct2D for 2D primitives and future bitmap drawing.
- DirectWrite for text.
- Borderless fullscreen for Alt+Enter.

Direct3D 12 is explicitly out of scope for this milestone.

### 4.4 Preserve a simple logical canvas

A SMILE game continues to use logical dimensions such as 960×540.

The runtime handles:

- Window size.
- Fullscreen display size.
- Uniform scaling.
- Letterboxing and pillarboxing.
- DPI changes.
- Native-resolution shape rendering.
- Native-resolution text rendering.

### 4.5 Render at the actual output resolution

Do not render a complete low-resolution frame and enlarge it at the end.

The active backend creates a render target matching the current client-area pixel dimensions. Logical coordinates are mapped into that target before rasterization.

### 4.6 VSync is enabled by default

The DirectX backend will normally present with a synchronization interval of one:

```cpp
swapChain->Present(1, 0);
```

Tearing must not be enabled by default.

### 4.7 Preserve GDI as a real supported backend

GDI is not merely retained unchanged. It will receive the improvements defined in Section 9 of this document.

GDI remains useful for:

- Compatibility.
- Diagnostics.
- Backend comparison.
- DirectX initialization fallback.
- Verifying that SMILE’s public drawing API is genuinely backend-neutral.

---

## 5. Goals

### 5.1 Visual goals

- Text remains sharp in windowed and fullscreen modes.
- Text is rerendered at the current output resolution.
- Circles remain round.
- Lines and shape edges are stable.
- No visible tearing with DirectX VSync enabled.
- No normal GDI flicker.
- No stretching caused by different horizontal and vertical scale factors.
- Alt+Enter remains automatic.

### 5.2 Timing goals

- PaddleBall movement is based on elapsed time rather than loop count.
- Movement speed remains materially consistent at 60, 100, 120, 144, and other refresh rates.
- Presentation pacing is measurable.
- Long pauses and breakpoints do not cause a giant physics jump.
- The runtime can report actual frame time and FPS.

### 5.3 Architecture goals

- Graphics implementation is separated from windowing, input, timing, persistence, and audio.
- Compiler-generated code calls stable backend-neutral exports.
- The DirectX implementation may be C++ behind a C ABI.
- Reusable graphics resources are cached.
- Device loss, resize, minimization, and shutdown are handled safely.
- No GDI or COM resource leaks occur.

### 5.4 Beginner-experience goals

Students must not be required to:

- Choose a screen resolution.
- Calculate scale factors.
- Change font size for fullscreen.
- Write DirectX initialization code.
- Write GDI initialization code.
- Manage a swap chain.
- Implement Alt+Enter.
- Know the monitor’s refresh rate.
- Make game speed depend on FPS.

---

## 6. Non-Goals

This milestone does not include:

- 3D graphics.
- Direct3D 12.
- Public shader support.
- A physics engine.
- A scene graph.
- A particle engine.
- A complete sprite-animation language.
- A tile-map engine.
- A cross-platform graphics backend.
- Exclusive fullscreen.
- A public font-selection language.
- Removal of GDI.
- A complete redesign of SMILE’s game-loop syntax.
- A new public `FRAME_TIME()` function without a separate specification.

Do not broaden the implementation into these areas.

---

## 7. Required Runtime Structure

### 7.1 Separate responsibilities incrementally

Refactor toward a structure comparable to:

```text
src/Smile.NativeRuntime/
    runtime_core.c
    runtime_exports.h

    platform/
        window_win32.c
        window_win32.h
        input_win32.c
        input_win32.h

    graphics/
        graphics_backend.h
        graphics_common.c
        graphics_common.h
        graphics_gdi.c
        graphics_gdi.h
        graphics_directx.cpp
        graphics_directx.h

    timing/
        frame_clock_win32.c
        frame_clock_win32.h
```

The exact file names may follow existing repository conventions, but responsibilities must be separated.

Do not rewrite the entire runtime in one step.

### 7.2 Preserve the external runtime ABI

Compiler-generated programs must continue using stable exports such as:

```text
smile_game_open
smile_game_clear
smile_fill_rectangle
smile_draw_rectangle
smile_fill_rounded_rectangle
smile_draw_rounded_rectangle
smile_fill_circle
smile_draw_circle
smile_draw_line
smile_draw_text
smile_draw_number
smile_show_screen
smile_game_closed
```

These exports become thin routing functions that call the selected backend.

### 7.3 Define a backend-neutral internal interface

Create an internal interface that supports at minimum:

```text
initialize
resize
begin_frame
clear
fill_rectangle
draw_rectangle
fill_rounded_rectangle
draw_rounded_rectangle
fill_circle
draw_circle
draw_line
draw_text
draw_number
present
on_fullscreen_changed
on_dpi_changed
shutdown
get_backend_name
get_diagnostics
```

The common interface must not expose:

- `HDC`
- `HBITMAP`
- `ID3D11Device`
- `ID2D1DeviceContext`
- `IDXGISwapChain`
- `HRESULT`

### 7.4 Keep rendering single-threaded for this milestone

All window messages, game drawing calls, and presentation remain on the game/window thread.

Do not add a separate rendering thread yet.

---

## 8. Resolution-Independent Rendering

### 8.1 Terminology

**Logical size**  
The dimensions used by SMILE source code, such as 960×540.

**Physical output size**  
The current client-area or swap-chain buffer dimensions in physical pixels.

**Viewport**  
The physical rectangle used to display the logical canvas while preserving aspect ratio.

### 8.2 Uniform scaling

Calculate:

```text
scaleX = physicalWidth  / logicalWidth
scaleY = physicalHeight / logicalHeight
scale  = min(scaleX, scaleY)
```

Then calculate:

```text
viewportWidth  = logicalWidth  × scale
viewportHeight = logicalHeight × scale

viewportX = (physicalWidth  - viewportWidth)  / 2
viewportY = (physicalHeight - viewportHeight) / 2
```

Use one uniform scale for both axes.

Never independently stretch width and height because that can turn circles into ellipses.

### 8.3 Coordinate mapping

Map logical positions:

```text
physicalX = viewportX + logicalX × scale
physicalY = viewportY + logicalY × scale
```

Map logical dimensions:

```text
physicalWidth  = logicalWidth  × scale
physicalHeight = logicalHeight × scale
```

### 8.4 Letterboxing and pillarboxing

Clear the complete physical target to black before rendering the viewport.

Use:

- Letterboxing when the display is relatively taller.
- Pillarboxing when the display is relatively wider.

### 8.5 Precision and rounding

- Keep viewport calculations in floating-point or equivalent high-precision internal values.
- Do not round logical values before scaling.
- For GDI, map and then apply deterministic physical-pixel rounding.
- For Direct2D, retain floating-point coordinates and use the rasterizer’s antialiasing.
- Derive both circle axes from the same mapped radius.

### 8.6 Resize and fullscreen behavior

Recalculate the viewport when:

- The window client area changes.
- Alt+Enter toggles fullscreen.
- The window moves between displays with different DPI.
- The physical swap-chain buffers are resized.

The SMILE program must not have to redraw differently for these events; it continues issuing the same logical drawing commands each frame.

---

## 9. Required GDI Enhancements

This section confirms that the plan includes the GDI improvements previously recommended.

### 9.1 Replace the logical-size final buffer

The permanent GDI backend must no longer use a fixed 960×540 DIB as the final presentation buffer when the client area is larger.

Create a GDI back buffer matching the current physical client-area dimensions.

Recreate it when the client size changes.

### 9.2 Map logical coordinates before drawing

Every GDI primitive will map logical coordinates into the physical viewport before rasterization.

This applies to:

- Rectangles.
- Rounded rectangles.
- Circles.
- Lines.
- Text.
- Numbers.

The GDI backend must therefore render shapes at the current physical resolution instead of enlarging a finished low-resolution frame.

### 9.3 Render GDI text at final resolution

For each text operation:

1. Map the logical X and Y position into physical viewport coordinates.
2. Calculate the physical font height from the logical font size and viewport scale.
3. Retrieve or create a cached `HFONT` for that physical size.
4. Draw the text directly into the physical-size DIB.
5. Present the DIB 1:1.

Example:

```smile
Draw Text "Score" At 40, 25 Size 16 Color WHITE
```

At 1920×1080 with a 2× viewport scale, GDI must create and rasterize an appropriately scaled font at the larger output size. It must not draw 16-pixel text into a 960×540 bitmap and double those pixels.

### 9.4 Use one-to-one final presentation

After the physical-size GDI back buffer is complete, copy it to the window without scaling.

Use a one-to-one `BitBlt` or equivalent final copy.

Do not use `StretchBlt` to enlarge the completed physical buffer.

### 9.5 Cache GDI resources

Cache reusable:

- Pens.
- Brushes.
- Fonts.

Suggested cache keys:

```text
Pen:    color + physical width + style
Brush:  color
Font:   family + physical height + weight + style + quality
```

Use bounded caches and deterministic cleanup.

Do not create and delete an `HPEN`, `HBRUSH`, or `HFONT` for every primitive on every frame.

### 9.6 Improve GDI font-quality selection

Test the supported GDI quality modes against the physical-size DIB.

Prefer the sharpest stable result among the applicable options, such as:

```text
CLEARTYPE_NATURAL_QUALITY
CLEARTYPE_QUALITY
ANTIALIASED_QUALITY
```

Do not assume that one mode behaves identically on every memory surface. Add a documented fallback.

### 9.7 Preserve the last completed GDI frame

`WM_PAINT` must repaint from the last completed physical back buffer.

Avoid clearing and redrawing directly in `WM_PAINT` unless no valid completed frame exists.

### 9.8 Best-effort GDI pacing

When VSync is requested:

- Present the physical back buffer.
- Optionally use `DwmFlush` as a measured, best-effort composition synchronization technique.
- Keep message processing responsive.
- Measure frame duration with the high-resolution clock.

Document clearly that GDI cannot offer the same explicit presentation guarantees as a DXGI swap chain.

### 9.9 Do not use `HALFTONE` as the main fix

`HALFTONE` may soften a scaled bitmap, but it cannot create glyph details that were never rendered.

It may be retained only as an optional bitmap-scaling choice for future image content. It is not the approved fullscreen text solution.

### 9.10 GDI diagnostics

The diagnostics system must report at least:

```text
Backend: GDI
Logical canvas size
Physical back-buffer size
Viewport rectangle
Uniform scale
VSync request
Pacing mode: best effort
Average draw time
Average present time
Average frame time
```

### 9.11 GDI leak tests

Run a long-duration test and verify that:

- GDI object count remains stable.
- User object count remains stable.
- Fonts, pens, brushes, DIBs, and DCs are released on shutdown.
- Repeated resize and Alt+Enter cycles do not leak resources.

---

## 10. DirectX Backend

### 10.1 C++ implementation behind a C ABI

The DirectX backend may be implemented in C++.

Expose backend entry points through `extern "C"` so the rest of the native runtime and generated programs retain a C-compatible ABI.

### 10.2 System libraries

Use the Windows platform libraries for:

```text
d3d11
dxgi
d2d1
dwrite
```

Add `dwmapi` for the optional GDI composition-pacing path.

Do not add a third-party rendering dependency.

### 10.3 Direct3D 11 device

Create a Direct3D 11 hardware device with BGRA support.

Required behavior:

1. Try hardware initialization.
2. In `Auto`, fall back to GDI if required DirectX initialization fails.
3. In explicit `DirectX`, display a readable error including the failing stage and translated HRESULT.
4. Enable the Direct3D debug layer only in debug builds and only when installed.
5. Do not make the program fail merely because the debug layer is unavailable.

### 10.4 DXGI flip-model swap chain

Use a modern flip-model swap chain for the HWND.

Preferred baseline:

```text
Format:        DXGI_FORMAT_B8G8R8A8_UNORM
Buffer count:  2 or 3
Swap effect:   DXGI_SWAP_EFFECT_FLIP_DISCARD
Alpha mode:    opaque/ignore alpha
Fullscreen:    borderless window
```

Use `CreateSwapChainForHwnd` or the appropriate modern equivalent.

Disable DXGI’s default Alt+Enter handling and preserve SMILE’s own borderless Alt+Enter implementation.

### 10.5 Frame-latency waitable object

Where supported:

- Create the swap chain with `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`.
- Obtain the waitable handle.
- Set maximum frame latency to one by default.
- Wait before beginning the next rendered frame.
- Combine waiting with message processing so the window stays responsive.
- Close the handle during shutdown.

If unavailable, fall back gracefully to ordinary synchronized presentation.

### 10.6 Direct2D device and target

Create Direct2D resources from the DXGI device.

Create the Direct2D target bitmap from the current swap-chain back buffer.

Before `ResizeBuffers`:

1. Release the Direct2D target bitmap and references to the old back buffer.
2. Resize the swap-chain buffers.
3. Recreate the Direct2D target.
4. Recalculate the viewport.
5. Invalidate output-size-dependent resources.

### 10.7 DirectWrite factory

Create one DirectWrite factory for the backend lifetime.

Cache reusable text formats.

Do not recreate the factory or every text format once per frame.

### 10.8 DirectX frame sequence

The normal sequence is:

```text
Wait for frame availability when supported
Pump window messages
Acquire high-resolution timing data
BeginDraw
Clear the entire physical target to black
Render the logical viewport
Render final-resolution text
EndDraw
Present
Record timing and presentation diagnostics
```

### 10.9 Present behavior

When VSync is enabled:

```cpp
Present(1, 0)
```

When VSync is disabled:

- Detect whether tearing is supported.
- Use the required swap-chain and present flags only when legal.
- Do not enable tearing by default.
- Continue using borderless fullscreen.

### 10.10 Device removal and recreation

Handle at minimum:

- `DXGI_ERROR_DEVICE_REMOVED`
- `DXGI_ERROR_DEVICE_RESET`
- `D2DERR_RECREATE_TARGET`
- Resize to zero while minimized.
- Restore after minimization.
- Display-mode changes.

In `Auto`, unrecoverable DirectX failure may fall back to GDI after releasing DirectX resources and recording the reason.

In explicit `DirectX`, report an actionable failure.

---

## 11. Direct2D Primitive Rendering

Implement Direct2D equivalents for existing SMILE drawing commands:

- Clear.
- Filled rectangle.
- Rectangle outline.
- Filled rounded rectangle.
- Rounded rectangle outline.
- Filled circle.
- Circle outline.
- Line.

Requirements:

- Draw directly into the physical swap-chain target.
- Use the logical-to-physical viewport mapping.
- Use Direct2D antialiasing for ordinary vector graphics.
- Preserve SMILE color semantics.
- Verify color-channel conversion because GDI `COLORREF` ordering differs from common DirectX formats.
- Cache solid-color brushes or use a small bounded brush cache.
- Release device-dependent brushes during device-target recreation.
- Keep the backend interface extensible for future image and sprite support.

Do not implement a full sprite system unless required by an existing sample.

---

## 12. Final-Resolution Text Rendering

This section is mandatory and defines the approved fix for fullscreen text.

### 12.1 Prohibited pipeline

Do not use this pipeline:

```text
Draw text at 960×540
Convert it to pixels
Enlarge those pixels to fullscreen
```

### 12.2 Required pipeline

Use this pipeline:

```text
Receive logical text position and size
Determine the actual physical viewport
Calculate the effective output position and font size
Ask the active font renderer to rasterize the glyphs at that output size
Draw directly into the final-size back buffer
Present without a second enlargement step
```

### 12.3 Preserve logical SMILE semantics

The source remains:

```smile
Draw Text "Score" At 40, 25 Size 16 Color WHITE
```

The runtime interprets the values as logical units.

At a 2× viewport scale:

```text
Logical size 16 → effective output size approximately 32
```

At a 4× viewport scale:

```text
Logical size 16 → effective output size approximately 64
```

The text remains the same size relative to the game. It simply uses more physical pixels and therefore has smoother, more detailed edges.

### 12.4 DirectWrite output-space text path

For each text operation:

1. Map the logical position into output-space coordinates.
2. Calculate the effective output font size.
3. Retrieve or create a cached DirectWrite text format.
4. Measure and lay out the string at the output resolution.
5. Apply the requested left or centered alignment.
6. Draw directly onto the final Direct2D target.
7. Do not scale a completed text bitmap afterward.

Geometry may use a logical-to-output transform.

Text may use explicit output-space coordinates and scaled font sizes to maximize pixel alignment and antialiasing consistency.

### 12.5 Antialiasing policy

Use high-quality grayscale antialiasing as the safe default for game text over colored and animated backgrounds.

ClearType may be enabled only when:

- The target is fully opaque.
- Text is axis-aligned.
- No later bitmap scaling occurs.
- Testing demonstrates an improvement without color fringing.

### 12.6 Text alignment and measurement

- Use DirectWrite metrics for centering.
- Do not estimate width from character count.
- Keep baselines stable.
- Avoid clipping at larger scales.
- Map first, then apply output-pixel snapping when appropriate.
- Preserve the current meaning of `Centered`.

### 12.7 Numbers use the same renderer

`Draw Number` must use the same high-quality text path as `Draw Text`.

Do not keep a separate lower-resolution number renderer.

### 12.8 Font policy

Preserve the current default font family unless separately approved.

The initial DirectWrite backend may use:

```text
Consolas
```

with a documented fallback if unavailable.

Do not add new public font-selection syntax in this milestone.

### 12.9 Cache policy

Cache text formats by relevant properties such as:

```text
font family
effective output size
weight
style
stretch
locale
```

Avoid unbounded layout caching for rapidly changing score values.

### 12.10 Invalidation

Recalculate or invalidate output-dependent text resources when:

- The window is resized.
- Fullscreen is toggled.
- The viewport scale changes.
- DPI changes.
- The backend changes.
- The Direct2D target is recreated.

### 12.11 GDI text parity

The GDI backend follows the same logical contract but uses a physically scaled `HFONT` and draws into the physical-size GDI DIB.

DirectWrite is the visual-quality reference, but GDI fullscreen text must no longer be blocky merely because the window is fullscreen.

---

## 13. High-Resolution Timing and Frame Pacing

### 13.1 Use QueryPerformanceCounter internally

Create a frame clock based on:

```text
QueryPerformanceCounter
QueryPerformanceFrequency
```

Cache the frequency during initialization.

Use the clock to measure:

- Full frame duration.
- Update duration where observable.
- Draw duration.
- Present duration.
- Average FPS.
- Minimum and maximum frame time.
- Long-frame events.

### 13.2 Preserve `Timer()` semantics

The public `Timer()` behavior should remain compatible with the language specification.

Its implementation may use the high-resolution monotonic clock internally and return elapsed milliseconds in the existing numeric type.

Do not change its public unit silently.

### 13.3 Treat `Show Screen` as the frame boundary

`Show Screen` is the natural runtime frame boundary.

When VSync is enabled:

- DirectX presentation may block or wait through the swap-chain pacing mechanism.
- GDI uses best-effort pacing.

A SMILE game must not need `Wait 8 Milliseconds` to define its physical movement speed.

### 13.4 Avoid uncontrolled frame queues

The DirectX backend should not render several frames ahead of the display.

Use the frame-latency waitable object and a maximum latency of one where supported.

### 13.5 Focus, pause, and breakpoint handling

Clamp abnormally large elapsed intervals caused by:

- Window dragging.
- Breakpoints.
- Loss of focus.
- System sleep.
- Temporary stalls.

Recommended maximum elapsed time passed to a simple game update:

```text
50 milliseconds
```

The exact value may be adjusted after testing, but it must be documented.

---

## 14. PaddleBall Movement Correction

### 14.1 Remove loop-count-dependent speed

Do not continue relying on:

```smile
BallX = BallX + BallVX
BallY = BallY + BallVY
Wait 8 Milliseconds
```

as the game-speed model.

### 14.2 Use elapsed time

Express movement conceptually as:

```text
position += velocity-per-second × elapsed-time
```

Because the current SMILE numeric model is integer-oriented, use fixed-point subpixel state.

Suggested representation:

```text
1 logical pixel = 1,000 subpixel units
```

Example concept:

```text
BallXSubpixels += BallSpeedXPerSecond × ElapsedMilliseconds
DrawBallX = BallXSubpixels / 1000
```

Adjust units carefully so the dimensional calculation is correct and integer overflow is impossible under normal gameplay.

### 14.3 Correct paddles and AI too

Convert all movement that currently depends on loop count:

- Player paddle movement.
- Computer paddle movement.
- Ball movement.
- Any future timed animation.

### 14.4 Collision stability

Use one of these approved approaches:

1. A fixed-step accumulator with sufficiently small simulation steps.
2. Swept collision logic for the ball.

For the first implementation, a small fixed-step accumulator is preferred because it is easier to reason about and test.

Clamp the number of catch-up steps per rendered frame to prevent a spiral of death after a long pause.

### 14.5 Rendering interpolation

When practical, interpolate the rendered position between the previous and current fixed simulation states.

This is optional for the first working milestone if fixed-point elapsed-time movement is already visually smooth, but the architecture must not prevent it.

### 14.6 Preserve gameplay feel

Choose initial velocity-per-second values that approximate the current intended PaddleBall speed.

Do not silently make the game substantially faster or slower.

Document the old approximate speed and the new configured speed.

---

## 15. Diagnostics

Add a developer-only graphics diagnostics mode.

It may be enabled through an existing debug mechanism, a command-line option, or an environment variable such as:

```text
SMILE_GRAPHICS_DIAGNOSTICS=1
```

Do not add public language syntax solely for diagnostics.

The overlay or log must report:

```text
Requested backend
Selected backend
Fallback reason
Logical canvas size
Physical output size
Viewport rectangle
Uniform scale
Display refresh rate
VSync state
Pacing mode
Average FPS
Average frame time
Longest recent frame
Average draw time
Average present time
DirectX device-removal reason, when applicable
```

Example:

```text
Backend:          DirectX
Logical canvas:   960 × 540
Output size:      1920 × 1080
Viewport:         0,0 1920×1080
Scale:            2.000
Refresh:          100 Hz
VSync:            On
FPS:              99.9
Average frame:    10.01 ms
Longest frame:    10.64 ms
```

Diagnostics must be disabled by default in release games.

---

## 16. Project and Command-Line Integration

### 16.1 Project parser tests

Add tests for:

- Missing `GraphicsBackend` → `Auto`.
- `Auto`.
- `GDI`.
- `DirectX`.
- Unknown value → clear diagnostic.
- Missing `VSync` → `true`.
- `VSync=true`.
- `VSync=false`.

Follow the project parser’s existing case-sensitivity rules.

### 16.2 Optional command-line override

If the current CLI architecture supports normal option extension, add:

```text
--graphics auto
--graphics gdi
--graphics directx
```

The command-line selection overrides the project file.

Do not build a new general CLI framework solely for this option.

### 16.3 Build outputs

Ensure the required Windows system libraries are linked automatically for graphical projects.

A beginner must not manually configure DirectX linker inputs.

---

## 17. Error Handling and Fallback

### 17.1 `Auto` behavior

`Auto` must:

1. Try DirectX.
2. Record the failing initialization stage if it fails.
3. Release partial DirectX resources.
4. Initialize GDI.
5. Continue running when GDI succeeds.
6. Make the selected backend and fallback reason available to diagnostics.

### 17.2 Explicit `DirectX` behavior

When DirectX is explicitly required, do not silently fall back.

Display a message comparable to:

```text
SMILE could not start the DirectX graphics backend.
Stage: DXGI swap-chain creation
Error: 0x887A0004 — DXGI_ERROR_UNSUPPORTED
Try <GraphicsBackend>GDI</GraphicsBackend> or update the graphics driver.
```

### 17.3 Explicit `GDI` behavior

When GDI is selected, do not initialize DirectX merely to test availability.

### 17.4 Shutdown

Release all resources in reverse dependency order.

Verify cleanup of:

- DirectWrite layouts and formats.
- Direct2D brushes and target bitmaps.
- Direct2D device context and device.
- DXGI swap chain and waitable handle.
- Direct3D device and context.
- GDI fonts, pens, brushes, bitmaps, and DCs.
- Window references.

---

## 18. Testing Requirements

### 18.1 Automated tests

Add automated tests for:

- Backend value parsing.
- Backend factory selection.
- Auto-fallback decision logic.
- Viewport calculations.
- Aspect-ratio preservation.
- Coordinate mapping.
- Text-size scaling calculations.
- DPI-change calculations.
- Fixed-point timing calculations.
- Elapsed-time clamping.
- PaddleBall speed consistency under simulated frame sequences.

Test viewport calculations for at least:

```text
960 × 540
1280 × 720
1920 × 1080
1920 × 1200
2560 × 1440
3440 × 1440
3840 × 2160
```

### 18.2 Manual visual tests

Test both backends at:

```text
960 × 540 windowed
1280 × 720 windowed
1920 × 1080 fullscreen
2560 × 1440 fullscreen
3840 × 2160 fullscreen
```

Test DPI scales:

```text
100%
125%
150%
200%
```

Test refresh rates where available:

```text
60 Hz
100 Hz
120 Hz
144 Hz
```

### 18.3 Required visual text sample

Render:

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

Compare screenshots captured in windowed and fullscreen modes.

### 18.4 Fullscreen-cycle tests

Repeat at least 100 times in an automated or assisted test:

```text
Windowed → Alt+Enter → Fullscreen → Alt+Enter → Windowed
```

Verify:

- No crash.
- No resource leak.
- No incorrect viewport.
- No lost input.
- No persistent topmost state after returning to windowed mode.
- Text remains sharp after every transition.

### 18.5 Resize and monitor tests

- Resize continuously while the game is running.
- Minimize and restore.
- Move between monitors.
- Change DPI context by moving between differently scaled monitors when available.
- Disconnect or disable a secondary display during testing if practical.

### 18.6 Long-run tests

Run PaddleBall for at least 30 minutes per backend.

Verify:

- Stable memory use.
- Stable GDI object count.
- Stable COM resource use.
- No progressive frame-time degradation.
- No sound or input regression.

### 18.7 Debug-layer tests

In debug builds, use:

- Direct3D debug output when available.
- Direct2D debug output when available.
- Live-object reporting at shutdown where practical.

No meaningful graphics resource leaks may remain.

---

## 19. Acceptance Criteria

This milestone is complete only when all applicable criteria pass.

### 19.1 Backend behavior

- `Auto` selects DirectX when initialization succeeds.
- `Auto` falls back to GDI when DirectX fails.
- Explicit `GDI` works.
- Explicit `DirectX` works or reports a clear error.
- Existing SMILE source does not require backend-specific changes.

### 19.2 GDI improvements

- The GDI back buffer matches the physical client area.
- GDI primitives are drawn after logical-to-physical mapping.
- GDI text is rasterized at the final physical size.
- The final GDI frame is copied 1:1.
- Pens, brushes, and fonts are cached.
- No long-run GDI resource leak exists.
- Fullscreen GDI text is substantially sharper than the old stretched-text path.

### 19.3 DirectX rendering

- Direct3D 11, DXGI, Direct2D, and DirectWrite initialize correctly.
- The swap chain uses a flip model.
- VSync is enabled by default.
- The frame-latency waitable object is used where supported.
- Shapes render at output resolution.
- Text renders at output resolution.
- Resize and device recreation work.

### 19.4 Visual quality

- Text remains sharp in fullscreen.
- A circle remains round at all tested resolutions.
- Ordinary vector edges are antialiased in DirectX.
- No visible tearing occurs with DirectX VSync enabled.
- No normal flicker occurs.
- Centered text remains correctly centered.
- Text is not clipped after scaling.

### 19.5 PaddleBall timing

- Game speed does not materially change with monitor refresh rate.
- Ball and paddle movement use elapsed time.
- The hard-coded wait is not the physical speed controller.
- Long pauses are clamped safely.
- Fast movement does not visibly jump because subpixel state was discarded.

### 19.6 Beginner experience

A beginner can continue writing:

```smile
Game Window "My Game"

Do
    Clear BLACK
    Fill Circle X, Y, 10, WHITE
    Draw Text "Hello" At 20, 20 Size 18 Color YELLOW
    Show Screen
Loop Until Game_Closed() = True
```

without writing graphics-backend, resolution, DPI, VSync, or fullscreen-management code.

---

## 20. Required Implementation Sequence

### Phase 1 — Baseline and diagnostics

- Build the current repository.
- Run all current tests.
- Record current GDI object count and frame behavior.
- Add high-resolution timing infrastructure.
- Add backend and frame diagnostic logging without changing rendering output.

**Stop condition:** Build and tests pass; baseline report exists.

### Phase 2 — Backend abstraction

- Define the backend-neutral interface.
- Route current exports through it.
- Move the current GDI behavior behind the new interface without intentionally changing visuals.

**Stop condition:** Existing games still build and run using GDI.

### Phase 3 — GDI modernization

- Add physical-size GDI back buffer.
- Add viewport mapping.
- Render primitives at physical resolution.
- Render text at final physical resolution.
- Add GDI resource caches.
- Present 1:1.
- Add optional measured `DwmFlush` pacing.

**Stop condition:** GDI fullscreen text is sharp, resources are stable, and existing samples pass.

### Phase 4 — DirectX device and swap chain

- Add C++ backend module.
- Create Direct3D 11 device.
- Create flip-model DXGI swap chain.
- Add frame-latency waiting.
- Implement resize and shutdown.
- Present cleared frames with VSync.

**Stop condition:** A DirectX window presents stable blank frames and survives resize/fullscreen cycles.

### Phase 5 — Direct2D primitives

- Implement the current shape commands.
- Add color conversion and brush caching.
- Verify output-space rendering and aspect ratio.

**Stop condition:** PaddleBall geometry renders correctly through DirectX.

### Phase 6 — DirectWrite text

- Implement final-resolution text and numbers.
- Add measurement, centering, antialiasing, caching, and invalidation.
- Compare screenshots with GDI and the old renderer.

**Stop condition:** DirectX fullscreen text meets the acceptance criteria.

### Phase 7 — Project selection and fallback

- Add project-property parsing.
- Add backend factory logic.
- Add `Auto`, `GDI`, and `DirectX` behavior.
- Add clear fallback and failure diagnostics.

**Stop condition:** Selection and fallback tests pass.

### Phase 8 — PaddleBall timing correction

- Convert movement to elapsed-time-aware fixed-point state.
- Correct paddles, AI, ball, and collision timing.
- Remove the hard-coded wait as the speed controller.
- Compare gameplay speed across refresh rates.

**Stop condition:** Movement is stable and visually smooth on all tested refresh rates.

### Phase 9 — Hardening and documentation

- Run long-duration tests.
- Run leak tests.
- Run resize, monitor, DPI, minimize, and Alt+Enter tests.
- Update public documentation.
- Document the backend selection properties.
- Document diagnostics.
- Produce an implementation report.

**Stop condition:** All acceptance criteria pass.

---

## 21. Required Deliverables

Codex must deliver:

1. The backend-neutral graphics interface.
2. The improved GDI backend.
3. The DirectX backend.
4. Project-property parsing for backend and VSync selection.
5. Auto-fallback behavior.
6. High-resolution frame timing.
7. Graphics diagnostics.
8. Final-resolution GDI text.
9. Final-resolution DirectWrite text.
10. Updated PaddleBall timing.
11. Automated tests.
12. Manual test checklist.
13. Updated public documentation.
14. A concise implementation report containing:
    - Files changed.
    - Architectural decisions.
    - Test results.
    - Visual test results.
    - Known limitations.
    - Any approved deviations from this document.

---

## 22. Commit and Review Guidance

Prefer separate commits for:

1. Baseline diagnostics.
2. Backend abstraction.
3. GDI physical-resolution rendering.
4. GDI text and resource caching.
5. DirectX device and swap chain.
6. Direct2D primitives.
7. DirectWrite text.
8. Project selection and fallback.
9. PaddleBall timing.
10. Tests and documentation.

Each commit must build and have a focused purpose.

Do not mix unrelated compiler or language changes into this work.

---

## 23. Future Extensions Enabled by This Architecture

These are not part of this milestone, but the design should make them possible later:

- Cross-platform graphics backends.
- Sprite and image rendering.
- Rotation and scaling.
- Transparency.
- Animation helpers.
- Particle effects.
- Gamepad input.
- Audio mixing.
- A formal beginner-friendly game-loop abstraction.
- Optional pixel-art filtering.
- Additional font families and styles.

Do not implement them now unless separately approved.

---

## 24. Source References

### SMILE 2.0 repository

- Current native runtime:  
  https://github.com/Sincioco/SMILE-2.0/blob/main/src/Smile.NativeRuntime/runtime.c

- Current PaddleBall source:  
  https://github.com/Sincioco/SMILE-2.0/blob/main/games/PaddleBall/Program.smile

- Current PaddleBall project file:  
  https://github.com/Sincioco/SMILE-2.0/blob/main/games/PaddleBall/PaddleBall.smileproj

### Microsoft documentation

- Direct2D overview:  
  https://learn.microsoft.com/en-us/windows/win32/direct2d/direct2d-overview

- Direct2D and DirectWrite text rendering:  
  https://learn.microsoft.com/en-us/windows/win32/direct2d/direct2d-and-directwrite

- Rendering DirectWrite:  
  https://learn.microsoft.com/en-us/windows/win32/directwrite/rendering-directwrite

- Windows graphics architecture overview:  
  https://learn.microsoft.com/en-us/windows/win32/learnwin32/overview-of-the-windows-graphics-architecture

- DXGI flip-model guidance:  
  https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model

- `IDXGISwapChain::Present`:  
  https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present

- Reduce latency with DXGI 1.3 swap chains:  
  https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains

- `IDXGISwapChain2::SetMaximumFrameLatency`:  
  https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency

- High-resolution timestamps and `QueryPerformanceCounter`:  
  https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps

- `DwmFlush`:  
  https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmflush

---

## 25. Final Governing Statement

SMILE 2.0 must preserve a simple, educational drawing language while allowing its runtime to use increasingly capable graphics systems underneath it.

A student should be able to think in terms of:

```smile
Fill Circle X, Y, Radius, WHITE
Draw Text "Score" At 40, 25 Size 16 Color YELLOW
Show Screen
```

The runtime should handle:

- GDI or DirectX selection.
- Output resolution.
- DPI.
- Aspect ratio.
- Fullscreen scaling.
- Text rasterization quality.
- Frame pacing.
- VSync.
- Resource management.
- Device recovery.

That separation is the central architectural rule of this implementation.
