# Luma town designs

Editable native `.town` maps for Sin Star I. These reuse the repository's Neris
catalog and terminal assemblies; no downloaded assets or new dependencies.

| Map | Design |
| --- | --- |
| Neris Canals | Four-point Neris star, triangular residential quays and bridge approaches |
| Neris Star Lake | Circular reservoir, eight bridge spokes, enlarged royal island and inward-facing homes |
| Neris Crown Isles | Seven paired palm branches inside an open circular perimeter |
| East Valley | Six-point star, circular perimeter, concentric promenades and six memorial gardens |
| Orin's Village | Circular civic hub, four round residential neighborhoods and eight curved garden branches |
| Neris Spaceport | Circular arrival landscape, eight approach bridges, embassy gardens and water pavilions |
| Horizon Airport | Scalloped lagoons, twin arrival villages, perimeter drive and pavilion piers |
| Verdant Reach | Forest trail, two ponds and three sheltered encounter clearings |
| Greyglass Pass | Winding level path between layered mountain ridges |
| Sunglass Expanse | Dunes, sandstone mesas and an oasis loop |
| Neris Waterworks | Five crescent basins and five round gardens around interlaced star service paths |
| Ancient Relay | Weathered rock spires around concentric service paths |
| Neris Relief Quarter | Level symmetrical civic square, City Hall, mirrored streets, homes, shops and gardens |
| Willowstep Highlands | Snowy massifs up to 154 m, graded trails, winding river, irregular lake and mountain landmarks |
| Silverfall Basin | Three 14 m terrain terraces, a 42 m climb, encounter shelves and a cascading stream |

The two terrain journeys are available through **Maps**, with Demo disabled by
default. Their west/east exits connect to each other and existing locations; the
older fourteen-map atlas remains unchanged. Their working names do not add story
canon. Water follows the sculpted bed; there is no vertical free-fall simulation.
These landscapes reserve room for questing and grinding but do not add quests,
enemies or combat rules. Willowstep has four snowy massifs, a winding downhill
stream, irregular collection lake, three buildings and small rock groups. Silverfall
retains pale cascade banks, two distinct dirt trails and a large irregular lake
that reaches the boundary. Greyglass uses softened ridge summits and a flattened
house plot; Verdant has irregular lakes and no redundant central road spur.

Six wilderness maps (Verdant, Greyglass, Sunglass, Ancient Relay, Willowstep and
Silverfall) use reusable campfires instead of street lamps. The manifest records
current item counts, height ranges, input checksums and acceptance routes.
All props are grounded against the native terrain sampler.

`Source/terrain_quests.py --output <new-folder>` authors the landscape set without
overwriting existing output. `journey_layouts.relief` uses `relief_landscape` for
the symmetrical town. Run `town_access.prepare` before saving a new building layout.
Use `prepare_maps.py` to bake a separate prepared folder, then
`validate_terrain_quests.py --maps <prepared-folder> --manifest <source-manifest>
--work <new-work-folder>` for native round trips, downhill flow and sampled 4 m
route corridors in both directions. The current full landscape pass is recorded
in `artifacts/landscape-v8-routes.log`; all sixteen actual native gateway arrivals
also pass standability, clear forward travel and trigger exclusion.

October 3: all sixteen documents have preparation 10 mesh recipes with refined
surface contours and closed road/shore sides. `scripts/test-town-surface-mesh.py`
checks the actual prepared faces against authored surfaces and checks shared wet
edges for missing side walls. Relief alone has a new authored layout; its symmetry,
native routes and existing travel destinations are checked before installation.

The installer accepts `--map` to limit the selection. It requires Studio closed,
a fresh backup folder and a matching baseline, rejecting newer user layout edits.
Use `--boundary-markers` explicitly when applying the requested edge policy; omit
it to preserve independently moved markers. `Towns/Neris Town.town` retains the
original town layout with its boundary approaches/markers updated. The other
fifteen documents remain independent. The existing sixteen-tab limit is unchanged.

The first five maps each have west/east pedestrian map tiles leading to Neris
Spaceport and Horizon Airport. Airport return travel remembers the town of origin.
The new airport landscapes retain the native terminal, doors, traffic and runway.
The original Neris layout is retained alongside these alternatives.

`Luma - Story Atlas.world` is a separate fourteen-map graph including original Neris.
It does not overwrite Sin's `Luma.world`. Graph connections visualize the network;
actual scene transitions use the destination tiles in each town document.
Open the atlas through **Edit Town → Files → Open World**. Missing images are
prepared by visiting saved maps, which also opens their tabs. **Demo** cycles the
enabled maps every 30 seconds and immediately starts orbit; click it again to stop.
Each thumbnail has a saved Demo toggle. Only Neris Town, Neris Spaceport, Horizon
Airport and Neris Star Lake participate by default.
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
square boundary approaches and full-width marker updates; its buildings remain intact.
Triggers cover the final 6 m of the road, side by side when sharing an exit. Arrival uses
the matching entrance and a clear inward route outside every trigger. Explicit airport destinations retain their names;
legacy shared-airport exits still remember their origin. The native route check
covers all 50 directed entrances.

## Road and placement cleanup

`Source/town_access.py` owns generated-map plot placement, front-door connections
and full scenery-footprint clearance. It considers rotated model bounds and whole
tree clusters. Small paths join the existing street network without passing through
another building. `validate_access.py` protects the reported blocked-road and
missing-entrance defects; it checks all thirteen generated maps and their entrances.
The user-approved center tower in Ancient Relay and Waterworks pump platforms have
explicit exceptions. Castles and airport terminals retain their authored approaches.

Star Lake keeps its enlarged royal island and smooth rings, removes the six shops,
and routes the cross-island avenue in front of the castle bridge. Orin's Village
uses four round neighborhoods and eight satellite gardens connected by curved paths. Crown Isles has building plots
off its through roads. Relay obelisks are offset from axial paths. Forest/mountain
encounter clearings and the desert oasis loop are retained as requested.

Spaceport keeps its terminal and overall landscape. Its service drive joins the
approaches, seven travel exits sit on dedicated outer spurs, the arrival circle is
beyond the apron, and trees/fountains occupy gardens. `validate_spaceport.py` checks
those specific regressions. Horizon retains its design with corrected plots and
entrance paths. These cleanup documents are installed; direct visual acceptance
remains pending where desktop tools were unavailable. Passing geometry checks
alone does not establish appearance.

East Valley follows Sin's six-point star-and-circle reference. Twenty-four homes
face the radial avenues; six round memorial gardens join the outer promenade.
The City Hall faces a short entrance walk ending at the innermost ring, and twelve
small ponds surround the civic center. There are 66 trees and 80 lamps. All 25
entrances pass the footprint/access check; native preparation and fresh import
reuse the saved terrain and road data. Use `--map "East Valley"` with the installer
to replace only this map, preserving edits in every other town.

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
folder is `<Windows Saved Games>\SMILE 2.0\Games\<SHA256 of
smile.tools.character3d-viewer>\Data`; resolve it with
`scripts/get-smile-data-root.ps1`. Generated runtime `.bin` files are not source
assets and are not committed.
To install the original Neris marker additions too, pass `--original
<staging-folder>/Neris Town.town`. The installer rejects a stale copy if any current
terrain, assembly, lighting or existing marker differs, and backs up the old save.

## Curved terrain

These maps use TWN9 analytic circles, rings, paths, rounded junctions, triangles
and quadratic Bezier strokes. The editor exposes Triangle, Curve and Edit Shape
Points with a thickness setting. Three clicks create a shape; dragging a saved
handle changes it on release. Curves preserve continuous geometry on save. Base grid cells remain the
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

## October 3 waterways and shared terrain fixes

Willowstep's central dead-end spur is removed. Three quadratic meltwater bends
feed a larger irregular lake open across approximately 141 m of the south edge.
The southern lake footprint is about 10,890 m² (previously 5,211 m²). Travel marker
placements/destinations are preserved; campfires overlapping the changed water
are excluded and retained props remain terrain-grounded.

Relief's desert/grass and mountain/grass boundaries use the shared material contour
builder. Willowstep, Relief and Silverfall documents carry version 9 preparation.
Reusable navigation adds elevation at road-over-water cells, fixing the party and
camera drop in both wilderness maps. `validate_terrain_quests.py` checks road and
bridge heights against canonical terrain along authored routes. Placed campfires
use Studio's animated Fire VFX without additional model or texture dependencies.

## Prepared installation

Run `Source/prepare_maps.py` against a separate authored-map directory. It invokes
the native save-preparation owner, exports portable bundles, then verifies every
terrain page and road graph in a fresh native import. `Source/install_maps.py`
requires these bundles and a baseline directory when replacing an older revision.
Close Studio first. A dry run checks for newer user geometry before any write;
the real install backs up every replaced key and retains per-map lighting.
The shipped Towns directory contains prepared files. Original Neris remains the
live PermanentNeris save; it is never regenerated from the alternative layouts.

### Crown Isles palm refinement

The transverse road through the palm is removed. Two short curved bridges near
the trunk base connect the perimeter, preserving every leaf-shaped branch.
The current map has 42 homes (previously 28), 96 trees (43), and 65 lamps (37).
All 43 building entrances reach pavement, and the full travel network remains
connected. Lamps follow the branches as well as the trunk and outer promenade.

### Orin and Waterworks reference redesigns

Orin's Village follows Sin's round hub reference: two central rings, four large
residential circles, and eight small fountain gardens on quadratic Bezier branches.
Sixteen homes face their neighborhood loops; 36 trees and 36 lamps follow the banks.
The central City Hall retains a short front-door connection. It remains castle-free.

Neris Waterworks follows the reference's five large circles, five small gardens
and two interlaced five-point service stars. Sin's refinement fills each large
reservoir with water up to its circular road, retaining the small central pump
platform and access bridge. Trees stand on the outer banks; 28 lamps light the
paths. These are level, editable landscape shapes; moving water and disaster
gameplay are separate work.

Both native prepared bundles freshly import without terrain/road rebuilding. All
50 directed atlas routes pass the native arrival/search check, including inward
walking clear of travel triggers. Eighteen building entrances and scenery clearance
pass authoring checks. The selective installer retained existing lighting, backed
up previous saves and changed only these two maps plus their prepared records.
Native Studio screenshots were inspected on October 1: Orin's Village loaded in
125 ms and the fully flooded Waterworks revision in 133 ms. The five filled basins,
pump platforms and bridges were directly inspected in the native application.
These are observed map switches, not cold-start benchmarks on other
hardware. Other authored maps and both Neris castles are intact.
# October 1 evening refinements

Crown Isles retains its leaf pattern and now trims the inward east/west road stubs
to the perimeter. Orin's Village has a small central pond in each of its four round
neighborhoods; east/west exits meet the outside ring instead of crossing the centers.
Spaceport's authored night preset is brighter. Studio additionally supplies its blinking
red antenna beacon, pad rim lights and apron lighting.

The installer preserves current day settings, travel destinations and newer user layouts;
replacing Spaceport's night preset is an explicit installation option. This installation
backed up all replaced save records. Native preparation and thirteen directed gateway
routes passed. Crown and Orin were inspected in the current Release application.

## October 2 road-cache repair

The batch preparer formerly reused generation 1 for successive documents, which
could attach an earlier map's road graph to a later map. Each preparation and
verification now advances its generation. The new native journey fixture caught
the error before the two terrain maps were accepted. It also caught a side trail
interrupted by a gateway bank; Silverfall's junction now stays inside that bank.

`Source/repair_prepared_roads.py` rebuilt all thirteen older authored-map graphs
with the native collision owner. Eight needed correction. Every authored payload
and every other prepared record stayed byte-identical. Live older-map bindings
are version 3, already rejected by the current version-4 terrain runtime; their
locally rebuilt road caches and all user saves were preserved. Only the two new
maps were installed, with a backup in `live-backup-v5`. Native repair evidence is
`artifacts/terrain-existing-road-repair.log`.

Native Release acceptance after installation: Willowstep loaded in 173 ms and
Silverfall in 152 ms. Actual minimap travel finished at Willowstep X-1320/Y381/Z-405
and Silverfall X1395/Y443/Z-555. These are observed arrivals, not continuous
manual motion recordings. Screenshots: artifacts/willowstep-summit-arrival-native.png,
artifacts/silverfall-basin-native.png and artifacts/silverfall-high-terrace-arrival-native.png.
The terrain core's loaded four-actor uphill/downhill fixture separately checks
all followers against the same ground sampler.

## October 2 wilderness landscape refinement

Verdant Reach, Greyglass Pass, Willowstep Highlands, Silverfall Basin and Sunglass
Expanse use broad Bezier trail bends. The first four no longer have large circular
road clearings. Verdant has irregular curved lakes and rounded hills; Greyglass
has varied terrain plateaus with graded paths to 54 m and 38 m summits, plus a few
smaller rock formations. Silverfall uses meadow ground, 180 trees, a 24 m-wide
terraced stream and an 84 m-wide receiving lake. The stream follows the terrain;
it is not a free-falling waterfall simulation.

The selective v9 installation backs up every replaced record and preserves newer
lighting/terrain-style choices. Atlas destinations and trigger coordinates are
retained. Both castles and every other map are untouched. Native preparation,
round trips, complete routes, four-metre trail corridors, grounded props and
spawn-to-gateway routes passed; logs are terrain-refinement-prepare-v9.log and
terrain-refinement-routes-v9.log. The generator uses a fresh output folder and
never overwrites its previous output. Source/terrain_quests.py now rebuilds these
five outdoor landscapes. New recipes require prepared bundle version 5.
