# Testing, Performance, and Acceptance

## Testing philosophy

Use focused, deterministic evidence for each capability. Do not add an enormous speculative test framework.

Every milestone must:

- add a narrow gate for its new reusable capability;
- run adjacent existing gates;
- build affected native/Web targets;
- perform a visible manual check when graphics change;
- report actual commands and results;
- avoid claiming unmeasured performance.

## Proposed new focused scripts

Names may be adjusted to repository conventions.

```text
scripts/test-renderer3d-sm3d-v2.ps1
scripts/test-renderer3d-pbr.ps1
scripts/test-renderer3d-animation-v2.ps1
scripts/test-renderer3d-post-processing.ps1
scripts/test-renderer3d-vfx-v2.ps1
scripts/test-character3d.ps1
scripts/test-dragonfall-visual-generation-2.ps1
```

Do not duplicate existing tests when extending the current script is clearer.

## Required existing regressions

At relevant milestones, continue running:

```powershell
.\scripts\test-renderer3d-lifecycle.ps1
.\scripts\test-renderer3d-materials.ps1
.\scripts\test-renderer3d-models.ps1
.\scripts\test-renderer3d-animation.ps1
.\scripts\test-battle3d.ps1
.\scripts\test-dragonfall.ps1
.\scripts\test-simple3d-space-wars.ps1
```

At release points:

```powershell
cmd /c scripts\build.cmd
cmd /c scripts\smoke-test.cmd
```

Codex must use the exact current commands after reconciliation.

## Fixture policy

Use repository-owned deterministic fixtures.

Required fixtures:

1. Static PBR test mesh:
   - tangent data;
   - four material textures;
   - known triangle count.
2. Skinned humanoid:
   - more than 32 bones;
   - no copyrighted design;
   - five named clips;
   - one socket;
   - one event.
3. Maximum-boundary fixture:
   - 128 bones;
   - capacity tests.
4. Invalid assets:
   - bad checksum;
   - invalid chunk;
   - bad joint;
   - bad descriptor;
   - bad GLB chunk.
5. VFX atlas:
   - small original generated flipbook;
   - known frame grid.

Fixtures should be tiny enough for tests. Do not use a 2K production hero in every automated gate.

## Semantic parity

Native/Web automated comparison should include:

- converted model inspection;
- material parameters;
- effective texture semantics;
- skeleton/bone count;
- clip names and durations;
- animation event sequence;
- animator times;
- root-motion deltas;
- socket positions at known times;
- particle states for known seeds;
- quality fallback flags;
- resource counts before/after teardown.

Pixel hashes are not required.

## Visual tests

For visual milestones, use stable scenes and fixed cameras.

### PBR scene

Include:

- rough dielectric;
- polished metal;
- rough metal;
- normal-mapped surface;
- emissive surface;
- one moving or selectable light.

### Animation scene

Include:

- bind pose;
- idle;
- walk;
- attack;
- cross-fade;
- socket marker;
- skeleton debug toggle if already consistent with project style.

### VFX scene

Include:

- additive impact;
- alpha smoke;
- flipbook;
- sword ribbon;
- pool-capacity display.

### Dragonfall scene

Use fixed camera keys or a diagnostic mode to capture the six comparison moments specified in the vertical-slice plan.

## Performance measurements

Add or use diagnostics for:

- FPS and frame time;
- draw calls;
- triangles;
- live meshes;
- live objects;
- live materials/textures;
- live skeletons/clips/animators;
- live particle/ribbon batches;
- active particles;
- post-process passes;
- effect rejections;
- selected quality profile.

When feasible, distinguish:

- update CPU time;
- render CPU submission time;
- GPU time.

Do not block M7 on a sophisticated profiler. Basic counters and frame-time sampling are sufficient.

## Target budgets

These are acceptance guides, not universal hardware guarantees.

### One principal hero

- 10,000–15,000 triangles at LOD0;
- one to four draw submissions, excluding separate transparent accessories;
- 55–80 bones;
- no per-frame allocation growth;
- no asset load after scene initialization.

### Dragonfall vertical slice

Suggested visible-scene target:

- less than approximately 150 geometry/effect batch submissions in the standard battle view;
- 60 FPS on the developer's high-end Windows system with substantial headroom;
- 60 FPS at the game's current logical presentation size in a current desktop WebGL2 browser;
- a medium profile that avoids excessive GPU memory on ordinary integrated/discrete student hardware;
- no repeated frame spikes from resource creation.

Record actual results instead of merely marking these as passed.

### Effects

- medium profile: at least 1,024 batched sprite particles available;
- high profile: target 4,096 if measurement supports it;
- particle count does not create the same number of `Object3D` handles;
- one sword trail uses one bounded ribbon resource rather than one object per segment.

## Memory and lifecycle

Every new resource type needs:

- live count;
- maximum count;
- validity query;
- reference count where dependencies exist;
- explicit destroy;
- reset integration;
- stale-handle rejection;
- allocation rollback;
- device/context loss cleanup where applicable.

Lifecycle tests must verify:

- zero counts after one complete create/draw/destroy;
- repeated restart without monotonic growth;
- failure halfway through asset load leaves no residual resources;
- destroying an asset with live actors is rejected;
- destroying a texture/material with live dependencies is rejected;
- resize and renderer reset release post-process/shadow targets.

Avoid excessive default soak loops. A focused repeated loop is appropriate only when it tests a known lifecycle risk.

## Error-path acceptance

Test at least:

- missing model;
- missing texture;
- wrong case/path;
- unsupported file version;
- malformed chunk;
- too many bones;
- insufficient palette capability;
- shadow target allocation failure;
- HDR extension absence;
- VFX pool exhaustion;
- missing clip;
- missing socket;
- cross-fade to invalid clip;
- partial asset-load rollback.

Errors must be visible through `LastError`/diagnostics and must not crash.

## Formatting and documentation

For touched SMILE files:

- run current formatter/check commands;
- use Visual Basic-style capitalization;
- preserve readable blank lines;
- avoid dense one-line conditions;
- update `libraries/Smile.Simple3D/API.md`;
- update architecture docs;
- update game README when user-visible behavior changes.

## Milestone acceptance matrix

| Capability | Native test | Web test | Manual visual | Regression |
|---|---|---|---|---|
| SM3D v2 core | required | required | simple draw | v1 model gate |
| PBR | required | required | material sphere/character | simple material gate |
| Animation v2 | required | required | skinned actor | old animation gate |
| Character3D | required | required | Character Lab | Simple3D/Space Wars |
| Shadows/post | required | required/fallback | PBR scene | Renderer2D composition |
| VFX v2 | required | required | VFX Lab | Generation 1 effects |
| Dragonfall slice | required | required | six captures | Dragonfall mechanics |

## Final acceptance report

The final M7 report must include:

- branch and commit;
- production asset provenance;
- model statistics;
- clip/socket/event list;
- native and Web build outputs;
- test commands and outcomes;
- effective quality profile results;
- draw calls and triangles at the standard battle camera;
- live resource counts before/after shutdown;
- FPS/frame-time observation;
- screenshots or precise manual observations;
- known visual differences between native/Web;
- remaining work for M8.
