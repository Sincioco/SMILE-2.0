# Executive Implementation Plan

## Objective

Modernize SMILE 2.0's reusable 3D foundation sufficiently for Dragonfall to present a contemporary stylized RPG battle while preserving the language's beginner-first design.

The project is successful when:

- a principal hero can use one imported skinned model instead of a 56-part rigid assembly;
- the equipped hero renders at approximately 10,000 to 15,000 triangles at close battle distance;
- armor, cloth, skin, hair, and emissive details respond differently to light;
- motion transitions are smooth and body deformation is believable;
- attacks combine animation events, ribbons, particles, lights, camera impulses, and sound;
- native Windows and browser builds come from the same SMILE source;
- a beginner can use the result through a small set of named character, scene, and effect commands.

## Visual target

The supplied knight reference should be treated as a target for:

- silhouette quality;
- armor layering;
- readable face and hair;
- clean deformation topology;
- differentiated materials;
- weapon and shield detail;
- believable battle stance.

It must not be copied literally. Dragonfall needs an original design.

### Recommended content budgets

| Asset | LOD0 target | LOD1 target | LOD2 target |
|---|---:|---:|---:|
| Equipped principal hero | 10,000–15,000 triangles | 5,000–8,000 | 2,000–4,000 |
| Normal enemy | 6,000–10,000 | 3,000–5,000 | 1,200–2,500 |
| Major boss | 20,000–35,000 | 10,000–18,000 | 4,000–8,000 |
| Weapon or shield | 750–2,500 each | approximately 50% | silhouette proxy |

Additional targets:

- one to three opaque material submissions per hero where practical;
- 2K base-color, normal, and packed ORM textures for high quality;
- 1K derivatives for medium/low quality;
- 55 to 80 deformation bones for a humanoid;
- a hard bounded maximum of 128 bones for the Generation 2 path;
- four bone influences per vertex;
- 15 to 25 useful animation clips for a principal actor;
- no impact-time asset load or allocation.

## Strategy

The work is divided into four reusable engine workstreams and one game proof.

### Workstream A — Asset pipeline

Add SM3D version 2 as a backward-compatible, deterministic offline package capable of carrying:

- tangent-space geometry;
- PBR material metadata and texture references;
- full skeleton hierarchy and inverse bind matrices;
- sampled named clips;
- animation events;
- named sockets;
- optional future extension chunks.

Add GLB input support to `smileasset.exe`. Continue accepting current glTF input and continue loading SM3D v1.

### Workstream B — Rendering

Extend the custom Direct3D 11 and WebGL2 backends with:

- tangent-space normal mapping;
- metallic/roughness/occlusion/emissive material channels;
- sRGB/linear texture semantics;
- mipmaps and anisotropic filtering;
- bounded ambient, directional, point, and spot lighting;
- one bounded shadow path;
- optional HDR scene targets;
- tone mapping and bloom;
- quality profiles and capability fallbacks.

### Workstream C — Animation

Extend the bounded animation system with:

- up to 128 bones in the Generation 2 path;
- arbitrary sampled clip frames;
- smooth cross-fades;
- exact-once animation events;
- root-motion extraction and application policy;
- named sockets;
- optional overlay, look-at, and two-bone IK after the core path is stable.

Battle mechanics remain authoritative. Visual animation never decides damage.

### Workstream D — Effects

Replace per-particle `Object3D` rendering with bounded dynamic batches supporting:

- camera-facing quads;
- flipbook atlas frames;
- additive and alpha particles;
- ribbons and weapon trails;
- mesh/fragment instances where practical;
- multi-emitter compositions;
- light, screen-flash, and camera-shake impulses;
- deterministic fixed-step simulation.

### Proof — Dragonfall remaster vertical slice

Use one original, production-quality, rigged hero in Dragonfall and prove:

- PBR armor/cloth/skin differentiation;
- smooth idle, locomotion, attack, defend, hit, KO, and victory motion;
- sword and shield sockets;
- layered attack VFX;
- cinematic camera compatibility;
- existing ATB mechanics and HUD unchanged;
- native and Web builds;
- explicit low-quality fallback.

## Milestone plan

### M0 — Reconciliation and baseline

- Reconcile every plan assumption against current `main`.
- Capture current native/Web test results and resource counts.
- Add missing diagnostic counters needed for later measurement only when narrowly reusable.
- Generate a deterministic skinned GLB fixture.
- Commit the final implementation mapping.

### M1 — SM3D v2 core

- Add GLB parsing.
- Add versioned v2 chunk/container support.
- Add tangents and PBR material metadata.
- Preserve v1 conversion and loading.
- Prove deterministic byte-identical conversion.

### M2 — PBR-lite renderer

- Load material texture channels.
- Add mipmapping, linear/sRGB semantics, normal mapping, and GGX-style direct lighting.
- Add bounded light state.
- Preserve the old simple material shader path.

### M3 — Animation Generation 2

- Import skins, skeletons, inverse bind matrices, clips, events, and sockets.
- Increase the bounded palette to 128 bones through backend-appropriate storage.
- Add cross-fade playback and root-motion support.
- Prove native/Web state parity.

### M4 — Beginner API and Character Lab

- Add high-level `Character3D` and `Scene3D` modules.
- Cache shared character assets while keeping instances independent.
- Add a small native/Web Character Lab using the generated fixture.
- Keep advanced low-level APIs available but unnecessary for beginners.

### M5 — Shadows and post-processing

- Add one directional or spotlight shadow map.
- Add HDR-capable scene rendering with safe fallback.
- Add tone mapping and bounded bloom.
- Add `LOW`, `MEDIUM`, `HIGH`, and `AUTO` profiles.

### M6 — VFX Generation 2

- Add dynamic sprite and ribbon batches.
- Add flipbooks and multi-emitter effects.
- Add `Effects3D` high-level presets.
- Rebuild representative slash, impact, fire, frost, heal, and breath effects without one object per particle.

### M7 — Dragonfall vertical slice

- Integrate one approved hero.
- Replace that hero's rigid rig while preserving the other current actors.
- Add modern materials, animations, VFX, lighting, and camera staging.
- Ship crowd-demo and no-demo versions.
- Demonstrate graceful fallback.

### M8 — Full remaster

- Convert the remaining heroes and enemies only after M7 is visually and technically accepted.
- Retain a Classic path until the remaster is stable.
- Make the remaster default through a deliberate final decision, not automatically.

## Scope exclusions

The initial project does not include:

- a general 3D scene editor;
- a full material node editor;
- student-authored shaders;
- rigid-body physics;
- full cloth simulation;
- motion matching;
- ray tracing;
- required WebGPU;
- a general ECS rewrite;
- runtime glTF/GLB parsing;
- automatic reconstruction of production characters from one image;
- conversion of all Dragonfall content before one hero is proven.

## Final definition of done

The complete program is done when:

1. All M0–M7 acceptance gates pass.
2. Existing SM3D v1, Renderer2D, GDI, Simple3D, Space Wars, Battle3D, BattleTime, and Dragonfall mechanics gates remain green.
3. Native and Web builds visibly show the same actor, material, animation, event, and VFX semantics.
4. The vertical-slice hero meets the agreed content budget and has documented provenance.
5. The scene runs without unbounded allocation growth or resource leaks.
6. The beginner example uses named high-level calls and contains no backend concepts.
7. API and architecture documentation match the shipped code.
8. M8 remains optional until the user approves the M7 visual result.
