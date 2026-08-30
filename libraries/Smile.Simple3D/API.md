# Smile.Simple3D 2.0 API

Public 3D positions and sizes use integer world units. Rotations use integer degrees, scales and opacity use percentages, and `Matrix4` values use `Core.FIXED_ONE` fixed point. A zero handle means failure.

## `Smile.Simple3D.Core`

True-3D types:

- `Vector3`: `X`, `Y`, and `Z`.
- `Matrix4`: `M11` through `M44`, scaled by `FIXED_ONE`.
- `Camera3D`: position, target, projection fields, near/far planes, FOV, and the legacy wireframe viewport fields.
- `CameraControl3D`: composable pan, wheel-zoom, middle-drag orbit, and return-spring state.
- `Object3D`: validated object/mesh handles plus mirrored position, rotation, scale, color, opacity, and visibility values.

The legacy wireframe types and limits remain source compatible.

## `Smile.Simple3D.Math3D`

- `Vector`, `Add`, `Subtract`, `MultiplyScalar`, `Dot`, `Cross`, `Length`, `Normalize`, and `Distance`.
- `Identity`, `Translation`, `Scale`, `RotationX`, `RotationY`, `RotationZ`, and `Multiply`.
- `TransformPoint`, `Perspective`, and `LookAt`.

Normalization returns a vector with length `FIXED_ONE`; normalizing zero returns zero. Matrix operations are deterministic integer operations shared by Windows and Web.

## `Smile.Simple3D.Graphics3D`

Availability and lifecycle:

- `RendererAvailable()`
- `LastError()`
- `ResetRenderer3D()`
- `LiveMeshCount3D()` and `LiveObjectCount3D()`
- `MaximumMeshCount3D()` and `MaximumObjectCount3D()`
- `DrawCallCount3D()` and `SubmittedTriangleCount3D()` for the current or most recently ended 3D frame; a successful new `Begin3D` resets both to zero
- `MeshHandleValid3D(Mesh)` and `ObjectHandleValid3D(Object)`
- `MeshReferenceCount3D(Mesh)`
- `DestroyObject3D(ByRef Object)` for an object and its owned mesh
- `DestroyObjectInstance3D(ByRef Object)` for an instance using a shared mesh

Camera and frame:

- `DefaultCamera()`
- `Begin3D(Camera, Red, Green, Blue)`
- `DrawObject3D(Object)`
- `End3D()`

Primitive objects:

- `CreateCube3D(Size)`
- `CreatePlane3D(Width, Depth)`
- `CreatePyramid3D(Size, Height)`
- `CreateSphere3D(Radius)`
- `CreateCylinder3D(Radius, Height)`
- `CreateTorus3D(MajorRadius, MinorRadius)` and `CreateDonut3D(...)`

Custom indexed meshes:

- `CreateMesh3D(VertexCount, IndexCount)`
- `SetMeshVertex3D(Mesh, Index, X, Y, Z)`
- `SetMeshTriangle3D(Mesh, TriangleIndex, A, B, C)`
- `CommitMesh3D(Mesh)`
- `CreateObjectFromMesh3D(Mesh)`
- `MeshVertexCount3D(Mesh)` and `MeshIndexCount3D(Mesh)`

Transforms and appearance:

- `SetObjectPosition` and `MoveObject`
- `SetObjectRotation` and `RotateObject`
- `SetObjectScale`
- `SetObjectColor`
- `SetObjectOpacity`
- `SetObjectVisible`

## `Smile.Simple3D.Interaction`

The standard 3D camera-control contract is renderer-independent and deterministic:

- `ResetCameraControls`
- `UpdatePanZoomControls` for simultaneous primary-drag pan and wheel zoom
- `UpdateOrbitControls` for independent middle-drag orbit
- `UpdateCameraControlsFromPointer` for the conventional primary/middle/wheel binding
- `AdvanceCameraControls` for the bounded slow return spring, including paused scenes
- `ApplyCameraControls` to compose an override over an authored `Camera3D` without drift
- `CameraControlsDragging` and `CameraControlsActive`

Games decide whether a press started on valid world geometry and pass that decision through `AllowPanStart` or `AllowOrbitStart`. Once accepted, the gesture retains capture until release; a missing Web-canvas release is recovered when the button is no longer held. Pan, zoom, and orbit remain mutually composable.

## Renderer contract

Windows uses D3D11 indexed triangle lists, generated normals, model/view/perspective matrices, a resize-aware D24S8 depth buffer, and hardware-selected 4x/2x/1x multisample anti-aliasing before the existing Direct2D HUD pass. Web uses an antialiased offscreen WebGL2 canvas with the same indexed mesh and depth contract, then composites it into the Canvas 2D back buffer before ordinary 2D drawing.

Both backends bound live data to 128 meshes and 512 objects and reject stale or deleted handles. Mesh destruction is rejected while a live object still references that mesh. Meshes support at most 65,535 vertices and 196,608 indices.

An object returned by a primitive creator or chosen as the owner of a custom mesh must outlive every shared instance. Destroy shared instances with `DestroyObjectInstance3D` before destroying the owning object with `DestroyObject3D`. `ResetRenderer3D` is the scene/battle ownership boundary and invalidates every outstanding Renderer3D handle without changing Renderer2D state.

## Legacy wireframe modules

`FixedMath`, `Mesh`, `Primitives`, `Renderer`, and the original `OrbitState3D` interaction calls remain source compatible with the deterministic wireframe examples, GDI builds, and Space Wars. `Interaction` also exposes the reusable true-3D camera-control contract above.
