# Renderer3D M7E-C GPU Particle Common Implementation

M7E-C adds ABI command 127 and a shared `GpuParticleSystem3D` API to `Smile.Simple3D`. Native and Web now implement the same bounded resource pool, changed-slot spawn queue, deterministic fixed-step CPU reference simulation, ping-pong generations, lifetime expiry, reuse rules, diagnostics, and in-flight lifecycle protection.

## Implemented Surface

- `CreateGpuParticleSystem3D`
- `SetGpuParticleSpawnKinematics3D`
- `SetGpuParticleSpawnVisual3D`
- `CommitGpuParticleSpawn3D`
- `AdvanceGpuParticleSystem3D`
- `KillGpuParticleSlot3D`
- `DrawGpuParticleSystem3D`
- handle validation and explicit destruction
- global, resource, and slot diagnostics through `GpuParticleValue3D`

`DrawGpuParticleSystem3D` currently records a logical frame queue entry and enforces the final resource lifetime rules, but deliberately emits no visual draw. M7E-D and M7E-E supply the first real GPU simulation/render paths.

## Validation

`scripts/test-renderer3d-gpu-particle-common.ps1` statically verifies the ABI, exact state stride, storage bounds, ping-pong implementation, time clamp, and absence of Web buffer readback. It then compiles and runs `examples/Renderer3DGpuParticles/Renderer3DGpuParticleTests.smileproj` on native DirectX and Web and requires exact console parity.

The executable checks:

- creation at valid capacity and rejection one over the 8,192 common limit;
- eight-system pool capacity and rejection one over;
- requested/effective simulation and fallback diagnostics;
- exact 80-byte state schema and CPU resource accounting;
- bounded staging, serial rejection, fixed 5+5 millisecond partitioning, and deterministic position/age;
- ping-pong generation changes, expiry, explicit kill, and slot reuse;
- the 250-millisecond accepted-time clamp and dropped-time accounting;
- changed-slot upload accounting and zero readbacks;
- in-flight destruction rejection and release after `End3D`;
- zero GPU dispatch/draw counters in the reference fallback;
- reset invalidation and capacity cleanup;
- continued staging, committing, drawing, and destroying of the existing CPU-driven `ParticleBatch3D` path.

The focused test is part of `scripts/smoke-test.cmd` immediately after the M7E-B distortion gate.

## Deferred by Design

This milestone does not contain a Direct3D 11 compute shader, WebGL2 transform feedback, GPU particle rendering, thermal fire shading, production emitter presets, sword fire, or dragon breath. Those remain the ordered M7E-D through M7E-H milestones and build on this common contract.
