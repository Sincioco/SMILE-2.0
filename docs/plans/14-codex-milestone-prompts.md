# Codex Milestone Prompts

## How to use

Run one prompt at a time. Allow Codex to complete, validate, commit, push, and report before starting the next prompt.

Every prompt assumes the plan folder is at:

```text
docs\plans\smile-2.0-dragonfall-visual-generation-2-plan
```

If the folder is elsewhere, update the path.

---

## M0 prompt — start here

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, then read every file under:
docs\plans\smile-2.0-dragonfall-visual-generation-2-plan
in the order listed by read-me-first.md.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M0 only, using the M0 tasks in 11-milestone-task-backlog.md.

Required outcomes:

1. Reconcile the plan against the actual current branch and HEAD without resetting or discarding unrelated work.
2. Record the exact Renderer3D numeric/image/text command ABI and all relevant native/Web dispatch paths.
3. Record current resource limits and ownership for meshes, objects, models, textures, materials, skeletons, clips, animators, and Dragonfall.
4. Run the current focused Renderer3D, Battle3D, Dragonfall, and Simple3D baseline gates.
5. Add only the smallest reusable draw-call/triangle diagnostics if those metrics are not already available.
6. Add a deterministic repository-owned GLB fixture generator or fixture suitable for M1 converter tests.
7. Write and commit:
   docs\implementation\renderer3d-visual-generation-2-reconciliation.md
   or the nearest repository-conforming equivalent.
8. Update the plan mapping in that note when current code differs from the handoff.
9. Do not implement SM3D v2, PBR, new animation, or VFX in this run.
10. Commit and push the completed M0 work.

Begin with the required **Flag:** notice for any missing capability encountered.

Report starting/ending commit, changed files, commands, exact test results, plan deviations, and whether M1 is unblocked.
```

---

## M1 prompt — SM3D v2 core

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md and the Visual Generation 2 plan. Review the committed M0 reconciliation note and use its exact paths/limits.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M1 only.

Deliver a backward-compatible SM3D version 2 core and offline GLB import:

- keep SM3D v1 conversion/loading green;
- define and document the exact deterministic v2 header/chunk layout;
- add strict GLB container parsing to smileasset.exe;
- import or deterministically generate tangents;
- convert PBR material metadata and safe external texture references;
- write/load v2 strings, parts, vertices, indices, materials, texture references, and bounds;
- validate the complete file before allocation;
- roll back atomically on load failure;
- add deterministic inspect output;
- implement matched native/Web v2 loading;
- draw a v2 static fixture on Direct3D 11 and WebGL2;
- add focused valid/invalid/determinism/lifecycle tests;
- update architecture/API documentation.

Do not implement skinned clips, PBR shading, shadows, post-processing, or VFX yet.

Preserve current command values and append only safe new commands. Do not add runtime glTF/GLB parsing or third-party dependencies.

Run focused tests, adjacent model/lifecycle regressions, native/Web builds, formatting checks, then commit and push.

Report exact v2 format decisions, limits, commands, tests, commit, and M2/M3 prerequisites.
```

---

## M2 prompt — PBR-lite renderer

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, the Visual Generation 2 plan, and the completed M0/M1 implementation notes.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M2 only.

Add a bounded PBR-lite material and direct-lighting path to the existing custom Direct3D 11/WebGL2 Renderer3D:

- preserve the current simple material path;
- add tangent vertex input;
- add base-color, normal, packed ORM, and emissive texture semantics;
- add sRGB versus linear texture handling;
- add mipmaps and bounded filtering/anisotropy;
- add metallic, roughness, normal-strength, occlusion, emissive, alpha, and double-sided factors;
- add one ambient/hemisphere contribution, one directional light, and a small fixed point/spot light set;
- implement matched documented HLSL/GLSL PBR-lite equations;
- auto-create required materials for SM3D v2 assets through the existing asset manifest/cache;
- add PBR diagnostics and a small PBR Lab fixture/example;
- add native/Web semantic and lifecycle tests;
- update Simple3D API and architecture docs.

Do not add shadows, HDR, bloom, WebGPU, IBL, material graphs, or full character animation in this run.

Run focused tests, old material/model/lifecycle regressions, native/Web builds, visible manual PBR checks, formatting checks, then commit and push.

Report actual material behavior, fallback behavior, draw counts, tests, and commit.
```

---

## M3 prompt — Animation Generation 2

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, the Visual Generation 2 plan, and all completed implementation notes.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M3 only.

Extend SM3D v2 and Renderer3D with production skeletal animation:

- keep current 32-bone/two-key APIs source compatible and tested;
- extend the deterministic fixture to an original skinned humanoid with more than 32 bones;
- convert JOINTS_0, WEIGHTS_0, hierarchy, bind TRS, and inverse bind matrices;
- import named clips through a documented fixed sample rate;
- import descriptor events, sockets, loop policy, and optional root-motion metadata;
- support a bounded hard maximum of 128 bones;
- implement an appropriate native palette buffer and a validated WebGL2 palette path;
- add sampled playback, one base-layer cross-fade, loop/once/hold, exact-once events, root-motion extraction, and socket transforms;
- keep all update storage preallocated;
- add native/Web state parity, invalid asset, boundary, lifecycle, and visible animation tests;
- update format, API, and animation architecture docs.

Do not add a general animation graph, motion matching, retargeting, morph targets, full IK, spring bones, or Dragonfall conversion in this run.

Run focused tests, current animation/model/material/lifecycle regressions, native/Web builds, visible manual animation checks, formatting checks, then commit and push.

Report bone/clip/socket/event counts, palette implementation, tests, and commit.
```

---

## M4 prompt — beginner Character3D and Character Lab

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, the Visual Generation 2 plan, and completed M0-M3 notes.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M4 only.

Add the smallest beginner-facing high-level modules over the completed reusable renderer:

- Character3D for cached load, actor creation/destruction, transforms, named play/crossfade, update/draw, events, sockets, and root-motion policy;
- Scene3D for simple lighting presets and quality selection placeholders;
- readable errors for missing assets, clips, sockets, and unsupported limits;
- a complete native/Web Character Lab using the deterministic original fixture;
- controls that demonstrate Idle, Walk/Run, Attack, Hit, Victory, cross-fade, a socket marker, and PBR light/material behavior;
- tutorials and updated API/package documentation;
- focused ownership/cache/error tests;
- existing Simple3D and Space Wars regression.

Do not add new language grammar. Do not expose GPU/backend concepts to the beginner example. Do not implement shadows/post-processing/VFX or modify Dragonfall yet.

Run focused tests, adjacent regressions, native/Web builds, visible Character Lab checks, formatting, then commit and push.

Report the final beginner API, example controls, tests, and commit.
```

---

## M5 prompt — shadows and post-processing

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, the Visual Generation 2 plan, and completed implementation notes.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M5 only.

Add bounded shadows and post-processing to the existing Renderer3D:

- one selected directional or spot shadow caster;
- quality-sized shadow map with small PCF and documented bias;
- optional HDR scene target on native and capable WebGL2;
- explicit LDR fallback;
- one documented matched tone-mapping operator;
- bounded half/quarter-resolution bloom;
- LOW, MEDIUM, HIGH, and AUTO profiles selected by capability tests and safe allocation, not GPU-name blacklists;
- diagnostics for effective profile, shadow/HDR/bloom status, draw/pass counts, and fallback reason;
- resize, device/context-loss, reset, and teardown integration;
- Character Lab/PBR Lab visual proof;
- focused native/Web/fallback/lifecycle tests.

Renderer2D must remain the final crisp HUD layer. Do not bloom or tone-map it.

Do not add WebGPU, point-light cube shadows, SSAO, motion blur, depth of field, or a general render graph.

Run focused tests, Renderer2D composition and adjacent regressions, native/Web builds, visible profile/fallback checks, formatting, then commit and push.

Report capability results, target formats, shadow/bloom settings, tests, measurements, and commit.
```

---

## M6 prompt — VFX Generation 2

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, the Visual Generation 2 plan, and completed implementation notes.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M6 only.

Add reusable bounded VFX Generation 2:

- dedicated particle-batch and ribbon-batch resources;
- native Direct3D 11 and WebGL2 batched rendering;
- deterministic fixed-pool simulation in reusable SMILE code;
- billboard modes, color/opacity/size progression, gravity/drag, rotation, and seeded spread;
- PNG flipbook atlas support;
- socket-attached ribbons and emitters;
- bounded multi-emitter compositions with delayed layers;
- requests for camera shake, screen flash, light impulse, and audio event without owning those systems;
- beginner-facing Effects3D calls;
- representative sword slash, impact, fire, frost, heal, and dragon-breath presets;
- VFX Lab and focused native/Web/determinism/capacity/lifecycle tests;
- proof that particle count does not consume one Object3D per particle.

Keep current Generation 1 effects functional. Do not add GPU compute simulation, a VFX editor, a node graph, or required distortion/soft particles.

Run focused tests, adjacent Renderer3D/Battle3D/Dragonfall regressions, native/Web builds, visible VFX checks, performance/resource measurements, formatting, then commit and push.

Report pool capacities, batch counts, object-count proof, tests, measurements, and commit.
```

---

## M7 prompt — Dragonfall one-hero vertical slice

```text
Work in D:\SMILE 2.0.

Read root AGENTS.md, the complete Visual Generation 2 plan, completed implementation notes, and the production asset provenance/checklist.

Use exactly one Codex agent. Do not create subagents.

Implement Milestone M7 only.

Prerequisite: an approved original/licensed Arin/paladin GLB and texture set satisfying 13-production-character-asset-handoff.md. If that asset is absent or fails rights/quality validation, complete only the integration seam and report M7 blocked; do not mislabel a test fixture as final art.

When the asset is valid:

- convert and inspect it through smileasset.exe;
- declare and document all assets/provenance;
- add the smallest Dragonfall scene seam allowing rigid and Character3D visuals to coexist;
- replace Arin's visual only while preserving his battle participant, mechanics, commands, HUD, and audio ownership;
- map Battle3D cues to Idle, approach, attack, special, defend, hit, KO, and victory clips;
- map effect anchors to SwordBase, SwordTip, ShieldCenter, Head, and Chest sockets;
- add the Ember Observatory lighting profile, floor shadow, tone mapping, and controlled bloom;
- add Generation 2 sword ribbon, holy impact, shield impact, sparks, light, flash, and camera impulses;
- preserve Program.smile and Program-NoDemo.smile;
- provide an explicit load/capability fallback to rigid Arin or the current 2D path;
- add focused native/Web integration/lifecycle tests;
- run existing Dragonfall mechanics and balance expectations unchanged;
- capture fixed-camera visual evidence and actual performance/resource diagnostics.

Do not convert the remaining cast in this run. Do not delete Classic visual code.

Run all focused/adjacent gates, native/Web builds, crowd/no-demo manual checks, formatting, then commit and push.

Report asset statistics/provenance, clip/socket/event list, screenshots or precise observations, draw/triangle/resource counts, frame-rate observations, tests, fallback proof, and commit.
```

---

## M8 prompt — full remaster after approval

```text
Work in D:\SMILE 2.0.

Do not begin unless the user has explicitly approved the M7 visual result.

Read root AGENTS.md, the Visual Generation 2 plan, completed implementation notes, and the user's M7 acceptance feedback.

Use exactly one Codex agent. Do not create subagents.

Implement the specifically approved M8 conversion slice only. Do not convert every remaining actor in one commit.

For the named actor/group:

- validate original/licensed asset provenance;
- convert and inspect the asset;
- integrate through existing Character3D/Dragonfall seams;
- map clips, events, sockets, materials, lights, VFX, and fallback;
- preserve mechanics;
- add focused tests and visible evidence;
- measure scene-wide budgets;
- remove obsolete code only when no supported path depends on it;
- commit and push.

At the end, report whether the next actor/group is safe to convert and whether Classic should still be retained.
```
