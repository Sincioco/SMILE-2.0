# Renderer3D textures and materials

`Smile.Simple3D.Graphics3D` provides reusable PNG textures and materials for every 3D application. The renderer has no knowledge of battles, characters, or individual games.

## Public API

- `LoadTexture3D(path, filter, wrap)` loads a declared PNG asset through SMILE's existing `Image` cache and retains it for Renderer3D.
- `DestroyTexture3D` releases the texture only when no material still refers to it.
- `CreateMaterial3D` and `SetMaterial3D` configure a texture, opaque/cutout/alpha-blend mode, RGB tint, opacity, unlit lighting, emissive intensity, and cutout threshold.
- `DefaultMaterial3D` creates a white opaque material.
- `SetObjectMaterial3D` binds a material to an object; `ClearObjectMaterial3D` returns to the built-in untextured lit material.
- `SetMeshUv3D` writes normalized UV coordinates as integer thousandths before `CommitMesh3D`.

Filter values are nearest or linear. Wrap values are clamp or repeat. Material and texture records are lightweight typed handles, while the renderer owns their GPU resources.

## Ownership and validation

The dependency order is object -> material -> texture and object -> mesh. Destroy operations fail without altering the public record while a dependent is live. `ResetRenderer3D` destroys resources in dependency order. Diagnostics expose live/fixed maximum counts, validity, dimensions, and reference counts.

Native textures retain the decoded `SmileImageResource`, lazily upload BGRA pixels to D3D11, and recreate only the GPU view and sampler after a device loss. Web textures retain the shared HTML image and lazily upload it to WebGL2. Asset publication remains format-neutral and unchanged.

Invalid filters, wrap modes, alpha modes, dimensions, percentages, stale handles, and resource exhaustion return a zero/false result. Missing or malformed PNG input follows the existing `Load Image` contract and reports a clear asset load failure before a texture handle exists.

## Render behavior

Opaque and cutout materials write depth. Alpha-blended materials use source-alpha blending with depth reads and no depth writes. Unlit materials bypass diffuse lighting; emissive intensity adds controlled self-lighting. Object tint/opacity multiply material tint/opacity. The native shader compensates for the image cache's premultiplied BGRA representation; the Web shader consumes browser-decoded straight alpha.

Renderer3D still ends before 2D composition. Text, HUD, menus, and images therefore retain their established Canvas 2D/Direct2D behavior and draw on top of the 3D scene.

## Verification

`scripts/test-renderer3d-materials.ps1` compiles and executes the same assertions on Windows and Web. It covers PNG creation, invalid configuration, texture sharing, every alpha mode, unlit/emissive settings, dependency-safe destruction, zero-count cleanup, a real 3D draw, and a subsequent 2D frame.
