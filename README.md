# SMILE 2.0

SMILE 2.0 is a small, structured BASIC-style language with a complete native Windows x64 target and a browser target. It includes explicit built-in types, first-class UTF-8 text, typed `ByVal`/`ByRef` routines, modules, reusable target-neutral libraries, true multi-file compilation, optional stable application identity, multiline parenthesized expressions and call arguments, a MASM-based native compiler, a Win32 game runtime, Canvas 2D Web publication, Visual Studio 2026 language and project support, console examples, and seven complete games written in SMILE. Windows x64 remains the default target, and every included application project exposes both native and Web publication.

The repository has one language authority: `src\Smile.Language`. The command-line compiler and Visual Studio extension use the same lexer, parser, syntax model, diagnostics, symbols, types, and semantic model. Game rules remain in `.smile` source; the C runtime provides only generic Windows graphics, input, sound, timing, and storage services.

## Development governance

Language evolution starts by asking whether current SMILE syntax can express the requirement clearly. When a new general-purpose feature is justified, SMILE uses a recognizable BASIC precedent first and the smallest beginner-friendly C#-inspired concept only when BASIC has no suitable precedent. New rules are implemented once in `src\Smile.Language`; game-specific statements and native helpers are not added.

Validation assumes the happy path and uses the lightest focused evidence that reasonably proves a milestone: proportional targeted checks, the normal smoke suite, and brief manual interaction where needed. Long soaks, large seed sweeps, exhaustive playthroughs, stress campaigns, or benchmarks are reserved for a stated known bug, crash, hang, leak, timing defect, performance problem, intermittent failure, or formal benchmark, with a defined stop condition.

When project work produces two or more Markdown requirement, specification, handoff, or instruction files, each remains individually usable and the complete set is also delivered in one ZIP with a `Start Here` file and any companion samples, configuration, or maps under useful repository-relative paths.

Every game with an attract/demo mode also ships a complete `Program-NoDemo.smile` teaching version. The no-demo source preserves normal gameplay while removing demo AI, lifecycle, timers, safety rules, UI, and cancellation code instead of hiding those systems behind a flag.

To build a teaching edition in Visual Studio, right-click `Program-NoDemo.smile` in Solution Explorer and choose **Set as Startup**. Both complete programs remain declared with `StartupOnly="true"`, and the selected file gains a `(Startup)` suffix while only that complete program enters the compilation. The XML remains available for automation, but normal switching no longer requires hand-editing it.

Attract demos always return directly to the title screen when their run ends or expires. Demo-only game-over, victory, retry, and rematch screens are not shown; normal player terminal screens remain part of the game.

## Included games

- `games\Snake` — graphical Snake with score, progressive speed, and a persistent high score.
- `games\FallingBlocks` — a seven-piece falling-block puzzle with rotation, row clearing, levels, and a persistent high score.
- `games\PaddleBall` — one-player AI and local two-player paddle modes with a persistent best rally.
- `games\BrickBreaker` — a 7-by-12 colored brick field, three lives, three levels, row scoring, and a persistent high score.
- `games\DungeonStarI` — an original three-floor pseudo-3D dungeon with student-editable external maps, validated pipe-style random generation, a blue map-selection title, doors, stairs, attract mode, and green, blue, and red floor palettes.
- `games\DungeonStarII` — an original continuous fixed-point raycasting walkaround with editable room-and-corridor maps, DDA projection, colorful stable wall materials, rising doors, collision and wall sliding, random generation, and demo/no-demo teaching sources.
- `games\MazeMuncher` — an original neon maze chase with pellets, power mode, four geometric enemies, wrap tunnels, levels, a persistent high score, demo and no-demo teaching sources, and an attract demo.

All games use a logical 960-by-540 canvas. A 16:9 output such as 1920-by-1080 fills the complete screen without letterboxing; other aspect ratios use centered letterboxing to preserve geometry. Alt+Enter toggles borderless full screen.

## Automatic game-audio focus

When a game defines background music, that track loops throughout active player gameplay and attract/demo gameplay. It stops on title and terminal game-over, victory, or match-result screens. Games without a defined background track remain silent.

Every program containing `Game Window` automatically inherits one shared audio-focus policy; games do not need activation handlers. Losing application or top-level window activation, or minimizing the window, immediately stops all active asynchronous WAV effects, suppresses new WAV requests, and changes MP3 music to effective volume zero. The requested `Music Volume` and playback position remain intact. Returning to an active, non-minimized window reapplies the exact requested volume without restarting the track or resuming music that the program paused or stopped. Suppressed effects are not replayed.

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

The smoke suite builds the solution, runs console examples, checks invalid-program diagnostics, exercises native Text lifetime and reentrancy, executes generated Web programs in a dependency-free Node host, compares native/Web UTF-8 output exactly, tests reusable Smile.UI and Smile.RPG state boundaries, validates ApplicationId isolation, asserts dynamic Canvas text, exercises save/reload and corrupt-value fallback, validates the supplied Dungeon Star I and Dungeon Star II maps, compiles the multi-file sample and both teaching variants of all seven games for Windows and Web, publishes and hashes declared project assets, verifies the VSIX contents, and confirms every graphical executable is a native x64 Windows GUI with no CLR header. Graphical gameplay and audible playback remain hands-on acceptance steps.

## Compile projects and publish assets

An application `.smileproj` is the publication contract for native and Web assets. A successful project build automatically copies the exact resolved `<Asset Include="..." />` set beside the executable or Web files; no second copy step is required:

```text
artifacts\compiler\smilec.exe --project examples\Phase4AssetPublication\Phase4AssetPublication.smileproj --target windows-x64 -o artifacts\games\Phase4AssetPublication\Phase4AssetPublication.exe
artifacts\compiler\smilec.exe --project examples\Phase4AssetPublication\Phase4AssetPublication.smileproj --target web --output-dir artifacts\web\Phase4AssetPublication
```

Asset paths are project-relative and match with ordinal case-sensitive semantics. `*` matches zero or more characters in one segment, `?` matches exactly one character in one segment, and a complete `**` segment matches zero or more directories. Overlaps deduplicate, wildcard matches may be empty, and results are sorted. Rooted/UNC/URI or escaping paths, unsupported glob forms, wrong explicit-path case, missing explicit files, and portable destination collisions fail with `SML36xx` project diagnostics. Library-owned assets are intentionally rejected until packaged library resources have their own target-neutral design.

Successful publication writes `<executable-base>.smile-assets.json` for native projects without an explicit `ApplicationId`, `<SafeApplicationId>.smile-assets.json` for native projects with an explicit identity, or `smile-assets.json` for Web output. A later build uses only validated matching manifests to remove stale managed assets; generated compiler output and unrelated files remain untouched. When an explicit identity first uses the stable name, a matching safe OutputName-based 2.0.39 manifest is migrated and removed only after successful publication. Mismatched, malformed, or unsafe manifests are never trusted or destructively migrated. Publishing multiple applications into one identical output directory is not recommended because application-level destination conflicts remain outside this ownership model.

## Compile loose files

The compiler accepts one startup `.smile` source file and an optional output path:

```text
artifacts\compiler\smilec.exe examples\Hello.smile
artifacts\compiler\smilec.exe examples\GraphicsBasics.smile -o artifacts\games\GraphicsBasics.exe
artifacts\compiler\smilec.exe games\Snake\Program.smile -o artifacts\games\Snake\Snake.exe
```

Build and consume a reusable library project:

```bat
artifacts\compiler\smilec.exe --project libraries\Smile.Math.Extras\Smile.Math.Extras.smilelibproj --target library --configuration Release
artifacts\compiler\smilec.exe --project examples\LibraryConsumer\LibraryConsumer.smileproj --target windows-x64 -o artifacts\games\LibraryConsumer.exe
artifacts\compiler\smilec.exe --project examples\LibraryConsumer\LibraryConsumer.smileproj --target web --output-dir artifacts\web\LibraryConsumer
```

Build the Phase 5 reusable UI package and gallery:

```bat
artifacts\compiler\smilec.exe --project libraries\Smile.UI\Smile.UI.smilelibproj --target library -o artifacts\libraries\Smile.UI.smilelib
artifacts\compiler\smilec.exe --project examples\MenuGallery\MenuGallery.smileproj --target windows-x64 --graphics DirectX -o artifacts\games\MenuGallery-DirectX\MenuGallery.exe
artifacts\compiler\smilec.exe --project examples\MenuGallery\MenuGallery.smileproj --target web --output-dir artifacts\web\MenuGallery
```

Build the Phase 6 RPG data package, state proof, and management gallery:

```bat
artifacts\compiler\smilec.exe --project libraries\Smile.RPG\Smile.RPG.smilelibproj --target library -o artifacts\libraries\Smile.RPG.smilelib
artifacts\compiler\smilec.exe --project examples\Phase6RpgStateTests\Phase6RpgStateTests.smileproj --target windows-x64 -o artifacts\tests\Phase6RpgStateTests.exe
artifacts\compiler\smilec.exe --project examples\RpgManagementGallery\RpgManagementGallery.smileproj --target windows-x64 --graphics DirectX -o artifacts\games\RpgManagementGallery-DirectX\RpgManagementGallery.exe
artifacts\compiler\smilec.exe --project examples\RpgManagementGallery\RpgManagementGallery.smileproj --target web --output-dir artifacts\web\RpgManagementGallery
```

The Phase 3A typed-text proof uses `libraries\Smile.Text.Extras`, `examples\Phase3ABasics`, `examples\Phase3ATextStress.smile`, and `examples\Phase3ATextGame`. The Phase 3B record proof uses `libraries\Smile.Data.Models`, `examples\Phase3BRecords`, `examples\Phase3BLocalRecords`, and `examples\Phase3BRecordMatrix.smile`; `examples\Phase3B1Hardening` adds reserved-name Web field parity. Phase 4 adds the shared `Image` type, high-resolution PNG drawing, clipping, text measurement, binary persistence, and 16 WAV SFX channels; `examples\Phase4VisualSlice` is its cross-backend proof. Phase 4.1 hardens high-DPI Web output, clips across presentation and resize, owned Image expressions, exact persistent identity/integrity, canonical media paths, audio completion, and concurrent native caching; `examples\Phase4Hardening` contains its focused fixtures. Phase 4.2 unifies exact asset resolution, publication, stale cleanup, compiler manifests, and Visual Studio hierarchy/watch behavior; `examples\Phase4AssetPublication` is its focused fixture. Phase 5 adds Unicode-scalar text inspection, routine capability metadata, and the SMILE-authored `Smile.UI` Window, BitmapFont, Text, Menu, and Dialogue modules; `examples\MenuGallery` is the DirectX/GDI/Web proof. Phase 5.1 hardens deep style bounds, multiline text, live dialogue theme reflow, menu row reflow, ownership, and bounded dialogue preparation. Phase 5.2 adds reusable bounded `MenuNavigator`; Phase 5.2.1 advances `Smile.UI` to 1.1.1 with active-edge pruning, cursors on every visible menu, proportional scrollbars, and hidden/after-text/right-aligned indicators. Phase 5.2.2 advances `Smile.UI` to 1.1.2 with current-navigator bound-item acceptance guards and one prepared, vertically centered fixed-row layout for labels, cursors, and markers. Smile.UI 1.1.3 standardizes the public `Insets.Left` and `Insets.Right` presentation casing without changing case-insensitive language behavior. Phase 6 adds stable optional ApplicationId identity and the source-authored `Smile.RPG` package for Characters, Party, Inventory, Equipment, Abilities, Shops, and transactional saves; Phase 6.1 advances Smile.RPG to 1.0.1 with save-boundary, rollback, native-manifest identity, formatter-context, and Shop-result hardening. `examples\RpgManagementGallery` composes Smile.RPG with Smile.UI without battle gameplay. Multiline parenthesized expressions add explicit continuation without changing normal line boundaries; `examples\MultilineExpressionParity` proves exact Windows/Web precedence and runtime parity. `.smilelib` packages remain deterministic formatVersion 5 packages with typed public record layouts, routine `requiresGameWindow` flags, and logical `LibraryName@Version` provider identities. See `docs\language\README.md`, `docs\language\phase5-ui.md`, and `docs\language\phase6-rpg.md`.

Phase 6.2 advances Smile.RPG to 1.0.2 and makes `SaveGames.Exists` observational with respect to the public codec buffer and RPG state. It preserves SRPG payload format 1 and introduces no Phase 7 world features.

Phase 7 adds `Smile.Game` 1.0.0 for reusable movement, animation, SMILE-MAP 1, camera, and collision mechanics, and advances `Smile.RPG` to 1.1.0 with World, Story, Encounters, and SRPG format-2 persistence while retaining format-1 reads. `examples\RpgWorldGallery` is the original DirectX/GDI/Web capability proof. See `docs\language\phase7-rpg-world.md` and `docs\architecture\phase7-top-down-rpg-world.md`.

Phase 7.1 advances `Smile.RPG` to 1.1.1 and makes visible-solid occupancy a world invariant across definitions, reveals, spawns, transitions, direct progress restoration, resets, and format-2 loads. Save restoration validates the complete final actor layout, preserves transient actors and reservations, applies persistent actors as one hidden batch, and rolls every RPG module and active reservation back after an unexpected apply failure. `CurrentScene` and `ControlledActor` now remain coherent whenever both are nonzero. The map and save formats remain SMILE-MAP 1 and SRPG 2 with SRPG 1 reads.

Phase 8 proves that those existing packages can compose complete dungeon exploration without a new public API or persistence format. `examples\RpgDungeonGallery` contains an original three-floor cardinal first-person dungeon and four-floor top-down dungeon with doors, locked doors, keys, treasure and Gold, traps, a hidden passage, stairs, a chute, a warp, state-aware NPC dialogue, escape, encounter preview, and in-dungeon save/load. `examples\Phase8DungeonStateTests` verifies the presentation-independent state composition through both project and package references. See `docs\language\phase8-rpg-dungeons.md` and `docs\architecture\phase8-rpg-dungeon-gap-matrix.md`.

Build the Phase 7 packages, state proof, and original world gallery:

```bat
artifacts\compiler\smilec.exe --project libraries\Smile.Game\Smile.Game.smilelibproj --target library -o artifacts\libraries\Smile.Game.smilelib
artifacts\compiler\smilec.exe --project libraries\Smile.RPG\Smile.RPG.smilelibproj --target library -o artifacts\libraries\Smile.RPG.smilelib
artifacts\compiler\smilec.exe --project examples\Phase7WorldStateTests\Phase7WorldStateTests.smileproj --target windows-x64 -o artifacts\tests\Phase7WorldStateTests.exe
artifacts\compiler\smilec.exe --project examples\RpgWorldGallery\RpgWorldGallery.smileproj --target windows-x64 --graphics DirectX -o artifacts\games\RpgWorldGallery-DirectX\RpgWorldGallery.exe
```

Build the Phase 8 dungeon state proof and original gallery:

```bat
artifacts\compiler\smilec.exe --project examples\Phase8DungeonStateTests\Phase8DungeonStateTests.smileproj --target windows-x64 -o artifacts\tests\Phase8DungeonStateTests.exe
artifacts\compiler\smilec.exe --project examples\RpgDungeonGallery\RpgDungeonGallery.smileproj --target windows-x64 --graphics DirectX -o artifacts\games\RpgDungeonGallery-DirectX\RpgDungeonGallery.exe
artifacts\compiler\smilec.exe --project examples\RpgDungeonGallery\RpgDungeonGallery.smileproj --target web --output-dir artifacts\web\RpgDungeonGallery
```

Loose-file builds can add a built package with repeated `--library <path.smilelib>`. Project builds read `<SmileProjectReference>` and `<SmileLibraryReference>` items, build project dependencies in deterministic order, reject cycles, and reuse a referenced project package only when its identity, modules, normalized source hashes, and direct dependency identities match the current library project. Imports follow direct provider boundaries: application and library-project sources see only their own modules and direct references, package sources see only exact manifest dependencies, and loose roots see every package supplied directly with `--library`.

Add each declaration-only support file with a repeatable `--source` option. The files are parsed independently and share one case-insensitive semantic model; only the startup file may contain executable top-level statements, `Game Window`, or `End Program`:

```text
artifacts\compiler\smilec.exe examples\MultiFileBasics\Program.smile ^
  --source examples\MultiFileBasics\GameState.smile ^
  --source examples\MultiFileBasics\Drawing.smile ^
  -o artifacts\games\MultiFileBasics\MultiFileBasics.exe
```

`examples\MultiFileBasics` is the small teaching example: `Program.smile` owns startup and the loop, while `GameState.smile` and `Drawing.smile` contribute shared declarations and routines.

Use `--keep-temp` to retain generated MASM assembly and object files under `artifacts\temp`. Copy declared asset trees beside a loose-file executable before running it; Dungeon Star I needs both its `Assets` and editable `Maps` directories, while Dungeon Star II needs its editable `Maps` directory.

Select the static browser target explicitly with `--target web` and an output directory:

```text
artifacts\compiler\smilec.exe games\PaddleBall\Program.smile --target web --output-dir artifacts\web\PaddleBall
artifacts\compiler\smilec.exe examples\MultiFileBasics\Program.smile --source examples\MultiFileBasics\GameState.smile --source examples\MultiFileBasics\Drawing.smile --target web --output-dir artifacts\web\MultiFileBasics
```

That command writes `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`. Copy the project's declared `Assets` and `Maps` trees beside those files when compiling a loose file. Web output is plain HTML, CSS, and JavaScript using Canvas 2D; it has no native runtime, MASM/linker, npm, framework, or machine-local-path dependency. Native commands continue to default to `windows-x64`, and the explicit native spelling is `--target windows-x64`.

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
artifacts\games\DungeonStarII\DungeonStarII.exe
artifacts\games\MazeMuncher\MazeMuncher.exe
```

The executables are self-contained native game programs with respect to SMILE: neither `smilec.exe` nor Visual Studio is needed to run them. They use normal Windows system libraries; a music-bearing executable also uses the Microsoft Visual C++ runtime installed by Visual Studio or the supported Visual C++ Redistributable.

## Visual Studio 2026

Build and automatically replace the installed extension with:

```text
scripts\install-vsix.cmd
```

The script targets Visual Studio Enterprise, rebuilds the VSIX, uninstalls the existing `Smile.VisualStudio.2.0` extension when present, and force-installs the newly built package. Visual Studio may close during the refresh, so save open work before running it. Detailed installer logs are written under `artifacts\temp`.

After Visual Studio restarts, use **File > New > Project** and search for `SMILE`. Three templates are installed: console, game, and **SMILE 2.0 Library**.

- **SMILE 2.0 Console Application**
- **SMILE 2.0 Game Application**

Both create a `.smileproj` with `Debug` and `Release` configurations. A game project also owns an `Assets` folder. Every included solution and newly created project exposes `Windows 64-bit .exe` first and `Web` beside it in the platform selector:

- `Debug|Windows 64-bit .exe` and `Release|Windows 64-bit .exe` preserve the existing native output at `bin\Debug\Game.exe` or `bin\Release\Game.exe`.
- `Debug|Web` and `Release|Web` publish the same selected startup/support source set to `bin\Debug\Web` or `bin\Release\Web`, including declared assets.

With `Web` selected, **Build > Build Solution** is the publish operation. `F5` or `Ctrl+F5` saves every open participating source, republishes the selected startup/support source set and assets, starts/reuses the VSIX's loopback-only static server, and opens a cache-busted `http://127.0.0.1:<port>/?game=<output-name>&v=<cache-token>` URL in the default browser. Switch back to `Windows 64-bit .exe` for native launch and source-level debugging, including breakpoints in support-file routines. While execution is stopped, hovering an in-scope variable shows its current debugger value in the ordinary educational Quick Info. Project-aware completion includes compilation-wide globals and routines, while diagnostics and squiggles remain attached to their owning physical files. Compiler failures appear in both the SMILE Output pane and Error List with the same diagnostic code, message, file, line, and column used by the editor and command-line compiler.

Solution Explorer supplies the routine project workflow directly. Every project has a References node. Right-click the project to **Build**, **Rebuild**, **Clean**, add a source, add a `.smilelibproj` or `.smilelib` reference, or open the project folder; remove a reference from its own context menu without deleting its target. Library projects build `.smilelib` packages and are intentionally non-runnable. Reference and source mutations refresh the hierarchy and shared editor workspace immediately. `Import` completion offers modules, and `Alias.` completion exposes public members only. Official SMILE 2.0 library aliases and qualified members use a distinct teal **SMILE 2.0 Built-in Module or Library** classification; student-created library symbols keep the ordinary identifier color.

The editor keeps snapshots for every open SMILE buffer in a project. An unsaved declaration change in one file therefore refreshes completion and diagnostics in the other open files after the normal short debounce. Opening an unselected `StartupOnly` program analyzes it as the hypothetical startup with the ordinary support files and excludes the currently selected complete program.

Debug native builds classify the generated MASM implementation as non-user code and expose unique source-mapped SMILE statement helpers as user code. Each helper receives debugger-only named parameters for the variables visible in that SMILE scope, so the native expression evaluator and VSIX Quick Info can display live values without changing program state. Breakpoints bind to physical startup and support files, and F10 moves between SMILE statements and across routine returns without opening generated C, MASM, disassembly, or **Source Not Available** pages.

The published Web directory is ready for a separate static-host upload; the VSIX does not upload to GitHub Pages, Azure, Cloudflare, Netlify, or another remote service.

The game template documents that automatic focus muting is inherited from the shared runtime; new games should use normal `Play Sound` and `Play Music` statements without per-game focus code.

Loose `.smile` files remain supported: open one and use **Tools > Build SMILE File**.

## Language overview

Implemented syntax includes:

- signed 64-bit `Number`, `Boolean`, and mutable UTF-8 `Text` values, constants, and typed one- or two-dimensional fixed arrays;
- project-global and module-owned `Type` records with built-in or nested fields, fixed one- or two-dimensional record arrays, deep value-copy assignment, `ByVal`/`ByRef`, and record function returns; module type lookup is same-module unqualified or explicit imported `Alias.Type`;
- per-physical-source `Option Explicit` and scalar `Dim Name As Number|Boolean|Text` declarations;
- arithmetic and comparison operators, integer `/`, `Mod`, `And`, `Or`, and `Not`;
- multiline `If`/`Else If`/`Else`, ascending and descending `For`, `Do`/`Loop Until`, `Exit For`, `Exit Do`, and `Select Case`;
- `Sub`, `Function`, `Call`, `Return`, typed parameters and returns, default `ByVal`, explicit `ByRef`, routine-local `Dim`, and calls tested through sixteen parameters;
- `Print`, `Get Key`, `Key_Held`, `Wait`, `Random`, `Timer`, `Abs`, `Min`, `Max`, and `Rgb`;
- `Game Window`, double-buffered rectangles, rounded rectangles, circles, arcs, lines, quadrilaterals, text, and numbers, `Show Screen`, asynchronous WAV effects, MP3 background music, and integer persistence through `Load` and `Save`;
- bounded executable-relative UTF-8 input through `Load Text File "path" Into Array Count Variable`, including BOM skipping, zero-fill, and safe missing-file behavior;
- named keyboard constants, including `KEY_OTHER` for unnamed ordinary key events, and named color constants used by the examples and games.

See `docs\language\README.md` and `examples\StructuredLanguageBasics.smile` for concrete syntax.

Arc outlines use `Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color`. Angles are integer screen degrees (`0` right, `90` down, `180` left, `270` up); positive sweeps move clockwise and negative sweeps move counterclockwise. Arcs use the normal outline stroke and do not add a fill, chord, or radial lines.

## Current limitations

- Windows x64 is the complete/default target and requires the Visual Studio MASM/link toolchain when compiling. Web publication supports the shared language and generic runtime surface used by all seven included games and their no-demo teaching variants.
- Web Number values use JavaScript safe integers. Unsafe literals fail Web compilation, and unsafe runtime arithmetic stops with a visible error rather than silently losing precision.
- Web uses Canvas 2D, browser keyboard/audio APIs, `fetch` for declared text/map assets, and `localStorage`. Browser autoplay policy may defer WAV or MP3 playback until the first key or click without stopping the program.
- Native routine-owned `For` limits and Number/Boolean/Text `Select Case` selectors are invocation-local and reentrant. Owned Text selector cleanup runs on normal completion, `Return`, `Exit For`, `Exit Do`, and `End Program`.
- Native console handles receive UTF-16 through `WriteConsoleW`; redirected files and pipes receive the original UTF-8 bytes without a BOM. The Web parity harness under `scripts\run-web-test.js` uses only built-in Node modules.
- Browser `.smile` breakpoints are not yet supported. Windows x64 `.smile` breakpoints, IntelliSense, normal file opening, and native F5 remain supported.
- Numeric storage is signed 64-bit integer only; there is no floating-point type or dynamic collection.
- Arrays are fixed at compile time and support at most two dimensions.
- Record fields cannot be arrays and there are no record literals, constructors, methods, inheritance, pointers, null, record comparison, or whole-array assignment/parameters/returns. Routine calls and hidden-buffer record returns are tested through sixteen explicit parameters.
- The Visual Studio project system intentionally remains focused rather than becoming an MSBuild SDK. Application `.smileproj` and library `.smilelibproj` files share one project model, including sources and project/package references.
- `Play Sound` supports 16 explicit asynchronous WAV effect channels. `Play Music` supports one independent MP3 background track through `Windows.Media.Playback.MediaPlayer`; there are no playlists, seeking, or multiple music channels.
- Project `ApplicationId` is the stable native/Web persistence identity when explicitly present; legacy projects continue to fall back to `OutputName`.
- `.smilelib` packages cannot own assets yet. Before reusable libraries require skins, fonts, sounds, or themes, choose explicitly between consumer-supplied resources and versioned target-neutral resources embedded in the package.
- Windows editions without the required optional media components may decline MP3 playback, but the game continues without crashing.
- Music-bearing native executables require the current Microsoft Visual C++ Redistributable; non-music output does not acquire that additional dependency.

## Asset provenance

All committed WAV files are original deterministic tones or melodies generated by `scripts\generate-sounds.ps1`. `games\FallingBlocks\Assets\Background.mp3` and `games\DungeonStarI\Assets\Background.mp3` are the exact music files supplied by the repository owner for their milestones. The games use runtime-drawn geometric art and system fonts. No third-party audio library or MP3 decoder is bundled.
