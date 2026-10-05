# October 2026 architecture evidence

> Historical architecture evidence preserved from commit
> `3609132beb0949d36d9525c947d4c64ad14e7e5d` (October 5, 2026).
> This archive is not a current specification. "Current", "now", fixed counts,
> line-growth reviews and validation outcomes refer to their original milestones.
> Start with the [current architecture](../../ARCHITECTURE.md),
> [runtime contracts](../architecture-runtime.md) and [status](../status.md).

Original source material is retained, including superseded layouts, obsolete
owner boundaries and test debt, old native/Web/Studio claims, exact evidence paths,
measurements and past exceptions. The current native Studio is the Viewer in this
folder; separate Studio S1 is historical and Web work remains paused. Ignored
`artifacts/` paths are repository-root-relative local evidence, not a requirement
to recreate old test outputs. Package links use the local `games/SinStarI` junction
into the independent game repository. Consult current source before reusing a plan.

## October 5 Dragon arm, wing and recoil correction

Sin's visual review exposed a gap in the first v1.3 pass: hands shared the wing
chain, the spread arms still read as a T-pose, and Hit lacked staggered spring
recovery. The canonical package now has 36 deform bones: eight added upper-arm,
forearm, wrist and claw joints, with separate weights and corrected wing roots.
Its existing `build.py` still owns rigging/export and `animate.py` owns motion.
No runtime owner, state, dependency or public asset path changed. Profiles only
updates the candidate label to v1.3.1; the existing hardening assertion expects
36 bones. Neither engine file grows. No architecture exceptions changed.

Package authoring grows by 77 lines in animate, 57 in build, two in preview and
46 in validate. These remain focused at 256, 271, 85 and 141 lines respectively.
The asset regression failed on the old GLB's missing arm chains before correction.
It now measures actual hand/wing deformation relative to the chest, resting wrist
height, recoil peak delay and opposite-direction rebound, alongside foot contact
and loop checks. All 668 samples and the native hardening/calibration suite pass.
Studio was rebuilt with 401 verified assets and relaunched for native inspection.
The previous contact-only validation did not establish performance quality;
the package journey records that lesson and the corrected 720p motion review.

## October 5 Dragon full-body animation

The independent game's `SourceAssets/Bosses/RedDragon/RedDragonV13` owns the
editable Blender rig, original sources, eight performances, baked GLB, descriptor,
audio, validation and previews. `animate.py` owns motion; `build.py` owns rigging,
weights and export. Four paw bones and four two-bone IK chains replace the former
fixed-leg preview. Export removes authoring controls after baking all 28 deform
bones. Geometry, UVs, texture, accepted scale and six socket definitions survive.

`Profiles.smile` owns the eight-clip presentation and looping policy, appending
Walk/Run after the six existing combat slots. `Prepare-BuildAssets.ps1` mirrors
the canonical package; both Studio and the game project declare the same model.
Character3D still owns sampling; existing Dragon attack/VFX owners keep their
cue times. No runtime IK, compiler extension, entry-point logic or navigation
state was added. In-place gaits do not translate the actor through the arena.

Production growth is four net lines in Profiles and zero in asset preparation;
the project declaration changes two paths. Focused hardening grows seven SMILE
lines and ten net PowerShell lines. No limits, exclusions or exceptions changed.
The native hardening suite, calibration isolation, focused formatting and diff
checks pass. The package validates all 668 exported samples, floor clearance,
foot paths, planted contacts and exact loop seams. Native Studio was rebuilt,
relaunched, and reviewed on Dragon Walk/Run, attack poses/effects and Party Dragon.
Original angular wing topology remains an art limitation, recorded in the package.

## October 4 Metropolis daytime water reflections

`TownWaterAppearance.Configure` accepts the active map name and keeps the existing
planar reflection pass enabled in daylight for Neris Metropolis. The session passes
its document name alongside displayed intensity. Water tint and ripple settings
remain with their existing owners; production line counts and dependencies do not
grow. No renderer, persistence, asset or compiler changes are required.
Native evidence `metropolis-e9c74507cd3f4bd19b7e7c37c8107801` passes day/night
reflection switching, existing map-transition and POV checks, and town save/reopen.
`daytime-lake-reflections.png` records the native daytime lake view. Focused
formatting and diff checks pass; no limits, exclusions or exceptions changed.

## October 4 city lighting and party POV isolation

`TownEditorSession.ConfigureLighting` keeps reflection and shadow configuration
before `Scene3D.Begin`. `ApplyLocalLighting` remains with the session and runs from
`NerisTown.DrawContents` after each overview, photograph or POV accepts its camera.
Previously the lamp selector read the previous view's accepted camera before the
main frame began. The 83 ms POV refresh therefore switched the overview between
city-facing and party-facing lamp sets, causing visible nighttime flicker.

The existing Metropolis native regression compares all 128 light slots' type and
position before and immediately after normal and High FPS POV rendering. Evidence
`metropolis-e733a5b0ecc54f43afeaf74f1c92e714` reproduces both failures;
`metropolis-abd0928d5ab44b0a887d3d909cbdb138` passes with the fix and the existing
city, reflection and save/reopen checks. Focused formatting and diff checks pass.
The rebuilt Studio was also reviewed at night with a minimap party route, a
zoomed-out main view, and both normal and High FPS POV modes. The evidence folder
includes `party-pov-lighting.png`; the automated slot comparison is the flicker
regression proof, while the live review confirms the integrated views render.
The session grows by 13 lines and the scene by 8 (including formatting). No new
state, entry-point behavior, renderer limits, dependencies or exceptions were added.

## October 4 Sin Star I Battle System host

The independent Sin Star I game now embeds `NativeViewerHost.Session` for its
Battle System. The optional `NavigationEnabled` start parameter defaults to true
for Studio; the game disables Studio tabs and supplies its own title-return row.
Studio's header-free visibility state is retained, with Escape still returning
to the title when nested menus and edits are settled. Encounter rules, planning, cameras,
statistics, rewards and presentation remain with the existing battle owners.
`CanLeaveBattle` and `HeaderVisible` expose the UI boundary without mutable state.
The game retains the host across title visits for session EXP and owns its Viewer
lifetime, music and window-close deferral. No battle algorithms, renderer code,
asset formats or language features are copied or extended.

## October 4 all-map nighttime water and 112% moonlight

The earlier two-map selection is removed. `TownWaterAppearance.Configure` now
takes only displayed intensity; the existing native water-receiver selection
determines which surfaces receive reflections. `TownEditorSession` always passes
the displayed light intensity to terrain rendering, including demo lighting.
`TownSurfaceRenderer` updates both its standing-water material and its three
bounded flow materials; daytime colors and flow settings are retained.

`TownDocument.NIGHT_INTENSITY` owns the new 112% default. Night water reaches the
same deep tint at that intensity. Current canonical maps and live permanent,
named and recovery saves received a one-time intensity-only update with backups.
All other document fields and prepared bundle records were verified identical;
future saved night edits are not overridden at load. Existing generators use 112%.

Production growth stays in existing owners: document +1 line, surface renderer
+15, water appearance -8 and session -6. The existing native receiver owner adds
16 lines to select the largest flat water batch instead of rejecting maps with
raised pools at another height. Other heights retain screen-space reflections;
explicit mesh receiver validation is unchanged. No entry-point behavior, renderer
limit, compiler extension, persistence format or architectural exception was added.
Native regression covers default intensity, standing/flowing tint, day restoration,
Metropolis-to-Neris transitions and another named water map, Neris Canals.

Validation: `metropolis-f90a4eddb52f4aecbaf0a9306b4abfa7` passes those native
checks plus the existing city editor/save/reopen regression. The first Canals run
exposed the incompatible-water-height rejection; selecting the dominant water
surface fixes it. The existing native Renderer3D reflection contract, authored-map
geometry checks and focused formatting pass. Saved-map migration verifies all
unrelated decoded fields and prepared records remain identical, with local backups.

## October 4 shared Neris nighttime water

`TownWaterAppearance.Supports` selects Neris Town and Neris Metropolis for the
same dark nighttime tint and planar scene/light/VFX reflections. The session uses
that single policy for material tint as well as reflection configuration; the
terrain renderer still owns its water material. Daytime restores the blue water
and disables this reflection pass. Authored geometry, placements and lighting
presets are unchanged, and other maps keep their existing presentation.

The existing native Metropolis-to-Neris transition regression now checks actual
Neris night reflection activation, day deactivation and return to night. Ownership
remains in the existing water appearance module (+10 lines), with no net growth
in the session or terrain renderer, no new dependencies or entry-point changes,
and no renderer/compiler extension or guardrail exception.

Validation: `metropolis-6f91192ba21048ab922ff3ffb89e2280` passes the native
transition and day/night reflection checks alongside the existing city regression.
Focused formatting and diff checks pass; no limits, baselines or exclusions changed.

## October 4 five-building lake fireworks

`TownWaterfrontShow` keeps the existing four launch sites and adds rooftop emitters
on the five user-selected Burj-lake landmark identities. Each origin uses the current
assembly transform and roof bounds; independent 5–5.7 second periods and larger
bursts make this lake's display denser without adding global random launch sites.
Town revisions refresh moved/deleted attachments. Existing Marina/Petronas timing
is unchanged. Two immutable particle submissions keep both displays visible in the
shared water reflection pass: 2,304 existing particles plus 2,880 rooftop particles.

`TownSpireLight` now reserves 363 particles per actual spire instead of preallocating
4,096 for a single placed spire. The current city therefore reserves 5,547 particles
for these effects, below its previous 6,400. The shared 8,192-particle pool and all
renderer limits remain unchanged. Ownership stays in these existing focused modules;
no entry-point, renderer, compiler, map geometry or saved placement changes are needed.

The native regression checks all five roof attachments, move/delete behavior,
overlapping bursts, separate native submissions, available particle capacity,
night water reflection, and travel to Neris Town after releasing the city effects.
The fixture runner now extracts the document envelope when seeding internal save
storage from a prepared `.town`; production portable save/open remains tested separately.
Native evidence `metropolis-be5466e8bb5a47b0a7c495c562ea5206` passes, including the
existing editor/save/reopen/resource-release checks and a lake review photograph.
The photograph uses the existing bounded 640 × 384 capture size; an attempted
1280 × 720 diagnostic exceeded the cached-image save envelope's 1 MiB limit.
Focused style and diff checks pass. No architectural limits or exclusions changed.


## October 4 Neris Metropolis native delivery

The canonical package is `games/SinStarI/SourceAssets/Towns/Neris/NerisMetropolisV1`.
Its README records source authority, the 6,400 m map, 47 appended templates,
current user placements, export evidence and runtime presentation limitations.

`TownCatalogRenderer` owns template GPU lifetime/admission. `TownCityLighting`
owns shared facade overrides and nearest street lamps, delegating to focused
`TownSphereDisplay`, `TownSpireLight`, `TownWaterfrontShow` and `TownCityTraffic`.
`TownWaterAppearance` owns city night tint/reflection configuration. `TownLighting`
fits shadow coverage to map bounds and the tallest building. The session and
scene only wire these owners; NativeProgram is unchanged. Generated `TownCityData`
contains slot classifications. Shared `Fireworks3D` owns caller-clocked particles.

The native renderer resolves flat-water receivers in `reflection_receiver3d.h`
and composes rippled scene captures in `water_surface3d.h`. The new reusable
per-pixel material lives in `procedural_display3d.h`, exposed through Graphics3D
command 141. No grammar, SM3D format or Web implementation changes were made.

Demonstrated fixes include actual glass textures, separated water beds/bridge decks,
camera-ray picking, new-template Place Copy, city-wide shadow coverage and Blender
portable member names. A nighttime city-to-Neris transition reproduced exhausted
particle staging. City effects now release after outgoing model instances; four
firework sources reserve 2,304 particles. The shared 8,192 pool was not raised.
Map/tab/queue/preview capacity increases from 16 to 17 for the actual added map.
Original map storage keys, catalog IDs and fingerprint stay stable.

Validation: latest-layout `metropolis-0cc136aaedb04f80963e7218806741c7` covers actual
native draws/photos, all new template placements, drag cancellation, timing, four
localized launch sites, four Sphere pattern frames, prepared town/PNG save/reopen,
and real Neris Town travel/draw after night-city resource release. Native reflection
regression `metropolis-reflection-8b4897ea54ab4b1ab2d9155aaa5625c6` passes. Blender
round trip preserves 630 assemblies and 77,553 patches within 0.001211 native
transform units. Production publication has 401 verified runtime assets.
13 formatter integration checks and the 689-file repository style check pass.

Two early export fixture runs hit a too-short 20-second deadline. A bounded
120-second diagnostic completed preparation in about 30 seconds. The fixture now
allows 60 seconds; production work remains cooperative with visible progress.
No broad soak or Web validation was run. Persisted Blender MCP autostart is true
in a fresh background process. Character calibration JSON remained unchanged.

Growth review: catalog/features additions are generated data. Substantial behavior
lives in the focused owners above; existing coordinators add delegation/lifetime
and bounds logic. Extracting receivers reduces the native renderer's net size.
No baseline, exclusion or architecture threshold changed. Existing oversized
session/renderer owners remain legacy debt; no separate architecture checker
exists, so direct dependency/diff review and focused native tests are the evidence.

## October 3 21:00 review handoff: R04 partial, R05/R06 repaired

Reviewed baseline: `3feda1527fd1c61012fb0917673e0354c4eb1759`, matching the clean
checkout at intake. The Downloads ZIP's nine payload hashes were verified in an
ignored confined extraction directory. All seven numbered Markdown files were
read. The Water audio, Mira checksum and Fire assertion findings closed by
`1b80ef67` were not replayed.

R04 reproduced through the real native WithPreview=True queue: map B's document
was frozen while its mutable photograph generations were subsequently replaced.
The later file job could no longer find its old photograph and timed out. The
queue now captures verified image/input pairs in sixteen fixed request slots;
the transfer takes its own single-slot copy before preparation. Strong serialized
input comparison guards same-name/same-revision content mismatches. Native PNG
byte comparison proves the old queued image survives two newer photographs and
complete live-cache eviction, including the sixteen-map bound. Failed draw results
cannot publish a photograph. Cancellation frees the remaining queue while the
already-started transfer retains its owned pair. Capture limits remain 32.

`TownSaveQueue` owns request capture/identity and cancellation, `TownWorldPreviews`
owns frozen capture/input resources, and `TownFileJobs` owns preparation/transfers
and partial-write reporting. `TownDocument.ReleaseSnapshot` releases dynamic text
pages and invalidates fixed snapshot storage in place. A full-size local empty
document in cleanup caused a native stack overflow in the full session fixture;
in-place release fixes that counterexample without increasing stack budgets.
The scene and editor session only delegate success/cleanup. No renderer, graph,
clock or transfer algorithm was added to either coordinator or NativeProgram.

**Remaining R04 blocker:** cold request-snapshot rendering is not implemented.
The native fixture reproduces this case and labels it as a limitation, not a
successful cold export. A missing matching preview is rejected before file output.
`TownSurfaceRenderer.CacheTown` is shared build state, and landmark creation changes
global route transforms. Rendering the frozen town through the active editor would
violate camera/tab/navigation isolation. The next scoped repair must isolate those
existing owners for one bounded snapshot scene and validate cold maps while the
live document, camera, NPCs, lighting and recovery remain unchanged. This handoff
does not add a replacement renderer or silently switch the user's town.

R05's old stdout-only route predicate demonstrably accepted a native process that
printed PASS then exited 7. `Invoke-TownNativeCheck.ps1` now bounds route, scene,
and town-group execution and validates immediate exit status plus stdout/stderr.
Compiled native negative controls reject exit 7 after PASS, missing PASS, FAIL after
PASS, timeout, and an actual failed graphics draw after successful operations.
No source-model result substitutes for these executions.

R06 reproduced the documented old bootstrap failures once, then used current
authored import/preparation with read-only canonical publication assets. The
route test retains its legacy/static contract. The scene uses the accepted authored
Neris fixture, existing inspection checks, actual door metadata, Court transforms,
reload and complete native resource release. README maps each retired static-scene
assertion category to current coverage or explicit unsupported composition scope.
The old static-bootstrap debt recorded below is historical and superseded by this
authored scene result; production movement/rendering was not changed to mimic it.

Actual validation: native export fixture, native negative controls, repaired Neris
route/scene, and all four town integration groups pass. The session was rerun after
the cleanup correction. Arin's count was read from current canonical JSON (24), not
forced into assets. Reference JavaScript models were inspected but not executed or
reported as native validation. Historical arena/hardening results below were not
rerun because no shared native renderer, compiler or runtime changed.

Final evidence under ignored `artifacts/`: export regression
`tests/town-export-828733f53d944411b242b8134da2f5a9`, negative controls
`tests/neris-policy-ee4144b0cc1d4fe78d1e7ea132a62911`, and Neris route/scene
`tests/neris-town-b1f98be7f84b416385f8ccc51da8ccb4`. The four-group transcript
is `temp/codex-handoff/r04-native/final-town-editor.log`; the corrected final
session rerun is `final-session.log` beside it. The normal NativeProgram UI,
compiled with an isolated ApplicationId and a read-only copy of live saved data,
completed single Save for Viewer and Save All for Viewer (16/16). All seventeen
town bundles and prepared-record checksums verified with corresponding PNGs;
`ui-output-verification.json` records their hashes. Native regression assertions,
not that artifact-only check, prove immutable document/photo provenance.

The production Studio was rebuilt with all 155 model preparations served from
cache, its 367 published runtime assets verified, and the old process closed
gracefully. The standard native executable was replaced and relaunched through
Launch.ps1. No debugger, Blender recook, compiler rebuild or VSIX install was used.
Arin and Orin calibration export reported unchanged canonical JSON bytes.
Final SHA-256 comparison preserves all 1,819 protected canonical source files and
the sixteen October 3 19:40 Luma town/PNG pairs. Isolated tests created no production
records. The subsequent normal close/relaunch updated only the active Working
record/backup, Waterworks recovery/permanent backups, and the file-worker heartbeat;
the actual permanent maps, named recoveries and calibration records are unchanged.

Owner growth against the reviewed baseline: TownDocument 403→425 (+22), previews
288→362 (+74), queue 176→235 (+59), file jobs 797→806 (+9), editor session
1734→1736 (+2); NerisTown stays 1599 lines. The scene fixture shrinks 741→500.
New imports stay within document/preview/job responsibilities. No guardrail,
baseline, renderer budget, dependency or NativeProgram change was made. The large
legacy editor remains existing debt; this repair does not refactor it broadly.

## October 3 resident loading, authored connections and editor additions

Measured root cause: synchronous resident nearest-road searches cost 10-22ms
per NPC; the last map update added graph-to-town reconciliation costing 227ms on
the first Neris pass, changing revisions and invalidating navigation. Individual
model loads measured 2-6ms. `TownWorldTravel` now projects directed atlas edges
from yellow destinations and never changes towns. It reads one closed document
per frame and uses unsaved open documents. Legacy serialized graph links are
ignored. Automatic connection and exit-generation scripts are removed.

`TownNpcLayout` owns named positions/facing in the document. `TownNpcPreparation`
validates/grounds the immutable save snapshot. `TownNearestRoadSearch` owns the
legacy fallback, bounded by 2048 cells, eight collision checks or two milliseconds
per step. Authored entry needs no search. `TownResidents` loads one actor per
frame, retains identity independently from compact actor slots, and pauses NPC
movement in edit mode. `TownNpcTools` owns the palette/gestures; `TownSpawnMarker`
shares arrival/NPC circle rendering. TWN16 appends NPC data; old formats remain
readable. History restores NPCs. Blank documents and legacy decoding now clear prior camera, spawn,
rotation and link metadata; codec validation caught this existing state leak.

Demo activation is consumed after idle activation in the update path, preserving
the displayed camera/pivot and easing into the next orbit frame. `TownDemo` owns
per-town duration and lighting thirds. `TownCompass` projects four letters at
map edges, offset outward along each projected edge's normal so the complete
label stays twelve pixels clear of the border at any rotation or zoom. This
presentation-only placement remains in that owner and does not alter map bounds.
The panel owns permanent-map pagination and non-overlapping hint text.

`TownFileJobs` freezes a matching photograph before exporting the town snapshot.
`TownWorldPreviews` retains bounded 384 x 240 captures. The reusable native
`Graphics3D.ExportViewportCapturePng3D` starts a data-file job; Windows WIC encoding
and verification live in `graphics/viewport_png.cpp`. The existing worker and
atomic-replacement path publish the PNG. No compiler feature, external dependency,
Web implementation or entry-point algorithm is added. Town and PNG are separate
verified writes; PNG failure after town success is explicitly reported. Save As
resolves the photograph by the source map name.

Focused validation covers immutable authored edges, bounded search, saved/empty
NPC layouts, placement/facing/Undo, 720p panel bounds, Demo continuity/timing,
PNG decoding/dimensions and invalid-handle protection, codec compatibility and
the reported map geometry. The existing four native town groups and hardening
suite provide integration checks. NativeProgram is unchanged; no guardrail
threshold, baseline or exclusion is raised.

Final native validation: the four town groups pass (foundations, routes, rendering
and session), including PNG format/dimensions and the new top-view fit regression;
59 native hardening checks and 13 formatter integration checks pass. All sixteen
permanent maps were prepared and imported by the native runtime, and eight authored
geometry categories pass. The live editor saved a matching `.town` / 384 x 240 PNG,
showed the NPC portraits/spawn controls, and rendered rotated edge letters. Previous
permanent maps are retained as recovery entries before the explicit installation.

Live acceptance caught another camera defect: Fit updated eye distance while
retaining a zoomed orthographic height. Fit now updates that projection size too;
the regression projects both opposite map corners after fitting a close top view.
The manual check confirms a complete 1x map and correct cardinal letters after R.

Growth review against 0baa5bd2: the new production owners are NPC data 115 lines,
NPC preparation 90, bounded search 106, NPC tools 280, shared spawn drawing 44 and
map-edge letters 71. PNG encoding is 57 lines. Existing file-job orchestration
grows 102 lines; editor 58, document storage 59 and residents 45. World canvas
shrinks 114 lines and the editor session shrinks 15. These extend each owner's
existing responsibility; large legacy owners remain debt. No separate architecture
checker exists, so ownership, imports, entry-point growth and diff were reviewed
directly. At that checkpoint the older static Neris fixture's layout mismatch
remained; the later 21:00 handoff section records its authored-bootstrap repair.

## October 3 predictable magnification and authored initial camera

The reported zoom video exposed three interacting causes: every wheel event
picked a new target and reset the offset reference; inward and outward offsets
used different distance curves; picking/orbit could enlarge the far bound. The
slider displayed eye-to-target distance while its handle represented that offset.
Horizontal orbit also applied the opposite sign to the renderer's yaw convention.

`DistanceZoom3D` now owns bounded current/target distance, proportional 10% wheel
steps and monotonic easing. Reversing input cancels the pending opposite movement.
`ArenaViewport3D` routes distance input to this owner; `ArenaCamera3D` only composes
the explicit distance. The old town offset conversion and per-wheel cursor zoom
are removed. Character/battle FOV controls retain their separate existing contract.

`NerisTownInspectionLimits` owns the map/viewport fit (1x), one fixed viewing ray,
and its terrain contact bound. `TownDocumentNavigation.GroundRay` uses the same
height triangles as terrain, with the highest road/water/ground base layer as a
conservative clearance surface. A sky/void ray cannot zoom inward into empty space.
The near plane shrinks near contact and retains distant bridge depth precision.
`NerisTownCameraPanel` shows fit-distance/current-distance magnification and maps
horizontal yaw in the renderer's direction. Scene code only coordinates mode changes.

`TownDocument` owns the authored initial camera and its validation.
`TownStartingCamera` selects it, the existing editor captures it with Undo/Redo,
and `TownDocumentStore`/the Python codec preserve its twelve precise values in
TWN15. TWN1–14 remain readable. The camera has its own document revision change;
capturing a shot does not rebuild terrain. New loading/reset/idle orbit honors
the saved target; the explicit Orbit command still orbits the displayed shot.

Regression coverage replaces the discarded offset/accelerated-wheel contracts
with exact proportional steps, rapid reversal, repeated limit input, H Orbit,
top view and angled terrain contact. Existing follow, fly, pan, reset and bridge
checks remain. Preparation preserves the 1x eye and the first settled wheel notch
is exactly 1.10x; Tab and Fit are checked with input running before camera composition.
Fit initializes its new distance before the next pointer frame can adopt the old shot.
The live check also reproduced idle orbit replacing a zoomed ground target with
the map center. Idle orbit now keeps that zoomed shot and pitch; the regression
advances the idle timer at maximum zoom and checks unchanged framing/magnification,
including the mouse/key wake-up path.
Camera persistence covers native/Python round trips, per-town
isolation, invalid-camera rejection and Undo/Redo. No compiler/runtime extension,
dependency, architecture exclusion or limit increase is required.

Historical validation debt, subsequently repaired in the 21:00 handoff above:
the older `test-neris-town.ps1`
static-chunk scene fixture fails its arrival-start, authored-wall, reload timing,
arena rendering and moving Royal Court leaf assertions. An isolated build of
pre-change commit `eae26d6e` reproduces those failures. That fixture starts the
legacy scene without the current authored-document session; its failures cannot
be treated as evidence of a new zoom regression. The current `test-town-editor`
session exercises the accepted maps, walking, doors, map transitions and camera
regressions. Migrating the old fixture's bootstrap/bridge expectations is deferred
as a separate test-maintenance task; next action is to adopt the accepted document
fixtures before updating assertions. No test is disabled or reported as passing.

Validation: 95 shared-arena checks, 59 native hardening checks and 13 formatter
integration checks pass. All four town groups pass after focused session reruns,
including six terrain-contact rays per map in Neris, Willowstep and Silverfall.
Native/Python TWN15 camera round trips pass alongside legacy TWN7/9/11/13/14 data.
Native visual checks confirm the first 1.10x wheel step, draggable H Orbit,
initial-camera capture/Undo, maximum surface contact, unchanged pitch/magnification
after idle orbit starts, and Fit returning from maximum zoom to the whole map at 1x.
The legacy scene comparison adds no failure category relative to its baseline.
The repository-wide style check passes. The reusable distance owner is 93 lines;
the existing scene coordinator grows by eighteen net lines, camera composition shrinks
by 79, and the editor session adds ten query lines. No entry-point growth or
new mutable global state is introduced.

## October 3 map identity, arrivals and reusable fountain streams

`TownEditorSession` binds its accepted terrain rollback state to `LiveTown`.
`NerisTown` releases outgoing landmark resources before loading another town;
the session similarly releases outgoing fountain streams. A failed allocation
therefore cannot publish Neris's surface/style into Willowstep's document.
`TownTransitionTests` reproduces the original allocation failure, then verifies
Neris/Willowstep/Silverfall transitions and complete resource release. Campfires
release the shared fire material cache when no effect emitter remains active.

`FountainStreams3D` owns the shared parabolic jet/ripple geometry, extracted from
the Royal Court fountain. Callers supply transforms, dimensions, batch and clock.
`TownFountains` owns one exact-size batch for the eight nearest placed fountains;
`TownAttachments` delegates update/draw/release. No actor or bootstrap owns this
state. Static wire-like stream parts are omitted while basin/crystal meshes remain.

`TownGatewayJourney` computes the same inward, standable marker used by arrival
and editor drawing. `TownMapLoadTools` owns its revision-keyed display cache and
the Teleport Spawn gesture. `TownHistory` restores that authored point with Undo.
TWN14 appends optional spawn coordinates, landmark rotation and world-link stamp;
native and Python readers retain TWN1–13 compatibility. Fresh Neris defaults use
X=0, Z=-2780; decoding no longer injects an unauthored spawn. Gate arrivals take priority.

`TownWorldDocument` persists the last successfully saved/opened atlas.
`TownWorldTravel` now reads authored yellow destinations into atlas edges.
The former link-stamp reconciliation is removed; the atlas never edits towns.
Map loading and the editor call these focused owners; no graph algorithm moved
into the scene or application entry point.

The existing airport route modules own map-center rotation and inverse collision
transforms. Their previews, doors, traffic and lights use those public operations.
`town_rotation.py` rotates authored grid/curves/props/markers; the Blender landmark
owner applies the same transform to appended root objects. Source generation keeps
Neris Spaceport at 180 degrees and Horizon at 270 degrees (90 counterclockwise).
`town_design.ground_prop` embeds landform footprints into slopes; the source gate
preparer groups connected areas instead of averaging separate exits to one town.

Validation: the four native town groups pass, including arrival/Undo/save,
rotated entrance movement, terrain style and effect lifecycle checks. Silverfall's
prepared curved route/exit journeys pass. Native/Python TWN14 and repeated exit
preparation checks pass. Native hardening passes all 59 checks; normal publication
verifies 366 assets. Focused visual inspection includes both airports, the saved
atlas, Ancient Relay, Willowstep, Silverfall and the editor palette/controls.
The map installation retained Horizon's live edits and backed up every replaced key.

Growth review: `NerisTown` adds five net coordination lines; `TownEditorSession`
adds 42 for identity/lifecycle/arrival wiring. The shared fountain owner is 162
lines; placed fountains and world reconciliation are bounded focused modules.
NativeProgram and ViewerWorkflow are unchanged. No limits, baselines, exclusions,
dependencies or architecture exceptions were changed. The existing large editor
session remains legacy coordination debt; this task does not refactor it wholesale.
Fountain particles are bounded to eight nearby props; remote fountains retain
their static structure/water. Travel reconciliation never invents new road geometry.

## October 3 road closure, contour refinement and Spaceport walking

`SurfaceContourRefinement3D` owns a bounded caller-owned depth-first work stack.
It checks painted surface classifications at face centroids and edge midpoints,
subdividing only inaccurate partitions. Its maximum-depth classification removes
phantom water slivers without expanding output arrays or resource budgets.
`TownTerrainBuilder` feeds the resulting slices to `TownTerrainSeams`, which owns
vertical road/ground/water closure using the actual partition boundaries. Uniform
cells use the same closure even when top faces are merged into strips. Level walls
use one quad; slopes use two triangles. Two canonical triangles per cell avoid
the redundant flat fans that exhausted dense-map upload capacity during testing.

`TownSurfaceRenderer` tolerates invisible triangles collapsed by portable coordinate
rounding when choosing a vertical normal. Preparation 10/Terrain10 invalidates older
derived geometry. The Blender recipe reader now accepts versions 9 and 10; its prior
version ceiling incorrectly rejected the previous version 9 writer. Authored TWN
formats, terrain ownership, collision elevations and GPU budgets are unchanged.

`NerisSpaceportGround` owns the r09 model's bounded ground-level floor and obstacle
footprint in local metres. `NerisSpaceportRoute` retains transform, activation,
door and entry-event state; its Contains operation now claims actual deck coverage.
Surrounding ground and City Hall access fall through to the existing edited-town
navigation. The ground footprint is a conservative source-level proxy measured
from r09 geometry, not a general mesh collider or an upper-floor/lift system.

The existing `relief_landscape.py` authoring owner now creates a level symmetrical
Relief town; native preparation and installation still use the existing shared
owners. Only Relief's authored layout changes. The other fifteen maps retain their
authored bytes and receive regenerated mesh recipes.

Focused validation covers flat straight/curved road curbs, raised wet sides,
hidden painted features, fresh/cached GPU replacement, and four party members
crossing the old Spaceport boundary, both City Hall stairs, the main door, halls
and a hangar. Actual prepared-mesh audits cover all 16 maps against their analytic
surfaces. Relief's native route corridors and all retained exits pass. Full native
town foundations/routes/render/session and formatter regressions pass.
The rebuilt native Studio was relaunched after backing up replaced live saves.
Manual review confirmed the new Relief layout and closed Neris road edges at
party height, plus Willowstep's curved crossing/shore geometry. Native movement
regressions provide the Spaceport side-entry, hall, hangar and City Hall evidence;
this was not an exhaustive interactive walkthrough of every interior.

Growth review: `TownTerrainBuilder` decreases from 688 to 587 lines; the focused
seam owner has 292 lines and shared refinement has 185. `TownSurfaceRenderer`
remains 837 lines. The Spaceport route grows from 133 to 136 lines and delegates
its stateless footprint to the 103-line ground owner. Its regression is 152 lines.
No bootstrap algorithms, dependency cycles, mutable global owners, resource-limit
increases or guardrail exceptions were introduced. Existing renderer coordination
remains legacy debt; this change does not undertake a broader extraction.

## October 3 terrain contours, elevation and campfires

`TownStyleContours` partitions triangles by interpolated weights from existing
painted cells. `TownTerrainBuilder` subdivides only cells near material changes
before road/water clipping. Both grass/desert and mountain/grass edges become
continuous contours without changing authored cells, heights or collision. Flat
lake interiors merge into row rectangles; banks and flows retain terrain triangles.
Prepared version 9 / Terrain9 invalidates older derived geometry.

`TerrainWater3D` accepts quadratic water brushes and checks each sampled segment
for uphill flow. Each authored span retains uniform drift in its endpoint direction;
this is not a per-vertex flow shader. Willowstep authoring owns its three bends,
removed central spur and larger lake open to the south edge. Relief, Willowstep
and Silverfall have refreshed portable preparation.

`TownDocumentNavigation` now adds terrain elevation to bridge cells as it does
roads. Rendering already draped them; the former flat-deck exception dropped
actors and the camera beneath elevated stream crossings. `TownCollisionWorld`
also applies the rendered slope limit. Flat bridges remain traversable. Party
placement and final camera clearance reuse these owners unchanged.

`TownDocumentMap.Update` advances its double buffer during loading and normal town
updates, including while hidden. Drawing checks name/revision readiness and shows
preparation progress until the complete new image is ready.

`TownCampfires` owns eight nearest placed emitters using the shared BrazierFire
preset, resource admission and scene VFX clock. Identity retention avoids restarting
fires when their distance order changes; four emitter slots remain available.
Draw/POV replay does not advance simulation. Catalog drawing omits static flame
mesh parts, retaining the portable model for existing authoring exports. Distant
fires retain their stone/log bases and local lighting. `TownLighting` owns the
subtle flickering glow during daylight and night, and removes deleted prop lights.

Validation: `scripts/test-town-editor.ps1` passes foundations, routes, GPU and actual
Studio session checks. Regressions cover elevated bridge/camera height, four loaded
party members crossing both ways, minimap preparation without Draw, contour area
and height preservation, curved downhill flow, fresh/cached landscape water under
shared resource pressure, and fire animation/move/delete/light cleanup. Native
preparation/reimport and both wilderness maps' route/bridge-height checks pass.
The supplied video shows the previous Willowstep height drop to 23 at a crossing.
The rebuilt native Studio was also inspected directly: both Relief material edges,
Willowstep's curved river/open lake/removed spur, hidden-map preparation followed by
Silverfall's correct first visible minimap, animated flames and warm lighting, and
party travel across raised stream crossings in both wilderness maps. Arin stayed
on the displayed roads and the follow camera stayed above the ground.

Growth from 151176ff: contour owner 231 lines, campfire owner 209; terrain builder
605 to 688, minimap 552 to 583, editor session 1679 to 1697, Neris coordinator +1;
navigation/collision shrink by three lines combined. No new compiler/runtime
dependency, raised resource limit, architecture exception, or bootstrap algorithm.
Existing oversized coordinators remain architectural debt; ownership/diff review
and focused tests remain the available architecture checks.

## October 2 mountain refresh and editor interaction

Landscape shape/layout remains in StoryTownsV1 authoring helpers and the sixteen
canonical portable town documents. `relief_landscape.py` is the focused mixed-town
height/style owner; `terrain_quests` owns Willowstep and Silverfall. `road_end_markers`
squares edge approaches and spans the road with destination cells. Installation
checks baselines, backs up replaced keys and only replaces edited marker positions
when explicitly requested. Native gateway arrival behavior is reused unchanged.

`TownTerrainStyles` owns reusable Greyglass, green-slope and Snow atlas coordinates;
plain ground stays separate from exposed slope detail. `TownTerrainLighting` owns
an 8192-entry bounded normal cache reset for each terrain build. State belongs to
`TownSurfaceRenderer`, with exact-coordinate checks preserving computed normals.
TWN13 adds Snow; previous document formats remain readable. Prepared version 8 /
Terrain8 invalidates old geometry. No compiler/native runtime extension was needed.

`SurfaceContours3D` resolves the nonroad material beneath road junctions and
bisects each exposed shore segment. Its prior probe could still fall inside the
road and leave the reported pointed protrusions. `TownSurfaceRenderer` collects
water across recipe pages in a bounded 8192-patch CPU buffer, then uploads exact-size
ribbons grouped by flow. Triangles use three ribbon points; their collapsed ends
isolate adjacent faces without a redundant fourth point. Before replacement it checks
the shared point/batch budgets, retaining old water if admission fails. When two full
water copies cannot coexist, it releases its old ribbons and publishes the complete
replacement in the same update, after land and the CPU recipe are ready. The GPU test
reserves 20480 points for other Studio effects and exercises both fresh and cached
replacement of the actual Willowstep/Silverfall documents. The four ribbons and 8192
points per ribbon remain the existing maximum; runtime limits were not increased.

`TownMapLoads` owns connected-area selection, validated transforms and projected
labels. `TownMapLoadTools` owns move/resize gesture state; editor history records
one accepted change. `NerisTownCamera` owns north-up top view and the 700 ms return
to perspective. Shared arena owners retain target easing but no cadence-based wheel
acceleration. `TownFileDialog` renders the queued snapshot's current map name.
`TownDocumentMap` owns its 256-pixel four-sample cache, built eight rows per frame
and published together. It continues to use the existing 2D renderer.

Campfire (template 39/chunk 37) reuses catalog loading and local-light ownership.
`wilderness_campfire.py` authors the self-contained GLB; Blender export constructs
the matching assembly. `bridge_seam.py` trims the Royal deck at the island edge,
updates both catalog/GLB floor data and immutable Blender source checksums.

Validation: landscape-final-native.log (foundations, routes, GPU, actual Studio
session), landscape-v8-routes.log (all five authored landscape routes),
landscape-v8-arrivals.log (all sixteen maps), landscape-final-codec.log (native /
Python TWN11/13 compatibility), and landscape-v9-blender.log (actual Willowstep /
Relief export/reopen). Repository formatter tests/check pass. VSIX 2.0.69 was rebuilt
and installed; native publication validates 366 assets. UI evidence is in the
landscape-final screenshots under artifacts. The final native build opened Willowstep
in 487 ms and Silverfall in 459 ms. GUI Save All completed sixteen exports; every
file reopened with a unique map name and valid document/prepared checksums. The
current-map heading, smooth minimap and bridge threshold were inspected in Studio.
No Web work was resumed.

Growth review against b55bf923: focused new normal cache 81 lines; existing
map-load owner +210 and gesture owner +232 lines for their own behavior; minimap
+35 net. Renderer +135 covers texture integration and bounded water replacement;
camera +18, session +1. Bootstrap unchanged. No new
libraries, dependency cycles, architecture exceptions or raised guardrails. The
large existing editor/storage/session files remain pre-existing architectural debt.
There is no separate Viewer architecture-check script; ownership/diff review is
performed directly with the focused tests above.

## October 2 queued saves and continuous terrain surfaces

`TownSaveQueue` owns a fixed set of at most sixteen click-time document snapshots.
The editor session owns that state and feeds one job at a time to `TownFileJobs`.
The latter retains preparation, worker communication and progress ownership.
Batch jobs do not overwrite recovery from an older snapshot or open Explorer for
each map. Only matching name/revision snapshots receive a saved revision. No
queue logic or state was added to bootstrap. The session regression exercises
failure continuation, snapshot isolation, newer edits and actual portable reopen.

`TownTerrainBuilder` aligns static and flowing water at a common datum when flow
exists. Clipped bank slivers whose centroid falls outside a flow use their vertices
to resolve group ownership. This fixes both the 12 cm outlet gap and isolated flat
water vertices on elevated banks. Prepared version 7 / Terrain7 invalidates older
geometry; TWN12 authored data remains compatible. `TownSurfaceRenderer` computes
continuous height-sampled normals for land/roads while retaining vertical curb
normals. Collision and traversal still use the unchanged canonical triangles.

The Python prepared recipe reader exports plane-5 elevation vertices and retains
flow/style metadata. `TownDocument.SupportsBlender` explicitly distinguishes this
prepared path from the unsupported legacy unprepared export. Actual Blender
export/reopen compares every exported vertex and material, including elevated maps.

Focused evidence: artifacts/queue-session-v3.log, surface-final-render.log,
final-map-seams.log, final-codec-check.log and final-blender-acceptance.log.
Native Save All produced sixteen distinct Viewer files and sixteen Blender files,
with the application reporting successful completion. A rectangle paint gesture
and its undo were checked in Studio. Close-up native inspection confirmed the
stream/lake join is continuous. Greyglass's automatic stepped appearance layer
was removed while retaining its authored geometry, paths and placed objects.
No compiler/runtime capability, resource budget or architecture limit was changed.

Growth review against 5b1ee641: new style data owner 234 lines, style gesture owner
156, queue owner 172 and focused style regressions 168. Existing document storage
grows 90 lines for TWN12 encoding/decoding; renderer +80, terrain builder +43,
editor +52 and session +33 for their existing responsibilities. Bootstrap is
unchanged. The large existing storage/editor/session owners remain architectural
debt; this change adds no dependency cycles or reverse dependencies into bootstrap.
No separate Viewer architecture-check script exists; ownership and growth were
reviewed directly, alongside focused native checks and the repository formatter.

## October 2 local terrain-style ownership and Silverfall load repair

`TownSurfaceStyles` owns optional paged appearance data on `TownDocument` and its
section remapping. Zero inherits the map default; 1–4 explicitly select Meadow,
Forest, Highland or Desert. Immutable text pages share unchanged content across
undo snapshots. `TownStyleTools` owns the caller-supplied staged stroke and publishes
once on release. The existing editor delegates commands/history and the session
acknowledges an atomic terrain replacement or restores the previous appearance.
Neither owner depends on UI, renderer or bootstrap. No entry-point behavior moved.

`TownDocumentStore` and the matching Python codec use optional TWN12 RLE data.
Prepared bundles and derived terrain use version 7 / Terrain7 so older recipes
cannot supply missing material fields. The builder separates material boundaries;
the renderer reuses its existing thirteen material slots. Blender's existing mesh
export preserves mixed styles and prepared elevations/flow.
Whole-map undo previously omitted TerrainStyle; history now restores that field
and the local layer together. Whole-map styling resets the local layer explicitly.

Actual native viewing exposed Silverfall's enlarged lake exceeding four water
batches. Detailed terrain had emitted a redundant four-triangle fan per cell.
The builder now clips the two canonical 00-to-11 triangles already used by the
height sampler. The failing saved-map fixture now loads without increasing any
resource budget, renderer pool, architecture threshold or exclusion. The native
render test imports the actual canonical Silverfall document to protect this fix.

Focused validation includes TownStyleTests (explicit/inherited persistence,
recipe boundaries/cache invalidation, section remapping, gesture rollback, and
whole-map undo), native material uploads and actual Blender save/reopen. Logs:
artifacts/terrain-style-native-final-v3.log,
terrain-style-blender-acceptance.log and silverfall-render-native[-v2].log.
Native visual evidence and final builds are recorded in the current handoff.

## October 2 wilderness refinement ownership

`TownTerrainBuilder` now closes actual clipped ground/road boundaries with
vertical faces, including split edges at material junctions. Ground and roads
remain at their established levels. `TownElevationTests` measures the exposed
curb area; the test failed before the fix and passes afterward. No shared runtime
or shader changed. `TownSurfaceRenderer` disables directed-water crest foam and
uses a less saturated blue-green tint with subtle existing normal animation.

Terrain recipes use the `Terrain5` namespace and prepared bundle version 5,
so pre-fix cached geometry is rebuilt. Binary recipes and TWN11 authored format
are unchanged. Old terrain Blender export remains explicitly rejected.

Authoring helpers own broad quadratic trail turns and exact prop grounding.
Five wilderness maps use those turns; town streets are unchanged. Existing atlas
triggers are retained. The selective installer preserves user lighting and
terrain-style changes and rejects unrelated layout differences. Greyglass's ramp
shoulder was widened after a four-metre corridor test failed. Five prepared maps
now pass native round trips, sampled corridors, complete journeys, and routes
from spawn to retained travel triggers. Native overview acceptance follows.

Evidence: artifacts/terrain-refinement-native-tests.log (four native groups),
terrain-refinement-prepare-v9.log, terrain-refinement-routes-v9.log,
road-seams-before.log / road-seams-after.log, terrain-water-natural-native.png.
Water remains draped downhill geometry, not vertical free-fall simulation.
No architecture limits or capacities were raised.

## October 2 terrain ownership and acceptance

`TownDocument.Surface` owns immutable shared-corner elevations and authored water
metadata. Shared `TerrainHeights3D`, `TerrainSurface3D`, `TerrainRay3D`,
`TerrainSculpt3D`, `TerrainRamp3D`, `TerrainTraversal3D` and `TerrainWater3D` own
storage, one triangular geometry contract, picking, deformation, ramps, slope
eligibility and directed flow respectively. They do not depend on Viewer owners.

`TownTerrainTools` owns gesture timing, snapshot rollback and publication;
`TownTerrainProtection` checks footprints; `TownRampTools` and `TownWaterTools`
own their two-point interactions; `TownTerrainPanel` owns presentation. The
existing editor delegates inputs/history and the session acknowledges accepted
geometry. TerrainBuilder/Renderer retain their bounded recipe and atomic GPU swap.
No feature state or algorithm was added to NativeProgram/bootstrap.

The existing document codec, save preparation and derived cache own TWN11 and
Terrain4. Both native and Python exporters reject unsupported Blender terrain
before publishing a request or replacing a destination. Flat exports remain
supported. Graphics3D's bounded water-flow material operation is reusable; native
DirectX owns its shader parameters. No capacity, dependency or guardrail exemption
was added. Web adoption remains paused.

Focused tests: TownElevationTests (sampler/geometry), TownSculptTests
(transactions/timing/picking), TownRampTests (grades/corridors/routes), TownWaterTests
(bed/flow/groups), TownTerrainPersistenceTests (save A/live B/atomic rejection),
TownRenderTests (native geometry/resources), and TownSessionTests (four loaded
actors travelling both directions and eased camera clearance). They run through
the existing four-group `scripts/test-town-editor.ps1` harness.

Actual Studio acceptance found and fixed three defects: flat minimap click Y was
incorrectly included in elevated-road distance; the eased follow camera could
intersect a hill; and a sub-50 ms sculpt stroke discarded its elapsed time.
Regression tests now exercise those cases. The production Release/Debug builds
and installed VSIX 2.0.69 include the fixes.

Growth is responsibility-based: editor +132 lines delegates terrain gestures;
document store +134 handles the optional format; builder +140 handles displaced
triangles; renderer +103 manages bounded flow groups. Navigation and camera gain
58 and 11 lines for terrain grounding/clearance. Existing large owners remain
legacy review concerns; no limits/baselines/exclusions were changed. Focused new
owners avoid moving those responsibilities into bootstrap or a general manager.
See `docs/architecture/terrain-elevation.md` for memory budgets, acceptance matrix,
actual screenshot/log paths and explicit unsupported paths.

## October 2 minimap and initial loading follow-through

NerisTown's existing idle owner treats hovering the visible minimap as activity;
hidden maps still allow the usual idle orbit. Its minimap and dialogue input branches advance
ArenaViewport3D with pointer input blocked, so an active journey's camera transition
does not freeze while the cursor stays over the map or a resident is speaking.
Camera state remains arena-owned. Native regressions cover visible/hidden hover,
blocked-input easing and elapsed progress through the actual speaking input branch.

TownEditorSession batches existing catalog and terrain loading steps within four-
and three-millisecond deadlines, respectively, capped at eight steps per call. It
does not enlarge upload buffers, change geometry, or expose partial terrain. An
isolated prepared Neris document improved from 100 loading frames / 2654 ms to
37 frames / 2139 ms; this probe remains above the two-second target. The remaining
initial-load cost includes about 737 ms in party asset loading. Warm native Studio
reopening measured 150–164 ms. The next performance action is to profile initial
party uploads, not increase the loading-frame deadline (a six-ms experiment still
took 2092 ms and was reverted).

Native party focus exposed a separate black-scene defect: a near-wall clearance
could collapse eye and target, and ArenaViewport3D opened a scene after rejecting
the camera. BeginFrame now returns before opening that scene. NerisTown balances
every successful BeginFrame even if preceding lighting setup fails. ClearFocus
keeps a valid view direction, clears the interpolated boom, and seeds follow recovery
from that cleared shot. Ownership remains in the existing arena and town camera.

Validation: the zero-length-camera arena regression failed before the fix (check
73) and passes afterward; 76 shared arena checks, all four native town groups,
13 formatter integration checks, and the 646-file style check pass. Normal native
Debug and Release builds publish all 362 assets. VSIX 2.0.68 is installed with all
36 payload hashes verified. Logs: artifacts/focus-recovery-*.log. Actual native
Tab focus and minimap arrival keep rendering with the new camera; evidence:
artifacts/focus-recovery-native.png and minimap-follow-arrival-native.png.

Actual native acceptance also clicked Ilan after minimap arrival: Arin approached,
the dialogue opened, both faced one another and the three companions were hidden.
Manual coverage limits remain explicit: a controlled red-beacon on/off pair has not
been captured, and the automation API cannot synthesize Shift+middle drag. Native
fixtures cover resident picking/approach/dialogue/follower visibility, camera
easing, beacon timing and modified-middle input. Endpoint screenshots are not a
continuous path-fidelity recording. Recheck these gestures in focused acceptance.

The town test runner now imports portable fixtures through Data_BundleStart in its
unique test profile. Directly copying modern SMB1-appended files into plain Save
Data records incorrectly rejected them; no production save format is changed.
Production growth is local: NerisTown +16 lines, NerisTownCamera +36,
TownEditorSession +30 and ArenaViewport3D +8; regression fixtures and the portable
test importer account for the remaining code. No bootstrap, native runtime,
compiler, dependency, capacity, or guardrail growth is introduced.

## October 1 formatting follow-through

The sixteen previously recorded formatting failures below are resolved in a separate
format-only change. The transactional formatter and a diff review found whitespace
and outer condition parentheses only. The repository check passes all 646 tracked
SMILE sources; native town Foundations, Routes, Rendering and Session checks pass.
The rebuilt VSIX 2.0.67 is installed with all 36 payload hashes verified. Source growth
is 259 lines from formatting across sixteen existing files, chiefly expanded conditions
in NerisTownLayout. No ownership, dependency, capacity or guardrail change is involved.
These checks do not substitute for the remaining native interaction acceptance.

## October 1 Demo lighting and portable terrain repair

TownDemo owns the 10/20-second phase calculation using its existing 30-second clock.
TownLighting selects an alternate Sun without changing the document, saved presets,
revision or thumbnail identity. TownEditorSession applies directional light on phase
changes and supplies the same intensity to local lights. Ending Demo restores the
authored light. A manual day/night click still changes the authored preset and leaves
Demo running. Thumbnail capture waits until the authored-light phase. Countdown
presentation remains in TownViewportLayout: large digits only below the upper-right
camera panel. NerisTownTour continues the current bearing when easing into Demo.

TownDocument owns stable airport assembly identity. TWN10 adds that byte only where
the name no longer implies it; TWN1-9 remain readable and ordinary saves retain their
existing encoding/cache identity. Numeric legacy airport copies recover their known
assembly. Native landmark lifecycle, initial framing, spawn, protected-area queries
and Blender assembly selection consume the identity rather than the tab's title.

TownFileJobs retains a pending request after a delayed Blender startup, accepting the
worker's eventual verified success or failure instead of discarding it at 15 seconds.
The background worker's actual saved-file verification remains the success authority.
town_blender_terrain reads the immutable native prepared mesh recipe. Blender receives
the same contour triangles and walls instead of rebuilding curves as square grid cells.
Missing or unsupported curve recipes fail explicitly; they are never silently flattened.

Validation: all four native town groups pass, including exact Demo phase boundaries,
authored-light preservation, delayed Blender success, stable landmark round trips,
beacon timing and resource cleanup. The first new file-job fixture exceeded the native
stack with a large local state; that test-owned state now lives at fixture scope.
Its runner now reports an empty-output process failure without a secondary null error.
Actual Blender exports/reopens of Spaceport, Canals and the user's 21:59 Horizon copy
match every prepared vertex/material, retain lighting and preserve renamed assemblies.
Spaceport's smooth rings were also inspected in the Blender viewport. The normal Release
build (22:45:20 +08:00, VSIX 2.0.66) showed Canals at night with 19 seconds remaining
and restored daylight with 9 remaining, with orbit active in both captures. A real
Save For Blender of Star Lake completed at 100%; the worker saved and reopened request
90 successfully. Crown's inward road stubs are trimmed, and Orin has four center ponds
with east/west roads joining the outside of the round neighborhoods. Both were viewed
in native Studio. Three refined maps also passed thirteen directed gateway routes.

Release and Debug builds publish 362 assets each. All 21 changed SMILE files pass the
formatter check; thirteen formatter integration checks pass. The whole-repository
format check still reports the sixteen previously recorded unrelated files. No full
smoke-suite success is claimed. Changed owners grow within their existing responsibilities:
TownDemo +11, TownLighting +39, Session +30, FileJobs -4, Document +60, Store +48,
Landmarks +5, SpaceportGlow +135, ViewportLayout +3, and the new Python recipe adapter
is 64 lines. NativeProgram/NativeViewerHost and ViewerWorkflow have no growth.

Ownership remains within the existing feature owners; no bootstrap growth, new package,
capacity increase, guardrail exception or architecture baseline change is required.

Additional native acceptance: the user's 21:59 Horizon Airport file opens as
Horizon Airport 2 with its terminal, runway, aircraft and edited lighting intact.
Spaceport's Demo night phase visibly lights the pads and aprons. Manual day/night
clicks preserve Demo and orbit; captures show the countdown continuing. The red
beacon's phase is covered by the native timing fixture; a controlled visual blink
pair has not been captured.

## October 1 persistent thumbnail ownership

TownWorldPreviews owns live photograph handles, per-revision validation and the
two-generation disk cache. TownDocumentStore compares exact serialized inputs
using one bounded reusable scratch buffer; document formats remain unchanged.
TownTabs checks inactive document caches before selecting a map for generation.
TownMapPicker owns Refresh/Refresh All hit targets, missing-image status and the
opaque backdrop. NerisTown suppresses ordinary loading imagery behind a modal.
NativeProgram and NativeViewerHost remain unchanged.

Graphics3D's two native capture save/load delegates use text opcodes 13/14. The
existing viewport owner admits/restores handles in its unchanged 32-slot registry.
viewport_capture_storage.cpp owns BGRA8 GPU readback/recreation and the bounded
SVP1 payload inside ordinary checksummed, atomic Save Data records. The payload
fits the existing one-MiB save limit; unsupported dimensions/backends fail without
changing the previous published cache. No external dependency or capacity growth.

Native renderer tests save a photograph, release its handle and reload/draw it
without another scene; missing and invalid handles return failure. Document tests
check exact matching plus lighting/layout invalidation. Picker tests distinguish
Refresh from Demo and map-open actions. Sin verified cached reopening and Refresh All.
Native relaunch reuse, single-card refresh and lighting-edit invalidation were checked;
the single-card action changed only that map's cached generation.

## October 1 Demo timing, camera and footer

TownDemo derives whole seconds remaining from its existing 30-second clock; it
adds no second timer. TownViewportLayout owns the small countdown above Demo.
TownEditorSession captures the displayed camera when Demo is enabled by its button
or the two-minute inactivity clock. NerisTown consumes that one-shot pose and uses ArenaCamera3D's existing
one-second transition to the overview. Loading a different included map cannot
overwrite the captured starting pose. Startup and ordinary resets retain their
existing immediate overview behavior. No runtime, compiler or save format change.

TownDemo also owns the 15-second statistics visibility decision and idle elapsed
time. Session eligibility postpones inactivity restart during editing, dialogs and
gestures; NerisTown supplies actual input and conversation activity. The idle clock
never advances while Demo runs. StudioBuildStamp draws plain artifact metadata
under the existing right-hand toolbar without changing button positions.

Focused native checks cover countdown boundaries, disabled state, restart, the
unchanged first camera frame, an intermediate frame, completion and orbit state.
Foundations, Routes, Rendering and Session checks pass. The new sequence exposed
an older NPC picking test's invalid inherited near plane; its close-up fixture
now sets NearPlane explicitly. Production picking behavior is unchanged.
Native checks also cover the exact idle threshold, input/blocked-state resets,
statistics boundaries and map-turn reset. Normal Release and Debug builds and all
four native town groups pass with idle/statistics/footer changes. In Release, direct button interaction
showed an intermediate camera pose, the completed overview and the advancing
countdown; opening Maps stopped Demo and hid the countdown. The final Release
shows statistics at Demo start and outside Demo, hides them during the second
half of a turn, and displays the plain lower-right footer below unchanged buttons.
Build.ps1's explicit source inventory now includes the existing StudioBuildStamp
module, correcting the script-only preflight failure after the footer addition.

Growth review: TownDemo +49 lines, TownViewportLayout +14, TownEditorSession +57
net lines, NerisTown +10 and StudioBuildStamp -3. These extend the existing clock, toolbar, session
and camera owners; NativeProgram and NativeViewerHost do not grow. No guardrail,
baseline, capacity or dependency exception changed.

## October 1 build identity and native save-store repair

Windows inherited packaged-host LocalAppData virtualization split Studio's saves
between the ordinary desktop and Codex, despite identical executables and logical
paths. The ordinary desktop recovered four maps; the redirected store recovered
all fourteen. TownTabs consequently omitted Maps when there was no tab overflow.
Both legacy stores were backed up, compared by content and retained. The recovered
Neris layout matches Sin's October 1 11:57 export; canonical Arin and Orin calibration
exports remain byte-identical. No factory map replacement or asset rollback occurred.

`Smile.NativeRuntime/storage_location.c` owns the shared Saved Games known-folder
lookup and non-overwriting, missing-save legacy import for scalar and structured
saves. `scripts/get-smile-data-root.ps1` provides the same lookup to support scripts.
Application identities, keys, save envelopes and recovery formats stay unchanged.
Existing native binaries require recompilation. A canonical backup prevents an
older legacy primary from taking precedence. No routine launch merges conflicts
by timestamp or deletes old saves.

`Build_Info()` reuses compiler-owned StartupBuildMetadata. StudioBuildStamp owns
only presentation (22 lines); NativeViewerHost adds one import and one draw call
(three lines). NativeProgram is unchanged. The native runtime shrinks by eight net
lines and delegates folder policy to a 46-line helper. CompilerProgress owns phase
and elapsed-time reporting; CompilerOutput drains both VSIX process pipes as they
arrive. Existing build cancellation/timeout ownership stays in SmileBuildService.
No capacity, baseline, architecture exemption or external dependency changes.

Evidence: 329 language/compiler tests, 13 formatter integration checks, native
save-status, migration/no-overwrite/backup-authority, recovery-export and town-worker
regressions pass. Installed VSIX 2.0.65 verifies all 36 payload hashes. An ordinary
desktop process and Codex both read fourteen maps. Visual Studio Release was built
and launched through its actual UI: Maps, the current East Valley star layout and
the bottom build stamp were directly observed. Debug was then built and launched
through the same Visual Studio UI: the full Spaceport, fourteen-map chooser and
current East Valley were directly observed, with VSIX 2.0.65 and its own compile
timestamp. Compiler heartbeat lines were visible during the active Debug build.
The 59 native Viewer hardening checks and surrounding release/preparation gates pass.

Known check failure: repository-wide `format-smile-style.ps1 -Check -FormatLongIf`
reports 16 pre-existing files outside this fix (SurfaceGrid3D; BattlePlanning,
BattlePresentation, BattleUi; NerisTownDistricts/EntranceNavigation/Entrances/Layout;
TownDecks/MapLoads/PartyInset/Selection/TerrainStyles/WorldCanvas/WorldDocument/
WorldEditor). Focused changed-source checks pass. Evidence is
`artifacts/build-stamp-style.log`; normalize these files in a separate formatting
change, then rerun the repository check. This is not a full smoke-suite pass.

## October 1 Neris orbital spacecraft

The spaceport now has three compact shuttles across its two hangar aprons and an
occasional medium transport on the marked central approach apron. All keep their
noses facing away from the terminal. Shuttles reverse into the open hangars,
emerge nose first and use staggered vertical takeoffs and landings. The original
four alien visitors and Horizon aircraft are unchanged.

NerisOrbitalFlight owns deterministic timing/positions; NerisOrbitalFleet owns two
models and 16 instances. NerisSpaceportPreview delegates their lifecycle. The
versioned Spacecraft package owns source geometry and checked exports. The Blender
landmark owner places the same craft at matching parked positions. No bootstrap,
shared runtime capacity, dependency or architecture exemption changes.

Native foundations/routes/rendering/session checks pass, including parked positions,
reverse/forward hangar motion, vertical flight axes, visibility intervals, renderer
heading, resource release and the medium craft's apron height. The final transport
anchor is the authored compass centre at X -5500, Z -5640, replacing the incorrect
front-lawn placement reported by Sin. Sin confirmed its correct native landing
after the October 1 20:48:48 Release build. Three parked shuttles were also directly
inspected. Blender export checks report 122 source objects, 16 landing feet and
matching deck heights; both GLB checksums and part budgets pass. Full-cycle visual
observation was not performed; phase/trajectory evidence comes from native checks.
These are ambient craft, with no boarding, moving collision, animated landing gear
or hangar-door simulation.

Growth: the two focused orbital owners total 236 lines; Preview +11 net lines,
Blender landmark export +28, and two source/two asset declarations. Session checks
also contain the separate Demo regression. No existing source-size limit changed.

## October 1 spaceport paving

Spaceport r09 removes fine apron grid meshes and bakes retained gold approach
markings into filtered pavement. The asset package owns the source, texture and
portable/native exports; NerisSpaceportPreview still owns incremental resources,
now with 22 static parts. The Blender landmark exporter appends the same r09
source at the shared road/structure height. Both castle sources are unchanged.
No map, language, runtime capacity or bootstrap behavior changes are required.

Validation: the full 360-asset native build and town Foundations, Routes, Rendering
and Session checks pass, including 22 static parts and resource release. The
appended Blender source retains evaluated bounds and the packed paint texture.
Two-file style and diff checks pass. Preview changes four lines, the existing
Blender landmark owner grows by 33 lines and the session test by one. No dependency
or guardrail exception. Moving-camera visual acceptance remains pending.

## October 1 East Valley star landscape

The existing village layout author owns the six-point star, concentric promenades,
six satellite gardens and twelve central ponds. Twenty-four homes face radial
roads; the civic entrance ends at the inner ring. The map stays 420 metres wide.
The prepared portable document is generated by the normal native save owner;
no renderer, runtime capacity or map-format changes are required. The installer
adds an optional map-name filter so a single-map revision never rewrites other
towns. Existing preflight, user-lighting preservation and backup checks remain.

Validation: 25 entrances, connected paving, mirror symmetry and budget checks
pass; all 50 directed atlas routes and clear forward arrivals pass in native code.
Fresh-process import verifies the baked terrain/road bundle. The authored layout
preview and the latest star layout were inspected in native Studio, including
the actual Visual Studio Release and Debug launches during the build-store repair.

## October 1 immediate startup showcase

TownEditorSession retains ownership of the Demo clock and participation. Its
read-only DemoActive query lets NerisTown frame the overview on the first loaded
frame, including the factory fallback. NerisTown ignores Demo-cancel input while
the scene is loading; normal click/key cancellation starts when it is ready.
This prevents launch/loading input from ending the showcase before it appears.
NativeProgram and the host remain unchanged. Growth is eight scene lines, ten
session lines and a focused startup regression in TownSessionTests. Native town
Foundations, Routes, Rendering and Session checks passed, including loading input,
first-ready-frame orbit and subsequent cancellation. The complete 359-asset
publication built and was gracefully relaunched. Direct mouse/visual acceptance
remains pending; no compiler, runtime, asset limit or VSIX change was needed.


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

Resolved in VSIX 2.0.67: the isolated PickerMath probe originally showed a declared
Left variable reading the direction constant 12 instead of its assigned 94.
Shared parsing now leaves Left/Right as contextual names; ModuleProcessor resolves
the existing routine, program or module scope before falling back to constants.
Both emitters consume that shared bound tree without private resolution rules.
ContextualIdentifierTests cover declaration reads, parameters, arrays, scalar types,
constants, module isolation and hover/definition lookup. All 333 language checks,
13 formatter integration checks and four native town groups pass. A native proof
prints 94,94,95,12,13; the installed VSIX's 36 payload hashes match the build.
Parser grows one net line and Modules grows 22 within existing ownership; no new
production module, dependency, bootstrap behavior or guardrail exception is added.
The earlier ButtonX workaround remains a clear coordinate name and needs no churn.


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

## October 2 terrain journey maps and preparation repair

TownLibrary registers Willowstep Highlands and Silverfall Basin using the existing
sixteen-tab capacity. Authored TWN11 documents remain in StoryTownsV1; its
terrain_quests.py owns deterministic corner heights, clear routes and prop placement.
No terrain algorithm, asset ownership or map-specific state moved into startup code.

Actual native inspection found an obstructed default spawn; both maps now have
clear spawn plazas connected to their roads. Full native travel then exposed the
batch preparation script reusing generation 1 between documents, retaining the
previous road graph. prepare_maps.py now advances preparation and verification
generations. Production TownFileJobs already supplies distinct request IDs.
validate_terrain_quests.py imports fresh prepared bundles and exercises 27 complete
journeys, all 23 four-metre route corridors in both directions, every prop height
and downhill flow. It reproduced the bad cache before the fix and now passes.

repair_prepared_roads.py is a bounded authoring maintenance tool using the existing
native graph. It corrected eight of thirteen older portable maps; all authored
payloads and non-road companion records remained byte-identical. Current live
version-3 bundle bindings are rejected by the terrain runtime, so existing user
saves and locally rebuilt navigation were untouched. New-map installation backed
up replaced records and checked for concurrent/newer geometry.

Growth: TownLibrary +4 net lines; two focused Python authoring/acceptance owners
under 200 lines each, one 100-line cache-repair utility, and generation handling
in the existing preparer. No bootstrap growth, dependencies, capacities, formats,
thresholds or exclusions changed. Release and Debug builds pass with 362 assets;
focused TownLibrary formatting and Python syntax checks pass. Logs use
artifacts/terrain-quests-v5-* and terrain-existing-road-repair.log. These maps
reserve encounter spaces but do not add quests, enemies or free-fall water physics.
