# Shared arena

The 3D Character Viewer is the visual and interaction authority for native SMILE
arenas. `Smile.Simple3D` supplies the implementation for existing and future games
and tools. These are source-library additions, with no new language syntax,
compiler feature, dependency or renderer API.

`Arena3D` owns a reflective black floor and an emissive blue grid (RGB 24, 150, 255;
40-unit spacing; 2-unit lines). Width, depth, RGB, spacing and thickness remain
explicit creation options. `Place` moves both meshes and the reflection receiver
without moving actors. F toggles only the floor; G toggles only the grid.

`ArenaCamera3D` owns the Viewer interaction policy: left drag pans in screen space,
middle drag orbits, and the wheel changes a bounded, elapsed-time eased zoom target.
Fractional movement remains Double. Captured drags release over UI panels; panels
block new gestures. Pointer pitch is bounded to ±75 degrees. Scene scale and pan
bounds are caller inputs. Reset returns the camera to its initial framing.
Orbit composition uses the Viewer's authored-shot policy: yaw around world Y,
world-up orientation and final elevation clamped to ±1.48 radians (about 84.8°).
This final bound includes the base camera elevation, so diagonal/rear base views
cannot introduce roll or cross a pole even when a caller supplies larger controls.
`ComposeOrbit` supplies this shared calculation without close-up distance easing;
`Compose` adds that easing for ordinary arena consumers. The Viewer calls the same
operation for normal and authored-shot views, preserving its shot FOV range.
Viewer-specific responsive fit, calibration cursor anchoring, numeric pose editing
and authored battle cameras remain in the Viewer.

`ArenaBackdrop3D` owns the palette and bitmap lifetime. B always cycles Black →
Green → Purple → Landscape → Title → Black; startup selects Landscape, so the
first B selects Title. The two images come from the accepted Viewer assets.

`ArenaViewport3D` composes these owners for a normal program. Each scene owns its
own `State`; no game or tool owns shared library state. Create once after renderer
initialization, call `Update` with the queued key and UI-blocking flag, then call
`BeginFrame`, `Draw`, and the ordinary scene end operation. Destroy before changing
scenes. Supply a base camera appropriate to the actors; the shared controls work
relative to that camera. Floor visibility also keeps the live camera above ground.
Screen-space composition derives its right/up basis from that base camera, including
rear views from positive Z. The Sin Star I battle exposed an inverted view when the
old helper assumed world +X was always screen right; native Precision3D regression
checks now cover upright projection and both pan directions for the rear view.
The base camera describes reset framing; the default zoom offset does not narrow
an existing scene's field of view. `ComposeCamera` supplies that same live camera
before drawing when a host needs screen picking or labels.
Use `BeginFrame(..., False)` for programs that close frames directly through
Graphics3D instead of Scene3D.

```smile
Import Smile.Simple3D.ArenaViewport3D As Arena
Import Smile.Simple3D.Precision3D As P
Import Smile.Simple3D.Scene3D As Scene

Dim Stage As Arena.State
Dim BaseCamera As P.Camera3D
Dim Ready As Boolean
Dim Key As Number

Game Window "My Arena" Size 1280 By 720

Stage = Arena.Create(1000, 800)
BaseCamera.Position = P.Vector(0.0, 180.0, -500.0)
BaseCamera.Target = P.Vector(0.0, 60.0, 0.0)
BaseCamera.UpDirection = P.Vector(0.0, 1.0, 0.0)
BaseCamera.FovDegrees = 55.0
BaseCamera.NearPlane = 1.0
BaseCamera.FarPlane = 3000.0

Do

    Get Key Key

    Call Arena.Update(Stage, Key)

    Ready = Arena.BeginFrame(Stage, BaseCamera)
    Ready = Arena.Draw(Stage) And Ready
    Ready = Scene.EndScene() And Ready

    Show Screen

Loop Until Game_Closed()

Call Arena.Destroy(Stage)
```

Reference `libraries/Smile.Simple3D/Smile.Simple3D.smilelibproj` in the application
project. Library projects currently cannot publish assets. Run
`scripts/copy-arena-assets.ps1 -ProjectDirectory <application-folder>` when creating
or updating a consumer, then include and commit its two bitmap copies:

```xml
<Asset Include="Assets\Backgrounds\SinStarLandscape.png" />
<Asset Include="Assets\Backgrounds\SinStarTitleWithLogo.png" />
```

Existing broad `Assets\**\*` declarations already include them. The native build
publishes both files beside the executable; no network access or optional download
is needed at runtime. Copies have the same Git content identity as their source.

Native consumers: Character Viewer; Sin Star I character/battle presentations and
Paladin Combat Lab; Fire, Lightning, Water and Earth Labs; Dragonfall and its two
character previews; Neon Cycles; Character3D, Animation, Post Processing, Renderer
VFX, Aether Blade and Lightning previews; Precision Curve; and hardware Simple3D
Conformance. Renderer unit fixtures keep deliberate test surfaces/materials.
The older Draw Line wireframe teaching gallery is a 2D projection example, not a
hardware arena. Studio and Web adoption/publication/acceptance remain on hold.

Regression evidence lives in `examples/Arena3DTests`, the Viewer hardening checks
and the native Sin Star I presentation fixture. Keep the four shared owners as
the maintenance boundary; new programs must not copy private camera/palette code.

## Native validation and ownership review — September 20, 2026

- 53 focused native arena checks pass: fractional pan, slow/moderate orbit inputs,
  capture/release, bounded eased zoom, independent F/G, all five rendered backgrounds,
  custom grid spacing, reset framing and resource release.
- Viewer NativeOnly hardening and architecture contracts pass, as do Sin Star I's
  native character/party presentation and music checks. The 13 formatter checks
  and 480-file repository style check pass. No guardrail exception was introduced.
- Native Viewer, Sin Star I, all four VFX Labs and 14 arena previews/game variants
  compile. Brief live checks cover Viewer, Water, Fire and Neon Cycles; the latter
  exposed and now verifies the fixed reset-framing regression. MMB input policy is
  exercised by native input packets; the desktop automation API cannot drag with
  the middle button, so physical MMB acceptance is not claimed.
- Dragonfall's three-cycle native fixture verifies pressure-failure cleanup, screen
  picking, rendered frames and zero-resource teardown. The arena replacement
  removes 37 objects and meshes; current initial ownership is 404 objects, 11 meshes,
  22 materials and seven textures. Authored camera framing remains unchanged.
- New library owners are 114 lines (background), 167 (camera) and 121 (viewport).
  Existing ViewerCamera shrinks by 46 lines, ViewerRendering by 59, ViewerWorkflow
  by six, Fire Program by 97, Lightning Program by 101 and DragonfallScene by 95.
  Specialized viewer editing, game logic and authored shots stay with their hosts.
- Arin and Orin calibration exports match their canonical JSON without changing
  bytes. Studio and Web remain held; no .NET or VSIX rebuild is required for this
  source-library adoption. Existing native consumers must be recompiled to adopt it.

## Constrained diagonal orbit correction

Sin Star I exposed roll when the old composition rotated a diagonal base camera
and its up vector around fixed world Euler axes. Sharing pointer pitch limits was
insufficient. The existing Viewer shot algorithm now lives in `ArenaCamera3D`;
both Viewer routes and ordinary arena composition delegate there. No new mutable
state, dependency, runtime feature or architecture exception is introduced.
The shared owner gains 42 net lines; ViewerCamera loses 24 net lines. The source
contract check now requires the shared call rather than the superseded helper.

The added diagonal/rear-camera regression failed ten checks before the fix;
all 67 arena checks pass afterward. It covers four yaw quadrants, upright world
vertical projection, both pitch extremes, existing slow/moderate input packets,
pan, eased zoom, reset and floor/background controls. Physical middle-button
drag acceptance remains a user check; native automation cannot inject that drag.
