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

The first five maps each have west/east pedestrian map tiles leading to Neris
Spaceport and Horizon Airport. Airport return travel remembers the town of origin.
The new airport landscapes retain the native terminal, doors, traffic and runway.
The original Neris Town is not replaced by these alternatives.

`Luma - Story Atlas.world` is a separate eight-map graph including original Neris.
It does not overwrite Sin's `Luma.world`. Graph connections visualize the network;
actual scene transitions use the destination tiles in each town document.
Open the atlas through **Edit Town → Files → Open World**. Missing images are
prepared by visiting saved maps, which also opens their tabs. **Demo** cycles the
open tabs once per minute; click it again to stop. Tab arrows page long lists.

## Story authority and remaining work

Story references were read from the independent canonical repository at
`D:\SMILE 2.0 - Sin Star I\Visual Script and Storyboard`: `Canon.md` and
`Storyboard-Draft-1/Storyboard.md`. East Valley and Orin's unnamed home village
appear in chapter 6. **Orin's Village is a working label**, not an invented canon
proper name. The three additional Neris designs are alternatives, not a claim
that the script establishes three more capital cities. Luma is Sin's current name
for the opening planet. Veyra is a different world and is not placed on this map.

Forest, mountain and desert journey maps, additional required story locations,
encounters and NPC inhabitants are still outstanding. The current atlas is not a
claim that every Sin Star I location or combat encounter has been implemented.

## Reproduce and install

Run `Source/build_towns.py`, then `Source/build_world.py` with the existing Python
runtime. The generator validates document round trips, authoring/resource bounds,
road connectivity, destination tiles and intended mirror symmetry. `manifest.json`
records counts and design notes. `world-layout.json` records graph positions/links.

Close Studio normally before using `Source/install_maps.py --data <Data folder>
--backup <new backup folder>`. It validates each input and retains every replaced
save. It installs named copies and replaces the two permanent airport keys;
original Neris and the user's original Luma world stay intact. The runtime Data
folder is `%LOCALAPPDATA%\SMILE 2.0\Games\<SHA256 of
smile.tools.character3d-viewer>\Data`. Generated runtime `.bin` files are not source
assets and are not committed.

## Curved terrain

These maps use TWN6 analytic circles, rings and paths. Base grid cells remain the
navigation broad phase; exact point tests resolve authored curves. Surface meshes
follow interpolated boundaries instead of exposing the navigation stair steps.
Building rotations retain decimal degrees. The existing Blender snapshot preserves
curve metadata on round trip; its terrain display still uses raster cells.
Bulk section transfer of curved terrain is explicitly unavailable; individual
buildings remain movable. Very narrow subcell features are a remaining tessellation
limitation; the generated maps use roads wider than their terrain cells.
