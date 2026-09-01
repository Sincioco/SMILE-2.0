# Animation Generation 2

## Goal

Replace rigid-part posing and two-key-only motion for production actors with imported skeletal animation that remains bounded, deterministic in state progression, and simple to control from SMILE.

## Compatibility

The current 32-bone/two-key API remains supported.

Generation 2 may reuse its resource types or add new ones, but it must preserve:

- current command values and semantics;
- current loop/once/hold behavior;
- current exact-once event behavior;
- current stale-handle and ownership rules;
- current tests.

## Skeleton profile

Recommended Generation 2 limits:

- normal target: 55–80 deformation bones;
- hard maximum: 128 bones;
- four influences per vertex;
- parent-before-child order after conversion;
- full local TRS bind transforms;
- full inverse bind matrices;
- no non-deforming Blender control bones unless required as sockets;
- validated finite matrices;
- bounded depth and node count.

The asset converter should remove or reject irrelevant control bones according to an explicit profile rather than silently exceeding limits.

## Bone palette transport

The existing fixed `mat4 bones[32]` vertex-uniform approach is too limited for the target.

### Native Direct3D 11

Use one of the smallest suitable bounded options:

- structured buffer/SRV containing the active palette; or
- a sufficiently sized dedicated constant buffer if confirmed safe for the hard limit.

Do not update one buffer per mesh part when multiple parts share one animator unless unavoidable.

### WebGL2

Use a non-filtered floating-point bone texture sampled with `texelFetch`, or another WebGL2 path that supports 128 matrices without depending on minimum vertex-uniform limits.

Validate required texture dimensions and vertex texture access. If the Generation 2 palette path is unavailable, report the capability failure and use the documented fallback rather than corrupting deformation.

## Clip storage

For the first production path, use offline fixed-rate samples.

Recommended defaults:

- 30 samples per second;
- integer clip duration in milliseconds;
- per-frame local translation, quaternion rotation, and scale;
- normalized quaternions;
- deterministic frame ordering;
- optional omission/default flags for static channels if simple to implement safely.

Why sampled clips:

- simple and predictable runtime;
- consistent Blender curve evaluation;
- straightforward cross-target playback;
- easy bounds checking;
- no need to reproduce every glTF interpolation mode at runtime.

Support 15–60 Hz in the format, but standardize Dragonfall content on 30 Hz initially.

## Clip playback

An animator should maintain:

- current clip;
- current time;
- speed;
- loop/once/hold mode;
- completion state;
- pending exact-once events;
- optional destination clip;
- destination time;
- cross-fade elapsed and duration;
- root-motion accumulator;
- final bone palette;
- socket matrices or transforms.

No per-frame allocation.

## Cross-fading

M3 must support one bounded base-layer cross-fade:

```text
current pose * (1 - blend)
    + destination pose * blend
```

Use:

- linear translation and scale interpolation;
- normalized shortest-path quaternion interpolation;
- a bounded integer blend duration;
- zero-duration immediate switch;
- event policy documented for source and destination clips;
- completion and hold behavior documented.

Do not begin with a general animation graph.

## Root motion

Support two modes:

1. **In-place**
   - root translation is removed from the visual skeleton;
   - game/Battle3D controls world motion.
2. **Extracted root motion**
   - clip root delta is exposed each update;
   - `Character3D` may apply it according to caller policy.

Battle mechanics remain authoritative. Damage does not depend on the animated sword touching a collider.

Required root-motion functions should allow:

- querying/taking delta X/Y/Z and yaw;
- discarding the delta;
- applying it to actor presentation only;
- clamping/warping the final approach through Battle3D later.

## Animation events

Events are imported from the SM3D descriptor.

Examples:

- `FootstepLeft`;
- `FootstepRight`;
- `SwordTrailOn`;
- `SwordImpact`;
- `SwordTrailOff`;
- `CastRelease`;
- `CameraImpulse`;
- `VoiceCue`.

Events must:

- trigger exactly once when crossed;
- work through normal and looping playback;
- have a documented policy during cross-fades;
- remain independent from visual frame rate;
- never directly modify battle health in the renderer.

Battle3D may use an event to align a previously authorized impact presentation.

## Sockets

Required named sockets:

- `Root`;
- `Head`;
- `Chest`;
- `HandRight`;
- `HandLeft`;
- `SwordBase`;
- `SwordTip`;
- `ShieldCenter`;
- optional VFX points.

The converter maps descriptor aliases to nodes/bones.

Runtime functions should provide:

- socket existence;
- world position;
- world rotation or transform;
- attachment update;
- failure without undefined values.

Sockets update after animation evaluation and before VFX update/render submission.

## State-controller layer

The renderer owns clips and playback. `Character3D` owns beginner-friendly state selection.

A simple controller should map states such as:

- Idle;
- Move;
- Attack;
- Special;
- Cast;
- Defend;
- Hit;
- KO;
- Victory.

It should provide default transition durations but allow overrides.

Do not make the renderer aware of Dragonfall battle commands.

## Later animation quality stages

These are valuable but must not block the first imported actor.

### M4 or later

- look-at for head/eyes;
- one upper-body overlay layer;
- additive breathing/recoil;
- turn-in-place selection.

### After M7 core proof

- two-bone foot IK;
- hand-to-weapon constraints;
- foot locking;
- simple deterministic spring bones;
- morph-target facial expressions;
- humanoid retargeting.

Each must have its own focused capability flag and test.

## Beginner-facing API direction

Illustrative:

```basic
Call Character3D.Play(Hero, "Idle", True)

Call Character3D.CrossFade(
    Hero,
    "SwordAttack",
    160
)

If Character3D.TakeEvent(Hero, "SwordImpact") Then
    Call Effects3D.PlayAt("SwordImpact", Character3D.SocketPosition(Hero, "SwordTip"))
End If
```

Advanced playback remains available in `Graphics3D`.

## Required tests

### Asset import

- valid 68-bone fixture;
- 128-bone boundary;
- 129-bone rejection;
- parent cycle rejection;
- bad inverse bind matrix rejection;
- out-of-range joint rejection;
- invalid weights rejection;
- clip name uniqueness;
- sample-rate and duration validation;
- socket mapping;
- event validation.

### Runtime

- bind pose;
- sampled frame selection;
- interpolation between samples;
- quaternion shortest path;
- 30/60/120 visual-update chunk equivalence at shared final times;
- loop and hold;
- zero and nonzero cross-fade;
- source/destination event policy;
- root-motion extraction;
- socket world transform;
- independent animators sharing one asset;
- native/Web state parity;
- reset and destruction ownership;
- no hot-path allocation growth.

### Visual

- shoulders, elbows, hips, and knees deform continuously;
- feet do not visibly detach from legs;
- sword and shield remain attached;
- attack returns smoothly to idle;
- hit reaction can interrupt according to documented policy;
- no pose pop at a normal cross-fade duration.

## M3 acceptance

M3 is complete when:

1. A generated original fixture with more than 32 bones imports through GLB -> SM3D v2.
2. Native and Web render it skinned.
3. At least Idle, Walk, Attack, Hit, and Victory clips play by name.
4. Attack cross-fades in and back out.
5. An exact-once `SwordImpact` event is observed.
6. `SwordTip` socket follows the animated skeleton.
7. Root motion can be extracted and ignored/applied.
8. Current v1/two-key animation tests still pass.
