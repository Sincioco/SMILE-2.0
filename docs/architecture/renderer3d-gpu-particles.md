# Renderer3D GPU Particle Architecture

## Contract

`GpuParticleSystem3D` is a persistent, bounded Renderer3D resource. SMILE source stages one fixed slot at a time, commits at most 512 changed slots between advances, advances a fixed-step scheduler, and queues the persistent system once per frame. The explicit 80-byte particle state and the public command 127 ABI remain shared by native and Web.

The common deterministic CPU backend remains available for exact cross-target tests and graceful fallback. Native automatic and fast requests may use a larger 16,384-particle per-system limit. WebGL2 automatic and fast requests support up to 8,192 particles per system. The portable CPU limit remains 8,192, with 32 systems and 32,768 total slots in either mode. M7E-G0 raises only the system ceiling (formerly eight), not the aggregate particle allowance. Query 60 (`GPU_PARTICLE_QUERY_MAX_TOTAL_CAPACITY`) exposes that aggregate ceiling for complete preflight admission across fire, future ice/magic, and other shared effects.

## Native D3D11 backend

Feature level 11 devices compile one cached compute shader and one cached particle vertex shader. Each fast system owns two default-usage structured buffers with SRV and UAV views plus one constant buffer. A 256-thread compute dispatch reads generation N and writes generation N+1. The render pass binds the current read buffer directly to the vertex shader, indexes it with `SV_InstanceID`, and issues one capacity-bounded instanced quad draw. Inactive slots are clipped in the vertex shader.

Only committed changed slots cross the CPU/GPU boundary. There is no state readback, no per-frame full-state upload, and no CPU mutation of fast-path position, velocity, rotation, or thermal fields. Small CPU metadata arrays retain only scheduling facts needed by the public bounded lifecycle: active slot, serial, age, and lifetime.

## WebGL2 backend

WebGL2 compiles one cached transform-feedback simulation program and one cached particle render program. Each fast system owns two 80-byte interleaved state buffers, one fixed per-slot spawn-input buffer, two simulation VAOs, two render VAOs, and two transform-feedback objects. The exact five-varying order is declared before link. A fixed-step point dispatch reads generation N, applies a newer per-slot spawn generation, writes generation N+1, and swaps the buffers.

The CPU uploads only committed 80-byte changed-slot ranges through `bufferSubData`; the fixed arrays and buffers do not grow after creation. Rendering binds the current state buffer as instanced attributes and draws the configured capacity. Inactive slots are moved outside clip space. Capability checks require ten vertex attributes, twenty interleaved transform-feedback components, complete object allocation, successful program links, and a clean first dispatch. Failure selects the deterministic CPU backend.

## Rendering and coexistence

GPU particles reuse the existing VFX material, blend, depth-read, soft-depth, HDR, and distortion contracts. Standard systems render with transparent submissions. Distortion systems render into the existing vector target and take part in the same composite pass. A queued fast system therefore costs one GPU particle draw, with no generated per-particle draw calls.

## Failure and device loss

Shader, attribute-limit, transform-feedback, or buffer creation failure changes the affected request to the deterministic CPU backend without leaking a partial GPU resource graph. Native device loss and Web context loss release the cached pipeline and every GPU system resource. Because readback is prohibited, live fast simulations restart empty, increment the restart diagnostic, retain their stable resource handles and serial history, and lazily recreate GPU storage on the next advance. Reset destroys systems before device/context cleanup and clears all counters.

The diagnostics distinguish portable maximum capacity, native GPU maximum capacity, effective simulation mode, backend, dispatches, render draws, CPU upload bytes, GPU state bytes, restarts, and readbacks. Readbacks must remain zero.
