# Renderer3D Animation Hardening M3.1

## Milestone status and reconciliation

M3.1 was implemented on `main` from commit `8fd1ff18cff12116a52c175e88424e3e4ffc8d2a`, with `origin/main` at the same commit. The user-owned untracked `docs/plans/` directory was preserved and is not part of this milestone. The starting commit already contained the completed M3 production animation path, numeric commands 1-109, image commands 1-2, text commands 1-9, and Visual Studio extension version 2.0.49.

**Flag:** M3 production animation did not yet preserve fractional time across updates, advance both sides of a crossfade, retain only animation data after model creation, or expose bounded event-overflow and memory diagnostics. Its capacity fixture also proved the 128-bone transport with only one skinned triangle rather than visibly articulated geometry. M3.1 adds the smallest reusable corrections and diagnostics for those gaps. It does not add a new format version, Character3D, Scene3D, PBR features, new animation features, VFX, or Dragonfall-specific behavior.

## Runtime corrections

- Native and Web animators retain independent source and destination hundredth-millisecond remainders. Splitting an update into smaller deltas now produces the same time and pose as one combined update for speeds 1, 33, 50, 75, 125, 175, 333, and 1,000 percent.
- Final fixed-rate sampling uses the exact clip duration and last sample interval, including clips whose duration is not evenly divisible by their sample rate.
- A moving crossfade advances both source and destination clips using their own mode, speed, time, completion state, and remainder. Only destination events enter the public queue while fading.
- Interrupting a fade promotes the evaluated destination to the new source. The implementation remains a bounded two-way blend and never creates a hidden third animation layer.
- Root-motion removal is applied independently to each sampled clip before pose blending. Source and destination root deltas are blended by the same fade weight.
- `Play`, crossfade, stop, time-zero events, queue clearing, and overflow state now have explicit lifecycle behavior. Overflow keeps the first 32 pending events, sets error 49, and records a sticky saturating dropped-event count until clear, play, or stop resets it.
- `StopAnimator3D` and destruction restore the public wrapper to `ClipIndex = -1` and playback mode zero. Crossfade wrappers mirror the actual source mode and clip instead of prematurely claiming the destination is current.

## Compact memory ownership

Native validates the complete SM3D file transactionally, then retains one aligned animation-only payload containing the nine production animation chunks and rebased descriptors. The full file buffer and temporary descriptor storage are released before publication. Web releases its fetched `ArrayBuffer` after validation and publication, retains skin weights as `Uint16Array` until mesh publication, and converts weights to floats only while filling the GPU-facing mesh data.

Model diagnostics report source file bytes, retained animation bytes, and mutable bytes per animator. The articulated fixture measures 9,712 source-file bytes and 6,544 animation payload bytes; native retained animation bytes are aligned but remain below the full file size. Mutable animator storage is backend-specific and intentionally reported rather than standardized byte-for-byte.

An animated model owns the immutable animation payload. Production animators borrow it and own only their fixed mutable playback, event, pose, palette, and scratch state. Objects borrow an animator. The established destruction order and refusal rules remain unchanged.

## Append-only command ABI

Every numeric command 1-109, image command 1-2, and text command 1-9 is unchanged. M3.1 appends two numeric commands and no image or text command.

| Numeric ID | Operation | Positional arguments | Result |
|---:|---|---|---|
| 110 | `ANIMATOR_PRODUCTION_VALUE` | `a=animator, b=property` | production playback, remainder, overflow, revision, or mutable-memory diagnostic |
| 111 | `CLEAR_ANIMATOR_EVENTS` | `a=animator` | clears pending events plus sticky overflow/drop diagnostics and returns success |

Command 110 properties are:

| Property | Result |
|---:|---|
| 1 | destination clip index, or -1 |
| 2 | source hundredth-millisecond remainder |
| 3 | destination hundredth-millisecond remainder |
| 4 | destination clip time in milliseconds |
| 5 | sticky event-overflow Boolean |
| 6 | saturating dropped-event count |
| 7 | current/source playback mode |
| 8 | destination playback mode |
| 9 | pose revision |
| 10 | mutable bytes for this animator |

Existing numeric command 98 gains read-only model properties 11 (`source file bytes`), 12 (`resident animation bytes`), and 13 (`mutable bytes per animator`). The next free IDs are numeric 112, image 3, and text 10.

The complete dispatch routes remain:

| Layer | Path |
|---|---|
| Public facade/constants and wrapper mirrors | `libraries/Smile.Simple3D/Graphics3D.smile`, `libraries/Smile.Simple3D/Core.smile` |
| Built-in bridge arity/types | `src/Smile.Language/Syntax.cs`, `src/Smile.Language/Semantics.cs` (unchanged) |
| Native emission | `src/Smile.Compiler/MasmEmitter.cs` to `smile_renderer3d_command`, `_image_command`, and `_text_command` |
| Native declarations and IDs | `src/Smile.NativeRuntime/graphics/graphics3d.h` |
| Native validation, ownership, sampling, and dispatch | `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` |
| Native owned-text routing | `src/Smile.NativeRuntime/runtime.c` |
| Web emission | `src/Smile.Compiler/WebEmitter.cs` to `smile.renderer3D`, `renderer3DImage`, and awaited `renderer3DText` |
| Web validation, ownership, sampling, and dispatch | `src/Smile.Compiler/WebOutputWriter.cs` |

## Current resource limits and ownership

| Resource | Current hard limit | Ownership |
|---|---:|---|
| Mesh | 128 live; 65,535 vertices and 196,608 indices per mesh | caller-owned standalone or owned by one model; objects borrow |
| Object/model part | 512 live | borrows mesh, material, and optional animator |
| Model | 64 live; 16 parts; 131,072 total vertices; 393,216 total indices; 64 materials; 128 texture references; 16 MiB file | owns part meshes, imported materials/textures/images, and immutable animation bytes |
| Texture | 128 live; 8,192 by 8,192 maximum | caller-owned standalone or model-owned; materials borrow |
| Material | 128 live | caller-owned standalone or model-owned; objects borrow |
| Legacy skeleton | 64 live; 32 bones | caller-owned; legacy clips and animators borrow |
| Legacy clip | 128 live; 16 events | caller-owned; active legacy animators borrow |
| Animator | 128 total; 32 pending events | caller-owned mutable state; objects borrow; production animators borrow their model |
| Production hierarchy | 256 nodes/model | model-owned immutable payload |
| Production skeleton | 128 bones/model | model-owned immutable payload and 128-matrix palette |
| Production clips | 64/model; 120,000 ms; 15-60 Hz | model-owned immutable tracks and samples |
| Production events | 64/clip; 32 pending/animator | model-owned definitions; animator-owned bounded FIFO and overflow diagnostics |
| Production sockets | 64/model | model-owned definitions; evaluated per animator |

Dragonfall remains on its custom Renderer3D scene and owns no loaded models or animators. Its established limits remain 48 meshes, 441 initial objects, 448 boss-scene objects, 24 materials, six textures, and a 35-object effect pool. M3.1 does not change Dragonfall ownership or resource counts.

## Deterministic articulated fixture

`scripts/generate-renderer3d-animation-v2-fixtures.ps1` now owns `AnimationArticulated.glb`, its descriptor, and the converted SM3D copied byte-for-byte to the test, hardening, and Animation Lab asset folders. `-Check` regenerates and compares every output.

| Property | Value |
|---|---:|
| GLB bytes / SHA-256 | 8,124 / `8363BA089E3CE25AB4D0ECA56D131CBB05E9CEC7030F57982EEFA6CDF7D8BBFF` |
| Descriptor bytes / SHA-256 | 944 / `18C4C6ABA536453BE3C2FF4E68F04ECEB24CEF204D49FB6FC98D9275CC99505B` |
| SM3D bytes | 9,712 |
| Parts / materials | 2 / 2 |
| Vertices / triangles | 32 / 36 |
| Skinning | 8 bones, with one-, two-, three-, and four-influence vertices |
| Clips | `Idle` 1,000 ms at 15 Hz; `Bend` 1,010 ms at 30 Hz; `WalkLike` 1,017 ms at 60 Hz; `AttackLike` 750 ms at 30 Hz; `RootMove` 1,200 ms at 30 Hz |
| Metadata | ordered time-zero/step/impact events, root motion, and `HandTip` socket |

The hardening gate draws both parts using one animator and asserts exactly two draw calls, 36 submitted triangles, and one palette upload. The Animation Lab uses two independent animators over the same model, four part objects, the animated hand socket, root-path markers, PBR lighting, and Renderer2D diagnostics.

## Plan mapping and deviations

- The M3 handoff described model ownership as retaining the complete immutable file. M3.1 narrows that to the nine rebased animation chunks after validation, matching the hardening requirement without changing SM3D v2.
- M3 advanced only the destination during a fade and used destination-only root motion. M3.1 advances and blends both bounded sides, while intentionally suppressing source events.
- M3 used floating multiplication that discarded sub-millisecond fractions. M3.1 uses integer remainder accumulation and rational final-interval sampling on both backends.
- M3 documented destination-driven interruption but did not promote all destination state. M3.1 promotes destination clip, time, remainder, mode, and completion before starting the replacement fade.
- The original 68/128-bone capacity fixtures remain unchanged. A separate visibly articulated fixture was added instead of weakening their capacity role or importing copyrighted content.
- Web already released the fetched model buffer after publication, but retained expanded float skin weights. M3.1 retains compact uint16 weights until final mesh publication.
- No file-format version, vertex stride, live resource limit, PBR behavior, new animation feature, VFX, Character3D, Scene3D, or Dragonfall scene behavior changed.

## Validation evidence

Validation completed with the repository-pinned .NET SDK 10.0.302 from `C:\Users\louie\AppData\Local\Microsoft\dotnet`. The system SDK location contained 10.0.400 and did not satisfy `global.json` during the first build attempt. Selecting the already-installed pinned SDK corrected the environment; no project SDK upgrade or download was required.

| Command or gate | Exact result |
|---|---|
| `scripts/build.cmd` | PASS; native runtime/tests, compiler, AssetTool, language tests, templates, and VSIX 2.0.50 built. |
| `scripts/test-smile-formatter.ps1` | PASS; all 13 formatter cases. |
| `scripts/format-smile-style.ps1 -Check -FormatLongIf` | PASS; 327 tracked SMILE files. |
| `scripts/test-renderer3d-m11-hardening.ps1` | PASS. |
| `scripts/test-renderer3d-v2-boundaries.ps1` | PASS; exact and over-limit SM3D v2 boundaries. |
| `scripts/test-renderer3d-models.ps1` | PASS; deterministic conversion and native/Web exact parity. |
| `scripts/test-renderer3d-lifecycle.ps1` | PASS. |
| `scripts/test-renderer3d-materials.ps1` | PASS. |
| `scripts/test-renderer3d-animation.ps1` | PASS; legacy animation native/Web exact parity. |
| `scripts/test-renderer3d-pbr.ps1` | PASS. |
| `scripts/test-renderer3d-pbr-hardening.ps1` | PASS. |
| `scripts/test-renderer3d-animation-v2.ps1` | PASS; fixtures, 128-bone boundary, native/Web exact parity, lifecycle, malformed rollback, and Animation Lab builds. |
| `scripts/test-renderer3d-animation-v2-hardening.ps1` | PASS; fractional timing, irregular final sample, moving/interrupted fades, root motion, event overflow, compact memory, articulated deformation, palette reuse, native/Web exact parity, and ten lifecycle cycles. |
| `scripts/test-battle3d.ps1` | PASS; native/Web exact parity. |
| `scripts/test-dragonfall.ps1` | PASS; native/Web mechanics, lifecycle, balance, demo, and no-demo builds. |
| `scripts/test-simple3d-space-wars.ps1` | PASS; Simple3D and Space Wars native/Web validation. |
| `dotnet run --project src/Smile.Tests/Smile.Tests.csproj -c Release` | PASS; 288 language, compiler, project, completion, and timing tests. Printed failure diagnostics are intentional negative-test fixtures. |
| Native manual Animation Lab | PASS; two independently animated articulated actors, PBR ground/lights, socket/root markers, and a live diagnostic overlay rendered. |
| Web manual Animation Lab | PASS; the same articulated scene rendered without browser console errors. |
| `scripts/smoke-test.cmd` | PASS; complete native/Web compiler, runtime, package, game, formatter, artifact, viewport, DPI, and VSIX verification. The first run found one stale 2.0.49 template-wizard verifier regex; after synchronizing it to 2.0.50, standalone artifact verification and the complete clean rerun passed. |
| `scripts/verify-artifacts.ps1` | PASS; VSIX compiler/shared-language/template payload and identity, assembly, file, and product versions synchronized at 2.0.50. |
| `scripts/install-vsix.cmd` | PASS; rebuilt and installed `artifacts/vsix/Smile.VisualStudio.vsix` into Visual Studio instance `91f001b5`. |
| `scripts/verify-vsix-install.ps1 -InstanceId 91f001b5 ...` | PASS; installed assembly version 2.0.50.0 and installed DLL SHA-256 `D13B36C1A04D108B3874F61383373F7E30598D60DBA784518F02B316C489CE5B`. |

The validated VSIX SHA-256 is `CA0F2629803A41020B731BBB26E9F37093F346E17BC40FDC2DB4AA9814F2BE33`. The compiler SHA-256 is `99D9D1A57594C0C9129B2D89C4DABC1332E807172F8C5B7FE90168440C60E161`; the AssetTool SHA-256 is `4C449A8F94B52FC9439B5CFB9B1B3E04E93151EB7D864410AC01F3E099DF010B`.

## M4 readiness

M4 is unblocked after this milestone commit is pushed: the complete smoke suite, artifact verification, and installed VSIX verification pass. M4 may build generic Character3D and Scene3D presentation over these exact model, animator, event, root-motion, and socket contracts. It must not replace the bounded runtime ownership or append-only ABI established here.
