# Renderer3D textures, PBR-lite materials, and lights

`Smile.Simple3D.Graphics3D` exposes one reusable material system for every 3D application. Renderer3D remains unaware of battles, characters, and individual games. M2 adds a PBR-lite path beside the unchanged simple path; it does not replace Renderer2D, SM3D v1, simple materials, or skeletal animation.

## Public texture API

`LoadTexture3D(path, filter, wrap)` keeps its original nearest/linear behavior. `LoadTexture3DEx(path, usage, filter, wrap, anisotropy)` adds explicit PBR semantics:

| Value | Meaning |
|---|---|
| usage 1 | Color: base color or emissive, sampled as sRGB |
| usage 2 | Data: tangent normal or packed ORM, sampled linearly |
| filter 0 | Nearest, one mip |
| filter 1 | Linear, one mip |
| filter 2 | Linear mip filtering |
| filter 3 | Anisotropic filtering, falling back to filter 2 when unsupported |
| wrap 0/1 | Clamp/repeat |

Requested anisotropy is bounded to 1–16. The texture diagnostic reports requested/effective anisotropy and mip count. Native clamps anisotropy to the D3D11 device limit. Web uses `EXT_texture_filter_anisotropic` when present and otherwise reports 1.

The shared image cache now retains two planes: the established premultiplied BGRA plane used by Renderer2D/simple native rendering, and a straight BGRA plane used by native PBR uploads. This preserves authored RGB even when alpha is zero without changing existing 2D behavior. D3D11 uploads a typeless BGRA resource and chooses an sRGB or linear shader-resource view. Web explicitly sets flip, premultiply-alpha false, and color-space-conversion pixel-store state, then uses `SRGB8_ALPHA8` or `RGBA8`. Mips are generated once at first GPU upload, never per frame.

## PBR material contract

A PBR material has four optional maps plus factors:

- base color: sRGB RGB and straight alpha;
- normal: linear tangent-space XYZ, with X/Y scaled by normal strength;
- ORM: linear red occlusion, green roughness, blue metallic;
- emissive: sRGB RGB multiplied by the linear emissive factor;
- base RGB/alpha, metallic, roughness, normal strength, occlusion strength, alpha cutoff;
- opaque, mask, or alpha-blend mode;
- single- or double-sided rasterization.

Missing maps use neutral constants. Roughness is clamped to 0.045 in the shader. Lighting uses Cook-Torrance with GGX distribution, Smith geometry, and Schlick Fresnel. Object tint and opacity multiply the base factor. Opaque/mask draws write depth; blend draws read but do not write depth. Double-sided draws disable culling and flip the interpolated geometric normal for back faces. Explicit sRGB encoding targets the existing low-dynamic-range UNORM output; HDR, tone mapping, IBL, and shadows remain deferred.

PBR and simple materials use separate built-in shader pipelines but share objects, meshes, camera, depth, animation palette, and frame lifecycle. PBR shader creation is attempted once per graphics-device/context lifetime. A PBR compile failure sets Renderer3D error 44 and leaves the simple pipeline available. Device/context restoration discards only GPU objects; retained image/material/model metadata lazily recreates them.

## Lighting

The bounded light set contains one ambient term, one directional light, and four fixed local slots. A local slot is disabled, point, or spot. Point/spot attenuation is range-bounded; spot lights add normalized direction and inner/outer cone cosines. `ResetLights3D` restores white ambient at 25%, the existing white directional light at 100%, and disables local slots. There is no allocation during lighting updates or drawing.

## Ownership and atomic model loading

Standalone ownership remains object -> material -> texture and object -> mesh. Direct destruction fails while a dependent is live. A PBR material counts each distinct texture handle once even when it is bound in more than one channel.

An SM3D v2 model is an atomic owner:

```text
model -> part meshes
      -> imported materials -> imported textures -> retained images
```

Loading parses and validates the complete SM3D first, preflights the global mesh/material/texture/model pools, resolves each exact `TEXR` path through the declared asset manifest, retains/decodes images, creates PBR textures and materials, then publishes the model handle. Failure at any stage releases every resource created by that attempt and preserves prior live counts. No command-80 hash participates in lookup. Multiple imported materials share one texture record per `TEXR` entry.

`CreateModelPart3D` automatically binds the part's imported material. An explicit `SetObjectMaterial3D` override is borrowed; `ClearObjectMaterial3D` restores the imported default. Model-owned material handles are not exposed and cannot be destroyed directly. A live part blocks model destruction; successful destruction releases imported materials, textures/images, and meshes in dependency order. Reset uses the same order.

## Limits and diagnostics

The global live limits remain 128 meshes, 512 objects, 64 models, 128 textures, 128 materials, 64 skeletons, 128 clips, and 128 animators. SM3D v2 remains limited to 16 parts, 64 materials, and 128 texture references and must also fit the currently free global pools.

Numeric commands 90–97 expose PBR texture/material/light/model state, PBR/simple draw counts, PBR triangle counts, and shader availability. A successful `Begin3D` resets frame counters. These diagnostics are for tests and student-visible lab output; they do not expose model-owned handles or alter ownership.

## Verification

`scripts/test-renderer3d-pbr.ps1` verifies deterministic fixtures, straight alpha-zero RGB, color/data classification, all sampler modes and fallback diagnostics, material factors/maps/reference counts, bounded lights, mixed simple/PBR draws, imported ownership, missing/wrong-case paths, texture/material/mesh pool rollback, stale handles, v1 compatibility, at least ten full model cycles, native/Web exact state parity, and PBR Lab native/Web builds. Existing material, model, lifecycle, animation, Battle3D, Dragonfall, Simple3D, and full smoke gates remain regression requirements.
