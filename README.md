# SMILE 2.0

SMILE 2.0 is a small, structured BASIC-style language that compiles directly to native Windows x64 executables. It includes a MASM-based native compiler, a Win32 game runtime, Visual Studio 2026 language and project support, console examples, and four complete games written in SMILE.

The repository has one language authority: `src\Smile.Language`. The command-line compiler and Visual Studio extension use the same lexer, parser, syntax model, diagnostics, symbols, types, and semantic model. Game rules remain in `.smile` source; the C runtime provides only generic Windows graphics, input, sound, timing, and storage services.

## Included games

- `games\Snake` — graphical Snake with score, progressive speed, and a persistent high score.
- `games\FallingBlocks` — a seven-piece falling-block puzzle with rotation, row clearing, levels, and a persistent high score.
- `games\PaddleBall` — one-player AI and local two-player paddle modes with a persistent best rally.
- `games\BrickBreaker` — a 7-by-12 colored brick field, three lives, three levels, row scoring, and a persistent high score.

All games use a logical 960-by-540 canvas. Window resizing preserves the 16:9 aspect ratio with letterboxing, and Alt+Enter toggles borderless full screen.

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

The smoke suite builds the solution, runs console examples, checks invalid-program diagnostics, exercises save/reload and corrupt-value fallback, compiles all four games, copies their assets, verifies the VSIX contents, and confirms every graphical executable is a native x64 Windows GUI with no CLR header. Graphical gameplay remains a hands-on acceptance step.

## Compile loose files

The compiler accepts one `.smile` source file and an optional output path:

```text
artifacts\compiler\smilec.exe examples\Hello.smile
artifacts\compiler\smilec.exe examples\GraphicsBasics.smile -o artifacts\games\GraphicsBasics.exe
artifacts\compiler\smilec.exe games\Snake\Program.smile -o artifacts\games\Snake\Snake.exe
```

Use `--keep-temp` to retain generated MASM assembly and object files under `artifacts\temp`. Copy any `Assets` directory beside the resulting executable before running a program that uses sounds.

## Build and run the games

`scripts\smoke-test.cmd` produces these runnable artifacts with their sound assets:

```text
artifacts\games\Snake\Snake.exe
artifacts\games\FallingBlocks\FallingBlocks.exe
artifacts\games\PaddleBall\PaddleBall.exe
artifacts\games\BrickBreaker\BrickBreaker.exe
```

The executables are self-contained native game programs with respect to SMILE: neither `smilec.exe` nor Visual Studio is needed to run them. They still use normal Windows system libraries.

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

Loose `.smile` files remain supported: open one and use **Tools > Build SMILE File**.

## Language overview

Implemented syntax includes:

- signed 64-bit numeric values, booleans, text literals, variables, constants, and one- or two-dimensional fixed arrays;
- arithmetic and comparison operators, integer `/`, `MOD`, `AND`, `OR`, and `NOT`;
- multiline `IF`/`ELSE IF`/`ELSE`, ascending and descending `FOR`, `DO`/`LOOP UNTIL`, `EXIT FOR`, `EXIT DO`, and `SELECT CASE`;
- `SUB`, `FUNCTION`, `CALL`, `RETURN`, and up to four scalar parameters;
- `PRINT`, `GET KEY`, `KEY_HELD`, `WAIT`, `RANDOM`, `TIMER`, `ABS`, `MIN`, `MAX`, and `RGB`;
- `GAME WINDOW`, double-buffered drawing primitives, `SHOW SCREEN`, asynchronous WAV playback, and integer persistence through `LOAD` and `SAVE`;
- named keyboard and color constants used by the examples and games.

See `docs\language\README.md` and `examples\StructuredLanguageBasics.smile` for concrete syntax.

## Current limitations

- The target is Windows x64 only and the native backend requires the Visual Studio MASM/link toolchain when compiling.
- Numeric storage is signed 64-bit integer only; there is no floating-point type, user-defined type, dynamic collection, module system, or package manager.
- Arrays are fixed at compile time and support at most two dimensions.
- Routines accept at most four scalar parameters. Text is currently used as a literal-oriented console/graphics/audio surface rather than a general mutable string type.
- The Visual Studio project system intentionally covers the core single-startup-file console/game workflow; it is not an MSBuild SDK or a general multi-target project system.
- Audio playback supports WAV files through the Windows multimedia runtime, one asynchronous sound at a time.

## Asset provenance

All committed WAV files are original deterministic tones generated by `scripts\generate-sounds.ps1`. The games use runtime-drawn geometric art and system fonts. No external reference image, commercial logo, third-party sprite, font, sound, or source code is included.
