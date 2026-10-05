# Compiler and runtime contracts

[Architecture index](README.md) · [Visual Studio tooling](visual-studio-tooling.md)

This topic carries the detailed implementation contracts formerly embedded in the architecture
entry. Language/package semantics remain owned by
[`src/Smile.Language`](../../src/Smile.Language); native emission and build lifecycle by
[`src/Smile.Compiler`](../../src/Smile.Compiler); native services by
[`src/Smile.NativeRuntime`](../../src/Smile.NativeRuntime). Web descriptions document the
existing shared implementation; Web development, publication and browser acceptance remain
paused by current project direction.

## Shared language authority

SMILE 2.0 uses one deliberately direct native pipeline:

```text
startup .smile + support .smile sources
    -> Smile.Language analysis
    -> Smile.Compiler MASM emitter
    -> ml64.exe and link.exe
    -> native Windows x64 .exe
```

`src\Smile.Language` owns source documents, tokenization, keyword and built-in facts, parsing,
syntax nodes, diagnostics, modules/imports/visibility, symbols, the unified built-in/nominal
type model, package validation, project graphs, and semantic analysis. Each physical source
becomes its own syntax tree. A shared exact-provider resolver orders project and package
libraries, checks identities, versions and cycles, and validates package-owned modules and API
metadata with declared dependencies present. A shared module processor resolves qualified public
values and types into stable semantic identities, then one compilation-wide bound model feeds
both emitters. `smilec` and the Visual Studio extension both call these same resolution and
analysis paths; there is no editor-only parser, dependency resolver, or semantic implementation.

Language evolution starts with clear existing syntax, then Visual Basic, then another
established readable BASIC precedent. Only when BASIC has no suitable precedent should an
addition use the smallest beginner-friendly concept from another language. Keep the shared
language authoritative; avoid aliases and duplicated compiler/editor semantics. Follow
[repository instructions](../../AGENTS.md).

## Lightweight OOP and package architecture

SMILE deliberately keeps three distinct composition models:

- `Module` is a shared service, namespace, bounded handle engine, or intentional singleton.
- `Type` is a nominal inline value. Assignment and `ByVal` make deep copies; `ByRef` and instance members operate on a stable writable location.
- `Class` is a nominal reference object. Assignment and `ByVal` retain the same identity; `ByRef` may rebind a scalar reference; `Nothing` is the default reference and `Is`/`Is Not` compare identity.

Enums, Optional defaults, named arguments, Type/Class members, constructors, and Properties all
enter the same syntax, symbol, semantic, capability, source-location, and package pipelines. A
bound call records explicit evaluation in source order separately from declaration/ABI
placement. Type methods and Class members receive a hidden `Me`; a Property setter receives
hidden contextual `Value`. These hidden values never become source-visible parameters, named
arguments, completion candidates, or public package parameters. Method receivers are captured
before explicit arguments. Property assignment preserves SMILE's established order by evaluating
the right-hand side before resolving the receiver, even though the accessor ABI receives
receiver before Value.

Type storage retains the existing generated initialize, deep-copy, and clear helpers. Native
Class objects use deterministic automatic reference counting. The compiler retains and releases
scalar roots, arguments, returns, staged values, fields, and globals, while generated Class
cleanup walks every cleanup-bearing field in reverse declaration order and every fixed array in
reverse element order before freeing object storage. A separate native active-frame chain
unwinds caller and callee Text, Image, Type, Class, and clip ownership during non-local
termination; the staged-call chain continues to own only partially evaluated arguments,
receivers, and values. Allocation failure has a distinct deterministic runtime message and exit
path. There are no user finalizers, class-reference fields, cycles, or tracing garbage collector
in this milestone. The Web target implements the same explicit retain/release and finalization
contract in its shared runtime instead of relying on JavaScript garbage-collection timing. Both
targets keep generated field storage collision-safe and fail deterministic `Nothing` member
access without invoking arbitrary debugger-time code.

`.smilelib` format 7 is the one current package format. Its canonical manifest and public API
carry exact logical `name@version` provider identity, normalized `src/...` source IDs, source
locations, visibility, capability requirements, Optional/default metadata, Enums, Type members,
Classes, constructors, and accessor-specific Property metadata. ZIP entry order, JSON order,
timestamps, normalized source bytes, and fingerprints are deterministic; absolute build and
extraction paths are forbidden. Formats 1 through 6 are rejected with a rebuild instruction, and
packaged source remains authoritative for byte-exact API regeneration. Validation rejects
unexpected, duplicated, executable, absolute, traversal, non-normal, missing, or hash-mismatched
archive entries before analysis, then regenerates and byte-compares the complete public API so
altered identities, dependencies, locations, ordering, defaults, nominal layouts, constructors,
members, Properties, visibility, or capabilities cannot be trusted from metadata alone.

Package loading is bounded by the supported package resource ceilings documented in [library
contracts](../libraries/README.md). Expanded data is counted during streaming, and package
publication stages, flushes, validates, and atomically replaces the destination while a finite
same-target lock is held. A malformed package, failed write, or competing writer therefore
cannot create a trusted partial provider or truncate the last-known-good package.

The editor, formatter, compiler, native/Web emitters, and package validator consume this shared
model. Completion, Quick Info, F12, diagnostics, formatter casing/rewrites, debug source
mapping, and `requiresGameWindow` propagation therefore agree for project and package sources.
Quick Info and debugger hover expose static Property information but never execute a getter.
Current package versions are declared by each `.smilelibproj`; `Smile.RPG` is 1.3.0. See [the
library index](../libraries/README.md) for package responsibilities.

## Renderer2D and Renderer3D coexistence

The native `SmileGraphicsBackend` vtable remains the stable **2D** drawing layer despite the
historical general name; DirectX and GDI implement that layer behind the compiler-facing C ABI.
Web Canvas 2D provides the same role for Web builds. These layers continue to own images,
shapes, text, clipping, and painter-order overlays.

Renderer3D now sits beside that 2D capability. DirectX renders the 3D pass into the shared D3D11
target before restoring Direct2D; WebGL2 renders into an offscreen canvas before Canvas 2D
composition. Backend objects and shaders remain internal, and GDI continues as an unchanged
Renderer2D-only fallback.

The shared project asset resolver and publisher are intentionally file-format-neutral: they
validate, identify, copy, and clean declared project assets without assuming PNG, WAV, or any
other media format. Runtime ownership and decoding remain type-specific (`SmileImageResource`,
WAV caches, and music), so future model, material, or animation resources can add their own
lifetime rules without replacing project asset publication or pretending every asset is an
image.

The detailed [Renderer3D contract](true-simple3d-renderer3d.md),
[materials](renderer3d-materials.md), [model format](sm3d-model-format.md), [skeletal
animation](renderer3d-skeletal-animation.md) and [GPU particles](renderer3d-gpu-particles.md)
own their supported features and limits.

## Native services and build publication

The native backend emits MASM x64 and links `Smile.NativeRuntime.lib`. Console programs use the
console subsystem. Programs containing `Game Window` use the Windows GUI subsystem and the
generic Win32 runtime for:

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
- application-scoped persistence; see the current [library storage contracts](../libraries/README.md).

Native and Web builds use a 30-second normalized-path output mutex, so writers targeting the
same EXE or Web root cannot interleave while different outputs remain independent. `vswhere` is
bounded to 30 seconds; the combined native C/MASM/link toolchain and Visual Studio compiler
process are bounded to 10 minutes. Timeout or Visual Studio cancellation terminates the complete
child process tree and reports `SML5005` or `SML5006`; output-lock timeout reports `SML5008`.
Native intermediates live under the owning project or loose-source
`obj\Smile\Compiler\<unique-build-id>`, are removed on every non-`--keep-temp` exit, and never
fall back to an arbitrary current directory's `artifacts\temp`.

Native EXE/PDB pairs, Web generated files, and project assets are built in unique same-volume
staging directories. Publication backs up every SMILE-managed destination, rolls back a failed
commit, writes the new asset manifest only after replacements are ready, and removes stale prior
managed files only after the replacement set commits. Unrelated output files are never part of
the managed set. A failed emission, tool invocation, generated-file write, or asset copy
therefore leaves the previous valid publication intact; ordinary success and failure remove
staging and backup residue.

## Value lifetime and target emission

Each native routine has one computed frame layout covering local scalars, inline records and
arrays, invocation-owned `For` limits, `Select Case` selectors, record-return temporaries, an
active-frame cleanup record, and a separate return slot. Text and record temporary slots start
at zero and participate in explicit cleanup. Compiler-generated record initialize/clear/copy
helpers recurse through inline fields, retain and release owned `Text`, and make self-assignment
safe. Record functions receive a hidden caller-provided return buffer before their explicit
Windows x64 arguments. Loop exit contexts retain the cleanup depth at entry, allowing `Return`,
`Exit For`, and `Exit Do` to release only the owned temporaries they leave while the common
epilogue remains an idempotent safety net. `End Program`, runtime `Nothing`, and Class
allocation failure first clean staged calls and then unwind every active routine frame before
global teardown. Windows console output detects a real console with `GetConsoleMode`, converts
UTF-8 to UTF-16 for `WriteConsoleW`, and retains raw UTF-8 `WriteFile` output for redirection.

Web publication remains browser-native. One emitter-owned mapping assigns each bound
`RecordFieldSymbol` a deterministic private JavaScript key from its record and field ordinals.
Default helpers, clone helpers, reads, writes, nested access, arrays, `ByRef` locations, and
record returns all use that mapping, so source names never become object-storage properties.
Generated record default/clone helpers give each array element and value transfer an independent
object graph. `scripts\run-web-test.js` supplies a repository-owned Node `vm` host for
behavioral regression without npm packages or network access. It provides the minimal DOM,
Canvas, storage, timing, audio, and fetch surfaces, captures logical console and `fillText`
output, enforces a timeout, and compares generated Web behavior with strict UTF-8 native output.

## Graphics ABI

The compiler emits one stable graphics configuration call before game startup and routes every
drawing export—including filled and outlined quadrilaterals—through the active
`SmileGraphicsBackend` vtable. DirectX builds quadrilaterals with short-lived Direct2D path
geometry; GDI maps the same four logical points into its physical back buffer and uses
`Polygon`. Backend implementations own their render targets and caches; windowing, input, audio,
persistence, and language-level game logic remain outside the graphics modules.

`Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color` follows the same
compiler-facing C ABI and backend vtable routing as the other drawing primitives. DirectX
renders partial arcs with short-lived Direct2D path geometry and reuses circle rendering for
complete arcs. GDI maps the same logical geometry into its physical back buffer, uses the cached
outline pen and `Arc`, and restores the prior GDI arc direction after every call. Both backends
use integer screen-coordinate degrees (`0` right, `90` down, `180` left, `270` up), with
positive clockwise and negative counterclockwise sweeps clamped to one revolution. The primitive
adds no fill, chord, radial lines, thickness option, or game-specific wall helper.

## Native audio and protected helper boundary

Music-bearing generated programs reference a dedicated C-compatible MediaPlayer object and link
`WindowsApp.lib` plus the static C/C++ support libraries required by the custom `/entry:main`
pipeline. Games without music do not pull that object from `Smile.NativeRuntime.lib`. The
MediaPlayer state is allocated lazily, owns no nontrivial global constructor, catches every C++
exception at the C ABI, and is shut down explicitly before each generated process exit.

Native compilation keeps the runtime at warning level 4 with SDL checks and `/GS`
buffer-security instrumentation. The C++/WinRT MediaPlayer object alone uses the DLL CRT because
the final custom-entry link explicitly selects the DLL CRT import libraries; it owns its state
behind a C ABI and never transfers CRT allocations to the C runtime. The generated debug C
helper is the narrow exception to `/GS`: it contains no arrays or writable buffers and is
compiled `/GS-` because the custom `/entry:main` path deliberately bypasses CRT startup and
therefore cannot initialize the normal security cookie. Production SMILE code remains emitted as
MASM and does not inherit that debug-helper exception. Any future C/C++ source with writable
buffers must stay in the protected runtime project or add explicit cookie initialization before
`/GS` is enabled in the custom-entry helper pipeline.

## Shared audio-focus contract

The Win32 window procedure owns one focus state above both graphics backends. Audio is active
only while the application is active, its top-level game window is active, and the window is not
minimized. An inactive transition stops the current `PlaySoundW` effect and suppresses later WAV
requests. The MediaPlayer remains at its current playback position with effective volume zero
while its requested volume is retained. Reactivation reapplies that volume only; it does not
restart playback or resume a track paused or stopped by SMILE source. Suppressed WAV effects are
not queued for replay.

This process-local policy is inherited by every `Game Window` program and requires no
game-specific activation code. It never changes Windows master volume or another process, and
DirectX and GDI behave identically because focus and audio remain outside their backend
implementations.

## Application composition

The runtime does not contain Snake, falling-block, paddle, brick, dungeon, score, level,
projection, generation, map-format, pathfinding, or win/loss rules. Those remain in the
corresponding files under `games`. Dungeon Star I parses its external map bytes, validates
topology, generates pipe graphs, plans its demo, and composes its pseudo-3D projection entirely
in `Program.smile`. Dungeon Star II likewise owns its fixed-point camera, bounded DDA traversal,
room map parser and generator, collision, doors, BFS attract route, and anti-aliased wall
composition in `.smile`. All use only generic file, graphics, audio, storage, input, and timing
services available to every program.

Every game with an attract demo keeps a complete `Program-NoDemo.smile` teaching edition without
demo or AI implementation code. Consult the [root project index](../../README.md) for the
current game inventory; Sin Star I now lives in its independent repository.

Phase 7 adds two source-library layers above the language/runtime: `Smile.Game` owns reusable 2D
movement/map/camera/collision mechanics; `Smile.RPG` owns reusable RPG definitions and
world/story/encounter progress. Applications own UI, art, audio, maps, and gameplay policy. The
complete design is documented in [phase7-top-down-rpg-world.md](phase7-top-down-rpg-world.md).

Phase 8 keeps that architecture unchanged and proves dungeon exploration as application
composition. Floors are World scenes, traversable endpoints are spawns, interactive objects are
persistent actors, and durable event outcomes are Story, Inventory, Party, Character, Encounter,
and World state already covered by transactional SRPG 2 saves. Cardinal first-person and
top-down views consume the same presentation-independent state without adding a renderer, scene
graph, actor model, or dungeon-specific public API. See [the complete dungeon
architecture](phase8-rpg-dungeon-systems.md) and [the pre-implementation gap
matrix](phase8-rpg-dungeon-gap-matrix.md).

Generation 3 Renderer3D effects use a persistent, fixed-slot particle resource shared by native
and Web. The native D3D11 path simulates two alternating structured buffers with a compute
shader. The WebGL2 path simulates two alternating interleaved buffers with transform feedback.
Both render current GPU state directly by instance ID, while the deterministic CPU path remains
the portable reference and fallback. See [Renderer3D GPU particle
architecture](renderer3d-gpu-particles.md).

The consolidated RPGSystems application adds a complementary application-level contract: unique
persistence domains within its single ApplicationId, cumulative initialization, fail-closed
partial cleanup, and repeated same-process entry for every modal Module. This changes no shared
package or lightweight-OOP rule. See [RPGSystems integration
hardening](rpg-systems-integration-hardening.md).

## Validation and historical evidence

Use focused language/compiler and native checks for the owner being changed, plus the
repository's required smoke gates. A docs-only move does not rerun or renew the historical
validation claims. The [original entry](archive/compiler-tooling-before-2026-10-05.md) retains
those claims and the former wording verbatim apart from moved link targets.
