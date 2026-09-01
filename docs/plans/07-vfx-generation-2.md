# VFX Generation 2

## Goal

Produce contemporary layered battle effects without allocating an ordinary `Object3D` for every particle.

Modern appearance should come from:

- good flipbook textures;
- proper blending;
- ribbons;
- emissive/bloom integration;
- layered timing;
- lighting;
- camera and screen impulses;
- deterministic composition.

It should not come from thousands of independently owned scene objects.

## Architecture

### Simulation

Keep deterministic fixed-step simulation in reusable SMILE code where practical.

Benefits:

- shared native/Web behavior;
- straightforward test snapshots;
- no dependence on GPU compute;
- easy Battle3D integration;
- beginner-accessible presets.

### Rendering

Add dedicated runtime batch resources:

```text
ParticleBatch3D
RibbonBatch3D
optional MeshInstanceBatch3D
```

Each batch owns:

- fixed capacity;
- shared material/texture;
- dynamic GPU buffer;
- active instance count;
- generation-safe handle;
- no per-frame heap growth.

## Particle instance data

A sprite instance should include enough information for:

- world position;
- size X/Y or uniform size;
- rotation;
- color and opacity;
- atlas frame;
- billboard mode;
- optional soft-depth parameters later.

The shader expands a unit quad or uses an instanced quad.

Supported billboard modes:

- camera facing;
- vertical-axis facing;
- velocity aligned;
- fixed orientation.

## Ribbon data

A ribbon should support:

- bounded point count;
- position;
- width;
- color/opacity;
- UV distance;
- age;
- camera-facing strip generation;
- break/reset.

Primary use cases:

- sword trails;
- claw trails;
- spell arcs;
- fire-breath core;
- projectile streaks.

Prefer a shared dynamic ribbon buffer. Do not create one mesh per segment.

## Effect simulation features

Initial Generation 2 emitters should support:

- fixed or ranged spawn count;
- lifetime;
- position spread;
- velocity spread;
- gravity;
- drag;
- size start/end;
- color start/end;
- opacity curve;
- rotation and angular velocity;
- atlas columns/rows;
- frame-rate or life-driven flipbook;
- alpha/additive material;
- billboard mode;
- deterministic seed;
- attachment socket or world origin;
- delayed start;
- effect-local duration.

Useful second-stage features:

- orbit;
- turbulence from a deterministic hash/noise function;
- ground kill/bounce;
- mesh fragments;
- depth-softening;
- distortion.

Do not block M7 on distortion or soft particles.

## Multi-emitter composition

An effect is a bounded list of emitter and impulse layers.

Example `HolySwordStrike`:

```text
0 ms     enable sword ribbon on SwordBase/SwordTip
250 ms   narrow blue energy arc
300 ms   white impact flash
300 ms   metal sparks
300 ms   blue motes
300 ms   small point-light impulse
300 ms   camera shake
315 ms   damage number presentation
520 ms   ribbon fade
700 ms   lingering motes finish
```

Example `DragonFireBreath`:

```text
charge glow
throat sparks
core flame ribbon
outer flipbook flames
smoke sprites
embers
impact burst
ground scorch overlay/decal if supported
orange light impulse
camera shake and exposure impulse
```

One high-level call should start the complete composition.

## Pool sizes

Suggested effective profiles:

| Profile | Sprite particles | Ribbon points | Active effects |
|---|---:|---:|---:|
| Low | 256–512 | 128 | 16 |
| Medium | 1,024–2,048 | 512 | 32 |
| High | 4,096 | 1,024 | 64 |

These are bounded capacities. Codex must measure before locking them.

Pool exhaustion policy:

- spawning one composition should be atomic where practical;
- use a documented lower-quality variant or reject the effect;
- never partially spawn an effect in an undefined state;
- never grow the pool dynamically during battle.

## Renderer API direction

Low-level illustrative operations:

```basic
Batch = Graphics3D.CreateParticleBatch3D(2048, Material)

Call Graphics3D.BeginParticleBatch3D(Batch)

Call Graphics3D.SetParticleInstance3D(
    Batch,
    Slot,
    X,
    Y,
    Z,
    Size,
    Rotation,
    Color,
    Opacity,
    Frame
)

Call Graphics3D.CommitParticleBatch3D(Batch, ActiveCount)
Call Graphics3D.DrawParticleBatch3D(Batch)
```

The exact bridge signature may need multiple commands because the current dispatch has a fixed numeric argument count. Keep the public SMILE facade readable and the native/Web command contract bounded.

High-level use:

```basic
Call Effects3D.PlayOn(
    "HolySwordStrike",
    Hero,
    "SwordTip"
)
```

## Relationship with current Battle3D effects

Do not delete `Smile.Battle3D.Effects`.

Options after reconciliation:

1. keep current effects as Generation 1 and add a separate Generation 2 module; or
2. extend the current module behind source-compatible calls.

Preferred initial path:

- retain current `Effects.smile` behavior;
- add `EffectsGen2.smile` or `Smile.Simple3D.Effects3D`;
- adapt Dragonfall incrementally;
- consolidate only after M7 if doing so clearly reduces complexity.

## Lights, flashes, and camera impulses

VFX should request rather than own global systems.

An effect may emit:

- point-light impulse request;
- screen flash intensity;
- camera shake magnitude/duration;
- hit-stop presentation request;
- audio event identifier.

`Scene3D`, Battle3D camera, Renderer2D, and audio remain responsible for applying their own state.

Battle mechanics must never wait on a particle to collide.

## Texture atlas policy

M6 should use PNG flipbook atlases through the existing asset manifest.

Metadata may be configured in SMILE presets:

- columns;
- rows;
- frame count;
- frame rate;
- looping.

Do not require a new external VFX editor or asset format for M6.

## Required diagnostics

- live particle-batch count;
- live ribbon-batch count;
- active particle count;
- active ribbon point count;
- active effect count;
- rejected effect count;
- pool capacity;
- batch draw count;
- last pool-exhaustion reason.

## Required tests

- create/destroy batch;
- stale handle rejection;
- exact capacity boundary;
- update with zero particles;
- additive and alpha batches;
- atlas frame progression;
- billboard math;
- ribbon point progression and reset;
- deterministic same-seed state;
- equivalent fixed-step chunking;
- atomic pool exhaustion;
- native/Web state parity;
- reset returns every counter to zero;
- no ordinary `Object3D` count increase per particle;
- no hot-path list growth.

## Visual acceptance

The Generation 2 slash effect must visibly include:

- a continuous weapon trail;
- an impact flash;
- sparks;
- a colored energy arc or motes;
- optional light and camera impulse;
- clean fade with no hard pop;
- no square texture borders;
- proper depth behavior.

The DragonFireBreath proof must visibly read as a coherent stream rather than disconnected cubes or planes.

## M6 acceptance

M6 is complete when:

1. At least 1,024 particles can render through a small fixed number of batch submissions.
2. Particle count does not increase `LiveObjectCount3D` one-for-one.
3. A sword ribbon follows animated sockets.
4. Native and Web show the same effect timing and event sequence.
5. Current Generation 1 effects remain functional.
6. Dragonfall can opt into Generation 2 effects one effect at a time.
