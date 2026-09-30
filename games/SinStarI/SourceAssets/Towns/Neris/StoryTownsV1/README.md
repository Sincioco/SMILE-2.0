# Luma town designs

Editable native `.town` maps for Sin Star I. These reuse the repository's Neris
catalog and terminal assemblies; no downloaded assets or new dependencies.

| Map | Design |
| --- | --- |
| Neris Canals | Symmetrical cross-canals, royal avenue, market streets and orchards |
| Neris Star Lake | Circular reservoir, eight bridge spokes, enlarged royal island and inward-facing homes |
| Neris Crown Isles | Nine garden islands with illuminated connections |
| East Valley | Small castle-free relief village, City Hall, memorial gardens and orchards |
| Orin's Village | Small castle-free home village, City Hall, twin lakes and repair stalls |
| Neris Spaceport | Circular arrival landscape, eight approach bridges, embassy gardens and water pavilions |
| Horizon Airport | Scalloped lagoons, twin arrival villages, perimeter drive and pavilion piers |
| Verdant Reach | Forest trail, two ponds and three sheltered encounter clearings |
| Greyglass Pass | Winding level path between layered mountain ridges |
| Sunglass Expanse | Dunes, sandstone mesas and an oasis loop |
| Neris Waterworks | Six reservoir basins, pump platforms and maintenance bridges |
| Ancient Relay | Weathered rock spires around concentric service paths |
| Neris Relief Quarter | Clinic courtyard, modest homes and provision stalls |

The first five maps each have west/east pedestrian map tiles leading to Neris
Spaceport and Horizon Airport. Airport return travel remembers the town of origin.
The new airport landscapes retain the native terminal, doors, traffic and runway.
The original Neris Town is not replaced by these alternatives.

`Luma - Story Atlas.world` is a separate fourteen-map graph including original Neris.
It does not overwrite Sin's `Luma.world`. Graph connections visualize the network;
actual scene transitions use the destination tiles in each town document.
Open the atlas through **Edit Town → Files → Open World**. Missing images are
prepared by visiting saved maps, which also opens their tabs. **Demo** cycles the
open tabs once per minute and immediately starts orbit; click it again to stop.
**Maps** displays the open tabs as a four-column gallery with perspective thumbnails.
The bottom **World Map** button reopens the atlas; double-click a card to enter.

## Story authority and remaining work

Story references were read from the independent canonical repository at
`D:\SMILE 2.0 - Sin Star I\Visual Script and Storyboard`: `Canon.md` and
`Storyboard-Draft-1/Storyboard.md`. East Valley and Orin's unnamed home village
appear in chapter 6. **Orin's Village is a working label**, not an invented canon
proper name. The three additional Neris designs are alternatives, not a claim
that the script establishes three more capital cities. Luma is Sin's current name
for the opening planet. Veyra is a different world and is not placed on this map.

The Waterworks, relief/clinic area and ancient relay interpret chapter 1. The relay
is not chapter 5's off-world observatory. Verdant Reach, Greyglass Pass, Sunglass
Expanse and the district labels are working design names, not new script canon.
The journey maps reserve clearings for future encounters; monsters, combat and
leveling in these maps are not implemented. Original Neris has nine prototype
residents; the generated alternatives do not yet have inhabitants.
The atlas is not a claim that every Sin Star I location has been implemented.

All 25 atlas connections have reciprocal road markers. The original Neris receives
only two additional marker pads; its edited buildings, terrain and existing markers
are retained. Arrival uses the matching entrance and a collision-checked adjacent
road cell outside the trigger. Explicit airport destinations retain their names;
legacy shared-airport exits still remember their origin. The native route check
covers all 50 directed entrances.

## Road and placement cleanup

`Source/town_access.py` owns generated-map plot placement, front-door connections
and full scenery-footprint clearance. It considers rotated model bounds and whole
tree clusters. Small paths join the existing street network without passing through
another building. `validate_access.py` protects the reported blocked-road and
missing-entrance defects; it checks all thirteen generated maps and 161 entrances.
The user-approved center tower in Ancient Relay and Waterworks pump platforms have
explicit exceptions. Castles and airport terminals retain their authored approaches.

Star Lake keeps its enlarged royal island and smooth rings, removes the six shops,
and routes the cross-island avenue in front of the castle bridge. Orin's Village
uses a clear perimeter street and two garden ponds. Crown Isles has building plots
off its through roads. Relay obelisks are offset from axial paths. Forest/mountain
encounter clearings and the desert oasis loop are retained as requested.

Spaceport keeps its terminal and overall landscape. Its service drive joins the
approaches, seven travel exits sit on dedicated outer spurs, the arrival circle is
beyond the apron, and trees/fountains occupy gardens. `validate_spaceport.py` checks
those specific regressions. Horizon retains its design with corrected plots and
entrance paths. These latest cleanup documents still need installation and visual
acceptance in Studio; passing geometry checks alone does not establish appearance.

## Reproduce and install

Run `Source/build_towns.py`, `Source/build_journeys.py`, then `Source/build_world.py` with the existing Python
runtime. The generator validates document round trips, authoring/resource bounds,
road connectivity, destination tiles and intended mirror symmetry. `manifest.json`
records counts and design notes. `world-layout.json` records graph positions/links.
After regeneration, run `Source/connect_world.py --original <current-Neris.town>
--output <staging-folder>` to add the atlas's reciprocal road markers. It prepares
an additive Neris copy in staging and updates the thirteen generated source maps.
`travel-connections.json` records the resulting destination network.

Close Studio normally before using `Source/install_maps.py --data <Data folder>
--backup <new backup folder>`. It validates each input and retains every replaced
save. It installs named copies and replaces the two permanent airport keys;
original Neris and the user's original Luma world stay intact. The runtime Data
folder is `%LOCALAPPDATA%\SMILE 2.0\Games\<SHA256 of
smile.tools.character3d-viewer>\Data`. Generated runtime `.bin` files are not source
assets and are not committed.
To install the original Neris marker additions too, pass `--original
<staging-folder>/Neris Town.town`. The installer rejects a stale copy if any current
terrain, assembly, lighting or existing marker differs, and backs up the old save.

## Curved terrain

These maps use TWN6 analytic circles, rings and paths. Base grid cells remain the
navigation broad phase; exact point tests resolve authored curves. Surface meshes
follow interpolated boundaries instead of exposing the navigation stair steps.
Building rotations retain decimal degrees. The existing Blender snapshot preserves
curve metadata on round trip; its terrain display still uses raster cells.
Bulk section transfer of curved terrain is explicitly unavailable; individual
buildings remain movable. Very narrow subcell features are a remaining tessellation
limitation; the generated maps use roads wider than their terrain cells.

## Journey terrain and landforms

TWN7 adds a document terrain style: Meadow, Forest, Highland or Desert. Studio's
Items → Surfaces → Terrain button cycles the style, with Undo. Versions 1–6 remain
readable as Meadow. Walking terrain remains level; the mountains, mesas, dunes and
spires are movable collision-bearing scenery, not climbable heightfields.

The four appended catalog templates (35–38) are generated with the standard-library
`../NerisTownV1/Source/journey_landforms.py`. Existing template IDs and catalog
fingerprint stay intact. After generating that chunk, run the existing catalog
native-data generators and Prepare-TownEditorAssets before compiling. The Blender
export reproduces the same geometry and retains whole-assembly move/scale/rotation.

Validation includes six native collision/path queries between entrance markers,
terrain-style persistence, the standard native rendering fixture, a desert/landform
Blender export/reopen, and expanded World coordinates save/reopen. New maps are
visually checked in Studio before release; screenshots are progress evidence, not
evidence that combat or encounters exist.
