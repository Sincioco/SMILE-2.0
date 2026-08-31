# Renderer3D SM3D Version 2 M1.1 Hardening

## Reconciliation and scope

M1.1 starts from `0049c72eb80a8c1ea366cdfe5840f7db71e89d76` on `main`, with `origin/main` at the same commit. The only pre-existing working-tree item was the untracked `docs/plans/` directory; it was preserved and is not part of this milestone.

This milestone hardens the M1 static conversion and loading path. It does not add PBR rendering, a runtime glTF/GLB parser, SM3D animation chunks, new animation behavior, VFX Generation 2, or a render graph. The existing SM3D v1 and v2 layouts remain unchanged.

## Importer semantics

The offline converter now walks only the selected glTF scene. When `scene` is absent, it uses scene zero; a missing `scenes` array is represented by deterministic root nodes. Traversal is depth-first in declared root/child order, rejects invalid references and cycles, excludes unreachable meshes, and emits a separate transformed part for each repeated mesh instance. Deterministic part names include the node, mesh, primitive, and instance indices.

Node transforms accept either a 4x4 column-major matrix or TRS, but never both. Local transforms compose with their parents. Quaternions are normalized; non-finite and singular transforms fail. Positions are baked with the composed world transform, normals use the inverse transpose, and tangents are transformed and re-orthogonalized. A negative determinant reverses triangle winding and tangent handedness. The SMILE coordinate reflection is then applied exactly once. Stored model and part bounds are recomputed from the final emitted positions.

An omitted primitive material now refers to a converter-owned implicit `Default` material rather than material zero. The implicit material is created only when required, participates in the 64-material limit, and remains distinct from a declared material at index zero.

The static profile rejects rather than strips skins, animations, joints, weights, morph targets, mesh/node weights, embedded images, nonzero texture-coordinate sets, texture transforms, compressed geometry, and unsupported extensions. The only recognized extension beyond core glTF is `KHR_materials_emissive_strength`. The representable sampler profile accepts an absent sampler or the equivalent explicit values: linear magnification (`9729`), trilinear minification (`9987`), and repeat wrapping (`10497`) on both axes.

## Source limits and input safety

Limits are checked before the corresponding read, decode, allocation, or collection expansion:

| Input or output resource | Limit |
|---|---:|
| Textual glTF JSON | 4 MiB |
| GLB source | 64 MiB |
| One declared buffer | 32 MiB |
| Aggregate declared buffers | 64 MiB |
| Buffers | 16 |
| Buffer views | 512 |
| Accessors | 512 |
| Scenes | 16 |
| Nodes | 4,096 |
| Source meshes | 256 |
| Source primitives | 4,096 |
| Images | 128 |
| One source name | 1,024 UTF-8 bytes |
| SM3D output | 16 MiB |
| Output parts | 16 |
| Output vertices | 131,072 total; 65,535 per part |
| Output indices | 393,216 total; 196,608 per part |
| Output materials | 64 |
| Output texture references | 128 |
| Texture path | 1,024 UTF-8 bytes |
| SM3D chunks | 32 |

Base64 input is size-preflighted before decoding. Accessors validate semantic type, component type, count, byte alignment, stride, buffer-view target, and their complete logical range against the declared buffer length. Physical trailing bytes cannot make an out-of-range declared buffer valid. Malformed JSON value kinds and arithmetic overflow produce controlled conversion failures.

Conversion rejects identical input and output paths. It writes to a unique sibling temporary file, flushes it, atomically publishes only after successful conversion, and removes the temporary file on every failure. An existing destination therefore remains unchanged after conversion or publication failure.

## Validator and diagnostic parity

AssetTool inspection, the native Direct3D loader, and the generated Web loader now apply the same printable-ASCII chunk-ID, canonical tangent-basis, structure, reference, path, and bounds rules. The basis check uses tolerance `0.0001` for normalized normal/tangent lengths, orthogonality, and exact positive or negative handedness. The v1 inspector now checks structural ranges and index validity instead of reporting header counts alone.

Numeric command 80 remains a read-only diagnostic. Valid zero properties are distinguished by their documented query semantics. Invalid query, index, or property combinations and stale model handles return zero and set `LastError` on native and Web. Hash results are diagnostic FNV-1a values only: the test fixture currently reports model/part hashes `2857009948`/`3250840144` for the M0 triangle and `1687710659`/`819355085` for the PBR triangle. No texture is loaded or identified through these hashes.

The generated Web draw path now reuses matrix, tint, and material scratch arrays. Matrix construction is performed in place, removing per-object typed-array construction, array spreading, and map-based coercion from the hot draw loop while retaining the existing draw-call and submitted-triangle counters.

## Renderer3D ABI and resource ownership

M1.1 adds no commands. Numeric commands are allocated continuously from 1 through 80; image and text commands each use only command 1. Numeric 78 is draw-call count, 79 is submitted-triangle count, and 80 is the multiplexed static-model metadata query. The next free numeric, image, and text commands are 81, 2, and 2 respectively. The exact positional ABI and all native/Web dispatch paths remain recorded in `docs/implementation/renderer3d-visual-generation-2-reconciliation.md` and the M1 command-80 addition remains recorded in `docs/implementation/renderer3d-sm3d-v2-core.md`.

Current live resource limits and ownership are unchanged:

| Resource | Live limit | Ownership |
|---|---:|---|
| Mesh | 128 | Standalone meshes are caller-owned. A loaded model owns its part meshes. |
| Object/model-part instance | 512 | Refers to a mesh and optional material/animator; owns none. |
| Texture | 128 | Caller-owned simple textures; imported model ownership begins in M2. |
| Material | 128 | Caller-owned simple materials; objects borrow them. |
| Model | 64 | Owns retained metadata and up to 16 part meshes; live part objects block destruction. |
| Skeleton | 64 | Caller-owned; clips and animators borrow it. |
| Clip | 128 | Caller-owned; animators borrow it. |
| Animator | 128 | Caller-owned; objects borrow it. |
| Bones per skeleton | 32 | Fixed bounded pose storage. |
| Animation events per clip | 16 | Fixed bounded event storage. |

Native resources use kind-tagged, generation-safe handles. Web resources use monotonically increasing safe-integer handles backed by bounded Maps and do not reuse stale handles after reset.

## Dragonfall capacity and atomic initialization

The reviewed scene exhausted all 512 object slots. The first rejected allocation was `ParticleObjects[9]`, after 503 non-particle objects and nine particle instances; the observed state was 48 meshes, 512 objects, 24 materials, six textures, and `LastError = 5`.

The fixed scene retains a 35-object effect pool and creates mutually exclusive encounter actors. Initial play creates the cinderlings but not the dragon: 48 meshes, 441 objects, 24 materials, six textures, `LastError = 0`, and 71 free object slots. Entering the boss encounter destroys 90 cinderling objects and lazily creates 97 dragon objects: 448 objects, `LastError = 0`, and 64 free slots. Effect presets clamp their requested count to the bounded pool; visible battle effects remain present.

Initialization now validates every required handle, exact live counts, minimum headroom, and a clear error state before publishing readiness. Any partial failure invokes idempotent cleanup and returns `False`. Shutdown also cleans a partially initialized scene. A lifecycle test pre-fills 80 object slots, forces initialization to fail, verifies that only those blockers remain, then destroys them and verifies complete zero-count teardown. The existing 100 clean restart cycles pass on native and Web.

## Repository baseline repairs

The authoritative `Smile.RPG` package is version 1.3.0. The smoke expectation and both managed assertions were stale at 1.2.1; they now expect provider `Smile.RPG@1.3.0` and the current 497-member API.

The transactional formatter was applied only to the seven reported files: four Dragonfall files, `Smile.Battle3D/Camera.smile`, `Smile.RPG/BattleCore.smile`, and `Smile.Simple3D/Interaction.smile`. Six are formatting-only; `DragonfallScene.smile` also contains the capacity and lifecycle work. The repository-wide formatter check now passes all 322 tracked SMILE files.

## Plan deviations

- The handoff identified one stale package expectation, while the full baseline also contained two stale managed assertions. All three were updated from the same authoritative 1.3.0 project/package evidence.
- The malformed-source corpus is generated deterministically inside the focused repository script rather than storing a large collection of textual glTF variants. The cross-loader corrupt SM3D corpus remains repository-owned binary fixtures.
- Repeated scene instances are emitted as separate baked parts because SM3D v2 intentionally has no scene graph. This preserves visible scene semantics without changing the container.
- Dragonfall uses a 35-object effect pool plus mutually exclusive cinderling/dragon allocation. This preserves visible effects and guarantees the preferred 64-slot boss headroom without increasing the global 512-object limit.
- No Renderer3D ABI allocation was required; commands 1-80, image 1, and text 1 retain their M1 meanings.

## Validation evidence

Validation completed on 2026-08-31 (Asia/Taipei) with .NET SDK 10.0.302.

| Command or gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` | PASS, exit 0; compiler, AssetTool, native runtime, tests, and VSIX built. |
| `scripts\test-smile-formatter.ps1` | PASS, all 13 formatter integration tests. |
| `scripts\format-smile-style.ps1 -Check -FormatLongIf` | PASS, 322 tracked SMILE files. |
| `scripts\test-renderer3d-m11-hardening.ps1` | PASS; scene/TRS/matrix/reflection/repeated-instance/default-material/unsupported-feature/source-safety/atomic-publication corpus. |
| `scripts\test-renderer3d-v2-boundaries.ps1` | PASS; exact 7,865,176-byte boundary model and over-limit rejection. |
| `scripts\test-renderer3d-models.ps1` | PASS; v1/v2 conversion, deterministic fixtures, corrupt corpus, command 80, hot-path assertion, native/Web rendering and parity. |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS, native/Web exact parity and zero teardown. |
| `scripts\test-renderer3d-materials.ps1` | PASS, native/Web exact parity. |
| `scripts\test-renderer3d-animation.ps1` | PASS, native/Web exact parity. |
| `scripts\test-battle3d.ps1` | PASS, native/Web exact parity. |
| `scripts\test-simple3d-space-wars.ps1` | PASS, Simple3D and Space Wars focused validation. |
| `scripts\test-dragonfall.ps1` | PASS; mechanics native/Web, balance, atomic lifecycle, 100 restarts, demo and no-demo builds. |
| Native atomic lifecycle gate | PASS. |
| `dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release` | PASS, all 288 managed tests. |
| `cmd /c scripts\smoke-test.cmd` | PASS, exit 0; full managed, formatter, native, Web, game, artifact, and VSIX verification baseline. |
| `cmd /c scripts\install-vsix.cmd` | PASS; rebuilt, replaced, installed, and verified VSIX 2.0.48. |
| `git diff --check` | PASS. |

The native Direct3D manual fixture displayed the transformed cyan generated-tangent triangle and orange imported-tangent triangle. Dragonfall attract mode displayed the complete party, cinderlings/dragon encounter presentation, and visible cyan impact effect at the 120 FPS cap; the no-demo build opened in playable command state.

The visible WebGL2 fixture displayed the same two triangles with no browser warnings or errors. Web Dragonfall displayed the complete actors and impact effect, loaded all six declared textures with HTTP 200 responses, and produced no browser warnings or errors.

The installed Visual Studio extension is version 2.0.48. `Smile.VisualStudio.dll` reports assembly version 2.0.48.0 and SHA-256 `F739DE09408634A1C2FA00A33144DD520AD0FD3FD71D24836BC75C68052470BA`. The VSIX SHA-256 is `8DBDD9B6C3188DFAAE466C3BEE32DF3C0981DF4159B412595B47076847EDDBAF`; the compiler and AssetTool SHA-256 values are `197EDD9F304FD73F0C0D79A4629A1EC2771C5EC65EC2576571C285D8E33D39FA` and `2FDF91C32A96E4754A6040C862C4B16594139AA7A96F2B28F5CA33BACBF036A6`.

## M2 readiness

M2 is unblocked after this milestone is committed and pushed. It can use exact retained texture paths and validated PBR metadata, while adding model-owned texture/material resources atomically. Command-80 hashes remain diagnostics only. Static SM3D v2 has no runtime scene graph, PBR shading, imported texture ownership, or animation chunks; those are deliberate milestone boundaries rather than M1.1 defects.

## Command ledger

The substantive repository commands used for M1.1 were:

```powershell
git fetch origin
git status --short --branch
git rev-parse HEAD
git rev-parse origin/main
git stash push -m codex-m2-wip-before-m1.1

cmd /c scripts\build.cmd
scripts\generate-renderer3d-v2-fixtures.ps1
scripts\test-smile-formatter.ps1
scripts\format-smile-style.ps1 -Check -FormatLongIf
scripts\test-renderer3d-m11-hardening.ps1
scripts\test-renderer3d-v2-boundaries.ps1
scripts\test-renderer3d-models.ps1
scripts\test-renderer3d-lifecycle.ps1
scripts\test-renderer3d-materials.ps1
scripts\test-renderer3d-animation.ps1
scripts\test-battle3d.ps1
scripts\test-simple3d-space-wars.ps1
scripts\test-dragonfall.ps1
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release
cmd /c scripts\smoke-test.cmd
cmd /c scripts\install-vsix.cmd

Get-FileHash -Algorithm SHA256 artifacts\vsix\Smile.VisualStudio.vsix
Get-FileHash -Algorithm SHA256 artifacts\compiler\smilec.exe
Get-FileHash -Algorithm SHA256 artifacts\assettool\smileasset.exe
git diff --check
```
