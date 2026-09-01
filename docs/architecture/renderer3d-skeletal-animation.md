# Renderer3D skeletal animation

Renderer3D provides bounded, generation-safe legacy authoring resources and production model-owned animation. The system is generic: it has no character, battle, action, or game-specific concepts.

## Legacy low-level data model

A `Skeleton3D` has 1-32 parent-ordered bones. Each bone declares a parent index and a local bind-pivot translation. Root bones use parent `-1`; a child parent must precede it. `CommitSkeleton3D` validates the complete acyclic hierarchy and derives inverse global bind translations before the skeleton can create clips or animators.

A mesh vertex can receive four bone indices and four normalized thousandth weights through `SetMeshSkin3D`. Weights must total 1000. Binding an animator to an object validates the mesh's highest weighted joint against the animator's skeleton before rendering.

An `AnimationClip3D` contains optional two-key translation, quaternion-rotation, and scale tracks per bone. Missing tracks use the bind translation, identity rotation, or unit scale. Translation/scale use linear interpolation. Quaternion keys use normalized shortest-path interpolation. Two-key clips deliberately keep the student API small while supporting idle, attack, cast, hit, KO/hold, victory, and creature motion.

The simple shader retains existing support for nonuniform bone-scale keys. The current PBR tangent/normal skinning profile supports uniform scale only: setting or replacing any scale track recomputes a cached clip-safety bit, and a PBR draw using an unsafe active clip fails before submission with Renderer3D error 45. This keeps animation resources reusable across both paths without rejecting legacy/simple animation creation. A future inverse-transpose or dual-quaternion skinning milestone may broaden the PBR profile.

Clips may contain up to 16 time-ordered integer event IDs. `TakeAnimationEvent3D` consumes a pending event exactly once. Events cross correctly during ordinary and looping updates.

An `Animator3D` owns independent playback time, speed, loop/completion state, event state, and a fixed 32-matrix palette. `UpdateAnimator3D(deltaMilliseconds)` advances integer time independently of visual frames. Once clips clamp and hold the final pose; loops wrap deterministically. Any number of objects may share an animator when their meshes use the same skeleton, while separate actors normally use separate animators.

## GPU parity

The vertex format is position, normal, UV, four float joint indices, and four weights. Static meshes default to bone zero with full weight, but skinning is disabled unless an animator is bound.

The legacy path remains unchanged: DirectX uploads a fixed row-major 32-matrix palette in the existing constant buffer, and WebGL2 uses the equivalent `mat4 bones[32]` uniform palette. This preserves every command and public behavior used by existing custom-mesh lessons.

Production SM3D v2 model animators use a separate fixed 128-bone path. Direct3D 11 uploads 128 row-major matrices through vertex constant buffer slot b1. WebGL2 stores the shared palette in an RGBA32F 4-by-128 vertex texture and fetches four texels per matrix; it does not depend on oversized vertex-uniform arrays. Both the simple and PBR vertex shaders blend the same four joint/weight influences. Palette transport is cached by animator handle and pose revision, so repeated draws of the same unchanged animator do not re-upload it. `ModelPaletteUploadCount3D` exposes the current generation's uploads.

Animation evaluation is CPU-side and deterministic; deformation is GPU-side on both targets. The Web production animator reuses preallocated local, destination, global, palette, root, socket, and time-result typed arrays. The native animator uses fixed bounded storage. Neither production update path allocates per update or draw.

## Production model animation

An animated `Model3D` owns up to 256 retained nodes, 128 bones with full inverse-bind matrices, 64 sampled clips, 64 named sockets, and all tracks, sample floats, events, and root-motion records from its optional SM3D v2 animation group. `CreateModelAnimator3D` creates independent mutable playback state over that immutable payload. Two objects can therefore draw one model at different clips, times, speeds, fades, and root-motion policies without copying the asset.

Clip lookup uses exact case-sensitive names. Playback modes are `ANIMATION_LOOP`, `ANIMATION_ONCE`, and `ANIMATION_HOLD`; once and hold retain the final pose, while completion and replay remain explicit. Production advancement retains independent source/destination hundredth-millisecond remainders, so fractional speeds are update-partition invariant. Exact-grid clips use ordinary fixed-rate interpolation; the last pair of an irregular 1,010 ms or 1,017 ms clip is interpolated across its shorter rational final interval.

One bounded base-layer crossfade advances both source and destination with independent remainder, mode, completion, and root state. The destination owns future events while the source continues moving without emitting events. At completion the destination state is promoted atomically. Interrupting an active fade promotes its current destination to the new source and starts one new destination, so no third pose or snap-back state is retained. Root removal occurs independently before pose blending, and future root deltas blend source/destination motion by fade weight.

Event traversal is chronological across ordinary updates and multiple loop wraps. `Play` starts a new event session and queues time-zero events once; `CrossFade` preserves queued events and adds the destination's time-zero events; `Update(0)` cannot duplicate them. The pending FIFO holds 32 entries. Overflow retains the first 32, advances pose/time/root normally, sets Renderer3D error 49, and records sticky overflow plus a saturating dropped-event count. `AnimatorEventOverflowed3D`, `AnimatorDroppedEventCount3D`, and `ClearAnimatorEvents3D` expose and clear that bounded state.

M5 shadow rendering reuses the same evaluated palette revision as the main pass. A production animator uploads at most once per revision even when several model parts submit to both shadow and main passes; the shadow palette diagnostic counts only distinct uploads. Legacy 32-bone and production 128-bone animation both cast opaque/masked shadows. Shadow rendering does not advance animation time or mutate event/root-motion state.

Root motion can extract any descriptor-selected XYZ axes plus yaw. Translation is returned in thousandths and yaw in thousandths of a degree through one atomic `RootMotionDelta3D` take; the final yaw component clears the accumulated delta. Optional removal restores extracted translation to bind values and removes the yaw twist from the visual pose. Loop and multi-loop updates include end-to-start motion exactly once per wrap.

A named socket stores local TRS on one retained node. Raw queries return the animated model-space transform. World queries additionally apply the attached object's current position, rotation, and positive scale, and reject an object not bound to that animator. Position and 3-by-3 orientation values use thousandths. Names remain exact strings rather than hashes.

Production PBR animation uses positive uniform node/socket/animation scale and the existing positive nonsingular object-transform contract. The converter rejects nonuniform production scale before publication; the legacy low-level simple path retains its existing nonuniform clip support and its PBR error-45 protection.

## Ownership

Objects refer to animators. Legacy animators refer to skeletons and the currently playing clip; clips refer to skeletons. Production animators instead refer to one compact model-owned immutable animation payload. Native retains only the nine aligned animation chunks rather than a second complete SM3D file; Web retains decoded bounded arrays without the fetched `ArrayBuffer` and keeps skin weights as uint16 until mesh publication. Destroy operations refuse while a live dependent exists and leave the public record intact: a bound object blocks animator destruction, and any live part object or model animator blocks model destruction. Reset clears objects, animators, models, materials, textures, meshes, clips, and skeletons in dependency order. Diagnostic APIs expose live/fixed maximum counts, handle validity, production capability/palette uploads, source file bytes, logical/resident animation bytes, bounded per-animator mutable bytes, fractional state, destination state, playback modes, event overflow/drop state, and pose revision.

SM3D v1 and static v2 files remain unchanged and load without animation. Animated v2 files add one wholly present optional nine-chunk group while preserving the static `VERT` stride. Skinned meshes can still be authored through the same public custom-mesh API; imported model animation is a production asset path, not a replacement for the bounded teaching API.

## Verification

`scripts/test-renderer3d-animation.ps1` preserves the legacy native/Web contract. `scripts/test-renderer3d-animation-v2.ps1` verifies deterministic animated import, 68- and 128-bone boundaries, 129-bone rejection, complete optional groups, exact uint16 weights, fixed sampling, independent production animators, loop/once/hold, crossfade, equal-time and multi-wrap FIFO events, root translation/yaw, raw/world sockets, PBR palette drawing and upload caching, ownership refusal, stale handles, malformed rollback, native/Web exact parity, and Animation Lab builds. `scripts/test-renderer3d-animation-v2-hardening.ps1` adds eight fractional speeds and split updates, irregular final intervals, moving/interrupted fades, time-zero and sticky-overflow behavior, compact-memory diagnostics, ten compact load/destroy cycles, and the original 8-bone/32-vertex/two-part articulated fixture with one- through four-influence vertices.
