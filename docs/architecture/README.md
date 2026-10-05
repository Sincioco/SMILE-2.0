# Compiler and tooling architecture

SMILE means **Simple Modern and Intuitive Language for Everyone**. This is the
architecture entry point; each topic owns its detailed contracts and evidence.

Current Studio is the existing native application under
[`tools/Character3DViewer`](../../tools/Character3DViewer/README.md), named
**SMILE 2.0 - Sin Star I - Game Engine and Studio**. The independent Sin Star I
game consumes Studio-approved systems and content. The September separate-Studio
design is historical; Web development, publication and browser acceptance remain
paused. [AGENTS.md](../../AGENTS.md) owns permanent project direction.

## Core owners and contracts

```text
startup .smile + support .smile + project/package references
    -> Smile.Language analysis and bound model
    -> Smile.Compiler MASM emitter
    -> ml64.exe and link.exe
    -> native Windows x64 .exe
```

`src/Smile.Language` is the single authority for language and project/package
semantics. The compiler and Visual Studio consume that model. The native runtime
provides generic services; SMILE source owns application rules.

| Read for | Current document |
| --- | --- |
| Emission, packages, lifetime, publication and native services | [Compiler and runtime](compiler-and-runtime.md) |
| Project commands, workspace analysis, debugging and UI checks | [Visual Studio tooling](visual-studio-tooling.md) |
| Mandatory logo, visible minimum, progress and artifact metadata | [Startup presentation](startup-presentation.md) |
| Language/project/package use and supported package limits | [Language](../language/README.md), [libraries](../libraries/README.md) |
| Numeric semantics and production precision boundary | [Double](../language/double.md), [Precision3D](../libraries/precision3d-boundary.md) |
| Module, value Type and reference Class design | [Lightweight OOP plan](SMILE-2.0-Lightweight-OOP-Implementation-Plan.md), [hardening](lightweight-oop-hardening-plan.md) |
| Native Studio ownership, save safety and focused tests | [Viewer architecture](../../tools/Character3DViewer/ARCHITECTURE.md) |
| Map documents, preparation, World links and terrain layers | [Town authoring](town-authoring.md), [terrain elevation](terrain-elevation.md) |

## Rendering and assets

Renderer2D remains first-class for images, text, shapes, menus and HUDs.
Renderer3D composes its filled 3D pass alongside it; backend GPU details stay
internal. Asset publication is format-neutral, while each runtime resource owns
its decoding and lifetime. The documents below own numerical budgets and APIs.

| Topic | Contract |
| --- | --- |
| Educational projection layer | [Simple3D software rendering](simple3d-software-rendering.md) |
| Indexed meshes, cameras and Renderer2D coexistence | [Renderer3D](true-simple3d-renderer3d.md) |
| Texture/color/material response | [Materials](renderer3d-materials.md) |
| Offline import and deterministic runtime model format | [SM3D model format](sm3d-model-format.md) |
| Rig, clip, skinning and animation ownership | [Skeletal animation](renderer3d-skeletal-animation.md) |
| Planar reflection limits and native checks | [Reflections](renderer3d-reflections.md) |
| Fixed-slot CPU/GPU effects and capacity admission | [GPU particles](renderer3d-gpu-particles.md) |
| Generation 3 stages and shared effect constraints | [Preflight](renderer3d-vfx-generation-3-preflight.md), [soft depth](renderer3d-soft-depth-m7e-a.md), [distortion](renderer3d-distortion-m7e-b.md), [common GPU particles](renderer3d-gpu-particle-common-m7e-c.md) |

## RPG application composition

`Smile.Game` owns reusable movement/map/camera/collision mechanics; `Smile.RPG`
owns reusable RPG definitions and progress. Applications own UI, art, maps and
gameplay policy. Module re-entry and persistence must retain these boundaries.

| Topic | Contract and evidence |
| --- | --- |
| Top-down world | [Phase 7](phase7-top-down-rpg-world.md) |
| Dungeons over existing presentation-independent state | [Phase 8](phase8-rpg-dungeon-systems.md), [gap matrix](phase8-rpg-dungeon-gap-matrix.md) |
| Battle logic and its implementation boundary | [Phase 9](phase9-rpg-battle-system.md), [gap matrix](phase9-rpg-battle-gap-matrix.md) |
| Renderer-neutral battle presentation | [Battle3D](battle3d.md), [BattleTime](battle-time.md), [battle camera/VFX](battle-camera-vfx.md) |
| Re-entry, unique persistence domains and cleanup | [RPGSystems hardening](rpg-systems-integration-hardening.md) |
| Earlier native/Web boss-fight acceptance | [Dragonfall delivery report](../implementation/dragonfall-3d-battle-delivery.md) |

## Historical direction and evidence

The [September 9 separate Studio design](2026-09-09%20-%20SMILE%202.0%20Studio.md)
and [September 10 visual interpretation](studio-visual-design.md) remain design
history. They do not authorize resuming `tools/SmileStudio` or replacing the current
native Studio. [Web/mobile controls](web-mobile-virtual-controls.md) preserve the
existing Web design without changing the indefinite pause.

The [original compiler/tooling entry](archive/compiler-tooling-before-2026-10-05.md)
retains pre-split prose, old measurements and dated VSIX results. The
[town-authoring archive](archive/town-authoring-2026-09-26-to-10-01.md) retains the
September 26–October 1 milestones. Historical pass counts are evidence for their
recorded revision, not current acceptance. Keep future status and evidence in the
relevant topic; do not grow another diary in this index.

## Previous entry anchors

<a id="lightweight-oop-and-package-architecture"></a>

Lightweight OOP and packages moved to [compiler/runtime contracts](compiler-and-runtime.md#lightweight-oop-and-package-architecture).

<a id="renderer2d-and-renderer3d-coexistence"></a>

Renderer coexistence moved to [compiler/runtime contracts](compiler-and-runtime.md#renderer2d-and-renderer3d-coexistence).

<a id="shared-audio-focus-contract"></a>

Audio focus moved to [compiler/runtime contracts](compiler-and-runtime.md#shared-audio-focus-contract).
