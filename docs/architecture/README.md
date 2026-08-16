# Compiler and tooling architecture

SMILE 2.0 uses one deliberately direct native pipeline:

```text
startup .smile + support .smile sources
    -> Smile.Language analysis
    -> Smile.Compiler MASM emitter
    -> ml64.exe and link.exe
    -> native Windows x64 .exe
```

`src\Smile.Language` owns source documents, tokenization, keyword and built-in facts, parsing, syntax nodes, diagnostics, modules/imports/visibility, symbols, the unified built-in/record type model, package validation, project graphs, and semantic analysis. Each physical source becomes its own syntax tree. A shared exact-provider resolver orders project and package libraries, checks identities, versions and cycles, and validates package-owned modules and API metadata with declared dependencies present. A shared module processor resolves qualified public values and types into stable semantic identities, then one compilation-wide bound model feeds both emitters. `smilec` and the Visual Studio extension both call these same resolution and analysis paths; there is no editor-only parser, dependency resolver, or semantic implementation.

Language evolution follows one permanent hierarchy: use existing syntax when it stays clear; otherwise prefer an established BASIC idea; use the smallest beginner-friendly C#-inspired concept only when BASIC has no suitable precedent. Additions remain general-purpose, avoid aliases and clever punctuation, and receive only proportional diagnostics, tests, examples, and documentation through the shared language authority.

## Phase 4 and future 3D readiness

Phase 4 remains a high-resolution 2D milestone. Its native `SmileGraphicsBackend` vtable is the stable current **2D** drawing layer despite the historical general name; DirectX and GDI implement that layer behind the compiler-facing C ABI. The Web Canvas 2D surface provides the same role for Web builds. These layers continue to own images, shapes, text, clipping, and painter-order overlays when a future 3D renderer is introduced.

A future renderer should sit beside this 2D capability rather than replace it or force 3D concepts into today's beginner-level commands. On a 3D-capable target, a future frame may render a 3D world and then composite the existing 2D HUD, menu, text, and image operations. Backend-specific DirectX, Canvas, WebGL, WebGPU, or similar objects remain internal.

The shared project asset resolver and publisher are intentionally file-format-neutral: they validate, identify, copy, and clean declared project assets without assuming PNG, WAV, or any other media format. Runtime ownership and decoding remain type-specific (`SmileImageResource`, WAV caches, and music), so future model, material, or animation resources can add their own lifetime rules without replacing project asset publication or pretending every asset is an image.

No Phase 4 transform, camera, mesh, material, shader, skeletal-animation, lighting, physics, particle, or model-import feature is implied by this direction. Add such systems only in their approved milestones, extending this compiler/runtime and preserving the 2D layer.

The native backend emits MASM x64 and links `Smile.NativeRuntime.lib`. Console programs use the console subsystem. Programs containing `Game Window` use the Windows GUI subsystem and the generic Win32 runtime for:

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

Each native routine has one computed frame layout covering local scalars, inline records and arrays, invocation-owned `For` limits, `Select Case` selectors, record-return temporaries, and a separate return slot. Text and record temporary slots start at zero and participate in explicit cleanup. Compiler-generated record initialize/clear/copy helpers recurse through inline fields, retain and release owned `Text`, and make self-assignment safe. Record functions receive a hidden caller-provided return buffer before their explicit Windows x64 arguments. Loop exit contexts retain the cleanup depth at entry, allowing `Return`, `Exit For`, `Exit Do`, and `End Program` to release only the owned temporaries they leave while the common epilogue remains an idempotent safety net. Windows console output detects a real console with `GetConsoleMode`, converts UTF-8 to UTF-16 for `WriteConsoleW`, and retains raw UTF-8 `WriteFile` output for redirection.

Web publication remains browser-native. One emitter-owned mapping assigns each bound `RecordFieldSymbol` a deterministic private JavaScript key from its record and field ordinals. Default helpers, clone helpers, reads, writes, nested access, arrays, `ByRef` locations, and record returns all use that mapping, so source names never become object-storage properties. Generated record default/clone helpers give each array element and value transfer an independent object graph. `scripts\run-web-test.js` supplies a repository-owned Node `vm` host for behavioral regression without npm packages or network access. It provides the minimal DOM, Canvas, storage, timing, audio, and fetch surfaces, captures logical console and `fillText` output, enforces a timeout, and compares generated Web behavior with strict UTF-8 native output.

The compiler emits one stable graphics configuration call before game startup and routes every drawing export—including filled and outlined quadrilaterals—through the active `SmileGraphicsBackend` vtable. DirectX builds quadrilaterals with short-lived Direct2D path geometry; GDI maps the same four logical points into its physical back buffer and uses `Polygon`. Backend implementations own their render targets and caches; windowing, input, audio, persistence, and language-level game logic remain outside the graphics modules.

`Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color` follows the same compiler-facing C ABI and backend vtable routing as the other drawing primitives. DirectX renders partial arcs with short-lived Direct2D path geometry and reuses circle rendering for complete arcs. GDI maps the same logical geometry into its physical back buffer, uses the cached outline pen and `Arc`, and restores the prior GDI arc direction after every call. Both backends use integer screen-coordinate degrees (`0` right, `90` down, `180` left, `270` up), with positive clockwise and negative counterclockwise sweeps clamped to one revolution. The primitive adds no fill, chord, radial lines, thickness option, or game-specific wall helper.

Music-bearing generated programs reference a dedicated C-compatible MediaPlayer object and link `WindowsApp.lib` plus the static C/C++ support libraries required by the custom `/entry:main` pipeline. Games without music do not pull that object from `Smile.NativeRuntime.lib`. The MediaPlayer state is allocated lazily, owns no nontrivial global constructor, catches every C++ exception at the C ABI, and is shut down explicitly before each generated process exit.

## Shared audio-focus contract

The Win32 window procedure owns one focus state above both graphics backends. Audio is active only while the application is active, its top-level game window is active, and the window is not minimized. An inactive transition stops the current `PlaySoundW` effect and suppresses later WAV requests. The MediaPlayer remains at its current playback position with effective volume zero while its requested volume is retained. Reactivation reapplies that volume only; it does not restart playback or resume a track paused or stopped by SMILE source. Suppressed WAV effects are not queued for replay.

This process-local policy is inherited by every `Game Window` program and requires no game-specific activation code. It never changes Windows master volume or another process, and DirectX and GDI behave identically because focus and audio remain outside their backend implementations.

The runtime does not contain Snake, falling-block, paddle, brick, dungeon, score, level, projection, generation, map-format, pathfinding, or win/loss rules. Those remain in the corresponding files under `games`. Dungeon Star I parses its external map bytes, validates topology, generates pipe graphs, plans its demo, and composes its pseudo-3D projection entirely in `Program.smile`. Dungeon Star II likewise owns its fixed-point camera, bounded DDA traversal, room map parser and generator, collision, doors, BFS attract route, and anti-aliased wall composition in `.smile`. All use only generic file, graphics, audio, storage, input, and timing services available to every program.

The seven complete game projects are Snake, Falling Blocks, Paddle Ball, Brick Breaker, Dungeon Star I, Dungeon Star II, and Maze Muncher. Each game keeps its rules and render composition in `.smile` source. Every game with an attract demo also includes a genuine `Program-NoDemo.smile` teaching edition without demo or AI implementation code.

The Visual Studio extension embeds the same compiler/runtime payload and registers one factory for `.smileproj` and `.smilelibproj`. The shared project model owns startup/support sources, library identity, and project/package references. Project builds pass `--project` to the compiler so editor diagnostics, build diagnostics, native/Web emission, dependency order, and source debugging use the same files and bound model. Solution Explorer projects References as a live node; add/remove reference commands update XML, hierarchy, and editor analysis immediately.

The editor workspace retains current text snapshots for every open project buffer. A buffer change invalidates analysis caches for the other participating files after the normal debounce. The language analysis carries one direct-provider access context used by project/package validation, editor completion, the compiler, and native/Web emitters; module presence alone never grants import access. Focused per-directory watchers use tolerant participation discovery, preserve last-known reachable paths through partial graph failures, and refresh only the owning project when direct or transitive dependencies change or reappear. Expected graph or package failures become shared `SML32xx` diagnostics while local analysis remains available; unexpected failures are logged and enter a safe diagnostic state instead of faulting the cache. The selected startup uses ordinary supports; an unselected `StartupOnly` file is instead analyzed as a hypothetical startup with those same supports and without the selected complete program. Missing project sources produce a physical-file `SML0001` diagnostic rather than falling back to unrelated single-file semantics. Loose source builds retain their ordinary program behavior while any supplied packages use the shared exact-provider resolver.

Phase 7 adds two source-library layers above the language/runtime: `Smile.Game` owns reusable 2D movement/map/camera/collision mechanics; `Smile.RPG` owns reusable RPG definitions and world/story/encounter progress. Applications own UI, art, audio, maps, and gameplay policy. The complete design is documented in [phase7-top-down-rpg-world.md](phase7-top-down-rpg-world.md).

Phase 8 keeps that architecture unchanged and proves dungeon exploration as application composition. Floors are World scenes, traversable endpoints are spawns, interactive objects are persistent actors, and durable event outcomes are Story, Inventory, Party, Character, Encounter, and World state already covered by transactional SRPG 2 saves. Cardinal first-person and top-down views consume the same presentation-independent state without adding a renderer, scene graph, actor model, or dungeon-specific public API. See [the complete dungeon architecture](phase8-rpg-dungeon-systems.md) and [the pre-implementation gap matrix](phase8-rpg-dungeon-gap-matrix.md).

Debug builds emit unique source-aware C helpers with physical multi-file `#line` mappings and compile those helpers with native Just My Code metadata. Each helper receives the in-scope SMILE variables as debugger-only named parameters. Numbers and Booleans retain readable scalar values, Text values are exposed as read-only strings, and arrays, images, and records retain inspectable native addresses. The native MASM implementation remains below that source surface. Consequently Windows breakpoints bind in startup and support files, hovering an in-scope variable can display its live value, and F10 advances among mapped SMILE statement helpers, including routine returns, instead of stopping in generated implementation ranges. Console and game templates build to `bin\Debug` or `bin\Release`, copy declared assets, populate the SMILE Output pane and Error List, and launch a freshly built executable for F5 or Ctrl+F5.
