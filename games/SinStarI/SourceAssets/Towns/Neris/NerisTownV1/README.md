# Neris Town V1 - Waterfront Blender Scene and Native Assets

Created by Louiery R. Sincioco (Sin).

## Current scene

**[Open Neris-Town-Waterfront.blend](Blend/Neris-Town-Waterfront.blend)**.
This is the current saved layout used for native export. Keep the adjacent
[TripoCastleV1](../TripoCastleV1/README.md) package: the Old Castle is linked by a
relative Blender path. Preserve interactive hand edits separately before rebuilding.
The original V1, Detailed and Expanded files remain authoring inputs/earlier revisions.
Do not run their old placement scripts over the current Waterfront arrangement.

[Overview](Previews/Waterfront-Overview.png) |
[Plan](Previews/Waterfront-Plan.png) |
[City Hall](Previews/Waterfront-Civic.png) |
[Arrival](Previews/Waterfront-Arrival.png).
Named cameras match those previews. Blender middle-drag orbits, Shift+middle-drag
pans, the wheel zooms and Numpad 0 enters the active camera view.

## Accepted layout

North is Blender +Y; dimensions are metres. Map bounds are X=-490..275 and
Y=-355..355. The Old Castle is a western addition to Sin's district diagram.

| Landmark | Blender X, Y | Scale |
| --- | --- | --- |
| Old Castle (Tripo) | -366, 226 | 170 times imported source |
| Royal Castle | -116, 235 | 2 times authored palace |
| Military HQ | 145, 242 | 2 times original |
| Comm Tower | 0, 60 | 4 times original, proportionate |
| City Hall | 0, -75 | 2 times original |
| Kingdom of Neris arch | 0, -265 | Original proportions |

The two castles have separate broad moats and one continuous land-backed main
road around them. The circuit continues around military HQ. Connected main
streets, district crossroads and short front paths serve eight rich-district
houses, ten middle houses plus six older cottages, twelve lower-district houses
plus two older cottages, and Weapon/Item/Armor shops. Homes and shops face inward;
landmark doors face south. No redundant inner enclosing roads remain.

Two bridges join the City Hall forecourt directly to the west/east district
crossroad. All seven civic bridge rails stop at the water banks; castle and HQ
rails retain their full lengths. City Hall paving fills both side strips to the
canal banks and reaches Y=-130.52. The fountain at Y=-112 sits 18.52 m from both
the stair foot and the southern plaza edge. The isolated original crystal circle
west of the park avenue is removed; the other garden circles remain.
The arrival avenue reaches the southern edge. Its sign sits just north of the
lower crossroad, at Sin's yellow mark. Arin initially stands 13 m south of it.
The avenue forms a grand park with trees, benches, lamps and flower beds. Other
gardens fill residential/market lawns. There are 147 detailed trees, 48 shared
flower planters, seven fountains and additional crystal circles. The City Hall
fountain sits south of the stairs, with room to approach around it. Decoration
placement checks keep managed footprints away from roads, water and buildings.

All water shares the deep-blue moat palette. Only bridges cross water; pale edging
follows actual shorelines. Paving shares a world-aligned two-metre grid without
wavy albedo/normal textures. Filtered, lower-contrast grass reduces motion shimmer.

## Ownership and regeneration

| Source | Responsibility |
| --- | --- |
| `waterfront_plan.py` | District/landmark/sign coordinates, roads, bridges and water |
| `waterfront_town.py` | Reorganize saved architecture, build terrain/roads, save/render Blender |
| `waterfront_landscape.py` | Gardens, avenue trees, benches/lamps and clearance checks |
| `castle_architecture.py`, `castle_facades.py` | Independently authored clean castle and detail |
| `paving_grid.py`, `align_paving.py` | One tile union, shared origins and separated bridge layers |
| `prepare_entrances.py`, `export_entrances.py` | Authored hinged leaves and stair/threshold data |
| `export_native.py`, `static_glb.py` | Evaluated static partitions and normals; no mesh decimation |
| `export_flowers.py` | Shared flower template and instances |
| `export_layout.py` | Tree/lamp/crystal/paving placement data |
| `collision_bounds.py` | Separate royal portal jamb/crown bounds, preserving hollow openings |
| `export_site.py`, `export_camera.py` | Ground solids and conservative camera volumes |
| `export_waterfront.py` | Ordered export of the saved Blender scene |
| `render_minimap.ps1` | Current map, leader marker and heading sheet |

From the repository root in PowerShell 7:

```powershell
$blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$town = 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'
& $blenderExe --background "$town/Blend/Neris-Town-Expanded.blend" --python-exit-code 1 --python "$town/Source/waterfront_town.py"
& $blenderExe --background "$town/Blend/Neris-Town-Waterfront.blend" --python-exit-code 1 --python "$town/Source/export_waterfront.py"
& "$town/Source/render_minimap.ps1"
& ./tools/Character3DViewer/Launch.ps1 -Build
```

Regeneration always starts from Expanded, not the already rearranged Waterfront.
The builder also repairs the input's old threshold overhang before exporting.
The static manifest records saved-source and output SHA-256 hashes. Runtime maps
Blender XYZ to native XZY at ten units/metre plus 21 units of height.

## Native behavior and loading

See the [Viewer controls](../../../../../../tools/Character3DViewer/README.md).
Town starts in Run at 200%. Ctrl+Tab cycles Arin, Orin, Zara and Mira. Followers
fade before crowding the selected leader. Tab eases north behind the leader in one
second; Ctrl+F toggles slow drone following. Floor remains F. The drone stays low,
looks slightly upward and moves closer in royal/HQ grounds and near doors.
Manual controls permit looking up. Fit retains orbit, which automatically hides
the minimap. The map shows the leader's name and X/Y/Z.

The static town is 37 models / 92 parts / 4,029,482 triangles. Trees and flower
planters use reusable templates and draw objects. The separate Old Castle has
14 models / 28 parts, shared 2K runtime maps and unchanged 4K originals. Identical
cooked static textures share publication paths. A local native check measured
about 0.63 seconds for town plus Old Castle model/material setup (about 0.24 seconds
for Old Castle alone). Actor loading, frame scheduling and the required visible
logo interval are additional; this is not a cold-start guarantee.

## Validation and limits

- Native full-scene checks pass for loading/drawing/resource cleanup, 24 accepted
  Arin keys, camera transition/bounds, follower fade and leader context cycling.
- Route checks traverse the complete royal bridge and hollow entrance gate,
  scaled tower stairs and City Hall stairs. All 66 authored doors open inward,
  produce one entry event per approach, and close after leaving.
- Waterfront checks cover connected main/district roads, all home/shop approaches,
  full south junctions and managed garden footprints clear of roads/water.
- Paving checks cover one tile grid, no duplicate faces and open water outside bridges.
  Background Blender checks all ten bridge support/grout/tile separations and
  civic rail footprints at the water banks.

Collision uses conservative solid rectangles and authored stair surfaces, not a
full triangle navmesh. The Old Castle is a fused reference mesh without separately
interactive leaves. Authored doors expose doorway IDs; interior scenes and cutscene
content are not created or loaded. Native rendering differs from Blender's lighting.
Seven fountains plus water currently use the full 16-ribbon batch allowance.
Current camera/gait feel remains available for Sin's visual review.
Studio development and Web adoption/publication/browser validation remain on hold.
