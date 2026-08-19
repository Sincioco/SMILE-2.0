# True Simple3D and Neon Cycles post-implementation review

**Flag:** SMILE 2.0 currently has only the bounded `Smile.Simple3D` educational projection layer; it does not yet provide a true depth-tested 3D renderer with indexed meshes on Windows and Web. This means Neon Cycles cannot meet the package’s “true simple 3D” requirements without adding the reusable `Renderer3D` capability described in the specifications.

## Delivery status

- Status: Complete
- Date: 2026-08-20
- Branch: `main`
- Actual starting SHA: `a586f3f1b230c01aeb7f4aafcb24d06568562090`
- Implementation base after reconciliation: `a97b5186bc228dd5a0d746a2c0a112b37f4fa57d`
- Pushed implementation SHA: `9638f98d3cb77b19315c3610e999637450a15435`
- Push result: `9638f98d3cb77b19315c3610e999637450a15435` was pushed successfully to `origin/main`.
- Source preservation: no reset, checkout, or movement to an older handoff baseline was performed.
- Reconciliation note: while implementation was in progress, the pre-existing user solution-file work was committed separately as `a97b5186bc228dd5a0d746a2c0a112b37f4fa57d`. This task did not edit or rewrite those solution files and based its implementation commit on that newer state.

## Delivered scope

### Reusable true 3D capability

`Smile.Simple3D` 2.0.0 now exposes an engine-neutral source API for vectors, matrices, cameras, indexed custom meshes, six primitives, object transforms, colors, opacity, visibility, explicit frame submission, and explicit destruction. The original fixed-point wireframe modules remain present and source compatible for GDI, the existing Gallery, Space Wars, and earlier teaching material.

The compiler/runtime boundary is deliberately narrow. One game-window-only `Renderer3D(command, a, ..., j)` built-in accepts eleven `Number` arguments and lowers to the native or Web backend. `Smile.Simple3D.Graphics3D` owns the internal command values and is the public teaching API; games do not call private backend classes or target-specific APIs.

The renderer provides:

- indexed triangle meshes with generated averaged normals;
- perspective projection and look-at cameras;
- model position, rotation, and scale;
- cube, plane, pyramid, sphere, cylinder, and torus generation;
- custom mesh declaration, vertex/index population, validation, and commit;
- depth testing and resize-aware aspect handling;
- RGBA tinting with standard source-alpha blending;
- 128 bounded mesh slots and 256 bounded object slots;
- typed generation-checked handles and stale-handle rejection;
- explicit reset, destroy, device-loss, and shutdown cleanup;
- Renderer3D first, existing Renderer2D overlay second, and one established `Show Screen` presentation.

### Windows backend

Windows uses Direct3D 11 inside the existing DirectX graphics owner. `graphics3d_directx.cpp` shares the established D3D11 device, context, render target, viewport, resize lifecycle, and presentation path. It adds immutable vertex/index buffers, a constant buffer, built-in HLSL compiled through `d3dcompiler.lib`, a resize-aware D24S8 depth texture, triangle-list rasterization, generated normals, and a standard alpha blend state.

`Begin3D` suspends the active Direct2D draw, binds D3D11 color/depth state, and clears the 3D target. `End3D` unbinds depth and resumes Direct2D on the same target so existing text, rectangles, sprites, menus, and HUD code remain unchanged. The GDI backend and Renderer2D vtable were not replaced.

### Web backend

Web uses a lazily created offscreen WebGL2 canvas. The generated runtime compiles built-in GLSL, uploads indexed geometry, enables depth testing and source-alpha blending, draws triangle lists, and composites the result into the existing Canvas 2D back buffer before normal SMILE 2D commands.

The generated top-level Web contract remains exactly:

- `index.html`
- `smile-runtime.js`
- `game.js`
- `smile.css`

Declared assets continue beneath `Assets/`. WebGL2 absence reports the renderer as unavailable without changing Console programs or ordinary Renderer2D games.

### Math and public API

`Smile.Simple3D.Core` now includes `Vector3`, `Matrix4`, the expanded `Camera3D`, and `Object3D`. `Smile.Simple3D.Math3D` supplies vector construction, add/subtract/scalar multiply, dot, cross, length, normalization, distance, identity/translation/scale/rotation matrices, matrix multiply, point transforms, perspective, and look-at construction. Deterministic integer fixed-point math is used in shared SMILE source.

`Smile.Simple3D.Graphics3D` supplies:

- availability, error, reset, and destruction operations;
- default camera and `Begin3D` / `DrawObject3D` / `End3D`;
- six primitive constructors;
- custom mesh creation, vertex and triangle setting, commit, and object creation;
- mesh vertex/index count queries;
- object position, movement, rotation, scale, color, opacity, and visibility.

Public units are integer world units, integer degrees, percentages, and fixed-point matrices. A zero handle represents failure. Meshes are additionally bounded to 65,535 vertices and 196,608 indices.

### Conformance example

`examples/Simple3DConformance` builds from one SMILE project for DirectX and Web. It shows all six primitives, a plane, near/far overlap for depth validation, perspective rotation, a movable camera, resize-derived aspect, a 2D HUD rendered after 3D, and complete object cleanup.

### Neon Cycles

`games/NeonCycles` is the reference true-Simple3D game and contains:

- a title menu, help, one-player versus AI, and two-player local selection;
- first-to-five match scoring, countdowns, round results, rematch, menu return, and pause;
- a bounded 60 Hz accumulator with at most six catch-up steps;
- continuous deterministic integer movement and relative left/right turning;
- swept arena, own-trail, opponent-trail, and simultaneous-path collision;
- same-tick draw handling and score-once behavior;
- a deterministic seeded AI that observes state and submits only legal turn requests;
- a true 3D arena, cycles, vertical trail walls, and existing Renderer2D HUD/menu overlays;
- keyboard and existing virtual-control support;
- standard game-focus audio lifecycle and four packaged audio assets;
- separate simulation, AI, presentation, and test modules.

The game creates render resources once: eight owning objects plus 130 shared-mesh trail instances. At most 137 visible objects are submitted in a saturated frame. Round reset and simulation steps only update transforms/visibility; they do not allocate renderer resources. Each player retains at most 64 authoritative trail segments.

## Syntax and compatibility

- New application-facing statement grammar: **None**
- Internal built-in added: `Renderer3D(command, a, b, c, d, e, f, g, h, i, j)` with eleven `Number` arguments, restricted to `Game Window` programs
- New package format: None
- New external or npm dependency: None
- New game-specific runtime branch: None
- Renderer2D: preserved
- GDI behavior: preserved
- Console program behavior: preserved
- Existing desktop keyboard behavior: preserved
- Windows native behavior outside the additive DirectX Renderer3D path: preserved
- Existing Simple3D wireframe API: preserved

## Files added or changed

The implementation commit changed 41 files with 4,297 insertions and 77 deletions.

### Compiler, language, and generated Web runtime

- `src/Smile.Language/Syntax.cs`
- `src/Smile.Language/Semantics.cs`
- `src/Smile.Compiler/MasmEmitter.cs`
- `src/Smile.Compiler/NativeToolchain.cs`
- `src/Smile.Compiler/WebEmitter.cs`
- `src/Smile.Compiler/WebOutputWriter.cs`
- `src/Smile.Tests/Program.cs`
- `scripts/run-web-test.js`

### Native runtime

- `src/Smile.NativeRuntime/Smile.NativeRuntime.vcxproj`
- `src/Smile.NativeRuntime/graphics/graphics_directx.h`
- `src/Smile.NativeRuntime/graphics/graphics_directx.cpp`
- `src/Smile.NativeRuntime/graphics/graphics3d.h`
- `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp`

### Public library and tests

- `libraries/Smile.Simple3D/Smile.Simple3D.smilelibproj`
- `libraries/Smile.Simple3D/Core.smile`
- `libraries/Smile.Simple3D/Math3D.smile`
- `libraries/Smile.Simple3D/Graphics3D.smile`
- `libraries/Smile.Simple3D/API.md`
- `libraries/Smile.Simple3D/README.md`
- `examples/Simple3DTests/Program.smile`
- `examples/Simple3DConformance/Program.smile`
- `examples/Simple3DConformance/Simple3DConformance.smileproj`
- `examples/Simple3DConformance/README.md`

### Neon Cycles

- `games/NeonCycles/Program.smile`
- `games/NeonCycles/NeonCyclesSimulation.smile`
- `games/NeonCycles/NeonCyclesAI.smile`
- `games/NeonCycles/NeonCyclesTests.smile`
- `games/NeonCycles/NeonCyclesTests.expected.txt`
- `games/NeonCycles/NeonCycles.smileproj`
- `games/NeonCycles/NeonCyclesTests.smileproj`
- `games/NeonCycles/README.md`
- `games/NeonCycles/Assets/Background.mp3`
- `games/NeonCycles/Assets/Crash.wav`
- `games/NeonCycles/Assets/Round.wav`
- `games/NeonCycles/Assets/Turn.wav`

### Documentation and permanent validation

- `docs/architecture/true-simple3d-renderer3d.md`
- `docs/architecture/simple3d-software-rendering.md`
- `docs/architecture/README.md`
- `docs/language/README.md`
- `docs/libraries/README.md`
- `scripts/test-true-simple3d-neon-cycles.ps1`

## Generated outputs

- Compiler: `artifacts/compiler/smilec.exe`
- VSIX: `artifacts/vsix/Smile.VisualStudio.vsix`
- Library: `libraries/Smile.Simple3D/bin/Release/Smile.Simple3D.smilelib`
- Math tests: `artifacts/tests/Simple3DTests.exe` and `artifacts/web/Simple3DTests/`
- Simulation tests: `artifacts/tests/NeonCyclesTests.exe` and `artifacts/web/NeonCyclesTests/`
- Conformance: `artifacts/examples/Simple3DConformance/Simple3DConformance.exe` and `artifacts/web/Simple3DConformance/`
- Neon Cycles: `artifacts/games/NeonCycles/NeonCycles.exe` and `artifacts/web/NeonCycles/`
- Existing Pong sample: `artifacts/games/PaddleBall/PaddleBall.exe` and `artifacts/web/PaddleBall/`

The two Web applications retain the four required generated top-level files and publish only their declared assets in addition to those files.

## Automated validation evidence

### Build and packaging

Command:

```powershell
cmd /c scripts\build.cmd
```

Result: exit code 0. Native runtime compilation included `graphics3d_directx.cpp`; the managed compiler and extension built successfully. The final paths reported were `D:\SMILE 2.0\artifacts\compiler\smilec.exe` and `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`.

### Managed language/compiler suite

Command:

```powershell
dotnet run --project .\src\Smile.Tests\Smile.Tests.csproj -c Release
```

Result: exit code 0 and `287 SMILE language, compiler, project, completion, and timing tests passed.` The printed synthetic assembler, linker, emission, and publication failures are expected negative-test fixtures.

### Formatter gates

Commands:

```powershell
& .\scripts\test-smile-formatter.ps1
& .\scripts\format-smile-style.ps1 -Check -FormatLongIf -IncludeUntracked
```

Results:

- `13 focused SMILE formatter integration tests passed.`
- `SMILE style check passed for 299 file(s).`

An explicit nine-file check of every changed/new SMILE source also passed.

### True Simple3D and Neon Cycles focused gate

Command:

```powershell
& .\scripts\test-true-simple3d-neon-cycles.ps1
```

Result: exit code 0 and `True Simple3D and Neon Cycles focused validation passed.` It verified:

- native and Web `Smile.Simple3D` math execution with exact console parity;
- vector/matrix edge cases, matrix order, perspective, look-at, legacy handles, and zero normalization;
- native and Web Neon simulation/AI execution with exact console parity;
- movement, left/right turns, wall/own/opponent collisions, tunneling, simultaneous draws, score-once, reset, target score, deterministic input sequence, deterministic AI choice, obstacle avoidance, and bounded survival;
- DirectX and Web conformance builds;
- WebGL2 depth enablement, indexed uploads/draws, 2D composition, all six primitive lifecycles, nonzero vertex/index counts, triangle alignment, stale-handle rejection, and repeated reset cycles;
- native and Web Neon Cycles builds;
- one-player and two-player generated Web execution paths.

### Existing Simple3D and Space Wars regression gate

Command:

```powershell
& .\scripts\test-simple3d-space-wars.ps1
```

Result: exit code 0 and `Simple3D and Space Wars focused validation passed.` Native/Web wireframe Simple3D, Gallery, Space Wars, no-demo, assets, and state parity remained intact.

### JavaScript syntax

Command:

```powershell
node --check scripts/run-web-test.js
```

Result: exit code 0 with no diagnostics.

### Full repository validation gate

Command:

```powershell
cmd /c scripts\smoke-test.cmd
```

Result: exit code 0. Recorded results include:

- developer environment and bounded-process runner passed;
- native runtime, compiler, tests, and VSIX rebuilt;
- 287 managed language/compiler/project tests passed;
- 13 formatter integration tests passed;
- the smoke gate's tracked-file style scan passed for 292 files before the new files were committed;
- 39 native graphics/audio-focus checks passed;
- 38 native Text runtime checks passed;
- Phase 2 through Phase 9 native/Web/package/rollback suites passed;
- Phase 4 media/cache/clip/data/audio and mobile-control coverage passed;
- Phase 5 UI/hardening coverage passed;
- all seven existing game demo and no-demo Web versions compiled;
- Paddle Ball/Pong demo and no-demo native/Web outputs compiled;
- all required native x64 GUI outputs, game asset copies, viewport sizes, DPI calculations, and VSIX payload were verified;
- VSIX identity and versions remained synchronized at `2.0.48`.

## Manual validation

Completed on Windows DirectX using the final built outputs:

- `Simple3DConformance.exe` opened and visibly rendered the plane, cube, sphere, pyramid, cylinder, and torus in perspective with occlusion/depth and the existing 2D status overlay.
- `NeonCycles.exe` opened the title/menu flow; `1` entered one-player mode; the perspective arena, cycles, vertical trails, HUD, round progression, and score changes were visible; the process closed cleanly afterward.
- `PaddleBall.exe`, the repository's Pong sample, opened and visibly ran its existing computer-versus-computer demo unchanged.

The dependency-free Web harness executed both renderer and game Web output. A real localhost WebGL2 browser visual was attempted, but the Codex in-app browser rejected local URLs under its security policy. No real-browser visual pass is claimed. This remains the only manual validation item requiring a normal browser environment.

## Architecture decisions

- Renderer2D remains permanent; Renderer3D is an additive sibling layer.
- One hidden numeric command bridge was preferred over broad parser grammar or backend types in SMILE source.
- Direct3D 11 and WebGL2 were selected because they match the repository's existing Windows graphics ownership and dependency-free generated Web runtime.
- The same public `Smile.Simple3D` source API is used on both targets.
- Custom meshes commit once; geometry creation is expected outside loops.
- Renderer handles are bounded and generation checked rather than raw backend pointers or unbounded maps.
- Renderer availability is explicit so WebGL2 absence and non-DirectX builds fail safely.
- Gameplay/collision remains deterministic application-owned integer geometry, independent from render meshes.
- Neon Cycles AI is constrained to the same turn-request interface as a human player.
- Existing Web output, virtual controls, keyboard handling, publication, native APIs, and GDI paths are preserved.

## Known limitations and deferred improvements

- Renderer3D currently provides tinted lit geometry only: no textures, material system, model loading, scene graph, skeletal animation, shadows, particles, rigid-body physics, networking, or student-authored shaders.
- GDI does not rasterize the new true-3D path; the preserved legacy wireframe library remains the GDI teaching path.
- The native shader compiler runs for the small built-in shader pair during renderer initialization; precompiled shader blobs could be considered later if startup profiling justifies it.
- Primitive geometry generation is implemented independently inside the two backends under one conformance-tested command contract. A future shared data generator is possible but was not needed for this milestone.
- Neon Cycles is intentionally local-only. No online multiplayer, replay file, tournament mode, or gamepad-specific mapping was added.
- Physical GPU performance numbers were not inferred from structural limits. The implementation instead bounds resources, catch-up steps, trail history, and frame submissions and avoids per-frame allocation.
- A normal-browser WebGL2 visual/resizing pass remains recommended because the in-app browser policy blocked localhost. Automated generated-Web rendering, lifecycle, parity, and input coverage passed.

## Acceptance conclusion

The reusable true Simple3D API, D3D11 backend, WebGL2 backend, all required primitives, depth-tested indexed meshes, 2D overlay coexistence, conformance example, deterministic Neon Cycles simulation/AI/gameplay, native/Web builds, permanent focused validation, legacy regressions, documentation, and manual Windows checks are complete. No known Critical or Major issue remains within the authorized milestone.
