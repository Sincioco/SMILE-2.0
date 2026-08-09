# Compiler and tooling architecture

SMILE 2.0 uses one deliberately direct native pipeline:

```text
.smile source
    -> Smile.Language analysis
    -> Smile.Compiler MASM emitter
    -> ml64.exe and link.exe
    -> native Windows x64 .exe
```

`src\Smile.Language` owns tokenization, keyword and built-in facts, parsing, syntax nodes, diagnostics, symbols, types, and semantic analysis. `smilec` and the Visual Studio extension both call `SmileLanguage.Analyze`; there is no editor-only parser or duplicate semantic implementation.

Language evolution follows one permanent hierarchy: use existing syntax when it stays clear; otherwise prefer an established BASIC idea; use the smallest beginner-friendly C#-inspired concept only when BASIC has no suitable precedent. Additions remain general-purpose, avoid aliases and clever punctuation, and receive only proportional diagnostics, tests, examples, and documentation through the shared language authority.

The native backend emits MASM x64 and links `Smile.NativeRuntime.lib`. Console programs use the console subsystem. Programs containing `GAME WINDOW` use the Windows GUI subsystem and the generic Win32 runtime for:

- a backend-neutral graphics interface that preserves the compiler-facing C ABI;
- `Auto`, `DirectX`, and `GDI` selection, with DirectX-first fallback in `Auto`;
- Direct3D 11 and a two-buffer flip-model DXGI swap chain for DirectX presentation;
- Direct2D final-resolution shapes and DirectWrite final-resolution text;
- a physical-output-size GDI DIB, bounded GDI resource caches, and one-to-one presentation;
- a 960-by-540 default logical canvas with uniform aspect-preserving viewport mapping;
- QPC frame measurements, VSync-default frame pacing, and opt-in diagnostic logs;
- per-monitor DPI handling and Alt+Enter full-screen transitions;
- queued pressed keys, simultaneous held-key state, and focus-loss clearing;
- asynchronous WAV effects and C++/WinRT `Windows.Media.Playback.MediaPlayer` MP3 music relative to the executable;
- application, window-activation, and minimization tracking that silences both audio channels while the game is inactive without changing system volume;
- bounded executable-relative file-byte loading with zero-fill and safe missing-file behavior;
- per-executable integer persistence under local application data.

The compiler emits one stable graphics configuration call before game startup and routes every drawing export—including filled and outlined quadrilaterals—through the active `SmileGraphicsBackend` vtable. DirectX builds quadrilaterals with short-lived Direct2D path geometry; GDI maps the same four logical points into its physical back buffer and uses `Polygon`. Backend implementations own their render targets and caches; windowing, input, audio, persistence, and language-level game logic remain outside the graphics modules.

`DRAW ARC CenterX, CenterY, Radius, StartAngle, SweepAngle, Color` follows the same compiler-facing C ABI and backend vtable routing as the other drawing primitives. DirectX renders partial arcs with short-lived Direct2D path geometry and reuses circle rendering for complete arcs. GDI maps the same logical geometry into its physical back buffer, uses the cached outline pen and `Arc`, and restores the prior GDI arc direction after every call. Both backends use integer screen-coordinate degrees (`0` right, `90` down, `180` left, `270` up), with positive clockwise and negative counterclockwise sweeps clamped to one revolution. The primitive adds no fill, chord, radial lines, thickness option, or game-specific wall helper.

Music-bearing generated programs reference a dedicated C-compatible MediaPlayer object and link `WindowsApp.lib` plus the static C/C++ support libraries required by the custom `/entry:main` pipeline. Games without music do not pull that object from `Smile.NativeRuntime.lib`. The MediaPlayer state is allocated lazily, owns no nontrivial global constructor, catches every C++ exception at the C ABI, and is shut down explicitly before each generated process exit.

## Shared audio-focus contract

The Win32 window procedure owns one focus state above both graphics backends. Audio is active only while the application is active, its top-level game window is active, and the window is not minimized. An inactive transition stops the current `PlaySoundW` effect and suppresses later WAV requests. The MediaPlayer remains at its current playback position with effective volume zero while its requested volume is retained. Reactivation reapplies that volume only; it does not restart playback or resume a track paused or stopped by SMILE source. Suppressed WAV effects are not queued for replay.

This process-local policy is inherited by every `GAME WINDOW` program and requires no game-specific activation code. It never changes Windows master volume or another process, and DirectX and GDI behave identically because focus and audio remain outside their backend implementations.

The runtime does not contain Snake, falling-block, paddle, brick, dungeon, score, level, projection, generation, map-format, pathfinding, or win/loss rules. Those remain in the corresponding files under `games`. Dungeon Star I parses its external map bytes, validates topology, generates pipe graphs, plans its demo, and composes its pseudo-3D projection entirely in `Program.smile`, using only the same generic file, quadrilateral, line, rectangle, text, input, timing, and audio services available to every SMILE program.

The Visual Studio extension embeds the same compiler/runtime payload and registers a minimal `.smileproj` project factory. Its console and game templates build to `bin\Debug` or `bin\Release`, copy declared assets, populate the SMILE Output pane and Error List, and launch the resulting executable for F5 or Ctrl+F5.
