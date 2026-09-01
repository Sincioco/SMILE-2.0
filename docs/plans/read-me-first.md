# SMILE 2.0 and Dragonfall Visual Generation 2

## Purpose

This package is the implementation handoff for modernizing SMILE 2.0's reusable 3D stack and proving it in a Dragonfall remaster vertical slice.

The intended result is a modern stylized 3D RPG presentation with:

- imported skinned characters rather than dozens of rigid primitive parts;
- approximately 10,000 to 15,000 runtime triangles for an equipped principal hero;
- PBR-style materials with normal, roughness, metallic, occlusion, and emissive information;
- modern lighting, a bounded shadow path, HDR-style post-processing, tone mapping, and bloom;
- animation clips with smooth cross-fading, sockets, events, and root-motion support;
- object-free batched particles, flipbooks, ribbons, impact lights, and layered effects;
- a small, beginner-friendly SMILE API that hides renderer complexity;
- Windows Direct3D 11 and browser WebGL2 support from the same SMILE source;
- permanent Renderer2D composition for HUD, menus, text, portraits, and accessibility overlays.

This remains **SMILE 2.0**. It is an incremental evolution of the existing architecture, not a restart and not a new language.

## Repository baseline

This plan was prepared against:

- repository: `Sincioco/SMILE-2.0`;
- branch: `main`;
- inspected commit: `ec61dfa6324de7b22ea5ca0959828ff40e5e3902`;
- local repository convention: `D:\SMILE 2.0`.

The inspected commit added the Paladin T-pose reference image under Sin Star I. That image and the supplied knight reference establish a **quality and style target only**. They are not production-ready 3D assets.

Codex must reconcile the plan with the actual checked-out `main` branch before editing. Newer repository decisions override stale path or implementation assumptions in this package.

## Important capability flags

**Flag:** The current SM3D version 1 path is primarily a static-model interchange and does not bundle a production skinned character, arbitrary animation clips, PBR material texture sets, sockets, morph targets, or LOD information. The implementation must add the smallest backward-compatible SM3D version 2 capability needed by the vertical slice.

**Flag:** The current skeletal layer is limited to 32 bones and two-key transform tracks. The implementation must add a bounded production-animation path without breaking the current educational animation API.

**Flag:** The current material path uses a single PNG plus basic tint, opacity, unlit, and emissive controls. The implementation must add a bounded PBR-style material path while preserving the simple material path.

**Flag:** The current Dragonfall effects path uses a small deterministic particle pool rendered through ordinary objects. The implementation must add a batched effect-rendering path rather than increasing the number of per-particle `Object3D` instances.

**Flag:** A production-quality knight cannot be reconstructed reliably from a single image by renderer code. Engine work must use deterministic test fixtures until an original or properly licensed, rigged GLB asset satisfying `13-production-character-asset-handoff.md` is available.

These flags are expected and authorized. Codex should still use the repository's required `**Flag:**` notice whenever it actually crosses one of these unsupported capability boundaries.

## Authority order

Codex must use this order when instructions differ:

1. Root `AGENTS.md`.
2. Current repository code, tests, and architecture documentation.
3. This handoff package.
4. Older design discussions or visual references.

Do not alter root architectural guarantees merely to match a draft API name in this package.

## How to use this package

Read the files in this order:

1. `00-codex-handoff-instructions.md`
2. `01-executive-implementation-plan.md`
3. `02-repository-baseline-and-constraints.md`
4. `03-target-architecture.md`
5. `04-sm3d-v2-asset-pipeline.md`
6. `05-renderer3d-pbr-lighting-and-post-processing.md`
7. `06-animation-generation-2.md`
8. `07-vfx-generation-2.md`
9. `08-beginner-facing-smile-api.md`
10. `09-dragonfall-remaster-vertical-slice.md`
11. `10-testing-performance-and-acceptance.md`
12. `11-milestone-task-backlog.md`
13. `12-risk-register-and-design-decisions.md`
14. `13-production-character-asset-handoff.md`
15. `14-codex-milestone-prompts.md`

Use `14-codex-milestone-prompts.md` to start one bounded Codex implementation run at a time.

## Milestone sequence

| Milestone | Outcome |
|---|---|
| M0 | Reconcile the plan against current `main`, capture baselines, and lock compatibility tests |
| M1 | Add backward-compatible SM3D v2 core, GLB import, tangents, and PBR material metadata |
| M2 | Add PBR-lite material rendering, linear texture handling, mipmaps, and bounded scene lighting |
| M3 | Add production skeletal animation import and playback, larger bone palettes, cross-fades, events, root motion, and sockets |
| M4 | Add beginner-facing `Character3D` and `Scene3D` modules plus a Character Lab example |
| M5 | Add one shadow path, HDR-capable scene targets, tone mapping, bloom, and quality profiles |
| M6 | Add batched VFX, flipbooks, ribbons, multi-emitter compositions, and `Effects3D` |
| M7 | Deliver a Dragonfall Visual Generation 2 vertical slice using one production-quality original hero |
| M8 | Remaster the remaining cast and make the new path the default only after acceptance |

## Non-negotiable implementation rules

- Do not replace the custom renderer with Three.js, Babylon.js, Unity, Unreal, or another engine.
- Do not add TypeScript or a JavaScript framework. The browser runtime remains pure JavaScript.
- Do not require WebGPU. Preserve WebGL2 as the shipping browser backend.
- Do not parse Blender, glTF, or GLB at game runtime. Conversion remains offline through `smileasset.exe`.
- Do not remove Renderer2D, GDI behavior, educational wireframe Simple3D, or current SM3D v1 support.
- Do not introduce game-specific native runtime helpers for Dragonfall.
- Keep resources bounded, generation-safe, diagnosable, and explicitly owned.
- Keep battle mechanics deterministic and independent from animation frame rate.
- Do not allocate or load assets at impact time.
- Do not attempt the complete program in one unreviewable change.
- Implement, run, debug, validate, document, commit, and push each milestone.

## What success looks like

A beginner should eventually be able to write code resembling:

```basic
Import Smile.Simple3D.Character3D As Character3D
Import Smile.Simple3D.Effects3D As Effects3D

Dim Hero As Character3D.Actor

Hero = Character3D.Load("Assets\Models\Arin.sm3d")

Call Character3D.Place(Hero, -240, 0, 80)
Call Character3D.Play(Hero, "Idle", True)
Call Character3D.CrossFade(Hero, "SwordAttack", 160)

Call Effects3D.PlayOn("HolySwordImpact", Hero, "SwordTip")
```

The engine—not the student—handles material channels, bone palettes, animation interpolation, render targets, post-processing, particle buffers, shadow maps, and backend differences.
