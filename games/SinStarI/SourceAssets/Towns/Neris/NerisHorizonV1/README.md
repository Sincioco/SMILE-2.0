# Neris Horizon — Gentle Wave r006

The current airport is in western Neris Town. Revision r006 adds a **900 × 50 m
rear runway**, a parallel return taxiway, and rotates both existing glass halls
inward so their long sides run parallel to the runway. Both hangars now use ivory
wave roofs matching the terminal. Their original entrances and aircraft remain.
The 960 × 600 m site, 520 × 150 m terminal and 285 m antenna remain full size.
The beacon housing raises the measured maximum height to 285.30 m.

## Current files

- `Source/Neris-Horizon-Gentle-Wave-r006.blend`: isolated authored airport.
- `Town/r006/Neris-Town-Horizon-r006.blend`: full review town, with all 362
  original placements and unrelated objects preserved.
- `Previews/r006`: actual Blender Rear, Top, Hero and Front renders.
- `Native` and `Revisions/r006/Native`: checked matching exports, 3 models,
  23 material parts and 60,680 triangles. The third aircraft reuses the transport
  geometry at 55% size; the native owner draws 29 objects.
- `Town/r006/Neris-Town-Horizon-r006.town`: layout snapshot. Live Viewer saves
  were not replaced; the update changes the airport asset and its native behavior.

r005 and earlier sources remain intact. Original Spaceport 01 remains in its own
**Neris Spaceport** town/tab, connected by the existing two-way foot route. All
castles remain intact. The map footprint and existing roads did not change in r006.
Future Save For Blender exports append the r006 airport collection.

## Movement and lighting

The third aircraft repeats a 180-second cycle: wait, accelerate, climb away,
return on approach, touch down, slow along the runway, follow the paved end turn,
and taxi back on the parallel lane. Its 45.1 m wingspan clears both moved glass
halls; its original side-apron siblings keep their previous cycles. Flight motion
is implemented by `NerisRunwayTraffic`, with resources owned by
`NerisHorizonPreview`; it does not require another model or runtime pool increase.

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
activation and follows the leader as the party moves. Free inspection retains
its existing cursor/current-view orbit. The framing algorithm stays in
`NerisTownCamera`, with small input wiring in `NerisTown`.

## Validation

Native editor foundations, route/navigation checks, rendering, and full-asset town
session pass. The session exercises all four party members entering the terminal,
eight actual draw phases for the third aircraft, beacon timing, both named towns,
resource release/reload, and Follow Party orbit. Neris Town uses 53/64 models,
497 meshes and 466 materials; all 24 accepted Arin pose keys remain loaded.
Blender geometry checks confirm the empty entrance, parallel hall bounds, two wave
hangars, runway dimensions, 71 inset fixtures and canonical export checksums.

No compiler/runtime capability or limit was changed. No commit or push was made.
Other paused camera follow-up, Studio and all Web work remain on hold.

Final native night inspection showed the runway fixtures, red beacon, third
aircraft and relocated halls; a brief multi-angle check showed no flicker.
Follow Party/O was also verified live. The final Viewer was rebuilt and restarted,
and the direct-launch file worker was verified. No user rebuild or browser refresh
is required. The r006 airport remains open in Blender.
