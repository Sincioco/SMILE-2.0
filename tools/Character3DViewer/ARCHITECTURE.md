# Character Viewer Architecture

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
| `NerisTownCameraPanel` | Standalone numeric controls and Orbit/Fit; no panel background |
| `NerisTownAppearance` | Lighting/water clock and effect lifecycle |
| `NerisTownTrees` / `NerisTownFlowers` | Reused template draw objects; trees also own recycled leaves |
| `NerisTownCrystals` / `NerisTownMap` | Halos/sparkles; map images/projection/fade/leader coordinates |
| Generated `Layout`, `Site`, `Obstacles`, `EntrancesData` | Saved Blender data; no behavior |

### Blender and export boundary

`Blend/Neris-Town-Waterfront.blend` is current; the reproducible input is preserved
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

Party movement is explicitly enabled by Tab. Town startup, orbit, pan and camera
framing commands leave WASD/arrows in inspection mode. `NerisTown` owns the mode
and dispatches movement; `FreeCamera3D` owns only translation velocity, and
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
