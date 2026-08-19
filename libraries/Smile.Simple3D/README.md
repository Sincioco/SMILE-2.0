# Smile.Simple3D

Smile.Simple3D is SMILE 2.0's dependency-free educational wireframe 3D library. It uses bounded fixed-point math and the existing cross-target `Draw Line` path, so one SMILE program runs through Windows DirectX, Windows GDI, and Web Canvas.

Reference it from a game project:

```xml
<SmileProjectReference Include="..\..\libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj" />
```

Import the modules you need:

```smile
Import Smile.Simple3D.Core As Core
Import Smile.Simple3D.Primitives As Primitives
Import Smile.Simple3D.Renderer As Renderer
```

Create geometry once, draw it each frame, then destroy it:

```smile
Dim Cube As Number
Dim Camera As Core.Camera3D
Dim Transform As Core.Transform3D

Cube = Primitives.CreateCube(200)
Camera = Renderer.DefaultCamera(960, 540)
Transform = Renderer.IdentityTransform()

Call Renderer.BeginFrame(Core.DEFAULT_LINE_BUDGET)
Call Renderer.DrawMesh(Cube, Transform, Camera, CYAN, 1)
```

For drag/throw orbiting, initialize `Core.OrbitState3D` with `Interaction.ResetOrbit` and call `Interaction.UpdateOrbitFromPointer` before `Show Screen`. The lower-level `UpdateOrbit` accepts explicit input and is preferable in deterministic tests.

Meshes are bounded generational handles. Check a returned handle for zero, reuse meshes instead of creating them every frame, and call `Mesh.DestroyMesh` during shutdown. See [API.md](API.md) for capacities, units, and every routine. See the [Simple3D Gallery](../../examples/Simple3DGallery/README.md) for a complete cube/sphere/pyramid/donut lesson.
