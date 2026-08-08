# Native Game Runtime: Graphics, Input, Audio, and Storage

## Implementation direction

Extend `Smile.NativeRuntime` using:

```text
Win32 window
GDI DIB/memory back buffer
GDI drawing and text
StretchBlt or StretchDIBits
Per-monitor DPI awareness v2
Win32 key messages and held-state tracking
PlaySoundW for WAV effects
LocalAppData integer storage
```

No third-party engine.

## Logical surface

Default back buffer:

```text
960 × 540, 32-bit
```

All SMILE drawing targets this logical buffer. `SHOW SCREEN` presents it.

Exact scale relationships:

```text
960×540 -> 1920×1080 = 2×
960×540 -> 3840×2160 = 4×
```

## Resize and full-screen

- Preserve the program's aspect ratio.
- Avoid stretching and cropping.
- Use centered bars only on non-16:9 displays.
- Handle resize, paint, DPI change, and monitor changes.
- Alt+Enter toggles borderless full-screen.
- Save and restore prior window style, position, and size.
- Do not change the monitor's hardware display mode.
- Alt+F4 and Close work normally.

## Responsiveness

- `SHOW SCREEN` pumps window messages.
- Graphical `WAIT` remains message-aware.
- `GAME_CLOSED()` becomes true after close.
- Focus loss clears held keys.

## Graphics API

Provide generic native functions for:

- open window;
- clear;
- fill/draw rectangle;
- fill/draw rounded rectangle;
- fill/draw circle;
- line;
- text;
- number;
- present;
- closed-state query.

No GDI leaks. Reuse or dispose fonts and objects correctly.

## Input

Support key events and held states for:

```text
W A S D
arrows
Enter Escape Space
1 2
```

`GET KEY` returns a new press or `KEY_NONE`. `KEY_HELD` supports simultaneous keys.

## Timer

Use a monotonic Windows clock and return integer milliseconds.

## WAV audio

- Resolve paths relative to the executable.
- Use asynchronous WAV playback.
- One active sound at a time is acceptable.
- Use `SND_NODEFAULT`.
- Missing files never crash.

Create original small WAV assets for every game; do not copy commercial sounds.

## Persistence

Recommended location:

```text
%LOCALAPPDATA%\SMILE 2.0\Games\<exe name>\<key>.txt
```

- sanitize names;
- plain signed integer text;
- safe default on missing/corrupt data;
- create directories;
- isolate games;
- failure does not crash.

## Compiler integration

A program containing `GAME WINDOW` compiles as a native x64 Windows GUI executable. Console programs remain console-subsystem programs.

Do not transpile to another high-level language.

## Runtime smoke example

Create `examples\GraphicsBasics.smile` that demonstrates every primitive, custom colors, text, number, key event, held keys, sound, persistence, resize, Alt+Enter, and clean close.
