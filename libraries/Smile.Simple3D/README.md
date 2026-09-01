# Smile.Simple3D

`Smile.Simple3D` 2.0.0 provides compatible teaching layers:

- `Graphics3D` and `Math3D` use the true indexed-triangle `Renderer3D` on Windows DirectX and WebGL2.
- `Character3D` shares animated SM3D character assets while giving each actor independent playback, transforms, events, sockets, and root motion.
- `Scene3D` supplies deterministic asset/render quality profiles, named lights, one selected shadow caster, HDR tone mapping, bloom, and balanced begin/end ownership over `Graphics3D`.
- Windows HDR Renderer3D prefers 4x MSAA and safely falls back to 2x or 1x; WebGL2 validates float-color targets and uses its documented single-sample fallback.
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

`Graphics3D.RendererAvailable()` is `True` for Windows DirectX and WebGL2. It is `False` on the GDI fallback. `ResourceEpoch3D` and `FrameActive3D` let high-level modules reconcile logical resets and frame ownership. Handles are bounded and validated; create geometry outside the frame loop, share meshes with `CreateObjectFromMesh3D`, destroy shared instances with `DestroyObjectInstance3D`, destroy owning objects with `DestroyObject3D`, and call `ResetRenderer3D` during final cleanup.

`Core.CameraControl3D` plus `Interaction.UpdateCameraControlsFromPointer` provides the shared primary-drag pan, middle-drag orbit, wheel zoom, lost-release recovery, and slow return behavior used by Dragonfall. Games retain control over which screen/world regions may start a gesture.

See [API.md](API.md), the [true Simple3D conformance sample](../../examples/Simple3DConformance/Program.smile), and [Neon Cycles](../../games/NeonCycles/README.md).

For an animated character, import the two high-level modules and keep the Renderer2D HUD after `EndScene`:

```smile
Import Smile.Simple3D.Core As Core
Import Smile.Simple3D.Graphics3D As Graphics3D
Import Smile.Simple3D.Scene3D As Scene3D
Import Smile.Simple3D.Character3D As Character3D

Dim Camera As Core.Camera3D
Dim Hero As Character3D.Actor
Dim FrameReady As Boolean

Camera = Graphics3D.DefaultCamera()
Hero = Character3D.LoadActor("Assets\Hero.sm3d")

FrameReady = Character3D.Place(Hero, 0, 0, 0)
FrameReady = Character3D.PlayAnimation(Hero, "Idle", True)
FrameReady = Scene3D.UseLighting("CharacterStudio")
FrameReady = Scene3D.Begin(Camera, 4, 8, 20)

If FrameReady Then
    FrameReady = Character3D.Draw(Hero)
    FrameReady = Scene3D.EndScene()
End If

Draw Text "Renderer2D HUD" At 20, 20 Size 20 Color WHITE
Show Screen
```

The keyword-shaped handoff names `Load`, `Play`, `Stop`, and `End` are reserved by the current SMILE grammar. The repository-conforming M4 names are `LoadActor`, `PlayAnimation`, `StopAnimation`, and `EndScene`.

Character3D transform changes are transactional across every model part. World position is bounded to +/-1,000,000, rotation input is bounded and normalized to 0-359 degrees, and scale is 1-1,000 percent. Advanced part/model/animator handles are borrowed read-only values; destroying them deliberately is treated as external tampering and quarantines only the affected actor or asset.

Low quality keeps the exact direct-LDR renderer and disables M5 shadow/bloom work. Medium enables a 1024 shadow, HDR tone mapping, and quarter-resolution bloom. High enables a 2048 shadow and half-resolution two-cycle bloom. `Scene3D.FallbackFlags()` reports independent effective downgrades, while `Character3D.SetShadows` applies cast/receive policy to every actor part transactionally. See [Renderer3DPostProcessingLab](../../examples/Renderer3DPostProcessingLab/README.md) for the native/Web controls and live M5 diagnostics.
