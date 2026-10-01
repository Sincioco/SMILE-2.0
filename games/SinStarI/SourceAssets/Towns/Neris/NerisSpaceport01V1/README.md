# Neris Spaceport 01 — southwest harbor

Current revision: **r09**, October 1, 2026. Original M00–M09 sources and earlier
builds remain preserved. Sin authorized the subsequent facade, crystal, entrance,
flicker and camera revisions without further approval stops.

- Standalone: `Source/NSP01-final-r09.blend`.
- Historical town assembly (current export appends the standalone source): `Town/Neris-Town-Spaceport-SW-r003.blend`.
- Terrain/roads/lighting: unchanged `Town/Neris-Town-Spaceport-SW-r001.town`.
- Complete portable geometry: `NSP01-preview.glb`.
- Current geometry and export checks: `Checkpoints/r09`.
- Historical renders and native screenshots: `Checkpoints/r08`; these are not r09 evidence.

The facade uses Royal Court limestone, gold trim, 99 source-derived window
assemblies, the castle's actual double entrance doors, a tall pointed ceremonial
screen, layered tower crowns, curved dock-arm braces and thirteen glowing tip crystals.
This remains a simplified architectural interpretation of the five concepts,
not a pixel-identical recreation. Opaque teal glazing is intentional in this export.

The user-marked curved strips on both sides and all eight hanging gold dock ties
were removed in r08 (sixteen objects total). All thirteen fixed views were rendered
again. Door/flicker/orbit evidence from the unchanged r07 owners remains in Checkpoints/r07.

## Scale and placement

Measured dimensions: **720 × 620 × 404 m**, including the 56 m underside. The raised
compass crown reaches Z=348 m. Components: 4 pads, 2 hangars, 13 primary towers,
9 keel pylons, 4 lift cabins, 24 separate hangar panels and 2 entrance door leaves.
Blender placement: (-550,-760,0.412), Z rotation 180°, scale 1. Native placement:
(-5500,25.12,-7600), Y rotation 180°, 10 world units per metre.

All 361 placements and 16,837 objects outside the former spaceport assembly are
preserved in r003, including terrain and review cameras. Southwest water, west
land, the 20 m road spine to Royal Court/town and 40 m causeway remain unchanged.
The live Town Editor document remains authoritative for later edits. Castle
sources remain unchanged. The earlier standalone sources and both castle packages remain unchanged.

## Native ownership and controls

NerisSpaceportPreview owns six models and 22 static draw objects. Entrance owns
six objects borrowing the already loaded Royal Court door models. Glow owns one
additive particle batch with thirteen anchors. Route owns placement, the bounded
central path, proximity opening/closing and a one-shot ConsumeEntry() event for a
cutscene owner. The leaves open to 95°, remain open around the party, then close
after it leaves. No cinematic content is authored by this package.

O/C and the bottom Orbit button start from the displayed camera, target, height,
lens and zoom. Orbit also stops the motion. Space pauses/resumes. Right-click
explicitly resets the overview; the header Orbit Spaceport command frames the
port. Earlier town documents without expanded coverage do not load the spaceport.

Portable geometry has 482,028 triangles and twelve materials. Native chunks retain
472,220 static triangles; 9,808 door triangles come from the existing Royal Court
models. Authored UVs and limestone texture are retained. The packer checks unchanged
model/part budgets and supplies orthonormal tangents. The revised pavement consumes one extra static part and material. No runtime,
compiler, addon or resource limit changed.

## Current r09 paving

Sixty fine grid-strip objects were removed from the approach and both hangar
aprons. Six gold approach markings are baked into a 2048 by 4096 texture on the
pavement, preserving the four lines and compass medallion. Native anisotropic
filtering and mipmaps filter their distant appearance without overlapping gold
geometry. The image is packed in the Blender source and embedded in the GLBs.
This reduces the identified shimmer; moving-camera acceptance remains pending.

## Earlier r08 validation and limitations

All thirteen original fixed cameras were rendered. Fresh GLB import verified bounds,
triangles and UVs. All 84 hangar ribs are separated from wall faces by 0.5 m,
retaining 102 m clearance. Original r01 fails this regression at zero gap. Close
native views sampled while the camera moved show clean inner walls. The native
route/door, original and expanded town sessions, calibration retention, cleanup,
nine-file style check and 58-check Viewer hardening gate pass. Actual main-Viewer
O and Orbit button checks retain the current panned target and zoom.

Elevators and hangar panels remain static in the game. General collision-proxy
loading is not implemented; navigation uses the controlled central route and door
gate. Old collision exports describe the original shell, not the new facade.
The optional 14-model Tripo castle does not fit in the eight remaining model slots;
separate asset consolidation would be needed. Fine distant trim can alias at
subpixel sizes. Mobile chat-image visibility is unconfirmed; no public upload
was made. Web development remains paused; native Studio development is active.

Full authoring history: D:/Projects/Sin-Star-I-Assets/Neris-Spaceport-01.
See INTEGRATION-HANDOFF.md for current evidence and Revisions/r001 for original metadata.
