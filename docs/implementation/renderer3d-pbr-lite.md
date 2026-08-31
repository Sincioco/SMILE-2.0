# Renderer3D M2 PBR-Lite Materials and Lighting

## Status and reconciliation

M2 started from `ba46ab37c3400560e75ee880cf2f524ea6523214` on `main`, with `origin/main` at the same commit. That commit is the separately validated and pushed M1.1 milestone. The pre-existing untracked `docs/plans/` input remains untouched and excluded from this milestone.

The current branch already contained the M0 diagnostics, SM3D v2 converter/loader, and M1.1 hardening described in the preceding implementation records. M2 extends those paths; it does not introduce SM3D animation chunks, new animation semantics, shadows, HDR, tone mapping, bloom, image-based lighting, a render graph, or any M3 VFX work.

## Public result

`Smile.Simple3D.Graphics3D` now exposes a bounded PBR-lite API on both Direct3D 11 and WebGL2:

- color and data textures with explicit filter, wrap, and anisotropy requests;
- metallic/roughness PBR materials with base-color, normal, ORM, and emissive maps;
- opaque, alpha-mask, and straight-alpha blend modes plus double-sided state;
- ambient and directional light plus four point/spot slots;
- material, texture, light, shader, model-ownership, and per-frame PBR/simple diagnostics;
- automatic SM3D v2 texture/material preparation with model-owned lifetime and rollback;
- a repository-owned PBR Lab and exact native/Web focused test program.

The existing simple material API and SM3D v1 path retain their behavior. A model-part object borrows its imported material by default, may borrow a caller-owned override, and returns to the imported default when `ClearObjectMaterial3D` is called.

## Renderer3D ABI

The source bridge signatures and dispatch chain are unchanged:

- `Renderer3D(command, a, b, c, d, e, f, g, h, i, j)` returns `Number`.
- `Renderer3DImage(command, image, a, b, c, d, e, f, g, h)` consumes an `Image` and returns `Number`.
- `Renderer3DText(command, text, a, b, c, d, e, f, g, h)` consumes `Text` and returns `Number`; Web emission awaits it.

The authoritative routes are `Smile.Simple3D/Graphics3D.smile`; shared-language intrinsic shape validation in `Smile.Language`; MASM/Web call emission in `Smile.Compiler`; native declarations in `graphics3d.h`; native numeric/image dispatch in `graphics3d_directx.cpp`; native text/manifest dispatch in `runtime.c`; and all generated WebGL2 dispatch in `WebOutputWriter.cs`.

Numeric commands are continuous from 1 through 97. Commands 1-80 retain their M1.1 meanings. Image commands are continuous from 1 through 2. Text command 1 remains the only text command. The next free numeric, image, and text IDs are 98, 3, and 2.

### M2 numeric commands

All PBR factors are thousandths unless stated otherwise. Unnamed arguments are zero at the public facade.

| ID | Command | Positional ABI | Result |
|---:|---|---|---|
| 81 | `CREATE_PBR_MATERIAL` | `a-d=base/normal/ORM/emissive texture handles, e=alpha mode, f=double-sided` | material handle or zero |
| 82 | `SET_PBR_FACTORS` | `a=material, b-d=base RGB, e=alpha, f=metallic, g=roughness, h=normal strength, i=occlusion strength, j=cutoff` | success |
| 83 | `SET_PBR_EMISSIVE` | `a=material, b-d=linear emissive RGB` | success |
| 84 | `SET_PBR_TEXTURES` | `a=material, b-e=base/normal/ORM/emissive textures, f=alpha mode, g=double-sided` | success |
| 85 | `RESET_LIGHTS` | none | success |
| 86 | `SET_AMBIENT_LIGHT` | `a-c=RGB 0-255, d=intensity` | success |
| 87 | `SET_DIRECTIONAL_LIGHT` | `a-c=direction, d-f=RGB 0-255, g=intensity` | success |
| 88 | `SET_LOCAL_LIGHT` | `a=slot 0-3, b=type 0/1/2, c-e=position, f-h=RGB 0-255, i=intensity, j=range` | success |
| 89 | `SET_SPOT_CONE` | `a=spot slot, b-d=direction, e=inner degrees, f=outer degrees` | success |
| 90 | `PBR_TEXTURE_VALUE` | `a=texture, b=property` | usage/filter/wrap/requested anisotropy/effective anisotropy/mip count |
| 91 | `PBR_MATERIAL_VALUE` | `a=material, b=property` | kind/maps/state/factors/ownership value |
| 92 | `LIGHT_VALUE` | `a=query, b=index, c=property` | active-count or light property |
| 93 | `PBR_DRAW_COUNT` | none | successful PBR submissions in the current/last frame |
| 94 | `SIMPLE_DRAW_COUNT` | none | successful simple submissions in the current/last frame |
| 95 | `PBR_TRIANGLE_COUNT` | none | submitted PBR index count divided by three |
| 96 | `PBR_SHADER_AVAILABLE` | none | PBR pipeline availability |
| 97 | `MODEL_PBR_VALUE` | `a=model, b=property, c=part index when required` | ready/owned-count/part-PBR value |

Image command 2 is `CREATE_PBR_TEXTURE`: owned image, `a=usage (1 color/sRGB, 2 data/linear)`, `b=filter (0 nearest, 1 linear, 2 trilinear, 3 anisotropic)`, `c=wrap (0 clamp, 1 repeat)`, and `d=requested anisotropy 1-16`. It consumes the decoded image on success or failure and returns a texture handle or zero. Image command 1 and text command 1 are unchanged.

Commands 78/79 continue to expose total draw calls and submitted triangles. Commands 93-95 are the smallest reusable split diagnostics needed by M2. All frame counters reset after a successful `Begin3D` and on renderer reset and remain queryable after `End3D`.

## Resource limits and ownership

| Resource | Current limit | Ownership |
|---|---:|---|
| Mesh | 128 live | Standalone meshes are caller-owned. A model owns one mesh per part. Objects borrow meshes and block their owner from destruction. |
| Object/model-part | 512 live | Borrows a mesh, optional material, and optional animator. Owns none. |
| Texture | 128 live; 8,192 x 8,192 maximum | Caller owns low-level textures. An SM3D v2 model owns its imported PBR textures and their retained decoded images. Materials borrow textures. |
| Material | 128 live | Caller owns low-level simple/PBR materials. An SM3D v2 model owns its imported PBR materials. Objects borrow materials. Model-owned materials cannot be destroyed directly. |
| Model | 64 live; 16 parts; 131,072 vertices; 393,216 indices; 64 materials; 128 texture references; 16 MiB | Owns retained metadata, part meshes, and—when v2 references resolve—prepared PBR textures/materials. Any part object blocks model destruction. |
| Skeleton | 64 live; 32 bones | Caller-owned; clips and animators borrow it. |
| Clip | 128 live; 16 events | Caller-owned; animators borrow it. |
| Animator | 128 live | Caller-owned; objects borrow it. |
| Lights | ambient + directional + 4 local | Renderer state, not handle resources. Local slots are disabled, point, or spot. |

SM3D v2 loading preflights mesh/model/material/texture capacity, resolves every retained manifest path exactly through the project asset resolver, decodes every image, creates all textures and materials, and publishes the model only after the complete dependency graph succeeds. Missing paths, case mismatches, decode failures, pool exhaustion, or material preparation failures roll back meshes, textures, materials, images, and the model slot. SM3D v1 remains geometry-only.

`ResetRenderer3D` destroys dependency users before owners: objects, models and their resources, animators, clips, skeletons, caller materials, caller textures, and remaining meshes. Native generation-safe handles and Web monotonic safe-integer handles preserve stale-handle rejection.

Dragonfall retains the M1.1 scene policy: six textures, 24 materials, 48 meshes, 441 objects initially, and 448 objects in the boss encounter, leaving at least 64 object slots at the highest reviewed state. It owns no loaded SM3D model, skeleton, clip, or animator resources. M2 does not migrate Dragonfall to PBR.

## Rendering contract

The PBR shader uses Cook-Torrance lighting with GGX distribution, Smith geometry, Schlick Fresnel, Lambert diffuse, and a minimum effective roughness of `0.045`. The base-color factor and object tint multiply the base texture. ORM channels are occlusion, roughness, and metallic. Normal strength affects tangent-space XY, and emissive is independent linear RGB multiplied by an optional emissive texture.

Base-color and emissive textures are sampled as sRGB; normal and ORM textures are sampled as linear data. The native image resource retains both its established premultiplied BGRA buffer for the simple path and a straight BGRA buffer for PBR, so nonzero RGB under alpha zero is preserved. Web uploads specify equivalent unpack/color formats. Filtering supports nearest, linear, trilinear mipmapping, and anisotropic filtering with a reported effective fallback. Mips are generated only during upload, never in the draw loop.

Opaque and mask materials write depth without blending. Mask compares the material cutoff. Blend uses straight source alpha, reads depth without writing it, and renders in caller submission order. Double-sided state disables culling and flips the geometric normal for back faces; single-sided state culls back faces. No sorting layer was added.

The simple and PBR pipelines coexist. PBR shader creation is attempted once per device/context generation. A PBR compile/link failure sets Renderer3D error 44 and leaves the simple pipeline usable. Native device loss and Web context loss release GPU objects while retaining CPU-side resources; restore lazily rebuilds pipelines, buffers, textures, mips, and samplers.

The Web PBR draw function reuses preallocated matrix/material/light scratch storage. The focused gate rejects typed-array construction, spread/map coercion, shader compilation, and mip generation inside that hot path.

## Repository-owned fixtures and PBR Lab

`scripts/generate-renderer3d-pbr-fixtures.ps1` deterministically creates four 4 x 4 PNGs plus valid, missing-texture, and wrong-case two-part SM3D v2 fixtures. It publishes/verifies the textures for the PBR Lab, the focused PBR tests, and the existing PBR model-metadata fixture.

The valid fixture is 1,392 bytes with two parts, eight vertices, twelve indices, two materials, and four shared texture references. Its SHA-256 is `6679C80BF2BF3289B57FFCC268B7581E11F9D7A100B5205884E7F97E05DDDBC5`. The texture hashes are:

- base color: `1B11197A8C4FE0E2C5E61F16E3CC9AA64ED6789D9DD098A98522B6CCD29C4177`;
- normal: `C5576A913A8767A2D5044C6DE4ECCA248827C67EFB930052D58360445758EA6A`;
- ORM: `6D0CECC17484D4909A41365F818F6AEE1AEB0EED80664C80DC3647A45D5C66B5`;
- emissive: `C29F98556522992F4CE2B961A141AB4C5A2ECBF743F707ED1B8CAF1DB7C9C4D6`.

The standard PBR Lab frame reports nine draws: eight PBR, one simple, 2,348 submitted triangles, four active lights, one model, nine meshes, nine objects, eight textures, eight materials, and effective anisotropy eight. Observed manual frames were approximately 8-13 ms and are illustrative diagnostics, not a benchmark.

## Plan mapping and deviations

- The requested PBR-lite shading, maps, material state, four local lights, atomic model ownership, lifecycle rebuild, mixed simple/PBR rendering, diagnostics, lab, fixtures, and native/Web parity are implemented.
- The handoff suggested `L` and `N` lab keys, but the current language exposes established `KEY_A`, `KEY_S`, and `KEY_D` constants, not `KEY_L` or `KEY_N`. The lab uses A for the point light, S for the normal map, and D for diagnostics rather than adding unrelated keyboard ABI in M2.
- No new text command is necessary. Existing text command 1 loads the model; automatic PBR preparation is internal to that same atomic load.
- The historical boundary-metadata model deliberately declares 128 nonexistent texture paths. Before M2 it could load as metadata only; after automatic preparation it must fail atomically. The adjacent model test now asserts that zero-resource rollback, while the dedicated v2 boundary gate retains exact geometry/container limits and the PBR gate tests material/texture/mesh pool exhaustion.
- The imported single-sided lab part was moved into a more strongly lit, unobstructed location after manual review showed it was submitted but visually indistinct. No rasterizer behavior was changed.
- Visual parity uses deterministic semantic assertions, exact console parity, shader/hot-path inspection, and manual native/Web review. No fragile pixel-exact cross-backend comparison was added.
- No M3 feature was implemented.

## Validation evidence

Validation completed on 2026-08-31 (Asia/Taipei) with .NET SDK 10.0.302, Node.js 24.14.0, and Visual Studio 18 Enterprise.

| Command or gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` | PASS, exit 0; native runtime, compiler, AssetTool, tests, and VSIX built. |
| `scripts\test-smile-formatter.ps1` | PASS; 13 focused integration tests. |
| `scripts\format-smile-style.ps1 -Check -FormatLongIf` | PASS; 322 tracked SMILE files. |
| `scripts\test-renderer3d-m11-hardening.ps1` | PASS. |
| `scripts\test-renderer3d-v2-boundaries.ps1` | PASS; exact 7,865,176-byte boundary and over-limit rejection. |
| `scripts\test-renderer3d-models.ps1` | Initial FAIL with nine assertions before publishing declared PBR textures; then two stale metadata-only capacity assertions. After reconciling the M2 ownership contract: PASS native/Web exact parity. |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS native/Web exact parity. |
| `scripts\test-renderer3d-materials.ps1` | PASS native/Web exact parity. |
| `scripts\test-renderer3d-animation.ps1` | PASS native/Web exact parity. |
| `scripts\test-renderer3d-pbr.ps1` | PASS; straight-alpha RGB, texture semantics, mip/filter/aniso, factors, lights, mixed draws, v1/v2, override restore, exact path failures, ten full cycles, pool exhaustion rollback, zero teardown, lab builds, JS syntax, and hot-path checks. |
| `scripts\test-battle3d.ps1` | PASS native/Web exact parity. |
| `scripts\test-dragonfall.ps1` | PASS mechanics, balance, atomic lifecycle, demo/no-demo, native/Web. |
| `scripts\test-simple3d-space-wars.ps1` | PASS Simple3D and Space Wars focused validation. |
| `dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release` | PASS; 288 managed tests. Expected synthetic failure diagnostics were observed. |
| `cmd /c scripts\smoke-test.cmd` | PASS, exit 0; full repository native/Web/game/artifact/VSIX baseline. |
| `cmd /c scripts\install-vsix.cmd` | PASS; rebuilt, replaced, installed, and verified VSIX 2.0.48. |
| `git diff --check` | PASS. |

The native Direct3D PBR Lab displayed distinct rough/smooth dielectric and metal spheres, a normal-mapped cube, emissive torus, rotating masked double-sided imported part, visible smooth single-sided imported part, and the simple cube. The captured standard frame reported the expected 9/8/1 draw split, 2,348 triangles, four lights, and bounded live counts.

The WebGL2 PBR Lab displayed the same feature set. Its HTML, CSS, runtime, game, model, and all four exact texture requests returned HTTP 200. Toggling the light, normal map, and diagnostics produced no runtime or browser-console errors.

The installed extension is VSIX 2.0.48. The VSIX SHA-256 is `AB08EC880CAD508E91260A772EDFE74998F08ED94A891098106C5FFD429727BD`. The installed `Smile.VisualStudio.dll` reports assembly version 2.0.48.0 and SHA-256 `692C9A1B8CC48887D5C63738C1A5486EA0F9CB97770FCA6BB5275B4AC4F7B065`. The compiler and AssetTool SHA-256 values are `A5740C42C2769894D39A16E4ECF677B97B4FB67AD6292D96FAE6792A05D81956` and `AB40CF34A90F6B2091BB5AC894CD1142C760EC21115ACD4373C8A6FD9A99D349`.

## M3 readiness

M3 is unblocked from an M2 resource and rendering perspective after this milestone is committed and pushed. It can build on explicit PBR material state, bounded lights, mixed-pipeline counters, automatic model ownership, context/device restore, and the PBR Lab without changing commands 1-97. Shadows, HDR/tone mapping, bloom, IBL, render graphs, and new VFX remain deliberate future work.

## Command ledger

Substantive M2 commands were:

```powershell
git status --short
git rev-parse HEAD
git rev-parse origin/main
git stash apply stash@{0}

cmd /c scripts\build.cmd
scripts\generate-renderer3d-pbr-fixtures.ps1
scripts\generate-renderer3d-pbr-fixtures.ps1 -Check
scripts\format-smile-style.ps1 -Files <changed SMILE files> -FormatLongIf
scripts\format-smile-style.ps1 -Check -Files <changed SMILE files> -FormatLongIf
scripts\test-smile-formatter.ps1
scripts\format-smile-style.ps1 -Check -FormatLongIf
scripts\test-renderer3d-m11-hardening.ps1
scripts\test-renderer3d-v2-boundaries.ps1
scripts\test-renderer3d-models.ps1
scripts\test-renderer3d-lifecycle.ps1
scripts\test-renderer3d-materials.ps1
scripts\test-renderer3d-animation.ps1
scripts\test-renderer3d-pbr.ps1
scripts\test-battle3d.ps1
scripts\test-dragonfall.ps1
scripts\test-simple3d-space-wars.ps1
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release
cmd /c scripts\smoke-test.cmd
cmd /c scripts\install-vsix.cmd

artifacts\compiler\smilec.exe --project examples\Renderer3DPbrLab\Renderer3DPbrLab.smileproj --target windows-x64 --configuration Release --graphics DirectX -o artifacts\examples\Renderer3DPbrLab.exe
artifacts\examples\Renderer3DPbrLab.exe
node <repository-local static server for artifacts\web\Renderer3DPbrLab>

git diff --check
Get-FileHash -Algorithm SHA256 artifacts\vsix\Smile.VisualStudio.vsix,artifacts\compiler\smilec.exe,artifacts\assettool\smileasset.exe,<installed Smile.VisualStudio.dll>
```

Read-only reconciliation and review also used `Get-Content`, `Get-ChildItem`, `rg`, `git diff`, `git status`, `git log`, `git show`, `git rev-parse`, `git stash show`, file hashes, native window capture, and in-app Web inspection. The final VSIX installation, artifact hashes, commit, and push are recorded in the milestone completion report.
