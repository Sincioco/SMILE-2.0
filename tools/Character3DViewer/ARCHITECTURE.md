# Character Viewer Architecture

This is the current owner and contract map for native **Sin Star Studio**, the
application in `tools/Character3DViewer`. Start with [README](README.md) for use,
[repository instructions](../../AGENTS.md) for mandatory development rules, and
[status](docs/status.md) for unresolved defects and acceptance boundaries.

## Scope and authority

Studio owns approved Towns/Maps, Battle Systems and Battle Simulations brought into
the independent `D:\SMILE 2.0 - Sin Star I` game. The local `games/SinStarI` junction
is compatibility access, not an engine-owned game copy. Reuse shared owners rather
than maintain a divergent game implementation. Canonical packages own asset sources,
accepted calibration JSON, rig/clip repair history and checksums; cooking mirrors
and runtime save envelopes are disposable. JSON content identity wins over timestamps.

Native Windows is the active target. Web work remains paused, and the separate
`tools/SmileStudio` S1/design work is historical. Existing compatibility source
inventory is not build acceptance or authorization to resume those projects.
Current native startup is `NativeProgram` -> `NativeViewerHost` -> town; Metropolis
is first in `TownLibrary`. Native tab visibility comes from `NativeViewerTabs`.

## Architecture guides

| Read | Contract |
| --- | --- |
| [Runtime and frame contracts](docs/architecture-runtime.md) | Shared session, actor/calibration ownership, frame order, effects, Beat editing and battle bridge |
| [Town authoring](../../docs/architecture/town-authoring.md) | Document/gesture/render/persistence boundaries and surface layering and linked historical evidence |
| [Terrain elevation](../../docs/architecture/terrain-elevation.md) | Height/flow editing, prepared geometry, validation and bounded terrain behavior |
| [Viewer export](docs/viewer-export.md) | Warm retention, isolated R04 cold lifecycle and verified bounded native acceptance |
| [Build and validation](docs/build-and-validation.md) | Safe native publication, launch, fixture identities and evidence standards |
| [Precision boundary](../../docs/libraries/precision3d-boundary.md) | Double authoring, shared projection and final GPU float narrowing |
| [Startup presentation](../../docs/architecture/startup-presentation.md) | Official branding, visible minimum, artifact metadata and loading ownership |
| [Battle checkpoint](../../docs/implementation/party-beat-camera-checkpoint.md) | Detailed camera/recovery evidence; obsolete startup/S1 instructions are historical |

## Owner map

Names below are module/file owners in this folder unless explicitly shared. Keep
behavior with its existing state owner; hosts coordinate rather than reimplement it.

| Owner | Responsibility and public boundary | Focused evidence route |
| --- | --- | --- |
| `NativeProgram`, `NativeViewerHost.Session`, `NativeViewerTabs` | One window/frame loop; `Start`, input/update/draw/release; tab/battle/town host lifecycle | Native build, hardening, battle and town session fixtures |
| `ViewerWorkflow.Session` | One coordinated session; start, scoped input, update/draw, pose/Beat commands, directed playback and release | Hardening and actual actor/calibration fixtures |
| `ViewerSession`, `ViewerLifecycle` | Identity, first failure, load/reset/retry/switch/cleanup; failed splash retains prior session | Hardening, rejected load, native recovery reports |
| `ViewerTiming`, `ViewerPlayback`, `ViewerInput`, `ViewerTimelineEditing` | Scene/playback clocks, queued modifiers, exclusive gestures, seek/repeat and pause | Timing/input fixtures; actual timeline interaction |
| `Profiles`, `ViewerActors` | Asset/clip/socket identity, grounding, independent actor/animator context and final transforms | Package validators, calibration/imported profile tests |
| `ViewerCalibration`, `CalibrationJson`, editing/controls owners | Checked per-character storage, strict JSON, draft/undo/import/backup recovery | Native isolated calibration and serializer checks |
| `ViewerCamera`, `BattleCamera`, shared `ArenaCamera3D`/`PrecisionCamera3D` | Smooth camera controls, anchors, fit, precise composition; no host-local camera solver | Precision/camera fixtures and bounded gestures |
| `ViewerUi`, inspector presentation/commands, `ViewerGizmo` | Pure layout/hit maps, typed commands, gizmo projection; calibration owns mutation | Hardening, layout, edit/drag isolation |
| `ViewerParty`, `ViewerDragon`, `ViewerBattlePreview` | Independent participants/opponent, choreography, action sampling and shared Party camera policy | Party/real-asset battle/calibration fixtures |
| `BattlePlanning`, `BattleProgression`, `BattleOutcome` | Orders/repetition, session EXP/rewards, stat policy and ending/reward presentation | `test-viewer-battle.ps1` |
| `BattlePresentation`, `BattleCinematics`, `BattleUi`, `BattleHud`, `BattleGrowthView` | Borrowed actor presentation, Party-based camera bridge, menus/HUD/stat views | Battle planning, scene and native UI checks |
| `BattleCameraShots`, `BattleCameraTimeline`, `ViewerBeatSequence` | Relative shot frames, camera-only timing, bounded serialization and action sampling | BeatCameraTests and real-actor sequence restore |
| `ViewerBeatEditor`, `ViewerBeatTimeline`, `ViewerBeatHeadPersistence` | Per-character draft/clipboard, gestures and independently recoverable head saves | Failed head/sequence save, retry/discard/identity checks |
| `ViewerEffects`, `OrinStorm`, `ArinShieldRim`, `DragonPresence`, `BattleAudio` | Caller-owned equipment/opponent effects, sound cue history and lifecycle | Real actor isolation, VFX/admission/fallback fixtures |
| `ViewerRendering`, shared `Arena3D`/`StaticBackdrop3D`/`SceneVfx3D` | Arena/backdrop, one draw transaction, scene VFX advance and reflection preferences | Frame order, graphics/reflection contracts |
| `NerisTown`, `NerisTownParty`, camera/keyboard/inspection owners | Native scene, party motion, overview/follow/inspection routing; host delegates here | Authored Neris scene and inspection fixtures |
| `TownEditorSession`, `TownEditor`, `TownSelection`, `TownEditorPanel` | One live town; lifecycle/file/render coordination; gesture/selection state and UI geometry | Four native town integration groups |
| `TownDocument`, `TownNpcLayout`, `TownSurfaceStyles`, `TownMapLoads` | Authored data; immutable snapshots, stable item identity and revision metadata | Codec/history/foundation fixtures |
| `TownHistory`, `TownGuides`, guide list/resize/files owners | Bounded undo, marker-only state, snapping and separate checked `.guide` transfer | Guide/undo/duplication/native file fixtures |
| `TownCatalogRenderer`, `TownSurfaceRenderer`, `TownAttachments`, `TownDecks` | GPU lifetimes, incremental terrain, attachments and support/rail geometry | Actual draw/cleanup, preparation and terrain fixtures |
| `TownLighting`, `TownCityLighting`, `TownWaterAppearance`, city effect owners | Per-view lighting, displayed day/night water, bounded facade/firework/traffic effects | Metropolis native and reflection fixtures |
| `TownDocumentNavigation`, road network/graph/search/travel/stops owners | Committed collision snapshot, cached connections, exact graph A*, queue and safe walking | Road/search/route fixtures |
| `TownDocumentMap`, `TownMinimapInput`, `TownPartyInset`, viewport/camera panels | Minimap/route rendering, captured gestures and independently scheduled POV | Minimap, bounds and POV/light isolation |
| `TownWorldDocument`, World editor/canvas/view/travel/import/files | Atlas layout and checked imports; directed links projected from Map Load data | World tests; authored connection/reopen checks |
| `TownLibrary`, `TownTabs`, `TownDocumentStore`, `TownDerivedCache` | Recovery/permanent policy, 17 document slots, strict codecs and derived identity | Save/reopen/recovery/queue checks |
| `TownSavePreparation`, `TownNpcPreparation`, `TownSaveQueue`, `TownFileJobs` | Frozen preparation, queued transfer, exact dirty checks and partial-failure reporting | Export photograph and real cold-scene fixtures |
| `TownWorldPreviews`, `TownExportScene`, route/`StaticBackdrop3D` selection contexts | Mutable thumbnail cache versus request-owned cold resources and scoped restoration | [R04 acceptance boundary](docs/viewer-export.md#acceptance-boundary) |
| Build/prepare/check/launch scripts | Complete publication, canonical prerequisites, synchronization and graceful replacement | Preservation fixture; manifest/native build checks |

## State and frame contracts

Milo's individual native tab uses the existing `Profiles`, `NativeViewerTabs` and
`ViewerSession` owners. Profile 15 and tab 17 leave Battle System/Town identities
unchanged. His canonical `Pet/MiloV1` package owns Blender skinning and baked
canine motion; Studio adds no rig solver or animation algorithm. `MiloTests`
checks the cooked asset's eight clips/sockets, actual draw and two independent
animators. The current Party roster and calibration banks remain unchanged.

The [runtime guide](docs/architecture-runtime.md) preserves frame order and the
boundaries between playback, calibration, camera, effects and rendering. Every
actor owns mutable pose/equipment/clock/effect state. Immutable assets may share
resources. Scene-level clocks, renderer and application loop must not be duplicated.
Reflections replay accepted submissions without another simulation update.

Pose JSON, Beat camera sequences, head definitions, town documents, guide files,
World layouts and derived caches are distinct stores with different authority. An export or backup
must not silently migrate identity, flatten unsupported content or overwrite
unknown storage. Failed saves preserve drafts/undo and surface a recoverable error.
Pending pose/Beat/head edits retain their close/switch protections.

The independent game can embed `NativeViewerHost.Session` with navigation disabled;
it owns title navigation, music and host lifetime. `CanLeaveBattle` / `HeaderVisible`
expose the boundary without moving battle rules or mutable state into the game host.

## Town and export boundaries

`TownLibrary`, `TownTabs` and `TownWorldDocument` each support 17 entries. World
connections follow authored yellow Map Load areas; atlas loading never repairs or
creates town roads/destinations. Road search is distance-weighted A* with a Manhattan
lower bound on the validated four-neighbor graph. Prepared connections persist by
content identity; actual journeys remain runtime work.

`TownSurfaceLayers.json` is the numeric surface authority for rendering, walking,
picking and Blender. Do not hide overlaps by changing road layouts or importing
independent height literals. Each town/floor/grid shares bounds and center; authored
initial cameras and viewport-center orbit have separate operations. Night defaults
to 112%; saved overrides survive. Lighting selects local lamps after each main,
photograph or POV camera is accepted. Water policy uses intensity and active map.

Explicit saves prepare the immutable clicked revision and include checked terrain
and road companion records. Viewer transfers run in-process; only Blender conversion
uses its external worker. Warm photos retain exact accepted pairs. R04 renders a
request-owned cold scene without switching the editing tab, separating accepted
`.Input`, prepared output and cold `.Rendered` identities. Per-slot freeze results
prevent stale image reuse; Save As carries source identity into legacy NPC preparation.
Scoped routes and backdrop selection restore the live scene after capture.

Catalog model leases and native reference-counted, immutable, untextured imported
PBR values share resource storage; scene objects, node overrides and animators stay
independent. Native staged-particle capacity is 16,384, allocated on demand for the
15,702-slot two-city-plus-CPU-fire budget; Web remains 8,192. Seven real cold cases
and the 17-slot warm regression pass. See [the export contract](docs/viewer-export.md)
and [evidence/remaining checks](docs/status.md#viewer-export-r04).

## Change review and validation

`ViewerWorkflow` retains the reviewed coordinator-size exception because its ordered
frame story is shared once. Actor, calibration, camera, effects and persistence
algorithms stay with their focused owners. `NerisTown`, `TownEditorSession` and other
existing large modules are legacy debt, not blanket exceptions for more unrelated
behavior. Historical growth reviews and the 600-line review trigger are preserved
in the archives. Do not raise thresholds/baselines/exclusions to make a change pass.

Review responsibility, imports, public operations, entry-point growth, resource
ownership and cleanup directly. Add a focused owner only for concrete maintenance
or reuse. No separate general architecture checker is claimed. Existing hardening
assertions and focused native tests support, but do not replace, that review.
Keep reusable language/compiler behavior in the shared language/runtime rather
than tool-specific workarounds; apply the authoritative SMILE formatting rules.

Use [bounded native checks](docs/build-and-validation.md#focused-validation), complete
assets and isolated save identities. Require checked draw results and the exact
native PASS predicate; separate manual interaction, model/source checks and prior
evidence. A valid artifact or unrelated passing test does not close a known defect.
Current failures, resource limits and next actions belong in [status](docs/status.md).

## Historical evidence

The [October architecture archive](docs/archive/architecture-2026-10.md) and
[September/undated architecture archive](docs/archive/architecture-2026-09.md)
preserve the complete prior body, including source maps, constraints, growth reviews,
open issues, superseded designs and dated test evidence. Existing [README archives](README.md#historical-evidence-and-old-links)
retain the parallel workflow history. The old headings below remain valid links;
use this file's owner map and topic contracts for current work.

<a id="october-5-dragon-arm-wing-and-recoil-correction"></a>[October 5 Dragon arm, wing and recoil correction](docs/archive/architecture-2026-10.md#october-5-dragon-arm-wing-and-recoil-correction)<br>
<a id="october-5-dragon-full-body-animation"></a>[October 5 Dragon full-body animation](docs/archive/architecture-2026-10.md#october-5-dragon-full-body-animation)<br>
<a id="october-4-metropolis-daytime-water-reflections"></a>[October 4 Metropolis daytime water reflections](docs/archive/architecture-2026-10.md#october-4-metropolis-daytime-water-reflections)<br>
<a id="october-4-city-lighting-and-party-pov-isolation"></a>[October 4 city lighting and party POV isolation](docs/archive/architecture-2026-10.md#october-4-city-lighting-and-party-pov-isolation)<br>
<a id="october-4-sin-star-i-battle-system-host"></a>[October 4 Sin Star I Battle System host](docs/archive/architecture-2026-10.md#october-4-sin-star-i-battle-system-host)<br>
<a id="october-4-all-map-nighttime-water-and-112-moonlight"></a>[October 4 all-map nighttime water and 112% moonlight](docs/archive/architecture-2026-10.md#october-4-all-map-nighttime-water-and-112-moonlight)<br>
<a id="october-4-shared-neris-nighttime-water"></a>[October 4 shared Neris nighttime water](docs/archive/architecture-2026-10.md#october-4-shared-neris-nighttime-water)<br>
<a id="october-4-five-building-lake-fireworks"></a>[October 4 five-building lake fireworks](docs/archive/architecture-2026-10.md#october-4-five-building-lake-fireworks)<br>
<a id="october-4-neris-metropolis-native-delivery"></a>[October 4 Neris Metropolis native delivery](docs/archive/architecture-2026-10.md#october-4-neris-metropolis-native-delivery)<br>
<a id="october-3-2100-review-handoff-r04-partial-r05r06-repaired"></a>[October 3 21:00 review handoff: R04 partial, R05/R06 repaired](docs/archive/architecture-2026-10.md#october-3-2100-review-handoff-r04-partial-r05r06-repaired)<br>
<a id="october-3-resident-loading-authored-connections-and-editor-additions"></a>[October 3 resident loading, authored connections and editor additions](docs/archive/architecture-2026-10.md#october-3-resident-loading-authored-connections-and-editor-additions)<br>
<a id="october-3-predictable-magnification-and-authored-initial-camera"></a>[October 3 predictable magnification and authored initial camera](docs/archive/architecture-2026-10.md#october-3-predictable-magnification-and-authored-initial-camera)<br>
<a id="october-3-map-identity-arrivals-and-reusable-fountain-streams"></a>[October 3 map identity, arrivals and reusable fountain streams](docs/archive/architecture-2026-10.md#october-3-map-identity-arrivals-and-reusable-fountain-streams)<br>
<a id="october-3-road-closure-contour-refinement-and-spaceport-walking"></a>[October 3 road closure, contour refinement and Spaceport walking](docs/archive/architecture-2026-10.md#october-3-road-closure-contour-refinement-and-spaceport-walking)<br>
<a id="october-3-terrain-contours-elevation-and-campfires"></a>[October 3 terrain contours, elevation and campfires](docs/archive/architecture-2026-10.md#october-3-terrain-contours-elevation-and-campfires)<br>
<a id="october-2-mountain-refresh-and-editor-interaction"></a>[October 2 mountain refresh and editor interaction](docs/archive/architecture-2026-10.md#october-2-mountain-refresh-and-editor-interaction)<br>
<a id="october-2-queued-saves-and-continuous-terrain-surfaces"></a>[October 2 queued saves and continuous terrain surfaces](docs/archive/architecture-2026-10.md#october-2-queued-saves-and-continuous-terrain-surfaces)<br>
<a id="october-2-local-terrain-style-ownership-and-silverfall-load-repair"></a>[October 2 local terrain-style ownership and Silverfall load repair](docs/archive/architecture-2026-10.md#october-2-local-terrain-style-ownership-and-silverfall-load-repair)<br>
<a id="october-2-wilderness-refinement-ownership"></a>[October 2 wilderness refinement ownership](docs/archive/architecture-2026-10.md#october-2-wilderness-refinement-ownership)<br>
<a id="october-2-terrain-ownership-and-acceptance"></a>[October 2 terrain ownership and acceptance](docs/archive/architecture-2026-10.md#october-2-terrain-ownership-and-acceptance)<br>
<a id="october-2-minimap-and-initial-loading-follow-through"></a>[October 2 minimap and initial loading follow-through](docs/archive/architecture-2026-10.md#october-2-minimap-and-initial-loading-follow-through)<br>
<a id="october-1-formatting-follow-through"></a>[October 1 formatting follow-through](docs/archive/architecture-2026-10.md#october-1-formatting-follow-through)<br>
<a id="october-1-demo-lighting-and-portable-terrain-repair"></a>[October 1 Demo lighting and portable terrain repair](docs/archive/architecture-2026-10.md#october-1-demo-lighting-and-portable-terrain-repair)<br>
<a id="october-1-persistent-thumbnail-ownership"></a>[October 1 persistent thumbnail ownership](docs/archive/architecture-2026-10.md#october-1-persistent-thumbnail-ownership)<br>
<a id="october-1-demo-timing-camera-and-footer"></a>[October 1 Demo timing, camera and footer](docs/archive/architecture-2026-10.md#october-1-demo-timing-camera-and-footer)<br>
<a id="october-1-build-identity-and-native-save-store-repair"></a>[October 1 build identity and native save-store repair](docs/archive/architecture-2026-10.md#october-1-build-identity-and-native-save-store-repair)<br>
<a id="october-1-neris-orbital-spacecraft"></a>[October 1 Neris orbital spacecraft](docs/archive/architecture-2026-10.md#october-1-neris-orbital-spacecraft)<br>
<a id="october-1-spaceport-paving"></a>[October 1 spaceport paving](docs/archive/architecture-2026-10.md#october-1-spaceport-paving)<br>
<a id="october-1-east-valley-star-landscape"></a>[October 1 East Valley star landscape](docs/archive/architecture-2026-10.md#october-1-east-valley-star-landscape)<br>
<a id="october-1-immediate-startup-showcase"></a>[October 1 immediate startup showcase](docs/archive/architecture-2026-10.md#october-1-immediate-startup-showcase)<br>
<a id="october-1-crown-isles-palm-refinement"></a>[October 1 Crown Isles palm refinement](docs/archive/architecture-2026-10.md#october-1-crown-isles-palm-refinement)<br>
<a id="october-1-landscape-authoring-and-map-revision"></a>[October 1 landscape authoring and map revision](docs/archive/architecture-2026-10.md#october-1-landscape-authoring-and-map-revision)<br>
<a id="october-1-map-showcase-selection-and-viewport-layout"></a>[October 1: map showcase selection and viewport layout](docs/archive/architecture-2026-10.md#october-1-map-showcase-selection-and-viewport-layout)<br>
<a id="october-1-minimap-gestures-route-fidelity-and-conversation-presentation"></a>[October 1: minimap gestures, route fidelity and conversation presentation](docs/archive/architecture-2026-10.md#october-1-minimap-gestures-route-fidelity-and-conversation-presentation)<br>
<a id="october-1-click-to-converse-and-mutual-facing"></a>[October 1: click-to-converse and mutual facing](docs/archive/architecture-2026-10.md#october-1-click-to-converse-and-mutual-facing)<br>
<a id="october-1-immutable-save-preparation-and-portable-maps"></a>[October 1: immutable save preparation and portable maps](docs/archive/architecture-2026-10.md#october-1-immutable-save-preparation-and-portable-maps)<br>
<a id="october-1-persisted-map-preparation"></a>[October 1: persisted map preparation](docs/archive/architecture-2026-10.md#october-1-persisted-map-preparation)<br>
<a id="october-1-bounded-terrain-uploads-and-failed-map-recovery"></a>[October 1: bounded terrain uploads and failed-map recovery](docs/archive/architecture-2026-10.md#october-1-bounded-terrain-uploads-and-failed-map-recovery)<br>
<a id="current-map-access-navigation-gallery-and-residents-september-30"></a>[Current: map access, navigation gallery and residents (September 30)](docs/archive/architecture-2026-09.md#current-map-access-navigation-gallery-and-residents-september-30)<br>
<a id="current-reciprocal-luma-entrances-september-30"></a>[Current: reciprocal Luma entrances (September 30)](docs/archive/architecture-2026-09.md#current-reciprocal-luma-entrances-september-30)<br>
<a id="current-luma-journey-terrain-and-expanded-atlas-september-30"></a>[Current: Luma journey terrain and expanded atlas (September 30)](docs/archive/architecture-2026-09.md#current-luma-journey-terrain-and-expanded-atlas-september-30)<br>
<a id="current-curved-terrain-luma-maps-and-scene-photographs-september-30"></a>[Current: curved terrain, Luma maps and scene photographs (September 30)](docs/archive/architecture-2026-09.md#current-curved-terrain-luma-maps-and-scene-photographs-september-30)<br>
<a id="current-cached-road-routing-and-pov-controls-september-30"></a>[Current: cached road routing and POV controls (September 30)](docs/archive/architecture-2026-09.md#current-cached-road-routing-and-pov-controls-september-30)<br>
<a id="current-precision-views-guide-editing-and-map-presentation-september-30"></a>[Current: precision views, guide editing and map presentation (September 30)](docs/archive/architecture-2026-09.md#current-precision-views-guide-editing-and-map-presentation-september-30)<br>
<a id="current-relocated-royal-court-drawbridge-clearance-september-30"></a>[Current: relocated Royal Court drawbridge clearance (September 30)](docs/archive/architecture-2026-09.md#current-relocated-royal-court-drawbridge-clearance-september-30)<br>
<a id="current-world-connections-and-viewport-controls-september-30"></a>[Current: world connections and viewport controls (September 30)](docs/archive/architecture-2026-09.md#current-world-connections-and-viewport-controls-september-30)<br>
<a id="current-map-load-areas"></a>[Current: Map Load areas](docs/archive/architecture-2026-09.md#current-map-load-areas)<br>
<a id="current-airport-access-and-map-presentation"></a>[Current: airport access and map presentation](docs/archive/architecture-2026-09.md#current-airport-access-and-map-presentation)<br>
<a id="current-explicit-permanent-map-updates"></a>[Current: explicit permanent map updates](docs/archive/architecture-2026-09.md#current-explicit-permanent-map-updates)<br>
<a id="current-guide-gestures-map-destinations-and-minimap-travel"></a>[Current: guide gestures, map destinations and minimap travel](docs/archive/architecture-2026-09.md#current-guide-gestures-map-destinations-and-minimap-travel)<br>
<a id="current-section-edits-and-independent-royal-court-placement"></a>[Current: section edits and independent Royal Court placement](docs/archive/architecture-2026-09.md#current-section-edits-and-independent-royal-court-placement)<br>
<a id="current-sin-star-studio-authoring-and-publication-safety-september-29"></a>[Current: Sin Star Studio authoring and publication safety (September 29)](docs/archive/architecture-2026-09.md#current-sin-star-studio-authoring-and-publication-safety-september-29)<br>
<a id="current-horizon-r009-and-centered-linked-maps-september-29"></a>[Current: Horizon r009 and centered linked maps (September 29)](docs/archive/architecture-2026-09.md#current-horizon-r009-and-centered-linked-maps-september-29)<br>
<a id="current-checkpoint-horizon-r008-september-29"></a>[Current checkpoint: Horizon r008 (September 29)](docs/archive/architecture-2026-09.md#current-checkpoint-horizon-r008-september-29)<br>
<a id="earlier-horizon-r007-facade-and-cursor-orbit-september-29"></a>[Earlier: Horizon r007 facade and cursor orbit (September 29)](docs/archive/architecture-2026-09.md#earlier-horizon-r007-facade-and-cursor-orbit-september-29)<br>
<a id="current-gentle-wave-and-separate-airport-towns-september-29"></a>[Current: Gentle Wave and separate airport towns (September 29)](docs/archive/architecture-2026-09.md#current-gentle-wave-and-separate-airport-towns-september-29)<br>
<a id="september-29-spaceport-facade-and-current-view-orbit"></a>[September 29 spaceport facade and current-view orbit](docs/archive/architecture-2026-09.md#september-29-spaceport-facade-and-current-view-orbit)<br>
<a id="september-28-royal-castle-town-inspection-and-water"></a>[September 28 royal castle, town inspection and water](docs/archive/architecture-2026-09.md#september-28-royal-castle-town-inspection-and-water)<br>
<a id="september-28-decor-palette-and-sun-sliders"></a>[September 28 Decor palette and Sun sliders](docs/archive/architecture-2026-09.md#september-28-decor-palette-and-sun-sliders)<br>
<a id="september-28-native-town-file-and-appearance-changes"></a>[September 28 native town file and appearance changes](docs/archive/architecture-2026-09.md#september-28-native-town-file-and-appearance-changes)<br>
<a id="native-neris-town-ownership"></a>[Native Neris town ownership](docs/archive/architecture-2026-09.md#native-neris-town-ownership)<br>
<a id="blender-and-export-boundary"></a>[Blender and export boundary](docs/archive/architecture-2026-09.md#blender-and-export-boundary)<br>
<a id="motion-and-camera-contracts"></a>[Motion and camera contracts](docs/archive/architecture-2026-09.md#motion-and-camera-contracts)<br>
<a id="rendering-loading-and-validation"></a>[Rendering, loading and validation](docs/archive/architecture-2026-09.md#rendering-loading-and-validation)<br>
<a id="native-battle-planning-and-presentation"></a>[Native battle planning and presentation](docs/archive/architecture-2026-09.md#native-battle-planning-and-presentation)<br>
<a id="native-battle-cinematics-and-responsive-hud-part-1b"></a>[Native battle cinematics and responsive HUD (Part 1b)](docs/archive/architecture-2026-09.md#native-battle-cinematics-and-responsive-hud-part-1b)<br>
<a id="native-battle-system-camera-parity-with-kael-party"></a>[Native Battle System camera parity with Kael Party](docs/archive/architecture-2026-09.md#native-battle-system-camera-parity-with-kael-party)<br>
<a id="explicit-auto-battle-interruption"></a>[Explicit Auto Battle interruption](docs/archive/architecture-2026-09.md#explicit-auto-battle-interruption)<br>
<a id="native-camera-return-pause-and-header-free-view--september-21"></a>[Native camera return, pause and header-free view — September 21](docs/archive/architecture-2026-09.md#native-camera-return-pause-and-header-free-view--september-21)<br>
<a id="calibration-rejection-recovery--september-21"></a>[Calibration rejection recovery — September 21](docs/archive/architecture-2026-09.md#calibration-rejection-recovery--september-21)<br>
<a id="native-battle-travel-facing-and-automatic-restart"></a>[Native battle travel facing and automatic restart](docs/archive/architecture-2026-09.md#native-battle-travel-facing-and-automatic-restart)<br>
<a id="initial-idle-auto-and-opening-revolution"></a>[Initial idle Auto and opening revolution](docs/archive/architecture-2026-09.md#initial-idle-auto-and-opening-revolution)<br>
<a id="standalone-tab-music"></a>[Standalone tab music](docs/archive/architecture-2026-09.md#standalone-tab-music)<br>
<a id="kael-bending-without-locomotion"></a>[Kael bending without locomotion](docs/archive/architecture-2026-09.md#kael-bending-without-locomotion)<br>
<a id="persistent-recovery-evidence"></a>[Persistent recovery evidence](docs/archive/architecture-2026-09.md#persistent-recovery-evidence)<br>
<a id="recovery-stage-diagnostics"></a>[Recovery-stage diagnostics](docs/archive/architecture-2026-09.md#recovery-stage-diagnostics)<br>
<a id="shared-arena-adoption"></a>[Shared arena adoption](docs/archive/architecture-2026-09.md#shared-arena-adoption)<br>
<a id="kael-silver-hair-and-separate-native-fire-lab"></a>[Kael silver hair and separate native Fire Lab](docs/archive/architecture-2026-09.md#kael-silver-hair-and-separate-native-fire-lab)<br>
<a id="native-kael-water-adoption"></a>[Native Kael Water adoption](docs/archive/architecture-2026-09.md#native-kael-water-adoption)<br>
<a id="native-kael-earth-casts-and-quiet-idle"></a>[Native Kael Earth casts and quiet Idle](docs/archive/architecture-2026-09.md#native-kael-earth-casts-and-quiet-idle)<br>
<a id="shared-native-party-status-and-dragon-roster"></a>[Shared native party status and Dragon roster](docs/archive/architecture-2026-09.md#shared-native-party-status-and-dragon-roster)<br>
<a id="native-kael-package-and-boss-selection"></a>[Native Kael package and boss selection](docs/archive/architecture-2026-09.md#native-kael-package-and-boss-selection)<br>
<a id="sin-star-i-presentation-host"></a>[Sin Star I presentation host](docs/archive/architecture-2026-09.md#sin-star-i-presentation-host)<br>
<a id="native-yalis-package-and-profile"></a>[Native Yalis package and profile](docs/archive/architecture-2026-09.md#native-yalis-package-and-profile)<br>
<a id="coordinated-standalone-battle-previews"></a>[Coordinated standalone battle previews](docs/archive/architecture-2026-09.md#coordinated-standalone-battle-previews)<br>
<a id="native-mira-and-preserved-comparisons"></a>[Native Mira and preserved comparisons](docs/archive/architecture-2026-09.md#native-mira-and-preserved-comparisons)<br>
<a id="party-beat-camera-ownership"></a>[Party Beat Camera Ownership](docs/archive/architecture-2026-09.md#party-beat-camera-ownership)<br>
<a id="preserved-frame-order"></a>[Preserved frame order](docs/archive/architecture-2026-09.md#preserved-frame-order)<br>
<a id="current-owners-and-maintenance-routes"></a>[Current owners and maintenance routes](docs/archive/architecture-2026-09.md#current-owners-and-maintenance-routes)<br>
<a id="validation-routes"></a>[Validation routes](docs/archive/architecture-2026-09.md#validation-routes)<br>
<a id="selectable-equipment-presentation"></a>[Selectable equipment presentation](docs/archive/architecture-2026-09.md#selectable-equipment-presentation)<br>
<a id="change-size-review"></a>[Change-size review](docs/archive/architecture-2026-09.md#change-size-review)<br>
<a id="native-town-authoring-ownership-september-26"></a>[Native town authoring ownership (September 26)](docs/archive/architecture-2026-09.md#native-town-authoring-ownership-september-26)<br>
<a id="town-guides-history-and-duplication-september-27"></a>[Town guides, history and duplication (September 27)](docs/archive/architecture-2026-09.md#town-guides-history-and-duplication-september-27)<br>
<a id="provisional-neris-castle-m01-comparison"></a>[Provisional Neris Castle M01 comparison](docs/archive/architecture-2026-09.md#provisional-neris-castle-m01-comparison)<br>
<a id="october-2-terrain-journey-maps-and-preparation-repair"></a>[October 2 terrain journey maps and preparation repair](docs/archive/architecture-2026-10.md#october-2-terrain-journey-maps-and-preparation-repair)<br>
