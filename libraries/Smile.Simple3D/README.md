# Smile.Simple3D

`Smile.Simple3D` 2.0.0 provides two compatible teaching layers:

- `Graphics3D` and `Math3D` use the true indexed-triangle `Renderer3D` on Windows DirectX and WebGL2.
- Windows Renderer3D automatically prefers 4x MSAA and safely falls back to 2x or 1x; WebGL2 requests browser-provided anti-aliasing.
- `Renderer`, `Primitives`, `Mesh`, and `Interaction` preserve the original bounded wireframe lessons over Renderer2D, including GDI support.

Reference the source library from a game project:

```xml
<SmileProjectReference Include="..\..\libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj" />
```

The beginner true-3D path needs only two imports; add `Smile.Simple3D.Interaction` when the game needs standard camera controls:

```smile
Import Smile.Simple3D.Core As Core
Import Smile.Simple3D.Graphics3D As Graphics3D
Import Smile.Simple3D.Interaction As Interaction
```

Create objects once, update their transforms, draw the 3D pass, then draw the ordinary 2D HUD:

```smile
Dim Camera As Core.Camera3D
Dim Cube As Core.Object3D
Dim FrameReady As Boolean

Camera = Graphics3D.DefaultCamera()
Cube = Graphics3D.CreateCube3D(200)

Call Graphics3D.SetObjectColor(Cube, 30, 220, 255)
FrameReady = Graphics3D.Begin3D(Camera, 2, 5, 14)

If FrameReady Then
    FrameReady = Graphics3D.DrawObject3D(Cube)
    Call Graphics3D.End3D()
End If

Draw Text "Renderer2D HUD" At 20, 20 Size 20 Color WHITE
Show Screen
```

`Graphics3D.RendererAvailable()` is `True` for Windows DirectX and WebGL2. It is `False` on the GDI fallback. Handles are bounded and validated; create geometry outside the frame loop, share meshes with `CreateObjectFromMesh3D`, destroy shared instances with `DestroyObjectInstance3D`, destroy owning objects with `DestroyObject3D`, and call `ResetRenderer3D` during final cleanup.

`Core.CameraControl3D` plus `Interaction.UpdateCameraControlsFromPointer` provides the shared primary-drag pan, middle-drag orbit, wheel zoom, lost-release recovery, and slow return behavior used by Dragonfall. Games retain control over which screen/world regions may start a gesture.

See [API.md](API.md), the [true Simple3D conformance sample](../../examples/Simple3DConformance/Program.smile), and [Neon Cycles](../../games/NeonCycles/README.md).
