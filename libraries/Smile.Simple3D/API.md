# Smile.Simple3D 2.0 API

Public 3D positions and sizes use integer world units. Rotations use integer degrees, scales and opacity use percentages, and `Matrix4` values use `Core.FIXED_ONE` fixed point. A zero handle means failure.

## `Smile.Simple3D.Core`

True-3D types:

- `Vector3`: `X`, `Y`, and `Z`.
- `Matrix4`: `M11` through `M44`, scaled by `FIXED_ONE`.
- `Camera3D`: position, target, projection fields, near/far planes, FOV, and the legacy wireframe viewport fields.
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

## Renderer contract

Windows uses D3D11 indexed triangle lists, generated normals, model/view/perspective matrices, a resize-aware D24S8 depth buffer, and the existing Direct2D renderer for the following HUD pass. Web uses an offscreen WebGL2 canvas with the same indexed mesh and depth contract, then composites it into the Canvas 2D back buffer before ordinary 2D drawing.

The native backend bounds live data to 128 meshes and 256 objects and uses generation-checked typed handles. The Web backend enforces the same live-resource limits and rejects deleted handles. Meshes support at most 65,535 vertices and 196,608 indices. Renderer3D does not include textures, model loading, a scene graph, skeletal animation, rigid-body physics, or user shaders.

## Legacy wireframe modules

`FixedMath`, `Mesh`, `Primitives`, `Renderer`, and `Interaction` remain available exactly for the original deterministic wireframe examples, pointer orbit lessons, GDI builds, and Space Wars. Their API is unchanged by 2.0.0.
