# Character3D and Scene3D Hardening — M4.1

## Status and reconciliation

M4.1 is complete on `main`. Work began from `39b413cdad0ef0899158b1826f30813d853c660a`, which exactly matched `origin/main` and the reviewed handoff baseline. The branch was zero commits ahead and behind. The only pre-existing worktree item was the user-owned untracked `docs/plans/` tree; it was neither edited nor staged. No reset, clean, destructive restore, rebase, amend, force-push, or history rewrite was used.

The implementation preserves SM3D v1/v2, numeric commands 1–111, image commands 1–2, text commands 1–9, M2.1 PBR, M3.1 production animation, Renderer2D/GDI, Battle3D, Dragonfall, Simple3D, and Space Wars. M5 shadows, HDR, post-processing, VFX, IK, retargeting, morphs, LOD, runtime glTF, WebGPU, and third-party engines were not started.

The repository-pinned .NET SDK 10.0.302 was unavailable while 10.0.400 and the .NET 10 targeting pack were installed. Under Sin's explicit SDK-upgrade authorization, `global.json`, the doctor check, and the README prerequisite now consistently select 10.0.400. The Visual Studio extension version advanced from 2.0.51 to 2.0.52 because this milestone changes the bundled Simple3D library and compiler/runtime payload.

## Renderer state and dispatch ABI

One read-only numeric bridge was appended:

| Range/ID | ABI | Meaning |
|---|---|---|
| numeric 1–77 | preserved | core mesh/object/material/model and legacy animation commands |
| numeric 78–79 | preserved | draw-call and submitted-triangle diagnostics |
| numeric 80–97 | preserved | SM3D v2 and PBR diagnostics/operations |
| numeric 98–111 | preserved | production animation diagnostics/operations |
| numeric 112 | `RENDERER_STATE` | property 1 = nonzero resource epoch; property 2 = frame-active Boolean |
| image 1–2 | preserved | simple and PBR texture creation |
| text 1–9 | preserved | model loading plus named production-animation operations |

The next free IDs are numeric 113, image 3, and text 10.

The complete dispatch path remains shared and append-only:

- SMILE source calls the private `Graphics3D.Dispatch`, `DispatchImage`, or `DispatchText` wrapper.
- Native compilation maps the three syntax forms in `MasmEmitter.cs` to `smile_renderer3d_command`, `smile_renderer3d_image_command`, and `smile_renderer3d_text_command`.
- The native declarations and numeric ABI live in `graphics3d.h`; `graphics3d_directx.cpp` owns the Direct3D 11 switch and resource state.
- Web compilation maps the forms in `WebEmitter.cs` to `smile.renderer3D`, `smile.renderer3DImage`, and `smile.renderer3DText`.
- `WebOutputWriter.cs` owns the WebGL2 switches and mirrors command 112 exactly.

The resource epoch starts at 1, increments only on explicit Renderer3D reset, and wraps from 2,147,483,647 to 1. Device/context recreation does not advance the logical epoch when the logical handles survive. Frame-active reports actual low-level Begin/End ownership on both targets.

## Integrity, cleanup, and errors

Character3D records the current renderer epoch. An epoch change is a global reset: actor/cache mirrors are invalidated without attempting to destroy already-reset native/Web handles. With the same epoch, invalid actor parts or animators quarantine only that actor; an invalid shared model quarantines only actors sharing that asset. Unrelated actors and assets remain valid.

Cleanup uses checked object, animator, model, material, and light/frame helpers. The first renderer failure is captured before rollback or cleanup. A later cleanup failure is recorded separately. A zero-reference asset that cannot be destroyed enters a bounded pending-release state, blocks duplicate acquisition of that exact identity, and is retried explicitly by `RetryPendingReleases`. Shutdown is idempotent and reports unresolved pending releases rather than pretending that they were freed.

Global compatibility diagnostics remain, with actor-specific error, renderer-error, fallback, asset-state, variant, profile-key, reference-count, resident-byte, cached-asset, and pending-release queries added. Error 15 identifies an invalid/tampered actor part; reset, tamper, partial-transform, pending-release, and bounds failures have distinct codes.

## Transactional transforms and root motion

Place, rotation, scale, visibility, and LookAt validate inputs before mutation. Position is bounded to ±1,000,000 world units, rotation input to ±1,000,000 degrees, scale to 1–1,000 percent, and elapsed time to 0–2,147,483,647 ms. Yaw is normalized to 0–359 degrees.

Every multi-part transform snapshots all part mirrors, applies checked low-level mutations, and commits the actor mirror only after all parts succeed. A later-part failure restores every earlier part; a failed restore quarantines only that actor. The original renderer failure remains the primary diagnostic.

Production root deltas are model-local. Character3D rotates translation by the actor's pre-update yaw before applying the yaw delta:

```text
world X = local X * cos(yaw) + local Z * sin(yaw)
world Z = -local X * sin(yaw) + local Z * cos(yaw)
```

The yaw delta is then applied and normalized. The deterministic `RootMove` fixture now authors glTF negative Z so the converted SMILE model-forward direction is positive Z; tests cover forward motion at yaw 0, 90, 180, and 270 plus lateral motion.

## Cache, Scene3D, bounds, and interop

The cache identity is exact declared path, asset-affecting Scene3D profile key, and the actual PBR or simple-fallback variant. Request policy controls admission but is not part of the stored identity. Auto, Require-PBR, and Allow-Fallback therefore share the same PBR resource when PBR succeeds; Auto and Allow-Fallback share the same fallback resource when PBR is unavailable. Lighting-only changes do not duplicate assets. Require-PBR still fails when PBR cannot be prepared.

Scene3D refreshes capabilities after reset, records requested and effective quality independently, exposes fallback flags, and keeps the profile key limited to asset-affecting settings. Named lighting presets preserve the first failure; successful custom lighting clears stale errors. Begin/End synchronizes against actual low-level frame state, detects nested/unmatched/external ownership changes, and provides deterministic `ResetState` and `Shutdown` behavior.

`LocalBounds` reports the immutable model AABB. `WorldBounds` transforms all eight corners by actor scale, rotation, and translation and can add a bounded positive animation margin. `WorldHeight`, `WorldCenter`, `WorldRadius`, and component queries are explicit; the legacy `Height` remains local for compatibility. These are conservative static/model bounds rather than per-frame skinned bounds.

Primary and indexed part handles are borrowed, read-only interop values. Character3D retains ownership. Consumers must not destroy or retain them as owned resources; deliberate external destruction is detected as tampering and quarantines the affected actor.

## Current bounded ownership and limits

| Resource | Limit | Ownership |
|---|---:|---|
| Meshes | 128 live | Runtime pool; objects and models borrow/reference; destruction is refused while referenced. |
| Objects | 512 live | Caller-owned instances; borrow mesh, material, and optional animator handles. |
| Textures | 128 live | Caller/material/model owned; each texture retains its decoded image; destruction is refused while referenced. |
| Materials | 128 live | Caller or model owned; objects borrow; model-owned PBR materials cannot be independently destroyed. |
| Models | 64 live | Own up to 16 part meshes, 64 materials, 128 texture references/images, and immutable production-animation data. |
| SM3D model data | 16 MiB; 131,072 vertices; 393,216 indices | Converter and runtime enforce the same bounded, rollback-safe publication limits. |
| Legacy skeletons | 64 live; 32 bones each | Caller-owned; legacy animators borrow. |
| Legacy clips | 128 live; 16 events each | Caller-owned; legacy animators borrow. |
| Animators | 128 total | Caller/Character3D-owned mutable state; objects borrow. Production animators borrow their model. |
| Production animation | 256 nodes, 128 bones, 64 clips, 64 events/clip, 64 sockets, 32 pending events | Immutable data is model-owned; fixed mutable pose/palette/event/scratch data is animator-owned. |
| Character3D | 16 cached assets, 32 actors, 16 parts/actor | Cache owns models and fallback resources; actors own animators/part objects and borrow the cached model. |
| Dragonfall | 48 meshes; 441 initial/448 boss objects; 24 materials; 6 textures; 35 effects | Dragonfall owns its current procedural scene resources and releases them through its established lifecycle. |

## Deterministic fixtures

`scripts/generate-renderer3d-animation-v2-fixtures.ps1 -Check` regenerates and byte-compares the repository-owned articulated GLB, descriptor, missing-texture GLB, and copied SM3D outputs.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `AnimationArticulated.glb` | 8,124 | `6486513749EB00B8C3A4CA6B06357C4D7C2D31F7EDE9EA729C13D22DC76D4A6C` |
| `AnimationArticulatedMissingTexture.glb` | 8,304 | `32E5929689193FB4BA6087BBA6B42EECD5A8AC49DAA91C9BFB9105D016B68891` |
| `AnimationArticulated.sm3d.json` | 944 | `4220CED10CF45F34A845743E4AE90056290F133ECC7EF902D7415DF43E24E493` |
| `AnimationArticulated.sm3d` | 9,712 | `23B2571E40612FE39AE9F28B923B2BAEA1F610A95F47288D379EB7FE5B86329B` |
| missing-texture SM3D | 9,740 | `E8B8B7AB8DAF799A28C4957AD0373C32336A872A3F051E6F43337B9B9DAA9504` |

The normal articulated asset contains two parts, 32 vertices, 108 indices/36 triangles, two materials, no texture/image references, eight nodes/bones, five clips, eleven events, one socket, two root-motion clips, and 6,544 bytes of immutable animation payload.

## Validation results

Validation completed on 2026-09-01 (Asia/Taipei) with .NET SDK 10.0.400, Node.js 24.14.0, and Visual Studio 18 Enterprise.

| Command/gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` | Initial SDK-resolution failure at unavailable 10.0.302; PASS after the authorized 10.0.400 reconciliation. |
| `scripts\test-smile-formatter.ps1` | PASS; 13 focused formatter integration tests. |
| `scripts\format-smile-style.ps1 -Check -FormatLongIf` | PASS; 332 files. |
| `scripts\test-renderer3d-m11-hardening.ps1` | PASS in the repository's current PowerShell host. A diagnostic invocation through legacy `powershell.exe -File` exposed an existing null-valued harness incompatibility; it did not reproduce under the supported current host used by the gate. |
| `scripts\test-renderer3d-v2-boundaries.ps1` | PASS; exact 7,865,176-byte boundary plus over-limit rejection. |
| `scripts\test-renderer3d-models.ps1` | PASS; deterministic conversion, corrupt corpus, native/Web exact parity. |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS; native/Web lifecycle, counters, exhaustion, restart, and frame cycles. |
| `scripts\test-renderer3d-materials.ps1` | PASS; native/Web ownership/material parity. |
| `scripts\test-renderer3d-animation.ps1` | PASS; legacy animation native/Web exact parity. |
| `scripts\test-renderer3d-pbr.ps1` | PASS; PBR ownership, lights, samplers, diagnostics, lifecycle, and Lab builds. |
| `scripts\test-renderer3d-pbr-hardening.ps1` | PASS; native/Web failure/fallback/ownership/transform/skinning/lifecycle coverage. |
| `scripts\test-renderer3d-animation-v2.ps1` | PASS; deterministic fixtures, 128-bone boundary, production playback, lifecycle, and Lab builds. |
| `scripts\test-renderer3d-animation-v2-hardening.ps1` | PASS; timing, sampling, crossfade, root, overflow, compact memory, deformation, palette reuse, and lifecycle. |
| `scripts\test-character3d.ps1` | PASS; normal and forced-fallback native/Web exact parity plus Lab builds. The existing coherent gate was extended instead of adding a duplicate hardening script. |
| `scripts\test-battle3d.ps1` | PASS; native/Web exact parity. |
| `scripts\test-dragonfall.ps1` | PASS; mechanics, balance/lifecycle, demo/no-demo, native/Web builds. |
| `scripts\test-simple3d-space-wars.ps1` | PASS; Simple3D and Space Wars native/Web validation. |
| `scripts\verify-artifacts.ps1` | PASS; library, native GUI, game assets, VSIX payload/version, viewport, and DPI checks. |
| `cmd /c scripts\smoke-test.cmd` | First run stopped on the stale 10.0.302 doctor expectation. The next run passed all code/game gates and exposed a stale 2.0.51 template verifier. After both exact consistency fixes, the complete clean rerun passed: 288 managed tests, 13 formatter groups, 332-file style gate, 39 native graphics/audio checks, 44 native Text checks, all retained native/Web packages and games, artifact verification, and VSIX 2.0.52 verification. |
| `git diff --check` | PASS; only checkout line-ending notices. |

Read-only reconciliation and evidence commands also used `git status`, `git rev-parse`, `git rev-list --left-right --count`, `git diff`, `rg`, `Get-Content`, `Get-ChildItem`, `Get-FileHash`, `smileasset inspect`, safe ZIP entry validation/extraction beneath `artifacts/temp/codex-handoff`, and package-manifest SHA-256 comparison. No downloaded executable was run.

## Manual native and Web evidence

The updated Character3D Lab exposes yaw, world center/radius, resource epoch/reset count, actor error, shared counts, draw/triangle counts, PBR state, and renderer error. Left/Right changes facing, Tab selects `RootMove`, W applies facing-aware root motion, S deliberately destroys one borrowed part, Enter performs explicit reset recovery, and Down reloads actors.

- Native: yaw/root motion and world bounds updated coherently; all parts stayed aligned. Borrowed-part tamper displayed actor error 15, removed only the affected actor, and retained the unrelated actor/shared model. Reset advanced epoch 2→3, restored two actors/one asset, and cleared errors. The sampled recovered frame was 8 ms / 125 FPS with six draws and 554 triangles.
- Web: the same tamper and reset sequence produced the same counts and visible result. `dev.logs()` returned an empty list before, during, and after the sequence.
- Evidence: `artifacts/screenshots/m4-1-character3d-native-root-world-bounds.jpg`, `m4-1-character3d-native-tamper-diagnostic.jpg`, `m4-1-character3d-native-reset-recovery.jpg`, `m4-1-character3d-web-tamper.png`, and `m4-1-character3d-web-reset-recovery.png`.

## Resource and performance measurements

The recovered native and Web Lab frame measured:

```text
Character3D actors:             2
cached assets:                  1
pending-release assets:         0
meshes:                         4 (two model parts, ground, socket marker)
models:                         1
animators:                      2
objects:                        6
materials:                      2
textures/images:                0 / 0
animation payload/resident:     6,544 / 6,544 bytes for the aligned fixture chunks
resource epoch:                 3 after one visible reset
frame active after EndScene:    0
logical/main draws:             6
submitted triangles:            554
character-only draws/triangles: 4 / 72
palette uploads after two poses: 2
frame sample:                   8 ms / 125 FPS
teardown:                       0 actors, assets, pending releases, meshes, models,
                                animators, objects, materials, textures, and active frame
```

This was a brief manual observation, not a benchmark or soak.

## Plan deviations and limitations

1. The handoff's expected start was the actual start; no descendant reconciliation or reset was needed.
2. Command 112 was genuinely free and was the only ABI addition.
3. The coherent existing `test-character3d.ps1` gate was extended rather than adding `test-character3d-hardening.ps1`.
4. The converter's handedness reflection required authoring the GLB root-motion endpoint on glTF negative Z to produce SMILE model-forward positive Z.
5. The installed SDK required the authorized repository pin upgrade to 10.0.400 and matching doctor/README updates.
6. Bounds remain conservative transformed static/model bounds plus an optional animation margin; exact per-frame skinned bounds are intentionally not implemented.
7. Borrowed part handles are intentionally exposed for interop but remain unsafe to destroy; tamper detection provides containment, not permission.

## VSIX and next milestone

The repository installer rebuilt and installed `artifacts/vsix/Smile.VisualStudio.vsix` into Visual Studio instance `91f001b5`.

| Item | Value |
|---|---|
| VSIX/product version | 2.0.52 |
| assembly version | 2.0.52.0 |
| built VSIX SHA-256 | `23FCD32CDA7878C45C6BF6AA22228D96B3BE6B356DA79E0DC9CADA279D0A8CAF` |
| installed DLL SHA-256 | `71B8642D1406887DF4D8CF961B628C11E3F25C5C5281DF93F0106494A008FF71` |
| installed DLL | `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\jm4bbnbm.wwu\Smile.VisualStudio.dll` |
| verification | PASS through installer and explicit `verify-vsix-install.ps1` |
| restart required | Yes, Visual Studio must restart to load the refreshed extension. |

M5 is unblocked after the focused M4.1 commit containing this report is pushed and the remote hash is verified.
