# Renderer3D M7E-D D3D11 GPU Particles

M7E-D replaces native automatic/fast Generation 3 particle simulation with a real D3D11 compute path while retaining M7E-C's deterministic CPU reference backend and public SMILE API.

## Delivered behavior

- D3D11 feature level 11 compute simulation with 256 threads per group.
- Two persistent 80-byte structured state buffers per system, alternating SRV/UAV roles.
- Direct `SV_InstanceID` vertex access and one capacity-bounded instanced draw.
- Changed-slot spawn uploads capped at 512 commands per advance.
- Native capacities verified at 1,024, 4,096, and 16,384 particles.
- Standard transparent, soft-depth, HDR, and heat-distortion pass integration.
- Transactional shader/buffer fallback to deterministic CPU simulation.
- Empty restart on device loss, stable handles, explicit restart accounting, and lazy recreation.
- Zero GPU readback and no full-state CPU upload in the fast path.

## Focused validation

`scripts\test-renderer3d-gpu-particle-d3d11.ps1` statically verifies the structured-buffer, compute-dispatch, direct-render, fallback-hook, restart, and no-readback boundaries. It then compiles and runs the native D3D11 test, verifies resource bytes and ping-pong generations, renders a normal particle system, renders the same system through soft-depth and distortion, and runs separate forced shader and buffer failure processes.

The permanent smoke suite runs the target-neutral M7E-C native/Web parity gate immediately before the native M7E-D gate. Existing VFX batch behavior remains covered independently.
