# SMILE 2.0

SMILE 2.0 is a small, structured BASIC-style language that compiles directly to native Windows x64 executables. It includes a MASM-based native compiler, a Win32 game runtime, Visual Studio 2026 language and project support, console examples, and five complete games written in SMILE.

The repository has one language authority: `src\Smile.Language`. The command-line compiler and Visual Studio extension use the same lexer, parser, syntax model, diagnostics, symbols, types, and semantic model. Game rules remain in `.smile` source; the C runtime provides only generic Windows graphics, input, sound, timing, and storage services.

## Development governance

Language evolution starts by asking whether current SMILE syntax can express the requirement clearly. When a new general-purpose feature is justified, SMILE uses a recognizable BASIC precedent first and the smallest beginner-friendly C#-inspired concept only when BASIC has no suitable precedent. New rules are implemented once in `src\Smile.Language`; game-specific statements and native helpers are not added.

Validation assumes the happy path and uses the lightest focused evidence that reasonably proves a milestone: proportional targeted checks, the normal smoke suite, and brief manual interaction where needed. Long soaks, large seed sweeps, exhaustive playthroughs, stress campaigns, or benchmarks are reserved for a stated known bug, crash, hang, leak, timing defect, performance problem, intermittent failure, or formal benchmark, with a defined stop condition.

When project work produces two or more Markdown requirement, specification, handoff, or instruction files, each remains individually usable and the complete set is also delivered in one ZIP with a `START HERE` file and any companion samples, configuration, or maps under useful repository-relative paths.

## Included games

- `games\Snake` — graphical Snake with score, progressive speed, and a persistent high score.
- `games\FallingBlocks` — a seven-piece falling-block puzzle with rotation, row clearing, levels, and a persistent high score.
- `games\PaddleBall` — one-player AI and local two-player paddle modes with a persistent best rally.
- `games\BrickBreaker` — a 7-by-12 colored brick field, three lives, three levels, row scoring, and a persistent high score.
- `games\DungeonStarI` — an original three-floor pseudo-3D dungeon exploration sample with random rooms, doors, stairs, attract mode, and green, blue, and red floor palettes.

All games use a logical 960-by-540 canvas. Window resizing preserves the 16:9 aspect ratio with letterboxing, and Alt+Enter toggles borderless full screen.

## Automatic game-audio focus

Every program containing `GAME WINDOW` automatically inherits one shared audio-focus policy; games do not need activation handlers. Losing application or top-level window activation, or minimizing the window, immediately stops the current asynchronous WAV effect, suppresses new WAV requests, and changes MP3 music to effective volume zero. The requested `MUSIC VOLUME` and playback position remain intact. Returning to an active, non-minimized window reapplies the exact requested volume without restarting the track or resuming music that the program paused or stopped. Suppressed effects are not replayed.

This policy is identical for DirectX and GDI and affects only the SMILE process. It never changes Windows master volume or another application's audio.

## Prerequisites

- Windows x64.
- .NET SDK 10.
- Visual Studio 2026 with **Desktop development with C++** and **Visual Studio extension development** installed.

The build scripts find the current Visual Studio installation through `vswhere.exe` and initialize its x64 C++ toolchain.

## Build from a fresh clone

From a command prompt in the repository root:

```text
scripts\build.cmd
```

The script builds `SMILE 2.0.sln` and creates:

```text
artifacts\compiler\smilec.exe
artifacts\vsix\Smile.VisualStudio.vsix
```

Run the complete noninteractive regression and artifact verification suite with:

```text
scripts\smoke-test.cmd
```

The smoke suite builds the solution, runs console examples, checks invalid-program diagnostics, exercises save/reload and corrupt-value fallback, compiles the required graphics text sample and all five games, copies and hashes their assets, verifies the VSIX contents, and confirms every graphical executable is a native x64 Windows GUI with no CLR header. Graphical gameplay and audible playback remain hands-on acceptance steps.

## Compile loose files

The compiler accepts one `.smile` source file and an optional output path:

```text
artifacts\compiler\smilec.exe examples\Hello.smile
artifacts\compiler\smilec.exe examples\GraphicsBasics.smile -o artifacts\games\GraphicsBasics.exe
artifacts\compiler\smilec.exe games\Snake\Program.smile -o artifacts\games\Snake\Snake.exe
```

Use `--keep-temp` to retain generated MASM assembly and object files under `artifacts\temp`. Copy any `Assets` directory beside the resulting executable before running a program that uses sound effects or music.

## Graphics backends and frame pacing

Game projects use a backend-neutral drawing API. `Auto` is the default: it tries the DirectX 11, Direct2D, and DirectWrite backend first and falls back to the physical-resolution GDI backend when DirectX initialization is unavailable. Both backends preserve the program's logical canvas, render shapes and text at the current output resolution, keep one uniform scale, and handle resizing, per-monitor DPI changes, and Alt+Enter automatically.

The game project template includes these optional `.smileproj` settings:

```xml
<GraphicsBackend>Auto</GraphicsBackend>
<VSync>true</VSync>
```

`GraphicsBackend` accepts `Auto`, `DirectX`, or `GDI`; `VSync` accepts `true` or `false`. Missing values default to `Auto` and `true`. The command-line compiler can override a project or loose-file build with `--graphics auto|directx|gdi` and `--vsync true|false`:

```text
artifacts\compiler\smilec.exe examples\GraphicsTextSample.smile -o artifacts\games\Text-GDI.exe --graphics gdi
artifacts\compiler\smilec.exe examples\GraphicsTextSample.smile -o artifacts\games\Text-DirectX.exe --graphics directx --vsync true
```

VSync remains the normal setting. With DirectX and VSync off, SMILE uses tearing presentation only when Windows and the display path report support. GDI VSync is best-effort composition pacing through `DwmFlush`.

Graphics diagnostics are disabled by default. For a temporary diagnostic run in PowerShell:

```powershell
$env:SMILE_GRAPHICS_DIAGNOSTICS = "1"
.\artifacts\games\PaddleBall\PaddleBall.exe
```

The runtime writes `%TEMP%\SMILE-graphics-diagnostics-<process-id>.log` with the requested and selected backend, fallback reason, output and viewport sizes, refresh rate, pacing mode, FPS/frame/present measurements, and any DirectX device-removal reason. Set `SMILE_GDI_DWM_FLUSH=0` only when comparing GDI pacing behavior.

## Build and run the games

`scripts\smoke-test.cmd` produces these runnable artifacts with their sound assets:

```text
artifacts\games\Snake\Snake.exe
artifacts\games\FallingBlocks\FallingBlocks.exe
artifacts\games\PaddleBall\PaddleBall.exe
artifacts\games\BrickBreaker\BrickBreaker.exe
artifacts\games\DungeonStarI\DungeonStarI.exe
```

The executables are self-contained native game programs with respect to SMILE: neither `smilec.exe` nor Visual Studio is needed to run them. They use normal Windows system libraries; a music-bearing executable also uses the Microsoft Visual C++ runtime installed by Visual Studio or the supported Visual C++ Redistributable.

## Visual Studio 2026

Build and automatically replace the installed extension with:

```text
scripts\install-vsix.cmd
```

The script targets Visual Studio Enterprise, rebuilds the VSIX, uninstalls the existing `Smile.VisualStudio.2.0` extension when present, and force-installs the newly built package. Visual Studio may close during the refresh, so save open work before running it. Detailed installer logs are written under `artifacts\temp`.

After Visual Studio restarts, use **File > New > Project** and search for `SMILE`. Two templates are installed:

- **SMILE 2.0 Console Application**
- **SMILE 2.0 Game Application**

Both create a `.smileproj` with `Debug` and `Release` configurations. A game project also owns an `Assets` folder. **Build > Build Solution** (`Ctrl+Shift+B`) compiles the startup file into `bin\Debug` or `bin\Release` and copies declared assets. `F5` and `Ctrl+F5` build and run the native executable. Compiler failures appear in both the SMILE Output pane and Error List with the same diagnostic code, message, line, and column used by the editor and command-line compiler.

The game template documents that automatic focus muting is inherited from the shared runtime; new games should use normal `PLAY SOUND` and `PLAY MUSIC` statements without per-game focus code.

Loose `.smile` files remain supported: open one and use **Tools > Build SMILE File**.

## Language overview

Implemented syntax includes:

- signed 64-bit numeric values, booleans, text literals, variables, constants, and one- or two-dimensional fixed arrays;
- arithmetic and comparison operators, integer `/`, `MOD`, `AND`, `OR`, and `NOT`;
- multiline `IF`/`ELSE IF`/`ELSE`, ascending and descending `FOR`, `DO`/`LOOP UNTIL`, `EXIT FOR`, `EXIT DO`, and `SELECT CASE`;
- `SUB`, `FUNCTION`, `CALL`, `RETURN`, and up to four scalar parameters;
- `PRINT`, `GET KEY`, `KEY_HELD`, `WAIT`, `RANDOM`, `TIMER`, `ABS`, `MIN`, `MAX`, and `RGB`;
- `GAME WINDOW`, double-buffered rectangles, rounded rectangles, circles, lines, quadrilaterals, text, and numbers, `SHOW SCREEN`, asynchronous WAV effects, MP3 background music, and integer persistence through `LOAD` and `SAVE`;
- bounded executable-relative UTF-8 input through `LOAD TEXT FILE "path" INTO Array COUNT Variable`, including BOM skipping, zero-fill, and safe missing-file behavior;
- named keyboard constants, including `KEY_OTHER` for unnamed ordinary key events, and named color constants used by the examples and games.

See `docs\language\README.md` and `examples\StructuredLanguageBasics.smile` for concrete syntax.

## Current limitations

- The target is Windows x64 only and the native backend requires the Visual Studio MASM/link toolchain when compiling.
- Numeric storage is signed 64-bit integer only; there is no floating-point type, user-defined type, dynamic collection, module system, or package manager.
- Arrays are fixed at compile time and support at most two dimensions.
- Routines accept at most four scalar parameters. Text is currently used as a literal-oriented console/graphics/audio surface rather than a general mutable string type.
- The Visual Studio project system intentionally covers the core single-startup-file console/game workflow; it is not an MSBuild SDK or a general multi-target project system.
- `PLAY SOUND` supports one asynchronous WAV effect at a time. `PLAY MUSIC` supports one MP3 background track through `Windows.Media.Playback.MediaPlayer`; there are no playlists, seeking, or multiple music channels.
- Windows editions without the required optional media components may decline MP3 playback, but the game continues without crashing.
- Music-bearing native executables require the current Microsoft Visual C++ Redistributable; non-music output does not acquire that additional dependency.

## Asset provenance

All committed WAV files are original deterministic tones generated by `scripts\generate-sounds.ps1`. `games\FallingBlocks\Assets\Background.mp3` and `games\DungeonStarI\Assets\Background.mp3` are the exact music files supplied by the repository owner for their milestones. The games use runtime-drawn geometric art and system fonts. No third-party audio library or MP3 decoder is bundled.
