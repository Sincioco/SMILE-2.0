# Renderer3D Shadow and Post Hardening — M5.1

Status: complete and validated on Windows native and Web on 2026-09-01.

Starting baseline: `6f5477040b64c1ec4c745204d39ba26368f50034` on `main`, equal to `origin/main`. The pre-existing untracked `docs/plans/` tree was preserved. This report is part of the separate M5.1 commit and supersedes the implementation details of the historical M5 report where the two differ.

## Reconciliation and plan mapping

The M5 handoff already had a fixed 512-entry object queue, retained object handles until `End3D`, native and Web shadow/HDR/bloom targets, and command ranges 1-117, image 1-2, and text 1-9. Review confirmed that this was not sufficient for M6: an accepted draw read mutable object/material/animator state later, Character3D submitted parts independently, target rebuilds mutated live state while constructing replacements, fallback bits could outlive their generation, directional shadows were not texel-snapped, and the shadow pass did not consistently use captured double-sided and actual selected-light state.

M5.1 maps the handoff to the existing architecture as follows:

- the existing fixed object queue became a fixed tagged snapshot queue rather than a new render graph;
- the proposed future queue seam is represented by object kind 1, reserved particle-batch kind 2, and reserved ribbon-batch kind 3; no particle or ribbon implementation is present;
- command 118 was the actual next numeric ABI slot and carries the nonnested group protocol; image and text ABIs did not change;
- Web uses 512 preallocated snapshot objects with typed-array fields and 512 preallocated 128-matrix palettes rather than allocating records in the frame hot path;
- Character3D uses the generic group protocol directly and reports `CHARACTER_ERROR_DRAW_SUBMISSION = 18`; it did not receive a special renderer command;
- target transactions were fitted to the existing native Direct3D state bundle and WebGL capture/apply/delete helpers; a compatible last-good bundle is retained, otherwise the renderer reports the existing direct-LDR/fallback policy;
- the existing Post Lab and focused M5 test project were extended instead of adding another project. The repository formatter also corrected three in-scope SMILE files (`Character3D`, Post tests, and Post Lab) to the permanent spacing rules.

No SM3D v2 format change, new PBR model, new animation feature, VFX resource, general render graph, WebGPU path, or Dragonfall conversion was added.

## Command ABI and dispatch

The exact public renderer command ranges after M5.1 are:

- numeric `Renderer3D`: 1-118 inclusive; next free numeric command is 119;
- image `Renderer3DImage`: 1-2 inclusive; next free image command is 3;
- text `Renderer3DText`: 1-9 inclusive; next free text command is 10.

Existing command meanings 1-117, image 1-2, and text 1-9 remain unchanged. Numeric command 118 is `SUBMISSION_GROUP`:

| Operation (`a`) | Value (`b`) | Result |
| --- | ---: | --- |
| 1, begin | exact physical capacity | positive monotonically generated frame-serial token, or 0 |
| 2, commit | exact token | 1 on atomic publication, otherwise 0 |
| 3, rollback | exact token | 1 after releasing provisional snapshots/references, otherwise 0 |

Groups are deliberately nonnested. Invalid capacity, nesting, stale tokens, incomplete commits, and ending a frame with an open group use renderer error 52. Queue exhaustion remains error 51. Mutation or destruction blocked by an in-flight snapshot uses error 53.

The source path is `Smile.Simple3D.Graphics3D` -> language built-ins `Renderer3D`, `Renderer3DImage`, or `Renderer3DText`. Native compilation lowers those built-ins in `MasmEmitter` to `smile_renderer3d_command`, `smile_renderer3d_image_command`, and `smile_renderer3d_text_command`; the declarations and numeric enums live in `graphics3d.h`, and Direct3D dispatch lives in `graphics3d_directx.cpp`. Web compilation lowers through `WebEmitter` to `smile.renderer3D`, `smile.renderer3DImage`, and `smile.renderer3DText`; the generated WebGL implementation and dispatch switches live in `WebOutputWriter.cs`. Command 118 is handled in all three numeric tables and both runtime dispatchers.

## Submission architecture and ownership

Each accepted visible draw copies its source and mesh handle; position, rotation, scale, color, opacity, visibility, cast/receive flags; simple or PBR material factors; alpha mode/cutoff/double-sided state; all texture handles; and animator palette identity/revision into the frame record. An accepted draw no longer consults the original object, material, or animator during `End3D`. Destroying that object after the draw is therefore safe.

Legacy palettes copy 32 matrices and production palettes copy 128. A palette is keyed by animator handle, pose revision, and legacy/production mode and is reused by actor parts at the same revision. Advancing an animator creates a distinct snapshot. The fixed accounting contract is 512 bytes per submission snapshot and 8,208 bytes per palette snapshot.

Accepted snapshots hold one in-flight reference on their mesh and on each distinct referenced texture. Mesh commit/mutation, texture deletion, and model deletion are refused while those references exist. Model-part mesh ownership also protects the parent model. Commit, rollback, frame failure, normal end, reset, and device/context-loss cleanup release every reference and palette.

The current native and Web limits remain:

| Resource | Limit | Ownership summary |
| --- | ---: | --- |
| meshes | 128 | caller-created or model-owned; objects and frame snapshots retain references |
| objects | 512 | caller-owned handles; objects retain mesh/material/animator references, but frame rendering uses copied snapshots |
| models | 64 | own up to 16 part meshes and deduplicated imported material/texture resources |
| textures | 128 | material/model references plus distinct per-snapshot in-flight references |
| materials | 128 | caller-created or model-owned; snapshots copy all draw-time material state |
| legacy skeletons | 64 | caller-owned; clips/animators retain their established references |
| legacy clips | 128 | caller-owned; animator references remain bounded |
| animators | 128 total | shared by legacy and production model animation; frame palettes are immutable copies |
| frame submission snapshots | 512 | renderer-owned, tagged, fixed storage |
| frame palette snapshots | 512 | renderer-owned, fixed 128-matrix storage |
| local lights | 4 | renderer-owned state, unchanged |

Meshes remain limited to 65,535 vertices and 196,608 indices. One SM3D v2 model remains limited to 16 MiB, 131,072 total vertices, 393,216 total indices, 16 parts, 64 imported materials, 128 metadata texture references, 256 animation nodes, 128 bones, 64 imported clips, 64 events per clip, and 64 sockets.

## Atomic submission groups

`BeginSubmissionGroup3D(Capacity)`, `CommitSubmissionGroup3D(Token)`, and `RollbackSubmissionGroup3D(Token)` expose the generic protocol. Begin reserves exact physical queue and conservative palette capacity without publishing. Direct-LDR commit rasterizes staged entries only after the group is complete; multipass commit publishes the complete group to the queue. Any validation/capacity failure rolls back all staged records and references. `End3DChecked` rolls an open group back and fails cleanly.

`Character3D.Draw` reserves its exact part count and commits only after every visible part has a valid snapshot. A two-part or sixteen-part actor is therefore all-or-nothing for ordinary validation and capacity failures.

## Shadow, target, and color corrections

- native and Web shadow passes select culling from each captured double-sided material;
- masked shadow casters retain captured alpha/texture state;
- both simple and PBR normal bias use the actual selected directional vector or spot position;
- directional light-space X and Y are snapped to shadow texel increments;
- static, legacy-skinned, and production-skinned main/shadow palette counters remain separate;
- pass exit restores/unbinds owned shaders, resources, samplers, blend, depth, rasterizer, viewport, framebuffer, and texture state;
- native and Web target rebuilds construct and validate candidates before swapping live state;
- only a compatible last-good target bundle can be restored after a failed candidate;
- fallback flags are recomputed for the current configuration/resource generation and a successful retry advances the generation;
- WebGL error checks stay at bounded resource-operation boundaries; there is no global `gl.getError()` frame/draw hot-path poll;
- reference tests cover linear-to-sRGB conversion, ACES-like tone mapping, max-RGB bloom brightness, bloom weights, and direct-LDR/Renderer2D bypass.

## Diagnostics and measurements

Existing draw-call and submitted-triangle diagnostics were already present as numeric commands 78 and 79, so they were reused. M5 query 117 now additionally exposes physical/provisional/reserved submissions, palette snapshots, in-flight mesh/texture references, snapshot bytes, palette capacity, group state, and read-only per-snapshot probes through query IDs 42-51 and 60-69.

The deterministic snapshot fixture submits one ground, two revisions of a two-part Character3D actor, and the same cube twice. It verifies seven physical/logical entries, two cube positions (`10.000` and `20.000`), two colors (red `1.000` and `0.000`), distinct pose revisions, palette reuse within each two-part pose, nonzero in-flight counts, and 20,000 bytes of snapshot storage (`7 * 512 + 2 * 8,208`). Teardown verifies zero palette snapshots, in-flight meshes, in-flight textures, and snapshot bytes. The group fixture observes one provisional entry inside a two-entry reservation, then zero after rollback. The overflow fixture accepts exactly 512 entries and rejects entry 513 with error 51.

Manual Post Lab observations:

| Target | HDR/MSAA | Shadow | Bloom | Draws | Frame observation | Estimated target bytes |
| --- | --- | --- | --- | --- | --- | ---: |
| native 960x540 | RGBA16F, 4x | 2048, 6 draws/452 triangles | 480x270, 2 cycles | main/post 6/6 | about 8 ms | 47,881,216 |
| Web 1280x720 | RGBA16F, Web effective 1x | 2048, 6 draws/452 triangles | 640x360, 2 cycles | main/post 6/6 | about 8 ms | 31,522,816 |
| native/Web direct LDR | direct LDR | disabled | disabled | post/shadow 0/0 | native about 1 ms; Web visually stable | 0 M5 off-screen bytes |

The target-byte values follow the runtime formulas: shadow depth, multisampled/resolve scene color plus depth, and two bloom targets. The focused target sequence validates successful 2048 -> 1024 -> 2048 replacement with monotonically increasing generations. The Web Post Lab console was empty in both captured states. Renderer2D text remained crisp and post-exempt.

Screenshots:

- `artifacts/screenshots/m5-1-native-post-lab.png`
- `artifacts/screenshots/m5-1-native-post-lab-direct-ldr.png`
- `artifacts/screenshots/m5-1-web-post-lab.png`
- `artifacts/screenshots/m5-1-web-post-lab-direct-ldr.png`

## Validation

All required commands passed:

```text
cmd /c scripts\build.cmd
.\scripts\test-smile-formatter.ps1
.\scripts\format-smile-style.ps1 -Check -FormatLongIf
.\scripts\test-renderer3d-m11-hardening.ps1
.\scripts\test-renderer3d-v2-boundaries.ps1
.\scripts\test-renderer3d-models.ps1
.\scripts\test-renderer3d-lifecycle.ps1
.\scripts\test-renderer3d-materials.ps1
.\scripts\test-renderer3d-animation.ps1
.\scripts\test-renderer3d-pbr.ps1
.\scripts\test-renderer3d-pbr-hardening.ps1
.\scripts\test-renderer3d-animation-v2.ps1
.\scripts\test-renderer3d-animation-v2-hardening.ps1
.\scripts\test-character3d.ps1
.\scripts\test-renderer3d-post-processing.ps1
.\scripts\test-renderer3d-post-processing-hardening.ps1
.\scripts\test-battle3d.ps1
.\scripts\test-dragonfall.ps1
.\scripts\test-simple3d-space-wars.ps1
.\scripts\test-true-simple3d-neon-cycles.ps1
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release
cmd /c scripts\smoke-test.cmd
cmd /c scripts\install-vsix.cmd
```

Focused exact results include 13 formatter integration groups, style conformance for 334 SMILE files, 288 managed language/compiler/project tests, 39 native graphics/audio-focus checks, 44 native text checks, exact native/Web console parity for every focused Renderer3D/Character3D/Battle3D/Dragonfall gate, and the final M5.1 result: `Renderer3D M5.1 native/Web snapshot, group, ownership, shadow, target, color, and hot-path hardening tests passed.` The build and VSIX rebuild have only the pre-existing `NU1503` warning for restore of the native `.vcxproj`; compilation succeeds.

The deterministic M0 GLB remains 1,168 bytes with SHA-256 `A4D0E8C9CFE8714C7C44241D4BF03066BEC464128DD587B81EE8244BCBE24060`.

## VSIX

The extension and bundled compiler/runtime/library payload version is 2.0.54. `scripts/install-vsix.cmd` rebuilt, removed the prior extension, installed the new package into Visual Studio Enterprise instance `91f001b5`, and hash-verified the installed DLL:

- VSIX: `artifacts/vsix/Smile.VisualStudio.vsix`
- VSIX SHA-256: `6092D5251CF1F7A47AC07218DE8C60FCCCBA191AFBE91E47F2E5DA883FCACEE3`
- built/installed DLL SHA-256: `FA544CF293C8FC58A02582D24ADFE56318BF73B920C89AC68880948BDEDB23F1`
- installed assembly version: 2.0.54.0
- Visual Studio restart required to load the refreshed extension: yes

## Remaining limitations and M6 readiness

Ordinary submission validation and capacity failures are atomic; a GPU/device failure after rasterization begins can still leave partial pixels, as expected for the existing immediate backend. Web cannot provide multisampled render-to-texture in this implementation, so High profile reports the existing MSAA-reduced fallback while retaining HDR/shadow/bloom. The diagnostics are deliberate test/lab APIs, not a general profiler.

M6 is unblocked after this report's commit is pushed and remote-verified. Its particle and ribbon work must consume the reserved tagged queue kinds and generic group protocol rather than reintroducing mutable handles or partial multi-part submissions.
