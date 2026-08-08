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

- a 960-by-540 default logical back buffer;
- double-buffered GDI drawing and aspect-preserving presentation;
- per-monitor DPI handling and Alt+Enter full-screen transitions;
- queued pressed keys, simultaneous held-key state, and focus-loss clearing;
- asynchronous WAV playback relative to the executable;
- per-executable integer persistence under local application data.

The runtime does not contain Snake, falling-block, paddle, brick, score, level, or win/loss rules. Those remain in the corresponding files under `games`.

The Visual Studio extension embeds the same compiler/runtime payload and registers a minimal `.smileproj` project factory. Its console and game templates build to `bin\Debug` or `bin\Release`, copy declared assets, populate the SMILE Output pane and Error List, and launch the resulting executable for F5 or Ctrl+F5.
