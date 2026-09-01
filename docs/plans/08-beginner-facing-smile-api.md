# Beginner-Facing SMILE API

## Principle

The engine may be sophisticated. The student's source code should not be.

A beginner should work with:

- actors;
- animation names;
- positions;
- scene presets;
- effect names;
- simple quality choices.

A beginner should not work with:

- bone matrices;
- tangent frames;
- shader resource views;
- framebuffers;
- uniform buffers;
- texture color spaces;
- shadow bias;
- animation sample arrays;
- GPU instance buffers;
- WebGL extensions.

## Package direction

Prefer adding small modules to the existing reusable `Smile.Simple3D` package after repository reconciliation:

```text
Smile.Simple3D.Character3D
Smile.Simple3D.Scene3D
Smile.Simple3D.Effects3D
```

Keep:

```text
Smile.Simple3D.Graphics3D
```

as the lower-level teaching and advanced-control facade.

Do not create new language syntax.

## Proposed value types

Exact fields may change to preserve source compatibility.

### `Character3D.Actor`

Suggested mirrored public state:

```basic
Public Type Actor
    Handle As Number
    Position As Core.Vector3
    Rotation As Core.Rotation3D
    ScalePercent As Number
    Visible As Boolean
End Type
```

Internally, the handle refers to:

- shared character asset;
- object parts;
- animator;
- active clip/blend;
- attachments;
- root-motion mode.

### `Scene3D.Profile`

Could be constants rather than a record:

```basic
Public Const QUALITY_AUTO = 0
Public Const QUALITY_LOW = 1
Public Const QUALITY_MEDIUM = 2
Public Const QUALITY_HIGH = 3
```

### `Effects3D.Effect`

A simple handle may expose:

- active;
- age;
- duration;
- owning preset;
- attachment actor/socket.

## Minimum Character3D API

Recommended beginner calls:

```text
Load(Path)
Destroy(ByRef Actor)
Place(ByRef Actor, X, Y, Z)
Rotate(ByRef Actor, X, Y, Z)
SetScale(ByRef Actor, Percent)
SetVisible(ByRef Actor, Visible)
Play(ByRef Actor, ClipName, Looping)
CrossFade(ByRef Actor, ClipName, DurationMilliseconds)
Stop(ByRef Actor)
Update(ByRef Actor, ElapsedMilliseconds)
Draw(Actor)
IsPlaying(Actor, ClipName)
AnimationComplete(Actor)
TakeEvent(Actor, EventName)
HasSocket(Actor, SocketName)
SocketPosition(Actor, SocketName)
LookAt(ByRef Actor, X, Y, Z)
SetRootMotion(ByRef Actor, Mode)
AttachObject(ByRef Actor, SocketName, Object)
DetachObject(ByRef Actor, Object)
```

A cached asset/instance API may also exist for advanced users, but the one-call `Load` path should be available.

## Minimum Scene3D API

Recommended calls:

```text
SetQuality(Profile)
EffectiveQuality()
UseLighting(PresetName)
SetExposure(Percent)
SetBloom(Enabled, IntensityPercent)
SetShadows(Enabled)
Begin(Camera, ClearRed, ClearGreen, ClearBlue)
End()
LastFallback()
```

Named built-in presets should include a small set such as:

- `Daylight`;
- `Dungeon`;
- `Moonlight`;
- `EmberObservatory`;
- `CharacterStudio`.

Presets are data/configuration inside the module, not native game-specific helpers.

## Minimum Effects3D API

Recommended calls:

```text
Initialize(Profile)
Shutdown()
DefinePreset(...)
PlayAt(PresetName, X, Y, Z)
PlayOn(PresetName, Actor, SocketName)
Stop(ByRef Effect)
Update(ElapsedMilliseconds)
Draw(Camera)
Active(Effect)
FlashOpacity()
RequestedShake()
LastError()
```

The first release may expose a more structured preset builder if one long `DefinePreset` signature is unreadable.

## Beginner example

```basic
Option Explicit

Import Smile.Simple3D.Core As Core
Import Smile.Simple3D.Graphics3D As Graphics3D
Import Smile.Simple3D.Character3D As Character3D
Import Smile.Simple3D.Scene3D As Scene3D
Import Smile.Simple3D.Effects3D As Effects3D

Dim Camera As Core.Camera3D
Dim Hero As Character3D.Actor
Dim Running As Boolean
Dim PreviousTime As Number
Dim CurrentTime As Number
Dim Elapsed As Number

Game Window "Character3D Example" Size 960 By 540

Camera = Graphics3D.DefaultCamera()
Hero = Character3D.Load("Assets\Models\Paladin.sm3d")

Call Character3D.Place(Hero, 0, 0, 0)
Call Character3D.Play(Hero, "Idle", True)

Call Scene3D.SetQuality(Scene3D.QUALITY_AUTO)
Call Scene3D.UseLighting("CharacterStudio")

Call Effects3D.Initialize(Scene3D.EffectiveQuality())

Running = Hero.Handle <> 0
PreviousTime = Timer()

Do

    CurrentTime = Timer()
    Elapsed = Max(0, Min(100, CurrentTime - PreviousTime))
    PreviousTime = CurrentTime

    If Key_Pressed(KEY_SPACE) Then
        Call Character3D.CrossFade(Hero, "SwordAttack", 160)
    End If

    Call Character3D.Update(Hero, Elapsed)

    If Character3D.TakeEvent(Hero, "SwordImpact") Then
        Call Effects3D.PlayOn("HolySwordImpact", Hero, "SwordTip")
    End If

    Call Effects3D.Update(Elapsed)

    Call Scene3D.Begin(Camera, 8, 10, 18)
    Call Character3D.Draw(Hero)
    Call Effects3D.Draw(Camera)
    Call Scene3D.End()

    Draw Text "SPACE: ATTACK" At 24, 500 Size 18 Color WHITE

    Show Screen
    Running = Running And Not Game_Closed()

Loop Until Not Running

Call Effects3D.Shutdown()
Call Character3D.Destroy(Hero)
```

Codex must adapt calls such as `Key_Pressed` to actual current language APIs; this example defines the intended complexity level, not new grammar.

## Advanced escape hatch

Advanced users retain:

- mesh creation;
- object transforms;
- material creation;
- skeleton/clip creation;
- direct animator control;
- light configuration;
- batch control.

High-level modules should use the same public/general bridges rather than private Dragonfall-only runtime entry points.

## Error design

Prefer errors understandable to a student.

Examples:

```text
Character3D could not load Assets\Models\Arin.sm3d.
The file uses 132 bones; this renderer supports at most 128.
```

```text
Animation "SwordAttack" was not found.
Available clips: Idle, Walk, Attack, Hit, Victory.
```

```text
Effect "HolySwordImpact" could not start because the effect pool is full.
The low-quality fallback was used.
```

Expose both:

- a small numeric `LastError` for programs;
- useful build/load diagnostics for humans.

## API stability rules

- Keep current numeric constant values.
- Append new command values rather than renumbering old ones.
- Do not change current zero-handle failure behavior.
- Maintain explicit shutdown/reset.
- Do not make one actor own a shared asset in a way that destroys other actors.
- Do not hide resource leaks behind garbage collection.
- Use names for clips/sockets/effects at the high level, handles at the low level.
- Keep integer/percentage public conventions.
- Document all bounded maxima.

## Teaching documentation

M4 must add:

- a short Character3D tutorial;
- a short material/lighting tutorial;
- a short animation-events tutorial;
- a short Effects3D tutorial;
- one complete native/Web Character Lab;
- clear explanation of high-level versus low-level APIs.

The tutorial should begin with an actor already prepared by an artist. It should not begin with bones, matrices, or shader theory.
