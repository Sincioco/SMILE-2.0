# Neris Horizon — Gentle Wave r009

Horizon Airport now has its own third town tab, east of compact Neris Town.
Neris Spaceport is west. Road gateways carry Arin and the party
between the three saved maps. All 362 central-town placements are preserved.

The airport retains teal wave roofs, terminal and hangar skylights, gold facade
ornaments and partially transparent ordinary glazing. The two hangar back walls
align with the main terminal; their vertical glass uses 80% opacity. The VTOL
noses face outward. Two runways connect through U-turn taxiways. Regional flights
arrive every 15 seconds and a larger cargo aircraft returns every 150 seconds.

The roof sign remains above the entrance. Its two banners now hang below the roof
on clear window bays, centered at local X=±91 m and topped at 44 m. The 129 fine
floor-joint meshes were removed; gold approach lines, compass medallions and flush
floor lights remain. Both connecting roads end at the modeled decks, eliminating
coplanar terrain overlap. The white platform border remains removed.

## Night and entry

A sun/moon header button controls each selected town's existing saved day/night
preset. Horizon uses 27 concealed lights, including ten runway washes, three roof
washes and a sign wash. Runway guide halos are larger and brighter. No light poles
were added. Three red antenna beacons alternate in an irregular sequence.

The gold-framed two-leaf terminal door opens on approach and permits all party
members through. The route owns a consumable entry event for a future cutscene;
no cutscene is played yet. Visit Horizon Airport places Arin outside the entrance
with the camera looking forward from behind him.

## Current editable files

- `Source/Neris-Horizon-Gentle-Wave-r009.blend`: authored airport and preview aircraft.
- `Town/r009/Horizon-Airport-r009.blend`: airport map with matching portable `.town`.
- `Town/r009/Neris-Town-r009.blend`: compact town and matching `.town`.
- `Town/r009/Neris-Spaceport-r002.blend`: original spaceport, four alien visitors and `.town`.
- `Town/r009/Neris-Region.world`: west/central/east world-editor links.
- `Native` and `Revisions/r009/Native`: five matching native model exports.
- `Previews/r009`: actual Blender renders and native night inspection.
- `Revisions/r009/validation.json`: current measured geometry and validation evidence.

Save For Blender appends only the selected map's landmark assemblies. The original
Spaceport 01, prior revisions and castle packages remain intact. Migration copies
and later road fixes were backed up before changing live saves; later user edits
and sun settings remain authoritative.

## Map controls and ownership

Floor and grid dimensions and center follow the committed map bounds, including
later expansion or contraction. Airport tabs immediately orbit the map center.
Neris right-click overview uses the approved 146°/20°/8500-distance framing around
that shared center. Follow Party keeps the leader as its anchor. Ctrl+click outside
Edit Town moves the party to the closest traversable road. Bottom diagnostics count
submitted mesh triangles and render vertices, including offscreen objects and
actors, excluding particles and repeated shadow/reflection passes.

Horizon uses five models, 35 parts and 89 draw objects. The door has four parts;
the main preview owns 85 objects. The unchanged shared pools also accommodate the
four alien visitors on the original spaceport's pads. Traffic, doors, lighting,
map persistence, camera interaction and HUD statistics stay in their focused owners.
Native is implemented and tested; Studio and Web adoption remain on hold.
