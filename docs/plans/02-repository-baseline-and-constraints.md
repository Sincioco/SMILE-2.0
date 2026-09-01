# Repository Baseline and Constraints

## Inspected baseline

Prepared against `main` commit:

```text
ec61dfa6324de7b22ea5ca0959828ff40e5e3902
```

That commit added:

```text
games/SinStarI/Assets/Characters/Sin Star - Character 1 - Paladin - T-Pose.png
```

Codex must record the actual starting commit used for implementation.

## Existing architecture to preserve

### Renderer composition

The existing 3D architecture renders indexed 3D content first, restores the established 2D target, then draws HUD, text, images, menus, and overlays. `Show Screen` remains the final presentation boundary.

This ordering is a permanent contract:

```text
Renderer3D scene
    -> optional 3D post-processing
    -> Renderer2D HUD/text/menu
    -> Show Screen
```

Post-processing must never blur or tone-map the 2D HUD unless an explicit future feature is added.

### Native backend

Primary implementation path:

```text
src/Smile.NativeRuntime/graphics/graphics3d.h
src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp
src/Smile.NativeRuntime/graphics/graphics_directx.cpp
src/Smile.NativeRuntime/graphics/graphics_backend.h
src/Smile.NativeRuntime/runtime.c
```

Current characteristics include:

- Direct3D 11;
- shared device, context, swap chain, and lifecycle;
- indexed triangle rendering;
- generated or explicit normals;
- depth buffering;
- 4x/2x/1x MSAA selection;
- bounded typed handles;
- explicit reset and ownership behavior.

Codex must locate all current dispatch and ABI definitions before assigning new command numbers.

### Web backend

Primary generated-runtime implementation is currently produced through compiler code including:

```text
src/Smile.Compiler/WebEmitter.cs
src/Smile.Compiler/WebOutputWriter.cs
```

Codex must find the exact WebGL2 Renderer3D implementation before editing. Do not create a second unrelated runtime.

Browser constraints:

- pure JavaScript;
- WebGL2 remains the shipping baseline;
- generated package shape remains compatible with current publishing;
- capability absence must fail gracefully;
- typed arrays and GPU buffers must be reused rather than grown per frame.

### Source-facing API

Current reusable source surface:

```text
libraries/Smile.Simple3D/Core.smile
libraries/Smile.Simple3D/Graphics3D.smile
libraries/Smile.Simple3D/Interaction.smile
libraries/Smile.Simple3D/API.md
libraries/Smile.Simple3D/Smile.Simple3D.smilelibproj
```

The existing public model uses:

- integer world units;
- integer degrees;
- percentage scales and material values;
- zero handles for failure;
- explicit destruction;
- `Renderer3D` numeric/image/text bridges hidden behind `Graphics3D`.

Generation 2 should extend this pattern. It should not add new language grammar unless an independently justified language feature is truly required.

### Asset converter

Current converter:

```text
src/Smile.AssetTool/Program.cs
src/Smile.AssetTool/Smile.AssetTool.csproj
src/Smile.AssetTool/README.md
```

Current pipeline:

```text
Blender or another authoring tool
    -> glTF 2.0
    -> smileasset.exe
    -> SM3D
```

Current SM3D version 1 supports static triangle data with position, normal, UV, indices, parts, and material slots. It is deterministic and checksum-protected.

Version 2 must be additive. Version 1 conversion and loading remain tested.

### Current animation

The existing renderer animation layer has:

- 1–32 parent-ordered bones;
- four bone indices and four normalized weights per vertex;
- two transform keys per track;
- up to 16 integer events;
- a fixed 32-matrix palette;
- deterministic CPU evaluation;
- GPU deformation on native and Web.

Generation 2 must not remove this educational path. It may share implementation internally, but v1/simple APIs keep their behavior.

### Current Battle3D

Relevant files include:

```text
libraries/Smile.Battle3D/Actor.smile
libraries/Smile.Battle3D/Articulation.smile
libraries/Smile.Battle3D/Camera.smile
libraries/Smile.Battle3D/Effects.smile
libraries/Smile.Battle3D/Presentation.smile
```

Battle3D currently:

- maps battle participants to caller-owned renderer handles;
- produces deterministic fixed-step presentation commands;
- keeps mechanics independent from render frame cadence;
- provides camera shots and seeded shake;
- provides a small deterministic effects pool;
- retains rigid articulation as a valid style.

Generation 2 must integrate beside `Articulation`; it must not delete the rigid-rig capability.

### Current Dragonfall

Primary files:

```text
games/Dragonfall/DragonfallBattle.smile
games/Dragonfall/DragonfallScene.smile
games/Dragonfall/DragonfallAudio.smile
games/Dragonfall/Program.smile
games/Dragonfall/Program-NoDemo.smile
games/Dragonfall/Dragonfall.smileproj
games/Dragonfall/Dragonfall-NoDemo.smileproj
games/Dragonfall/README.md
```

Current visual construction:

- four heroes use 56-part rigid rigs plus face planes;
- Wave 1 combatants use approximately 30-part role rigs;
- Ashwing uses a 97-part creature rig;
- particle objects are preallocated;
- all combat assets load before the encounter;
- native and Web mechanics are tested for parity;
- both attract-mode and no-demo entry points exist.

The first remaster slice replaces **one hero only**. Do not rewrite all current scene content before the new path is proven.

## Existing focused gates

Codex must inspect and continue using the current scripts, including:

```powershell
.\scripts\test-renderer3d-lifecycle.ps1
.\scripts\test-renderer3d-materials.ps1
.\scripts\test-renderer3d-models.ps1
.\scripts\test-renderer3d-animation.ps1
.\scripts\test-battle3d.ps1
.\scripts\test-dragonfall.ps1
.\scripts\test-simple3d-space-wars.ps1
```

Build and broader gates include repository scripts such as:

```powershell
cmd /c scripts\build.cmd
cmd /c scripts\smoke-test.cmd
```

Use focused gates throughout development. Run the broader smoke gate only at appropriate release points rather than after every small edit.

## Protected compatibility invariants

The following must remain true:

1. Current SM3D v1 assets still load.
2. Current simple materials still render.
3. Current 32-bone/two-key public operations still behave.
4. Current primitive and custom-mesh APIs remain source compatible.
5. Renderer2D remains sharp and composes after Renderer3D.
6. GDI remains a valid non-Renderer3D path.
7. WebGL2 absence does not break console or 2D programs.
8. Existing handles remain generation-safe and stale-handle checks remain meaningful.
9. Destroy operations still reject live dependents.
10. Reset returns all live counters to zero.
11. Dragonfall mechanics do not change because of visual timing.
12. `Program-NoDemo.smile` remains free of attract-mode/player-demo AI.
13. No third-party copyrighted model or effect is silently committed.
14. No runtime network access is required to play the game.
15. No runtime package manager or CDN is required.

## Baseline reconciliation procedure

M0 must:

1. Record:
   - branch;
   - HEAD;
   - upstream;
   - working-tree status;
   - compiler/runtime build status.
2. Read root `AGENTS.md`.
3. Compare current implementations with every claim in this file.
4. Identify newer changes that alter:
   - command dispatch;
   - resource capacities;
   - model format;
   - animation;
   - material handling;
   - Web runtime structure;
   - Dragonfall scene ownership.
5. Update a repository implementation note with:
   - confirmed paths;
   - confirmed limits;
   - changed assumptions;
   - accepted deviations;
   - final milestone map.
6. Run adjacent baseline tests before modifying behavior.
7. Do not proceed to M1 if existing relevant tests are already failing without documenting the failure and isolating its cause.

## Likely code hotspots

The plan expects work in these areas, but Codex must confirm them:

| Area | Likely files |
|---|---|
| Binary model conversion | `src/Smile.AssetTool/*` |
| Native renderer ABI and resources | `src/Smile.NativeRuntime/graphics/graphics3d.h`, `graphics3d_directx.cpp` |
| Runtime dispatch | `src/Smile.NativeRuntime/runtime.c` and related headers |
| Web Renderer3D | `src/Smile.Compiler/WebOutputWriter.cs` and/or `WebEmitter.cs` |
| SMILE value records/constants | `libraries/Smile.Simple3D/Core.smile` |
| Low-level public facade | `libraries/Smile.Simple3D/Graphics3D.smile` |
| High-level actor/effect APIs | new modules in `libraries/Smile.Simple3D` or the nearest confirmed reusable package |
| Battle integration | `libraries/Smile.Battle3D/*` |
| Dragonfall proof | `games/Dragonfall/*` |
| Focused verification | `scripts/test-renderer3d-*.ps1`, `scripts/test-dragonfall.ps1`, fixtures |

## Change-size control

A milestone should not mix:

- binary format redesign;
- PBR shader implementation;
- animation blending;
- particle batching;
- full Dragonfall content conversion;
- unrelated compiler/language work.

Each milestone must have one dominant capability, focused tests, and a coherent commit history.
