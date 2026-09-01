# Risk Register and Design Decisions

## Locked decisions

### DD-001 — SMILE 2.0 remains the product

Do not create a separate SMILE 3.0 codebase for this work.

### DD-002 — Custom renderer remains

Extend current Direct3D 11 and WebGL2 Renderer3D. Do not replace it with a third-party engine.

### DD-003 — Renderer2D remains permanent

3D renders first. HUD, text, menus, and accessibility overlays remain in Renderer2D after 3D/post-processing.

### DD-004 — Offline asset conversion remains

`smileasset.exe` converts glTF/GLB to SM3D. Games do not parse glTF/GLB.

### DD-005 — SM3D v1 remains supported

Version 2 is additive.

### DD-006 — WebGL2 remains required browser baseline

WebGPU is a future optional backend, not an M0–M7 dependency.

### DD-007 — Browser code remains pure JavaScript

No TypeScript, framework, npm, or CDN runtime dependency.

### DD-008 — PBR-lite, not a full material graph

Use a bounded metallic-roughness path.

### DD-009 — CPU-deterministic VFX simulation, batched GPU rendering

Do not make GPU particle simulation a first requirement.

### DD-010 — One converted hero before full cast

M7 proves one hero; M8 expands only after acceptance.

### DD-011 — Production art is a separate deliverable

Reference images guide quality. They are not runtime models.

### DD-012 — PNG texture sets first

KTX2/Basis or other compressed texture distribution is deferred until the visual path is stable. Mipmaps and quality-sized PNG variants provide the first bounded solution.

## Deferred decisions

Revisit only with evidence:

- exact LOD container format;
- morph-target facial animation;
- animation compression;
- retargeting;
- cloth/spring simulation;
- soft particles;
- distortion;
- decals;
- environment-map IBL;
- WebGPU backend;
- KTX2 texture packaging;
- full character editor.

## Risk register

| ID | Risk | Probability | Impact | Mitigation | Gate |
|---|---|---:|---:|---|---|
| R-001 | Big-bang renderer rewrite breaks stable games | Medium | Critical | One capability per milestone; preserve old paths | Every milestone |
| R-002 | SM3D v2 becomes overdesigned | Medium | High | Core chunks first; defer morph/LOD/compression | M1 |
| R-003 | AssetTool single-file growth becomes unsafe | High | Medium | Small focused refactor only when needed | M1 |
| R-004 | WebGL2 bone uniform limit blocks 80-bone actors | High | High | Bone texture or validated alternative | M3 |
| R-005 | Native/Web shader math diverges | Medium | High | Document matched equations and semantic tests | M2/M5 |
| R-006 | HDR extension unavailable on some browsers | Medium | Medium | Explicit LDR fallback | M5 |
| R-007 | Shadow acne/peter-panning damages quality | High | Medium | One controlled light, bias controls, stable test scene | M5 |
| R-008 | Bloom washes out HUD | Medium | High | Apply before Renderer2D composition | M5 |
| R-009 | Particles overwhelm object pool | Certain with old design | High | Dedicated batches; no object per particle | M6 |
| R-010 | Animation events change battle mechanics | Medium | Critical | Mechanics authorize outcomes; events align presentation only | M3/M7 |
| R-011 | Root motion causes gameplay drift | Medium | High | In-place default; extracted delta is presentation policy | M3 |
| R-012 | Production knight is unavailable or poor quality | High | High | Engine fixtures first; strict asset handoff gate | M7 |
| R-013 | Unlicensed model enters repository | Medium | Critical | Provenance file required; no silent downloads | M7 |
| R-014 | 2K textures inflate Web download/memory | High | Medium | 1K medium/low variants; preload reporting; compression deferred | M7 |
| R-015 | Full cast conversion blocks core engine | High | High | One-hero slice before M8 | M7 |
| R-016 | Current rigid Dragonfall code is too coupled | High | Medium | Small actor-visual seam; avoid scene duplication | M7 |
| R-017 | New commands break ABI by renumbering | Medium | Critical | Append commands; baseline map in M0 | M0 onward |
| R-018 | Asset load failure leaks partial resources | Medium | High | Validate first; atomic allocation/rollback tests | M1/M3 |
| R-019 | Per-frame JS allocations cause browser stutter | High | High | Preallocated typed arrays and fixed pools | M2–M6 |
| R-020 | Quality profiles become hardware blacklist | Medium | Medium | Capability probes and safe creation only | M5 |
| R-021 | Overly ambitious IK delays the slice | Medium | Medium | Crossfade/sockets first; IK after core proof | M3/M7 |
| R-022 | Visual gains are attributed only to polygon count | Medium | Medium | Acceptance covers materials, light, motion, VFX | M7 |
| R-023 | Tests become large and slow | Medium | Medium | Tiny fixtures; focused gates; broader smoke at release | Every milestone |
| R-024 | Current Simple3D lessons become confusing | Low | Medium | High-level modules additive; retain old tutorials | M4 |
| R-025 | Draw-call count remains high despite better models | Medium | High | Limit material parts; report submissions | M7 |
| R-026 | Character topology deforms badly | Medium | High | Asset checklist and deformation review | M7 |
| R-027 | Browser float texture/palette capability fails | Low/Medium | High | Validate vertex texture access; explicit unsupported diagnostic | M3 |
| R-028 | Device loss/resize leaks post targets | Medium | High | Integrate current lifecycle and focused tests | M5 |
| R-029 | Generated fixture is accidentally treated as final art | Medium | Medium | Label Character Lab fixture clearly | M4 |
| R-030 | Codex attempts all milestones in one run | High | High | Dedicated one-milestone prompts and commits | Process |

## Escalation/stop conditions

Codex should stop the selected milestone and report before proceeding when:

- current architecture contradicts a locked decision;
- an existing relevant baseline gate is failing and cause is unknown;
- a third-party dependency appears necessary;
- a public API break appears unavoidable;
- the production asset has uncertain rights;
- the selected milestone would require implementing a deferred feature;
- Web parity cannot be achieved without a deliberate fallback;
- working-tree overlap risks destroying unrelated changes.

Stopping here means reporting a concrete blocker, not abandoning the overall project.

## Change-control questions

Before adding a new abstraction, Codex should answer:

1. Does an existing Renderer3D, Simple3D, Battle3D, or asset layer already own this responsibility?
2. Is the abstraction needed for the selected milestone?
3. Does it reduce repeated native/Web logic or merely add indirection?
4. Can it remain fixed-capacity and diagnosable?
5. Does it make beginner-facing code simpler?
6. Can it be removed without breaking the core language?

If the answers are weak, use the existing layer.

## Visual-quality decision gate after M7

The user should review:

- fixed-camera native captures;
- equivalent Web captures;
- motion clip;
- measured diagnostics;
- fallback behavior.

Only then decide:

- whether the quality target is met;
- whether to raise/lower triangle or texture budgets;
- whether IK/morphs are required before M8;
- whether Classic remains;
- whether the rest of the cast should be converted.
