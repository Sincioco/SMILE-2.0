# Simple3D software-rendering architecture

The original `Smile.Simple3D` wireframe layer is a bounded educational source library. It transforms and projects integer wireframe geometry, then submits ordinary SMILE `Draw Line` commands. Windows DirectX, Windows GDI, and Web Canvas therefore share the same `.smile` geometry, camera, clipping, interaction, and game logic while the permanent 2D renderer remains responsible for final pixels and HUD overlays. Smile.Simple3D 2.0 preserves this API alongside the newer true [Renderer3D architecture](true-simple3d-renderer3d.md).

## Capability boundary

The wireframe modules consume only generic logical-canvas pointer input and ordinary Renderer2D calls. They know nothing about Space Wars, orbit policy, targets, or collision rules. Hardware rendering belongs to the separate Graphics3D/runtime contract and does not turn the permanent 2D backend vtable into a 3D abstraction.

This seam is now consumed by the separate first-class `Renderer3D`: applications may use the hardware path while preserving the wireframe lessons and existing 2D overlay path.

## Number model

SMILE `Number` is a signed 64-bit integer. Public values use world units, degrees, logical pixels, percentages, and milliseconds. `FixedMath` uses `16384` as its internal trigonometric scale and a deterministic rational sine approximation. Angles wrap to 0 through 359 degrees. Rotation order is X, then Y, then Z; camera inversion applies the reverse order. Positive Z is forward.

All public mesh coordinates are bounded to ±1,000,000. Primitive segment ranges are bounded before multiplication, projection rejects invalid camera values, near-plane interpolation checks its denominator, frame delta is clamped by the interaction layer, and no supported default can overflow the intended calculation range.

## Storage and ownership

`Mesh` owns a fixed process-local pool:

- 32 mesh slots;
- 768 vertices per mesh;
- 1,536 edges per mesh;
- generational integer handles, with zero invalid;
- checked vertex/edge indices;
- idempotent destroy and stale-handle rejection.

Primitive construction is transactional from the caller's perspective: any vertex or edge failure destroys the partially created mesh. Applications create reusable meshes during startup and destroy them during shutdown; rendering creates no mesh or unbounded collection per frame.

## Projection and clipping

The renderer transforms object vertices into world and then camera space. It clips every edge against the positive near plane before perspective division. Orthographic and perspective projection both produce logical-canvas coordinates. A bounded Cohen-Sutherland-style iteration then clips projected lines to the declared viewport before calling `Draw Line`.

`BeginFrame` resets counters and establishes a hard maximum of 2,500 emitted line passes. Optional glow is at most three extra offset passes. Over-budget lines are counted and dropped safely. Applications can inspect drawn and dropped counts without enabling noisy runtime diagnostics.

## Interaction

`Interaction.UpdateOrbit` is a pure, deterministic state update driven by explicit pressed/held/released values, delta, wheel, and elapsed milliseconds. It clamps pitch, velocity, distance, and frame delta, applies bounded friction, and reaches a dead zone. `UpdateOrbitFromPointer` is only a convenience adapter over the generic pointer built-ins. Capture loss and focus cleanup belong to the runtime input contract, not to this library.

## Non-goals

The legacy wireframe modules do not provide filled faces or hidden-surface removal. The sibling `Graphics3D` module now provides filled indexed primitives, simple lighting, perspective, and depth testing; textures, model import, animation, skeletal rigs, a scene graph, physics engine, student shaders, and general 3D collision remain outside this teaching layer.
