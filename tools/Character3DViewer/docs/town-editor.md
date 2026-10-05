# Maps, travel and Town Editor

[Start here](../README.md) · [Build and checks](build-and-validation.md) · [Limits and issues](status.md)

## Current maps and authority

The **Sin Star I** group starts with **Neris Metropolis**. There are 17 permanent
map names and at most 17 open town documents / World nodes, verified in
`TownLibrary.PERMANENT_COUNT`, `TownTabs.CAPACITY` and `TownWorldDocument.MAX_NODES`.
The [Metropolis package](../../../games/SinStarI/SourceAssets/Towns/Neris/NerisMetropolisV1/README.md),
[StoryTownsV1](../../../games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/README.md)
and [Horizon package](../../../games/SinStarI/SourceAssets/Towns/Neris/NerisHorizonV1/README.md)
own layouts, templates and authored assets. Those links use the local compatibility
junction into the independent game checkout. Saved editor documents remain
authoritative; old Blender layouts and fixed camera coordinates do not replace them.

**Maps** opens the thumbnail gallery; double-click a card to enter. Each map has
independent saved Demo participation and Refresh; Refresh All regenerates images.
Missing thumbnails prepare incrementally and the chooser restores the original map
when closed. Photographs use the saved initial perspective camera, exclude editor
controls, and persist as checked image/input pairs. Refresh after changing external
models without changing the map document. A failed cache write retains the previous
published generation and never discards map edits.

Demo starts with map inspection, stops on ordinary input and can be toggled explicitly.
Enabling it continues from the displayed camera. Two minutes without input can
restart it; editing, dialogs and conversations postpone that restart. The seven
wilderness/Relief maps use 15-second visits, other maps 30 seconds. Day/night changes
at one-third and two-thirds without overwriting saved lighting. Participation is
per map; refer to `TownDemo.smile` for defaults instead of an old fixed list.

## Camera and travel

Each map, floor and grid share bounds and center. Initial/reset overview uses the
authored camera when present and otherwise fits the actual bounds. **O / Orbit**
preserves the displayed eye, direction and lens while choosing the viewport-center
world point as pivot. Right-click restores that map's initial overview and orbit.
Follow Party manual orbit keeps the leader pivot without recentering the shot.

| Control | Town action |
| --- | --- |
| WASD | Move party; move camera with Fly Inspect enabled |
| Tab / Ctrl+Tab | Ease behind party / cycle Arin, Orin, Zara, Mira as leader |
| Left drag / middle drag | Pan / orbit; editing owns its active gestures |
| Shift + middle drag | Pan, retaining the gesture until release |
| Wheel / Up and Down | Eased distance zoom; 1x fits the map |
| Left and Right | Orbit while held |
| Space | Pause/resume orbit; in Fly Inspect ascend, Shift+Space descend |
| R | Walk/run outside editing; next cardinal view while editing or in top view |
| 1 / 2 | North-up top view / perspective front view |
| F / G / B | Floor / grid / shared background |
| M / Ctrl+M / Ctrl+F | Minimap / statistics / leader POV |
| Ctrl+click outside editor | Relocate party to nearest walkable road |

Town zoom uses 10% wheel steps, bounded targets and immediate reversal; old wheel
acceleration wording is historical. True 2D uses orthographic projection while
editing. Middle orbit returns to 3D. **Set Initial Orbit Camera** stores the current
3D eye, target and lens, participates in Undo, and travels with saves. Camera values
are map-specific. Manual Fly Inspect moves no party actors. Alt has no camera binding.

Click a connected minimap road to travel; Shift+click appends up to 64 numbered
stops. A normal click replaces the queue. Left-drag or Shift+middle-drag pans the
minimap, and dragging never starts a journey. Wheel/plus/minus zoom 1x-8x around
the pointer. A cyan line is the confirmed route; yellow/numbered markers show stops.
Manual movement, editing, map changes or a blocked leg cancel travel.

Routing is distance-weighted **A*** on the collision-validated four-neighbor road
grid, including unequal tile dimensions (`TownRoadSearch`), with Manhattan lower
bound and indexed priority queue. It is shortest on that graph, not a globally
shortest continuous terrain path. Connected routes are confirmed before movement.
Road preparation yields around 2 ms; search around 3 ms. Prepared local connections
persist with checked surface/placement identity, not all origin/destination routes.

The leader POV below the minimap is independent of the main view, remains for six
seconds after movement, and defaults to roughly 12 refreshes/second. High FPS
refreshes every frame with extra CPU/GPU cost; hidden POV skips rendering and replay.
The current lighting path selects lamps after each view accepts its own camera.

## World Map and arrivals

World Map reopens the last `.world` atlas. First reopen after restart frames all
cards; subsequent reopens retain pan/zoom. Add open towns or checked `.town` files,
drag cards to arrange them, and save/open atlases through native dialogs.
**Connections are derived only from yellow Map Load areas.** Graph lines cannot
create/delete destinations, and opening an atlas never adds roads or town content.
`TownWorldTravel` reads unsaved open documents and ignores historical cached links.

In **Edit Town > Items > Surfaces > Map Load (Yellow Border)** draw an area and
choose another open town. Select/Move, Resize, Reassign and Delete work on complete
connected areas; separate islands stay independent. Undo restores the operation.
Each gate has an inward arrival circle. **Teleport Spawn (Blue)** sets the separate
direct-entry point on walkable ground; Delete removes it. Direct entry falls back
to an inward gate circle, while walking between maps uses a matching destination
gate. Blue guides are editor-only.

## Editing

Open **Edit Town**. Items contains Buildings, Surfaces, Decor and NPCs. Place Copy,
Select/Move, group selection, Ctrl-add, 15-degree rotation, absolute decimal Angle,
Delete and Ctrl+D duplication use existing authored assets. Clicking the selected
palette category again enters Pan Mode. Escape cancels the active gesture.
Ctrl+Z and Ctrl+Y / Ctrl+Shift+Z provide up to twenty edits; a complete drag/stroke
is one edit. Resource limits reject a group before partial duplication.

Surfaces offers Line, Rectangle, Circle/Ring, Filled Circle, Triangle, Curve and
Edit Shape Points for Ground/Road/Water. Width is metres; road over water creates
a bridge. Three-point shapes commit after the third point, handle edits on release.
Paint Terrain Styles applies Meadow, Forest, Highland, Desert or Snow locally;
Erase Local Style restores the map default. Whole Map cycles the default and clears
overrides; Undo restores both. Style does not change walkability.

Terrain offers Raise/Lower, Flatten/Sample Height and Smooth. Radius is metres,
strength metres/second. Protected structures/actors reject intersecting edits.
Ramp freezes two anchor heights for review before Apply; defaults are 6 m width,
2 m shoulders and a 30-degree walking limit. Both party and picking use the same
triangular terrain; gentle centerlines do not excuse steep shoulders.
Water Line drapes 2 cm above the bed, supports width, 0-4x speed, reversal and erase,
and warns about uphill portions. At most three direction/speed groups fit a map.
This is authored flow, not fluid simulation or a free-falling curtain. See
[terrain elevation](../../../docs/architecture/terrain-elevation.md).

Items > NPCs lists nine prototype residents. Place/Move chooses walkable ground,
Face Point sets facing, and rotation/Delete/Undo work normally. Exact grounded
positions (including an intentionally empty layout) save in TWN16. Prepared
residents load one per frame; old defaults resolve incrementally. Click a resident
or press E nearby to converse; E/Enter/Escape closes. Manual travel cancels approach.
The [resident package](../../../games/SinStarI/SourceAssets/Characters/Civilians/NerisResidentsV1/README.md)
owns visual and behavior limitations.

Sun controls and Save As Day/Night presets belong to each town. New maps default
to **Night Intensity 112%** (`TownDocument.NIGHT_INTENSITY`); saved overrides remain
editable. All maps share dark rippled nighttime water reflections; Metropolis also
reflects in daylight. Flowing water retains its motion and daytime colors.
The eight nearest campfires and eight nearest fountains animate within shared
budgets; do not infer unlimited effect capacity from repeated placements.

Royal Court has Select/Move, Place/Relocate and Delete; one is allowed per map.
Doors, bridge, fountain, lights and collision move together; independent terrain
does not. Bulk Move/Copy Sections transfers whole intersecting items, terrain and
Map Load tiles, replacing the destination with one undoable operation. It waits
for replacement terrain and rolls back on failure. Court can move but cannot be
copied. Protected airports and unsupported curved/elevated/directed-water section
transfers remain limitations; do not flatten or silently drop data to transfer them.

Guides are separate `.guide` files: up to 128 named line/rectangle markers, selection,
group move/duplicate, eight resize handles, and a scrollable Guide List. Uniform
Guide Grid accepts 0.001-10,000 m; Actual Tile Grid shows nonuniform authored cells
or filtered object footprints. Snap cycles Off/Grid/Markers/Grid + Markers.
Hide Guides retains markers. Save/Load Guides preserves grid/filter/snap/visibility;
load validates before replacement and Undo restores prior markers. Guides never
enter `.town` or Blender saves; tab/document changes clear temporary guides/history.

## Files, exports and recovery

| Files command | Result |
| --- | --- |
| Save for Viewer | Chosen `.town` plus same-name 384 x 240 `.png` from matching initial-camera revision |
| Save As | New name/path; saved copy opens a separate tab |
| Open for Viewer | Native `.town` picker; checked import opens a tab |
| Recent Versions | Last ten exported versions for the selected town |
| Save All for Viewer / Blender | Freeze all open maps, choose first file, process unique names sequentially in that folder |
| Save for Blender / Open Blender | Verified `.blend` conversion / compatible embedded-layout import |
| Update Permanent Map | Explicitly replace the selected startup map after durable save |
| Open Recovery / Show Last File In Explorer | Recover a named working copy / reveal last successful output |

Viewer transfers run inside the native app without a helper or Blender dependency.
Suggested names use local `YYYY-MM-DD HHmm - Town Name`. Persistent progress reaches
100% only on verified success; errors retain edits. Single successful saves reveal
the exact file in Explorer; batch completion avoids one Explorer window per map.

Queues freeze clicked documents and matching photographs. Later edits, replacement
of both photo generations or live-cache eviction must not redirect an accepted pair.
Save As retains source-photo identity. Only the clicked revision can be marked
saved; later edits stay dirty. One failed map does not discard the remaining queue.
Cancellation releases pending snapshots while an already-started transfer owns its
copy. A PNG failure after `.town` success reports partial success and retains the
town file. Blender-only export has no photograph prerequisite. See
[Viewer export guide](viewer-export.md) for established warm-photo retention and
the proposed cold-photo path. [R04 status](status.md#viewer-export-r04) records its
uncommitted implementation and failing native acceptance; it is not a completed feature.

Prepared portable files include checked terrain recipes, road connections and
resident data; legacy files remain readable and rebuild missing/stale preparation
incrementally. GPU resources and current journey search state are not persisted.
The authored document is limited to 512 KiB; excessive complexity fails explicitly.
TWN11-16 add terrain/flow, local styles, snow, arrival metadata, initial camera and
NPC layout as needed; do not downgrade formats to bypass validation.

Permanent updates preserve the prior destination as `<name> Before Permanent Update`,
retain camera/party position, and switch only after successful persistence. A
different source name requires typing `Yes` exactly. Additional tabs can close with
right-click and retain named recovery; permanent tabs stay. Duplicate names receive
suffixes. The 17-tab bound also applies when opening copies; no extra slot is implied.

Portable versions live at chosen paths. Reused names retain numbered siblings;
retention deletes only indexed files whose checksums still match. Externally edited
files survive. Autosave/recovery uses Windows' actual **Saved Games** known folder,
resolved by `scripts/get-smile-data-root.ps1`, under
`SMILE 2.0/Games/<ApplicationId hash>/Data`. Existing primary/backup wins over legacy
imports. Never choose conflicting stores by timestamp or replace saves from defaults.

Blender conversion uses installed `bpy`, immutable templates and a frozen document;
it writes a temporary file, reopens/verifies it, then replaces the chosen destination.
It does not mutate an open Blender scene or two-way-sync later changes. Prepared
native triangles preserve terrain/flow metadata but not Studio's animated water shader.
Compatible imports support assembly move/upright rotation/positive scale/deletion;
arbitrary meshes, material edits and direct Blender surface edits are not imported.

The shared [town-authoring contract](../../../docs/architecture/town-authoring.md)
owns persistence and module boundaries, with a focused
[surface layering policy](../../../docs/architecture/town-surface-layering.md). Its
linked archives retain old tab counts, graph editing and provisional landmarks.
Consult [ARCHITECTURE](../ARCHITECTURE.md)
for current owners and [historical evidence](archive/2026-09.md#native-neris-town)
for old layouts, coordinates and one-time measurements.
