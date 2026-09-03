# Renderer3D M7E-B Heat Distortion

M7E-B adds an opt-in heat-distortion pass to the shared Renderer3D ABI. It does not add GPU particle simulation.

## Render order

Renderer3D now uses this order whenever distortion is requested:

1. Opaque and masked 3D geometry.
2. The M7E-A linear-depth snapshot when soft depth is enabled.
3. Distortion-vector particle and ribbon submissions.
4. One bounded scene-color composite when at least one distortion emitter was submitted.
5. Ordinary alpha and additive VFX.
6. Bloom and tone mapping when HDR post-processing is active.
7. Renderer2D composition.

Transparent non-VFX geometry and ordinary VFX render after the distortion composite. Renderer2D remains outside the 3D scene target, so HUD text and overlays are not distorted.

## Resource ownership

The M5 target transaction owns the distortion vector target and a full-resolution scene-color scratch target. Native Direct3D 11 uses `RGBA16_FLOAT`. WebGL 2 prefers `RGBA16F` and falls back to signed-packed `RGBA8`. A target-allocation failure disables distortion without making the frame invalid.

High quality uses a half-resolution vector target. Medium uses quarter resolution. Low intentionally disables the pass. Target size, format, bytes, resource generation, draw counts, emitter counts, maximum requested strength, and fallback reason are queryable.

The scene and scratch targets swap after the composite. Renderer3D never samples from the render target it is writing.

## Material and composite contract

`SetEffectMaterialDistortion3D` changes an alpha or additive effect material to `VFX_SHADING_DISTORTION`. The material supplies bounded strength, noise scale and speed, and a two-dimensional flow vector. A zero-strength call restores standard visible VFX shading.

The vector shader accumulates signed screen-space displacement. The fullscreen pass clamps aggregate displacement to plus or minus 0.03 UV units and clamps the final lookup to the screen edge. Distortion changes sample location only; it does not add light.

## Fallbacks

The pass reports disabled, shader-unavailable, target-unavailable, and quality-disabled reasons. Direct-LDR rendering uses an offscreen sampleable scene target and the same scratch discipline. Existing applications remain on the established M5 path until they explicitly request distortion or initialize the advanced `Effects3D` layer.
