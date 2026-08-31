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

The shared native image cache retains straight BGRA as its canonical decoded plane, preserving authored RGB even when alpha is zero. A premultiplied derivative is created lazily for Direct2D, GDI, and simple Renderer3D consumers. GPU-backed consumers release that derivative immediately after upload; GDI may retain it because it has no durable GPU copy. Native diagnostics report straight, premultiplied, and total retained CPU bytes. D3D11 uploads a typeless BGRA resource and chooses an sRGB or linear shader-resource view. Web explicitly sets flip, premultiply-alpha false, and color-space-conversion pixel-store state, then uses `SRGB8_ALPHA8` or `RGBA8`. Mips are generated once at first GPU upload, never per frame.

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

Blend submission order is caller order. Draw opaque and masked objects first, then submit overlapping blended objects from farthest to nearest; Renderer3D does not hide a transparent sort. PBR object transforms use a bounded positive-determinant profile: singular or mirrored transforms fail before submission with error 46 and do not increment draw or triangle counters. Positive nonsingular object scale is the M2/M3 production profile. The simple path retains its prior transform behavior.

PBR and simple materials use separate built-in shader pipelines but share objects, meshes, camera, depth, animation palette, and frame lifecycle. PBR shader creation has a cached not-attempted/available/unavailable state and is attempted at most once per graphics-device/context generation. A PBR compile failure sets stable Renderer3D error 44 and leaves the simple pipeline available without per-frame retries. Reset or device/context restoration starts a new generation; retained image/material/model metadata lazily recreates GPU objects.

## Lighting

The bounded light set contains one ambient term, one directional light, and four fixed local slots. A local slot is disabled, point, or spot. Point/spot attenuation is range-bounded; spot lights add normalized direction and inner/outer cone cosines. `ResetLights3D` restores white ambient at 25%, the existing white directional light at 100%, and disables local slots. There is no allocation during lighting updates or drawing.

## Ownership and atomic model loading

Standalone ownership remains object -> material -> texture and object -> mesh. Direct destruction fails while a dependent is live. A PBR material counts each distinct texture handle once even when it is bound in more than one channel.

An SM3D v2 model is an atomic owner:

```text
model -> part meshes
      -> imported materials -> imported textures -> retained images
```

`LoadModel3D` remains the atomic all-in-one operation. It parses and validates the complete SM3D, loads geometry, and returns zero unless PBR preparation also succeeds. `LoadModelGeometry3D` instead publishes geometry and metadata without resolving an image or consuming a texture/material slot. `PrepareModelPbr3D` then preflights the global material/texture pools by unique identity, resolves every exact `TEXR` path through the declared asset manifest, and publishes the complete PBR ownership set only on success. Failure releases temporary images, textures, and materials while preserving the model handle, its meshes, and prior live counts. No command-80 hash participates in lookup.

A prepared texture identity is the exact retained path plus color/data usage, filter, wrap, requested/effective anisotropy, and mip policy. The reference-to-resource map remains separate from the unique-owned-texture list, so repeated references neither consume duplicate pool slots nor cause double destruction. Applications should prepare before creating parts: an existing part retains its current default, while a new part receives the imported PBR material.

`CreateModelPart3D` automatically binds the part's imported material. An explicit `SetObjectMaterial3D` override is borrowed; `ClearObjectMaterial3D` restores the imported default. Model-owned material handles are not exposed and cannot be destroyed directly. A live part blocks model destruction; successful destruction releases imported materials, textures/images, and meshes in dependency order. Reset uses the same order.

## Limits and diagnostics

The global live limits remain 128 meshes, 512 objects, 64 models, 128 textures, 128 materials, 64 skeletons, 128 clips, and 128 animators. SM3D v2 remains limited to 16 parts, 64 materials, and 128 metadata texture references; only the deduplicated owned texture count consumes the global texture pool.

Numeric commands 90–97 expose PBR texture/material/light/model state, PBR/simple draw counts, PBR triangle counts, and cached pipeline/model preparation diagnostics. Text commands 2 and 3 provide geometry-only load and asynchronous-compatible explicit preparation without renumbering numeric commands 1–97. A successful `Begin3D` resets frame counters. These diagnostics are for tests and student-visible lab output; they do not expose model-owned handles or alter ownership.

## Verification

`scripts/test-renderer3d-pbr.ps1` retains the M2 baseline. `scripts/test-renderer3d-pbr-hardening.ps1` adds deterministic native/Web normal and forced-failure generations, geometry fallback, atomic explicit preparation, exact-reference deduplication, pool preflight, native CPU-plane diagnostics, positive-transform policy, PBR-only nonuniform animation-scale rejection, ten explicit lifecycle cycles, and Web hot-path source checks. Existing material, model, lifecycle, animation, Battle3D, Dragonfall, Simple3D, and full smoke gates remain regression requirements.
