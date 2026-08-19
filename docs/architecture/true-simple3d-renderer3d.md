# True Simple3D Renderer3D architecture

SMILE 2.0 keeps Renderer2D as a permanent first-class layer and adds Renderer3D beside it. A 3D-capable frame renders indexed geometry first, restores the existing 2D target, draws HUD/menu/text/image commands, and presents once through the established `Show Screen` path.

## Source-facing boundary

Students use ordinary modules from `Smile.Simple3D` 2.0.0:

- `Core` owns `Vector3`, `Matrix4`, `Camera3D`, and `Object3D` value types.
- `Math3D` owns deterministic vector and matrix helpers.
- `Graphics3D` owns primitive/custom-mesh creation, transforms, appearance, frame submission, and explicit lifecycle.

The compiler has one narrow game-window-only `Renderer3D(command, a, ..., j)` built-in bridge. `Graphics3D` is the public teaching surface and hides its command values. This avoids new statement grammar, backend-specific APIs, game-specific runtime calls, and duplicate parser rules. The existing wireframe modules remain supported for GDI and older lessons.

## Windows DirectX backend

`graphics3d_directx.cpp` shares the active D3D11 device, context, swap chain render target, and resize lifecycle already owned by `graphics_directx.cpp`. It provides:

- immutable vertex/index buffers with generated averaged normals;
- model, look-at view, and perspective matrices;
- a small built-in HLSL shader pair compiled with `d3dcompiler.lib`;
- an output-size D24S8 depth texture recreated after device/size changes;
- indexed triangle lists with depth testing;
- 128 mesh and 256 object slots with typed generation-checked handles;
- explicit cleanup on destroy, reset, resize, and graphics shutdown.

`Begin3D` suspends the current Direct2D draw, binds D3D11/depth state, and clears the 3D target. `End3D` unbinds depth and resumes Direct2D on the same target. The 2D backend vtable and GDI renderer are unchanged. `RendererAvailable()` is false when DirectX is unavailable.

## Web backend

The generated four-file Web package remains exactly `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`. The runtime lazily creates one offscreen WebGL2 canvas, compiles built-in GLSL, uploads the same indexed vertices, enables `DEPTH_TEST`, and draws triangle lists. `End3D` composites that canvas into the existing Canvas 2D back buffer; subsequent SMILE 2D commands therefore remain painter-order overlays.

The Web renderer enforces the same live mesh/object limits, rejects deleted handles, regenerates its backing dimensions with the logical canvas, and recomputes perspective aspect from the current backing width and height. WebGL2 absence returns unavailable without changing Console programs or Renderer2D behavior.

## Primitive and lifecycle policy

Cube, plane, pyramid, sphere, cylinder, and torus geometry is generated inside each backend through one common command contract. Custom meshes declare fixed vertex/index capacity, set positions and triangle indices, then commit once. Commit validates all indices and calculates normals before GPU upload.

Geometry should be created outside the frame loop. An owning `Object3D` destroys both its object and mesh; shared-mesh instances destroy only the object slot. Neon Cycles preallocates bounded trail objects once, updates their transforms/visibility, and performs no resource allocation per simulation step or round.

## Deliberate limits

This milestone does not add textures, model loading, a scene graph, skeletal animation, rigid-body physics, shadows, particles, student shaders, networking, or a GDI 3D rasterizer. Renderer3D colors are simple lit RGBA tints. Logical gameplay/collision geometry remains application-owned and independent from render objects.
