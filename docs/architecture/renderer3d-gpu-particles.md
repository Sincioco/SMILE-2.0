# Renderer3D GPU Particle Architecture

## Contract

`GpuParticleSystem3D` is a persistent, bounded Renderer3D resource. SMILE source stages one fixed slot at a time, commits at most 512 changed slots between advances, advances a fixed-step scheduler, and queues the persistent system once per frame. The explicit 80-byte particle state and the public command 127 ABI remain shared by native and Web.

The common deterministic CPU backend remains available for exact cross-target tests and graceful fallback. Native automatic and fast requests may use a larger 16,384-particle per-system limit; the portable CPU limit remains 8,192, with eight systems and 32,768 total slots in either mode.

## Native D3D11 backend

Feature level 11 devices compile one cached compute shader and one cached particle vertex shader. Each fast system owns two default-usage structured buffers with SRV and UAV views plus one constant buffer. A 256-thread compute dispatch reads generation N and writes generation N+1. The render pass binds the current read buffer directly to the vertex shader, indexes it with `SV_InstanceID`, and issues one capacity-bounded instanced quad draw. Inactive slots are clipped in the vertex shader.

Only committed changed slots cross the CPU/GPU boundary. There is no state readback, no per-frame full-state upload, and no CPU mutation of fast-path position, velocity, rotation, or thermal fields. Small CPU metadata arrays retain only scheduling facts needed by the public bounded lifecycle: active slot, serial, age, and lifetime.

## Rendering and coexistence

GPU particles reuse the existing VFX pixel shader, material, blend, depth-read, soft-depth, HDR, and distortion contracts. Standard systems render with transparent submissions. Distortion systems render into the existing vector target and take part in the same composite pass. A queued fast system therefore costs one GPU particle draw, with no generated per-particle draw calls.

## Failure and device loss

Shader or buffer creation failure changes the affected request to the deterministic CPU backend without leaking a partial GPU resource graph. Device loss releases the cached pipeline and every structured resource. Because readback is prohibited, live fast simulations restart empty, increment the restart diagnostic, retain their stable resource handles and serial history, and lazily recreate GPU storage on the next advance. Reset destroys systems before device cleanup and clears all counters.

The diagnostics distinguish portable maximum capacity, native GPU maximum capacity, effective simulation mode, backend, dispatches, render draws, CPU upload bytes, GPU state bytes, restarts, and readbacks. Readbacks must remain zero.
