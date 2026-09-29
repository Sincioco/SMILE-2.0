# Neris Horizon — Gentle Wave r007

The current airport is in western Neris Town. Revision r007 uses the castles'
actual teal-roof and gold materials, ivory arched facade details, gold roof ribs,
window tracery and entrance trim. The terminal mixes 304 opaque blue panels with
32 clear passenger panels. All 196 original skylight panels remain unchanged.
Each side hangar has a real glazed roof opening, with gold borders: 38.5 × 84 m
and 24 × 74 m. The rear halls restore the earlier opaque blue glass, ivory sawtooth
roofs and gold frames, with 16 evergreen planters clear of the aircraft lanes.
The **900 × 50 m rear runway**, parallel taxiway and hall positions remain intact.
The 960 × 600 m site, 520 × 150 m terminal and 285 m antenna remain full size.
The beacon housing raises the measured maximum height to 285.30 m.

## Current files

- `Source/Neris-Horizon-Gentle-Wave-r007.blend`: isolated authored airport.
- `Town/r007/Neris-Town-Horizon-r007.blend`: full review town, with all 362
  original placements and unrelated objects preserved.
- `Previews/r007`: actual Blender Rear, Top, Hero and Front renders, plus
  `Native-Front.png` and `Native-Rear.png` captured from the rebuilt live Viewer.
- `Native` and `Revisions/r007/Native`: checked matching exports, 3 models,
  25 material parts and 69,264 triangles. The third aircraft reuses the transport
  geometry at 55% size; the native owner draws 31 objects.
- `Town/r007/Neris-Town-Horizon-r007.town`: layout snapshot. Live Viewer saves
  were not replaced; the update changes the airport asset and its native behavior.

r006 and earlier sources remain intact. Original Spaceport 01 remains in its own
**Neris Spaceport** town/tab, connected by the existing two-way foot route. All
castles remain intact. The map footprint and existing roads did not change in r007.
Future Save For Blender exports append the r007 airport collection.

## Movement and lighting

The third aircraft repeats a 180-second cycle: wait, accelerate, climb away,
return on approach, touch down, slow along the runway, follow the paved end turn,
and taxi back on the parallel lane. Its 45.1 m wingspan clears both moved glass
halls; its original side-apron siblings keep their previous cycles. Flight motion
is implemented by `NerisRunwayTraffic`, with resources owned by
`NerisHorizonPreview`; it does not require another model or runtime pool increase.
The model's native nose is -Z after asset cooking. Heading now includes the
required 180-degree correction and pitch follows that same native axis.

The centered tower antenna has a red halo beacon, on for 0.45 seconds every
1.5 seconds. Seventy-one inset runway fixtures provide edge, centerline and
threshold guidance. `NerisHorizonGlow` owns one 72-particle batch including the
beacon; `NerisHorizonLighting` retains the 13 concealed building light sources and
moves the two glass-hall sources to their new positions. There are no light poles.
The blinking beacon and aircraft cycle run in the native Viewer; the Blender
review contains the authored fixtures and a parked third-aircraft preview.

Arin and all followers can walk from the town road through the physically clear
23.4 m central entrance and around the terminal ground floor. Parked sliding
leaves flank the opening. Counters, seating, remaining columns, stairs and glazing
have solid pedestrian footprints; runway/taxi areas are excluded. Upper-floor
stair traversal is not part of this update. `NerisHorizonRoute` owns these bounds
and the matching step/landing heights.

Follow Party orbit uses the leader's position as its pivot for O, the Orbit
button, mouse orbit and arrow orbit. It retains the current camera position on
activation and follows the leader as the party moves. Free-camera right-click
uses depth picking (ground-plane fallback) to orbit the point under the cursor,
without moving the eye on activation. O/Orbit retains its current pivot. The framing algorithm stays in
`NerisTownCamera`, with small input wiring in `NerisTown`.

## Validation

Blender checks pass for the unchanged terminal skylights, both open hangar
skylights, gold curbs, planters, opaque hall glazing, clear entrance, parallel
halls, 71 runway fixtures and canonical checksums. The town assembly retains all
362 placements and 16,842 objects outside the airport collection. The Horizon
mesh has 127,253 vertices, under the existing 131,072 limit; buried roof-cell
walls were removed rather than increasing limits.

Native editor foundations, route/navigation, scene/camera, rendering and full-asset
session checks pass. They cover all four party members entering the terminal,
eight actual runway-aircraft draw phases, seven nose-versus-motion samples,
beacon timing, both named towns, round-trip walking and resource release/reload.
Right-click checks preserve the picked world pivot and radius; close follow orbit
admits a captured radius below 80 instead of pushing the camera outward. The full
town uses 53/64 models, 499 meshes and 468 materials, with all 24 accepted Arin keys.
The native hardening gate and its 58 graphics/input/audio checks also pass.
No compiler/runtime capability or limit changed. Other paused camera follow-up,
Studio and all Web work remain on hold.

The native Viewer was rebuilt and reopened with the user's existing town save.
A brief multi-angle inspection checked the facade, roof openings and restored
rear halls, and the actual right-click control was exercised. The r007 airport
is also open in Blender. No user .NET rebuild, app restart or browser refresh is
required. Current implementation owners and focused growth review are in the
Character Viewer's `ARCHITECTURE.md`.
