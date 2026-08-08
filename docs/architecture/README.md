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
- per-executable integer persistence under local application data.

The compiler emits one stable graphics configuration call before game startup and routes every drawing export—including filled and outlined quadrilaterals—through the active `SmileGraphicsBackend` vtable. DirectX builds quadrilaterals with short-lived Direct2D path geometry; GDI maps the same four logical points into its physical back buffer and uses `Polygon`. Backend implementations own their render targets and caches; windowing, input, audio, persistence, and language-level game logic remain outside the graphics modules.

Music-bearing generated programs reference a dedicated C-compatible MediaPlayer object and link `WindowsApp.lib` plus the static C/C++ support libraries required by the custom `/entry:main` pipeline. Games without music do not pull that object from `Smile.NativeRuntime.lib`. The MediaPlayer state is allocated lazily, owns no nontrivial global constructor, catches every C++ exception at the C ABI, and is shut down explicitly before each generated process exit.

The runtime does not contain Snake, falling-block, paddle, brick, score, level, or win/loss rules. Those remain in the corresponding files under `games`.

The Visual Studio extension embeds the same compiler/runtime payload and registers a minimal `.smileproj` project factory. Its console and game templates build to `bin\Debug` or `bin\Release`, copy declared assets, populate the SMILE Output pane and Error List, and launch the resulting executable for F5 or Ctrl+F5.
