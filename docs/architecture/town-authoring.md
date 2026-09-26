# Native World and Town Authoring

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

## Ownership and state

| Owner | Responsibility |
| --- | --- |
| `catalog_scene`, `export_catalog`, `catalog_native`, `catalog_features`, `catalog_feature_native` | Saved Blender assemblies, local geometry/features and generated native tables |
| `TownDocument` | Caller-owned identities, horizontal transforms, surface and sun; no GPU handles |
| Shared `SurfaceGrid3D` | Packed cells, physical line/rectangle painting and exterior-bank predicates |
| `TownSelection`, `TownPicking`, `TownEditor` | Category selection, cancelable group gestures, projection and commands |
| `TownGuides`, `TownHistory` | Caller-owned temporary construction drawing/snapping and bounded edit snapshots; no persistence |
| `TownEditorPanel` | Palette/control hit rectangles and progress; no persistence or GPU ownership |
| `TownCatalogRenderer`, `TownSurfaceRenderer`, `TownAttachments`, `TownLighting` | Shared-template submission, incremental surfaces, door/water attachments and light application |
| `TownGeometry`, `TownDocumentNavigation` | Template transforms and a committed movement/camera snapshot |
| `TownDocumentMap` | Cached edited minimap, labels and current leader position |
| `TownDocumentStore`, `TownLibrary` | Bounded transactional codecs, named saves and working-copy recovery |
| `TownWorldDocument`, `TownWorldEditor` | Sixteen saved town references, draggable map nodes and bidirectional links |
| `TownEditorSession` | One private active document, pending explicit save snapshot and lifecycle coordination |
| `Watch-TownSaves`, `town_document_codec`, `town_blender_save` | Launcher-owned background Blender job and atomic verified result |
| `NerisTown` | Delegated input/update/draw/lifecycle alongside the existing party and camera |

World links are authoring data and the map can open a linked town in the editor.
They do not implement overworld travel, encounters or a new RPG runtime. There is
no additional library/project dependency. Existing Scene3D, Precision3D, native
Save Data and Character3D remain the runtime owners.

## Catalog, coordinates and resource budget

The immutable `NerisTownV1/Authoring/Catalog.blend` is the checksum-verified template
source. Current `Blend/Neris-Town-Waterfront.blend` is the explicit save-back target.
The live interactive Blender session is never saved, closed or replaced by the worker.
Catalog export creates 358 initial instances, 35 templates, 337 parts and 30 models,
plus 14 reused Old Castle models. Template GLBs are shared, never copied per tree
or placed house. Blender XYZ maps to native XZY at ten units/metre and height +21.
Original dimensions, calibrations and character asset identities are preserved.

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

Save For Viewer uses application-owned atomic Save Data. Save To Blender writes a
structured request; Save New Version prompts for a unique name and produces both
Viewer and Blender snapshots. Names contain 1–80 Unicode scalar values without
control characters. Filesystem names are sanitized and hashed under the town root.
Duplicate named versions are refused. Save-back opens the immutable template,
reconstructs transformed assemblies/surfaces/light, saves a temporary `.blend`,
reopens and verifies it, then replaces the destination. The saved Blender file
contains the document JSON and packed paving texture. The worker also writes the
matching named Viewer snapshot even if the Viewer closes before it finishes.

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
