# Renderer3D M7E-E WebGL2 GPU Particles

M7E-E replaces Web automatic/fast Generation 3 particle simulation with a pure-JavaScript WebGL2 transform-feedback path while retaining M7E-C's deterministic CPU reference backend and public SMILE API.

## Delivered behavior

- Two persistent 80-byte interleaved state buffers per system, alternating transform-feedback input/output roles.
- One fixed per-slot spawn buffer updated only at committed changed-slot ranges.
- Exact five-`vec4` varying order declared before link in interleaved mode.
- Direct current-state instanced rendering with inactive slots clipped outside the viewport.
- Web capacities verified at 1,024, 4,096, and 8,192 particles.
- Standard transparent, soft-depth, HDR, and heat-distortion pass integration.
- Attribute, shader, buffer, transform-feedback, and first-dispatch capability checks with deterministic CPU fallback.
- Empty restart on context loss, stable handles, explicit restart accounting, and lazy resource recreation.
- No GPU readback and no fast-path CPU mutation of particle position, velocity, rotation, or thermal state.

## Focused validation

`scripts\test-renderer3d-gpu-particle-webgl2.ps1` verifies the exact attribute offsets, 80-byte stride, varying declaration, point dispatch, changed-slot upload, allocation-free steady simulation path, context-loss hook, and readback prohibition. It compiles and runs the Web GPU test at all three supported capacities, checks dispatch and render diagnostics, triggers context loss/restore, and runs forced shader-link and attribute-limit fallback cases.

The permanent smoke suite runs the target-neutral M7E-C gate, native M7E-D gate, and Web M7E-E gate in order. Existing VFX batches and deterministic native/Web scheduling remain covered independently.
