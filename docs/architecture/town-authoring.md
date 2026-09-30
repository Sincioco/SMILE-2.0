# Native World and Town Authoring

Current September 30 controls and ownership are documented in the Character Viewer
README and ARCHITECTURE. These supersede the historical milestone descriptions below.
Guide files now support names, list operations and resize handles; exact orthographic
editing shares projection/picking with the renderer. World cards use generated plan
thumbnails, with independent pan/zoom. No change to the `.town` or `.world` format is
required for these presentation changes.

## Surface layering policy (September 30)

`tools/Character3DViewer/TownSurfaceLayers.json` is the shared numeric authority.
Asset preparation projects it into `TownSurfaceLayers.smile`; Blender reads the
same profile. Tests reject a stale projection. Do not add independent surface
height literals to render, navigation, picking, or export owners.

| Layer | Native elevation | Rule |
| --- | ---: | --- |
| Submerged bed and skirts | 20.70 | Below every visible surface |
| Ground | 20.90 | Below authored building foundations |
| Water | 21.85 | One surface per painted cell; submerged bed receives shadows |
| Road and painted bridge paving | 23.12 | Shared physical and walking elevation |
| Movable structural deck | Road + 2.00 | Its lowest exposed recessed top must clear the road by at least 0.60 |
| Guides and Map Load borders | Projected from road level | Draw once as 2D overlays after the scene; no competing coplanar mesh |

Ten native units equal one metre. The legacy ground walking baseline remains
22.50 to preserve the accepted actor grounding; rendered structural deck changes
must also change their navigation height. Skirt tops stop at their owning surface.
Painted surface kinds replace one another in the grid instead of stacking full
duplicate planes. Existing civic bridge supports remain below their paving.

New imported decks must be measured against this profile, including recesses,
trim and authored scale. Shift the coherent assembly and its walking surface,
or omit hidden substrate; never alter the user's road layout to hide overlap.
Do not solve solid-surface conflicts by draw order or transparent materials.
Retain the camera's scene-relative depth range. Decorative overlays may use a
controlled overlay pass; they must not change collision or walking heights.

This policy prevents the demonstrated terrain/deck conflicts. It cannot make
arbitrary intersecting imported meshes valid automatically; those still need
an explicit support/overlap rule when added. Geometry regressions verify native
and Blender elevations, all drawbridge planks and recessed support, and unchanged
navigation across the bridge and gatehouse.

## Current September 29 update

The active native application is **SMILE 2.0 - Sin Star I - Game Engine and Studio**,
conversationally Sin Star Studio. The former separate Studio concept is abandoned
for now; Web remains indefinitely paused. Current owners and checks are listed at
the top of tools/Character3DViewer/ARCHITECTURE.md; earlier descriptions below are
historical where they conflict with this section.

- `.town` transfers run in-process through the native data-file API. The external
  worker handles Blender conversion only. A save writes its immutable clicked
  revision; subsequent edits and other open maps retain their own recovery state.
  Progress and errors are nonmodal. Only verified completion displays 100%.
- `.guide` files independently store up to 128 markers, fractional grid spacing,
  tile filter, snapping and visibility. They never enter `.town` or `.blend`.
  Marker selection/duplication/movement participates in undo.
- Guide Grid accepts 0.001–10,000 metres. Actual Tile Grid exposes original cell
  boundaries or object footprints by selected type. Imported cells remain nonuniform;
  uniform macro alignment is supplied by Guide Grid. Surface painting defaults to
  Rectangle. Editor-only R rotates the view clockwise 90 degrees.
- TWN4 adds custom Day/Night presets and retains TWN1–3 reading. TWN3 stores Map
  Load cells as destination metadata independent of the visible ground material.
  Map-load triggers and minimap road-only journeys have separate state owners.
- Minimap road clicks mark a destination and request incremental connected-road
  routing, then follow the party. Manual movement or a map/document change cancels it.
- Native night lighting admits 128 local sources; copied lamps rebuild that list.
- Full district Move/Copy remains unfinished. SurfaceSections3D supplies terrain
  transfer only; editor integration and fixed landmark ownership are still required.

The complete native session and guides/file tests passed. Actual save/load of a
`.guide`, all three map tabs, and the restored main publication were inspected.
The latest native build and installed VSIX include these changes. Very short editor
windows can still crowd controls; the initial window is 1440 × 960.

## Scope and implemented workflow

The native Character Viewer now edits Neris, creates blank environments, and saves
linked world maps. This is the September 26 request; Studio and Web adoption stay
on hold. The pre-editor camera checkpoint is `227f39f`.

`Edit` opens a right palette. Buildings, Surfaces and Decor are exclusive categories.
Select a palette item and click the ground to place a shared instance. Select/Move
supports ground-plane dragging, Ctrl additive selection, empty-space rubber-band
selection, group movement, 15-degree rotation, Delete and Esc cancellation. Vertical
placement and scale remain authored values. Shift+left drag pans; middle drag orbits;
wheel zooms; WASD/arrows inspect. Tab leaves editing and enables party control.

Surfaces provide Ground, Road and Water with two-click Line and Rectangle tools.
Line width is in metres, including the exact nonuniform Neris grid. Erase restores
grass inside the rectangle. Road over water becomes a bridge. Shorelines have no
thin trim; bridge rails stop at the water crossing.
Pending surfaces replace the live geometry only after the complete build succeeds.

Undo and Redo buttons are available on every editor tab. Ctrl+Z undoes; Ctrl+Y
or Ctrl+Shift+Z redoes. The session retains twenty completed edits, including
placement, grouped movement/rotation/deletion/duplication, surfaces, lighting,
marker creation and marker clearing. A drag is one edit; Esc cancels its preview.
A new edit after undo clears the redo branch. Opening a different town clears
history and markers. Saving does not clear history, but undo never changes the
current save name or rewinds its save status; save again after undoing town edits.

Select one or more items and use Duplicate Selected or Ctrl+D. Copies retain the
original template, height, scale and rotation, get new identities, and become the
selection. They appear one grid interval to the right, ready to drag into place.
Resource limits are checked for the entire group before creating any copies.

Guides provides temporary two-click Marker Line and Rectangle tools, Clear All
Markers, Show Grid, 1-20 metre grid spacing, and Snap: Off / Grid / Markers /
Grid + Markers. Guides follow the town ground as the camera moves. Snapping
preserves height and group spacing, using the first selected assembly's origin.
Nearby marker endpoints/corners take priority over segments, then grid snapping.
Distant grid lines are thinned for readability; the actual snap interval stays
unchanged. Guides, grid preferences and history are session-only: they are excluded
from Viewer saves, recovery files and Blender exports. Up to 128 guides are kept.

Sun exposes RGB, intensity, ambient strength, azimuth, elevation, day/night presets
and shadow toggle through the existing Scene3D light/shadow owner. Neris buildings
and trees submit geometry to the same shadow pass.
The seven numeric Sun controls use draggable tracks with live previews and a
one-unit wheel adjustment. Release commits one undo step; Esc cancels a drag.
Active slider capture prevents camera gestures even after leaving the panel.
Raising Ambient lightens dark shadows; native edge diffusion uses a 5x5 weighted
shadow filter independently of that contrast setting.
Grass, courtyard lawn and paving use the same PBR lighting response. Flat terrain
receives scenery shadows but does not cast into the town-sized shadow map: its
self-shadow sampling previously produced curved bands that looked like a grass
texture pattern. Building/tree/bridge-rail casters remain enabled.
`TownDocument.Light` is part of the existing town payload, including RGB, intensity,
ambient, both direction angles and the Shadows toggle. Viewer files and version
copies retain that payload; Blender writes it into its embedded town document
and creates the corresponding Sun/world light. Reopening either format restores
the town's authored settings, independently of other town tabs.

## Ownership and state

| Owner | Responsibility |
| --- | --- |
| `catalog_scene`, `export_catalog`, `catalog_native`, `catalog_features`, `catalog_feature_native` | Saved Blender assemblies, local geometry/features and generated native tables |
| `TownDocument` | Caller-owned identities, horizontal transforms, surface and sun; no GPU handles |
| Shared `SurfaceGrid3D` | Packed cells, physical line/rectangle painting and exterior-bank predicates |
| `TownSelection`, `TownPicking`, `TownEditor` | Category selection, cancelable group gestures, projection and commands |
| `TownGuides`, `TownHistory` | Caller-owned temporary construction drawing/snapping and bounded edit snapshots; no persistence |
| `TownEditorPanel` | Palette/control hit rectangles, one thumbnail atlas and progress; no persistence |
| `TownSunControls` | Sun row values/ranges, slider presentation and cancelable live preview using shared UI drag capture |
| `TownCatalogRenderer`, `TownSurfaceRenderer`, `TownAttachments`, `TownLighting` | Shared-template submission, incremental surfaces, door/water attachments and light application |
| `TownGeometry`, `TownDocumentNavigation` | Template transforms and a committed movement/camera snapshot |
| `TownDocumentMap` | Cached edited minimap, labels and current leader position |
| `TownDocumentStore`, `TownLibrary` | Bounded transactional codecs, named saves and working-copy recovery |
| `TownWorldDocument`, `TownWorldEditor` | Sixteen saved town references, draggable map nodes and bidirectional links |
| `TownEditorSession` | Active document/renderer lifecycle, tab and transfer coordination |
| `Watch-TownSaves`, `town_document_codec`, `town_blender_save` | Launcher-owned background Blender job and atomic verified result |
| `NerisTown` | Delegated input/update/draw/lifecycle alongside the existing party and camera |

World links are authoring data and the map can open a linked town in the editor.
They do not implement general overworld travel, encounters or a new RPG runtime.
NerisTownConnections now provides two explicit walking gates between Neris Town
and Neris Spaceport. TownEditorSession.TravelTo switches the named document and
applies the arrival after its terrain is ready. This does not make arbitrary
world-editor links traversable. There is
no additional library/project dependency. Existing Scene3D, Precision3D, native
Save Data and Character3D remain the runtime owners.

## Catalog, coordinates and resource budget

The immutable `NerisTownV1/Authoring/Catalog.blend` is the checksum-verified template
source. Save For Blender writes only the path chosen in the Windows dialog.
The live interactive Blender session is never saved, closed or replaced by the worker.
Catalog export creates 358 initial instances, 35 templates, 337 parts and 30 models,
plus 14 reused Old Castle models. Template GLBs are shared, never copied per tree
or placed house. Blender XYZ maps to native XZY at ten units/metre and height +21.
Original dimensions, calibrations and character asset identities are preserved.
The Decor palette exposes 11 choices: repeated fountain and crystal-circle copies
are hidden by generated presentation metadata. Their template IDs, geometry,
catalog fingerprint, placed items and saved-file formats remain unchanged. The
two distinct lettered signs are labeled Civic Plaza Sign and Market Walk Sign.

The native renderer admits 512 meshes and 4,096 submissions; measured catalog demand
is 2,081 submissions before surfaces/attachments/actors. The nine-bit mesh slot
encoding matches the pool. Placement is capped at 1,024 items, 3,500 estimated item
submissions and 160 attached water disks, leaving room for terrain and party.
Full editable Neris with its actual party measured 392 meshes and 374 materials.
Terrain permits 32 mesh batches, four water ribbons and 32,768 merged patches;
a rejected terrain change restores the previous surface. Existing static town
resources are released before the editable catalog loads. No dual scene remains live.

Neris keeps its exact 421 by 414 edge grid; blank environments are 128 by 128 cells
at two metres per cell. The reusable grid supports at most 512 by 512. Sixteen
base-eight cells pack into each Number. Generated tables contain data only.
Template navigation follows moved/rotated stairs, doors and solid bounds. Camera
volumes refresh after edits/movement using a bounded nearby 512-box snapshot.

Editable lawns retain the original Blender -0.01 m surface (native Y=20.9), below
the Royal Castle and Military HQ floors. The earlier +0.15 m lawn overlapped those
floors and flickered during camera motion. Roads/bridges retain their +0.212 m
height, and water remains +0.085 m. This is a terrain-render/save correction, not
a change to building transforms, saved documents, or navigation clearance.

## Saves and background work

Save For Viewer and Save For Blender use Windows file dialogs for explicit `.town`
and `.blend` destinations. Save As prompts for a new town name and opens a copy in
another tab. Viewer saves keep the latest ten indexed versions per town, using the
local dated filename suggestion. Open For Viewer lists these versions newest first;
Open As browses other town files. File transfer runs in the launcher-owned worker.
Blender conversion reconstructs assemblies, surfaces and light from the immutable
catalog and editor snapshot, verifies a temporary `.blend`, then replaces the chosen
file. It embeds the document and packs the current textures. No implicit canonical
Blender destination is used. Names contain 1-80 Unicode scalar values without
control characters. Native recovery remains separate from these portable files.

Working recovery is saved after edits settle and on view changes. Opening another
town or creating a blank one first preserves the current working copy. Explicit
saves and recovery remain separate. A failed decode leaves the current document
unchanged. Files use TWN1/linked-world formats inside the checksummed native SMD4
envelope; town documents carry the catalog fingerprint and reject mismatches.

Launch through `Launch.ps1` for the hidden process-bound Blender worker. Its log is
`town-blender-worker.log` in this application's Save Data directory. Progress and
errors appear in the panel. Surface work advances in small frame batches and swaps
atomically. A Blender save snapshots the requested revision; subsequent edits remain
in the working copy and another open town is never renamed by that job's completion.

## Shared capability changes and defect fixes

- `Text_Prompt` supplies an owner-modal native Unicode name prompt; movement input
  is consumed while it is open. `Text_From_Code` enables scalar-safe saved names.
- `KEY_SHIFT` and `KEY_DELETE` are shared held/event constants, values 43 and 44.
- `Precision3D.SetMeshVertex` submits fractional coordinates to an existing mesh.
- Native `ByRef` record parameters now reserve one address, not the entire record.
  The former allocation caused a stack overflow in nested town/inspection calls.
  The fix preserves value parameters and ownership cleanup.
- Source parity was maintained for the shared Web built-ins; no editor Web build,
  publication or browser acceptance was performed.

## Validation and practical limits

`scripts/test-town-editor.ps1` checks packed surfaces, physical-width lines, connected
banks, exclusive selection, cancelable group transforms, Unicode/precise saves,
malformed-document preservation and linked worlds. It reuses the existing Neris
route assertions against the editable document: Royal Castle entrance, City Hall
crossings, stairs and canals. Rendering checks cover all 512 mesh slots, fractional
vertices, incremental rebuilds, actual party/calibration, Tab/inspection/zoom and
resource cleanup. `scripts/test-town-blender-save.py` checks a real named Blender
round trip, duplicate protection, save-back and the matching Viewer snapshot on
isolated paths while verifying the canonical scene is unchanged.

The native hardening/architecture gate and language/compiler/text suites remain
applicable. A compiler regression checks small ByRef stack frames; the full town
session reproduces the previous overflow. Camera gesture checks cover walking
pan/orbit, one-second return and preserved zoom, with no injected user input.

This is an instance/surface editor, not a mesh modeler. No vertical/scale authoring,
catalog migration or automatic placement collision avoidance is
included. Decorative falling leaves and crystal billboard halos from the static
showcase are not reproduced in editable mode; catalog materials, fountain water,
opening doors, stairs and navigation are retained. The old one-shot doorway event
API has no current edited-scene consumer; interior scene transitions remain outside
this authoring slice. Very dense custom scenes can reach the documented budgets.

### September 26 validation result

Native solution and Viewer builds passed. The compiler suite passed 326 checks;
native Text passed 46 checks plus the prompt fixture; calibration passed 49;
graphics/pointer/audio passed 58. Foundations, edited routes, real rendering,
full editable session and static Neris scene checks passed. Full-session checks
include category switching, moving camera gestures, preserved zoom, clean release
and centered arrival when reopening the edited town. Blender acceptance passed
with packed paving texture, and the worker failure response checksum/identity was
verified. The right palette and complete scene were visually inspected in an
isolated native preview without injecting mouse/keyboard input. The refreshed
VSIX payload was installed and hash-verified. Manual acceptance of the editing
workflow remains available to Sin after the release relaunch.

`scripts/test-town-terrain.py` runs in background Blender without saving the
catalog. It measures actual castle/HQ floor faces, checks at least 0.1 m clearance
from generated lawns, verifies native/Blender height parity and unchanged road/water
heights, and requires bare shores without removing bridge rails. The native
rendering fixture also checks that bare shores produce no extra trim mesh.

September 27 follow-through: the native editor foundations, routes, rendering and
full session pass with twenty-step undo/redo, temporary guides, snapping and
duplication. Native interaction checked grid visibility, two-click rectangles,
clear/undo/redo, selected-object duplication and undo, and Fit during editing.
The bridge depth regression fails with the former fixed near plane and passes
with height-based near clipping; Royal/HQ bridges were inspected at multiple
overview angles and zoom levels. The release Viewer was rebuilt and relaunched.

## Native files and tab ownership (September 28)

`TownTabs` owns up to eight document snapshots with Neris permanently at index zero.
`TownFileJobs` owns one immutable pending save/open snapshot, origin name/revision,
request identity, recent-version results and UI progress. It uses the existing
`TownDocumentStore` codec and `TownLibrary` recovery rules. The session delegates
file transfer and tab operations; it does not implement filesystem or Blender work.
`TownEditorPanel` owns/releases one shared thumbnail atlas generated from actual
catalog assemblies. The static catalog fingerprint and user layouts are unchanged.

`File_Pick` is a shared native path-only primitive backed by Windows common dialogs.
`Watch-TownSaves.ps1` owns the Viewer-bound background worker. `TownFileWorker.ps1`
validates bounded UTF-32 job metadata and checksummed envelopes; `TownVersionFiles.ps1`
owns date-ordered retention and its local index. `town_blender_files.py` reuses the
existing assembly/terrain/light builder for explicit paths and validated imports.
The portable `.town` format remains the original checksummed document envelope.
See the Viewer README's file-command table for current user behavior; these dialogs
supersede the earlier name-only save controls and implicit Blender destination.

The live Town Editor remains authoritative. A Blender import only occurs after an
explicit Open command; the worker never reloads the old canonical `.blend` over live
edits. New exports include assembly anchors and normalized document checksums.
Whole-assembly transforms/deletions round-trip; arbitrary model/material edits and
direct Blender terrain edits are outside this importer. Previous editor exports
restore their saved metadata and whole-assembly deletions. Additional tabs preserve
working copies on close. Opening/switching documents resets markers/history.

`ArenaViewport3D.MaximumPitch` opts the editor into a safe near-vertical pole limit;
other callers retain the existing limit. Neris owns the `1` command and its camera
mode transition. Ordinary pan/zoom/orbit still belongs to shared camera modules.
The lawn texture generator removes directional periodic waves. Catalog lawn parts
use the same current grass texture/material as editable terrain. An opaque submerged
water bed overlaps banks below lawn height, sealing exposed seams and receiving
building shadows through water transmission. It adds no raised shoreline trim.
Water Fresnel reflection preserves reflected colors while its roughness attenuates
mirror strength. Reflections remain a bounded screen-space effect, with a sky fallback
for offscreen geometry. Exposed road/water sides now close down to the bed; this
also prevents the background showing through at grazing camera angles.

Town sun vectors point from the surface toward the sun, matching the native PBR
and shadow-camera contract. The former downward vector placed the shadow camera
under the ground. Town-scale shadow bias is explicit. Simple-material shadow
filtering now uses the shadow-map texel size, matching PBR filtering. Grass and
paving load mip chains with anisotropic filtering. Simple grass uses Data pixels
because its shader performs color decode; PBR paving uses a Color texture. Flat
stone faces replace the periodic grain that produced visible wavy bands. Subtle
tile joints remain, with nonmetallic 38% roughness for a soft sheen in both the
Viewer and future Blender saves.

Validation: the native editor foundation/route/render/session fixtures cover category
release, top-down input, tab retention/permanent Neris, submerged shore geometry and
resource cleanup. `test-town-version-files.ps1` covers ten-version retention,
same-minute saves, order, checksum rejection and externally modified-file protection.
`test-town-file-roundtrip.py` checks a real full-scene Blender save/reopen, metadata,
whole-assembly movement/deletion and terrain/light retention on isolated paths.
Native UI acceptance verified category deselection/pan, top-down view, thumbnail
placement choices, Windows Save/Open dialogs, dated Viewer saving/recent ordering,
Blender save/reopen in a new tab, and right-click closure with permanent Neris.
Compiler checks passed 327, graphics/input/audio checks passed 58, and native
Viewer hardening/architecture passed. The refreshed VSIX payload was installed
and hash-verified. Terrain checks measure closed edges below their surface heights,
castle/HQ/bridge clearances and native/Blender height parity.
No Studio implementation or Web/browser acceptance is part of this change.
