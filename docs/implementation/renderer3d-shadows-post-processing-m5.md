# Renderer3D Shadows and Post-Processing — M5

> Historical M5 delivery record. The current queue ownership, destruction behavior, diagnostics, command range, and next-free numeric ID are superseded by `renderer3d-shadow-post-hardening-m5-1.md`.

## Status and reconciliation

M5 is complete on `main`. Work began from `4a6cb87b3eaf1666193469a40c15b1bb74d3b9fc`, which exactly matched `origin/main` and contains the completed M4.1 prerequisite. The branch was zero commits ahead and behind. The only pre-existing worktree item was the user-owned untracked `docs/plans/` tree; it was neither edited nor staged. No reset, clean, destructive restore, rebase, amend, force-push, or history rewrite was used.

The implementation preserves the Renderer2D/GDI path, SM3D v1/v2, M2.1 PBR, M3.1 production animation, M4/M4.1 Character3D and Scene3D, Battle3D, Dragonfall, Simple3D, and Space Wars. It adds only the bounded M5 submission, shadow, HDR, tone-map, bloom, configuration, and diagnostic capabilities. M6 VFX, IK, retargeting, morphs, LOD, runtime glTF, WebGPU, third-party engines, and a general render graph were not started.

Because M5 changes the native runtime, Web runtime, Simple3D library, compiler payload, examples, and bundled Visual Studio extension content, the VSIX version advances from 2.0.52 to 2.0.53.

## Renderer3D command ABI and dispatch

The ABI remains append-only. Numeric commands 1–112, image commands 1–2, and text commands 1–9 retain their previous meanings.

| Range/ID | ABI | Arguments and result |
|---|---|---|
| numeric 1–112 | preserved | Existing mesh, object, model, material, animation, PBR, production-animation, and renderer-state operations. |
| numeric 113 | `CONFIGURE_POST` | `a` post Boolean, `b` HDR Boolean, `c` bloom Boolean, `d` exposure percent 25–400, `e` threshold thousandths 500–8000, `f` intensity percent 0–400, `g` downsample 2 or 4, `h` cycles 0–2, `i` requested samples 1, 2, or 4. |
| numeric 114 | `CONFIGURE_SHADOW` | `a` enabled Boolean, `b` caster 0 none/1 directional/2 spot, `c` spot slot 0–3, `d` resolution 1024 or 2048, `e` constant bias millionths 0–1000, `f` normal bias hundred-thousandths 0–1000. |
| numeric 115 | `SET_SHADOW_AREA` | `a`–`c` center XYZ in ±1,000,000; `d` width and `e` height in 1–2,000,000; `f` near greater than zero; `g` far greater than near and no more than 2,000,000. |
| numeric 116 | `SET_OBJECT_SHADOWS` | `a` object handle, `b` casts-shadow Boolean, `c` receives-shadow Boolean. |
| numeric 117 | `M5_VALUE` | `a` property, `b` index or object handle; returns the requested diagnostic value. |
| image 1–2 | preserved | Simple and PBR texture creation. |
| text 1–9 | preserved | Model loading and named production-animation operations. |

The current ranges are numeric 1–117, image 1–2, and text 1–9. The next free IDs are numeric 118, image 3, and text 10. Invalid M5 configuration/query input reports renderer error 50. A submission beyond the fixed 512-entry frame queue reports error 51. Ending a frame after a queued resource becomes stale preserves existing error 14 behavior.

The full dispatch path remains shared and explicit:

- SMILE source calls the private `Graphics3D.Dispatch`, `DispatchImage`, or `DispatchText` wrapper.
- Native compilation maps those syntax forms in `MasmEmitter.cs` to `smile_renderer3d_command`, `smile_renderer3d_image_command`, and `smile_renderer3d_text_command`.
- `graphics3d.h` declares the exported ABI; `graphics3d_directx.cpp` owns the Direct3D 11 command switch and fixed resource state.
- Web compilation maps the same forms in `WebEmitter.cs` to `smile.renderer3D`, `smile.renderer3DImage`, and `smile.renderer3DText`.
- `WebOutputWriter.cs` owns the WebGL2 switches and mirrors commands 113–117, configuration validation, fallbacks, and query meanings.

`M5_VALUE` properties are:

| Property | Meaning | Property | Meaning |
|---:|---|---:|---|
| 1 | logical submissions | 20 | bloom height |
| 2 | submission capacity | 21 | bloom cycles |
| 3 | multipass active | 22 | post draws |
| 4 / 5 | shadow requested/effective | 23 | tone map effective |
| 6 | shadow resolution | 24 | exposure percent |
| 7 / 8 | shadow draws/triangles | 25 | low-level fallback flags |
| 9 | shadow palette uploads | 26 | target generation |
| 10 / 11 | HDR requested/effective | 27 | total target bytes |
| 12 | HDR format | 28 / 29 | caster type/spot slot |
| 13 | effective sample count | 30 / 31 | constant/normal bias |
| 14 / 15 | scene width/height | 32 / 33 | post requested/effective |
| 16 | resolve count | 34 | rejected submissions |
| 17 / 18 | bloom requested/effective | 35 / 36 / 37 | shadow/scene/bloom bytes |
| 19 | bloom width | 40 / 41 | object casts/receives shadow |

The low-level fallback bits are 1 shadow resolution reduced, 2 shadow disabled, 4 HDR unavailable, 8 MSAA reduced, 16 bloom resolution reduced, 32 bloom disabled, 64 tone mapping disabled, and 128 direct LDR. Scene3D exposes the corresponding bits as 16, 32, 64, 128, 256, 512, 1024, and 2048 while retaining its earlier capability and asset-fallback bits.

## Bounded rendering architecture

Direct LDR rendering remains the immediate compatibility path. Multipass mode records each logical draw independently in a fixed 512-entry caller-order queue; it does not merge duplicate submissions or allocate on the hot path. End-frame validates queued handles before work, renders the selected shadow pass, renders the HDR scene, resolves native multisampling when needed, applies bounded bloom, tone maps through the fitted ACES curve, encodes sRGB, and returns to the permanent Renderer2D overlay path.

M5 owns one shadow target, one HDR scene target, and two reusable bloom targets. The shadow caster is either one directional light or one selected spot light. Shadow maps are 1024 or 2048 square, use a depth resource sampled as R32, fixed 3×3 PCF, and bounded constant/normal bias. Opaque and mask materials cast; blend materials do not. Static, legacy-skinned, and production-skinned objects participate, with per-object cast and receive flags.

The native HDR target is `R16G16B16A16_FLOAT` and supports requested 4, 2, or 1 samples with resolve. Web uses a single-sample floating-point framebuffer only when `EXT_color_buffer_float` exists and framebuffer completeness succeeds. Its deterministic downgrade from requested multisampling is reported rather than hidden. Bloom uses half- or quarter-resolution ping-pong targets and no more than two cycles. Tone mapping uses:

```text
(x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14)
```

followed by sRGB encoding. Commands 113–115 detect unchanged configuration and avoid target churn. Failures fall back through reduced sampling or features to the established direct LDR path. Renderer2D text, HUD, menus, and overlays remain post-processing-exempt.

Scene3D supplies Low, Medium, High, and Auto profiles. Low selects direct LDR with no shadows or bloom and requests one sample. Medium selects HDR/tone mapping, a 1024 shadow, quarter-resolution one-cycle bloom, and requests two samples. High selects HDR/tone mapping, a 2048 shadow, half-resolution two-cycle bloom, and requests four samples. Auto chooses High when PBR is available and Low otherwise. Independent setters remain available. `RenderProfileKey` and `AssetProfileKey` are separate so render-only changes do not duplicate model assets.

Character3D mirrors cast/receive state and applies changes transactionally to every part. `SetShadows`, `CastsShadow`, and `ReceivesShadow` preserve the first renderer failure, restore already-updated parts when a later part fails, and use Character3D error 17 for a shadow-state failure.

## Current resource limits and ownership

| Resource | Limit | Ownership |
|---|---:|---|
| Meshes | 128 live | Runtime pool; objects and models borrow/reference; destruction is refused while referenced. |
| Objects | 512 live | Caller-owned instances; borrow mesh, material, and optional animator handles. Each owns its M5 cast/receive flags. |
| Textures | 128 live | Caller/material/model owned; each retains its decoded image; destruction is refused while referenced. |
| Materials | 128 live | Caller or model owned; objects borrow; model-owned PBR materials cannot be independently destroyed. |
| Models | 64 live | Own up to 16 part meshes, 64 materials, 128 texture references/images, and immutable production-animation data. |
| SM3D model data | 16 MiB; 131,072 vertices; 393,216 indices | Converter and runtime enforce the same bounded, rollback-safe publication limits. |
| Legacy skeletons | 64 live; 32 bones each | Caller-owned; legacy animators borrow. |
| Legacy clips | 128 live; 16 events each | Caller-owned; legacy animators borrow. |
| Animators | 128 total | Caller/Character3D-owned mutable state; objects borrow. Production animators borrow their model. |
| Production animation | 256 nodes, 128 bones, 64 clips, 64 events/clip, 64 sockets, 32 pending events | Immutable data is model-owned; fixed mutable pose, palette, event, and scratch data is animator-owned. |
| Character3D | 16 cached assets, 32 actors, 16 parts/actor | Cache owns models and fallback resources; actors own animators/part objects and borrow the cached model. |
| Dragonfall | 48 meshes; 441 initial/448 boss objects; 24 materials; 6 textures; 35 effects | Dragonfall owns its current procedural scene resources and releases them through its established lifecycle. |
| M5 submissions | 512 per frame | Renderer-owned fixed queue; one entry per logical draw in caller order. |
| Shadow | one 1024/2048 target | Renderer-owned and recreated only when effective configuration/device size requires it. |
| HDR scene | one target | Renderer-owned; native supports 4/2/1 samples, Web supports one effective sample. |
| Bloom | two half- or quarter-resolution targets; two cycles maximum | Renderer-owned reusable ping-pong targets. |

## Deterministic fixtures and examples

`examples/Renderer3DPostProcessingTests` is a deterministic native/Web contract fixture for queue order, capacity, shadow draws/triangles, post targets, fallbacks, configuration, and object flags. It reuses a repository-owned SM3D asset and records exact normal and forced-fallback expectations. `scripts/test-renderer3d-post-processing.ps1` builds and executes the native and Web fixture, exercises forced HDR and shadow failure hooks, checks exact parity, builds both Post Lab targets, and validates generated JavaScript syntax.

`examples/Renderer3DPostProcessingLab` is the visible M5 sample. It shows a PBR animated Character3D scene, one directional shadow, HDR/tone mapping, bloom, profile controls, 2D post-exempt diagnostics, target sizes, draw/triangle counters, and fallback state.

## Validation results

Validation completed on 2026-09-01 (Asia/Taipei) with .NET SDK 10.0.400, Node.js 24.14.0, and Visual Studio 18 Enterprise.

| Command/gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` | PASS; compiler, tests, asset tool, Visual Studio extension, and x64 native runtime built. One stable NU1503 warning reports restore skipped for the native `.vcxproj`. |
| `scripts\test-smile-formatter.ps1` | PASS; 13 focused formatter integration groups. |
| `scripts\format-smile-style.ps1 -Check -FormatLongIf` | PASS; 332 files. |
| `scripts\test-renderer3d-post-processing.ps1` | PASS; native normal, forced HDR fallback, forced shadow fallback, matching Web cases, native/Web Post Lab builds, and generated JavaScript syntax. |
| `scripts\test-renderer3d-m11-hardening.ps1` | PASS. |
| `scripts\test-renderer3d-v2-boundaries.ps1` | PASS; exact boundary and over-limit rejection retained. |
| `scripts\test-renderer3d-models.ps1` | PASS; deterministic conversion, corruption rejection, and native/Web parity retained. |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS; ownership, counters, exhaustion, restart, and frame cycles retained. |
| `scripts\test-renderer3d-materials.ps1` | PASS; ownership/material parity retained. |
| `scripts\test-renderer3d-animation.ps1` | PASS; legacy animation native/Web parity retained. |
| `scripts\test-renderer3d-pbr.ps1` | PASS; PBR ownership, lights, samplers, diagnostics, lifecycle, and Lab builds retained. |
| `scripts\test-renderer3d-pbr-hardening.ps1` | PASS; failure, fallback, ownership, transform, skinning, and lifecycle coverage retained. |
| `scripts\test-renderer3d-animation-v2.ps1` | PASS; deterministic fixtures, 128-bone boundary, production playback, lifecycle, and Lab builds retained. |
| `scripts\test-renderer3d-animation-v2-hardening.ps1` | PASS; timing, sampling, crossfade, root motion, overflow, compact memory, deformation, palette reuse, and lifecycle retained. |
| `scripts\test-character3d.ps1` | Initial check 171 exposed that shadow palette uploads had been added to the legacy main-pass counter. After separating main and shadow counters, PASS for native/Web normal and forced fallback plus Lab builds. |
| `scripts\test-battle3d.ps1` | PASS; native/Web exact parity. |
| `scripts\test-dragonfall.ps1` | PASS; mechanics, balance/lifecycle, demo/no-demo, and native/Web builds. |
| `scripts\test-simple3d-space-wars.ps1` | PASS; Simple3D and Space Wars native/Web validation. |
| `scripts\test-true-simple3d-neon-cycles.ps1` | PASS; native/Web true Renderer3D Simple3D validation. |
| `dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release --no-restore` | PASS; 288 managed tests. Printed SML diagnostics are intentional negative fixtures. |
| `scripts\verify-artifacts.ps1` | The first invocation exposed three stale 2.0.52 version regexes after the intentional 2.0.53 bump. After synchronizing those checks, PASS for libraries, native executables, game assets, VSIX payload/version, viewport, and DPI. |
| `cmd /c scripts\smoke-test.cmd` | PASS, exit code 0: doctor/build, 288 managed tests, 13 formatter groups, 332-file style gate, focused M5 gate, 39 native graphics/audio checks, 44 native Text checks, all retained native/Web packages and games, artifact verification, and VSIX 2.0.53 synchronization. The suite ends with its expected manual-gameplay reminder. |

Read-only reconciliation and evidence commands used `git status`, `git branch --show-current`, `git rev-parse`, `git rev-list --left-right --count`, `git diff`, `rg`, `Get-Content`, `Get-ChildItem`, `Get-FileHash`, safe ZIP entry validation/extraction below `artifacts/temp`, package-manifest SHA-256 comparison, and browser/native process inspection. Implementation and validation commands additionally used `apply_patch`, the listed repository build/test scripts, targeted compiler invocations from those scripts, a localhost static server for the Web sample, and the repository VSIX install/verification scripts. No reset, clean, checkout restore, rebase, amend, or force-push command was run.

## Manual native and Web evidence

The Web Post Lab completed a clean High-profile frame at 1280×720 with HDR format 1, effective sample count 1, one 2048 shadow, six shadow draws/452 shadow triangles, 640×360 bloom, two cycles, 125% exposure, six main draws, six post draws, six logical submissions, two Character3D actors, six objects, and an approximately 8 ms frame. Scene fallback flags were 128 because Web deliberately downgraded the requested four samples to its supported single sample. The 2D HUD remained sharp and post-processing-exempt. Browser console logs were empty. Evidence is `artifacts/screenshots/m5-final-web-post-lab.png`.

The native Post Lab was briefly inspected with the same HDR/bloom scene, animated objects, floor, and visible directional shadow. A Windows Security firewall prompt for the temporary local Python server overlapped the native window, so that obstructed capture is not accepted as final evidence and the security UI was not manipulated. The server and sample processes were stopped. The deterministic native gate remains the exact functional evidence for the native path.

This was a brief manual observation, not a benchmark or soak.

## Plan deviations and limitations

1. The handoff's expected M4.1 commit was the actual starting `HEAD`; no descendant reconciliation or reset was needed.
2. Numeric command 113 was genuinely free, so M5 occupies the planned append-only range 113–117 without changing the image or text ABI.
3. Direct LDR stays immediate for compatibility; only multipass profiles use the fixed submission queue.
4. WebGL2 cannot mirror native multisampling for floating-point scene targets in this bounded implementation. Web deliberately uses one effective sample, sets the MSAA-reduced fallback bit, and otherwise preserves output and diagnostics.
5. The handoff's palette accounting required an independent shadow-pass counter. Existing production-animation palette-upload diagnostics remain main-pass-only so M3/M4 contracts do not change.
6. Native target creation initially released newly created M5 targets through an overly broad cleanup path. Focused validation exposed it and target ownership was corrected before the full gate.
7. The first artifact-verifier run retained three 2.0.52 regular expressions. They were updated to the intended 2.0.53 version before the clean full smoke run.
8. Shadows are intentionally limited to one directional or selected spot caster, fixed 3×3 PCF, and opaque/mask casters. Transparent shadow casting, cascades, point-light cube shadows, temporal effects, and a general render graph remain outside M5.
9. Bloom is intentionally limited to two reusable targets and two cycles. Tone mapping is the single fitted ACES curve rather than a configurable operator family.
10. M6 was not started.

## VSIX and next milestone

The repository installer rebuilt and installed `artifacts/vsix/Smile.VisualStudio.vsix` into Visual Studio instance `91f001b5`. The installer verification and an independent `verify-vsix-install.ps1` invocation both passed.

| Item | Value |
|---|---|
| VSIX/product version | 2.0.53 |
| assembly version | 2.0.53.0 |
| built VSIX SHA-256 | `49D01B1F83CEF82FFEAEFA6571013970669503E285527556B35C8318A8C66740` |
| built/installed DLL SHA-256 | `FBC2C609DC22479F46790B4DF8A2FCE60F6AE29ED768971A9E62C1AAD611E5B6` |
| installed DLL | `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\zwhsckdn.fe0\Smile.VisualStudio.dll` |
| verification | PASS through installer and explicit `verify-vsix-install.ps1` |
| restart required | Yes, Visual Studio must restart to load the refreshed extension. |

M6 is unblocked after the focused M5 commit containing this report is pushed and `origin/main` is verified at that commit. M6 remains intentionally unimplemented in this run.
