# Character3D and Scene3D tutorial

`Character3D` is the smallest beginner-facing layer for an animated SM3D v2 character. `Scene3D` adds quality-aware asset preparation, named lighting, and balanced 3D frame ownership. Both are ordinary SMILE modules over `Graphics3D`; they do not create a second renderer or hide gameplay rules.

## Prepare a project

Reference `libraries\Smile.Simple3D\Smile.Simple3D.smilelibproj`, declare the converted `.sm3d` file as an `Asset`, and import:

```smile
Import Smile.Simple3D.Core As Core
Import Smile.Simple3D.Graphics3D As Graphics3D
Import Smile.Simple3D.Scene3D As Scene3D
Import Smile.Simple3D.Character3D As Character3D
```

An M4 character asset must contain validated geometry plus animation. Convert glTF/GLB offline through `smileasset model`; runtime glTF parsing is not provided.

## Load and animate

```smile
Dim Hero As Character3D.Actor
Dim Updated As Boolean

Updated = Scene3D.SetQuality(Scene3D.QUALITY_AUTO)
Hero = Character3D.LoadActor("Assets\Hero.sm3d")
Updated = Character3D.Place(Hero, -120, 0, 80)
Updated = Character3D.SetScale(Hero, 20000)
Updated = Character3D.PlayAnimation(Hero, "Idle", True)
```

The first load creates one cached model, one animator, and one object per model part. A second actor loaded from the exact same path, asset profile, and actual PBR/simple variant shares the model but owns independent playback and part objects. Request policy controls admission, not identity. Destroying the last actor immediately releases the cache entry unless an external dependent refuses model cleanup; `RetryPendingReleases` completes that bounded pending release later.

Use exact clip names:

```smile
If Key_Pressed(KEY_SPACE) Then
    Updated = Character3D.CrossFade(Hero, "AttackLike", 160)
End If

Updated = Character3D.Update(Hero, ElapsedMilliseconds)
```

An unknown clip returns `False`, preserves current playback, and sets `CHARACTER_ERROR_CLIP_NOT_FOUND`.

## Draw 3D, then 2D

```smile
Dim Camera As Core.Camera3D
Dim FrameReady As Boolean

Camera = Graphics3D.DefaultCamera()
FrameReady = Scene3D.UseLighting("CharacterStudio")
FrameReady = Scene3D.Begin(Camera, 4, 8, 20)

If FrameReady Then
    FrameReady = Character3D.Draw(Hero)
    FrameReady = Scene3D.EndScene()
End If

Draw Text "HP 100" At 20, 20 Size 20 Color WHITE
Show Screen
```

`Scene3D.Begin` rejects nesting and reapplies the selected preset unless `UseCustomLighting` was chosen. `EndScene` rejects an unmatched end and restores ordinary Renderer2D composition.

## Events, sockets, and root motion

Animation events are notifications, not gameplay damage:

```smile
If Character3D.TakeEvent(Hero, "Impact") Then
    Print "The animation reached its impact frame."
End If
```

Use a socket to place a weapon or marker:

```smile
Dim Hand As Core.Vector3

Hand = Character3D.SocketPosition(Hero, "HandTip")
```

Root motion is ignored by default. Apply mode retains subunit motion in thousandths, rotates model-local translation by the actor's starting yaw, applies translation, then applies root yaw to every actor part:

```smile
Updated = Character3D.SetRootMotion(Hero, Character3D.ROOT_MOTION_APPLY)
Updated = Character3D.PlayAnimation(Hero, "RootMove", True)
```

## Quality and fallback

Low, Medium, High, and Auto only control capabilities that exist in M4: PBR preparation, texture filtering/anisotropy, local-light count, and permission for capability-only simple fallback. Changing quality affects new/reloaded actors; it never silently mutates a live asset.

`LoadActor` may use one neutral simple material only when PBR capability is unavailable and current policy permits it. A malformed model, absent animation, or a missing declared texture when PBR validation is available remains a visible load failure. Inspect both layers:

```smile
Print Character3D.LastError()
Print Character3D.LastRendererError()
Print Character3D.LastFallback()
```

## Cleanup and advanced interop

Call `Character3D.Destroy` for individual actors or `Character3D.Shutdown` at scene exit. `Shutdown` is idempotent. A direct `Graphics3D.ResetRenderer3D` advances the renderer epoch and invalidates every high-level actor/cache entry; the next Character3D operation reports `CHARACTER_ERROR_RENDERER_RESET` instead of double-destroying stale resources. Same-epoch tampering quarantines only the affected actor or shared asset and preserves unrelated actors.

Battle systems can read `PrimaryObjectHandle`, indexed `PartObjectHandle`, `AnimatorHandle`, and `ModelHandle`. These are borrowed read-only values: do not destroy them or mutate Character3D-owned transforms. Character3D does not import Battle3D and never applies damage, VFX, shadows, HDR, bloom, or game-specific policy.

See `examples\Character3DLab` for the native/Web sample and `scripts\test-character3d.ps1` for deterministic parity and lifecycle coverage.
