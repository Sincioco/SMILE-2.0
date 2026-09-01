# Renderer3D VFX Generation 2 — M6

Status: implemented and validated on Windows native and Web on 2026-09-01.

Starting baseline: `8b8fbe1bd095652a56a4657fd9b9f1c3caf59689` on `main`, equal to `origin/main`. The pre-existing untracked `docs/plans/` tree was preserved and is not part of M6. This milestone extends the M5.1 immutable tagged submission queue; it does not implement or begin M7.

## Reconciliation and plan mapping

The current branch already contained the complete M0-M5.1 line. In particular, M5.1 reserved tagged queue kind 2 for particle batches and kind 3 for ribbon batches, provided atomic nonnested submission groups, captured immutable material/texture state, and blocked mutation or destruction of in-flight resources. M6 consumes those seams directly rather than adding a render graph or a second renderer.

The handoff maps to the repository as follows:

- `Smile.Simple3D.Graphics3D` owns the thin public batch facade and diagnostics.
- native Direct3D 11 owns fixed batch pools, CPU staging arrays, dynamic GPU buffers, instanced particle submission, and ribbon-strip submission;
- the generated Web runtime owns matching fixed Maps/typed arrays, WebGL2 dynamic buffers, instanced particles, and ribbon strips;
- `Smile.Simple3D.Effects3D` owns deterministic fixed-step simulation, preset composition, quality scaling, bounded effect/particle/impulse pools, and three renderer batch handles;
- the calling application owns the atlas image, texture, three materials, scene lights, audio playback, and any `Character3D.Actor` used for socket attachment;
- the existing Battle3D and Dragonfall Generation 1 paths remain unchanged.

No SM3D format, PBR, animation, light-pool, audio runtime, VFX graph, compute shader, WebGPU, or Dragonfall gameplay change is included.

## Command ABI and dispatch paths

The exact public command ranges after M6 are:

- numeric `Renderer3D`: 1-121 inclusive; next free numeric command is 122;
- image `Renderer3DImage`: 1-2 inclusive; next free image command is 3;
- text `Renderer3DText`: 1-9 inclusive; next free text command is 10.

Numeric commands 1-118, image commands 1-2, and text commands 1-9 retain their M5.1 meanings. M6 adds only numeric commands 119-121.

The dispatch route is unchanged: `libraries/Smile.Simple3D/Graphics3D.smile` calls the shared-language built-ins. Native lowering in `src/Smile.Compiler/MasmEmitter.cs` targets `smile_renderer3d_command`, `smile_renderer3d_image_command`, and `smile_renderer3d_text_command`; ABI declarations live in `src/Smile.NativeRuntime/graphics/graphics3d.h`; numeric/image Direct3D dispatch lives in `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp`; native text/asset resolution enters through `src/Smile.NativeRuntime/runtime.c`. Web lowering in `src/Smile.Compiler/WebEmitter.cs` targets `smile.renderer3D`, `smile.renderer3DImage`, and `smile.renderer3DText`; the WebGL2 implementation and all dispatch switches live in `src/Smile.Compiler/WebOutputWriter.cs`.

### Numeric command 119: `PARTICLE_BATCH`

| Operation (`a`) | Arguments | Result |
| ---: | --- | --- |
| 1, create | `b=capacity, c=material, d=billboard mode, e=atlas columns, f=atlas rows` | typed particle-batch handle or 0 |
| 2, set transform/frame | `b=batch, c=index, d-f=XYZ, g=size, h=rotation degrees, i=zero-based atlas frame` | success |
| 3, set color | `b=batch, c=index, d-f=RGB, g=opacity percent` | success |
| 4, commit | `b=batch, c=particle count` | success; publishes one staged prefix/revision |
| 5, draw | `b=batch` | success; captures immutable kind-2 submission |
| 6, destroy | `b=batch` | success unless in flight |
| 7, valid | `b=batch` | handle validity |

Billboard mode 1 is camera-facing and mode 2 is vertical. Atlas dimensions are 1-16 by 1-16. Particle size must be positive; color and opacity use the existing 0-255 and 0-100 conventions.

### Numeric command 120: `RIBBON_BATCH`

| Operation (`a`) | Arguments | Result |
| ---: | --- | --- |
| 1, create | `b=capacity, c=material` | typed ribbon-batch handle or 0 |
| 2, set point | `b=batch, c=index, d-f=left XYZ, g-i=right XYZ, j=U thousandths` | success |
| 3, set color | `b=batch, c=index, d-f=RGB, g=opacity percent` | success |
| 4, commit | `b=batch, c=point count` | success; publishes one staged prefix/revision |
| 5, draw | `b=batch` | success; captures immutable kind-3 submission |
| 6, destroy | `b=batch` | success unless in flight |
| 7, valid | `b=batch` | handle validity |

### Numeric command 121: `M6_VALUE`

`a` is the query and `b` is an optional generation-safe resource handle.

| Query | Meaning |
| ---: | --- |
| 1-4 | live/max particle batches, live/max ribbon batches |
| 5-8 | staged/max particle capacity, staged/max ribbon-point capacity |
| 9-10 | committed active particle and ribbon-point counts |
| 11-13 | VFX draw calls, triangles, and dynamic uploads in the current/most recently ended frame |
| 14-15 | currently reserved VFX CPU and GPU bytes |
| 16 | rejected VFX operations since reset |
| 17-18 | particle and ribbon draw calls |
| 19 | batches retained by in-flight submissions |
| 20-21 | accepted particle and ribbon submissions |
| 22-23 | particle and ribbon triangles |
| 30-36 | resource capacity, count, revision, CPU bytes, GPU bytes, in-flight count, and material handle |

The established general diagnostics at commands 78-79 remain the authoritative whole-frame draw and triangle totals. M6 adds the smallest reusable VFX breakdown needed to distinguish batch count, upload count, particle/ribbon submissions, and their triangle cost.

## Resource limits and ownership

Native kind-tagged handles use generations; Web uses monotonically increasing safe-integer handles and Maps. Both reject stale or missing handles and enforce the same dependency order.

| Resource | Limit | Ownership and lifetime |
| --- | ---: | --- |
| meshes | 128; 65,535 vertices and 196,608 indices each | caller-created or model-owned; objects and immutable frame snapshots retain them |
| objects | 512 | caller-owned; refer to mesh/material/animator but own none |
| models | 64 | own up to 16 part meshes and any prepared imported textures/materials |
| textures | 128; up to 8,192 by 8,192 | caller- or model-owned; materials and in-flight snapshots retain them |
| materials | 128 | caller- or model-owned; objects and VFX batches retain them; immutable submissions copy factors |
| skeletons | 64 | caller-owned; referenced by clips and legacy animators |
| clips | 128 | caller-owned; referenced by playing animators |
| animators | 128 total | caller-owned; objects refer to them and frame palettes snapshot their poses |
| model animation | 256 nodes, 128 bones, 64 clips, 64 sockets, 32 pending events | model/animator-owned fixed production-animation storage |
| frame submissions | 512 | renderer-owned fixed immutable tagged records |
| frame palettes | 512 | renderer-owned fixed 128-matrix snapshots |
| particle batches | 16; 1-4,096 particles each; 8,192 staged particles total | caller-owned batch; borrows one simple alpha/additive material; owns fixed CPU staging and one dynamic GPU instance buffer |
| ribbon batches | 16; 2-1,024 points each; 2,048 staged points total | caller-owned batch; borrows one simple alpha/additive material; owns fixed point/vertex staging and one dynamic GPU vertex buffer |
| Effects3D presets | 64, with up to 8 emitter layers each | Effects3D-owned fixed definitions |
| Effects3D active effects | 64 | Effects3D-owned generation-safe slots |
| Effects3D particles | quality Low 256, Medium 1,024, High 2,048 | Effects3D-owned fixed simulation slots; reservation is atomic per composed spawn |
| Effects3D impulses | 32 | Effects3D-owned fixed camera-shake slots |

A particle instance is 48 CPU/GPU bytes. A ribbon point reserves 44 bytes of semantic CPU staging plus two 36-byte vertices, or 116 CPU bytes and 72 GPU bytes. The particle quad adds one fixed 64-byte vertex buffer and 12-byte index buffer. Capacity accounting is exposed rather than inferred.

Committed batches are revisioned. Mutating, committing, or destroying a batch while an accepted frame submission retains it fails cleanly. Submission capture copies the material state and retains the texture plus batch revision; `End3D`, rollback, reset, and device/context-loss cleanup release all references. Materials cannot be destroyed while a batch borrows them. `ResetRenderer3D` destroys particle/ribbon buffers before materials and textures and advances the renderer resource epoch, which `Effects3D` detects.

Dragonfall remains on its bounded Generation 1 implementation: its current scene declares 35 ordinary `Object3D` particle slots and owns their template/material/texture lifecycle. M6 does not silently redirect or migrate those effects. Battle3D similarly receives no game-specific native helper.

## Rendering architecture

Native particles use one immutable unit quad and `DrawIndexedInstanced`; Web uses the matching `drawElementsInstanced`. Instance attributes carry position, size, rotation, atlas frame, and color. Native uploads with `D3D11_MAP_WRITE_DISCARD`; Web uses `bufferSubData` into preallocated typed arrays. Ribbons expand retained left/right points into one dynamic triangle strip and render with native `Draw` or Web `drawArrays`.

Both paths support camera-facing and vertical billboards, 4x4 lab atlas animation, alpha and additive blending, depth test with depth writes disabled, HDR/bloom composition, and direct-LDR fallback. VFX does not cast or receive shadows. Particle and ribbon submissions use the M5.1 immutable tagged queue and participate in the existing alpha ordering and atomic group protocol. No per-draw array, typed-array, buffer, shader, or pipeline allocation is performed on the Web hot path.

## Effects3D contract

`Smile.Simple3D.Effects3D` is deterministic and allocation-free after initialization:

- fixed 10 ms simulation steps, maximum accepted update of 250 ms, and at most 25 catch-up steps;
- deterministic LCG stream `Seed = (Seed * 25173 + 13849) Mod 65536`;
- delayed particles, interval spawning, gravity, drag, size/color/opacity interpolation, rotation, atlas animation, alpha/additive partitions, optional ribbon trail, flash, camera shake, transient light request, and numeric audio cue;
- quality-scaled emitter counts and capacities of 256/1,024/2,048;
- atomic all-or-nothing reservation across every emitter layer in a composed effect;
- generation-safe `Effect` values and explicit `StopEffect` cleanup;
- `SpawnAtSocket` and `MoveToSocket` through the existing `Character3D` socket API;
- one atomic three-command draw group for alpha particles, additive particles, and ribbon;
- counters for fixed steps, clamped/dropped time, dropped effects/particles, committed partitions, active/reserved particles, and last library/renderer errors;
- renderer-epoch detection that invalidates the library cleanly after an external renderer reset.

The six standard presets are Holy Sword Strike, Shield Impact, Fire Burst, Frost Burst, Heal Spiral, and Dragon Fire Breath. Fire Burst composes an additive fire emitter with an alpha smoke emitter; the fixed composition table supports up to eight layers for user presets.

Transient light and audio requests are intentionally one-slot, newest-wins messages. The caller remains the owner of Scene3D lighting and audio playback. Effects3D reports the request but never steals or silently resets those systems.

## Teaching recipes

### Play an effect at a position

```smile
Dim Burst As Effects3D.Effect

Burst = Effects3D.Spawn(Effects3D.PRESET_FIRE_BURST, 0, 120, 0, 401)
```

### Attach and move an effect with a character socket

```smile
Dim Holy As Effects3D.Effect

Holy = Effects3D.SpawnAtSocket(
    Effects3D.PRESET_HOLY_SWORD_STRIKE,
    Hero,
    "HandTip",
    911
)

Call Effects3D.MoveToSocket(Holy, Hero, "HandTip")
```

### Keep and stop a sword trail

Spawn the Holy Sword Strike at `HandTip`, call `MoveToSocket` after each actor update, then stop it explicitly:

```smile
Call Effects3D.StopEffect(Holy)
```

`StopEffect` is the public spelling because `Stop` is an existing SMILE keyword.

### Apply flash, shake, light, and audio without transferring ownership

```smile
Flash = Effects3D.FlashOpacity()
Camera.Position.X = Camera.Position.X + Effects3D.CameraShakeX()
LightRequest = Effects3D.TakeTransientLight()
AudioRequest = Effects3D.TakeAudioCue()
```

Check each request's `Available` field before applying it through the scene or game audio owner.

### Define and compose a custom preset

Fill an `Effects3D.EffectPreset`, call `DefinePreset`, then optionally add already defined emitters with `AddEmitterLayer`. The combined quality-scaled reservation must fit the active quality pool; otherwise spawn returns an invalid effect and increments the bounded drop diagnostics without partially spawning.

## Deterministic fixture and lab

`scripts/generate-renderer3d-vfx-fixtures.ps1` creates the repository-owned 256x256 `examples/Renderer3DVfxLab/Assets/VfxAtlas.png`. `-Check` regenerates the expected bytes in memory and rejects drift. The 4x4 atlas SHA-256 is `f76344de74be7d306d38540dc578c294e0f61b0fa651b4dffa8b9447c3acd57d`.

`Renderer3DVfxLab` compiles natively and for Web. It shows an articulated Character3D actor, six selectable presets, socket attachment, alpha/additive particles, a dynamic sword ribbon, HDR/bloom and direct-LDR states, a 1,024-particle stress toggle, and live capacity/draw/triangle/drop diagnostics.

The repository currently exposes key constants for `1`-`4`, arrows, Space, Enter, Escape, and WASD, but not `5`, `6`, `H`, `B`, or `R`. The lab therefore uses `1`-`4`, Left/Right plus Space for all six presets, `S` for stress, `A` for HDR/direct LDR, `W` for bloom, `D` for diagnostics, Enter for VFX reset, and Escape to exit. No language-key addition was justified for this milestone.

Screenshots:

- `artifacts/screenshots/m6-native-vfx-lab-hdr.png`
- `artifacts/screenshots/m6-native-vfx-lab-stress.png`
- `artifacts/screenshots/m6-native-vfx-lab-direct-ldr.png`
- `artifacts/screenshots/m6-web-vfx-lab-hdr.png`
- `artifacts/screenshots/m6-web-vfx-lab-direct-ldr.png`

## Validation

The final M6 validation set is:

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
.\scripts\test-renderer3d-vfx-batches.ps1
.\scripts\test-effects3d.ps1
.\scripts\test-battle3d.ps1
.\scripts\test-dragonfall.ps1
.\scripts\test-simple3d-space-wars.ps1
.\scripts\test-true-simple3d-neon-cycles.ps1
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release
cmd /c scripts\smoke-test.cmd
cmd /c scripts\install-vsix.cmd
```

The focused M6 gate regenerates/checks the atlas, checks ABI and bounded-storage contracts, compiles/runs native tests, compares exact native/Web console output, checks both generated JavaScript files, compiles both lab targets, rejects Web draw-path allocation regressions, and includes a 1,024-instance path. `test-effects3d.ps1` additionally covers deterministic seed state, time partition parity, delayed/rate spawning, quality scaling, composed emitters, capacity exhaustion, stop/reuse, renderer reset, and exact native/Web parity.

Final results:

| Gate | Exact result |
| --- | --- |
| `scripts\build.cmd` | PASS, exit 0; compiler, AssetTool, native runtime/tests, managed tests, and VSIX built. The only warning is the established `NU1503` restore skip for the native `.vcxproj`; native MSBuild succeeds. |
| formatter gates | PASS; 13 focused formatter integration groups and repository style conformance for 334 SMILE files |
| M1.1, SM3D v2 boundaries/models, lifecycle, materials, and legacy animation | PASS; all native/Web exact-parity and deterministic fixture checks |
| PBR and PBR hardening | PASS; native/Web materials, lights, failure/fallback, ownership, transform, skinning, and lifecycle checks |
| animation v2 and hardening | PASS; native/Web 128-bone import/playback/crossfade/events/root motion/sockets plus fractional/irregular/hardening checks |
| Character3D/Scene3D | PASS; native/Web cache, ownership, atomicity, profiles, animation, sockets, rendering, reset, fallback, and Lab builds |
| M5 post-processing and M5.1 hardening | PASS; native/Web queue/snapshot/group, shadow, target transaction, color, and allocation-free hot-path checks |
| `test-renderer3d-vfx-batches.ps1` | PASS; exact native/Web parity, queue/lifecycle, 1,024-instance, HDR/direct-LDR, and Web hot-path checks; final reported native runtime 934 ms |
| `test-effects3d.ps1` | PASS; deterministic seed, partition, timed spawning, quality, composition, exhaustion, stop/reuse, reset, and exact native/Web parity |
| Battle3D | PASS native/Web validation |
| Dragonfall | PASS native/Web mechanics, lifecycle, demo, no-demo, balance, program, and asset validation |
| Simple3D/Space Wars | PASS package/state/gallery and native/Web demo/no-demo validation |
| True Simple3D/Neon Cycles | PASS conformance, state, and native/Web validation |
| managed suite | PASS; 288 language, compiler, project, completion, and timing tests. Printed synthetic diagnostics are intentional negative cases. |
| `scripts\smoke-test.cmd` | PASS, exit 0; includes environment/toolchain, full build, managed, formatter, 39 native graphics/audio-focus checks, 44 native Text checks, application/library/game matrices, artifact verification, and all seven demo/no-demo Web builds |
| `scripts\verify-artifacts.ps1` | PASS; packaged libraries/games, project templates, compiler/shared-language payload, VSIX version, viewport, and DPI checks |
| `scripts\install-vsix.cmd` | PASS; refreshed Visual Studio Enterprise instance `91f001b5` and verified installed VSIX 2.0.55 |

Corrective validation findings were retained rather than hidden:

- the first grouped boundary run reported a wrapper failure because an expected over-limit converter left native process exit code 2 in `$LASTEXITCODE`; both gates had passed, and rerunning with PowerShell script-success semantics passed the complete group;
- the first M5 post gate found `new Float32Array` inside its audited Web frame-source region; moving the two VFX creation helpers before `renderer3DBegin` preserved initialization-only allocation and made both M5 and M6 hot-path guards pass;
- the first managed run found four stale 2.0.54 assertions/references after the M6 version bump; both project-template wizard references and their managed assertions were synchronized to 2.0.55, then all 288 tests passed;
- the first smoke run reached final artifact verification and found one additional stale 2.0.54 verifier expression; after synchronizing it, focused artifact verification and a complete fresh smoke rerun both passed with exit 0.

## VSIX

The extension and bundled compiler/runtime/library payload version is 2.0.55. The required installation completed automatically:

- VSIX: `artifacts\vsix\Smile.VisualStudio.vsix`
- VSIX bytes/SHA-256: 1,733,787 / `6BB7ABEFC870974A307B156BC9064A0BD8B94C130FE3B22E7D755E277D5BC240`
- built and installed DLL SHA-256: `433CA864D0F1A96286CB0F5FFAB64523FC71DE87BD9BAAA056271816C033D611`
- installed DLL: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\dxxlwp22.jdd\Smile.VisualStudio.dll`
- installed assembly version: 2.0.55.0
- Visual Studio restart required: yes

The five final screenshots are 962x572 native and 1248x720 Web captures from the validated build. Native direct LDR and Web direct LDR visibly report `HDR / Bloom / Format = 0 / 0 / 0`; the HDR captures report `1 / 1 / 1`. The native stress capture reports 1,480 active particles (the normal effect population plus the 1,024-particle stress burst), three VFX draws, and 3,042 VFX triangles. The Web console is empty.

## Plan deviations and M7 readiness

| Handoff assumption | Current repository fact and M6 decision |
| --- | --- |
| Effects3D can load and own an atlas from a reusable library asset | `.smilelibproj` libraries do not publish application runtime assets. `Initialize` accepts three application-owned effect materials; the lab project publishes/owns the atlas, texture, and materials. |
| Public effect stop can be named `Stop` | `Stop` is a SMILE keyword. The clear public API is `StopEffect`. |
| Lab controls can use `5`, `6`, `H`, `B`, and `R` | Those key constants do not exist. Existing portable keys provide the complete lab control set without widening the language/runtime input ABI. |
| Effects can directly own lights and audio | Existing Scene3D and game-audio ownership remains authoritative. Effects3D emits bounded transient requests for the caller to consume. |
| M6 may migrate Dragonfall | The milestone explicitly requires reusable infrastructure first. Dragonfall's 35-object Generation 1 pool remains unchanged and is a future explicit migration decision. |

M7 is unblocked after this milestone is committed, pushed, and remote-verified. The next milestone can consume commands 119-121, the fixed batch pools, and Effects3D without changing the SM3D v2, PBR, animation, M5.1 snapshot, or Dragonfall Generation 1 contracts.
