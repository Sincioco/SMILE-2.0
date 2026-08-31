# Renderer3D M2.1 PBR Hardening

Date: 2026-08-31
Branch: `main`
Starting commit: `b34f4c5284f9f636e17a62ce5b6e2721d53be464`
Starting upstream: `origin/main` at the same commit; ahead/behind `0/0`

## Reconciliation

The M2.1 handoff was reconciled against the current M2 implementation without resetting or discarding work. The starting tracked tree was clean. The existing untracked `docs/plans/` tree is user-owned and remained untouched.

The implementation keeps numeric commands 1-97 and image commands 1-2 source/ABI compatible. The handoff recommended numeric command 98 for explicit model preparation, but Web asset resolution is asynchronous while the numeric `Renderer3D` intrinsic is synchronous. The reconciled cross-target ABI therefore uses text command 3 for preparation. This is the smallest native/Web-parity extension and leaves numeric command 98 free.

The preferred mirrored-object policy would require additional tangent-handedness and front-face handling. M2.1 instead adopts the handoff's accepted bounded alternative: positive, nonsingular object scale is the PBR production profile. Singular and mirrored PBR draws fail deterministically with error 46 before counters change. The simple path is unchanged.

No SM3D v2 format change, new PBR feature, new animation system, shadow, post-processing, or VFX work is included.

## Command ABI

### Ranges

| Bridge | Occupied before M2.1 | Occupied after M2.1 | Next free |
|---|---:|---:|---:|
| Numeric `Renderer3D` | 1-97 | 1-97 | 98 |
| Image `Renderer3DImage` | 1-2 | 1-2 | 3 |
| Text `Renderer3DText` | 1 | 1-3 | 4 |

### Added and extended commands

| Bridge/ID | Name | Arguments | Result |
|---|---|---|---|
| Text 2 | `LOAD_MODEL_GEOMETRY` | owned exact project asset path; remaining values zero | model handle or zero |
| Text 3 | `PREPARE_MODEL_PBR` | text is ignored/consumed; `a=model`, `b=filter`, `c=wrap`, `d=anisotropy` | success |
| Numeric 96, `a=0` | `PBR_SHADER_AVAILABLE` | no remaining values | availability; starts the generation's sole attempt if needed |
| Numeric 96, `a=1` | `PBR_PIPELINE_STATE` | no remaining values | 0 not attempted, 1 available, 2 unavailable |
| Numeric 96, `a=2` | `PBR_PIPELINE_FAILURE` | no remaining values | stable failure code, normally 0 or 44 |
| Numeric 96, `a=3` | `PBR_PIPELINE_ATTEMPT_COUNT` | no remaining values | 0 or 1 in the current generation |
| Numeric 97, property 1 | model PBR ready | `a=model` | Boolean |
| Numeric 97, property 2 | model-owned material count | `a=model` | count |
| Numeric 97, property 3 | unique model-owned texture count | `a=model` | count |
| Numeric 97, property 4 | part uses imported PBR default | `a=model`, `c=part` | Boolean |
| Numeric 97, property 5 | model geometry ready | `a=model` | Boolean |
| Numeric 97, property 6 | model PBR preparation failure | `a=model` | error code |
| Numeric 97, property 7 | metadata texture-reference count | `a=model` | count |

Numeric commands 1-95, numeric 96/97's existing queries, image commands 1-2, and text command 1 retain their established meanings.

### Dispatch paths

The public facade is `libraries/Smile.Simple3D/Graphics3D.smile`. Numeric calls pass through its private `Dispatch` to the `Renderer3D` intrinsic. Native lowering is in `src/Smile.Compiler/MasmEmitter.cs`, reaches `smile_renderer3d_command` in `graphics3d_directx.cpp`, and uses the existing fixed pools. Web lowering is in `src/Smile.Compiler/WebEmitter.cs`, reaches `renderer3D` in `WebOutputWriter.cs`, and uses equivalent `Map`-owned resources.

Text calls lower through `Renderer3DText`. Native reaches `smile_renderer3d_text_command` in `runtime.c`, which consumes the text, resolves commands 1/2 through the project asset manifest, and routes command 3 to `smile_renderer3d_prepare_model_pbr`. Web emits an awaited `smile.renderer3DText` call; `renderer3DText` awaits geometry/model loading or PBR preparation as required. No second parser, renderer, or asset path was added.

## Pipeline failure state

Each native graphics-device or WebGL2-context generation owns a tri-state PBR pipeline record, a stable failure code, and an attempt count. Compilation/linking is attempted at most once. The deterministic test hook is native environment variable or Web host global `SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE`; it produces unavailable state, error 44, and attempt count one. Simple Renderer3D creation and drawing continue, and another frame does not retry. `ResetRenderer3D`, native device loss, or Web context loss begins a fresh generation.

## Model preparation and texture ownership

`LoadModel3D` remains the source-compatible all-in-one operation. `LoadModelGeometry3D` publishes validated v1/v2 geometry and metadata without resolving an image or consuming texture/material slots. `PrepareModelPbr3D` accepts only v2 geometry, treats an already-prepared model as a no-op success, preflights capacity, creates temporary dependencies, and publishes them atomically.

Failure preserves the model handle and part meshes, leaves PBR readiness false, records the error in both `LastError` and the model diagnostic, and restores prior image/texture/material counts. Objects created before preparation keep their current default (normally the simple path); newly created parts receive the imported PBR default.

The identity of a prepared texture is:

```text
exact retained path
+ color/data usage
+ filter
+ wrap
+ requested/effective anisotropy
+ mip policy
```

All references in one preparation share the requested sampler settings. The metadata reference-to-handle map is separate from the unique-owned-handle list. Destruction iterates only unique owned handles. The deterministic `PbrDeduplicated.sm3d` fixture has four metadata references, two imported materials, and two unique owned textures.

## Native image memory

Straight BGRA is now the canonical retained decoded plane. It preserves nonzero RGB beneath alpha zero for PBR uploads. Premultiplied BGRA is derived lazily under the image-cache lock. Direct2D and simple Renderer3D acquire it only for bitmap/texture creation and release it after a durable GPU object exists; GDI's CPU-backed path may retain it through the compatible `smile_image_resource_pixels` accessor.

The native image test layer now exposes and verifies straight, premultiplied, and total retained CPU byte counts. A 2,048 x 1,024 fixture retains 8,388,608 straight bytes after decode, temporarily reaches 16,777,216 total bytes while premultiplied pixels are acquired, returns to 8,388,608 after release, and returns all three diagnostics to zero after final image release.

## Transform, animation-scale, alpha, and output policy

- A clip caches whether every authored two-key scale track is uniform at both keys. Replacing a track recomputes the cache across the clip.
- Simple-material animation retains nonuniform bone scale.
- A PBR draw with a nonuniform-scale active clip fails with error 45 before submission.
- A PBR object matrix with determinant at or below `1e-7` natively or `1e-8` on Web fails with error 46. This rejects singular and mirrored transforms consistently at SMILE's integer-percent scale precision.
- Opaque and mask PBR draws write depth. Blend draws use straight source alpha, read depth, do not write depth, and remain in caller order. Callers should submit overlapping blended objects farthest to nearest.
- The final explicit sRGB transfer remains isolated in `ApplyLdrOutputTransfer`/`applyLdrOutputTransfer`; no HDR, tone-mapping, or post-processing contract was added.

## Web hot path and lifecycle

Web capability probes are cached per context generation. Anisotropy extension/limit discovery is not repeated by texture creation. The PBR program is not recompiled per frame after failure. `renderer3DDrawPbr` uses preallocated matrix, normal, palette, and light storage; the gate rejects typed-array construction, map/spread allocation, mip generation, or shader compilation inside that function. Reset deletes the PBR program and resets the tri-state/attempt diagnostics while retaining the WebGL context's cached anisotropy capability.

## Limits and ownership

| Resource | Current limit | Ownership |
|---|---:|---|
| Mesh | 128 live | Caller-owned standalone; model owns one per part; objects borrow and block destruction |
| Object | 512 live | Caller/scene-owned; borrows mesh, material, and optional animator |
| Model | 64 live | Owns metadata, part meshes, prepared imported materials, unique prepared textures, and retained images |
| Texture | 128 live; 8,192 x 8,192 maximum | Caller-owned standalone or uniquely model-owned; materials borrow |
| Material | 128 live | Caller-owned standalone or model-owned; objects borrow; model-owned handles are not directly exposed |
| Skeleton | 64 live; 32 bones each | Caller-owned; clips/animators borrow and block destruction |
| Clip | 128 live; 16 events each | Caller-owned; borrows skeleton; active animators block destruction |
| Animator | 128 live | Caller-owned; borrows skeleton/current clip; objects borrow and block destruction |
| SM3D v2 model | 16 parts, 131,072 vertices, 393,216 indices, 64 materials, 128 metadata texture refs, 32 chunks, 16 MiB | Counts also must fit the live global pools; only deduplicated prepared textures consume texture slots |

Dragonfall remains on the unchanged simple path. Its reviewed scene owns six textures, 24 materials, 48 mesh-owning templates/arena objects, 441 objects initially, and 448 objects in the boss encounter. It owns no loaded model, skeleton, clip, or animator resources and tears down instances before mesh owners, then materials, then textures.

## Stable M2.1 error meanings

| Error | Meaning |
|---:|---|
| 40 | invalid model/preparation arguments or non-v2 model |
| 41 | unique texture or material capacity preflight failed |
| 42 | exact asset resolution, image decode, texture creation, or preparation transaction failed |
| 43 | existing bounded light input failure |
| 44 | PBR pipeline unavailable |
| 45 | PBR draw rejected an active clip with nonuniform scale keys |
| 46 | PBR draw rejected a singular or mirrored object transform |

## Deterministic fixtures

`scripts/generate-renderer3d-pbr-fixtures.ps1` owns all PBR fixtures and verifies them under `-Check`. M2.1 adds:

- `examples/Renderer3DPbrHardeningTests/Assets/PbrDeduplicated.sm3d`: 1,300 bytes, SHA-256 `3E7092C32A94698BDC5E8C499059E0F842749CEB8A35E2CA52736AC80598A8B8`;
- four metadata texture references mapped to one exact path, with color/data usage separation producing two unique prepared textures;
- normal, missing-dependency, legacy-v1, and exact PNG copies for the focused native/Web project.

The pre-existing deterministic M0 GLB remains verified at 1,168 bytes and SHA-256 `A4D0E8C9CFE8714C7C44241D4BF03066BEC464128DD587B81EE8244BCBE24060`.

## Validation recorded before commit

| Command | Result |
|---|---|
| `scripts\build.cmd` | PASS; compiler, asset tool, native runtime/tests, solution, and VSIX built |
| `scripts\test-renderer3d-pbr-hardening.ps1` | PASS; native/Web normal and forced failure, fallback, ownership, transforms, animation scale, lifecycle, and hot-path checks |
| `scripts\test-renderer3d-pbr.ps1` | PASS; existing M2 native/Web gate and PBR Lab builds |
| `scripts\test-renderer3d-models.ps1` | PASS; deterministic conversion plus native/Web loading/lifecycle |
| `scripts\test-renderer3d-materials.ps1` | PASS; existing native/Web texture/material gate |
| `scripts\test-renderer3d-animation.ps1` | PASS; existing native/Web animation gate |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS; existing native/Web pool/generation/frame gate |
| `artifacts\tests\Smile.NativeTextTests.exe` through `run-bounded-test.cmd 60` | PASS; 44 native Text/image runtime checks |

| `scripts\\test-smile-formatter.ps1` | PASS; 13 focused formatter integration tests |
| `scripts\\format-smile-style.ps1 -Check -FormatLongIf` | PASS; 324 files checked |
| `scripts\\test-renderer3d-m11-hardening.ps1` | PASS; scene, material, unsupported-feature, input-safety, and atomic-publication gate |
| `scripts\\test-renderer3d-v2-boundaries.ps1` | PASS in the repository PowerShell host; exact/over-limit SM3D v2 boundary gate |
| `scripts\\test-battle3d.ps1` | PASS; native/Web validation with exact Web console parity |
| `scripts\\test-dragonfall.ps1` | PASS; native/Web mechanics, lifecycle, demo, and no-demo validation |
| `scripts\\test-simple3d-space-wars.ps1` | PASS; Simple3D and Space Wars focused native/Web validation |
| `dotnet run --project src\\Smile.Tests\\Smile.Tests.csproj -c Release` | PASS; 288 language, compiler, project, completion, and timing tests |
| `scripts\\smoke-test.cmd` with pinned SDK environment | PASS; complete repository smoke and VSIX payload verification |
| `scripts\\install-vsix.cmd` with pinned SDK environment | PASS; installed and hash-verified SMILE VSIX 2.0.48, assembly 2.0.48.0, SHA-256 `821E0CD19322DEB72297505315238623733E06AE4DE944A0BA17BB24F75C6448` |
| Visible native `Renderer3DPbrLab.exe` | PASS; 8 PBR, 1 simple, 9 draws, 2,348 triangles |
| Visible Web `Renderer3DPbrLab` | PASS; matching 8/1, 9 draws, 2,348 triangles; no browser warnings/errors |

The first bare `scripts\\smoke-test.cmd` invocation stopped in `doctor.ps1` because the fresh process found the machine-wide .NET 10.0.400 host before the per-user pinned 10.0.302 installation. Setting `DOTNET_ROOT` and prepending `C:\\Users\\louie\\AppData\\Local\\Microsoft\\dotnet` to `PATH` made the required run pass without changing `global.json`. An additional diagnostic rerun of the v2-boundary script under Windows PowerShell 5 reported its intentionally rejected native converter stderr as a terminating `NativeCommandError`; the repository PowerShell host completed the same gate and reported PASS.

Visible evidence is saved outside source control at `artifacts/verification/renderer3d-pbr-hardening-native.png` and `artifacts/verification/renderer3d-pbr-hardening-web.jpg`.

## M3 gate

M3 is technically unblocked by the completed gates and becomes procedurally unblocked when this milestone commit is pushed. It can rely on stable PBR failure state, explicit geometry-first preparation, unique texture ownership, straight-alpha memory behavior, positive object transforms, uniform PBR animation scale, caller-ordered blend submission, and allocation-free Web PBR draws. It must not renumber commands 1-97 or reinterpret text commands 1-3.
