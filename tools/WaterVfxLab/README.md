# SMILE 2.0 — Water Lab (native)

Run `pwsh tools/WaterVfxLab/Launch.ps1 -Build` from the repository. The Lab uses the
installed Windows compiler, D3D11 renderer and repository assets; no downloads or
external dependencies are required. The official one-second startup presentation
and remembered native window placement come from the normal runtime.

Seven previews: Healing Veil, Tide Barrier, Torrent, Impact, Waterball, Tsunami and
Tempest Combo. The waterball forms at Mira's staff, stretches along its travel,
continuously deforms, and spreads into a surface splash at contact. The target
recoils nine world units and returns. Tide Barrier places a small translucent disc
in front of each of the four recipients, facing the attacker. It leaves the party
visible and does not enclose the formation. The preview receives an impact halfway
through its cycle. The struck shield compresses backward toward its recipient,
while impact droplets fan across the surface and curl outward around the rim.
The shield recovers over 900 milliseconds. Other
shields stay in place. The storm preview shows
a conductor strike followed by overhead lightning into the wave and target.

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
one 4,096-slot GPU spray system. Sixteen strips provide the animated shape and
internal flow without a fluid solver. The native GPU handles spray motion, gravity,
turbulence and drawing. Lightning borrows the scene's existing global initialization
and clock; the water adapter owns only its three strike handles. No resource ceilings
were raised. Glow overlays borrow Mira's model and animator and die before the actor.
The staff-head shimmer uses 128 instanced sprites and the shared droplet texture.
Its positions are rebuilt in the StaffGrip/StaffTip frame so it stays around the
head during every pose and back carry; it does not leave particles down the shaft.
The glow context releases that borrowed texture's material before water cleanup.

The native lit-water material adds Fresnel reflection, directional specular lighting,
animated surface normals and depth-dependent absorption/refraction. It borrows the
existing opaque scene and depth for a bounded 20-step screen-space reflection search;
offscreen rays use a simple sky/horizon fallback. It allocates no new scene targets
and performs no GPU readback. These are real-time approximations, not ray tracing or
a physically simulated fluid. Quiet barriers use very little foam.

This is authored real-time VFX, not a fluid/collision simulation. Target contact is
provided by the caller; the Lab uses the front surface of its target cylinder.
Party target/reaction policy remains in the Viewer, and no Sin Star I gameplay or
health rules are changed. The package's original 2M body and staff remain preserved.

Procedural PNGs and original PCM splash/barrier/tsunami sounds are in
`TechnicalAssets/Generation3/Water`, reproducible with
`scripts/prepare-water-vfx-assets.ps1`. Build-local asset copies are disposable.

`Test.ps1` exercises all seven modes, rendering, backward seek, facing, contact
recoil and cleanup using the actual Mira asset. Native GPU shader mode 5 preserves
texture color; modes 0–4 retain their historical behavior. All Web adoption and
publication are on hold at Sin's direction.

The speed regression first reproduced two failures with a 100 ms frame at 400%:
lightning advanced only 100 ms and water treated the 400 ms gap as a seek. It now
passes with 400 ms of lightning age and live water spray, alongside all seven modes.
