# Smile.Simple3D API

Public units are integer world units, degrees, logical canvas pixels, percentages, and milliseconds. Zero is an invalid mesh handle. Boolean/query failures return `False`; numeric queries on invalid handles return zero except invalid edge indices, which return `-1`.

## `Smile.Simple3D.Core`

Constants: `FIXED_ONE` (16384), `ANGLE_FULL` (360), `PROJECTION_PERSPECTIVE`, `PROJECTION_ORTHOGRAPHIC`, `MAX_MESHES` (32), `MAX_VERTICES` (768 per mesh), `MAX_EDGES` (1536 per mesh), `DEFAULT_LINE_BUDGET` (2500), and `MAX_WORLD_COORDINATE` (1,000,000).

Types:

- `Vector3`: `X`, `Y`, `Z` world values.
- `Rotation3D`: `X`, `Y`, `Z` integer degrees.
- `Transform3D`: `Position`, `Rotation`, `ScalePercent`.
- `Camera3D`: position/rotation, projection mode, near plane, focal length, orthographic scale, and logical viewport.
- `ProjectedPoint3D`: projected `X`, `Y`, camera-space `Depth`, and `Visible`.
- `OrbitState3D`: yaw, pitch, inertial velocities, distance, and dragging state.

## `Smile.Simple3D.FixedMath`

- `WrapDegrees(Angle)`: wraps any integer angle to 0–359.
- `SinFixed(Angle)`, `CosFixed(Angle)`: return values scaled by `FIXED_ONE`.
- `MultiplyFixed(LeftValue, RightValue)`: fixed multiply.
- `DivideFixed(Numerator, Denominator)`: fixed divide; zero denominator returns zero.
- `RotateX`, `RotateY`, `RotateZ`, `Rotate`: rotate `Vector3` values; combined order is X/Y/Z.

## `Smile.Simple3D.Mesh`

- `CreateMesh()`: allocates a clean bounded slot or returns zero.
- `DestroyMesh(Handle)`, `ClearMesh(Handle)`: safe for invalid/stale handles.
- `IsValid`, `VertexCount`, `EdgeCount`.
- `AddVertex(Handle, X, Y, Z)`: returns the zero-based index or `-1`.
- `AddEdge(Handle, StartVertex, EndVertex)`: validates both existing indices.
- `VertexX`, `VertexY`, `VertexZ`, `EdgeStart`, `EdgeEnd`: checked renderer/lesson accessors.

## `Smile.Simple3D.Primitives`

- `CreateCube(Size)`: 8 vertices, 12 edges.
- `CreatePyramid(Size, Height)`: 5 vertices, 8 edges.
- `CreateSphere(Radius, Segments, Rings)`: segments 6–32, rings 3–16.
- `CreateDonut(MajorRadius, MinorRadius, MajorSegments, MinorSegments)`: major segments 6–32, minor segments 4–16.
- `CreateAxes(Length)` and `CreateGrid(HalfLines, Spacing)`.

Each returns zero on invalid arguments, capacity exhaustion, or partial construction failure. Partial meshes are destroyed before return.

## `Smile.Simple3D.Renderer`

- `DefaultCamera(ViewWidth, ViewHeight)` and `IdentityTransform()`.
- `TransformPoint`, `WorldToCamera`, `ProjectPoint`.
- `BeginFrame(MaximumLines)`: resets draw/drop counters; values outside 1–2500 select 2500.
- `FrameLinesDrawn()`, `FrameLinesDropped()`.
- `DrawLine3D(First, Second, Camera, LineColor, GlowPasses)`.
- `DrawMesh(Handle, Transform, Camera, LineColor, GlowPasses)`.
- `SphereVisible(Center, Radius, Camera)`: conservative near-plane visibility.

Perspective projection clips in camera space before division. Both modes clip in logical viewport space. Glow is clamped to 0–3 passes.

## `Smile.Simple3D.Interaction`

- `ResetOrbit(ByRef Orbit)` and `CancelOrbit(ByRef Orbit)`.
- `UpdateOrbit(ByRef Orbit, Pressed, Held, Released, DeltaX, DeltaY, WheelDelta, ElapsedMilliseconds)`: pure bounded input update.
- `UpdateOrbitFromPointer(ByRef Orbit, ElapsedMilliseconds)`: adapter over `POINTER_PRIMARY` and the pointer built-ins.
