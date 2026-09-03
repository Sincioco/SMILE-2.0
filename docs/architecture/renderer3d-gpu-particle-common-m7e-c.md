# Renderer3D GPU Particle Common Contract — M7E-C

M7E-C introduces the target-neutral persistent particle resource and deterministic reference scheduler used by the later native compute and Web transform-feedback backends. It does not claim GPU simulation or render particles yet. Both targets report `GPU_PARTICLE_BACKEND_OFF`, select `GPU_PARTICLE_SIMULATION_CPU_DETERMINISTIC`, and preserve identical observable state.

## Public Resource

`GpuParticleSystem3D` is a generation-safe, fixed-capacity resource associated with one alpha-blended or additive effect material. A program stages one slot, commits it with a monotonically increasing spawn serial, advances the system in fixed steps, queues it inside a frame, and explicitly destroys it.

The resource limits are deliberately bounded:

- 8 live systems.
- 8,192 slots per system in the common reference implementation.
- 32,768 total reserved slots.
- 512 spawn commands between simulation advances.
- 5–50 milliseconds per fixed step.
- At most 250 milliseconds of accepted simulation time per call; excess time is reported as dropped.

Slots remain stable for their lifetime. A slot cannot be reused until it expires or is explicitly killed, and its next spawn serial must be greater than its previous serial. This makes reuse deterministic without allocation during an update.

## State Schema

Every slot uses schema version 1 with an exact 80-byte stride. The order is permanent for M7E GPU backends:

| Byte offset | Type | Fields |
| ---: | --- | --- |
| 0 | `float4` | Position X/Y/Z, age in milliseconds |
| 16 | `float4` | Velocity X/Y/Z, lifetime in milliseconds |
| 32 | `float4` | Start size, end size, rotation, angular velocity |
| 48 | `float4` | Temperature, density, noise phase, reserved |
| 64 | `uint4` | Seed, active flags, gradient/frame selection, reserved |

Native stores this as an asserted 80-byte structure. Web uses paired `Float32Array` and `Uint32Array` views over 80-byte-per-slot `ArrayBuffer` storage, so integer bits are not converted through floating-point values.

## Deterministic Scheduling

Each system owns two full state buffers. At every fixed step it applies pending spawn commands to the read buffer, copies and simulates all fixed slots into the write buffer, expires slots whose age reached lifetime, and swaps the buffers. Read and write generations expose the swap without exposing pointers.

CPU-to-backend upload accounting includes only committed changed-slot commands: 88 bytes per command (8-byte slot/serial header plus the 80-byte state). There is no state readback, visibility sorting, or per-frame resource allocation.

## Frame and Lifetime Rules

Queuing a nonempty system marks it in flight until `End3D`, the next `Begin3D`, reset, or device/context loss releases that frame reference. Mutation and destruction reject in-flight systems. Duplicate queue requests in one frame are idempotent. Submission groups reject this M7E-C no-visual resource because it has no physical draw submission yet.

Reset destroys all particle systems, invalidates their handles, releases material references, and clears aggregate counters. Web context loss releases frame references but retains the deterministic CPU state for later milestones to rebuild GPU resources.

## Backend Boundary

M7E-C is intentionally the reference/fallback layer:

- GPU dispatch count: 0.
- GPU-render draw count: 0.
- GPU state bytes: 0.
- Backend restart count: 0.
- CPU/GPU readback count: 0.

M7E-D and M7E-E may change the effective backend and GPU counters while preserving this public SMILE API, slot schema, scheduling limits, lifetime behavior, and diagnostic meanings.
