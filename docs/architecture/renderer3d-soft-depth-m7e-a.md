# Renderer3D Soft Depth — M7E-A

## Contract

Renderer3D soft particles are an optional, capability-tested extension of the existing M5 scene-target transaction. Existing programs remain direct-rendered unless they explicitly request soft depth. `Effects3D.Initialize` requests the feature and assigns bounded defaults to its alpha, additive, and ribbon materials.

The append-only numeric Renderer3D ABI adds command 125. Image commands remain 1-2 and text commands remain 1-12.

## Frame order

```text
shadow
opaque and alpha-mask scene geometry
linear-depth snapshot
alpha and additive VFX with depth read/no write
HDR bloom and tone mapping, or direct-LDR presentation
Renderer2D HUD
```

The depth-stencil resource is unbound before its shader-resource view is sampled. After the bounded fullscreen copy, the source view is unbound and the original depth-stencil is rebound for transparent depth testing.

## Native Direct3D 11

- Scene depth uses `R24G8_TYPELESS` storage with a `D24_UNORM_S8_UINT` depth-stencil view and an `R24_UNORM_X8_TYPELESS` shader-resource view.
- The single-sample linear-depth target is `R32_FLOAT`.
- The 1x path samples `Texture2D<float>`.
- The 2x/4x path samples `Texture2DMS<float>`, takes the minimum visible sample, and then linearizes ordinary-Z Direct3D depth in the 0..1 range.
- Direct LDR at 1x renders scene color directly to the back buffer while using the separate sampleable depth target. Direct LDR with MSAA resolves normally.

## WebGL2

- The offscreen scene framebuffer uses a `DEPTH_COMPONENT24` texture.
- The preferred linear-depth target is `R32F` after a real framebuffer-completeness test.
- `RGBA8` packed linear depth is the bounded fallback.
- WebGL ordinary-Z depth is converted from 0..1 window depth to -1..1 clip depth before linearization.
- Direct LDR uses the existing post fullscreen program only as a lossless scene copy; Renderer2D is composited afterward and is never depth-faded.

## Material policy

Effect materials select one of:

- `SOFT_DEPTH_MATERIAL_OFF`;
- `SOFT_DEPTH_MATERIAL_AUTOMATIC`, currently 24 world units;
- `SOFT_DEPTH_MATERIAL_EXPLICIT`, from 1 through 1,000,000 world units.

The fade is `Saturate(Max(SceneLinear - ParticleLinear, 0) / SoftnessDistance)`. Invalid modes and explicit distances are rejected before mutation.

## Transaction and fallback

Linear-depth resources participate in the same dimension, quality, and configuration revision transaction as M5 scene targets. A failed replacement keeps an existing compatible generation. A first-generation failure disables soft depth, reports shader or target fallback, and preserves ordinary VFX. No target is allocated during an ordinary frame.

Diagnostics expose requested/effective mode, format, dimensions, bytes, copy draws/failures, softened VFX draws, opted-in materials, fallback reason, material settings, and resource generation.
