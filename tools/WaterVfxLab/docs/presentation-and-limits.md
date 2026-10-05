# Water Lab presentation and limits

Return to the [Water Lab entry point](../README.md) for native build/launch and
controls. The [September archive](history-2026-09.md) preserves all prior reports,
references, numerical growth reviews and one-time acceptance evidence.

## Presentation and target policy

[WaterLabScene](../WaterLabScene.smile) preloads Mira and Kael. Kael's WaterWhip,
WaterOrbit and WaterSurge clips use baked arm sweeps, torso turns, lowered stances
and recoveries; the sword is hidden. The canonical
[Kael package](../../../games/SinStarI/SourceAssets/Characters/Kael/KaelV1/README.md)
owns `kael-v1-water-preview.glb`, descriptor, Blender checkpoints, authoring script,
measurements and pose previews. Its native adoption is shared with Studio and
Sin Star I; normal Lab builds stage/cook the local preview without re-authoring it.
Preserve original model data and package ownership.

Kael releases at 42% of a cast and contacts at 72%. The caller supplies the target's
actual radius, height and recoiling center and aims at its surface. Four cylinders
have width × height **24×60, 48×95, 72×130, 96×165**; Kael visits them in order.
[WaterImpact3D](../../../libraries/Smile.Simple3D/WaterImpact3D.smile) permits wrap
only with radius greater than zero and at most 40, and height greater than zero
and at most 140. Thus the first three wrap and the largest uses the normal crown.
Wrapping separates around both sides, meets behind the target, then drains and
breaks apart. This is an explicit cylinder approximation, not mesh collision.

Mira retains eight previews:

| Preview | Presentation |
| --- | --- |
| Healing Veil | Two spirals per recipient; recipient positions follow Mira and the first three pillars |
| Tide Barrier | Small attacker-facing translucent discs for four recipients; no enclosing party dome |
| Torrent | Connected whip winds around the staff, releases, contacts at 72%, then splashes/recoils |
| Impact | Compact curled crown with a thinning irregular rim |
| Waterball | Staff formation, stretching/deforming travel, surface splash at contact |
| Tsunami | Wave travel with the shared compact contact finish |
| Tempest Combo | Conductor strike followed by overhead lightning into wave and target |
| Waterbending | Ground-fed column, thick curling rounded head, moving folds and sparse falling spray |

Waterbending chooses a random pillar only at cast restart and retains it through
pause/playback; other Mira attacks use the far pillar. Its head extends to contact
at 72%. Recoil moves the selected target nine world units and returns it; the
scene restores other pillars. Target choice stays in the Lab, outside shared VFX.
Tide Barrier receives a midpoint impact: only the struck disc compresses toward
its recipient, opens a crown and recovers over 900 ms. Other shields remain fixed.
All eight use clear water, low foam, smooth surfaces and sparse runoff while
retaining their own paths, shapes and timing.

## Clocks, controls and audio

`WaterLabScene` owns character/effect selection, target/demo state and the scaled
presentation clock. `WaterLabUi` delegates transitions. Speed is 25–400% in
25-point steps, default 200%; pause and effect changes retain speed. Scaled time
is consumed in steps of at most 100 ms, keeping pose, spray, lightning and cues
together at 400% after a slow frame. Camera interaction/orbit uses real time;
audio pitch stays unchanged. Paused effects rebuild from the selected cast time.

`SetPaused`, `SetAudioEnabled` and `Seek` stop only Lab channels **6/7/8** on mute,
pause, seek, restart, character/effect change or destruction. Seeking samples
silently. Resume/Sound On rebases cues without replaying old sounds; normal forward
playback plays each cast/contact once. Independent unrelated audio is unaffected.

The shared arena owns floor/grid and its Black, Green, Purple, Landscape, Title
palette. The UI calls the last two Scene/Logo. Both images preload, refraction
samples the active background, and switching neither allocates again nor resets
camera/cast/pause. Canonical artwork is preserved; build-local copies are disposable.

## Ownership and resource budgets

| Owner | Responsibility |
| --- | --- |
| [Program](../Program.smile) | Native input/update/draw loop only |
| [WaterLabScene](../WaterLabScene.smile) | Actors, renderer setup, lighting, presentation clock and audio |
| [WaterLabUi](../WaterLabUi.smile) | Visible and keyboard controls |
| [WaterVfx3D](../../../libraries/Smile.Simple3D/WaterVfx3D.smile) | Caller-owned effect lifecycle, presets, cast identity and bounded spray |
| [WaterFlow3D](../../../libraries/Smile.Simple3D/WaterFlow3D.smile) | Stateless centerline, combat-whip path and closed skin samples from origin/target/progress/time |
| [WaterImpact3D](../../../libraries/Smile.Simple3D/WaterImpact3D.smile) | Stateless contact sheets, breakup opacity, radial spray impulse and small-target wrapping |
| [WaterStorm3D](../../../libraries/Smile.Simple3D/WaterStorm3D.smile) | Water/lightning adapter; three owned strike handles, borrowing scene initialization/clock |

The same shared effect owners serve Studio/game callers. Shared flow/impact do
not own Lab character identity, random target selection, battle policy or a new
clock. Original Mira body/staff assets remain preserved. There is no Mira body
aura, staff glow overlay, head shimmer or Lab cast-following point light.

- Water owns **two ribbon batches** (surface and refraction) and **one 4,096-slot
  GPU spray system**. Each ribbon has 3,168 points (`32 × (97 + 2)`).
- Waterbending, Torrent, Waterball, Tsunami and Tempest use 32 strips × 97 samples,
  producing 6,144 surface triangles. Healing uses two 97-sample spirals per recipient.
- Barriers use eight 49-sample radial bands per recipient plus sixteen 21-sample
  crown sectors per struck shield. Four crowns use 3,104 points, within the same
  3,168-point batch. Standalone/attack crowns use sixteen 97-sample sectors.
- Contact emits one **128-droplet whole-event budget**: 96 initial plus a 32 tail,
  stopping before 26% contact. Simultaneous shield hits share it. Pause cannot
  re-emit it; new casts/hits/backward seeks reset it. Ordinary runoff admits at
  most 20 droplets per update. GPU owns motion, gravity, turbulence and drawing.

No renderer pool increase or resource-limit exception is implied. Materials,
effects and cleanup stay with their existing owners rather than reserving all
scene capacity for a single character.

## Materials and reflections

Native lit water provides Fresnel reflection, directional specular lighting,
animated multiscale normals and bounded depth-dependent absorption/refraction.
Smooth seam normals cross triangles/ribbons. Nearly clear interiors and a sky
fallback produce silver highlights whose brightness remains view-dependent.

A bounded 20-step screen-space reflection search borrows the opaque scene/depth;
offscreen rays use sky/horizon fallback. Arena water uses a mirrored camera and
an opaque reflected-color copy owned by the reflection renderer, reused each
frame and released on reset, resize or device loss. Main view reuses its scene
target. There is no GPU readback, ray tracing or fluid volume/thickness simulation.
The arena retains its 85% reflection strength and 5% softness defaults.

Water receives the existing directional sun shadow map with the opaque receiver's
tent filter. Ambient fill and environment reflection remain; direct water light,
foam and sun highlights are occluded. There is no extra target/public setting.
Spot-caster maps are excluded because this shader shades only the directional
light. Barriers retain subtle opacity so the party stays visible.

## Assets and validation

Procedural PNGs and original PCM splash/barrier/tsunami sounds live in
`TechnicalAssets/Generation3/Water`, reproducible with
[prepare-water-vfx-assets.ps1](../../../scripts/prepare-water-vfx-assets.ps1).
Visual-reference links and no-import constraints remain in the archive: no video
footage, textures, plugin package, Unity/Unreal implementation or reference-service
assets were imported. This Lab does not change health rules or a battle scheduler.

[Test.ps1](../Test.ps1) compiles native seam-normal and WARP shader-shadow checks
with Visual C++ tools, then runs the actual-asset SMILE fixture under a 30-second
bound. The expected marker is `Water Lab native failures: 0`. The fixture covers
Mira's eight modes, Kael's cooked motions, contacts/target sizes, seek/pause/speed,
backgrounds, resource ownership and cleanup. It requires staged asset inputs.
[Test-Audio.ps1](../Test-Audio.ps1) observes compiled scene Play/Stop statements
with real playback retained, covering transition/variant cases and channel
ownership; it is not a speaker recording or device-output check.

This reorganization ran no native visual, audio or browser acceptance. Preserve
the archive's unresolved all-eight-presets visual follow-up; historical passes
are limited to their described source/artifact and scenario. All Web adoption
and publication remain paused pending explicit direction.
