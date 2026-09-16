# SMILE 2.0 — Water Lab (native)

Run `pwsh tools/WaterVfxLab/Launch.ps1 -Build` from the repository. The Lab uses the
installed Windows compiler, D3D11 renderer and repository assets; no downloads or
external dependencies are required. The official one-second startup presentation
and remembered native window placement come from the normal runtime.

Eight previews: Healing Veil, Tide Barrier, Torrent, Impact, Waterball, Tsunami,
Tempest Combo and Waterbending. Waterbending is the startup selection. It raises a
ground-fed column into a thick, curling, rounded head, with moving surface folds
and light falling spray. Each cast chooses one of the four enemy pillars at random
and keeps that target through pause and playback. After the rise and curl, the head
extends to that pillar at 72% of the cast, with a splash and brief recoil. Continuous
waterbending droplet emission is 75% lower; the main surface and rounded skin remain.
Torrent winds a connected water whip around the staff, releases
it toward the target, then splashes and recoils the target at 72% of the cast.
The waterball forms at Mira's staff, stretches along its travel,
continuously deforms, and spreads into a surface splash at contact. The target
recoils nine world units and returns. Tide Barrier places a small translucent disc
in front of each of the four recipients, facing the attacker. It leaves the party
visible and does not enclose the formation. The preview receives an impact halfway
through its cycle. The struck shield compresses backward toward its recipient,
while impact droplets fan across the surface and curl outward around the rim.
The shield recovers over 900 milliseconds. Other
shields stay in place. The storm preview shows
a conductor strike followed by overhead lightning into the wave and target.

The four target pillars form an evenly spaced enemy row across from Mira, all
in front of her facing direction. Waterbending attacks and recoil follow its chosen
pillar; the other attack previews continue to use the far pillar. Meanwhile,
the healing/barrier preview positions follow Mira and the first three pillars.

The screen-fixed background matches the Character Viewer's default landscape,
using `games/SinStarI/Assets/Sin Star - Title Screen - Background.png` through
the shared `StaticBackdrop3D` owner. Build stages an ignored `SinStarLandscape.png`
copy; the original artwork is unchanged. Water refraction samples the background.
The arena uses the shared planar reflection pass, including Mira, targets, water
and the background, with the existing 85% strength and 5% softness arena defaults.
The camera begins a slow two-minute orbit. Use Orbit or O to toggle it, middle drag
to orbit manually, left drag to pan, wheel to zoom and right-click/Enter to reset.
Space pauses the presentation; R restarts; Tab cycles effects. Buttons also control
pause, restart and sound. Manual pan/orbit stops the automatic orbit. Water and
lightning stop advancing while paused; effects rebuild from the selected cast time.

Animation speed defaults to **200%**. Press `+` (or `=`), `-`, numpad plus/minus,
or the visible speed buttons to change it in 25-point steps between 25% and 400%.
One scaled presentation clock drives Mira's pose, water motion, staff shimmer,
lightning and cue timing. Camera interaction/orbit keeps real-time responsiveness;
audio clips retain their original pitch. Pause and effect changes preserve the speed.
Scaled time is consumed in steps of at most 100 milliseconds, respecting the shared
particle/lightning update limits. A slow frame at 400% therefore advances Mira,
droplets and lightning together instead of clipping only the effect clocks.

## Ownership and limits

`Program` only wires the loop. `WaterLabScene` owns preview actors, renderer setup,
lighting, clock and audio; `WaterLabUi` owns controls. The same `WaterVfx3D`,
`WaterStorm3D` and `CharacterGlow3D` modules are used by the Character Viewer.
`WaterVfx3D` owns two bounded ribbon batches (visible surface and refraction) and
one 4,096-slot GPU spray system. `WaterFlow3D` owns only the stateless waterbending
centerline, combat-whip path and closed skin samples, receiving origin, target, progress and time;
it depends on precision math, not on the water context, Lab or character code.
Sixteen strips provide the animated shape and internal flow without a fluid solver.
The native GPU handles spray motion, gravity,
turbulence and drawing. Lightning borrows the scene's existing global initialization
and clock; the water adapter owns only its three strike handles. No resource ceilings
were raised. Glow overlays borrow Mira's model and animator and die before the actor.
The staff-head shimmer uses 128 instanced sprites and the shared droplet texture.
Its positions are rebuilt in the StaffGrip/StaffTip frame so it stays around the
head during every pose and back carry; it does not leave particles down the shaft.
The glow context releases that borrowed texture's material before water cleanup.

The native lit-water material adds Fresnel reflection, directional specular lighting,
animated surface normals and depth-dependent absorption/refraction. Multiscale
world-space folds cross strip boundaries; bounded absorption keeps the interior
nearly clear, while a brighter sky fallback gives the surface silver reflections.
It borrows the
existing opaque scene and depth for a bounded 20-step screen-space reflection search;
offscreen rays use a simple sky/horizon fallback. The arena's water reflection
uses the mirrored camera and a color copy of the reflected opaque scene, replacing
the former fixed blue-green transmission fallback. That copy is owned and bounded
by the shared reflection renderer; it is reused each frame and released on reset,
resize or device loss. Main-view water reuses the existing scene target. There is
no GPU readback. These are real-time approximations, not ray tracing or
a physically simulated fluid. Quiet barriers use very little foam.

This is authored real-time VFX, not a fluid/collision simulation. Target contact is
provided by the caller; the Lab uses the front surface of its target cylinder.
Party target/reaction policy remains in the Viewer, and no Sin Star I gameplay or
health rules are changed. The package's original 2M body and staff remain preserved.

Procedural PNGs and original PCM splash/barrier/tsunami sounds are in
`TechnicalAssets/Generation3/Water`, reproducible with
`scripts/prepare-water-vfx-assets.ps1`. Build-local asset copies are disposable.

`Test.ps1` exercises all eight modes, rendering, backward seek, facing, contact
recoil and cleanup using the actual Mira asset. Native GPU shader mode 5 preserves
texture color; modes 0–4 retain their historical behavior. All Web adoption and
publication are on hold at Sin's direction.

The speed regression first reproduced two failures with a 100 ms frame at 400%:
lightning advanced only 100 ms and water treated the 400 ms gap as a seek. It now
passes with 400 ms of lightning age and live water spray, alongside all seven modes.

On September 13, 2026, Sin confirmed that manual controls, including orbit, panning
and zooming, work as expected. This completes the native camera acceptance check.

## Waterbending reference (September 16, 2026)

Visual reference: CG Visuals # VFX,
[Trapcode Elements V1.3: Improved Waterbending](https://www.youtube.com/watch?v=-hdFJrBEZy0).
The inspected frames around 0:03, 0:19, 0:27 and 0:31 show a rounded moving water
head, a connected curved column, clear interiors, broken silver highlights and
downward runoff. The 0:27 gray studio shot is the closest comparison to this Lab.
The video was used as visual reference only; no footage, textures or plugin assets
were imported. Sin selected the Character Viewer's default landscape for the Lab.
The Lab's existing 200% speed remains available; use 100% to inspect
the slower rise and curl. The existing camera controls are unchanged.

The native approximation uses the existing two ribbon batches and 4,096 spray
slots. It does not implement fluid collisions, fluid volume conservation, true
thickness, or photographic environment reflections. Waterbending is a Lab preview;
the existing seven cast identities are unchanged. Waterbending and Torrent use the
same 72% visual contact boundary as Waterball; no battle scheduler or health rules change. Recompilation
adopts the shared shader improvements in other native programs. Web adoption is
still on hold.

Combat motion reference: the first 20 seconds of A Bit Of Game Dev's
[How To Create Water Effects in Unity - Water Bending Tutorial](https://www.youtube.com/watch?v=3CcWus6d_B8).
The opening shows waterball throws, a water whip winding around the caster, then
forward release and breakup. Torrent adopts that wind-up/release/contact progression
through `WaterFlow3D.AttackCenter`; it uses the existing 72% contact boundary and
caller-provided target. No Unity package or C# effect implementation is imported.

## Native validation (September 16, 2026)

The normal Debug/Release build, 13 formatter regression checks, repository style
check, native GPU particle checks and Character Viewer native hardening checks
passed. The latter includes the ownership checks and 58 native graphics, input and
audio checks. The final Water Lab fixture reports zero failures across all eight
modes, including closed skin/contact geometry, reflection draws/composition, spray,
seek, pause/speed and resource cleanup. The installed VSIX payload verification
matched all 35 checked files.

The reflection-color regression reproduced nine missing-snapshot failures before
the native renderer fix and passes afterward: all eight effect modes in HDR and
one LDR transition, followed by zero reflection bytes after reset. Native reflection
normal/failure/retry checks also pass. Visual comparison confirms clear/silver water
in both the main and arena views; highlight brightness still depends on view angle.
The runtime and native Lab were rebuilt and the refreshed VSIX was installed.

The rebuilt Lab was launched and visually checked for the curved water body,
falling spray, landscape and arena reflection. Its published landscape has the same
SHA-256 as the Character Viewer's default asset. This is a visual approximation of
the reference, subject to the rendering limits above; no Web validation is claimed.

The targeting follow-up passed the native fixture for all four pillar positions,
exact water-head contact, selected-pillar recoil, target stability during a cast,
and the reduced continuous spray count. The 13 formatter regressions, repository
style check and native Viewer ownership/hardening checks (including all 58 native
graphics/input/audio checks) also passed. Native visual inspection confirmed the sparse runoff,
rounded column, enemy row and impact splash. No browser refresh or .NET rebuild is
needed for these source-library refinements; launch the rebuilt native Water Lab.

`WaterLabScene` owns the randomly selected `TargetIndex`, choosing it only when a
cast restarts. It restores all pillars before applying the selected one's recoil.
`WaterFlow3D` owns only the smooth geometric extension to caller-provided contact;
`WaterVfx3D` owns the lower droplet rate and existing impact transition. No random
selection, enemy identity or new shared clock enters the reusable VFX modules.

Growth review: the new stateless `WaterFlow3D` has 192 lines. `WaterVfx3D` grows
715→733, the native water shader 103→130, the scene 316→344, the UI 147→149,
the focused native fixture 132→273 and the build script 25→29. The library and Lab
project inventories each add one entry. Existing lifecycle owners remain unchanged;
no dependency cycle, renderer resource ceiling, size limit or legacy baseline was
introduced or raised.

The reflection correction adds 48 lines to its existing resource owner (435→483)
and two declarations to its header. The legacy native draw coordinator adds 24
lines for view constants and capture delegation (10,611→10,635); allocation,
failure caching and teardown stay in the reflection owner. No new source module,
dependency cycle or review-limit exception is introduced.
