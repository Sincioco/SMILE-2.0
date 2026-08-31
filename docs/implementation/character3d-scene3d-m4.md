# Character3D and Scene3D M4 implementation report

## Reconciliation

M4 began on `main` at `c47837ad522d2501cf579c231c28f2b13f63b736`, with `origin/main` at the same commit and only the user-owned untracked `docs/plans/` directory present. That commit is the pushed M3.1 descendant required by the handoff. No reset, rebase, force push, or unrelated-work discard was performed.

The M4 handoff was prepared against M3 plus the required M3.1 descendant. Reconciliation confirmed that M3.1 had already extended the actual ABI beyond the older architecture page, added production animation diagnostics and event clearing, hardened the articulated fixture, and shipped VSIX 2.0.50. M4 builds on those exact resources rather than duplicating them.

## Result

`Smile.Simple3D.Scene3D` and `Smile.Simple3D.Character3D` are ordinary source-library modules. They add no statement grammar, compiler intrinsic, native runtime resource, Web runtime resource, or renderer command.

Scene3D provides:

- deterministic Auto/Low/Medium/High profiles for new assets;
- exact named `CharacterStudio`, `Daylight`, `Dungeon`, `Moonlight`, and `EmberObservatory` presets;
- a custom-light escape hatch;
- balanced begin/end state with nested and unmatched rejection;
- stable profile keys and high-/low-level error diagnostics.

Character3D provides:

- a 16-entry exact path/profile/policy cache and 32 generation-safe actors;
- one shared immutable model with independent animator and part objects per actor;
- atomic creation rollback and dependency-ordered destruction;
- exact named playback/crossfade, FIFO events, world sockets, bounds, visibility, and yaw-only LookAt;
- combined root-delta consumption once per update with thousandths accumulation;
- explicit advanced object/animator/model handles for Battle3D without a Battle3D dependency;
- renderer-reset detection that invalidates high-level ownership without double destruction.

No resource loading or creation occurs in `Character3D.Update` or `Character3D.Draw`.

## Renderer3D ABI and dispatch

M4 preserves every existing command and appends none:

| Bridge | Current occupied range | Next free ID |
|---|---:|---:|
| Numeric `Renderer3D` | 1-111 | 112 |
| Image-owning `Renderer3DImage` | 1-2 | 3 |
| Text/path `Renderer3DText` | 1-9 | 10 |

Numeric 110 remains the multiplexed production-animator query and numeric 111 remains atomic animator event-state clearing. Existing numeric 98 properties 11-13 remain source file bytes, resident animation bytes, and mutable bytes per animator. M4 calls those established surfaces only through `Graphics3D`.

Authoritative paths remain:

| Responsibility | Path |
|---|---|
| Public low-level SMILE facade and command constants | `libraries/Smile.Simple3D/Graphics3D.smile` |
| High-level M4 ownership | `libraries/Smile.Simple3D/Scene3D.smile`, `libraries/Smile.Simple3D/Character3D.smile` |
| Shared intrinsic validation | `src/Smile.Language` |
| Native emission and text dispatch | `src/Smile.Compiler`, `src/Smile.NativeRuntime/runtime.c` |
| Native numeric/image D3D11 dispatch | `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` |
| Generated Web dispatch and WebGL2 renderer | `src/Smile.Compiler/WebOutputWriter.cs` |

## Limits and ownership

| Resource | Limit and M4 ownership |
|---|---|
| Character cache | 16 entries; exists only while referenced by an actor |
| Character actors | 32 generation-safe slots |
| Parts per character | 16, matching the SM3D model limit |
| Renderer meshes | 128 global; model-owned part meshes |
| Renderer objects | 512 global; each actor owns one object per part |
| Renderer models | 64 global; one cache entry owns one model |
| Textures | 128 global; prepared PBR textures are model-owned |
| Materials | 128 global; PBR materials are model-owned, or one cache-owned neutral fallback material |
| Legacy skeletons | 64 global, 32 bones each; not owned by Character3D |
| Legacy clips | 128 global, 16 events each; not owned by Character3D |
| Animators | 128 total; each actor owns one production model animator |
| Production model | 16 parts, 131072 vertices, 393216 indices, 64 materials, 128 texture references, 16 MiB |
| Production animation | 256 nodes, 128 bones, 64 clips, 64 events per clip, 64 sockets, FIFO 32 |
| Dragonfall | unchanged: 48 meshes, 441 initial/448 boss objects, 24 materials, 6 textures, 35 effect objects, no models or animators |

Destruction order is part objects, animator, actor slot, cache reference, fallback material if present, then model. Direct renderer reset is a global ownership boundary.

## Quality and lighting policy

| Profile | Filter | Requested anisotropy | Local-light limit | Capability-only simple fallback |
|---|---|---:|---:|---|
| Low | Linear | 1 | 1 | Yes |
| Medium | Mip-linear | 4 | 2 | Yes |
| High | Anisotropic | 8 | 4 | Explicit policy only |
| Auto | High when PBR is available; Low otherwise | 8 or 1 | 4 or 1 | Yes when PBR is unavailable |

The effective profile is part of cache identity. Hardware may clamp requested anisotropy through the existing PBR texture contract. Quality changes affect only new/reloaded actors.

Preset source values are exact integer `Graphics3D` inputs. Ambient and directional entries are `RGB @ intensity`; directional vectors precede their color. Local entries are position, `RGB @ intensity`, and range. Low/Medium/High apply at most the first 1/2/4 local slots.

| Preset | Ambient | Directional | Local slots in deterministic order |
|---|---|---|---|
| CharacterStudio | `54,62,82 @ 34` | `-3,-7,4`; `255,242,224 @ 125` | Point `260,240,-180`; `255,174,112 @ 150`; `850`. Point `-280,150,40`; `112,174,255 @ 105`; `700`. |
| Daylight | `166,194,226 @ 42` | `-4,-8,3`; `255,249,232 @ 130` | Point `-220,180,-120`; `180,214,255 @ 65`; `900`. |
| Dungeon | `22,28,42 @ 20` | `2,-7,5`; `116,142,188 @ 72` | Point `230,90,-80`; `255,92,36 @ 165`; `620`. Point `-260,60,100`; `60,104,188 @ 90`; `540`. |
| Moonlight | `48,64,108 @ 28` | `3,-8,5`; `158,190,255 @ 96` | Point `-180,120,80`; `86,132,255 @ 72`; `760`. |
| EmberObservatory | `62,30,20 @ 26` | `-3,-7,4`; `255,140,74 @ 92` | Point `220,100,-100`; `255,72,22 @ 185`; `700`. Point `-240,160,90`; `255,174,62 @ 125`; `760`. Spot `0,300,-260`; `108,154,255 @ 115`; `900`; direction `0,-3,5`; cone `18/38`. |

## Deterministic fixtures

The M3.1 articulated generator remains authoritative and now also publishes the fixture into the Character3D test and Lab projects. It additionally creates an animated missing-texture variant so Character3D proves that content failure is not silently converted to a simple fallback.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `AnimationArticulated.glb` | 8124 | `8363BA089E3CE25AB4D0ECA56D131CBB05E9CEC7030F57982EEFA6CDF7D8BBFF` |
| `AnimationArticulated.sm3d` | 9712 | `258B03E0EE9DF0811F3F7AB02E70B07582ABA7D71CE97D4D9BD9ADCA6FC9092C` |
| `AnimationArticulatedMissingTexture.glb` | 8304 | `BBF445057005E2B4EAC22DD03E9DFF1C58257ABAA55DB10236849D906E0A6532` |
| `AnimationArticulatedMissingTexture.sm3d` | 9740 | `9DDFD69630C583B46F34F07F56ADC759224C71FA104788D4B031E496308C9F8B` |

## Plan deviations

1. SMILE reserves `Load`, `Play`, `Stop`, and `End`, so ordinary modules cannot declare the handoff’s exact keyword-shaped member names. M4 uses `LoadActor`, `PlayAnimation`, `StopAnimation`, and `EndScene`. Expanding the shared language/parser was deliberately excluded from this graphics milestone.
2. The M3.1 articulated fixture contains `Idle`, `Bend`, `WalkLike`, `AttackLike`, and `RootMove`, not the illustrative `Hit` and `Victory` names. The Lab exposes the real five clips and does not invent animation.
3. Part-creation rollback is implemented for part zero and every later part, but the focused test uses deterministic object-capacity preflight rather than adding a runtime-only failure-injection command. No command or production test hook was added.
4. M4 does not add shadows, HDR, tone mapping, bloom, IBL, particles, IK, physics, or automatic battle damage. Those remain later milestones.

## Validation and visual evidence

`scripts/test-character3d.ps1` passes exact native/Web output in normal PBR and forced PBR-unavailable modes. It covers cache sharing/release, independent animators/parts, high-level actor capacity, object and animator preflight, missing texture, exact profiles/lights, named clips/events/sockets, root accumulation, draw/triangle/palette counts, stale handles, renderer reset, idempotent shutdown, and native/Web Lab builds.

For two actors sharing one two-part model, the focused frame records one model, two animators, four character part objects, four character draws, 72 submitted character triangles, and two palette uploads after two changed poses. Update and Draw contain no load/create call. The complete Lab frame adds one ground and one socket marker for six draws and 554 triangles. Brief native and Web visual checks both displayed an 8 ms / 125 FPS sampled frame with no browser warnings or errors; no long benchmark or soak was run.

Screenshots are retained as build artifacts:

- `artifacts/screenshots/character3d-m4-native.jpg`
- `artifacts/screenshots/character3d-m4-web.png`

The final validation result is:

| Gate | Result |
|---|---|
| `scripts/test-smile-formatter.ps1` | 13 integration tests passed |
| `scripts/format-smile-style.ps1 -Check -FormatLongIf` | 328 files passed |
| `dotnet run --project src/Smile.Tests/Smile.Tests.csproj -c Release --no-restore` | 288 tests passed |
| Renderer3D M1.1, v2 boundaries, models, lifecycle, materials, animation, PBR, PBR hardening, animation-v2, and animation-v2 hardening focused gates | Passed |
| `scripts/test-character3d.ps1` | Native/Web normal and forced-fallback parity plus Lab builds passed |
| Battle3D, Dragonfall, and Simple3D/Space Wars focused gates | Passed |
| `scripts/smoke-test.cmd` | Passed, including 39 native graphics/audio checks and 44 native Text checks |
| `scripts/verify-artifacts.ps1` through the smoke suite | Passed; VSIX payload synchronized at 2.0.51 |
| Native/Web Character3D Lab visual checks | Passed; 8 ms / 125 FPS sample, six draws, 554 triangles, Web console clean |

The repository installer refreshed Visual Studio Enterprise instance `91f001b5` and verified installed VSIX version `2.0.51`, assembly version `2.0.51.0`, and installed `Smile.VisualStudio.dll` SHA-256 `34D02FF8A60613016419FF683D3672DA703108F024B5DC021F11978D20227E95`. The built VSIX SHA-256 is `D0E51CC819B25D5736279B5EB9759CBDC53CD911D0D02FE7209397045425C873`.
