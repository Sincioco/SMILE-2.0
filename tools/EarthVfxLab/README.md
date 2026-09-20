# SMILE 2.0 Earth Lab (native)

The native arena uses the [shared Viewer arena library](../../docs/libraries/arena3d.md).
F toggles Floor; G toggles Grid; B cycles the same five Viewer backgrounds.
Left drag pans, middle drag orbits, wheel zooms smoothly, and right-click resets
the camera. Grid color, spacing and dimensions remain library creation options.

Run `Build.ps1`, then `Launch.ps1` with PowerShell 7. The launcher closes an older
Lab gracefully and preserves the normal runtime window placement. Keep it to the
right of Codex so the chat stays visible.

Kael demonstrates **Boulder Hurl**, **Stone Volley**, **Fault Line** and **Quiet Idle**.
He plants a raised foot, drops into a wide stance, lifts the earth and drives the
cast with his torso and both arms. Volley alternates three thrusts; Fault Line
finishes in a deep downward strike. His sword is hidden during Earth casts and
returns for Idle. The default speed is 200. His normal sword attacks remain in
the Character Viewer and Sin Star I.

The Lab uses Water Lab's Sin Star background, a reflective arena, random pillar
targets and a slow two-minute camera orbit. Hurl and Volley fade all 60 loose
ground stones between 24% and 32% of the cast, leaving none after lift-off.
New impact fragments still appear at the target. Lighter, shorter-lived lift dust
keeps Kael and the floating rocks readable. Each impact releases a bounded puff
that expands and fades over 1–1.8 seconds of playback time, including the held
final pose and the next animation. At the default 200 speed, this takes 0.5–0.9
seconds on screen; Pause freezes the remaining dust.

| Control | Action |
| --- | --- |
| Top buttons / Tab | Select cast or Quiet Idle |
| Space / Pause | Pause character, effect and cues |
| R / Restart | Restart and choose a pillar |
| O / Orbit | Toggle slow cinematic orbit |
| Left drag / middle drag | Pan / orbit |
| Wheel | Smooth bounded zoom |
| Right click / Enter | Reset camera |
| + / - | Speed, 25–400% |
| B | Scene, logo, black, green and purple backgrounds |
| Sound | Toggle original stone sound cues |

## Owners and resources

`Program.smile` only wires startup, input, update, draw and shutdown.
`EarthLabScene` owns the actor, targets, clock, camera and arena; `EarthLabUi`
owns controls/readouts. The shared `KaelEarth` adapter owns clip/effect timing,
sword visibility and audio cues. `Smile.Simple3D.EarthVfx3D` owns a caller-supplied
effect context: 63 reusable rock instances and a 4,096-slot GPU dust pool. It
accepts explicit cast time, elapsed playback time, scale and target; it has no
character or Lab dependency. Resources preload during startup. Pausing does not
emit particles; a paused backward seek clears old dust. Normal cast loops and
clip changes retain the fading tail. Hidden instances use a positive transform scale, avoiding the
native zero-scale rejection found during this work.

Three original rock meshes have 5,120 triangles each, smooth normals, weathered
fracture relief and 2K mineral/normal textures. Instances share their mesh/material
data. Rendering uses the existing native PBR, GPU particles and planar reflections;
there is no new physics engine, runtime/compiler extension or RTX-only feature.

Run `Test.ps1` after building. Its focused native fixture checks every mode,
actual hand/head socket movement, quiet Idle, complete ground-rock clearing,
dust aging after the final pose and into Idle, pause/seek, speed, reflection
readiness and resource cleanup. The shared Viewer
hardening and Sin Star I presentation checks cover integration, including all
three Earth casts in Kael Party. Web and Studio remain on hold.

## References and reproduction

- [Jared Koh — Avatar Earthbending Animation](https://www.youtube.com/watch?v=lZRYNRqdWD4):
  pose/choreography reference for planted stances, weight shifts and forceful strikes.
- [The VFX Wizard — Earthbending #01](https://www.youtube.com/watch?v=E323JquhIv4),
  13–20 seconds: ground lift, suspended stone and flying fragments.
- [Vivian — Earth Bending Sound Design Practice](https://www.youtube.com/watch?v=3JZXQ-8dvOM),
  1–16 seconds: user-supplied sound/animation reference. The shipped sounds are
  original synthesized PCM cues; no reference audio or footage is packaged.

The canonical Kael package owns animation authoring, original sources, floor checks
and export. `TechnicalAssets/Generation3/Earth` owns rock geometry, textures and
audio. Normal builds use only local assets and existing SMILE libraries.
