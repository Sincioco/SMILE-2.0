# Character Viewer Architecture

## October 1 Crown Isles palm refinement

The capital layout author owns the road removal, two curved base bridges and
increased houses/trees/lamps. The canonical portable map is prepared again through
the existing native save owner. All 43 entrances and all 50 directed atlas links
pass; fresh import adopts prepared terrain and roads. No runtime behavior or
resource limits change. NerisTownCameraPanel is normalized to UTF-8 so the square
metre label no longer becomes a replacement glyph during compilation.

## October 1 landscape authoring and map revision

SurfacePath3D owns triangle containment and adaptive quadratic Bezier evaluation.
SurfacePaint3D keeps these as forms 7/8 alongside the rounded fillet/crossing forms
5/6. SurfaceGrid3D and the terrain builder continue resolving the same brush stack;
no alternate navigation or rendering authority was introduced. TWN9 stores the
third point only for forms 7/8. TownDerivedCache compares it before adopting saved
preparation. Existing document versions retain their record layout.

TownSurfaceTools owns in-progress points, handle selection and previews. The editor
delegates three-click creation and release-time commits, records one undo revision,
and cancels unfinished gestures on tool changes. TownEditorPanel owns compact
controls that preserve the existing projection commands. SurfacePath3D is 104
lines; SurfaceTools grows by 271, Editor by 55, Store by 38 and Panel by three net
lines. All growth remains within these existing responsibilities; bootstrap,
resource budgets, dependencies and architecture exclusions are unchanged.

Authoring scripts keep plot/access, junction geometry, road-end triggers, portable
preparation and installation separate. Installation preflights every map before
writing, backs up replaced keys and preserves live lighting. Original Neris keeps
its user-authored geometry. Only missing approved atlas links and trigger locations
change there; newer generated-map user edits require a merge instead of overwrite.

Native Foundations, Routes, Rendering and Session checks cover shape persistence,
control gestures and stale preparation. Fourteen saved map bundles freshly import
with valid terrain pages and zero road preparation collision checks. All 50 directed
connections pass, including 24 inward steps clear of triggers. Authoring checks
verify 146 entrances, scenery clearance, symmetry and connected paving. Native
hardening and focused formatting pass. Authored top-down previews were inspected;
live mouse/visual acceptance remains pending after desktop control's interruption.

## October 1: map showcase selection and viewport layout

TownDemo owns the 30-second clock and saved participation keyed by map name.
TownMapPicker owns each thumbnail toggle and hit rectangle; TownTabs selects the
next participating document. The session coordinates tab changes, including an
immediate switch when Demo starts from an excluded map. No map data is changed by
toggling participation. An empty selection starts no load; a single included map
keeps orbiting without reloading itself.

NerisTown delegates screen-to-world picking to TownPicking and cursor-anchored
zoom to NerisTownCamera, retaining its current automatic orbit state. Reset and
top-view commands release the old cursor anchor. TownViewportLayout remains the
shared drawing/hit-testing authority for the toolbar, now aligned right with Edit
Town last. NerisTownCameraPanel measures its own labels and readings to size the
bottom-left statistics panel and avoid active conversation text.

TownResidents owns the relocated starting positions. TownResidentMovement rejects
autonomous errands crossing the 26-metre entrance exclusion area. TownCatalogData
owns preview indices, allowing repeated object instances to share one atlas image;
the renderer-generated palette includes all landforms without duplicate fountain
cells. No bootstrap behavior, capacity, dependency or architecture limit changes.

Validation: native Foundations, Routes, Rendering and Session checks pass, including
saved Demo choices, exclusion/wrap/single-map behavior, 30-second timing, separate
button hit areas, cursor anchoring through moving/paused orbits, and reset cleanup.
The 359-asset build was gracefully installed. Exact mouse/visual acceptance remains
pending because the desktop-control tool stopped for the current turn.

Growth review: NerisTown +14 net lines, Session +13, MapPicker +80, TownDemo +85,
CameraPanel +22 and Tabs +26. These additions remain in their existing responsible
owners; NativeProgram and ViewerWorkflow do not change. A session fixture initially
overflowed its stack by declaring sixteen full map documents locally; its dedicated
test-owned storage now has static duration. Empty failed fixture output now reports
the native exit code instead of hiding it behind a PowerShell null error. The
launcher recognizes repository-staged Studio builds when closing older versions.
The original scene/calibration suite and launcher preservation fixtures pass too.
Inspection tests now tear down the editor directly even in the legacy factory scene,
which intentionally does not route input through editable-town controls.

Tracked diagnostic issue: `Dim Left As Number : Left = 94 : Print Left` compiles,
but reads the direction constant 12. Evidence is the isolated PickerMath native
probe and Syntax.cs's LeftKeyword mapping; the new button coordinate uses ButtonX.
The remaining ViewerParty uses assign 12, so they currently render as intended.
A compiler diagnostic or contextual-name resolution change is deferred from this
layout milestone because it changes shared keyword compatibility. Next action:
add a language regression for declared direction-name reads and select a consistent
parser/semantic diagnostic policy before updating both native and Web generation.


## October 1: minimap gestures, route fidelity and conversation presentation

TownMinimapInput owns press/drag/release classification and capture; it reads a
pointer snapshot without navigation or renderer dependencies. TownDocumentMap owns
bounded world extents and map projection. TownEditorSession delegates the gesture,
pans those extents, and starts a journey only for a click released without dragging.
The existing NerisTown input routing consumes captured drags outside the map too.
TownRoadTravel now walks the same compressed grid bends that its public route
points expose; independent lane-center projections caused the reported hunting at
junctions. Graph validation and the existing final destination behavior remain.

ArenaCamera3D owns wheel-cadence acceleration; ArenaViewport3D keeps per-view state
and retains its existing smooth bounded zoom. NerisTownCamera preserves unfinished
zoom while reanchoring to the cursor. Conversation framing reuses NerisTown's Tab
operation once per conversation; NerisTownParty hides followers only in presentation
and preserves their logical opacity. TownResidentInteraction owns click-to-approach.
TownDemo owns default-on and input-stop policy, with input observed before tab routing.

Focused regressions cover click versus drag, captured Shift-middle drag, pan/world
coordinates, junction lateral wandering, wheel ramp/reset, ongoing conversation
framing, follower visibility restoration, facing and body picking. No dependency,
resource capacity, bootstrap algorithm, architecture exclusion or baseline changes.
The complete native town and shared arena checks pass; final live acceptance and
installation are tracked separately in the task handoff.

Growth review: MinimapInput is 95 lines and ResidentInteraction 136. Existing
coordinators grew by 65 net lines in NerisTown and 66 in TownEditorSession for
delegation and lifecycle; NativeViewerHost adds four input-observation lines.
TownDocumentMap adds 33 lines for its own extents/presentation. NativeProgram and
ViewerWorkflow remain unchanged. Native town tests, 73 shared arena checks,
native hardening, 359-asset publication and the 20-file style check pass. Desktop
control was interrupted before installing the staged build, so these results do
not claim final live gesture or conversation acceptance.

## October 1: click-to-converse and mutual facing

TownResidentInteraction owns character-body ray picking and pending approach state
transitions. The resident population holds the selected index and navigation stamp;
the existing road journey owns pathfinding. Its optional exact endpoint prevents a
conversation destination from being snapped away to a wide road's center. Movement,
map edits and failed journeys cancel the approach; residents wait until it finishes.
TownPicking shares its existing slab intersection with resident picking. NerisTown
only wires the request, road journey and presentation. NerisTownParty turns the leader
through its existing grounded/equipment presentation; no character assets or accepted
calibration were changed. Residents retain their individual model headings.

Focused regressions prove precise approach, dialogue after arrival, manual cancellation,
opposing forward vectors, actual presented actor rotations and body picking in the full
scene. The complete native town suite and 359-asset publication pass. The interaction
owner is below 150 lines; no bootstrap growth, new dependencies or changed limits.

## October 1: immutable save preparation and portable maps

TownSavePreparation owns one clicked revision's cooperative CPU work. It uses a
caller-owned TownTerrainBuilder state and TownRoadGraph state; neither borrows the
live renderer or party navigation. TownCollisionWorld owns the immutable collision
snapshot used by both active navigation and save preparation. TownRoadNetwork is
the small active-game facade. TownSurfaceRenderer still owns staged GPU resources
and atomic presentation, delegating CPU recipe rows to TownTerrainBuilder.

TownFileJobs gates verified save completion on preparation and asynchronous file
transfer. Permanent-map UI updates use the same snapshot pipeline. TownDerivedCache
binds immutable prepared records to exact document inputs and preserves a valid
binding when an imported tab is renamed. New edits cannot adopt older preparation.
No generic shared state bag, reverse dependency into Session, or GPU work was added
to persistence. The road byte codec uses private fixed scratch storage because
Save Data requires a standalone array; graph state itself remains caller-owned.

The native Data_BundleStart operation extends the existing checked file-transfer
job API. A portable map keeps its original SMD4 document followed by an optional
SMB1 checked companion trailer. Imports validate all records before writing to a
fresh staging key, with the main document written last. Old files remain readable;
Python/Blender document readers validate the trailer before extracting the document.
Native bundles are bounded to 64 MiB and 2,048 relative companion keys, with no new
renderer or authored-document capacity. The native-only intrinsic does not add Web
support. The existing data-file runtime owns the worker; no external service is used.

Focused evidence: later live edits remain independent while an earlier save prepares;
renamed imported tabs retain preparation only for matching inputs; native transfers
round-trip companions and preserve the previous export on failure. A full Canals
bundle (2,584,894 bytes) opened in another fresh application identity with every
terrain page readable and zero road collision checks. Python rejected its damaged
trailer. GPU upload and query-specific pathfinding still occur at runtime. Unsaved,
legacy or invalidated input can still require incremental preparation.

Growth review: renderer -317 net lines, active road owner -252, navigation -162;
Session +46 lines of save lifecycle wiring; FileJobs +127/-28 for its existing job
pipeline. The four extracted/preparation owners remain under 400 lines each. Native
bootstrap and ViewerWorkflow are unchanged, with no baseline/exclusion changes.
The final Foundations, Routes, Rendering and Session checks, 359-asset publication,
changed-source formatting and diff checks pass. Studio was gracefully restarted;
its normal Save For Viewer produced a verified prepared Canals bundle and the map
switch measured 482 ms. Existing user maps and calibration were retained.

## October 1: persisted map preparation

TownDerivedCache owns exact input validation and two-generation commit points for
disposable native prepared data. TownTerrainCache owns bounded, checksummed pages
of geometry recipes; it owns no GPU resources. TownSurfaceRenderer retains terrain
generation, staged GPU upload and atomic replacement. TownRoadNetwork persists
its completed local connection graph and validates the complete surface/placement
snapshot before adopting it. Editing invalidates affected data; lighting and marker
changes do not unnecessarily invalidate terrain geometry. Cached recipes preserve
the authored resolution and use the document's existing millionth-unit precision.

SurfacePaint3D resolves ordered paint from the newest covering brush, retaining the
underlying water test for bridges. Its region selection eliminates unrelated brushes
before cell sampling. These reusable surface algorithms remain outside Studio's
coordinators. No compiler extension, dependency, runtime capacity increase or
architecture exclusion was introduced. NativeProgram and ViewerWorkflow are unchanged.

Focused native regressions cover saved multi-batch Canal terrain, invalidation after
painting, ordered region filtering, persisted road reuse and changed-road rejection.
The normal native Foundations, Routes, Rendering and Session checks pass. An isolated
fresh-process run of all fourteen current maps read saved terrain successfully and
performed zero road collision checks. Warm terrain upload/decoding took 9–67 ms and
road adoption 2–17 ms on this machine; these phase measurements are not full scene
load times. Full live measurements belong in the task handoff.

Live tab-switch checks in the installed 359-asset Studio measured all thirteen new
maps at 36–525 ms. The eight reported regressions now read: Spaceport 466, Horizon
210, Canals 525, Star Lake 307, Crown Isles 362, East Valley 131, Orin 94 and
Waterworks 138 ms. These are single observed HUD measurements on Sin's machine,
not a hardware-independent guarantee. The initial original-Neris load after process
startup measured 2,402 ms; that includes first-use resources and remains distinct
from these map switches.

Growth review: DerivedCache 218 lines, TerrainCache 229; renderer +90 net lines,
road-network owner +89, session +8, NerisTown net zero. The new owners do not depend
on the session, UI, renderer or bootstrap. No no-growth baseline or exclusion was
changed. The live tab-wheel check found and corrected camera-input leakage when
the editor was closed; tab/header input now blocks scene zoom in either edit state.

The subsequent immutable-save section above supersedes this milestone's former
portable-export limitation; existing legacy exports need to be saved again.

## October 1: bounded terrain uploads and failed-map recovery

TownSurfaceRenderer now drains its existing 65,536-patch scratch buffer in blocks
while scanning large curved documents. The cleaned Neris Canals document exceeded
that buffer at row 222 of 240. Every new object stays in the existing staging arrays
until the entire map is ready, preserving atomic replacement and the existing
64-object limit. No geometry, runtime capacity, format or library boundary changes.
TownRenderTests loads that real document and checks both completion beyond one
buffer and retention of the previous live revision during incremental uploads.

NerisTown retains the name of the current load attempt. Selecting another town
clears a previous scene failure, and failed scenes keep tab input available.
TownSessionTests exercises arrival with an outgoing failed scene. This is lifecycle
coordination in the existing scene owner; terrain construction stays in its renderer.

## Current: map access, navigation gallery and residents (September 30)

TownMapPicker owns responsive card layout and modal input (161 lines). TownTabs
retains document switching and schedules missing photographs behind that modal;
TownWorldPreviews remains the only photograph cache. The modal is drawn during
incremental loading too. TownWorldCanvas owns the double-click gesture, separate
from drag/port gestures; TownWorldEditor owns save-before-leave and reopening.
The existing session delegates these behaviors. No new serialized UI/GPU state,
compiler grammar, runtime budget or external dependency is introduced.

TownResidents owns nine original-Neris actors (280 lines). TownResidentMovement
owns short road errands (157); TownResidentDialogue owns identities and greeting
presentation (79). NerisTown holds resident state and delegates input, update,
draw and lifetime. Residents are released before another town starts loading.
Movement reads TownDocumentNavigation without sharing the party search's mutable
state. Future quest content can replace the dialogue owner independently.

The procedural NerisResidentsV1 package owns models, palette textures, animation
sources, clip descriptors, grounding measurements, hashes and previews. One palette
material per actor fixes the demonstrated full-town material exhaustion. An isolated
load test was insufficient: the complete live Neris document now loads all nine
using 500 of 512 material slots. Additional unique assets still require budget review.

StoryTownsV1's town_access.py owns authoring-time plot clearance and entrance walks.
It changes generated documents, not runtime/editor placement semantics. Continuous
brush queries, rotated complete model footprints and short clear connections address
the reported obstructions; native reciprocal-road checks still validate travel.
Original Neris geometry and the approved central Relay tower remain intact.

Changed legacy coordinators: NerisTown +35 net lines, TownEditorSession +32,
TownTabs +68; native bootstrap/ViewerWorkflow have no growth. UI algorithms,
resident simulation and map geometry stay in focused owners. Manual ownership/diff
review found no new dependency cycle or generic shared state bag. No architecture
baseline/exclusion was changed; there is no separate automated architecture check
for these owners. Focused native validation and live acceptance are tracked in the
current task handoff; a build alone does not establish visual correctness.

Validation passes: the 359-asset native publication, Town Editor Foundations,
original/edited route regressions, renderer fixture, complete town session,
thirteen-map footprint/entry checks and fifty reciprocal road connections. The
session compares fully loaded resident populations before/after airport travel;
its decoration paging expectations include the four existing landforms. An ad hoc
rerun against the fixture's mutated saves was invalid; the normal test script
creates a fresh isolated application identity and passes. Live map installation,
thumbnail appearance and NPC interaction were accepted on October 1 after the
earlier desktop interruption. The cleaned maps and 359-asset build are installed;
live airport lighting and saves were preserved in backups. The atlas generated all
fourteen photographs, its double-click entered the Spaceport, and the four-column
picker selected Star Lake. Live views confirmed airport plot/apron clearance,
Star Lake's removals, Crown Isles, Orin's Village, Relay and Relief Quarter access.
Original Neris displayed Tessa's greeting and residents walking and idling.
Demo was enabled with fourteen tabs open; after one minute it wrapped from Relief
Quarter to original Neris and was already orbiting within seconds of arrival.

The canal streaming and failed-scene recovery changes add 15 and 12 net lines to
their existing owners respectively. Focused native Foundations, Routes, Rendering
and Session checks pass, including the two observed loading regressions. Four-file
SMILE formatting and the diff check pass; no resource/architecture limits changed.

## Current: reciprocal Luma entrances (September 30)

TownGatewayJourney owns matching an origin name to an arrival road cell. It consults
TownDocumentNavigation and TownMapLoads after the new document navigation is ready;
the session only delegates and applies the returned position. Arrival avoids trigger
cells and checks the connecting segment, so it cannot skip across water or immediately
trigger the return trip. Named airport road markers override the legacy remembered
return only when an explicit marker for that origin exists. The older airport interior
return contract is unchanged. TownGatewayTests covers these observed arrival risks.
TownRoadTravel retains the selected gate cell as its final point instead of moving
that point to the road lane center. The live Neris check exposed a small gate that
was otherwise missed; the focused regression covers this endpoint behavior.

StoryTownsV1/Source/connect_world.py authors reciprocal road markers from the atlas
without changing scenery or terrain. Its native release check covers fifty directed
entrances and road connectivity. The original Neris additive copy stays in staging;
installation verifies current non-marker fields and retains every existing marker.
No format, compiler, runtime, NativeProgram or ViewerWorkflow change is needed.
Native validation: Town Editor Foundations and all fifty directed entrance/road
checks pass. A live minimap trip from original Neris Town entered Neris Waterworks
and placed Arin off its reciprocal gate without retriggering. The rebuilt 341-asset
publication is installed. Changed owners: Journey +124 lines, Session +7,
RoadTravel +5; no architecture thresholds or exclusions changed.

## Current: Luma journey terrain and expanded atlas (September 30)

TownTerrainStyles maps the document's four appearances to the existing renderer's
material slots. TownSurfaceRenderer still owns incremental terrain uploads and
material cleanup. TWN7 appends the style byte; versions 1–6 retain Meadow. The
document, native storage and Python/Blender codec agree on this field. No compiler
or runtime capability is added. Four catalog landforms append to stable template
IDs; navigation shape arrays use Catalog.TEMPLATE_COUNT instead of the former 35.
They are solid scenery around level roads, not a heightfield navigation system.

World node coordinates can extend beyond the first screen up to 10,000 units.
TownWorldCanvas owns framing all cards on open/reset; TownWorldDocument owns the
coordinate bound, and pointer dragging uses that bound. Existing worlds keep their
coordinates and format. The destination picker handles all sixteen town slots in
two columns. These are focused corrections for the expanded fourteen-map atlas.

New geometry/data generation lives in StoryTownsV1 and the immutable catalog
package. Blender landform construction is separated from town assembly handling;
saved child matrices are explicitly local identity matrices. NativeProgram and
ViewerWorkflow do not grow. No architectural baseline or exclusion was raised.

## Current: curved terrain, Luma maps and scene photographs (September 30)

SurfacePaint3D owns a bounded list of 256 ordered analytic brushes. SurfaceGrid3D
retains base cells and exposes exact point evaluation for navigation. SurfaceContours3D
owns boundary clipping, preserving triangle winding. TownSurfaceRenderer owns the
incremental mesh replacement and layers continuous water below grass. It remains
responsible for bounded upload batches; no per-frame town mesh rebuild is added.
Contour sampling keeps each legacy cell's base material at its own edges. This
avoids artificial slivers and the observed capacity failure when adding a single
circle to the dense original Neris layout; the native rendering fixture covers it.
TWN6 stores base cells plus brushes and reads versions 1–5. The Python codec retains
the same document data; Blender's visible terrain is still rasterized. Thin subcell
features can be missed by the current boundary sampling. Bulk section transfer is
rejected for curved maps rather than discarding analytic geometry.

TownSurfaceTools owns circle gestures and construction previews; TownSelection owns
absolute decimal angles. TownStartingCamera supplies the shared initial perspective.
TownDemo owns elapsed cycling time; TownGatewayJourney remembers airport return
origins. TownTabs owns sixteen document slots and paging. A tab revision must exceed
both the outgoing document and terrain revisions, preventing reuse of another map's
mesh. World preview visits suspend party/camera movement and return to the opening
town. The graph remains responsive during incremental loading.

TownWorldPreviews replaces the old plan generator with sixteen cached photographs.
NerisTown coordinates an occasional small scene pass using existing draw owners.
The native viewport owner exposes Capture/Draw/ReleaseViewportCapture3D; DirectX
owns the bitmap snapshots, bounded to 32 handles and 2048-square images. Resize,
device loss and session release invalidate/release captures. World cards contain no
GPU handles or renderer references in their serialized document. Web adoption is
not part of this native milestone.

The frame-latency wait runs only when a new Direct2D frame begins. Replaying cached
cards within an active frame must not wait again; a focused native regression
guards the observed 100 ms per-card delay. Terrain readiness is retained while
landmark loading advances, and background photographs never request party teleport.

The reproducible map generators and editable files live in StoryTownsV1. The World
atlas is separate from Sin's saved Luma world. No compiler grammar change or external
dependency was introduced. NativeProgram and ViewerWorkflow have no growth. The
legacy coordinators gain only input/lifecycle delegation; feature math, photograph
caching, map generation and persistence remain in their respective owners. No
review baseline or guardrail was raised.

## Current: cached road routing and POV controls (September 30)

TownRoadNetwork owns collision-validated four-neighbor road connections for one
committed navigation snapshot. TownDocumentNavigation exposes a monotonic snapshot
version; even an identically named/revisioned replacement expires the cache.
Generation stamps avoid whole-array clearing. Undirected connections resume
interior collision samples within a time budget. There is no authored document
mutation or background thread reading live editor state.

TownRoadSearch owns an exact A* query, indexed binary heap, distance costs, parents
and progress. World Manhattan distance is admissible for this axis-aligned graph,
including nonuniform cells. TownRoadTravel retains destination validation, safe
road-center walking, movement collision and cancellation. It delegates planning and
retains route indices and compressed bends. TownRoadStops owns pending destinations;
arrival starts the next leg, normal clicks/cancellation clear the queue, and Shift
click appends without reframing the camera. TownDocumentMap draws remaining bends
and numbered stops. Unplanned future legs have markers, not speculative route lines.
TownEditorSession schedules cooperative preparation and passes progress to Files.
Search yields around 3 ms, preparation around 2 ms. Preparation pauses during an
active search so their resumable collision sample cannot overwrite each other.

Research: [Red Blob Games A*](https://www.redblobgames.com/pathfinding/a-star/introduction.html),
[heap implementation](https://www.redblobgames.com/pathfinding/a-star/implementation.html),
and [Likhachev et al., ARA*](https://www.cs.cmu.edu/~maxim/files/ara_nips03.pdf).
Exact cached A* is sufficient here; a full confirmed route precedes movement.
All-pairs double costs for the saved town's 45,360 road cells would require about
16.5 GB. This cache instead stores local connections and rebuilds after edits/load;
it is per-session and not serialized into `.town` files.

One saved-town planning measurement: previous FIFO 173 calls/292 ms CPU; cold A*
30 calls/80 ms; prepared A* 5 calls/5 ms. These are one route's planning work, not
global latency guarantees or an FPS benchmark. Preparation used about 593 ms
distributed over 301 calls. Focused regressions cover shorter physical routes with
more cells, reverse cache reuse, edits, disconnected and zero-length routes,
minimap bends, ordered queued arrivals and normal-click queue replacement.

TownPartyInset owns a six-second hold after movement and High FPS, default Off.
High FPS bypasses its 83 ms refresh delay; hidden state bypasses both scene render
and cached replay. Opaque header/side/bottom backing surrounds the inset image.
TownViewportLayout preserves action IDs while placing the compact toolbar left,
with Edit Town last. Royal detail revision 3 removes original fine and replacement
broad exterior masonry/tower joints. Native and Blender share the generator;
structure, cornices, doors, foliage, floors and collision remain intact.

Validation: the full native town foundations, routes, renderer and party-session
checks pass, including queue continuation/replacement. Native hardening and all 59
graphics/pointer/audio checks pass. The real Blender save/reopen/import round trip
passes with revision 3. Publication verifies all 340 runtime assets. Live inspection
confirms all three map tabs load, the new left toolbar works, the castle no longer
has the removed line geometry, road travel reaches its marker, and the POV toggle
and post-arrival hold work. Shift-click queue mechanics are covered by native
regression; the desktop automation API cannot hold a keyboard modifier during a
mouse click, so that exact physical chord was not automated.

Ownership/growth review versus 2011584c: no entry-point growth or new runtime,
compiler, dependency or persistence format. NerisTown +7 and TownEditorSession +13
lines are coordination. RoadTravel +100 retains journey/waypoint coordination while
Network (248 lines), Search (255), and Stops (62) own graph, query and queue state.
DocumentMap +76 owns route/status presentation; PartyInset +51 owns hold/refresh UI;
Navigation +10 exposes its snapshot stamp. No architecture baseline, exclusion or
limit was raised. The existing 506-line journey owner is still cohesive but should
not absorb new search algorithms or rendering logic.

## Current: precision views, guide editing and map presentation (September 30)

Precision3D.Camera3D owns explicit orthographic height; PrecisionCamera3D owns its
projection, basis and cursor unprojection. ArenaCamera3D composes orthographic pan,
zoom and rotation. Native precise camera command 9 carries that projection; existing
perspective command 1 is unchanged. NerisTownCamera owns view presets and NerisTown
only routes actions. Normal follow/overview framing explicitly restores perspective.
NerisTownCamera also owns cursor-anchored zoom: the displayed pose is rebased to the
picked depth, then pan compensation keeps the surface beneath the pointer during
easing. It uses PrecisionCamera3D's existing unprojection and basis, with no new
runtime camera command. Clockwise rotation snaps to the next cardinal boundary.

TownGuideList owns disclosure, scroll and row operations. TownMarkerResize owns a
single reversible resize gesture; TownGuides retains authored marker/name state and
TownHistory records completed operations. TownGuideFiles v2 serializes names, reads
v1 and commits validated guides only. Native File_Pick resets pointer capture/deltas
after a modal picker so its cursor relocation cannot pan the scene.

TownWorldView owns graph pan/zoom and card text layout; TownWorldPreviews now caches
starting-perspective photographs by document revision. TownWorldImport validates a native
file transfer before adding the named document/tab/node. Existing WorldDocument,
WorldFiles and WorldCanvas retain graph persistence and connections. No scene
renderer or editor-state reference is added to the graph document.

TownViewportLayout shares toolbar drawing/hit geometry. TownPartyInset owns its
forward-facing eye camera/rectangle attached to the minimap; NerisTown reuses
DrawContents for both passes and places route status below the inset. The native viewport
owner preserves surrounding color while rendering an inset, restores the full
viewport afterward, and rejects depth queries from the inset as main-view picks.
The same viewport owner caches the last preserved subviewport color image and
replays it without a new scene submission. TownPartyInset schedules at most one
refresh per 83 ms after the previous render, and its hidden state skips rendering
and replay. Resize mismatch requests a fresh render; reset/device loss releases
the cache. Ctrl+F controls POV visibility and Ctrl+M controls map statistics.
Web explicitly rejects preservation/replay; Web adoption remains paused.
The native model pool is 96 (formerly 64): the combined legacy town/party fixture
needs 71. This is a runtime resource capacity change, not an architecture guardrail
exception. Catalog renderer destruction covers all 64 owned catalog slots.

TownSurfaceLayers.json owns terrain/deck elevations for native and Blender. The
whole movable Royal Court assembly, including its base and bridge, clears roads;
navigation follows the same height. The earlier leaf-only clearance below is
superseded. Royal Castle detail generation appends six catalog chunks without
changing template IDs, source placements, footprint or document fingerprint.
The Blender worker applies the same decorative changes to its fresh immutable
catalog copy. Detail revision 3 now removes the exterior masonry strips and later broad joints;
see the current slice above for Sin's superseding appearance request. Flag wind derivatives retain original part
slots and bind geometry, skin only the flag details, and keep other parts on a
stationary root. TownCatalogRenderer owns those animators and their lifecycle.

Catalog floor metadata is extracted from the Royal Castle island and bridge deck.
TownDocumentNavigation treats those floors as support above underlying water while
retaining per-item solids and stair/door clearance. TownDecks owns a terrain-build
snapshot of only assemblies with floors and subtracts their transformed footprints
from generated rail segments. TownSurfaceRenderer owns that snapshot; the session
invalidates terrain when the relevant placements change. The Blender terrain worker
uses the same catalog metadata. Town documents and catalog fingerprint remain stable.

No third-party dependency, entry-point feature logic, architecture limit, exclusion,
or baseline change. The explicit native source inventory includes each new owner.
Validation: native town foundations/routes/rendering/full party session pass, including
relocated/rotated castle floors over water, preserved wall/fountain collision, rail
clipping, Guide List narrow animation widths, cursor anchoring and permanent-save
camera/party retention. Blender terrain geometry and save/reopen/import round trip
pass with the current detailed castle. Shared native graphics checks pass (59); the
compiler/native runtime build and native hardening checks pass. The isolated POV
work-count check records 90 forced second renders versus 9 cached refreshes in 90
frames, and zero while hidden; frame limiting means this is not an FPS benchmark.

Ownership review: no bootstrap feature growth. NerisTown gains 130 net lines of
input/draw coordination; NerisTownCamera gains 148 lines of camera behavior;
TownEditorSession gains 38 lines of owner coordination; TownDocumentNavigation gains
15 and TownSurfaceRenderer 26. New focused owners hold guide list/resize, world
view/previews/import, toolbar, POV and deck clipping. The native viewport owner is
extracted from existing rendering code. No architecture checks/limits were disabled,
raised or reset. The deployed native build was relaunched after preserving the town.
Live checks confirm Guide List opens, saved guides load without moving the camera,
True 2D displays eight resize handles, all three town tabs load, Spaceport and Airport
accept top/cardinal rotation, front view levels the camera, Ctrl+M toggles statistics,
and Arin's forward POV attaches below the minimap and stays hidden during travel
when disabled. Both minimap road trips reached their destinations. The bridge-over-
water cases use isolated regression documents, leaving Sin's painted ground intact.

Guide List animation guards its narrow opening/closing widths before clipping or
row drawing. The native rendering regression draws widths 1–32 and the fully open
panel, covering the reported fatal nonpositive clip rectangle.

## Current: relocated Royal Court drawbridge clearance (September 30)

NerisCastleRoute owns the lowered leaf's two-native-unit (20 cm) clearance above
painted roads. NerisCastlePreview applies it to the complete hinged assembly;
GroundHeight uses the same offset only over the leaf. This clears both the planks
and their recessed support when the user relocates the court over painted terrain.
Gatehouse floors, saved court X/Z placement and the user's road tiles stay intact.
Town Blender landmark export applies the same clearance in authored local units.
It also evaluates appended collections before reading world matrices, preserving
their original placement and scale when adding the saved document offset.

Regression coverage includes a translated native leaf and matching walking height,
the existing raise/lower/crossing checks, and actual Blender support/plank geometry
at a relocated court. The fix stays in the existing landmark/render/navigation
owners; no entry-point growth, runtime extension, format change or dependency.

## Current: world connections and viewport controls (September 30)

TownWorldDocument owns the sixteen-node graph and each endpoint's edge index.
Version 2 preserves those ports; version 1 loads with facing left/right endpoints.
TownWorldCanvas owns graph picking, offset-preserving node dragging, port previews,
connection commits and deletion confirmation state. TownWorldFiles owns native
file-picker/asynchronous checked `.world` transfers with an immutable export key.
TownWorldEditor coordinates these owners and lists current TownTabs documents;
adding one persists its current working copy before referencing it. Opening a node
uses TownTabs.OpenNamed, preserving an already-open map instead of duplicating it.
Import validation is transactional. Canceled/invalid files leave the graph intact.
Transfer completion is polled independently of the editor's visible graph.

NerisTownCameraPanel owns the upper-right header statistics, bottom-right sliders
and bottom-left Help disclosure. TownTabs reserves the header's statistics width.
NerisTownKeyboard owns Space/Shift+Space vertical Fly Inspect movement; other camera
modes retain orbit pause. NerisTown suppresses its idle orbit timer during Fly Inspect.
TownDocumentMap owns cursor-anchored zoom and bound clamps.
TownRoadTravel restores the earlier FIFO breadth-first search, yielding every 128
nodes, while retaining the requested road-center walking and collision checks.
TownEditorPanel requests exact `Yes` before a differently named permanent update;
TownEditorSession delegates only after that confirmation succeeds.

Focused world regressions cover port placement, connect/cancel/delete confirmation,
node movement/removal and version 1/2 reads. The complete native town suite covers
cursor zoom, vertical flight, route completion, safe road centers and all three maps.
Native hardening and graphics checks pass. The full publication verifies 333 assets.
No compiler/runtime/dependency change or architecture limit/exclusion change.
The explicit build source inventory gains the two new production owners, not a
size-limit exception. Against af423401: the town coordinator shrinks 12 lines,
session grows 7, panel grows 17, world editor grows 20 and document grows 86.
WorldCanvas is 287 lines and WorldFiles 113; each owns the focused behavior above.
Live native acceptance: the existing Luma v1 world opened through the Windows picker,
the open-town chooser listed all three existing tabs, a connection click showed its
Delete/Cancel prompt, cancel retained the line, and Save World produced a verified v2
file through the Windows save dialog. The mismatched permanent-map prompt rejected
`No`; decoded authored fields in all three permanent maps remained unchanged.
Help opened in the live viewport. Held vertical-flight vectors, idle-orbit suppression,
graph drag/release transitions and cursor zoom are covered by native regressions;
the automation tool cannot reliably hold a physical key or mouse button over frames.
Returning from World Editor now carries its completed status into the town panel.
All three map tabs loaded in the final deployed build. A live minimap click completed
the road journey at X=0, Z=-1840 with `Destination reached`; zoom buttons enlarged
the pointer's map region and returned to the full extent. A native input trace found
that idle orbit was still active when minimap hit-testing ran, so the wheel fell
through to the main camera before UpdateIdle canceled orbit. Input now records
activity and wakes idle orbit before routing, including controls that return early.
The regression checks that ordering; the final live wheel-in/wheel-out check changes
the minimap while preserving main-camera zoom at 355. Diagnostic instrumentation
was removed before the final 333-asset build. Deleting a world node also ends any
active node/port drag so no subsequent gesture addresses the removed selection.

## Current: Map Load areas

TownMapLoads owns connectivity for destination inheritance, reassignment and
whole-area deletion. A logical area is a four-connected set of tiles with the same
destination; separate islands remain independent. Painting touching areas merges
their destinations as before. Reassignment and deletion follow the same boundary,
and TownHistory records each gesture as one change. Existing saves need no migration.

TownMapLoadTools draws only exposed edges from TownMapLoads.BuildOutline, using its
existing bounded lookup rather than repeated linear neighbor searches. It renders
the actual nonuniform cell boundaries, including irregular regions and internal
destination boundaries. No tile grid appears inside a solid area. The renderer
owns its outline scratch; the document remains the only authored state. Lookup and
connectivity scratch are rebuilt for each operation, so no cached map identity or
undo invalidation is required. No runtime/compiler/dependency/format change.

Foundations covers perimeter edges, one-click deletion, whole-area reassignment,
separate islands, different destinations and single-step Undo. The session fixture
exercises deletion through the actual rectangle gesture owner. MapLoadTools grows
by 27 lines, MapLoads by 89 with its shared connectivity extraction; the existing
panel changes one help line. No architecture limit or baseline was changed.

## Current: airport access and map presentation

NerisHorizonRoute's central pedestrian corridor reaches local Z=-200, the modeled
deck edge at world X=5400 where the editable connecting road ends. The apron keeps
its inset elsewhere; terminal walls, closed doors, furniture and flight areas keep
their existing collision rules. NerisTownTests covers crossing that margin in both
directions; TownSessionTests starts the outgoing trip at the airport arrival and
walks across the join, and exercises entry by all four party members after nearest-road
teleportation. Earlier round-trip tests teleported beyond the blocked margin.

NerisTownCameraPanel owns map statistics, now in the header as described above.
NerisTown removes the
three landmark header buttons and their hit regions. NerisTownTour accepts an
elevation for its existing framing operation; tab opening and right-click reset share
the approved front-facing Spaceport and Horizon views, anchored at the map floor
center. O/Orbit retains its current-view behavior.

The complete native town suite and seven-file style check pass; before the route
fix the two new boundary assertions failed. The full native build validates all
333 published assets. Ownership stays in the existing route, camera panel and tour
modules. No runtime, compiler, save format, dependency or guardrail change is needed.
Production growth is one route comment, three camera-layout lines and one tour
signature line; the town coordinator shrinks by 55 lines. Camera reset regressions
check both approved bearings, elevations, distances and map-centered anchors.

## Current: explicit permanent map updates

TownLibrary owns startup keys for Neris Town, Neris Spaceport and Horizon Airport.
It preserves the previous destination under a named recovery key before atomically
writing the permanent document. TownTabs owns replacement and activation of the
chosen tab, preserving other maps and any source copy; failed persistence leaves
the live document and destination unchanged. Startup and named map recovery prefer
these keys, with existing recovery/named-save fallbacks for older installations.
TownEditorPanel presents one destination chooser; TownEditorSession only delegates
and refreshes rendering/history, preserving temporary guides. Blender remains an
explicit immutable-snapshot export, independent of permanent map persistence.

TownSessionTests exercises all three copied-map promotions, fresh startup recovery,
previous-map backups, invalid destination and failed-serialization preservation.
No runtime, compiler, bootstrap, dependencies or format changes are needed. The
existing persistence owners grow by 63 lines (TownLibrary) and 71 (TownTabs).
No guardrail was changed.

## Current: guide gestures, map destinations and minimap travel

TownMarkerTools owns rubber selection and grouped marker movement; TownHistory
records one change on release. TownEditorPanel owns the temporary Hide Guides
display preference. TownMapLoadTools owns the rectangle/reassignment gesture and
preview, while TownMapLoads applies the connected tile mutation. The session lists
other open maps and persists the chosen destination before committing its name;
the gesture owner has no filesystem dependency. Cancel discards the preview.

TownDocumentMap owns minimap zoom and visible bounds, shared by rendering and
click conversion. TownRoadTravel owns the bounded road search and
road-center waypoints; TownDocumentNavigation caches object world bounds to reject
distant collision candidates before transforming points into object coordinates.
The existing exact collision checks still decide traversability. No compiler,
runtime, startup or dependency changes are required.

Growth against 619ea1b4: TownEditor adds 57 net lines, the session 76 and panel 36.
Focused owners add 94 (minimap), 56 (navigation), 176 (travel), 58 (markers),
69 (map-load gesture) and 67 (map-load data); existing regression fixtures add 212.
No architectural limits or exclusions changed. Full native foundations, routes,
rendering and session checks pass, including all permanent map updates, group
selection/Undo, rectangle-first map loading, zoom picking and wide-road centering.
Physical multi-marker drag acceptance remains subject to the automation limitation
documented below; gesture-state regressions do not substitute for that observation.

Live acceptance verified the native file picker, promotion of Sin's 01:11 snapshot
to Neris Town, the recovery backup, guide visibility, destination choice after a
Map Load gesture and cancellation, minimap zoom in/out, and a marked road trip
ending with `Destination reached` at road center X=0. A full decoded comparison
confirmed every authored field in the permanent town matches the 01:11 snapshot.
The complete native publication contains 333 verified assets; style and diff
checks pass. Blender's unsaved session was not touched.

## Current: section edits and independent Royal Court placement

TownDocument owns the single optional Royal Court placement. TWN5 appends its X/Z
offset and presence bit; older documents infer the original Court only for Neris
Town. TownDocumentStore and town_document_codec.py share this version contract.
TownHistory includes placement and deletion in ordinary undo. TownCourtTools owns
selection/drag/place/delete gestures; it does not mutate the terrain. The existing
NerisCastlePreview translates its mesh instances and delegates attached halo,
light, fountain, door and drawbridge placement. NerisCastleRoute owns the matching
collision/entry coordinates. Blender export translates the existing authored
collection roots; surrounding moat tiles remain in the terrain document.

TownSections prepares replacement documents without live mutation. TownSectionTools
owns rubber selection and translated previews. TownMapLoads remaps tile destinations
using its existing bounded lookup scratch. SurfaceSections3D inserts source and
destination boundaries, preserving nonuniform cells and overlapping transfers.
TownEditorSession stages replacement terrain while drawing the previous document,
then records one undo entry on success or restores the entire document on failure.
New section gestures wait for any prior terrain build to finish. No feature logic
was added to bootstrap, no dependencies were introduced, and no architecture limits
or exclusions were changed.

The Royal Court supports section moves, deletion and destination replacement,
but rejects copies to maintain one instance per map. Spaceport and Horizon still
have fixed airport assemblies: sections intersecting them are rejected unchanged.
Their model, fleet, lighting and navigation owners need a common saved placement
before those assemblies can participate. Do not report bulk coverage as literally
every map object until that remaining work is complete.

Focused checks cover section overlap, destination replacement, source fill,
nonuniform boundaries, Map Load remapping, rollback/undo, single-Court placement,
native/Blender save compatibility, and translated meshes/effects/collision.
See artifacts/court-*.log for this run's evidence and the current handoff for any
pending live acceptance. Earlier checkpoint results below are historical.

Final native validation also covers switching from Bulk Sections to guide-marker
selection and clearing a stale Court message before Undo. Live delete, place on
plain ground, and two-step Undo preserved the original moat. A live minimap trip
reached its marked destination; walking onto a newly painted Map Load tile loaded
Horizon with the party. Test-only edits were removed and all 371 saved objects,
terrain, light presets and original map links were compared with Sin's preserved
town before relaunching the complete 333-asset publication.

Physical mouse acceptance remains for Court/section dragging: the automation
input probe received only a press/release at the requested destination, without
held movement. Explicit gesture regressions pass, but that tool result cannot
prove an interactive drag. The existing short-window palette crowding remains.
Sin confirmed on September 30 that the previously duplicated street lamps now
illuminate their surroundings. That visual acceptance is complete, alongside the
passing 75-lamp rendering check. These are separate from the protected
airport-assembly work above.

Growth review against 32c5a350: NerisTown adds five net coordinator lines,
TownEditor adds 61, TownEditorSession 82 and TownMapLoads 75. The new focused owners
are TownCourtTools (159 lines), TownSections (236), and TownSectionTools (224);
TownSectionTests adds 186 lines of regression coverage. NativeProgram is unchanged.
No compiler/runtime capability, third-party dependency, architecture exclusion or
review baseline was added. Full town/native checks, 58 hardening checks, Blender
collection placement, legacy/TWN5 codec round trips, style and diff checks passed.

## Current: Sin Star Studio authoring and publication safety (September 29)

Sin Star Studio is the existing native Viewer, with unchanged folders and application
identity. NativeViewerTabs owns parent/child navigation; NativeViewerHost delegates
to it. Sin Star I retains the three saved map documents. Startup and map re-entry
consume an overview intent in TownEditorSession; NerisTown applies the current
document center. NerisTown also suppresses automatic idle tours while editing.
The earlier separate Studio/Web direction below is historical; see root AGENTS.md.

TownGuideFiles owns independent `.guide` serialization and transfer state. TownGuides,
TownMarkerTools and TownTileGuides own guide geometry, marker manipulation and actual
cell/footprint display. TownEditor routes commands; TownHistory retains marker undo.
Guide data never enters town or Blender payloads. TownMapLoads/TownMapLoadTools own
named map-trigger tiles. TownRoadTravel owns one incremental road/bridge search and its
journey state; the session passes the selected document and party position.

TownFileJobs owns immutable save snapshots and completion bookkeeping. TownFileDialog
draws nonmodal progress; TownFileLocations persists separate format paths. Native
storage/data_file.cpp performs bounded asynchronous checked SMD4 transfers with
flushed temporary files and atomic replacement/backup. Compiler/language changes
add typed Data_FileStart/Status/Progress/Message and Local_Timestamp intrinsics;
Web reports native-only transfer rather than pretending to save. Only Blender
conversion retains the worker. TWN4 adds custom day/night presets and reads TWN1–3.

Build.ps1 supports isolated full staging and refuses a running publication folder.
Check-Publication.ps1 verifies manifest identity, declared entries, and nonempty
files before launch. Launch validates before closing an existing session. Session
fixtures run with unique identities in artifacts, never the live publication.
This prevents the demonstrated stripped-project cleanup of the live asset folder.

Validation: full native solution/build; 327 language/compiler checks; native editor
foundations/routes/rendering/three-map session; focused camera and publication checks;
direct save failure/backup/checksum tests; immutable-save/newer-edit/tab-switch checks;
TWN codec round trips; actual `.guide` save/remove/reload/undo in the working Studio.
All three map tabs were clicked and rendered. Detailed current logs remain in
artifacts/studio-*.log and artifacts/town-*.log. The VSIX was installed and its 35
payload hashes verified. These results do not imply completion of the work below.

Growth review against the previous commit: NativeProgram has one line replaced;
NativeViewerHost shrinks by 90 lines. NerisTown adds 111 net lines, TownEditor 146,
and TownEditorSession 149, chiefly input/update delegation. New bounded owners are
NativeViewerTabs (234 lines), TownGuideFiles (314), TownRoadTravel (307), and native
data_file.cpp (171). Native data transfer has one bounded application-owned job;
route search has one bounded journey. No dependency, architecture exclusion or
review baseline was raised. Native additional-light capacity changes from 64 to
128 for the demonstrated duplicated-lamp failure, with a 75-lamp rendering check.

Outstanding: complete district Move/Copy with destination replacement and undo;
the SurfaceSections3D terrain foundation alone is not that feature. Fixed Royal
Court/airport assemblies still have specialized placement/navigation ownership.
Their document representation must be resolved before promising “everything”.
The existing editor can crowd controls in very short windows; the fresh default
is now 1440 × 960 while remembered user placement remains authoritative.

Earlier checkpoint sections below are historical.

## Current: Horizon r009 and centered linked maps (September 29)

NerisTownConnections owns four west/east foot gateways and arrival poses. TownTabs
retains three independent documents. TownEditorSession owns tab arrival intent,
the existing saved sun presets, and applies committed document dimensions to the
shared Arena3D floor/grid. ArenaFloor now records its authored width/depth; the
existing Create/Place operations perform resizing only when bounds change.
NerisTownTour owns the approved 146°/20° overview offset. The town coordinator
wires tab orbit, map-centered framing, door updates and UI actions; no bootstrap
algorithm or renderer API was added. Follow Party still anchors on the leader.

TownDocumentNavigation finds the nearest traversable road in its committed surface
for Ctrl+click party placement. NerisTownCameraPanel reads existing native submission
mesh snapshots twice per second before EndScene, counting each mesh instance once.
The totals include offscreen objects and actors, omit particles and duplicate
shadow/reflection passes, and report renderer vertices rather than welded positions.

NerisHorizonEntrance owns one four-part door model and four objects, while the route
owner controls proximity motion, clearance and a consumable entry event. Horizon
now uses five models, 35 parts and 89 draw objects; the main preview owns 85 and
Entrance owns four. Glow retains 201 fixtures, with brighter runway halos. Lighting
owns 27 concealed sources. Native model/material/pool limits remain unchanged.

The r009 source removes 129 fine paving-joint meshes while retaining gold floor
decoration, and moves two banners below the roof onto clear windows. Immutable
fleet GLBs are copied from validated r008 exports. Three portable documents and
Blender assemblies preserve all 362 central-town placements. Live saves were
backed up before migration; the later two-cell road fix preserves user objects
and lighting, and ends at the airport deck instead of overlapping it.

NerisAlienFleet owns four static models, twelve instances and four independent
parked/lifting/clear/landing schedules. It uses the existing Random statement and
elapsed frame time. Each pad stays completely clear for 10–15 seconds before the
next descent starts. NerisSpaceportPreview handles incremental loading/destruction;
NerisTown adds only the update delegation. Blender export places the same four
models at the original elevated pads. The Spaceport entrance road now ends at
the original causeway edge instead of overlapping six terrain cells.

Validation is recorded in artifacts/neris-fleet-tests-final.log,
artifacts/horizon-r009-hardening-final.log and the versioned r009 validation report.
The focused native session covers linked walking, entry, day/night, geometry
instances, camera regressions and resource release. Studio and all Web adoption
remain on hold. No size baseline, exclusions, save format or dependency changed.

Current change review: Arena3D adds four metadata lines; NerisTown adds 106 and
removes 19 coordinator lines, TownEditorSession adds 92 and removes three. The
new alien fleet owner is 197 lines and the terminal door owner is 85 lines.
Neither NativeProgram nor ViewerWorkflow grows. The existing focused hardening
source ownership checks pass; no baseline or exclusion was changed.

The final native mouse check exposed retained Follow intent after a pan had
already displayed Free Camera. Orbit selection now checks the visible party
camera/orbit mode as well as that intent; the focused regression reproduces
the stale state and verifies the map-centered reset. The complete town session
passes again in artifacts/horizon-final-camera-tests.log.

Earlier checkpoint sections below are historical.

## Current checkpoint: Horizon r008 (September 29)

The versioned Horizon package owns Spaceport 01 paving, gold inlays and floor
fixtures, tower relief, two strips with U-turn roads, aligned glass hangars,
partially transparent plain terminal glazing, and smooth Neris aircraft.
The sign follows the wave roof. The ground sign and white platform border are
removed. VTOL noses face outward without rotating the buildings. Source and town
assemblies preserve the previous revisions and all 362 town placements.

NerisHorizonPreview owns four models and 86 draw objects. NerisRunwayTraffic owns
ten slots with fifteen-second staggering in a 150-second cycle; one is cargo.
NerisHorizonDownwash owns 48 low-opacity particles near the ground. Glow owns 201
fixture particles, including three independently selected red antenna beacons.
Lighting and pedestrian bounds remain in their existing owners. NerisTownLandmarks
supplies the minimap label and actual airport location. NerisTown wires the Visit
Horizon Airport button to party arrival; NerisTownCamera owns the arrival pose.
Right-click restores overview orbit while O/Orbit retains the current view.

Runway paint is one packed 4096 x 640 texture using existing anisotropic mip
filtering. The former overlapping thin paint meshes are removed. The GLB exporter
adds only an optional planar color texture path; no runtime extension, dependency,
resource limit, size baseline, or save format changed. Geometry validation reports
123,301 vertices in the airport, 32 model parts and 78,290 triangles across four
models. GLB preparation verifies checksums and the actual cockpit nose axis.
The new Downwash, roof-sign, hangar-glass and runway-surface owners isolate their
respective behaviors rather than extending bootstrap.

The native build and Blender geometry checks pass. The previous full-session and
58-check hardening runs passed before the latest sign/glass/filter changes. The
latest full-session rerun is recorded in artifacts/horizon-r008-tests.log. Native
preview captures predate those final geometry changes and are historical evidence,
not final acceptance. Final live orbit inspection remains pending.

Requested follow-up after this checkpoint: lower the banners onto clear windows,
improve night runway lighting, remove paving grid lines while retaining gold
inlays, move Horizon into a third town tab, compact Neris Town, connect west/east
walking gateways and add the opening terminal door and entry trigger. These are
not implemented in this checkpoint. Studio, Web adoption and other paused camera
work remain on hold.

## Earlier: Horizon r007 facade and cursor orbit (September 29)

r007 changes the authored airport facade and native asset publication: castle teal
and gold materials, arched ivory/gold trim, mixed terminal glazing, two hangar
skylights, restored sawtooth glass halls and 16 evergreen planters. The existing
terminal skylights, runway, site, party doorway and town document remain intact.
The three models contain 25 parts and 31 drawn objects; the third aircraft still
borrows the transport model. NerisRunwayTraffic corrects yaw/pitch for the cooked
model's -Z nose. NerisHorizonGlow retains 71 runway fixtures plus the tower beacon.
Lighting and pedestrian bounds remain in their existing owners. No public save
format, compiler, runtime, asset limit or entry-point behavior changed.

NerisTown.StartCursorOrbit converts a secondary-click pixel through TownPicking's
existing opaque-depth/ground fallback operation. NerisTownCamera.AnchorPoint owns
the camera capture and neutral input reset; AnchorParty reuses it and owns the
moving-leader offset. The displayed eye is retained, while the picked point becomes
the automatic orbit pivot. Follow Party still anchors on the leader. No state,
generic picking API or reverse dependency was added. NerisTown grows 1008→1029
lines, NerisTownCamera 461→473 and NerisRunwayTraffic 141→143; bootstrap is unchanged.
No size baseline, guardrail or exclusion changed. Other camera follow-up, Studio
and Web remain held.

The nose-direction regression compares the transformed native nose against actual
world displacement through seven flight/taxi phases; reverting the fix fails all
seven. Native session coverage also checks the actual drawn mesh rotations.
Cursor-orbit coverage checks world picking, unchanged eye, fixed pivot and radius.
Scene fixtures measure the current leader-pivot radius, use the expanded map's
40,000-unit bound for the reported airport zoom, and restore minimap state around
movement checks. AnchorPoint admits a captured radius below 80, preventing the
close-pivot camera jump. Blender checks verify retained skylights,
real hangar openings, gold trim, gardens, doorway and unchanged runway fixtures.

Native route, scene/camera, editor foundations, rendering and real-asset session
checks pass. The session verifies the four-member airport entry, eight aircraft
draw phases, beacon timing, linked-town round trip and cleanup, using 53/64 models,
499 meshes and 468 materials. All 24 accepted Arin keys remain loaded.
The native hardening/architecture gate and its 58 graphics/input/audio checks pass.

The following sections retain the previous milestone's details.

## Current: Gentle Wave and separate airport towns (September 29)

NerisHorizonV1 r005 supersedes the earlier comparison layout. Full-size Horizon is
on expanded western/northern land, facing the town; original Spaceport 01 r08 is
in its own Neris Spaceport tab. All 362 latest town placements are retained.
NerisTownLandmarks owns selected-town landmark loading/release, including two
owned Royal Court door models for the standalone original airport. Preview owners
retain their own geometry lifetimes. NerisTownConnections owns two authored foot
gates and arrival coordinates; TownEditorSession.TravelTo delegates tab/persistence
work to TownTabs and consumes one pending arrival after terrain is ready. The
party's existing Teleport accepts the arrival heading. No generic world traversal
or new compiler grammar was introduced. Existing world-editor links remain
authoring metadata; these two walking gates are the implemented gameplay route.

The 496 × 461 document retains existing limits. The selected Neris Town uses
53/64 models, 496 meshes, 464 materials and 24 accepted Arin calibration keys.
Earlier authorized two-airport capacity work expanded native mesh slots to 1024
and asset-object slots to 256; current separated towns do not require both airports
resident. Native model/material limits remain 64/512. Background worker startup is
owned by the project NativeWorkerScript contract and startup/background_worker.cpp;
direct executable launch starts/restarts the existing PowerShell file worker with
the actual storage directory and a per-Viewer duplicate guard.

town_blender_landmarks.py appends the same versioned landmark collections to both
Blender export paths; it adds no mesh/codec work to the entry point. The worker runs
Blender separately and verifies exports before publishing them. Full town Blender
export can take several minutes; Save For Viewer is the small document transfer.

Validation: native routes, editor foundations/rendering, real-asset town session,
two-way walking with four party members and no resource growth, Blender export
reopen/import, and 58 native hardening checks pass. The isolated calibration test
now removes production NativeWorkerScript when relocating its generated project.
This fixes the demonstrated missing-relative-worker fixture failure. No guardrail
limit, baseline or exclusion was changed. Main/bootstrap did not grow; lifecycle
wiring stays in NerisTown/TownEditorSession, algorithms in focused owners. Studio,
Web and the previously paused camera follow-up remain held. No commit/push.
Current size review against the original repository baseline: NerisTown 854→979
lines, TownEditorSession 690→760; the new landmark and connection owners are 105
and 44 lines. Those coordinator changes include prior orbit/worker work in this
uncommitted session. No persistence or geometry algorithm moved into bootstrap.

The following earlier sections describe their named historical milestones.

## September 29 spaceport facade and current-view orbit

NerisSpaceport01V1 owns the completed standalone Blender source, portable GLB,
six native chunks, expanded town revision and actual Blender/native evidence.
NerisSpaceportPreview owns incremental loading, 21 static draw objects and release.
NerisSpaceportEntrance owns six draw objects borrowing Royal Court's existing two
door models; it never destroys borrowed models. NerisSpaceportGlow owns one additive
particle batch for thirteen authored crystal anchors. NerisSpaceportRoute owns its
full-scale transform, conservative central arrival path, proximity door motion and
one-shot cutscene entry event. These owners do not depend on the town coordinator.
NerisTown adds lifecycle wiring and
an Orbit Spaceport button; NerisTownTour accepts framing parameters while retaining
the earlier map defaults. TownEditorSession exposes a read-only coverage query so
older town revisions do not acquire a spaceport outside their saved terrain.

The southwest snapshot retains 361 placements and the latest live Day lighting,
extends west land and southwest water, and connects a 20 m road spine to Royal
Court and the town. Its 40 m causeway ends exactly at the spaceport approach.
The new region uses coarse 10 m cells while all existing cell edges remain intact.
The 478 × 508 grid stays below the unchanged 512 × 512 document limit. Spaceport
placement is (-5500, 23.12, -7600), 10 native units/m, yaw 180 degrees. Its 720 ×
620 × 404 m r08 geometry includes the raised ornamental crown; underside structure
remains below harbor water. r003 Blender town retains all 361 placements and
16,837 objects outside the old spaceport assembly, including terrain and review cameras.

The portable r08 has 483,336 triangles and eleven materials. Native packing retains
473,528 static triangles and reuses the 9,808 entrance triangles from the existing
Royal Court models. It preserves authored UVs and embeds the limestone texture;
orthonormal tangents remain explicit. No runtime/compiler or pool limits change.
The expanded scene uses 495 meshes, 461 materials and 56/64 models, including the
existing 98 Royal Court parts and 24 accepted Arin pose keys. The optional 14-model
Tripo composition no longer fits simultaneously in this expanded town; source
assets and older town revisions are preserved. This is an asset-consolidation
follow-up, not permission to increase runtime limits or remove another castle.

Sin's latest instruction supersedes the fixed-overview behavior for O and Orbit.
StartOrbit captures the displayed camera through the existing Keyboard.Capture,
clears follow/transition activity, and rotates that captured shot without a reset.
The neutral shared zoom is -16, not zero. Orbit preserves low party angles and the
explicit top-down pitch limit; ordinary manual inspection keeps its existing
constraints. Right-click and the dedicated Orbit Spaceport command still request
new framing. Native fixtures check position, target, FOV, radius and one revolution;
the actual O key and Orbit button were checked after zoom and pan in the main Viewer.
Route, drawing, retained calibrations and resource-release fixtures pass. The
58-check native Viewer hardening gate passes. All thirteen original fixed Blender
cameras were rendered for r07 and r08; fresh GLB import verifies dimensions, triangles and UVs.
The r08 asset-only follow-up removes the four user-marked ivory/gold flying-strip
pairs and eight hanging gold dock ties (sixteen objects). All earlier revisions remain.

Owner growth versus the repository baseline: NerisTown 854→934 lines (including required long-If formatting),
TownEditorSession 690→713, NerisTownTour 25→24, NerisTownNavigation +11. New resource
and route owners are 123 and 131 lines; entrance and glow are 87 and 94 lines.
No architecture threshold, exclusion or
baseline changes. Studio and Web adoption remain held. No commit or push occurred.

## September 28 royal castle, town inspection and water

NerisCastleM06V2 owns source M06-r006, the complete portable GLB, actual fixed-camera
Blender evidence, masonry texture and native derivatives. NerisCastlePreview loads
12 static model chunks, one seven-part bridge and two three-part palace doors:
15 models and 98 draw objects. NerisCastleRoute owns the placement, courtyard
clearance, stairs, proximity bridge and palace-door trigger. Native terrain owns
the moat. The native foundation exports its top only, omitting buried faces.
The r003 town document names this castle Royal Court at the former Tripo site.
It completes 470 road cells across the front promenade up to the bridge at Z=1012,
preserving all 361 live placements and Sun settings. The original Royal Castle's
side stairs are removed from the versioned Catalog-r002; central stairs remain.
The catalog retains its saved-document fingerprint and permits the specifically
retired members when importing an older Blender assembly. The source Tripo package/palette remain
available. TownCatalogRenderer admits its 14 models only while a Tripo placement
exists; TownEditorSession loads additions incrementally and the last removal
releases those models/parts. Document removal never deletes the source asset.

The existing cooker allows 65,535 vertices per part, 131,072 vertices and 16 parts
per model. Export copies are split at complete triangles; source geometry and
runtime limits are unchanged. prepare-native.mjs validates hashes, part names,
UV presence and all native counts before copying. The portable candidate remains
one complete GLB with embedded texture. Native source textures use relative paths;
the normal project cooker owns publication.

NerisTownKeyboard owns explicit Fly Inspect and held-key mapping. NerisTownCamera
owns the zero-degree manual inspection floor, ground-level Fly Inspect and temporary
cursor-orbit anchor. TownPicking unprojects the last rendered opaque depth with
a ground fallback; ray construction respects the current near plane. Releasing
an off-center orbit absorbs the displayed camera without snapping its target.
NerisTownTour now owns only the approved fixed overview framing: 33 degrees,
8,725 distance, 32-degree FOV and -179-degree starting bearing. TownEditorSession
supplies the actual document center. NerisTown advances only horizontal bearing
at 2.5 degrees per second; there is no landmark state, randomness or shot transition.
Manual orbit in either keyboard mode stops at zero; party shots may look up.

water_surface3d.h owns water shading, directional shadow opacity and a bounded
screen-space reflection search. water_scene3d.h owns one reusable opaque-color
snapshot, independent of heat distortion. Native water requests the existing
linear depth snapshot while any live water material needs it. The renderer wires
opaque capture before transparent draws and releases resources with its targets.
Blue tint bounds the reflection/highlight contribution. Offscreen objects cannot
appear in screen-space reflections; missing hits use the environment fallback.
Sun shadow opacity lives in TownLighting and the TWN2 document field; TWN1 and
legacy Blender checksums remain compatible. Studio/Web adoption is held.

Opaque water now ignores submerged scene color while retaining reflections,
directional shadows and sheen. The existing shader/GPU regression owns this behavior.
TownLighting owns placed lamp/landmark light positions and night intensity, with
no rendering or input state added to the document. Royal Court reserves slots 0–3;
town placements use 4 onward. Native Graphics3D has 64 bounded local slots and
reports their capacity through LIGHT_QUERY_MAX_LOCAL. Shared clearing, LightPool
range validation and Scene shadow-slot validation use that capacity; LightPool
still leases at most four slots. Web retains four slots and remains on hold.
Town release and Day clear local lights. Night uses RGB 140/174/255, intensity and
ambient 8%, azimuth 207, elevation 25 and shadow opacity 65%; Day also uses azimuth 207.

Current evidence is NerisCastleM06V2/Checkpoints/M06-r006. Native session checks
exercise 473 meshes, 442 materials, 50/64 models, 98 castle parts, actual rendered
cursor depth, a complete constant-framing map orbit, Fly Inspect floor and preserved 24 Arin keyframes.
WARP executes the production shader for shadow opacity 0/50/100, local reflection
colors without distortion, capture reuse and SRV release. Town rendering tests
remove/re-add/remove Tripo and require every live resource count to return to its
baseline. The main native Viewer is also inspected visually; this is not Web evidence.

Catalog-r004 and town r005 retain stable member identities. City Hall's four front
planters reuse scaled Royal Court foliage. Five small parts move from catalog 29
to spare slots in catalog 0, freeing five slots for the shrubs in catalog 29.
The catalog remains 30 models; the complete party/castle scene retains room for
14 optional Tripo models. The Communication Tower's dishes rotate to front/back
with baked geometry transforms and updated native normals, bounds and camera
volumes; its left/right crystal poles stay unchanged. These are asset changes,
not new runtime owners.

No architecture limits, exclusions or baselines changed. Native local-light capacity
is the explicit reusable renderer extension for this task. Model/resource limits,
Studio and Web scope remain unchanged.

Current changed-owner sizes: NerisTown 906→854 lines (scene/input coordination),
NerisTownCamera 424→426, NerisTownTour 232→25, TownEditorSession 680→690 and
TownLighting 35→110. Native graphics3d_directx.cpp is 10,717→10,718 lines: fixed
array/loop bounds and one capacity query, with no new feature algorithm in that
legacy coordinator. Generated catalog features shrink after stair removal.
NativeViewerHost has no growth. Tests cover light capacity/cleanup, placed-light
movement/removal, day/night presets, fixed map orbit, zero pitch and opaque-water beds.


## September 28 Decor palette and Sun sliders

Generated catalog metadata curates palette visibility without altering template
identity, assets or saved documents. TownEditor and TownEditorPanel use the same
visibility flag for initial selection, page bounds and cards. Ten repeated Decor
entries are hidden; differently lettered signs retain separate readable labels.

TownSunControls owns the seven numeric Sun sliders and their preview snapshot,
using shared UI.UpdateSlider for drag capture and release. TownEditor owns undo
commit/cancellation, and TownEditorSession applies live lighting while avoiding
navigation rebuilds during a slider drag. The controls need no runtime extension,
catalog migration, bootstrap growth, dependency or architecture exception.
The native session fixture checks palette pagination/retained geometry and Sun
preview, cancellation, undo/redo and clamped endpoints.
TownSurfaceRenderer and TownCatalogRenderer use matte PBR lawn materials so grass
responds to the document's sun/ambient just like paving. Flat surface batches and
catalog lawn parts receive shadows without casting onto themselves; rail batches
and scenery still cast. A native comparison isolated the reported curved bands
to terrain self-shadowing, and the render fixture protects the material/caster
contracts. Shadow edge diffusion stays in the existing native renderer's simple
and PBR filters (5x5 tent weights, 1.5 texel spacing), with no new public API,
resource, document field or texture. Web adoption remains held.

Changed owner sizes: Sun controls 210 lines (new cohesive gesture owner), Editor
591 (down from 611), Panel 380 (down from 398), Session 660 (up from 640), surface
renderer 531 (up from 524), catalog renderer 144 (up from 138). Catalog data grows
by 36 lines of generated metadata; the native renderer grows by two comment lines.
No limits or baselines change. Validation: Town foundations/routes/rendering/session,
native Viewer hardening (including 58 graphics/input/audio checks), style gate,
Release build and installed VSIX payload verification pass. The grass A/B preview
isolated self-shadowing and showed scenery shadows crossing paving.

## September 28 native town file and appearance changes

`TownEditorSession` coordinates the existing document/render lifecycle and delegates
file jobs to `TownFileJobs`, tab snapshots to `TownTabs`, and thumbnail controls to
`TownEditorPanel`. `TownLibrary` owns permanent Neris recovery. Filesystem work stays
in `TownFileWorker.ps1`; retention is isolated in `TownVersionFiles.ps1`; Blender
conversion reuses the current builder from `town_blender_files.py`. No bootstrap,
Studio implementation, architecture threshold or exclusion changes are required.

The shared `File_Pick` primitive owns only a Windows path dialog. It introduces no
filesystem access or town-specific logic in the runtime. `ArenaViewport3D` exposes
an optional pitch limit; the town's `1` command opts into near-vertical inspection.
Grass material ownership remains with catalog/surface rendering; submerged shoreline
geometry and closed road/water sides belong to the surface renderer. Shared water
shading stays town-independent. TownLighting owns the corrected upward sun vector;
simple-material shadow texel filtering remains in the native renderer. Terrain and
catalog grass use mipmapped anisotropic textures without changing catalog identity.

Changed feature-owner sizes (review triggers, not new baselines):

| Owner | Lines |
| --- | ---: |
| `TownEditorSession.smile` | 640 |
| `TownEditorPanel.smile` | 398 |
| `TownFileJobs.smile` | 422 |
| `TownTabs.smile` | 210 |
| `TownSurfaceRenderer.smile` | 524 |
| `TownFileWorker.ps1` | 118 |
| `TownVersionFiles.ps1` | 60 |
| `town_blender_files.py` | 130 |

Compiler, native Viewer hardening, editor foundation/route/render/session, real
Blender round-trip, terrain separation and dated-version retention checks pass.
The editor catalog fingerprint is unchanged. Detailed contracts and current manual
acceptance results belong in the town-authoring document.

## Native Neris town ownership

The native host retains top tabs and delegates town lifecycle to `NerisTown`.
Battle System uses the same visible tab strip. Unsaved calibration guards scene
replacement. Studio/Web adoption remains held.

| Owner | Responsibility |
| --- | --- |
| `NerisTown` | Lifecycle, input and camera/party/effect coordination |
| `NerisTownAssets` | Incremental static/Old Castle loading and progress/timings |
| `NerisTownParty` | Actor contexts, gait/rate, fade, calibration and leader selection |
| `PartyTrail3D` | Bounded arc-length history and ordered follower slots, no actors/input |
| `NerisTownNavigation` / `NerisTownDistricts` | Movement substeps, sliding, land/water/solid collision and height |
| `NerisTownEntranceNavigation` | Authored stair surfaces and oriented door approaches |
| `NerisTownEntrances` | Shared hinged leaves, open/close clocks and one-shot doorway IDs |
| `NerisTownCamera` | Low horizon framing, close grounds, heading/boundary policy |
| `FollowCamera3D` / `CameraClearance3D` | Reusable acceleration-limited following and padded segment/AABB clearance |
| `FreeCamera3D` | Input-independent eased translation of the eye and target together |
| `ArenaViewport3D` / `ArenaCamera3D` | Shared input, distance zoom and framing transition |
| `NerisTownCameraPanel` | Numeric controls and Orbit/Fit/Fly Inspect |
| `NerisTownKeyboard` | Explicit keyboard mode, held input routing and camera travel |
| `NerisTownAppearance` | Lighting/water clock and effect lifecycle |
| `NerisTownTrees` / `NerisTownFlowers` | Reused template draw objects; trees also own recycled leaves |
| `NerisTownCrystals` / `NerisTownMap` | Halos/sparkles; map images/projection/fade/leader coordinates |
| Generated `Layout`, `Site`, `Obstacles`, `EntrancesData` | Saved Blender data; no behavior |

### Blender and export boundary

The live Town Editor document is authoritative. Explicit Blender imports/exports
are described in `docs/architecture/town-authoring.md`; the static showcase
`Blend/Neris-Town-Waterfront.blend` must not replace live edits. Its reproducible input is preserved
`Neris-Town-Expanded.blend`. `waterfront_plan.py` owns coordinates;
`waterfront_town.py` places architecture/terrain/streets; `waterfront_landscape.py`
places checked gardens/furniture. No script saves a live interactive Blender session.
`export_waterfront.py` derives entrances, static models, effects, camera bounds and
site collision after saving Blender. `static_glb.py` preserves evaluated normals
without decimating architecture. `export_flowers.py` separates the flower template.
`collision_bounds.py` separates royal portal jambs and crowns for both walk and
camera export: whole-mesh bounds previously sealed the hollow gate.
`paving_grid.py` owns unioned two-metre tiles; only bridges cross water.

Blender XYZ maps to native XZY at ten units/metre, height +21. The sign is at
(0,-265 m); the initial leader is (0,23.12,-2780 native units). City Hall/tower scales
are 2/4; HQ is doubled. The town README owns district counts and rebuilding steps.

### Motion and camera contracts

Town starts Run at 200%; movement and catch-up share one rate. High-speed run
cadence follows a square-root curve, retaining fractional elapsed time. Private
town clips reduce airtime; public model and calibration identities are unchanged.
`Character3D.SetOpacity` fades owned objects without mutating shared materials.

Ctrl+Tab rotates complete Member contexts through route slots; slot zero always
leads. Camera, map, navigation and doors consume that slot. There is no duplicate
selected-index state. Each context retains its actor, profile, key base and clips.
Previous presentation positions reset to prevent reordering from changing facing.
No actor or route reload occurs.

Tab sets north yaw and a 1,000 ms arena transition. During this explicit ease,
`NerisTown` settles the follow solver at the displayed camera instead of adding
its slow velocity limits. Ground correction translates eye and target together,
preserving angle and distance. Close grounds ease from 30 m to 8.5 m behind the
leader and raise aim for stairs. Manual pitch is -20..80 degrees. Boundary turns
look inward slowly. Ctrl+F retains F for floor; Fit preserves automatic orbit.
Movement stops orbit; manual camera input suspends following.

WASD moves the party by default regardless of camera framing. The explicit Fly
Inspect toggle (off at startup) sends WASD/arrows to camera travel; with it off,
arrows control continuous zoom/orbit. Space pauses/resumes the automatic tour;
Alt and Shift+Space have no separate travel action. Fly Inspect reaches the ground
by moving forward/down the viewing direction, retaining a 0.1-unit clearance.
`NerisTownKeyboard` owns the toggle, held-key mapping and camera travel state;
`NerisTown` coordinates party/navigation and camera owners. `FreeCamera3D` owns
smooth translation math, and
`NerisTownParty.Stand` keeps all route slots stationary while presenting idle poses.
`NerisTownCamera.Compose` resolves the unzoomed drone shot before applying arena
zoom and paired ground clearance. Feeding the zoomed shot to the slow follow
solver caused the reported skyward tilt and long zoom drift. The focused native
inspection fixture reproduces release drift on the former implementation and
checks angle preservation, zoom settling and stationary party positions.

Arrival is a centered, north-facing shot through the Kingdom sign; it bypasses
obstacle-driven reframing while idle. Twenty seconds without keyboard/mouse
activity starts inspection orbit; input stops only the automatic idle orbit.
The camera panel owns actual frame-time sampling through existing `ViewerTiming`
and displays the final composed camera above Orbit/Fit with an 80% opaque backing.

September 26 inspection/zoom review: `NerisTown` is 584 lines (+72), camera policy
265 (+50), camera panel 176 (+57), party 496 (+16), and shared `FreeCamera3D` 75
new lines. Camera constraint/composition moved to its existing camera owner;
the host and renderer did not grow. No thresholds, exclusions, dependencies or
baselines changed. Native scene/route regression, 49 calibration checks and
58 graphics/input/audio checks pass. Native build/relaunch passed; final visual
framing and the panel remain available for Sin's review. Web checks stayed held.

Sixty-six authored doors swing inward and expose one-shot IDs. Interior/cutscene
loading is a future consumer; no destination content was created. Old Castle's
fused mesh has no authored hinged leaves. Conservative exterior bounds are not a
full navigation mesh or general interior collision system.

### Rendering, loading and validation

Static geometry uses 37 models / 92 parts / 4,029,482 triangles. Old Castle retains
14 models / 28 parts with shared 2K runtime maps and preserved 4K originals. Two
tree templates reuse eight draw objects for 147 placements; one flower template
reuses eight for 48 placements. Native draw submissions snapshot transforms, so
later object reuse cannot relocate earlier draws. Ten leaves recycle without
allocation. Seven fountains plus town water use eight water and eight distortion
batches, reaching the current 16-ribbon limit.

The native fixture uses 155/256 meshes and 144/512 materials. The populated scene
exhausted 512 frame submissions; native capacity is now 2,048. Other pool limits
stay unchanged. PBR blending follows captured object opacity; cutout masking uses
material/texture alpha. Renderer code has no town dependency. Static texture names
hash converted pixels, avoiding ORM collisions and sharing partition textures.
Character texture identity is unchanged; the cooker version invalidates caches.

Focused checks: `test-neris-town.ps1` (actual routes, all actors, full draw, fade,
water, camera, 66 doors, leader cycle, cleanup); `test-neris-waterfront.py`
(connected roads/clear gardens); `test-neris-paving.py` (tile union/seams/clear water);
Blender `test-neris-bridge.py` (ten bridge layer separations and civic rail banks);
`test-model3d-shared-textures.ps1` (cooked-pixel identity); native Viewer hardening.
The gate regression walks the whole bridge and entrance; isolated open-point
checks did not catch the hollow-arch blockage.

No architecture baseline, threshold, exclusion or dependency was relaxed.
Generated placement tables account for most growth. Behavior stays in focused
owners. Bootstrap is unchanged; the host change preserves Battle System tabs.

## Native battle planning and presentation

`NativeProgram` owns only the native window/frame loop and constructs Workflow
and NativeViewerHost. `Program` remains the existing held-host entry point.
`NativeViewerHost.Session` owns encounter, battle presentation and menu state,
tab selection and their clock. It passes the existing Workflow instance explicitly
because SMILE does not support storing another class instance as a class field.
The build removes native-only source/font entries when deriving the existing
Studio/Web host inventory; it does not adopt or publish this feature there.

`BattlePlanning` collects four orders, wraps the unchanged rules for automatic
whole-round repetition and applies this encounter's +300% defense policy.
`BattlePresentation` translates resolved actions into borrowed Viewer actor clips,
movement, reactions, floating feedback and the requested camera preset.
`BattleUi` owns menus and input; it does not resolve combat. The existing
Sin Star I progression, stats-view input, attacks, rules, feedback and icon
functions are source-linked unchanged. Part 1a adds Viewer-owned curved stats and
rewards, plus approved Victory additions to the canonical character packages;
game code and held Web publications remain unchanged.

`BattleProgression` owns the pure stat and Kael reward curve. `BattlePlanning`
owns session EXP, previous values for display and one-time reward application.
`BattleGrowthView` draws the curved table/graphs using existing stats-view input.
`BattleOutcome` owns the ending clock, inactivity timeout, sound and reward
dialog. Its cinematic camera composes the shared ArenaCamera3D orbit; it does not
introduce private pan/zoom/orbit controls. NativeViewerHost owns these states and
delegates input/update/draw. The sound is native-only in derived host inventories.

Right-click restarts from captured formation homes and retains session EXP.
Outcome presentation cannot grant or revoke rewards; early dismissal is safe.
Victory clips are selected by name from each actual actor, with Idle fallback
for future packages without one. The three approved additions preserve all
pre-existing GLB animation/model data and indices. Arin's fingerprint migration
preserves every authored pose key by exact clip name, with an empty Victory bank.

Workflow retains scene/actor/calibration ownership and exposes a bounded directed
playback bridge. Existing Party choreography is bypassed while that explicit mode is active;
its camera policy is reused through the directed camera bridge described below. Playback owns pause/demo state; ViewerCamera
owns preset adoption and the shared smooth controls. Returning to Kael Party
clears directed mode and reloads the original scene. Unsaved editing and active
Beat previews block tab changes; battle execution restores the primary actor
after a legacy inspector binding. No renderer or language extension is needed.

The reported stacking bug was caused by capturing frames before the first Party
formation placement. Directed startup now applies the existing formation before
the presenter records homes. The real-asset `BattleSceneTests` checks distinct
homes before frame one, return to those homes after the full round, initial
waiting, scene health and return to ordinary Kael Party. `BattlePlanningTests`
covers order collection, implicit repetition, deferred interruption, +300% defense,
LB and progression boundaries. `scripts/test-viewer-battle.ps1` derives the scene
fixture inventory from the native project and uses isolated application storage.

The existing native hardening/architecture gate passes, including isolated
calibration and 58 graphics/input/audio checks. Its startup inventory and one
duplicate import were adjusted for the native entry point; assertions were not
relaxed. Native visual checks cover formation, camera, Fight/Auto, repeated rounds,
deferred orders and stats displays. Web acceptance remains held.

The new feature owners remain below the 600-line review trigger. Workflow's
existing coordinator exception gains only explicit scene operations, state and
delegation; Party gains directed-mode guards and selected healing-target wiring.
No size threshold, no-growth baseline, exclusion or dependency is changed.

Growth review after Part 1a: NativeProgram is 37 lines, NativeViewerHost 370,
BattlePlanning 238, BattlePresentation 413 and BattleUi 537. The new focused
owners are BattleProgression 55, BattleGrowthView 274 and BattleOutcome 209.
Profiles adds ten net lines for clip inventory, hold policy and the migrated
fingerprint. The asset append script is 317 lines and remains build-time tooling.
Part 1a does not grow Workflow or Party. The original battle coordinator grew by
195 net lines to 3,058, Party by 35 to 4,083, Camera by 14 to 663, Playback by 14
to 535, inspector presentation by two and legacy UI by five. Camera's small preset
operation belongs with its existing controls; no new camera algorithm is placed
in Workflow. The existing oversized legacy owners keep their responsibilities.
The pre-existing held Studio asset-manifest mismatch is recorded in the Party
Beat checkpoint; preserving its source inventory is not Studio build acceptance.

Part 1a validation covers one-time rewards, retained EXP/levels, full-heal restart,
curved growth, all four living Victory clips, dialog counting/activity/timeout,
centered dialog placement and full-circle camera endpoints. Native hardening and
calibration pass, including exact preservation of the nine original Arin pose
matrix rows and all 24 keys; the tenth reference is Victory. The shared native
Fire Lab pose comparison also passes after accepting the expanded clip inventory.
Visual preview checks caught and fixed the reserved `Left` coordinate collision
and Zara's root-parent/export basis mismatch. The dialog, dismissal controls,
ending camera and all four celebrations were inspected in the native renderer.
No language/runtime extension, new dependency or architecture exception was added.
Audio asset/playback calls are exercised; no captured audible-output check is claimed.

Directed-mode resets restore the camera without entering the legacy inspector
borrow/reset lifecycle. That lifecycle otherwise reopens panels and restarts demo
state underneath the encounter. Both reset entry paths are guarded; the native
scene fixture checks reset visibility and the visible right-click check verifies
the requested camera and formation are retained.

## Native battle cinematics and responsive HUD (Part 1b)

`BattleCinematics` owns the host's enabled flag, opening clock and phase changes.
The current native camera path reuses Party framing/attack shots; see the camera
parity section below. It no longer calculates separate close-up radii or rotates
while orders are waiting. The host coordinates camera ownership with pending
pose edits, Beat preview and the pre-existing twelve-second ending orbit.

`BattleHud` owns pure responsive geometry, status rendering and the native right
panel's Battle/Inspector tabs. `BattleUi` consumes the same geometry for hit tests
and menu drawing. Hidden editors yield four wider hero cards and a wider enemy
bar, with a fixed 166-pixel command card and symmetric margins. This extraction
moves existing HUD responsibilities out of BattleUi; combat and progression are
unchanged. No Workflow, Party, shared source inventory or asset change is needed.

The highlighted red order pointer remains in `BattlePresentation`, the owner of
world-projected feedback. It is a faceted, glowing, gently floating game cursor,
visible only for a living, present hero in WAITING. EXECUTING and terminal phases
hide it, following Sin's corrected instruction.

Growth: BattleUi shrinks from 537 to 409 lines; BattleHud is 237 lines and
BattleCinematics 220. NativeViewerHost grows from 370 to 393 lines through state
and delegation; BattlePresentation grows from 413 to 453 for order-pointer policy
and drawing. BattleSceneTests is 249 lines. All feature owners stay below the
600-line review trigger; no threshold, baseline or exclusion was altered.

Validation: the native planning/real-asset scene fixtures pass, including fixed
command geometry, expanded margins, all shot types moving, impact timing and
both subjects' heads inside the unobscured scene area. The order-arrow policy
passes in waiting/executing phases. The native hardening gate passes, including
calibration isolation and 58 graphics/input/audio checks; scoped formatting and
diff checks pass. Native UI review verified compact/expanded layouts, Battle
toggle off/on, an attack close-up, deferred interruption, relocated Kael stats,
and the final crystal pointer following the next hero. Arin and Orin exports
match their canonical JSON bytes. The final native Viewer was rebuilt and
relaunched through Launch.ps1; no Web build, publication or browser acceptance.

## Native Battle System camera parity with Kael Party

The September 21 correction replaces BattleCinematics' separate close-up/radius
formulas with the existing Kael Party camera policy. `ViewerParty.FormationCamera`
is a small extraction of the original formation zoom/height/revolution. The demo
and directed bridge call the same implementation. `ApplyAttackCamera` explicitly
opts directed playback into its existing shots; the ordinary frame-loop call
still bypasses it. A small time adapter maps the directed 400-ms approach to the
same hero/boss camera beats. Kael's directed shot reads his real presented position
instead of estimating movement from the demo's different timeline.

Workflow captures the ordinary Party camera preset, including its initial orbit
angle, pitch and zoom, before adopting a directed preset. Its new operations only
compose/delegate camera policy and expose the rendered camera for verification.
BattleCinematics keeps the two-second revolution and one-second blend, selects
Party action shots, and restores the exact order preset on the transition to
WAITING. Waiting has no timed cuts or automatic rotation.

`ViewerCamera.FollowView` changes the scripted base without clearing input capture,
pan, orbit or zoom state. Existing pan/orbit input disables automatic following
and retains the last shot; smooth wheel zoom remains effective across shots.
Only explicit entry/reset, re-enabling cinematics or returning to orders clears
those offsets. Directed playback does not also advance the legacy automatic orbit
or apply a saved demo Beat timeline over that result. NativeHost passes arrow keys
to camera navigation during execution; they continue to select menus in orders.
Ending-camera and battle-restart policy are unchanged.

Growth from the preceding committed version: BattleCinematics 255 -> 87 lines,
BattlePresentation 505 -> 515, NativeViewerHost 419 -> 420, ViewerCamera 663 -> 679,
ViewerParty 4,084 -> 4,131 and Workflow 3,065 -> 3,132. SceneTests 392 -> 479.
The legacy coordinator gains only state, bounded operations and delegation;
framing stays with Party and interaction stays with Camera. No new dependency,
language/runtime feature, file-size limit, baseline or exclusion is introduced.

The real-asset fixture covers the complete revolution and blend boundaries,
all four order selections across former cut deadlines, wide planted hero and
behind-defender boss shots, all five actors' rendered keyboard-camera response,
retention on the next attack frame, and small/moderate horizontal/vertical pointer
pan/orbit plus smooth zoom in/out and reset through the shared input owner.
The normal native hardening/architecture, calibration and 58 graphics/input/audio
checks pass. Native visual inspection confirms the wider attack framing, arrow-key
orbit during execution, horizontal/vertical pan during Kael's action, wheel zoom
in/out, and right-click replay of the wide opening followed by the default order
view. Small/moderate middle-button deltas are covered through the actual shared
input owner; the available desktop automation supports only left-button drags.
The final native Viewer is rebuilt and relaunched in its saved position. Web/Studio
validation remains held.

## Explicit Auto Battle interruption

`BattleUi.InterruptForInput` owns the native interruption policy: Enter or
a primary click inside a Battle System panel requests orders. Camera drags,
scene clicks, wheel motion, ordinary keys and showing the panels with backtick
leave automatic rounds running. Existing battle HUD/stats hit areas are reused;
NativeViewerHost additionally supplies the existing Battle options-panel hit.
The shared planning/rules owner still finishes all heroes and Kael before it
turns Auto off at the round boundary. Initial 30-second inactivity startup,
right-click restart and the ending/restart timers are unchanged.

BattleUi grows 422 -> 439 lines, NativeViewerHost 420 -> 422 and SceneTests
479 -> 517. BattleHud stays 237 lines and updates the visible control guidance,
including stale text for the replaced cinematic policy. No new source owner,
language/runtime feature, dependency or architectural exception is needed.
Focused regression checks cover camera/ordinary input retaining Auto and each
of panel click and Enter deferring the stop until the round finishes. Sin's later
Space correction below supersedes the original Space interruption policy.

Validation: native battle planning/scene regression checks, the native-only
hardening/architecture suite and focused source formatting checks pass. The
rebuilt native Viewer retained Auto across rounds after scene dragging and
arrow-key camera input. Enter and a party-panel click each displayed
`Orders Next Round` and returned to the orders screen at the next round.
The Viewer was gracefully replaced and remains running in its saved position.
Web/Studio validation remains held.

## Native camera return, pause and header-free view — September 21

`NativeViewerHost` owns the battle pause flag and fourth UI visibility state.
Space now pauses/resumes like Kael Party without requesting new orders. Combat,
actor, inactivity and result/restart clocks pause together; Viewer camera input
continues. `BattleCinematics` captures the rendered camera and composes the existing
`BattleCameraShots.Blend` for C's one-second return to the initial orders view,
including while paused or cinematics are disabled. It retains that default until
manual camera input or a cinematic toggle/reset. No camera algorithm is duplicated.

The fourth backtick state hides the header/tab drawing and hit regions, then
returns to the original three-state cycle. Battle startup selects this fourth
state through the existing `NativeViewerHost.HeaderHidden` flag; restarting a
battle preserves the current selection. `BattleHud.Layout.EnemyY` owns Kael's
94/18-pixel placement and is shared by rendering and `BattleUi` stats hit tests.
Ordinary character tabs retain their existing cycle. `KEY_C = 41` extends the
shared language constant inventory and native key map/held/event snapshot range;
old values are unchanged. Web input adoption remains held and is documented.

Growth from the preceding commit: BattleCinematics 87 -> 122, BattleHud 237 -> 258,
BattleUi 439 -> 443, NativeViewerHost 422 -> 471, BattleSceneTests 517 -> 594.
State stays with the existing feature owners; Workflow, Party and the entry point
do not grow. No dependency, threshold, baseline or exception changes.

The startup-default follow-up adds one line to NativeViewerHost (471 -> 472),
without introducing state or changing the existing four-state cycle.

## Calibration rejection recovery — September 21

The live Viewer showed rejected storage and zero keys while its saved binary was
byte-equivalent to the authoritative 24-key JSON. Import returned before opening
the picker because `StorageRejected` stayed set. Rebuilding/relaunching restored
the authored values and the picker without changing either saved file. The
original rejection trigger was not reproduced; the open evidence/next action is
in `docs/implementation/party-beat-camera-checkpoint.md`.

`ViewerCalibration` retains transaction ownership. Import retries the normal
checked read when an earlier rejection is latched, and proceeds only after the
current save passes validation. Persistent rejection still blocks writes. Load
diagnostics now distinguish profile, clip and frame failures; they no longer
leave a misleading default “Saved Keys” message after a failed decode. Existing
recovered-backup wording is preserved. No pose, profile or asset migration occurs.

ViewerCalibration grows 2,485 -> 2,509 lines within its existing persistence
responsibility; CalibrationTests grows 2,433 -> 2,460. This is a local recovery
change in the reviewed legacy owner, without new shared state or extraction.
Focused checks cover read-only recovery, still-unavailable storage protection,
unchanged saved JSON, and Arin key availability through Battle startup and return
to the editor. The battle fixture now uses fresh isolated storage on each run.

Validation for this delivery: battle planning/real-asset scene tests, native-only
Viewer hardening/architecture and calibration isolation pass, including 49
synchronizer and 58 graphics/pointer/audio checks. The native queued-key fixture
passes with C, as do 323 shared language/compiler tests. The shared test's stale
Sin Star I music names and appended Battle enum expectation were updated to its
current source contract. Scoped SMILE formatting and diff checks pass. The rebuilt
VSIX is installed and all 35 installed payload hashes match. Visible native checks
confirm Space retains Auto, C returns while paused, pan/zoom remain available,
the fourth UI state relocates Kael, and Arin's Import picker opens. No Web/Studio
adoption, publication or browser validation is claimed; generic compiler tests
include their existing temporary Web-output fixtures.

## Native battle travel facing and automatic restart

`BattlePresentation` captures formation facing alongside formation positions.
During approach/return it faces the active fighter along the path, then restores
the accepted battle stance at the attack and home endpoints. It stops selecting
Run at the arrival boundary. Orin's -55-degree hammer stance is restored only
outside locomotion; Kael continues to face his selected target after arrival.
The original Party demo is unchanged.

Runtime calibration yaw belongs to the borrowed `ViewerActors.Context` and is
copied through Workflow's existing inspector/context bridge. ViewerCalibration
turns equipment position vectors and rotation corrections, including glow, using
shared `PrecisionMath3D.ReorientYaw` and `Character3D.SetPartPositionOffsetPrecise`.
Both extend existing source-library owners; no renderer, language, persistence
format or asset change is required. Default zero yaw preserves existing callers
and all saved calibration channels. Wrist corrections retain local bone space.

`BattleOutcome` owns a separate 15,000-ms ending clock unaffected by dialog
activity/dismissal. NativeViewerHost passes actual frame elapsed time to ending
presentation (combat integration retains its 100-ms cap), then delegates to its
existing full-heal, EXP-preserving restart. Victory and defeat use the same path.
Pending editor changes keep the existing save/cancel guard. Restart clears ending
and menu state, restores the camera/formation and waits for initial orders.

Growth: BattlePresentation 453 -> 505 lines, BattleOutcome 209 -> 221 and
NativeViewerHost 393 -> 399. The math owner grows 205 -> 240. Existing legacy
owners gain only related responsibility: Character3D +15 lines for a precise
offset overload, ViewerCalibration +6 for correction-space application,
Workflow +7 for context/reset/delegation and Party +1 for the calibration argument.
ViewerActors gains one context field. No limit, baseline or exclusion changes.

The native real-asset fixture covers all four traveling fighters, approach/return
direction, arrival Idle/facing and Arin's actual Run SwordTip under the reversed
body transform. It also checks both outcome deadlines at 14,999/15,000 ms,
including dialog activity. Existing planning checks cover retained EXP/levels and
full-heal restart. The native precision fixture compares all three rotated basis
vectors for authored sword/shield corrections, fractional yaw, Euler pole cases
and unchanged zero yaw. BattleSceneTests grows 249 -> 360 lines; Precision3DTests
adds 39 lines to the existing fixture.

Validation also passes the native hardening/architecture gate, 49 Arin calibration
checks, isolated pose round-trip and 58 graphics/input/audio checks. Scoped style
and diff checks pass. Both pre-commit calibration exports preserve the canonical
JSON bytes. The native Viewer was rebuilt/relaunched through Launch.ps1 in its
saved placement; visible Auto battle exercises the new presenter and automatically
returns from defeat to Round 1 with full HP/MP and initial orders. Web/Studio
adoption and their existing held acceptance work remain out of scope.

## Initial idle Auto and opening revolution

`BattlePlanning.State` owns the initial inactivity clock. `AdvanceInitialIdle`
starts the same `BeginRound` path as the Auto button after 30,000 inactive ms,
only before the first action of a fresh encounter. User activity and unavailable
presentation reset the clock. `Start` rearms it for entry and both reset paths;
orders requested after a running round remain indefinite. Confirmed and remembered
orders retain their existing meaning.

`BattleUi.ObserveActivity` samples keys, pointer motion, wheel and pressed/held
buttons before any menu/editor early return. NativeViewerHost passes this state
and actual elapsed time to the planning owner. Stats, visible inspectors, pending
edits or an unavailable scene prevent idle startup. The host clears an abandoned
order submenu and resumes the existing directed scene when Auto starts.

`BattleCinematics` captures a formation-wide opening and reuses Kael Party's
two-second `EstablishOrbitDegrees` timing with shared `ArenaCamera3D.ComposeOrbit`.
The following second uses the existing `BattleCameraShots.Blend` operation to ease
into the current cinematic shot, including target, direction, distance and FOV.
The full revolution completes before blending. Entry and reset replay it;
disabled cinematics continue to preserve the manual view. No shared camera math,
renderer, language, asset or calibration extension is needed.

Growth: BattlePlanning 238 -> 267 lines, BattleUi 409 -> 422,
NativeViewerHost 399 -> 419 and BattleCinematics 220 -> 255. Existing focused
owners retain their responsibilities; no legacy owner, threshold, baseline or
exclusion changes. PlanningTests adds 41 lines and SceneTests adds 32.

Native planning checks cover 29,999/30,000-ms boundaries, activity reset,
inspector/stats suspension, preserved orders, restart rearming and later manual
planning. Real-asset scene checks cover opposite-side and full-circle positions,
both smooth transition boundaries and the exact destination camera/FOV, plus
the existing battle regressions. Native hardening/calibration and 58
graphics/input/audio checks pass; scoped formatting and diff checks pass.
The rebuilt native Viewer visibly starts Auto without a button click. Activity
just before the original deadline keeps it waiting beyond that deadline, then
Auto starts after the renewed idle interval. Native opening and following-shot
views were inspected; exact revolution/transition timing is covered by the fixture.
Launch.ps1 preserves the saved window placement and authoritative calibration.

## Standalone tab music

`ViewerMusic` owns the Kael Party track policy using the existing `Play Music`
runtime. Its small state belongs to each Workflow instance. Workflow delegates
after a tab load and at release only for standalone sessions, preserving the
game's separate music owner. Repeated Kael Party loads do not restart the track.
The new owner is 39 lines; Workflow adds ten import/state/delegation lines.
`Prepare-BuildAssets.ps1` mirrors the canonical Sin Star I MP3 into ignored cooking
inputs; the Viewer project publishes it. No runtime extension or guardrail change.
Studio receives only the required shared-source inventory entry.

Validation: both native builds, native theme transition fixture, actual-asset
presentation/music continuity checks, Viewer hardening (including calibration and
58 graphics/input/audio checks), scoped formatting and brief native launches pass.
The hosted fixture rejects any standalone Viewer music override. No Web validation
or audible-output capture is claimed.

## Kael bending without locomotion

`Profiles.PartyTravelClip` owns the distinction between Kael's named bending casts
and sword attacks. `ViewerParty` uses it for both home-position gating and live
travel-phase animation; `ViewerBeatSequence` uses the same policy when seeking.
The previous position-only gate left the Run clip active while Kael stood still.
Bending now uses Idle before/after the authored cast without moving impact times,
camera beat durations or saved camera data. Fire's three existing Lab names share
the policy; its assets/effects are not adopted into Party by this change.

The travel policy adds 23 profile lines; the two existing callers change only their clip/position
selection. The actual-asset presentation fixture checks approach, cast and return
in live and Beat playback for all six Earth/Water casts and normal sword attacks.
Hardening checks the Fire names and Vrax's unchanged Run policy. No new module,
dependency, state owner, compiler/runtime capability or guardrail exception.

Mira's default speed is a shared profile constant (200), used by all Mira profile
variants, Party's per-actor speed resolver and the Dragon demo's return to Idle.
Mira's own solo speed control and explicit Party overrides remain respected.
The Water Lab already advances its sampled animation/VFX clock at 200 by default.
The native presentation fixture checks real actor clocks: 50 ms advances Mira by
100 animation milliseconds in solo and all loaded Party rosters. Runtime assets
and saved pose/Beat data do not change.

## Persistent recovery evidence

`ViewerDiagnostics` owns bounded report formatting, a 32-event in-memory history
and checked storage writes. Its state belongs to each Workflow instance. It reads
existing actor/Party/Beat/session state without owning or modifying that state;
no owner calls back into Workflow. Workflow only observes input, marks load/reset
actions and delegates one report after entering recovery. A failed retry starts a
new reporting attempt; repeated frames never rewrite the same failure. Session
and the existing inspector/UI presentation pass through the checked save status.

The report is limited to 16 KiB and uses existing `Save Data` with the stable
`Viewer.Recovery.v1` key. Error codes are taken from the already latched session
before diagnostic resource queries. `export-viewer-recovery.ps1` owns native
envelope validation and readable local archival; the launcher starts its hidden
watcher. It verifies the SMD4 length/version/SHA-256, reads through atomic file
replacement, preserves valid backups, and deduplicates by application and payload.
Existing checked storage preserves only the latest and previous report until
export. This is handled-recovery evidence, not unhandled OS exception interception.

Validation lives in `ViewerDiagnosticsTests.smile` (the native hardening fixture)
and `scripts/test-viewer-recovery.ps1`. The hardening script also verifies the
actual saved native report after process exit. No language/runtime extension,
dependency, architecture threshold, baseline or guardrail exception is introduced.
Studio only receives the shared-source inventory entry; its workstream stays held.

This slice adds a 335-line diagnostics owner and 122-line focused test module.
Existing Workflow grows by 17 delegation/state lines; Session by one status field;
Inspector presentation by three net lines; UI by eight and the launcher by eleven.
Feature algorithms remain outside the startup program. Native Viewer and Sin Star I
builds, the hardening/architecture checks (including 58 graphics/input/audio checks),
checked native report readback/export, exporter/watch lifecycle checks and scoped
SMILE formatting pass. No crash reproduction or Web acceptance is claimed.

## Recovery-stage diagnostics

The existing session owner now captures failures immediately after the boss update
(25), additional Party equipment/water update (26), shared effect advance (27), and
before drawing (28). Previously an uncaptured late update failure could reach
`BeginScene`, clear the lower-level last-error values, then be labelled keyboard
stage 21 on the next frame. `ViewerRendering.DrawFrame` now returns immediately
for an already failed update, preserving the failure and avoiding an unclosed scene.
The native hardening fixture covers this demonstrated reporting gap. This improves
diagnosis; it is not a confirmed fix for Sin's intermittent Kael Party recovery.
Current evidence and the investigation limit are in the Party Beat checkpoint.

Ownership is unchanged: Workflow adds seven lifecycle-delegation lines, Rendering
adds five failure-boundary lines, and the existing regression fixture adds eight.
No compiler/runtime, dependency, state-owner, guardrail or baseline changes.

## Shared arena adoption

The normal camera and authored-shot camera now delegate roll-free orbit composition
to `ArenaCamera3D.ComposeOrbit`. This is the previous `ComposeShot` world-up spherical
policy, including its ±1.48-radian final elevation bound, extracted into the shared
library after Sin Star I exposed roll with diagonal base cameras. Viewer-specific
fit, cursor anchoring, close-up application and the authored-shot minimum FOV remain
with their existing callers. ViewerCamera loses 24 net lines; the library gains 42.
The architecture contract requires the shared call; no size baseline is raised.
Native `Arena3DTests` covers the reported angle regression (ten pre-fix failures,
67 passing checks afterward). Saved camera and pose formats remain unchanged.

The native Viewer now consumes `Smile.Simple3D.Arena3D`, `ArenaCamera3D` and
`ArenaBackdrop3D` as the shared visual/interaction authority. `ArenaViewport3D`
composes those owners for other applications. The library depends on no Viewer
module. Scene state owns its resources, camera controls, zoom and palette index.
ViewerCamera retains responsive fit, actor framing and calibration cursor anchoring;
ViewerInput classifies keys, and ViewerInspectorCommands applies game presentation
commands. No pose, socket, equipment or calibration values are changed.

F and G now toggle independent floor/grid visibility. B uses the shared palette.
The move gizmo uses E and rotation uses R, leaving G available during pose editing.
See [the arena contract](../../docs/libraries/arena3d.md) for reuse and asset staging.
The existing native hardening fixture and focused Arena3DTests cover these boundaries.
No architecture threshold, baseline, exclusion or dependency exception was added.


## Kael silver hair and separate native Fire Lab

September 20 alignment keeps music state in Sin Star I's `BackgroundMusic` module
and calibration conflict decisions in the existing PowerShell synchronizer. The
Fire Lab adapter reuses `ViewerActors.FaceToward` and shared profile opponent
coordinates to preserve the current world-axis calibration reference. Its native
test compares all nine wrist/equipment transforms against the Viewer baseline.
`Character3D` alone owns the expanded 30000% scale limit; `ViewerDragon` applies
Kael's 3× presentation scale and `ViewerParty` keeps bending casts at home.
No new dependency, entry-point algorithm or guardrail exception was introduced.

The canonical Kael exporter separates existing hair faces into a third skinned part
and applies a silver-gray material factor. Body stays at part 0, sword at part 1;
the same skeleton, clips, sockets and 48,996 total triangles are retained. Viewer,
Sin Star I and Water Lab still use the sixteen-clip model; Earth Lab uses thirteen.
Their callers need no changes. The new nineteen-clip Fire preview remains separate.

Fire Lab ownership and focused checks are documented in
`tools/AdvancedFireVfxLab/README.md`. Its new scene owner is 279 lines, Kael effect
owner 199 and Arin adapter 120. The existing 894-line sample/camera coordinator
gains 129 lines of routing, controls and safe diagnostic selection; feature logic
lives in those focused modules. `FireEmitter3D` grows by 12 lines for a separate
flow-aligned preset. Existing preset parameters, renderer resource budgets,
language/compiler contracts, dependencies and guardrails are unchanged. This is a
bounded addition to the legacy Lab, not a replacement monolithic controller.

## Native Kael Water adoption

`KaelWater` owns the caster adapter, frame and two audio cues (channels 12/13),
using a caller-owned `WaterVfx3D.Context`. `ViewerEffects` owns the solo context;
`ViewerDragon` owns the Party context. `ViewerParty` supplies the selected hero's
final transform and grounded head-derived body cylinder after boss animation update.
The Earth adapter remains the single sword-visibility writer; callers exclude
Water clips from its normal sword preference. Both families contact at 72%.

`WaterFlow3D.SurfacePoint` takes an optional scale and transforms its local geometry
back to world space; target dimensions and impact wrapping remain in world units.
`WaterVfx3D.Frame.FlowScale` defaults to one for existing callers. No renderer,
language, compiler, resource-budget or entry-point changes are required. Kael and
Mira own separate water resources and release them with their scene; no shared
mutable effect state is introduced.

`Profiles` defines all sixteen clips and normal → Earth → Water attack categories,
cycling all six bending skills in nine boss turns. Normal attacks alternate across
cycles. Both applications link the same implementation and stage the same canonical
water-named GLB/descriptor. The historical Earth GLB remains a preserved baseline.
Studio's source inventory is aligned only; Studio and Web work remain on hold.

Validation extends the existing native game fixture (actual demo and scheduler,
flight/contact, scale and cleanup), Water Lab geometry checks and Viewer hardening.
No size limit, baseline, exclusion, dependency or architecture guardrail is changed.
The adapter is 160 lines. Existing owners grow by 27 lines (`Profiles`), 25
(`ViewerParty`), 15 (`ViewerDragon`), 8 (`ViewerEffects`), 9 (`WaterFlow3D`) and
5 (`WaterVfx3D`); bootstrap files do not grow. These are bounded wiring and
geometry changes within their current responsibilities.

## Native Kael Earth casts and quiet Idle

`KaelEarth` is the focused adapter for Kael's clip time, static grounded scale,
target, temporary sword hiding and audio cues. The caller passes the desired
normal sword visibility, so leaving an Earth clip respects Weapon/W. The canonical
Kael package owns the actual baked body animation and its export/floor measurements.
`Smile.Simple3D.EarthVfx3D` owns only reusable rocks and GPU dust through a caller-owned
context and explicit frame input; it depends on no game, tool, actor or global clock.
Callers also supply elapsed playback time so GPU dust can finish fading after
the actor's clip clock stops. Normal loops and clip changes preserve the tail;
a paused backward seek clears it. Dust draws independently of the active rock
cast. The existing native textured-particle shader supplies the lifetime fade;
impact emission is a bounded burst with density in the actual density argument.

`ViewerEffects` owns the standalone adapter; `ViewerDragon` owns its Party instance.
Both preload and release their own effect resources. `ViewerParty` supplies its
existing target and updates Earth effects after advancing the boss actor. Its
impact helper aligns Earth hit/shield reaction to 72% of the cast; Volley has three
visual/audio contacts and the existing single Party hit resolves at the middle one.
Sword attacks retain their existing contact policy. `Profiles` appends three clips
and initially selected the five-attack cycle (superseded by Water adoption above). Sin Star I links these same owners; Studio only
keeps its source inventory aligned while development remains held.

The Earth Lab owns one actor/camera/arena/clock in `EarthLabScene`, controls in
`EarthLabUi`, and a thin startup loop. No entry-point implementation, runtime or
compiler feature, size threshold, baseline, exclusion or dependency rule changed.
Native socket-motion checks prevent effects passing while the body remains still;
the ground-debris check requires no ground stones after lift-off and preserves
airborne stones and target fragments. Dust regressions cover the held final pose,
Idle transition, pause and complete expiration. Existing native
Viewer hardening and game presentation fixtures cover integration and cleanup.
The shared game fixture advances the solo demo through all sixteen clips and
the real Party turn scheduler through nine successive boss attacks, verifying
all three Earth and all three Water casts without assigning the boss counter or selected clip.

The initial Earth slice added a 442-line shared effect, 167-line Kael adapter,
31-line Lab entry point, 267-line scene owner, 162-line UI owner and 168-line native
fixture. The dust/ground-clearing correction adds 13, 1, 0, 0, 0 and 55 lines to
those files respectively. Existing Party and standalone-effects owners gain nine
and seven lines; Workflow gains one elapsed-time delegation line. No executable entry
point, runtime, compiler, dependency or architecture guardrail changes. These
boundaries keep reusable rendering independent of character policy and controls.

## Shared native party status and Dragon roster

`ViewerParty` continues to own participant state and choreography. Zara's existing
Fourth context now also loads for native Party Dragon; readiness controls her actor,
equipment and water-recipient lifecycle independently of `UnityRoster`, which still
selects the Vrax/Kael boss choreography and camera policy. Dragon retains its fire,
claw and fatal-hit timing, with Zara added between Orin and Mira. No second scheduler
or actor pool is introduced. Standalone Dragon/Mira previews retain their rosters.

`ViewerInspectorPresentation.CapturePartyMember` reads each actor's live clip, state
and speed. Boss state is derived from its actual clip. `DrawPartyStatuses` presents
the same five rows in the Viewer and game; Workflow only delegates the host call.
Kael's profile and Party default are 200. Mira's presentation state resets with her
Idle animation at a Vrax/Kael stage boundary. Beat actor navigation and pose bookmarks
include Zara when loaded in Dragon Party.

Focused native checks cover the actual four-hero rosters, Zara's attack and KO/revival,
Mira's four recipients, actor selection, Kael's speed and all twelve game entries.
No entry-point algorithms, new mutable global state, dependencies, compiler/runtime
features, size-limit changes or guardrail exceptions are needed. Studio and Web
adoption remain on hold.

Net source growth for this slice: Party +73 lines, inspector presentation +115,
Workflow +18, profile +1, UI +4, Beat Editor +1 and Beat Sequence +1; the game
adapter adds one delegation call. Both executable entry points are unchanged.
The Party growth extends its existing participant paths; status layout stays in
the inspector owner, without moving feature algorithms into Workflow.

## Native Kael package and boss selection

Kael v1's canonical package owns body reduction, sword depth repair, Mixamo rig and
animation provenance, equipment fitting, grounding, GLB export and focused asset
validation. `Profiles` owns profile 14 and the ten-clip/ten-socket inventory; tab
identities 13 and 14 are Kael and Kael Party. `ViewerSession` routes tabs,
`ViewerUi` renders them, and the standard asset preparer stages the package.

The existing opponent owner, `ViewerDragon`, records the selected boss profile
and loads Kael with twice the solo fit scale. `ViewerParty` selects the boss's own
attacks, labels and contact distances while retaining the established Arin/Orin/
Zara/Mira scheduler. Vrax's node aim and effects remain conditional on Vrax.
`ViewerLifecycle` wires this selection during loading. No additional actor pool,
renderer, clock, calibration bank or game-specific battle scheduler is introduced.

`ViewerBeatSequence` gives Kael identity 7. The editor's tracks and head persistence
have eight entries, and every boss identity lookup includes the selected profile,
including the target label. Existing Dragon/Vrax storage names remain compatible.
Sin Star I appends two menu actions and hosts these same tabs. Its title layout
fits the additional character row above the help text.

Focused validation adds Kael profile/tab/attack/contact checks, independent camera
identity checks, and actual-model roster checks in the twelve-entry Sin Star I
presentation fixture. Existing native hardening architecture checks apply. Growth
stays in the current owners; no size limit, reviewed baseline, exclusion or
dependency guardrail is changed. Studio and all Web adoption remain on hold.

Kael growth review (net lines): Profiles +39, Party +27, opponent owner +24,
UI +26, lifecycle +10, session +6, Beat Editor +4, Beat Sequence +5, inspector +8
and workflow coordinator +1. Sin Star I's title owner grows by 11 lines and its
presentation adapter by six; both executable entry points remain unchanged.
The changes stay with metadata, actor selection, choreography or presentation;
no new mutable global owner or reverse dependency into an entry point is added.

## Sin Star I presentation host

Sin Star I's `CharacterPresentation` links this shared session and its existing
owners, with game-owned menu/status/Back UI. The optional `Start` flag `CycleAllClips`
selects individual character clip cycling even on the coordinated Mira/Dragon
profiles; the default remains unchanged for Viewer and Studio. The two simulation
entries use ordinary Party tab identities. `PresentationInput` exposes only camera,
pause, reflection and playback-speed actions; `PresentationCaption` reports the
current clip or battle turn. No editor save/import/control surface is drawn by the
game. Actor/clock/lighting/calibration ownership remains here, while Sin Star I
retains its own application storage namespace and packaged calibration defaults.
These are thin shared-session operations, not a second game battle scheduler.

The game host explicitly constructs a session during scene entry, displays the
standard loader, and prepares its first camera frame before drawing. The shared
rendering owner retries cache setup only for the character owner's acknowledged
renderer-reset signal; a scene restart must not inherit a fatal error from that
intentional reset. The ten-entry native game regression covers loading, first
draw, advancement and resource release. Existing native hardening guards remain
unchanged and pass.

Growth review: the game entry point remains 159 lines (+15); menu ownership stays
in its 356-line title module, and the new 130-line presentation module owns one
session. The existing reviewed workflow coordinator grows by 53 lines to 2,814
for hosting options/input/caption delegation. The rendering owner grows by eight
lines for reset handling. No threshold, baseline, exclusion or dependency rule
was raised. The module-declaration class initializer issue is tracked separately
in the language reference; game entry uses ordinary explicit construction.

## Native Yalis package and profile

Yalis remains an asset/profile addition, not a character-specific runtime subsystem.
Her package owns reduction, proxy-assisted Mixamo rigging, skin transfer, sword/grip
posing, keyed hair follow-through, grounding, portable export and validation.
`Profiles` owns her identity, ten-clip/ten-socket inventory, looping/hold rules,
equipment part and 133-unit equipped bind height. `ViewerSession` owns the stable tab
route, `ViewerUi` appends its button and derives hit bounds from the final tab, and
`Prepare-BuildAssets.ps1` stages the canonical GLB/descriptor into ignored cooking
inputs. Existing `ViewerActors`, playback, renderer and equipment visibility paths
load the result without a new actor pool, clock, physics service or calibration bank.

Sin Star I adds only its menu action and maps that action back to the shared Yalis
profile. The existing presentation fixture now opens, draws, advances and releases all
eight character entries plus both battle simulations. Viewer hardening directly checks
Yalis identity, profile height, ten clips, loop policy, sockets, equipment visibility,
tab routing and battle-opponent policy. Full-frame Blender and GLB round-trip checks
remain package-owned. Web publication and browser validation are explicitly deferred.

Growth stays in the existing owners: `Profiles` adds the Yalis metadata, clip and
socket branches; `ViewerSession` and `ViewerUi` add only routing/hit-area cases;
`Prepare-BuildAssets.ps1` adds one canonical staging entry; and Sin Star I adds one
menu action plus one presentation mapping. No workflow coordinator, renderer, actor
pool, effect service, guardrail, baseline or dependency exclusion changes for Yalis.

## Coordinated standalone battle previews

`ViewerBattlePreview` adapts the Mira and Dragon inspectors to `ViewerParty`'s
existing turn scheduler. Its state owns only activation, saved home framing and
presentation labels; it owns no actor, effect resource or clock. Within each call,
Mira's primary actor/water or Dragon's calibrated Arin opponent is temporarily bound
to the Party roles, then returned to its original owner before draw/input/lifecycle.
`ViewerParty.MiraDuo` supplies the bounded two-hero turn and recipient mapping; the
ordinary Party Dragon and Party Vrax rosters retain their existing paths. Dragon's
extra Mira is owned by `ViewerParty` and loaded/released by its normal actor lifecycle.

Workflow remains the coordinator: advance, update actors once, apply calibrated
transforms, resolve Dragon and water effects, then audio/draw. The Dragon primary
is advanced by the battle's Dragon owner; updating its Arin opponent skips the
additional-member update/draw so Mira is never advanced/drawn twice. Demo Off
restores companion homes and Idle while preserving the inspector's selected clip.
Space pauses both choreography and actor time. Arin and Orin calibration banks and
canonical assets are unchanged. Wider default framing admits the expanded roster.

`CalibrationTests.CheckCoordinatedPreviews` uses real assets to verify both rosters,
Arin attacks and turn handoff, all six Mira casts, simultaneous Attack/ThorAttack
with storm mode, exact Mira elapsed time, pause, Demo Off and ownership cleanup.
The same native fixture also checks both existing Party scenes and calibration
round trips. Growth stays within these responsibilities: a focused adapter,
small duo branches in the legacy Party coordinator and thin Workflow wiring;
no size threshold, baseline or dependency exclusion was changed.

## Native Mira and preserved comparisons

`Profiles` owns Mira/Mira1/Mira2/Mira3 identities, their shared nine clip names and hold/loop
policy, and each package's independent staff part. `ViewerSession` routes their stable
tab IDs; `ViewerUi` appends the tabs and
derives the tab-strip hit boundary from its last button. That removes the old fixed
510-pixel boundary which could not admit the comparison button. The existing
hardening fixture covers the new hit region. `Prepare-BuildAssets.ps1` stages the
self-contained MiraTripoV1/MiraV1/MiraV2/MiraV3 packages and the normal project asset cooker publishes them.
`INCLUDE_MIRA_COMPARISONS` is disabled in generated Web profiles while that adoption
is deferred. No new actor pool, calibration bank, game behavior or runtime dependency
was added. The rigid staff uses the same production skin and the existing equipment
visibility path. The comparison staffs have no equipment glow. Healer policy lives in
`MiraBattle` (cast timing and action labels); `MiraWater` maps final sockets, recipients
and clip time into caller-held WaterVfx3D and WaterStorm3D contexts.
It owns cast audio on channel 6 and contact audio on channel 8. ViewerParty owns Mira's named
actor/context and formation, loads the new Tripo Mira in both battles, applies policy samples to existing presentation states,
and routes her through both battle turn orders, targeting, draw/update and cleanup.
It does not borrow a calibration bank or advance another shared scene clock.
`CreateAdditionalParticipants`, `UpdateAdditionalEquipment` and
`DrawAdditionalEquipment` admit Mira independently of the optional Unity roster.
ViewerEffects owns a separate water context for every standalone Mira tab.
Targets are rebuilt from final actor positions each frame and cleared on destruction.
This prevents the four-member Vrax list leaking into the three-member Dragon scene.
`CalibrationTests.CheckMiraBattles` exercises real models, all six casts in both
battles, healing without boss damage, target-count cleanup, seek/restore and turn exit.
The existing Beat fixture verifies selection reaches Mira and both bosses.
Mira cycles Torrent, Heal One, Heal Party, Waterball, Tsunami and Tempest with Orin.
Healing classification uses the six-action policy in both live and sought playback.
Water contact occurs at 72 percent of the attack clip; sampled recoil returns to the
fresh boss pose each frame. Guard impacts use the boss timeline, including Mira's Hit
pose. The combo falls back to a wave if Orin is knocked out. The real-asset fixture switches
Vrax to Dragon and back to Arin after every cast family to protect that lifetime.
WaterVfx3D builds each barrier from one recipient's final position and the attacker
direction. ViewerParty maps the struck actor to that recipient's index; dragon
fire can ripple all the separate shields without creating a formation-sized dome.
WaterVfx3D owns their backward compression/recovery and the matching short-lived
droplet impulse, which fans radially and curls around the rim. It keeps that
displacement separate from the actor's grounded pose.
Sin removed Mira's added glow on September 16. MiraWater and WaterLabScene no longer
allocate, update or draw CharacterGlow3D contexts; the generic module remains
available to other callers. Sin Star I links the same MiraWater owner, so the body
aura, staff outline and head shimmer are absent there too. The Lab also removes
its cast-following point light. No model/package material or other actor changes.

Growth review: WaterVfx3D exceeds the 600-line review trigger because it owns the
shared staging/spray lifecycle and original seven bounded cast presets. The eighth,
Lab-only waterbending preset and the refined Torrent combat whip delegate their
centerlines and closed skin to the stateless WaterFlow3D module, which takes
precision vectors and scalar samples without importing
WaterVfx3D's Frame or Context. This keeps the dependency one-way without a new shared
state module. WaterVfx3D retains one caller-held context, no game/Viewer dependency,
and no hidden scene clock. Native water lighting is isolated in water_surface3d.h;
all attack contacts and struck-shield crowns delegate to stateless WaterImpact3D.
The eight water presets share Waterbending's clear material and sparse runoff.
The same context tracks cast/contact transitions and the 128-droplet burst budget,
shared across simultaneous shield hits. Denser preset surfaces and up to four
barrier crowns stay within the existing 3,168 points per ribbon batch; no new
effect context, hidden clock, renderer cap or scene-owned water state is introduced.
The native water_ribbon_normals3d.h helper computes area-weighted smooth normals
at ribbon commit and welds coincident seam vertices; reusable scratch belongs to
the ribbon batch and is freed with it. No character identity enters the renderer.
the existing native
renderer only admits the material, passes constants and binds the borrowed scene.
No hard size limit or reviewed legacy baseline was raised.
No Scene VFX pool, public calibration format or game rule changes are required by
the roster wiring. Original Mira water/chime cues are retained in the three comparison packages;
MiraV1 retains the original heal/attack cues; the new package preserves its own copy.
Shared water contact/wave cues and procedural textures live in TechnicalAssets/Generation3/Water. Tool-local audio copies
are disposable build mirrors.

Mira1's texture, seam weights, closed staff, cape hinge and portable animation keys
are asset-authoring responsibilities inside MiraV1. The Viewer consumes its exported
GLB and unchanged seven-socket descriptor. This repair adds no runtime skinning,
cloth simulation, model editor, calibration format or dependency. The existing
real-asset fixture covers new Mira selection in both battles and standalone water
ownership for all four tabs. It also checks the new staff's equipped bind bounds and
standalone/Party framing so the character cannot float from a negative bind minimum.
MiraTripoV1 owns its 35-bone rig, nine actions, 4K body/2K staff PBR bake, mesh repair,
cape clearance and floor measurements; the Viewer remains an asset consumer.

The new Mira tab also uses the existing Dragon arena and `ViewerParty.Companion`
slot for Arin, without enabling Party choreography. `ViewerLifecycle` loads and
places that companion, restores Mira's inspector calibration owner after loading
Arin's existing bank, and binds borrowed preview actors to the standalone `MiraWater`
context. `MiraWater` rebuilds final-position targets (Arin first for Heal One, then
Mira) and the Dragon Chest attack target each frame. Its existing destruction resets
the borrowed handles. Update/draw admit an actual ready companion independently of
Party mode; the existing calibrated update and destruction paths retain ownership.
Companion creation restores the primary effects profile. Dragon tracking selects a
valid body reference for Mira's two-part package while retaining Arin/Orin part two.
`CheckMiraPreview` covers all nine clips, calibration/effect ownership, target positions,
pause, cleanup and preserved solo comparisons; `CheckMiraBattles` retains both Party
routes. Standalone water consumes the actual clip name rather than the inspector
label: `HealOne`/`HealParty` drive the effect and sound even though the UI displays
spaces. The native fixture checks active heal surfaces at the middle of both clips.
This adds no runtime API, calibration format, actor pool or scene clock.
Growth is limited to existing owners: the workflow/rendering gates are replacements,
with small lifecycle, profile, target-adapter and companion changes. The existing
reviewed workflow exception is unchanged; no guardrail or baseline is raised.

Small valid triangles in Tripo Mira exposed an absolute area cutoff in the asset
cooker and native loader. Their existing geometry-validation owners now use a
normalized, Double-precision area test. The focused triangle-scale regression checks
small/normal/large valid imports and collapsed geometry rejection. This changes no
SM3D format or actor scale. Web runtime adoption remains explicitly on hold.

## Party Beat Camera Ownership

`BattleCameraShots` owns actor-independent Double reference frames, motion, linked
shot interpolation and bounded Save Data serialization. Linked shots interpolate
their target, distance and unit viewing direction independently. The direction follows
a stateless great-circle path; exactly opposing views use one deterministic perpendicular
plane, and the up vector is made orthogonal before native submission. Exact endpoints
remain unchanged. Shot-frame version 2 keeps
longitudinal distance unbounded in the horizontal plane but clamps its vertical
head-baseline influence to the attacker/target span. This preserves distant camera
placement without multiplying animated head-height changes. Version-1 shots within
a bounded longitudinal margin retain their original coordinate semantics; extreme
version-1 shots use the current built-in camera until an editor interaction captures
them in version 2. `ViewerBeatSequence` adapts
the existing Party timing/actors to explicit seek, remembers and restores actor
clips/times/modes and turn state, resolves Head sockets and performs head/body
picking with full actor bounds as a fallback. No character-specific pick offsets
or triangle-picking backend is introduced.
`BattleCameraTimeline` owns the four main camera timing weights, up to sixteen extra
shots, chronological navigation and versioned sequence serialization. It normalizes
camera intervals to the original battle duration; no action time is remapped.
Structural decoding remains source-independent. `EffectiveValid` applies one bounded
contract to the final rounded main and extra marker times for a particular action:
positive main intervals, 16-millisecond boundary/neighbor spacing, unique reachable
markers and the unchanged total duration. Insert, move and resize replace a draft only
after this check. A stored sequence that no longer fits changed action timing remains
on disk while playback and a newly opened draft use the four safe main markers.
`ViewerBeatTimeline` owns timeline gestures, frame stepping/repeat and presentation in
the normal animation timeline's bottom layout. Beat Preview hides the animation
timeline and closing Preview restores it; no second timeline panel is drawn.
`ViewerBeatHeadPersistence` owns each identity's current and last-persisted head
cuboid plus its independent pending/failure, retry and discard lifecycle.
`ViewerBeatEditor` owns selection, seven character identities, per-character sequence
drafts and clipboard, head-cuboid presentation/mutation routing, preview playback and reset controls. The
existing `ViewerWorkflow.Session` coordinates those operations; it retains the same
renderer, Party actor instances, calibration owners and main update/draw order.
`ViewerUi` keeps one dedicated Camera panel at the original lower-right location for
every standalone and Party tab. The upper inspector and Beat Editor use the shared
`Smile.UI.Controls` vertical-scroll operations, with a hover-only scrollbar for
overflowing content. The Beat Editor reuses the upper inspector background instead
of stacking another layer. The upper inspector stops above the separate Camera panel,
so all panel backgrounds preserve the shared 80-percent on-screen opacity.
`ViewerBeatEditor.ResolvePointerCoordinates` keeps raw logical viewport Y separate
from inspector-content Y. Head picking and every held drag sample use the raw coordinate;
only visibly clipped inspector controls use the scroll-adjusted coordinate.
During Beat Edit, workflow routing binds Camera sliders and actions to the active
Beat camera; right-click closes the draft and runs the existing current-tab reset.
`ViewerUi` also presents independent 0-200 percent
Weapon/Shield sliders for Arin, Orin, Valor and Zara; `ViewerEffects` owns those
session values and their profile-to-pair mapping.
`ViewerCamera.ComposeShot` composes input in an arbitrary saved camera's basis;
`ViewerInput.ClassifyBeatKey` and `ViewerPlayback` own key and pause integration.
The inspector binds the explicitly selected actor, independently of Party.Turn.

During editing the existing battle is paused and explicitly sampled with zero
animation update elapsed. Space advances a separate preview playhead through the
same action sampler; it does not retime the action or play audio. Continuous playback
retains equipment VFX history; discontinuous seeking invalidates it once.
Scrubbing does not advance logical events, counters or audio.
Save writes the current sequence in place without changing Beat Editor state. Cancel
or closing Preview restores the prior turn, actor clip times and camera/playback state.
Reset Beat clears the containing main camera in the draft. Reset All initializes a
fresh four-beat draft with default timing; no saved data changes until Save.
Reset All Beat Lengths changes only the four timing weights, retaining cameras and
extra shots at their relative beat positions.
`UseDefault` keeps unauthored/reset cameras as runtime policy fallbacks while the
editor displays a captured view; an actual camera edit adopts that view as a shot.
Head offsets/sizes auto-save independently of camera drafts. A successful camera save
clears only the camera draft; a failed head save retains a character-specific warning
through Preview close and actor navigation until Retry succeeds or Discard restores that
identity's last-persisted value. The standalone native loop defers X/Alt+F4 while this
recovery is pending, reusing `Session.HasEdits`; the existing Studio host receives the
same shared edit-state signal without a new Studio phase. Cancel still cancels only camera changes, and copying a
shot cannot replace another character's head definition. No asset rebake or new renderer is
involved. Verified canonical `head` bones are exposed as Head sockets in the
Valor/Zara/Vrax descriptors; all other socket and placement data is preserved.
Their profile inventories include that Head socket: Valor 15, Zara 11 and Vrax 5.
Standalone loading retains exact inventory validation; it must agree with the
canonical descriptor whenever attachment metadata changes. The real-asset
`CalibrationTests.CheckImportedProfileTabs` switches through all three standalone
tabs, checks their Head attachments and verifies rejection of mismatched inventories.

`BeatCameraTests` extends the existing hardening fixture with relative-frame motion,
moving-head vertical stability, versioned legacy fallback, cross-character copy,
connected boundaries, malformed data and real Save Data round trips, camera-only
retiming, extra markers, Space and reset draft isolation. Its isolated native application
identity also selectively fails one head record and one sequence record, verifies the
previous head bytes, then exercises retry, discard, reopening and identity isolation.
The larger sequence records exposed a native compiler stack-guard fault.
`MasmEmitter.AllocateStack` probes each page before allocating variable-size local
or call frames; `CheckLargeFrame` verifies real execution with Number/Double arrays
and a fractional argument. The refreshed compiler is included in the installed VSIX.
`CalibrationTests.CheckBeatSequence` seeks existing heroes/Dragon in
reverse and verifies restored clip names/times. The same fixture runs native and
generated Web for the initial editor; this timeline enhancement runs NativeOnly.
Web delivery and gesture validation remain explicitly on hold. Visible Viewer
checks cover the locally licensed Vrax route.
The Studio project merely lists the same shared source dependencies; no additional
Studio workspace or feature phase is introduced by this standalone Viewer slice.

The optional [Dragon retarget trial](../../games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV12VraxTrial/README.md)
uses a separate generated project/profile and private preview outputs. It preserves
the default Dragon package and provides an explicit Original launcher. The existing
`ViewerUi` animation grid pages every profile with more than nine clips, including
this 16-clip trial; paging is determined by clip count rather than import origin.

The local Unity import extends existing owners: `Profiles` supplies Valor, Zara
and Vrax identities, clips, scale policy and measured grounding; `ViewerActors`
applies transforms; `ViewerParty` owns two additional contexts, shared Party
choreography and independent Dragon/Vrax camera policies; `ViewerDragon` owns the selected Party
opponent while preserving Dragon
inspection. `Profiles.HasBattleOpponent` admits the four hero tabs without changing
inspection policy; `Profiles.UsesVraxOpponent` selects Vrax only for Valor/Zara and
supplies their doubled arena-camera distance. `ViewerLifecycle` applies those
policies before the shared actor/camera owners create the scene. `Profiles` also
distinguishes the shared `Party Dragon` and `Party Vrax` routes; `ViewerUi` owns
their eight-tab presentation. `ViewerParty.ActorPlaybackSpeed` owns independent
Party Vrax rates: Arin/Orin 150, Zara/Vrax 100. The Party panel routes each pair
of speed buttons to that actor, and the existing inspector borrows/restores its
rate through `ViewerPlayback`. Clip timing and VFX consume the same actor rate.
`Build.ps1` and
`Prepare-UnityAssets.ps1` validate and stage the permanent locally licensed roster
into ignored project-local cooking inputs. No parallel actor pool
or coordinator was introduced. Orin's effect target falls back to boss bounds when
a Chest socket is absent. Canonical package journeys own import/grounding and rights information.

Equipment extensions retain these owners: `Profiles` maps the imported weapon and
shield parts and glow appearance; `ViewerEffects` uses the same equipment routines
for primary and Party actors; `ViewerParty` owns independent effect state beside
Valor/Zara actor contexts. Only the main effect state initializes and advances the
shared scene clock and fire pool. `ArinShieldRim.Context` is caller-owned and accepts
the actor's part, socket prefix and color style, so Arin, Valor and Zara never share
mutable ribbon state. Their sockets are canonical package data. Native mesh draws
restore triangle-list topology after reflected ribbon draws.

`ArinShieldRim` acquires each complete texture/material/three-ribbon candidate
before publishing it to its caller. Failed candidates are released locally; the
same caller can retry on its next normal update. The isolated calibration fixture
proves native/generated-Web capacity recovery and independent owners without
private roster assets or pool changes.

`ViewerEffects.ZaraAttackEffects` owns Zara's weapon charge and four crimson
strikes, using Godstorm Ultra/Forked Judgment layouts from Lightning Lab.
Primary and Party instances call the same socket/clip-time controller, with no
extra scene clock. Canonical Zara sockets cover the entire weapon silhouette.
The existing native/generated-Web calibration fixture checks attack windows,
independent owners, freeze/resume, cuts and cleanup using synthetic attachments.
`ViewerParty.PARTY_VALOR_VISIBLE` temporarily removes Valor from Party Vrax's
rendering, effects, formation count and turn order without removing his package
or tab. Party Dragon uses the pre-Vrax shots; Party Vrax uses the requested close-up
and tracking sequence, then the later low frontal attack/impact/recovery composition.
The hero and boss align on the viewing axis so the hero stays fully visible and
Vrax dominates the frame. Logical impact timing is retained without the old wide cut.
The hero attack cut stores the complete camera pose/lens, choosing the horizontal
orbit angle behind the selected hero around the established arena target.
Impact/recovery reuse that pose unchanged, following Sin's final fixed-shot request.
The Vrax intro rotates position and up direction together around the vertical axis,
then settles into continuous slow revolutions. `ViewerCamera.AdvanceAutoOrbit`
supplies the shared phase; `ViewerParty.OrbitBattleShot` rotates each retained
battle composition at the same rate and direction. Vrax has close-up, moving
rear approach, complete-attack and frontal aftermath shots, without Beat 3a.
His Beats 3/4 use Sin's later planted-camera exception: a low fixed position
behind the selected defender, with a constant lens and a look-at target following
Vrax. There is no dolly or orbital translation during those two beats.
`VraxPosition` supplies both the actor and follow camera; body/head/effects aim at
the selected defender. The historical Dragon camera path remains independent.
Zara's shared 25-percent hit policy keeps her storm, camera and boss reaction
aligned. `BattleAudio.CrossedCue` drives original Unity sounds on channels 1/3 and
Lab thunder on channel 2; pause, seek and clip changes use its existing semantics.
The optional private audio wildcard is staged by `Prepare-UnityAssets.ps1` and
excluded by `Build.ps1 -PublicRoster`. No licensed audio is tracked in Git.
`ViewerDragon.UpdateVraxAudio` similarly shares the original Unity growl/swoosh/
death cue policy between `ViewerEffects.State.VraxAudio` for standalone inspection
and `ViewerDragon.State.VraxAudio` for the Party opponent. Its channels 4/5 and
caller state are independent of hero channels. The existing playback clock and
`BattleAudio.CrossedCue` handle speed, seek and one-shot triggering; pause and
teardown stop these channels. No new audio engine or scene clock was introduced.

Tab selection calls `ViewerLifecycle.BeginCharacterSwitchLoading` before releasing
interaction captures or invoking the existing lifecycle teardown. Its Boolean result
guards the switch; a failed presentation only sets `Session.LoadingFailed` for the
inspector notice and preserves the old session, actor, errors and epoch.
The shared native/Web startup owners reopen their existing presentation with
the artifact's original metadata. The first new `Show Screen` completes the
loading interval. No Viewer-local splash renderer or loading loop is introduced.

`ViewerDragon.Create` keeps all Vrax setup results, including required `VraxMouth`
aim admission, and sends every failed candidate through the common `Destroy` path.
Owned actors are released; borrowed actors remain alive while the wrapper clears
its handle and aim state. `HardeningTests.CheckDragonCreation` uses public skinned
fixtures for failure, repeated cleanup and a valid retry on native/generated Web.

Vrax attack effects remain in `ViewerDragon.AttackEffects`. The primary
`ViewerEffects.State` and Party's `ViewerDragon.State` each own a separate context.
Both call the same socket/clip-time update and cleanup operations. `ViewerParty`
selects the six attack clips through `Profiles.PartyAttackName`; `ViewerLifecycle`
initializes existing fire/lightning services for Vrax inspection. The scene clock,
lightning draw and reflection capture remain shared and advance only once per frame.
No coordinator changes or new pool are needed. `CalibrationTests.CheckVraxAttackEffects`
tests actual admission/lifecycle operations with synthetic points on native and
generated Web; the private model is needed only for local attachment inspection.

The [Studio design](../../docs/architecture/2026-09-09%20-%20SMILE%202.0%20Studio.md)
remains authoritative. [Studio S1](../SmileStudio/README.md) hosts the existing
Viewer workflow. Both entry points instantiate `ViewerWorkflow.Session`; neither
loads another executable, application loop or renderer.

## Preserved frame order

Double precision spans these owners without changing their frame order
or the accepted coordinator structure. `ViewerCamera` uses shared
`PrecisionCamera3D` controls and `Precision3D.Camera3D`; `ViewerParty` evaluates
continuous approach/return positions, while `ViewerActors` places the same
Character3D actors. `ViewerCalibration` and `ViewerGizmo` consume actual precise
socket/transform queries and retain integral saved channels. `ViewerEffects`,
`ArinShieldRim`, `OrinStorm`, FireEmitter3D and LightningVfx3D pass fractional
attachments/particles through the existing renderer's typed bridge. `ViewerRendering`
calls `Scene3D.BeginPrecise`. No new actor pool or renderer owns these values.

`CalibrationTests` contains
the real Party approach/return submission and elapsed-partition assertion;
`test-viewer-calibration-native.ps1 -IncludeWebPrecision` also runs it on Web.
The shared precision and VFX fixtures cover camera acceptance, fractional staging,
immutable capture and invalid writes. See `docs/libraries/precision3d-boundary.md`
for units, exact narrowing points and retained Number compatibility.

Equipment appearance is selected by `ViewerProfiles.EquipmentGlowStyle`, then
applied by `ViewerEffects.ConfigureEquipmentGlow` for primary and borrowed Party
equipment. Fire uses an aligned surface coating; Lightning uses a front-culled
expanded outline. `UpdateEquipmentGlow` owns that transform policy and
`ViewerCalibration` still applies the actor's saved equipment corrections.
`CalibrationTests.CheckEquipmentGlowAlignment` exercises both styles on both real
character models, across three calibrated poses and fractional actor placement.
No character identity or special offset is needed in the shared rendering operation.
`LightningVfx3D.WeaponTrail` owns bounded corona/trail emission from caller-supplied
precise edge points. The existing Orin storm contexts own its lifecycle and forward
hide/freeze/discontinuity decisions. Foundation tests cover independent arbitrary
segments; the real two-actor fixture covers ownership and native/Web GPU fallback.

The coordinator retains this dependency order:

1. Read queued keyboard input, route commands, then route UI pointer ownership before
   camera pointer handling.
2. Sample and clamp the shared frame clock. Feed raw time to frame-rate observation;
   use separate animation, camera, and presentation elapsed values.
3. Advance the sequence and Party choreography, then update the primary actor.
4. Apply presentation grounding, current-frame calibration, wrists, and equipment
   coupling.
5. Advance smooth zoom and auto-orbit; apply responsive fit, screen-space controls,
   close-up framing, and the calibration orbit anchor.
6. Place calibrated equipment and update equipment effects, companions, and Dragons.
7. Apply the current-pose Party camera and floor clearance.
8. Update audio, Orin storm state, and the once-per-scene VFX clock.
9. Update optional socket gizmos and current world bounds.
10. Begin the scene; draw floor/grid, actors, equipment glow, shared effects, optional
    gizmos, and end the scene.
11. Draw flash/UI overlays, present the frame, and test window closure.

`CaptureViewerFailure` remains immediately after the stages it identifies. The first
Viewer/renderer error is retained until explicit retry or reload.

The optional planar-reflection request is applied by `ViewerRendering` through shared
`Arena3D.ConfigureForFrame` immediately before the scene begins. The native/Web
Renderer3D owners replay the already accepted opaque/masked object snapshots under the
mirrored camera during `End3D`, followed by eligible transparent meshes and committed
CPU/GPU particles and ribbons when VFX reflections are enabled. Application code does
not draw or update an actor twice; capture never advances simulation or consumes new
effect admission. Reflected draws have separate diagnostics from main-scene draws.
The arena floor is the receiver. The flat screen-fixed backdrop is mapped from only the
region visible above the topmost projected receiver edge, preventing floor-covered image
content from being exposed as if it were new 3D scenery. Eligible 3D objects still use the
mirrored camera. The grid is excluded from capture and drawn above both the reflective and
original matte floor. Legacy simple-material meshes remain double-sided so the grid's
established winding renders on native and Web. Excluded gizmos and heat distortion stay
outside capture. Reflected effects use the reflection target's depth and floor clipping;
the backdrop must restore that target's depth attachment, including when the main target
uses a different resolution or MSAA count. Main-camera soft-intersection depth is not
sampled during reflection. `ViewerInspectorCommands` and `ViewerParty` provide typed UI
actions calling `ViewerRendering.ToggleFloorReflections` and `ToggleVfxReflections`.
Both default-on session preferences live only in `ViewerRendering.State`;
`ViewerInspectorPresentation` projects them and derives the Reflective/Original/Unavailable
floor label from shared renderer diagnostics. Generic Arena3D/Graphics3D callers retain
the compatible opaque-only default unless they explicitly request IncludeVfx.

## Current owners and maintenance routes

Calibration authority belongs to the JSON saved/exported by Sin from the Viewer,
stored in the named character's canonical package. The synchronization script owns
the ignored native receipt of the last synchronized JSON hash; binary Save Data is
a working copy. Content changes to the JSON take precedence over that working copy,
independent of timestamps. Unchanged exports do not rewrite the accepted JSON.
`test-arin-calibration.ps1` covers this conflict and subsequent editor saves;
`test-viewer-calibration-native.ps1` checks Arin's accepted frame-zero correction
channels and four socket matrices against the package's screenshot reference set.

| Owner | Responsibility / public operations | Focused checks |
|---|---|---|
| Standalone Program / Studio Program | Own window and one frame loop; choose input/overlay adapter and viewport | Native/Chrome launch and focus checks |
| ViewerWorkflow.Session | One private set of existing owners; Start, scoped input, UpdateFrame, DrawFrame, pose commands, Suspend/ResumePreview, Release | HardeningTests wiring; isolated Studio SessionTests |
| StudioShell | Chrome layout, workspace choice, inspector hit maps and close confirmation only | Native/Chrome resize, focus, dirty-close checks |
| ViewerSession / ViewerLifecycle | First failure, identity, load phases, reset/retry/switch/release/shutdown | HardeningTests; isolated real-asset CalibrationTests |
| ViewerTiming / ViewerPlayback | Clocks, clip selection/start, speed, pause/demo, timeline seeks/events | HardeningTests; real-actor playback |
| ViewerActors / Profiles | Context load/update/draw/destroy, facing, profile/asset/grounding policy | CalibrationTests; package validators |
| ViewerCamera / BattleCamera | PrecisionCamera3D pan/orbit/zoom, fit, anchors, composed camera | HardeningTests; precision fixtures and native/Chrome gestures |
| ViewerCalibration / CalibrationJson | Profile banks, strict codec, save/backup/recovery and transactions | Isolated persistence, malformed import and failed-write fixtures |
| ViewerCalibrationEditing / ViewerCalibrationControls | Edit/Undo/grip/gizmo mutation, explicit panel commands | CalibrationTests; disposable identities only |
| ViewerInput / ViewerTimelineEditing | Exclusive captures, repeat/seek/drag policy, timeline transactions | HardeningTests; queued modifiers and timeline fixtures |
| ViewerGizmo | Projection, hit/drag math and drawing; calibration owns mutation | HardeningTests; camera/drag observations |
| ViewerUi / ViewerInspectorPresentation / ViewerInspectorCommands | Hit maps, presentation, typed commands; no canonical asset ownership | HardeningTests; responsive UI and command routing |
| ViewerParty | Participant state, placement, choreography, inspector borrowing and camera beats | Party timing/formation/calibration and native/Chrome playback |
| ViewerEffects / OrinStorm / ArinShieldRim | Caller-owned equipment effects, continuity, budgets and shared scene clock | CalibrationTests; ActorIsolationTests; Fire/Lightning fixtures |
| ViewerDragon / DragonPresence / BattleAudio | Opponent lifecycle, animation, aim, effects and clip-time cues | HardeningTests; calibration effects/admission and audio checks |
| ViewerRendering | Arena/backdrop/lighting/socket resources and one ordered DrawFrame transaction | Frame-order guards; renderer reflection/material tests |
| Build / Prepare-BuildAssets / Prepare-UnityAssets / Launch | Canonical verification, disposable mirrors, publication and calibration sync | Preservation fixtures; native/Web and PublicRoster builds |

`ViewerWorkflow.smile` is the reviewed size exception: it contains the former
standalone coordinator once, including the existing standalone input/overlay
adapters. The extraction exposes real hosting boundaries (`Start`, `UpdateFrame`,
`DrawFrame`, `HostedInput`, explicit pose operations and `Release`), rather than
copying those procedures into Studio. The standalone entry point now consumes the
same session. Keeping the accepted ordered pipeline together avoids mechanical
file splitting; actor, calibration, rendering, camera, Party and effects behavior
still belongs to the typed owners above. StudioShell stores no such domain state.

StudioShell's layout/command module was reviewed at the 600-line size trigger.
Its state contains pane geometry, UI control selections and the close dialog;
the drawing routines follow those pane boundaries. It owns no actors, calibration,
animation clocks or renderer resources. Timeline transport and clip-position reads
use the existing playback owner. Planned Scene/Code/Visual and future workspaces
have no input handlers or hidden sessions. The permanent Studio project links the
same Viewer sources and stages only ignored asset mirrors.

Studio Viewer and Character Editor are two presentations of one session. A workspace
switch releases captures and keeps pending calibration edits. Stop silences channels
1–5 and freezes clocks without unloading the scene or autosaving. Resume primes the
existing clock so time away cannot become one large animation step. A dirty pose
blocks clip/tab/frame replacement; Save/Undo use the existing calibration owner.
Close Save must report persistence before exit; a failure retains the preview.
Native X/Alt+F4 use `Window_DeferClose`/`Window_CloseRequested`. Browser tab close
uses its standard unsaved-work confirmation; Studio's own Close shows its dialog.

`Graphics3D.SetViewport3D` selects one bounded logical rectangle on the existing
renderer, between frames. It does not allocate a second application or engine.
Viewport-local pointer coordinates and dimensions go to `ViewerCamera`; unfocused
or outside input releases captures. The host resets the rectangle before drawing
2D chrome. The existing native/Web reflection, backdrop and VFX paths use the same
viewport aspect; the lightning flash overlay is explicitly bounded too.

Immutable Character3D model/cache resources may be shared. Every live actor retains
independent pose, equipment visibility, calibration, playback and effects. Only one
scene clock advances shared VFX. Stop retains that session; actual close releases
its resources and audio. Official launchers close the other repo-owned host normally
before starting a replacement, and never force past a dirty-close confirmation.
Both hosts retain the same canonical calibration application identity and synchronizer.

## Validation routes

- `scripts/test-studio-session.ps1`: actual shared session, disposable Arin/Orin saves,
  denied atomic replacement, Stop/Resume, dirty switching and resource cleanup;
  native execution and Node logic checks, plus generated real-Chrome fixtures.
- `scripts/test-character-3d-viewer-hardening.ps1`: production owner assertions,
  frame order, native graphics/input/audio and generated-Web console parity.
- `scripts/test-viewer-calibration-native.ps1 -IncludeWebPrecision`: real actors,
  precise placement, lifecycle, effects, failed saves and isolated storage.
- `scripts/test-character-viewer-preservation.ps1`: launcher/synchronizer and
  canonical/publication protection using disposable paths.
- `scripts/test-character-3d-viewer-actor-isolation.ps1`: independent actor/effect contexts
  on native/Web, including forced fallback.
- `scripts/test-renderer3d-reflections.ps1`: frozen submissions, receiver/culling,
  allocation/fallback, toggles and resource cleanup.

Actual native/Chrome interaction is distinct from generated-Web logic fixtures.
Inspect the selected script's prerequisites before running private-asset checks.
Do not use live user calibration for failure tests.

## Selectable equipment presentation

- ViewerUi owns the three-style action and 0–200% Weapon/Shield controls;
  InspectorCommands changes ViewerEffects' four session intensity values and
  selected Orin style. InspectorPresentation only captures labels/values.
- ViewerEffects routes the same preferences to the primary actor and Party;
  OrinStorm contexts own two optional fire emitters and four idle neon effects.
  Style transitions destroy only that context's old attached resources. Charge
  and release retain their existing target/event/audio owners; returning to Blue
  Flame idle explicitly clears charge ribbons.
- Shared FireEmitter3D owns contour sampling, palette and world-space trails;
  LightningVfx3D owns closed outlines and short travelling arcs. SceneVfx3D still
  advances each family once. Precision3D borrows the existing socket matrix basis;
  Character3D retains the same actor/part pools and calibrated transforms.
- OrinV13/OrinEquipmentContours.smile is canonical measured presentation data,
  linked into Viewer and focused fixtures. It is separate from the accepted
  descriptor/cooked asset, preserving calibration fingerprints. The generator
  explicitly matches the cooker's Z conversion.
- Focused gates: FireEmitterTests (palette/contour/admission), Lightning foundation
  (closed triangle/edge transition/no sparks/in-flight), ActorIsolationTests
  (two actual actors, authored socket alignment, three styles, charge cleanup,
  independent release and native/Web forced fallback), HardeningTests (command
  routing and intensity bounds). The shared workflow retains the original coordinator order.

`CalibrationRoundTripTests.smile` is a focused companion injected by the existing
native isolation runner. It owns only complete-pose recovery assertions: all 20
channels plus wrist/equipment world matrices. Production state ownership remains
in ViewerCalibration and ViewerCalibrationEditing; no persistence code changed.

### Change-size review

- `NerisTown.smile`: 391 -> 512 lines.
- `NerisTownParty.smile`: 406 -> 480 lines.
- `NerisTownTrees.smile`: 201 -> 200 lines.
- `NativeViewerHost.smile`: 554 -> 554 lines.

New behavior owners remain bounded: Camera 215 lines, camera controls 119,
EntranceNavigation 79, Entrances 185 and Flowers 83. Generated site/placement
tables are data growth, not new runtime algorithms. The existing native renderer
changes by three net lines for opacity handling and frame capacity; no new global
feature state or bootstrap behavior was introduced. No guardrail exception applied.

The civic paving/bridge follow-through stays in the existing Blender plan and
builder (+2/+15 net source lines). Camera controls remain 119 lines; their only
changes are bottom-edge placement and the existing pitch range mapping. The route
fixture adds 18 lines to protect the two reported missing crossings. Regenerated
meshes and placement/collision tables add no runtime owner or dependency.

## Native town authoring ownership (September 26)

The [town-authoring contract](../../docs/architecture/town-authoring.md) is the current
map for the native editor. `TownEditorSession` owns one private town, pending save
snapshot and lifecycle state. Gesture state belongs to `TownEditor`/`TownSelection`;
control geometry belongs to `TownEditorPanel`; immutable catalog data has no behavior.
`TownCatalogRenderer`, `TownSurfaceRenderer` and `TownAttachments` own independent
GPU lifetimes. `TownDocumentNavigation` keeps the committed surface/item snapshot
and nearby camera bounds; `TownDocumentMap` rebuilds only when the document changes.
`TownDocumentStore` owns its codec buffer/candidate, `TownLibrary` recovery policy,
and `TownWorldDocument`/`TownWorldEditor` the bounded linked-map data and interaction.
No editor algorithms or persistence were added to the program or shared Viewer host.

Public routes are `BeginEditing`, `Input`, `Update`, `Draw`, `Overlay`, `Release`,
`FrameDocument` and `TakeSpawn`. The native host still delegates only to `NerisTown`.
Static town resources are destroyed before editable assets load; the actual party
and authoritative calibration contexts are retained. `NerisTownCamera.MovingGesture`
owns temporary walking pan/orbit and the one-second return. Movement axes remain
frozen for the held-key gesture and wheel zoom is preserved on camera return.

Size review: `NerisTown` 584 -> 738 lines is a reviewed coordination expansion:
load/draw/release branches, input routing, edited-map presentation and camera-mode
routing. It adds no document/mesh/codec algorithms. Camera policy is 265 -> 330,
party 496 -> 512. New session coordination is about 600 lines; store about 570 and
incremental surface renderer about 500. Generated catalog/feature/surface accessors
are data-only tables. Existing Viewer bootstrap/workflow modules did not grow.
The session's file-command branch owns coordination only; codecs, Blender rebuilding
and map editing remain separate. No guardrail threshold, baseline or exception changed.

Native mesh capacity is measured at 512 slots/4,096 submissions, with matching
nine-bit mesh handles; the full editor and party use 392 meshes/374 materials.
ByRef record frame storage in the native compiler changed from full-record space
to an eight-byte address, fixing the reproduced nested-call stack overflow without
changing record value semantics or ownership. `MasmEmitter` grows 13 net lines.
The editor adds no third-party or RPG library dependency. Shared precise vertex,
Unicode prompt/scalar and Shift/Delete additions remain focused existing boundaries.

Validation owners: `test-town-editor.ps1` (data, reused edited routes, real rendering,
party/calibration/camera/cleanup), `test-town-blender-save.py` (isolated actual save,
duplicate protection and Viewer snapshot), the normal native Viewer hardening gate,
compiler/text suites and the existing Neris scene fixture. Native visual review is
separate from these assertions. Studio and Web execution/adoption remain held.

Terrain correction: `TownSurfaceRenderer` restores the authored lawn height and
omits narrow shoreline trim; the Blender save owner mirrors that geometry. This
removes lawn/floor overlap at Royal Castle, Military HQ and bridge approaches.
Building transforms, surface documents, navigation and bridge rails are unchanged.
The surface renderer shrinks by 23 lines and the Blender worker by eight; the
native rendering fixture gains 24 lines and `test-town-terrain.py` measures actual
Blender floor/bridge clearance and cross-output height parity. No new production
owner, dependency, guardrail exception or coordinator growth is introduced.

### Town guides, history and duplication (September 27)

`TownEditor` owns a caller-held `TownHistory.State` and `TownGuides.State` alongside
its selection/gesture state. `TownHistory` stores a twenty-edit circular history;
per-step town/surface flags keep marker-only undo out of document persistence and
object-only undo out of terrain rebuilding. Live document/surface revisions stay
monotonic for their consumers; save identity and allocated item identities are
never rolled back. New/open document routes reset history and temporary guides.
The existing terrain-limit rollback resets history after rejecting an edit.

`TownGuides` owns bounded ground-anchored line/rectangle data, endpoint/segment/grid
snapping and projected overlay rendering. Grid rendering thins distant lines,
while snapping retains the selected interval. `TownSelection` owns duplication
resource preflight and group-origin snapping; clicking an off-grid item does not
move it until an actual drag begins. `TownEditorPanel` exposes the controls and
shortcut labels; `NerisTown` suppresses inspection movement during editor Ctrl
shortcuts. Save codecs and Blender contracts have no guide/history fields.

Growth review: Editor 475 -> 588 lines, Panel 263 -> 331, Selection 168 -> 265;
new Guides 300 and History 158. Session 599 -> 603 adds document/history reset
coordination to existing lifecycle paths. NerisTown 738 -> 739 routes camera controls
ahead of editor canvas gestures; panel hit regions exclude those controls. The native
entry point is unchanged. Dependencies point from editor/selection/panel to the
focused guide/history owners; neither reaches back into the session. No compiler,
runtime, public save format, threshold, baseline or dependency exception changed.

Validation: focused foundation checks cover bounded/branched history, grouped
duplication, grid/marker snapping, selection-without-drag, terrain restoration,
save identity and marker-only history. The real renderer fixture exercises the
buttons and guide overlay; the full native editor/session and Viewer hardening
gates pass. Native interaction and release relaunch complete the user-facing check.

Bridge orbit precision stays in `NerisTownCamera.Compose` (+3 lines). The Royal/HQ
bridge paving is only 0.62 native units (6.2 cm) above its support deck; the old
fixed near plane loses this separation in the 24-bit depth buffer at distant
overview angles. Near clipping now scales with eye height above town ground,
retaining the existing 25-unit minimum for close, ground-level shots. Position,
aim, movement, authored geometry and collision remain unchanged. The focused
inspection fixture checks nine distant depths against four depth-buffer bins
and verifies the ground-level Tab shot retains its original near clipping plane.
An isolated negative control using the old fixed near plane fails that exact
bridge assertion. The corrected native fixture passes; overview orbit, pan and
closer zoom inspection show distinct Royal/HQ bridge surfaces.

### Provisional Neris Castle M01 comparison

Sin explicitly requested an unapproved M01 blockout beside the two existing castles,
with an automatic party-proximity drawbridge. The native-only comparison is separate
from the immutable catalog and editable document. No save schema or fingerprint changes.

- NerisCastlePreview owns four display objects, two models and cleanup; it adds no ground mesh.
- NerisCastleRoute owns fixed comparison placement, route heights/collision and occupancy.
- ProximityDrawbridge is a pure reusable motion state: 3-second travel, 2.5-second clear delay.
- NerisTown coordinates lifetime/update/draw and an approach inspection button.
- NerisTownNavigation delegates only points inside the comparison's owned footprint.
- NerisTownTests and NerisTownSceneTests cover route/motion and actual native mesh transforms.

Dependencies run from the scene and navigation into the comparison owners, then into
shared Precision3D/PartyTrail3D; none point back into NerisTown or the host. Existing
party/calibration owners remain unchanged. The source asset package is
`games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM01V1`; its JavaScript preparation
validates part layout and the UV islands authored in disposable Blender export staging. It uses
installed Node built-ins, without packages or downloads. Build.ps1 only invokes it.

The new owners stay below 250 lines each. Scene growth is limited to coordination and
the preview button; no threshold, baseline, exclusion or dependency exception changes.
M02 and later modeling, Studio and Web adoption remain held.

The front ground and main road use normal native terrain cells in a versioned
town document. Existing TownSurfaceRenderer and TownDocumentNavigation own their
appearance and collision. The added ground mesh was removed after it overlapped
the existing road and visibly flickered. No duplicate ground surface remains.
All existing road cells, assemblies, lighting and catalog identity are preserved.
The previous live town records were backed up before applying the terrain-only
revision while the Viewer was closed. The rectangular west land retains a moat.
The visit action handles its header hit box separately from viewport blocking,
while respecting editor/modal capture. NativeProgram is unchanged.

The native fixture exposed insufficient bridge depth separation in the unedited
overview. NerisTownCamera changes one coefficient from 0.05 to 0.1, retaining the
25-unit close-view minimum. The existing nine-depth regression and ground-level
clip check both pass. No threshold or baseline was relaxed.

September 28 controls: Shift+middle pan is resolved by shared ArenaCamera3D.ResolveGesture,
including captured mode through Shift release. ViewerCamera delegates before its
calibration orbit logic, covering characters, Party and Battle; the town uses the
same resolver. Space descends and Shift + Space ascends through existing shared key support.
Alt has no camera binding; native Windows shortcuts remain unchanged.

The edited-town regression exposed a leaked 90-degree Top Down pitch override.
Normal orbit/framing commands now restore the standard 80-degree upper bound;
Top Down remains an explicit editor view. The town-session runner uses the prepared
Release asset mirror, matching the normal native build and Neris scene fixture;
its obsolete Debug mirror lacked the comparison castle and failed scene loading.
