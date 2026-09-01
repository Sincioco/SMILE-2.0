# Milestone Task Backlog

## Usage

Codex should execute one milestone per run. Task IDs are planning identifiers, not required issue numbers.

A task is complete only when implementation, focused tests, documentation, and cleanup are all present.

## M0 — Reconciliation and baseline

### VG2-M0-001 — Confirm repository state

- Record branch, HEAD, upstream, and working tree.
- Read root `AGENTS.md`.
- Preserve unrelated changes.
- Compare actual HEAD with the prepared baseline.

**Acceptance:** implementation note records exact starting state.

### VG2-M0-002 — Map current Renderer3D command ABI

Inspect:

- `Graphics3D.smile` command constants;
- native dispatch;
- text/image bridges;
- Web dispatch;
- reset and diagnostics.

**Acceptance:** note lists exact files and next available safe command ranges without renumbering existing commands.

### VG2-M0-003 — Map current resource ownership

Document current ownership for:

- mesh/object;
- texture/material;
- model/part;
- skeleton/clip/animator;
- Dragonfall scene.

**Acceptance:** planned Generation 2 ownership does not conflict.

### VG2-M0-004 — Run baseline gates

Run all relevant existing renderer, Battle3D, Dragonfall, Simple3D, native, and Web gates.

**Acceptance:** green baseline or documented pre-existing failure with reproduction.

### VG2-M0-005 — Add minimal metrics needed by later work

Only if absent, add narrowly reusable counters for draw calls and triangles.

**Acceptance:** counters reset predictably and do not alter rendering.

### VG2-M0-006 — Create deterministic GLB fixture generator

Generate a tiny original GLB fixture through a repository script or test helper.

Initial M0 fixture may be static; M3 extends it with skin/animation.

**Acceptance:** fixture bytes and metadata are deterministic.

### VG2-M0-007 — Commit reconciliation note

Preferred path:

```text
docs/implementation/renderer3d-visual-generation-2-reconciliation.md
```

**Acceptance:** note identifies any plan deviations and final exact M1 scope.

---

## M1 — SM3D v2 core and GLB import

### VG2-M1-001 — Define final SM3D v2 byte format

- Choose header and chunk layout.
- Document required/optional chunk policy.
- Define safety maxima.
- Define checksum coverage.

**Depends on:** M0.

### VG2-M1-002 — Refactor AssetTool only as needed

If `Program.cs` becomes unsafe to extend, split parsing/writing into a small number of focused files.

Do not build a general framework.

### VG2-M1-003 — Add GLB container reader

- Validate magic/version/length/chunks/alignment.
- Feed existing accessor logic where possible.
- Retain `.gltf`.

### VG2-M1-004 — Add tangent import/generation

- Read `TANGENT`.
- Generate deterministically when absent.
- Validate UV degeneracy and finite values.

### VG2-M1-005 — Add PBR material metadata conversion

- base-color;
- normal;
- ORM;
- emissive;
- factors;
- alpha/double-sided;
- safe texture paths.

### VG2-M1-006 — Write SM3D v2 core chunks

- strings;
- parts;
- vertices;
- indices;
- materials;
- texture references;
- bounds.

### VG2-M1-007 — Add native v2 loader

- full validation before allocation;
- atomic rollback;
- expose parts/material metadata.

### VG2-M1-008 — Add Web v2 loader

Match native validation semantics.

### VG2-M1-009 — Add `smileasset inspect`

Output deterministic semantic summary.

### VG2-M1-010 — Add v1/v2 focused tests

Include valid/invalid GLB, checksum, chunks, paths, determinism, load/unload.

### VG2-M1-011 — Update model-format documentation

Document exact layout and CLI.

**M1 gate:** v1 and v2 static assets draw on native/Web.

---

## M2 — PBR-lite materials and lighting

### VG2-M2-001 — Extend texture semantics

- color/data classification;
- mip generation;
- trilinear/anisotropic modes;
- capability clamping.

### VG2-M2-002 — Add PBR material resources

Add required map handles and factors with dependency-safe ownership.

### VG2-M2-003 — Extend vertex input

Add tangent data to native/Web input layouts without breaking v1/simple meshes.

### VG2-M2-004 — Implement matched PBR-lite shaders

- HLSL;
- GLSL;
- shared documented equations;
- defaults for missing maps.

### VG2-M2-005 — Add bounded light state

- ambient/hemisphere;
- one directional;
- fixed point/spot lights;
- deterministic selection.

### VG2-M2-006 — Add low-level Graphics3D operations

Append commands; do not renumber.

### VG2-M2-007 — Auto-create materials from SM3D v2

Resolve texture references through current asset manifest and cache.

### VG2-M2-008 — Add PBR Lab fixture/example

Prove roughness, metallic, normal, emissive, and moving light.

### VG2-M2-009 — Add focused native/Web tests

Include old material regression and full teardown.

### VG2-M2-010 — Update API and architecture docs

**M2 gate:** stable PBR material rendering in existing direct scene path.

---

## M3 — Animation Generation 2

### VG2-M3-001 — Extend generated fixture

Create original 68-bone skinned fixture with named clips, event, and socket.

### VG2-M3-002 — Add skin/skeleton conversion

- JOINTS_0/WEIGHTS_0;
- hierarchy;
- bind TRS;
- inverse bind matrices;
- parent ordering;
- limits.

### VG2-M3-003 — Add sampled clip conversion

- fixed sample rate;
- names;
- durations;
- local TRS samples;
- descriptor loop metadata.

### VG2-M3-004 — Add events and sockets

Read validated descriptor data and write v2 chunks.

### VG2-M3-005 — Add Generation 2 runtime resources

Load atomically and expose lookup.

### VG2-M3-006 — Add 128-bone native palette transport

Use bounded backend-appropriate storage.

### VG2-M3-007 — Add 128-bone Web palette transport

Use a validated WebGL2 path such as a float bone texture.

### VG2-M3-008 — Add sampled playback

No per-frame allocation; deterministic time progression.

### VG2-M3-009 — Add cross-fade

One base-layer source/destination blend.

### VG2-M3-010 — Add root-motion extraction

Support in-place and extracted modes.

### VG2-M3-011 — Add socket transforms

Expose world transforms after animation.

### VG2-M3-012 — Add exact-once named events

Preserve old integer-event API.

### VG2-M3-013 — Add focused tests

Boundary, invalid assets, playback, crossfade, root, socket, native/Web parity.

### VG2-M3-014 — Update animation documentation

**M3 gate:** imported 68-bone actor plays five clips on native/Web.

---

## M4 — Character3D, Scene3D, and Character Lab

### VG2-M4-001 — Define final high-level records

Choose minimal actor/asset/cache ownership.

### VG2-M4-002 — Add `Character3D.smile`

- load/cache;
- create/destroy;
- place/rotate/scale;
- play/crossfade;
- update/draw;
- event/socket;
- root-motion policy.

### VG2-M4-003 — Add `Scene3D.smile`

- quality placeholder;
- lighting presets;
- begin/end wrapper;
- diagnostics.

### VG2-M4-004 — Update Simple3D project/package metadata

Use repository versioning rules.

### VG2-M4-005 — Add Character Lab

Native and Web example with controls for clips, light, material debug, sockets, and diagnostics.

### VG2-M4-006 — Add beginner diagnostics

Readable missing clip/socket/bone-limit errors.

### VG2-M4-007 — Add focused tests and tutorials

### VG2-M4-008 — Run Simple3D/Space Wars regression

**M4 gate:** a beginner can animate the fixture through names only.

---

## M5 — Shadows, HDR, tone mapping, bloom, quality

### VG2-M5-001 — Add quality capability probe

No GPU-name heuristics.

### VG2-M5-002 — Add one shadow pass

- target creation;
- selected caster;
- PCF;
- bias;
- quality resolution;
- fallback.

### VG2-M5-003 — Add optional HDR scene target

- native float target;
- Web float target when supported;
- LDR fallback;
- resize/device-loss lifecycle.

### VG2-M5-004 — Add tone mapping

Matched HLSL/GLSL equation.

### VG2-M5-005 — Add bounded bloom

Half/quarter resolution, fixed pass count, no HUD bloom.

### VG2-M5-006 — Complete quality profiles

`AUTO`, `LOW`, `MEDIUM`, `HIGH`.

### VG2-M5-007 — Add profile/fallback diagnostics

### VG2-M5-008 — Add post-processing tests

### VG2-M5-009 — Verify Renderer2D composition

**M5 gate:** PBR Lab has shadow and bloom with clean fallback.

---

## M6 — VFX Generation 2

### VG2-M6-001 — Add particle-batch resource

Fixed dynamic instance buffer and lifecycle.

### VG2-M6-002 — Add ribbon-batch resource

Fixed dynamic ribbon/segment buffer and lifecycle.

### VG2-M6-003 — Add native rendering

Instanced or otherwise batched Direct3D 11 path.

### VG2-M6-004 — Add Web rendering

WebGL2 instanced/dynamic buffer path.

### VG2-M6-005 — Add deterministic Generation 2 simulation module

Fixed pools and seeded update.

### VG2-M6-006 — Add flipbooks

Atlas metadata and frame selection.

### VG2-M6-007 — Add multi-emitter compositions

Delayed layers, attachments, impulses.

### VG2-M6-008 — Add `Effects3D.smile`

Beginner-facing named effect calls.

### VG2-M6-009 — Rebuild representative effects

- sword slash;
- impact;
- fire;
- frost;
- heal;
- dragon breath.

### VG2-M6-010 — Add VFX Lab and tests

### VG2-M6-011 — Prove object count independence

Particle count must not create one object per particle.

**M6 gate:** sword ribbon plus 1,024-particle proof on native/Web.

---

## M7 — Dragonfall vertical slice

### VG2-M7-001 — Validate production hero

Run asset checklist and converter inspect.

### VG2-M7-002 — Add asset declarations and provenance

No undeclared or unlicensed files.

### VG2-M7-003 — Add actor visual seam

Support rigid and Character3D actor visuals together.

### VG2-M7-004 — Replace Arin visual only

Retain mechanics and actor ID.

### VG2-M7-005 — Map cues to clips/events

Attack, special, defend, hit, KO, victory.

### VG2-M7-006 — Map anchors to sockets

Sword, shield, head, chest.

### VG2-M7-007 — Add Ember Observatory lighting profile

### VG2-M7-008 — Add Generation 2 Arin effects

### VG2-M7-009 — Tune camera framing from bounds

### VG2-M7-010 — Add load failure fallback

Rigid Arin or explicit 2D path.

### VG2-M7-011 — Preserve crowd/no-demo start paths

### VG2-M7-012 — Add focused visual-generation gate

### VG2-M7-013 — Run full adjacent regressions

### VG2-M7-014 — Capture visual/performance evidence

**M7 gate:** approved one-hero remaster slice on native/Web.

---

## M8 — Full Dragonfall remaster

### VG2-M8-001 — User visual acceptance decision

Do not begin mass conversion before approval.

### VG2-M8-002 — Convert remaining heroes one at a time

Each retains focused fallback and tests.

### VG2-M8-003 — Convert Wave 1 enemies

Share assets/animations where sensible.

### VG2-M8-004 — Convert Ashwing

Use a dedicated creature skeleton/animation budget.

### VG2-M8-005 — Tune scene-wide budgets and LODs

### VG2-M8-006 — Remove only proven-obsolete duplicate visual code

### VG2-M8-007 — Decide Classic retention

### VG2-M8-008 — Make remaster default in an isolated commit

### VG2-M8-009 — Final docs, artifacts, and smoke gate

**M8 gate:** complete remaster release after explicit approval.
