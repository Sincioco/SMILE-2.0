# Renderer3D SM3D Version 2 Static Core

## M1 reconciliation

Milestone M1 starts from `fb33b44449043b8f52db6e0b828c774044e3bc3f` on `main`. The current SM3D v1 converter and both runtime loaders already validate a complete file before allocating model meshes, own part meshes through `Model3D`, and roll back a failed multi-part allocation. M1 extends that ownership path rather than adding another model resource.

The Generation 2 planning maxima were proposals. The static v2 core therefore retains the 16 MiB file ceiling and 16-part model limit, adds total limits of 131,072 vertices and 393,216 indices, retains 64 materials, and permits at most 128 external texture references. This is sufficient for the intended M7 hero while remaining compatible with the live pools of 128 meshes, 512 objects, 128 textures, and 64 models. M3 may add new optional animation chunks, but it must justify any file-size increase from measured animation fixtures.

M1 does not create PBR runtime materials or load texture images. It preserves static PBR metadata and canonical project-relative texture references in the model resource so M2 can resolve them through the existing exact asset manifest.

## Final container decision

SM3D v2 remains little-endian and uses the existing `SM3D` magic. Its 64-byte header contains version, flags, exact file size, FNV-1a checksum, directory metadata, deterministic model-name reference, and summary counts. The checksum covers every byte after the header, including the directory, known chunks, unknown optional chunks, and alignment padding.

The chunk directory starts at byte 64. Every 32-byte entry contains a four-byte ID, flags, offset, length, count, stride, and two zero fields. Directory entries and chunks are four-byte aligned, ranges may not overlap, and known chunks occur exactly once. Flag bit zero marks an optional chunk; all other flag bits are reserved. Unknown required chunks fail. Unknown optional chunks are range/checksum validated and ignored.

The M1 writer emits these required chunks in this order:

| ID | Count/stride | Purpose |
|---|---|---|
| `STR0` | string count / 0 | UTF-8, NUL-terminated deterministic string table |
| `PART` | part count / 32 | names, vertex/index ranges, material slots, bounds indices |
| `VERT` | vertex count / 48 | position, normal, tangent XYZW, UV0 |
| `INDX` | index count / 4 | local unsigned 32-bit part indices |
| `MATL` | material count / 80 | PBR factors, texture-reference indices, alpha and sidedness |
| `TEXR` | texture count / 16 | canonical external path and semantic |
| `BOND` | part count + 1 / 32 | model bounds followed by one record per part |

`NODE`, `SKEL`, `CLIP`, `AFRM`, `EVNT`, `ROOT`, and socket data are reserved for M3 and are not emitted or interpreted in M1. Morphs, LODs, compression, embedded textures, multiple UV sets, and runtime glTF/GLB parsing remain deferred.

## Texture-reference policy

Texture references use forward-slash, case-preserving, project-relative paths. Empty segments, `.`, `..`, absolute/drive/UNC paths, URI schemes, backslashes, wildcards, NUL/control characters, and paths longer than 1,024 UTF-8 bytes fail conversion and loading. Texture bytes are never embedded. The v2 loader retains the exact validated path; M2 will resolve it only through the declared project asset manifest.

## Tangent policy

Valid glTF `TANGENT` float `VEC4` data is normalized and imported. Coordinate reflection negates Z and handedness. When tangents are absent, the converter accumulates triangle tangent/bitangent directions in stable primitive/index order, Gram-Schmidt orthogonalizes against a normalized normal, and derives handedness from the bitangent sign. Non-finite data, zero-length normals, degenerate triangles, and degenerate UV derivatives fail. Tangents are generated only offline.

## ABI decision

Renderer3D commands 1–79, image command 1, and text command 1 remain unchanged. M1 uses numeric command 80 as one multiplexed, read-only static-model metadata query. No image or text command is added. Runtime GLB parsing is not added.

Command 80 arguments are model handle, query, index, and property in numeric bridge positions A-D. Query IDs are: 1 format version, 2 vertex count, 3 index count, 4 texture-reference count, 5 positive tangent handedness count, 6 negative tangent handedness count, 7 material property, 8 texture property, 9 model/part bounds, 10 part-name FNV-1a hash, and 11 model-name FNV-1a hash. Bounds and finite material factors are returned in thousandths. Texture references in material properties are returned one-based so zero remains “not present.”

The native path remains `Graphics3D.smile` -> numeric/text built-ins -> generated call -> `runtime.c` -> `graphics3d_directx.cpp`. The Web path remains `Graphics3D.smile` -> awaited `renderer3DText`/numeric `renderer3D` -> the generated pure-JavaScript Renderer3D in `WebOutputWriter.cs`. M1 extends those existing dispatches; it does not add a second runtime or touch the image bridge.

## Implemented ownership and failure behavior

Both v2 loaders parse and validate header, checksum, directory, every required chunk, unknown optional chunks, strings, paths, material references, vertices, tangents, triangles, indices, and exact computed bounds before creating a mesh. They then preflight the 64-model and 128-mesh pools. A capacity failure leaves the original live counts unchanged; an allocation/commit failure after preflight deletes every mesh created by that load. A published model owns its part meshes and retained static metadata. Existing part objects share those meshes and continue to block model destruction until destroyed.

The runtime vertex allocation grows from the prior 16-float internal layout to 20 floats by appending tangent XYZW. Position, normal, UV, joints, and weights keep their previous offsets, so primitives, custom meshes, and animation skinning preserve their ABI. M1 intentionally does not bind tangent as a shader attribute or consume PBR metadata during drawing; M2 owns that rendering change.

## M1 backlog mapping

| Task | Result |
|---|---|
| VG2-M1-001 | Final 64-byte header, 32-byte directory, seven required chunks, FNV-1a coverage, optional policy, and reconciled hard limits documented in `docs/architecture/sm3d-model-format.md`. |
| VG2-M1-002 | Added one focused `Sm3dV2.cs`; retained the compact v1 implementation and CLI entry in `Program.cs`. No asset framework was introduced. |
| VG2-M1-003 | Added strict GLB 2.0 parsing and retained textual glTF; GLB selects v2 and textual glTF uses explicit `--format-version 2`. |
| VG2-M1-004 | Added supplied-tangent import, deterministic offline generation, coordinate/handedness reflection, and finite/degeneracy rejection. |
| VG2-M1-005 | Added all requested static PBR metadata and exact safe external texture paths without texture bytes or runtime network access. |
| VG2-M1-006 | Added deterministic `STR0`, `PART`, `VERT`, `INDX`, `MATL`, `TEXR`, and `BOND` writing. |
| VG2-M1-007 | Added complete native v2 validation, metadata retention/querying, pool preflight, and rollback on the existing model owner. |
| VG2-M1-008 | Added matching pure-JavaScript/WebGL2 v2 validation, metadata retention/querying, pool preflight, and rollback. |
| VG2-M1-009 | Added deterministic v1/v2 `smileasset inspect`. |
| VG2-M1-010 | Extended the model gate with valid/invalid GLB/glTF/SM3D, determinism, exact/over-limit capacities, native/Web parity, drawing, and teardown. |
| VG2-M1-011 | Updated the AssetTool README, authoritative format document, and Simple3D API. |

## Reconciled plan deviations

- The draft proposal's larger model maxima were not adopted. M1 retains 16 MiB, 16 parts, and 64 materials; total geometry is capped at 131,072 vertices/393,216 indices and texture references at 128 to fit the existing 128-mesh/512-object/64-model pools safely on native and Web.
- Textual `.gltf` without a format option still emits byte-compatible v1. This protects the existing command while making v2 selection explicit; `.glb` unambiguously emits v2.
- A single multiplexed numeric command 80 exposes validation metadata. Image command 2 and text command 2 remain free because neither is needed for an offline container or a model already loaded through text command 1.
- The implementation did not modify `runtime.c` or `WebEmitter.cs`: the actual current dispatch and generated-runtime ownership already route through `graphics3d_directx.cpp` and `WebOutputWriter.cs`.
- Model/part names use the deterministic first mesh and primitive naming path. General scene nodes remain deferred rather than introducing an M1 scene graph.
- M3 chunk IDs are reserved in documentation only. Skeletons, clips, events, root motion, and sockets were not implemented.

## Final M1 evidence

Validation completed on 2026-08-31 (Asia/Taipei) from starting commit `fb33b44449043b8f52db6e0b828c774044e3bc3f`.

| Gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` | PASS, exit 0; compiler, AssetTool, native runtime, managed tests, and VSIX built with .NET SDK 10.0.302 |
| Targeted formatter check | PASS for `Core.smile`, `Graphics3D.smile`, model-test `Program.smile`, and `Manual.smile` |
| `scripts\test-smile-formatter.ps1` | PASS, all 13 formatter integration tests |
| `scripts\test-renderer3d-models.ps1` | PASS; v1 byte compatibility, GLB/glTF v2 equivalence, valid/invalid fixtures, exact/over-limit boundaries, native/Web draw and exact console parity |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS native/Web exact parity |
| `scripts\test-renderer3d-materials.ps1` | PASS native/Web exact parity |
| `scripts\test-renderer3d-animation.ps1` | PASS native/Web exact parity |
| `scripts\test-battle3d.ps1` | PASS native/Web exact parity |
| `scripts\test-simple3d-space-wars.ps1` | PASS Simple3D and Space Wars focused validation |
| Native manual project | PASS; visible Direct3D window drew the cyan GLB/generated-tangent and orange glTF/imported-tangent fixtures |
| Web manual project | PASS; visible WebGL2 page drew the same fixtures and reported zero browser warnings/errors |
| `scripts\test-dragonfall.ps1` | Same M0 known failure: mechanics native/Web and native balance pass, then lifecycle reports `Dragonfall 3D lifecycle tests failed: 100` instead of `passed` |
| Repository formatter check | Same seven M0 files differ: four Dragonfall files plus `Battle3D/Camera.smile`, `Smile.RPG/BattleCore.smile`, and `Simple3D/Interaction.smile` |
| `scripts\smoke-test.cmd` | Same M0 unrelated managed-test mismatch: expected `Smile.RPG` 1.2.1, found 1.3.0 |

The boundary fixture loads seven 16-part models plus one single-part model at 8 models/113 meshes. A further 16-part load fails without changing 8/113, and teardown returns models and meshes to 0/0. The normal semantic fixture phase reaches 5 models/6 meshes; a malformed load leaves those counts unchanged; five created objects draw and final teardown returns models, meshes, objects, materials, and textures to zero.

The validated manual project builds are `artifacts/tests/Renderer3DModelManual.exe` and `artifacts/web/Renderer3DModelManual`. The rebuilt VSIX is version 2.0.48 with SHA-256 `CA68A4D3C6FDD2FB7EEB1FA296FE1E2321FD89BA6A8ADB20DE08E3CDA4F3436D`. Installation verification found exactly one extension, assembly version 2.0.48.0, and an installed DLL identical to the build with SHA-256 `1B7A0C3A5E605D518A0EB87E4C0E2C07298E6FE8AE5F1ADD2768A87EF0D3CB3A`.

M2 is unblocked: both runtimes now retain and expose validated static PBR metadata and exact safe texture paths on the existing model owner. M2 may resolve declared images, create the bounded material resources, bind tangents, and add PBR-lite lighting without changing the v2 static container or revisiting GLB parsing.
