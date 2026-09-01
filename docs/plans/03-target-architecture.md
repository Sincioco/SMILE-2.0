# Target Architecture

## Design intent

Generation 2 adds a production-quality character and effects path **beside** the current educational Renderer3D path.

It does not replace:

- Renderer2D;
- primitive Simple3D;
- rigid articulation;
- current Battle3D presentation;
- SM3D v1;
- Direct3D 11;
- WebGL2.

## Layer diagram

```text
Dragonfall and other SMILE games
    |
    +-- Smile.RPG / Smile.BattleTime
    |       deterministic gameplay
    |
    +-- Smile.Battle3D
    |       actor binding, timeline, camera, presentation cues
    |
    +-- Smile.Simple3D.Character3D
    |       asset cache, actor instances, named clips, sockets, cross-fades
    |
    +-- Smile.Simple3D.Scene3D
    |       quality profile, lights, environment, post-processing preset
    |
    +-- Smile.Simple3D.Effects3D
    |       emitters, batches, ribbons, effect compositions
    |
    +-- Smile.Simple3D.Graphics3D
            low-level bounded renderer facade
            |
            +-- Renderer3D bridge
                    |
                    +-- Direct3D 11 backend
                    |
                    +-- WebGL2 backend
```

`Character3D`, `Scene3D`, and `Effects3D` are ordinary SMILE modules. They do not introduce new language statements.

## Asset pipeline

```text
Original or licensed Blender source
    |
    +-- mesh, UVs, material slots
    +-- armature and skin weights
    +-- named actions
    +-- named socket nodes
    +-- optional morph targets
    |
    v
glTF 2.0 or GLB
    |
    +-- optional SMILE descriptor JSON
    |
    v
smileasset.exe model
    |
    +-- validation
    +-- coordinate conversion
    +-- tangent generation or validation
    +-- clip sampling
    +-- deterministic ordering
    +-- checksum
    |
    v
SM3D version 2 plus declared PNG texture assets
    |
    v
Native/Web runtime loader
```

The game never requires Blender or a glTF parser.

## Runtime ownership

Recommended logical ownership:

```text
Character asset
    owns:
        model meshes
        material records
        texture references
        skeleton
        clips
        socket definitions

Character actor
    refers to:
        one character asset
    owns:
        object instances
        animator state
        active blend state
        root transform
        optional attachments

Attachment
    refers to:
        a character actor socket
        a caller-owned object or character actor

Effect asset/preset
    owns:
        bounded emitter definitions
        texture/material references

Effect instance
    owns:
        active particle/ribbon ranges
        deterministic age and seed
```

Destroy order must be explicit:

```text
attachments/effect instances
    -> character actors
    -> character assets
    -> materials
    -> textures
```

A reset invalidates all Generation 2 handles and returns every live count to zero.

## Frame pipeline

Recommended update sequence:

```text
1. Advance deterministic battle mechanics.
2. Compile or advance Battle3D presentation cues.
3. Advance Character3D animation at fixed or bounded elapsed time.
4. Apply root-motion policy and animation events.
5. Update sockets and attachments.
6. Advance Effects3D deterministic simulations.
7. Select quality profile and active lights.
8. Render optional shadow pass.
9. Render opaque/cutout 3D geometry.
10. Render alpha geometry and batched VFX.
11. Resolve MSAA as required.
12. Apply bloom/tone mapping when available.
13. Restore Renderer2D target.
14. Draw HUD, menus, combat text, flashes, and accessibility overlays.
15. Present through the established Show Screen path.
```

Gameplay state must be correct even if steps 7–12 are unavailable.

## Render data flow

### Static and skinned geometry

A Generation 2 vertex should logically provide:

- position;
- normal;
- tangent including handedness;
- UV0;
- four joint indices;
- four normalized weights.

The exact packed runtime representation may differ by backend, but semantics must match.

### Material data

A PBR material should logically provide:

- base-color texture and factor;
- normal texture and strength;
- packed occlusion/roughness/metallic texture;
- metallic factor;
- roughness factor;
- emissive texture and factor;
- alpha mode and cutoff;
- double-sided flag when required.

Texture references remain external declared project assets for the first implementation. Do not embed 2K textures into SM3D merely for convenience.

### Animation data

A Generation 2 clip should provide:

- name;
- duration;
- fixed sample rate or deterministic key times;
- per-bone translation, quaternion rotation, and scale;
- event records;
- loop recommendation;
- optional root-motion source;
- optional morph samples later.

The initial implementation should favor a fixed offline sample rate because it simplifies deterministic runtime evaluation.

### Lighting data

A bounded scene light set should support:

- one ambient or hemispherical contribution;
- one primary directional light;
- a small fixed number of point/spot lights;
- one selected shadow caster;
- optional environment contribution later.

The public scene API uses simple presets and integer values. Shader internals may use floating point.

### Effect data

Effects should use fixed pools and shared batches:

```text
sprite batch
    -> N active quad instances

ribbon batch
    -> N active trail points or segments

optional mesh-instance batch
    -> N active fragment instances
```

Do not allocate an `Object3D` for each particle.

## Compatibility seams

### Old material path

Current simple material calls continue to use the existing simple shader semantics. A PBR material is a new mode or resource configuration.

### Old animation path

The existing public 32-bone/two-key API remains operational. Internally, it may be represented as a small Generation 2 clip only if behavior remains exact.

### Old model path

SM3D v1 continues to load through its current validation. SM3D v2 is selected strictly by file version.

### Old Dragonfall scene

The rigid scene remains available during M7 so visual and mechanical regressions can be compared. Do not delete it until the remaster is accepted.

## Backend parity policy

Native and Web must match in:

- asset validation outcome;
- material semantics;
- clip names/durations/events;
- animation state progression;
- root-motion values;
- socket transforms;
- effect seed progression;
- resource counts and ownership;
- fallback reporting.

Pixel-identical output is not required because GPU rasterization differs. State and visible intent must agree.

## Capability fallback policy

A capability may degrade but must not silently misbehave.

Examples:

- no floating-point color attachment:
  - use LDR rendering;
  - disable HDR bloom;
  - keep PBR direct lighting.
- no sufficient texture size:
  - use medium/low texture variant.
- no Renderer3D:
  - keep existing 2D battle fallback.
- shadow allocation failure:
  - disable real-time shadow and continue.
- full effect pool:
  - reject the complete requested effect atomically or use its documented lower-quality variant.

Expose the selected profile and disabled features through diagnostics.

## Future-compatible but not future-heavy

The architecture should not block a later WebGPU or Renderer3D backend, but M0–M7 must not implement WebGPU merely to prove abstraction.

The proper seam is the current Renderer3D command/resource contract, not a speculative new engine layer.
