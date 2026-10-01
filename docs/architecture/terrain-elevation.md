# Terrain, ramps and following water

## Phase 0 audit — October 2, 2026

Implementation baseline is main d28b7877, preserving subsequent work, maps, both
castles and accepted Arin/Orin calibration. The reference commit in the handoff is
not a rollback target. The newest Downloads package is the October 1 1418 ZIP,
SHA256 77EA5F5B84D300C06137198D7803BFE01B212D04F1DD2F163BFF68AA071787CE.
The non-overwriting extraction is artifacts/temp/codex-handoff/terrain-20261001-152406-589de740;
SHA256SUMS matches the fully read numbered brief (E2B0995BA1483C2226584347AF7BDBF6B0BECD0B9085984D61F2113D7AA6ECA4).

Current authority: SurfaceGrid3D owns nonuniform X/Z edges, packed materials and
ordered analytic brushes. TownDocument owns that surface. History holds 21
snapshots, tabs hold 16 documents, and renderer/collision/save preparation each
retain committed snapshots. TownTerrainBuilder produces bounded CPU recipes;
TownSurfaceRenderer swaps completed GPU geometry. There is no second terrain
engine. Ten native units equal one metre, from TownSurfaceLayers.json. Legacy
ground walking Y22.50 differs deliberately from rendered ground Y20.90.

The inspected baseline used TWN10, Terrain3 and prepared bundle version3.
The new authored format is TWN11, with Terrain4 / prepared bundle version4.
Save preparation captures clicked revision A while live revision B stays editable.
The native document byte budget remains 524288; over-budget exports must fail
before publication. No limits or architecture baselines may be raised.

Water audit: Graphics3D.SetEffectMaterialWater3D supplies roughness, foam, time and
ripple strength. water_surface3d.h derives a continuous world-space pattern with
a hard-coded travel vector. Ribbon normals already support sloped geometry.
Independent authored flow direction is missing; Phase4 will add a bounded shared
water-effect operation, keeping existing flat-water behavior as the default.
This is authored flow over terrain, not fluid simulation or a free-falling curtain.

## Phase 1 storage and geometry ownership

TerrainHeights3D stores 263169 possible shared corners in 1029 immutable Text
pages of 256 samples. A sample uses four ASCII bytes with signed 24-bit encoding,
quantized to 0.01 native units (1 mm), admitted range +/-1000 metres. Empty pages
mean zero, including absent legacy elevations. A copied height state adds 8248
bytes (1029 eight-byte text references plus two counters), not a 2.1 MB embedded
Double array. Native Text assignment retains immutable references; editing one
page copies at most 1024 payload bytes. No mutable page references escape.

Worst-case payload per fully independent surface is 1,053,696 bytes plus text
headers. Live, tabs, history, gesture, collision, renderer, comparison, clicked
save, decode and the two ramp preview snapshots hold fewer than 50 height states:
under 413 KB of tables and 52.7 MB of payload even if all pages differ.
Normal history shares unchanged pages. A local
height-state temporary is ~8 KB, and hot sampling takes ByRef. GPU limits remain
64 meshes and four water batches in each live/pending set; 65536 recipe patches
and 2048-patch uploads remain unchanged. Added recipe Y2 and FlowGroup cost 1 MB
per maximal CPU batch. Each surface renderer owns at most three live plus three
pending directed-water materials; old ribbons are destroyed before their materials.
Admission/preview must report
capacity failures before replacing the live terrain.

TerrainSurface3D owns validation and the single barycentric sampler: diagonal
00 -> 11. Existing four center triangles are a refinement of those two canonical
triangles because their center lies on that diagonal. Analytic material clipping
stays within each canonical plane; every resulting vertex samples that authority.
Flat untouched cells retain existing batching. Displaced triangles use explicit
Y0/Y1/Y2 and geometry-derived normals, including negative elevations. Flat water
retains its authored layer; structural elevations are not globally shifted.

TWN11 appends the canonical corner count, (run length, signed millimetre offset)
pairs, then (water mode, direction sign, speed percent) for every ordered brush.
Legacy maps without nonzero heights or directed water retain their prior version.
Decode clears optional state before reading, validates counts/ranges and only
publishes a complete candidate. Immutable height pages preserve clicked revision A
while live B changes. The 512 KiB document limit remains enforced; complex maps
that exceed it fail saving rather than dropping terrain. Terrain4 adds triangle Y2
and flow-group identity. Version3 prepared data is ignored and regenerated.

Save For Blender rejects elevations/directed flow before queueing the worker or
touching the destination. Both Python exporter entry paths repeat that preflight.
Legacy maps still use the matching prepared native contour recipe. This is an
explicit unsupported export boundary, not implemented Blender terrain animation.
Section transfer similarly rejects elevated maps; whole-document saves/imports,
recovery, tabs and Save As retain heights and authored flow.

## Editing and movement

TownTerrainTools owns sculpt transaction state and signed-decimal setting validation.
Raise/Lower use fixed 20 Hz, interpolated world-space stamps; Smooth reads immutable
neighbours and blends at most 25%. One accepted gesture creates one history entry;
Escape or failed geometry publication restores heights, curves and revisions.
TownTerrainProtection blocks edits beneath structural footprints and current actors.
TownRampTools owns frozen snapped endpoints and source/preview surfaces. The default
6 m corridor has 2 m shoulders plus a grid-diagonal apron; the panel reports endpoint
heights, length, centreline grade/angle and maximum actual transition angle.

TerrainTraversal3D partitions movement at every crossed cell edge and canonical
diagonal. TownCollisionWorld applies a 30-degree maximum, preserves bridge/structure
precedence and rejects water. Party/followers and road routing use this authority.
TownTerrainPanel draws conforming brush/flow previews; TownPicking uses exact
ray/triangle intersection in perspective and orthographic views.

TownWaterTools owns two-point line input, 4 m default width, flat/follow mode, erase,
reverse and speed 0–4x. TerrainWater3D chooses the initial downhill endpoint order
and detects interior uphill intervals. Three independent direction/speed groups
fit the existing four ribbon batches. Following water is bed +0.02 m, with an
opaque bed below. Shader world-space coordinates make phase continuous across
triangles. Zero speed pauses the pattern. This is draped flow, not fluid simulation
or unsupported vertical free-falling curtains.

## Validation and remaining phases

Baseline passed: normal Debug/Release Studio, 76 shared arena checks, four native
town groups, 13 formatter integration checks and 646-file style check. Commands:
scripts/test-town-editor.ps1; scripts/test-smile-formatter.ps1;
scripts/format-smile-style.ps1 -Check -FormatLongIf; tools/Character3DViewer/Build.ps1
-Target Native -Configuration Release (and Debug). Native acceptance uses an
isolated Terrain-and-Flowing-Water-MVP document, never an existing authored town.

Phases1–5 source and focused native tests pass (terrain-phase5-persistence-tests.log).
Water-authoring tests were initially imported without a call; that was corrected
before this recorded passing run. Native/Python TWN11 interoperability, negative
heights, reversed paused flow, save A/live B, truncated/future rejection and legacy
TWN7/TWN9 pass (terrain-phase5-python-codec.log). Native water fixture captures at
0 and 0.5 seconds show perpendicular downhill shifts (4,-1) and (-3,-2) pixels,
correlations .9806/.9445; paused captures remain still while time advances.
These are actual native renderer captures. Full Studio interaction evidence follows.

Phase6 uses focused native checks plus actual Studio interactions. The isolated fixture is
generated by scripts/create-terrain-acceptance.ps1 using the production sampler,
ramp tool, navigation, save codec and prepared export. It does not modify canonical
towns. This is not an exhaustive manual playthrough or a full smoke-suite claim.

### Phase 6 evidence — October 2

The final four native town groups pass in `artifacts/terrain-party-full-tests.log`.
The loaded-party check walks all four actors uphill and downhill in 600 updates,
checks every follower against the canonical ground and limits leader height change
to 0.41 native units per 2-unit step. Release and Debug builds pass with 362 assets
(`terrain-short-stroke-release-launch.log`, `terrain-short-stroke-debug-build.log`).
The normal Release executable displays compile time 2026-10-02 04:20:17 +08:00 and
VSIX 2.0.69. Installation verified all 36 payload hashes; runtime DLL SHA256 is
CD505C2A23AD02671F2B64F0906DD059F93C159513AD50A4EC99C4A79BD77DFC.

Actual UI import: `artifacts/tests/terrain-acceptance-20261002-033454/` contains
the isolated original and `2026-10-02 0425 - Terrain-and-Flowing-Water-MVP.town`.
A short Raise drag changes 24 corners and 59,113 triangles to 59,423. The Viewer
save reports Saved And Verified; reopening a new tab retains 59,423 triangles.
Python decoding verifies TWN11, all changed corners, and identical flow metadata,
curves, objects, grid and base materials. No permanent town was replaced.

| Acceptance | Evidence actually performed |
|---|---|
| T01 legacy | Four native groups include three existing journeys, deck/stairs/doors and resource checks; Python TWN7/TWN9 round trips; actual Canals remains visible after relaunch. |
| T02 physical brush | TownSculptTests equal physical distances on nonuniform edges; TownElevationTests shared-corner/nonuniform sampler. |
| T03 stroke timing | 30 and 60 simulated FPS yield identical immutable pages over two seconds; 16 ms release regression and actual short Studio drag. Spatial interpolation is bounded by brush/grid size. |
| T04 flatten/smooth | Native constant terrain, immutable-neighbour 25% blend and target without overshoot checks. |
| T05 triangle contract | Native nonplanar unit-cell 0.5 result, all clipped road/ground/water vertices and Terrain4 third-height round trip; exact vertical/oblique ray tests. |
| T06 ramp | Native 10 m/50 m gives 20% and 11.309932 degrees; samples across the full 6 m corridor; both-direction traversal. Actual Studio preview/apply changes 59,113 to 60,037 triangles. |
| T07 ramp robustness | Reversed anchors identical, diagonal and nonuniform sampler checks, coincident/subgrid-width rejection, actual maximum shoulder angle reported. |
| T08 grounded party | Loaded four-actor continuous test described above; actual minimap journeys reach low Y23 and high Y123. Endpoint screenshots alone are not continuous-motion proof. |
| T09 walkability | Native narrow-ridge large-step rejection, 30-degree gate, shared route eligibility and cache invalidation; elevated minimap Begin/Append regression. |
| T10 precedence | Existing structure/bridge/door checks plus underwater rejection and whole-stroke protected footprint rejection. |
| T11 sloped water | Native all-three-vertex canonical bed +0.2-unit checks and at least one unequal-height patch; actual Studio streams visibly drape over slopes. |
| T12 flow direction | Fixed-time native captures at 0 and 0.5 s show both perpendicular motions; 0x remains stable; native reversed-anchor direction check. |
| T13 seams | Connected sections share one world-phase material; geometry derives from identical bed samples; captured native streams show continuous ribbons. |
| T14 warnings | Native interior hump, reverse, zero-length and flat handling; actual isolated Studio map displays Uphill Flow warning. |
| T15 bed edits | Native edit retains direction and updates uphill warning; regenerated vertices use the edited bed. 2 cm offset and captured ribbons avoid coincident sheets in the tested views. |
| T16 history/cancel | Native full snapshot cancellation, erase retains heights, undo/redo/branch rules; actual ramp undo/redo and water-line undo restore triangle counts. |
| T17 persistence | Native Save As/recovery, Python interop/negative heights/paused reversed flow, fresh-profile preparation, actual Studio save/reopen described above. |
| T18 save revision | Native A remains exact while B is dirty; prepared data is bound to A and rejected for B. |
| T19 cache | Native height/direction comparison, old version rejection, malformed prepared data rejection and existing complete-scene swap checks. |
| T20 malformed/limits | Native invalid counts/pages, full corner bound and unchanged 512 KiB save cap; Python nonfinite/invalid/truncated/future rejection. |
| T21 lifecycle | Forced stroke resource failure restores the previous revision; normal native switch/undo/render lifecycle checks show no resource growth. No long soak was performed. |
| T22 isolation | Separate imported tabs and immutable snapshots; full native castle/character scene loads (98 castle parts, 24 Arin keys); Git review confirms no existing town/castle/calibration asset modifications. |
| T23 conformance | Every native curved material clipped vertex agrees with the canonical terrain; both road and ground present. |
| T24 camera/picking | Native vertical/oblique/no-hit rays and UI capture check; actual 2D sculpt/water and perspective ramp interaction, plus actual camera journey. Native camera test clears the final eased eye in both follow modes. |
| T25 export | Actual Studio Save For Blender rejects at 0% with explicit terrain message; both Python paths and native queue modes reject before replacement. Prior legacy curved Blender export/reopen evidence remains applicable. |
| T26 gates | All four native town groups, 59 hardening checks, 13 formatter integration checks, toolchain build, installed VSIX hash verification and normal Release/Debug builds pass. Final formatting/diff review is recorded with the commit. |

Actual screenshots under `artifacts/`: `terrain-studio-import-native.png`,
`terrain-ramp-preview-native.png`, `terrain-short-sculpt-native.png`,
`terrain-save-complete-native.png`, `terrain-reopened-native.png`,
`terrain-water-authored-native.png`, `terrain-downhill-arrival-fixed-native.png`,
`terrain-uphill-ramp-native.png`, `terrain-blender-explicit-rejection-native.png`.
The ramp-preview image predates the decimal-label cleanup; the uphill image is an
arrival, despite its filename. Fixed-time water captures are
`terrain-water-crests-t0.png`, `terrain-water-crests-t0500.png` and paused-a/b.

### Limitations and prior defects

Blender terrain/directed-flow and elevated section transfer are explicitly
unsupported; no flattening fallback exists. Water is a terrain ribbon, not a
vertical free-fall simulation. The unchanged three directed-flow-group and save
byte limits may reject complex maps. Native helper gestures cannot synthesize
Shift+middle drag; its existing native input fixture passes, but no new manual
gesture pass is claimed. The earlier cold Neris load is 2139 ms versus a 2 s
target; warm reopen is 150–174 ms. A controlled red-beacon on/off screenshot pair
and continuous manual minimap-route trace are not claimed; their focused native
timing/path regressions pass. These are preserved follow-up evidence limits, not
reasons to change assets or expand capacity. Web terrain acceptance was not run.
