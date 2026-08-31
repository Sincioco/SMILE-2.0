# Renderer3D skeletal animation

Renderer3D provides bounded, generation-safe skeleton, animation-clip, and animator resources. The system is generic: it has no character, battle, action, or game-specific concepts.

## Data model

A `Skeleton3D` has 1-32 parent-ordered bones. Each bone declares a parent index and a local bind-pivot translation. Root bones use parent `-1`; a child parent must precede it. `CommitSkeleton3D` validates the complete acyclic hierarchy and derives inverse global bind translations before the skeleton can create clips or animators.

A mesh vertex can receive four bone indices and four normalized thousandth weights through `SetMeshSkin3D`. Weights must total 1000. Binding an animator to an object validates the mesh's highest weighted joint against the animator's skeleton before rendering.

An `AnimationClip3D` contains optional two-key translation, quaternion-rotation, and scale tracks per bone. Missing tracks use the bind translation, identity rotation, or unit scale. Translation/scale use linear interpolation. Quaternion keys use normalized shortest-path interpolation. Two-key clips deliberately keep the student API small while supporting idle, attack, cast, hit, KO/hold, victory, and creature motion.

The simple shader retains existing support for nonuniform bone-scale keys. The current PBR tangent/normal skinning profile supports uniform scale only: setting or replacing any scale track recomputes a cached clip-safety bit, and a PBR draw using an unsafe active clip fails before submission with Renderer3D error 45. This keeps animation resources reusable across both paths without rejecting legacy/simple animation creation. A future inverse-transpose or dual-quaternion skinning milestone may broaden the PBR profile.

Clips may contain up to 16 time-ordered integer event IDs. `TakeAnimationEvent3D` consumes a pending event exactly once. Events cross correctly during ordinary and looping updates.

An `Animator3D` owns independent playback time, speed, loop/completion state, event state, and a fixed 32-matrix palette. `UpdateAnimator3D(deltaMilliseconds)` advances integer time independently of visual frames. Once clips clamp and hold the final pose; loops wrap deterministically. Any number of objects may share an animator when their meshes use the same skeleton, while separate actors normally use separate animators.

## GPU parity

The vertex format is position, normal, UV, four float joint indices, and four weights. Static meshes default to bone zero with full weight, but skinning is disabled unless an animator is bound.

DirectX uploads a fixed row-major 32-matrix palette in the existing constant buffer. The D3D11 vertex shader blends four bone matrices and skins position/normal before applying model/view/projection transforms. WebGL2 uses the equivalent `mat4 bones[32]` uniform palette and vertex attributes. Thirty-two bones remain beneath the WebGL2 minimum vertex-uniform budget after camera/model uniforms and are enough for the intentionally low-poly actors.

Animation evaluation is CPU-side and deterministic; deformation is GPU-side on both targets. The Web animator reuses a preallocated `Float32Array` palette, and the native animator uses inline fixed storage. No animation resource allocates inside its update path.

## Ownership

Objects refer to animators; animators refer to skeletons and the currently playing clip; clips refer to skeletons. Destroy operations refuse while a live dependent exists and leave the public record intact. Reset clears objects, then animators, clips, and skeletons. Diagnostic APIs expose live/fixed maximum counts and handle validity.

SM3D v1 and the v2 M1 core remain static-model interchanges. V2 reserves later skeleton/clip/event/root-motion/socket chunks but does not emit or interpret them yet. Skinned meshes can still be authored through the same public custom-mesh API; bundled M3 sections are an authoring convenience rather than a replacement for the bounded runtime resource API.

## Verification

`scripts/test-renderer3d-animation.ps1` performs the same mechanics and draw assertions on DirectX and Web. It covers hierarchy validation, invalid bone references, bind pose, four-weight skin data, translation/rotation/scale interpolation, idle looping, attack completion, hit final-pose hold, victory looping, dragon-wing bones, exact-once events, independent animators, invariant 30/60/120-style update totals, live-owner refusal, stale handles, GPU drawing, and zero-count cleanup.
