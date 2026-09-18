# SMILE 2.0 â€” Water Lab (native)

## Kael water preview â€” September 18, 2026

The native Lab starts with **Kael**, speed **200%**, **Demo** and **Realistic Water**
enabled. **Mira / Kael** buttons or **X** switch between the preloaded actors.
Mira keeps her eight existing previews. Kael has **Water Whip**, **Serpent Orbit**
and **Tidal Surge**, with original baked arm sweeps, torso turns, lowered stances
and recoveries. His sword stays hidden in this water preview.

**D / Demo** cycles all three Kael casts. Selecting a cast or pressing Tab turns
Demo off and repeats that cast. **F / Realistic Water** compares the clear standard
look with thicker irregular water, sharper reflections, lower foam and stronger
refraction. Switching looks preserves the paused pose and clock and allocates no
new resources. This approximates the reference's appearance with authored real-time
geometry; it is not an offline fluid simulation.

Water winds around Kael, releases at 42% and contacts the selected pillar at 72%.
The impact splits around both sides of the pillar, meets behind it, then drains
and breaks apart. Callers supply cylindrical target bounds: radius up to 40 and
height up to 140 world units can wrap; missing or larger bounds keep the normal
crown splash. The four pillars have width × height 24×60, 48×95, 72×130 and 96×165.
Kael visits them in order across casts; the first three wrap and the largest uses
the crown splash. The Lab supplies the selected pillar's actual radius, height and
recoiling center, and aims at its surface. The footer identifies the target and size.
This is an explicit target approximation, not automatic mesh collision.

Space pauses; **Left / Right** step the paused presentation by 100 ms. The header
shows the clip and time. Camera and background controls remain available while paused.
Leave Demo running for continuous feedback, or use these controls to inspect contact.

References: [Jared Koh â€” body motion](https://www.youtube.com/watch?v=5Lq0vTq6eeo),
[Krifton â€” clear water winding around the body](https://www.youtube.com/watch?v=QjWQXMB5xNs),
and [A Bit Of Game Dev â€” attacks from 16:13](https://www.youtube.com/watch?v=3CcWus6d_B8&t=973s).
These are visual references; no video, animation, shader, package or service was downloaded.

The canonical Kael package owns a separate `kael-v1-water-preview.glb`, descriptor,
Blender checkpoints, original authoring script, measurements and pose previews.
The accepted thirteen-clip Viewer/game model remains unchanged during this Lab review.
Normal Lab builds stage and cook the local preview; Blender is needed only to re-author it.

### Ownership and focused validation

`WaterLabScene` owns both actors and selection/clock/target/demo state; `WaterLabUi`
owns controls. `WaterFlow3D` owns stateless surrounding-water paths and skin samples.
`WaterImpact3D` owns small-target eligibility and paired wrap sheets. `WaterVfx3D`
accepts those frame parameters and retains its existing two ribbons and 4,096-slot
spray allocation. No runtime, compiler, startup-loop or dependency extension is needed.
The offline IK baker now accepts pose samplers and output names, allowing Water to
reuse the grounded Earth workflow without replacing its assets.

Native tests cover Mira's eight modes, Kael's three cooked body motions, target
clearance/contact, paused toggle/clock preservation, automatic demo progression,
constant resource counts across character switches, and complete cleanup. Export
checks every frame of all sixteen clips; a GLB round trip checks clip starts and
final Defend/Hit/Death plus mid/end Water poses. Native visual inspection accompanies
these checks. Formatter integration and the repository style gate also apply.

Growth for this slice: scene +107 lines, UI +88, shared flow +97, shared impact +60,
shared water renderer +50; the focused native fixture grows by 189 lines. These
changes stay within their existing responsibilities. No guardrail, baseline,
exclusion or resource ceiling changes. Studio and all Web adoption remain on hold.

Run `pwsh tools/WaterVfxLab/Launch.ps1 -Build` from the repository. The Lab uses the
installed Windows compiler, D3D11 renderer and repository assets; no downloads or
external dependencies are required. The official one-second startup presentation
and remembered native window placement come from the normal runtime.

Mira’s eight previews: Healing Veil, Tide Barrier, Torrent, Impact, Waterball, Tsunami,
Tempest Combo and Waterbending. Waterbending is selected when switching to Mira. It raises a
ground-fed column into a thick, curling, rounded head, with moving surface folds
and light falling spray. Each cast chooses one of the four enemy pillars at random
and keeps that target through pause and playback. After the rise and curl, the head
extends to that pillar at 72% of the cast, with a splash and brief recoil. Continuous
waterbending droplet emission is 75% lower; the main surface and rounded skin remain.
Its contact now opens a compact, curled crown of clear water sheets, breaks into
an irregular rim, and fades. All eight previews now share this clear-water finish,
low foam, smoother surfaces and sparse runoff. Every attack contact and the Impact
preview use the same compact crown and bounded 128-droplet impulse. There is no
sustained impact shower; each preset keeps its own travel, shape and timing.
Torrent winds a connected water whip around the staff, releases
it toward the target, then splashes and recoils the target at 72% of the cast.
The waterball forms at Mira's staff, stretches along its travel,
continuously deforms, and spreads into a surface splash at contact. The target
recoils nine world units and returns. Tide Barrier places a small translucent disc
in front of each of the four recipients, facing the attacker. It leaves the party
visible and does not enclose the formation. The preview receives an impact halfway
through its cycle. The struck shield compresses backward toward its recipient,
while the same compact crown opens at the struck disc and droplets spread outward.
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
Press **B** or the background button to cycle Scene â†’ Logo â†’ Black â†’ Green â†’ Purple
â†’ Scene, matching the Character Viewer. Both images preload at startup; switching
does not reload textures, reset the cast, move the camera or unpause playback.
The arena uses the shared planar reflection pass, including Mira, targets, water
and the background, with the existing 85% strength and 5% softness arena defaults.
The camera begins a slow two-minute orbit. Use Orbit or O to toggle it, middle drag
to orbit manually, left drag to pan, wheel to zoom and right-click/Enter to reset.
Space pauses the presentation; R restarts; Tab cycles effects. Buttons also control
pause, restart and sound. Manual pan/orbit stops the automatic orbit. Water and
lightning stop advancing while paused; effects rebuild from the selected cast time.

Animation speed defaults to **200%**. Press `+` (or `=`), `-`, numpad plus/minus,
or the visible speed buttons to change it in 25-point steps between 25% and 400%.
One scaled presentation clock drives Mira's pose, water motion,
lightning and cue timing. Camera interaction/orbit keeps real-time responsiveness;
audio clips retain their original pitch. Pause and effect changes preserve the speed.
Scaled time is consumed in steps of at most 100 milliseconds, respecting the shared
particle/lightning update limits. A slow frame at 400% therefore advances Mira,
droplets and lightning together instead of clipping only the effect clocks.

## Ownership and limits

`Program` only wires the loop. `WaterLabScene` owns preview actors, renderer setup,
lighting, clock and audio; `WaterLabUi` owns controls. The same `WaterVfx3D`,
`WaterStorm3D` modules are used by the Character Viewer.
`WaterVfx3D` owns two bounded ribbon batches (visible surface and refraction) and
one 4,096-slot GPU spray system. `WaterFlow3D` owns only the stateless waterbending
centerline, combat-whip path and closed skin samples, receiving origin, target, progress and time;
it depends on precision math, not on the water context, Lab or character code.
Waterbending, Torrent, Waterball, Tsunami and Tempest use 32 strips with 97 samples
each: 6,144 surface triangles. Healing retains two spirals per recipient with 97
samples each. Barriers use eight radial bands with 49 samples per recipient,
leaving room for sixteen 21-sample crown sectors on each struck shield. Even four
simultaneous barrier crowns fit the existing 3,168-point allocation per batch
(3,104 points used). Standalone/attack impacts use sixteen 97-sample crown sectors.
`WaterImpact3D` owns stateless contact
sheet geometry, breakup opacity and the compact radial spray impulse; it has no
Lab, character, clock or resource dependency. WaterVfx3D retains the cast identity
internally when transitioning to impact and owns the bounded emission count.
All contact modes share the same burst: 96 initial droplets and a 32-droplet tail,
ending before 26% of contact. Simultaneous shield hits share that whole-event budget
across recipients. Pausing cannot re-emit it; a new cast, hit, or backward seek
resets it. Ordinary runoff uses Waterbending's maximum 20 droplets per update.
The native GPU handles spray motion, gravity,
turbulence and drawing. Lightning borrows the scene's existing global initialization
and clock; the water adapter owns only its three strike handles. No resource ceilings
were raised. Mira no longer has a body aura, staff glow overlay, head shimmer or
Lab cast-following point light. Her authored model/materials remain intact. The
same glow removal in `MiraWater` covers the Viewer and Sin Star I shared sessions.

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
a physically simulated fluid. All modes use Waterbending's low foam setting;
barriers retain their subtle opacity so the party remains visible.

This is authored real-time VFX, not a fluid/collision simulation. Target contact is
provided by the caller; the Lab uses the front surface of its target cylinder.
Party target/reaction policy remains in the Viewer, and no Sin Star I gameplay or
health rules are changed. The package's original 2M body and staff remain preserved.

Procedural PNGs and original PCM splash/barrier/tsunami sounds are in
`TechnicalAssets/Generation3/Water`, reproducible with
`scripts/prepare-water-vfx-assets.ps1`. Build-local asset copies are disposable.

`Test.ps1` exercises all eight modes, rendering, backward seek, facing, contact
recoil and cleanup using the actual Mira asset. Native GPU shader mode 5 preserves
texture color; modes 0â€“4 retain their historical behavior. All Web adoption and
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

The shared-preset follow-up applies Waterbending's material, surface quality and
compact impact to all eight previews, without changing cast paths, actor assets,
combat timing or renderer code. `WaterVfx3D` remains the caller-owned lifecycle and
preset owner; `WaterImpact3D` supplies the existing stateless crown. No new module,
dependency, native runtime/VSIX change or resource ceiling is needed.
The extended native fixture checks each preset's surface density, sparse runoff,
single contact burst, paused contact, late emission cutoff, and one to four
simultaneous barrier crowns. An initial three/four-recipient burst exceeded the
GPU spawn limit (error 68); sharing one 128-droplet budget fixes that path and
keeps the accepted sparse appearance. The regression covers it directly.
Validation passes: Water Lab native fixture, seam-normal check, 13 formatter
regressions, repository style check (464 sources), Viewer native ownership/hardening
checks (58 graphics/input/audio checks), and all eight Sin Star I character entries
plus both battle simulations. WaterVfx3D grows 792â†’802 lines and its focused native
fixture 347â†’440; existing ownership and review limits remain unchanged.
Visual review of this follow-up remains unverified: the native computer-use capture
returned `foreground window did not report a process id` after refreshing the
window selection and launching a separate fixed-frame inspection. The native app
and automated rendering checks run successfully. Next acceptance step: inspect
all eight presets and their contact in the rebuilt Lab, especially Tide Barrier,
Waterball and Tsunami. No browser or Web acceptance is claimed.

Impact geometry reference: Ugur VFX Studios,
[UE | Mixed Magic "5" - Water Impact](https://www.youtube.com/watch?v=ZyA4GJgItOk),
particularly the opening burst and breakup around 0:01â€“0:02. Only its sheet spread,
curled crown and thinning edge are adapted. Sin requested a much smaller splash
around a single pillar using the already accepted clear-water shading. No video
assets, Unreal package or external dependency is imported.

The grid-pattern follow-up adds native smooth water normals across triangle and
ribbon seams, with batch-owned reusable scratch and unchanged resource ceilings.
`WaterSurfaceTests.cpp` checks welded seam normals and degenerate connectors.
The native fixture also checks the denser committed surface, single impact burst,
pause stability, compact sheet extent, fade, five-background wraparound and no
Mira shimmer allocation. Native VFX batches, reflections, 58 Viewer checks and all
ten Sin Star I presentations pass. All three native applications and the VSIX
were rebuilt; the installed VSIX matches its 35 checked payload hashes.

Follow-up growth review: WaterVfx3D 733â†’792, WaterLabScene 344â†’369,
WaterLabUi 149â†’170, WaterLabTests 273â†’347; MiraWater shrinks 324â†’311.
The new stateless WaterImpact3D is 128 lines. Native normal generation is a
74-line helper; the existing renderer coordinator grows 10,635â†’10,656 for
vertex layout, scratch lifetime and delegation. No dependency cycle, public
language syntax, size-limit exception or renderer-cap increase is introduced.

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
715â†’733, the native water shader 103â†’130, the scene 316â†’344, the UI 147â†’149,
the focused native fixture 132â†’273 and the build script 25â†’29. The library and Lab
project inventories each add one entry. Existing lifecycle owners remain unchanged;
no dependency cycle, renderer resource ceiling, size limit or legacy baseline was
introduced or raised.

The reflection correction adds 48 lines to its existing resource owner (435â†’483)
and two declarations to its header. The legacy native draw coordinator adds 24
lines for view constants and capture delegation (10,611â†’10,635); allocation,
failure caching and teardown stay in the reflection owner. No new source module,
dependency cycle or review-limit exception is introduced.
