# Renderer3D VFX Generation 3 Preflight

Status: M7E-0 contract frozen on 2026-09-04 before any Generation 3 renderer command or target allocation.

## Current frame

The native and Web renderers use the same immutable submission model. `Begin3D` captures objects, particle batches, and ribbon batches. `End3DChecked` runs the shadow pass when enabled, then drains the captured submissions into the scene target in caller order. VFX submissions use depth read with depth writes disabled. HDR output then runs bloom and tone/output transfer before Renderer2D presents HUD and UI.

Generation 3 disabled therefore remains:

```text
shadow -> current scene submissions -> bloom -> tone/output transfer -> Renderer2D
```

No M7E-0 implementation changes that order, creates a new render target, or consumes an ABI command.

## Frozen Generation 3 phase order

Future M7E renderer work must extend the current pass loop at these insertion points:

```text
1. shadow pass
2. opaque and alpha-mask scene geometry
3. sampleable linear-depth preparation
4. distortion-vector submissions
5. distortion composite into scene color
6. soft alpha VFX and smoke
7. additive thermal VFX, energy, sparks, and ribbons
8. bloom
9. tone mapping and output transfer
10. Renderer2D HUD, menus, captions, and screen-space UI
```

The existing immutable queue remains authoritative. A future submission phase tag may partition that queue, but ordering within each phase must remain caller-stable. VFX does not enter the shadow pass, transparent effects keep depth writes disabled, and Renderer2D is never distorted.

## Transaction and ownership

Linear depth, distortion vectors, distortion composite storage, deterministic noise, and GPU simulation buffers will join the existing renderer-generation transaction. Candidate resources are validated completely before publication. A failed creation preserves the previous valid target bundle and reports exact effective fallback modes.

Configuration changes are accepted only outside a frame and outside an in-flight submission. Device/context loss recreates resources from requested policy, then publishes effective policy and fresh fallback flags without retaining stale capability state.

## M7E-0 capability contract

`Smile.Simple3D.Effects3D` exposes the requested simulation policy and effective backend, soft-depth, distortion, flame-shading, and fallback values. M7E-0 intentionally resolves:

| Request | Effective result |
|---|---|
| `CPU_DETERMINISTIC` | CPU simulation; GPU fallback bit clear |
| `GPU_FAST` | CPU simulation; GPU fallback bit set |
| `AUTO` | CPU simulation; GPU fallback bit set |
| soft depth | off |
| distortion | off |
| GPU backend | off |
| flame shading | basic atlas |

The remaining fallback bits identify unavailable soft depth, distortion, and thermal shading. Renderer reset restores the default `AUTO` request and the complete M7E-0 fallback mask.

## ABI and bounded baseline

M7E-0 consumes no new ABI identifiers. Numeric commands remain 1-124, image commands 1-2, and text commands 1-12. The current VFX commands remain particle batch 119, ribbon batch 120, and M6 diagnostics 121.

The inherited limits remain 16 particle batches, 4,096 particles per batch, 8,192 aggregate particle capacity, 16 ribbon batches, 1,024 points per ribbon, and 2,048 aggregate ribbon-point capacity. Effects3D quality capacities remain Low 256, Medium 1,024, and High 2,048 particles.

## AetherBlade fallback

`Smile.Simple3D.AetherBlade3D` is a generic socket-driven effect. It requires distinct named base and tip sockets, owns bounded atlas/material/batch resources, and renders an outer halo, inner glow, bright core, and 16-sample unconnected afterimage trail. Trail samples use a fixed 12-millisecond interval with four-step catch-up. Clip restart, seek, wrap, serial change, socket loss, renderer reset, and enable/disable transitions clear temporal history rather than joining old and new blade positions.

This fallback is deliberately CPU-authored and GPU-rendered through the existing instanced particle and ribbon paths. It is not a claim of GPU simulation, soft particles, heat distortion, or thermal fire shading.
