# True Simple3D Renderer3D architecture

SMILE 2.0 keeps Renderer2D as a permanent first-class layer and adds Renderer3D beside it. A 3D-capable frame renders indexed geometry first, restores the existing 2D target, draws HUD/menu/text/image commands, and presents once through the established `Show Screen` path.

## Source-facing boundary

Students use ordinary modules from `Smile.Simple3D` 2.0.0:

- `Core` owns `Vector3`, `Matrix4`, `Camera3D`, `Object3D`, `Model3D`, `Animator3D`, and `RootMotionDelta3D` value types.
- `Math3D` owns deterministic vector and matrix helpers.
- `Graphics3D` owns primitive/custom-mesh creation, transforms, appearance, frame submission, and explicit lifecycle.
- `Scene3D` owns quality profiles, named lighting presets, and balanced 3D-frame entry/exit.
- `Character3D` owns a bounded shared-model cache plus generation-safe animated actor instances.

The compiler has narrow game-window-only numeric, image-owning, and text-path Renderer3D bridges. `Graphics3D` is the public teaching surface and hides their command values. The image bridge lets Renderer3D retain the existing decoded `Image` resource instead of duplicating PNG decoding, while the text bridge resolves model paths through the existing exact asset manifest. This avoids new statement grammar, backend-specific APIs, game-specific runtime calls, and duplicate parser rules. The existing wireframe modules remain supported for GDI and older lessons.

## Windows DirectX backend

`graphics3d_directx.cpp` shares the active D3D11 device, context, swap chain render target, and resize lifecycle already owned by `graphics_directx.cpp`. It provides:

- immutable position/normal/UV/joint/weight vertex and index buffers with generated or explicit normals;
- model, look-at view, and perspective matrices;
- legacy 32-matrix and production 128-matrix animation palettes, with the production palette in vertex constant-buffer slot b1;
- a small built-in HLSL shader pair compiled with `d3dcompiler.lib`;
- a separately compiled PBR-lite HLSL pipeline with tangent input, four texture channels, bounded lights, blend/depth policy, and single/double-sided raster states;
- an output-size D24S8 depth texture recreated after device/size changes;
- indexed triangle lists with depth testing;
- 128 mesh, 512 object, 128 texture, and 128 material slots with typed generation-checked handles;
- explicit cleanup on destroy, reset, resize, and graphics shutdown.

`Begin3D` suspends the current Direct2D draw, binds D3D11/depth state, and clears the 3D target. The native renderer prefers a 4x multisampled color/depth pair, falls back to 2x or 1x according to device support, and keeps the flip-model swap chain single-sampled. `End3D` resolves the multisampled 3D image into the swap-chain back buffer before resuming Direct2D, so the HUD remains sharp and ordinary 2D painter order is unchanged. The 2D backend vtable and GDI renderer are unchanged. `RendererAvailable()` is false when DirectX is unavailable.

## Web backend

The generated four-file Web package remains exactly `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`. The runtime lazily creates one offscreen WebGL2 canvas, compiles the simple and optional PBR-lite GLSL programs once, uploads the same indexed vertices, enables `DEPTH_TEST`, and draws triangle lists. PBR drawing reuses fixed matrices/light arrays, creates no typed arrays in the draw path, and restores explicit blend/depth/cull/program state before a following simple draw. Production animation uses one shared RGBA32F 4-by-128 vertex palette texture with cached animator/revision uploads; legacy 32-bone uniforms remain unchanged. `End3D` composites the canvas into the existing Canvas 2D back buffer; subsequent SMILE 2D commands therefore remain painter-order overlays.

The Web renderer enforces the same live resource limits, rejects deleted handles, regenerates its backing dimensions with the logical canvas, and recomputes perspective aspect from the current backing width and height. WebGL2 absence returns unavailable without changing Console programs or Renderer2D behavior.

## Primitive and lifecycle policy

Cube, plane, pyramid, sphere, cylinder, and torus geometry is generated inside each backend through one common command contract. Custom meshes declare fixed vertex/index capacity, set positions and triangle indices, then commit once. Commit validates all indices and calculates normals before GPU upload.

Geometry should be created outside the frame loop. An owning `Object3D` destroys both its object and mesh; shared-mesh instances destroy only the object slot. Neon Cycles preallocates bounded trail objects once, updates their transforms/visibility, and performs no resource allocation per simulation step or round.

The public diagnostics expose live counts, fixed capacities, handle validity, reference counts, PBR/simple draw counts, triangles, lights, samplers, imported ownership, animation palettes, logical resource epoch/frame state, M5 submissions/passes, effective targets, target bytes, and independent fallbacks. A resource cannot be destroyed while a live dependent refers to it: objects retain meshes, materials, and optional animators; materials retain textures; and an SM3D v2 model atomically owns its part meshes, imported materials/textures, and immutable animation payload. An owning object must outlive its shared instances. Model-part objects and production model animators must be destroyed before their model. `ResetRenderer3D` ends any active 3D pass, clears the fixed submission queue, releases shadow/HDR/bloom targets, destroys objects before animators and models, then releases materials, textures, meshes, legacy clips, and skeletons; it invalidates every handle, advances the resource epoch once, and leaves Renderer2D available. Device/context loss does not advance the epoch because retained logical resources remain valid and recreate GPU state lazily. Native object handles reserve nine slot bits so every entry in the 512-object pool remains reachable while retaining generation-based stale-handle rejection; smaller resource pools keep the original eight-bit layout. Web handles are never reused and deleted handles remain invalid.

Texture and material details are documented in [Renderer3D textures and materials](renderer3d-materials.md).

## Command ABI allocation

The compiler bridge is append-only. Numeric commands now occupy 1-117, image commands 1-2, and text commands 1-9. M5 adds `113 CONFIGURE_POST`, `114 CONFIGURE_SHADOW`, `115 SET_SHADOW_AREA`, `116 SET_OBJECT_SHADOWS`, and `117 M5_VALUE`; it adds no image or text command. Native dispatch is the `smile_graphics3d_command` switch in `graphics3d_directx.cpp` through the declarations in `graphics3d.h`. Web dispatch is the generated `renderer3DCommand` switch in `WebOutputWriter.cs`; both are reached through the existing game-window numeric bridge emitted by the compiler. The next free IDs are numeric 118, image 3, and text 10. The exact positional mapping, properties, errors, and fallback bits are recorded in the M5 implementation report.

## Deliberate limits

M5 deliberately remains one shadow caster and one bounded post chain. It does not add runtime glTF import, a general render graph or scene graph, rigid-body physics, point/cascade shadows, IBL, SSAO, motion blur, depth of field, TAA, particles/VFX Generation 2, student shaders, networking, WebGPU, ray tracing, or a GDI 3D rasterizer. glTF is converted offline to the bounded SM3D runtime format. `Scene3D` is a small frame/quality/lighting/post facade, not a replacement renderer. Logical gameplay/collision geometry remains application-owned and independent from render objects.
