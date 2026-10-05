# Runtime, frame and persistence contracts

[Architecture entry](../ARCHITECTURE.md) · [Character workflow](characters-and-calibration.md) · [Battle workflow](battles-and-cameras.md)

These are maintained contracts for the native Viewer/Studio session. The older
[architecture evidence](archive/architecture-2026-09.md) records why they exist,
including historical targets and source-size measurements. Current source and
repository instructions resolve conflicts; no archived PASS is a fresh validation.

## Session and host lifetime

`NativeProgram` creates one `ViewerWorkflow.Session` and `NativeViewerHost.Session`,
opens the window, and delegates input, update, draw, overlay and release. Native
close deferral follows `Viewer.HasEdits()`. `NativeViewerHost` coordinates native
navigation, Battle System and NerisTown; domain algorithms stay in their owners.
The game embeds the same host with Studio navigation disabled, retaining title
navigation/music/lifetime and session EXP itself. Existing separate StudioShell
compatibility work is historical; it is not a second active application design.

`ViewerWorkflow` retains one set of actors, playback, camera, calibration, effects,
rendering and Beat state. Start/reset/retry/switch/release are lifecycle operations,
not opportunities to create parallel scene engines. A failed switch/loading splash
retains the previous scene, errors, pending edits and captures. Immutable Character3D
assets may be cached/shared; live actor instances never share pose, animator, equipment,
calibration or effect state. The unused-asset cache is bounded and eviction/retry and
renderer-reset invalidation stay explicit. Stop/resume must not turn time away into
a giant animation step. Actual close releases scene resources and audio.

## Frame order

The shared Viewer workflow preserves this order:

1. Read queued key events and commands, then route UI pointer ownership before camera input.
2. Sample/clamp the shared clock; retain raw time for FPS and separate animation/camera/presentation elapsed values.
3. Advance Party/action choreography and update the primary actor.
4. Apply grounding, current-frame calibration, wrists and equipment coupling.
5. Advance smooth zoom/orbit and compose fit, controls, close views and calibration anchors.
6. Place final equipment and stage equipment/companion/Dragon effect endpoints.
7. Apply the current-pose Party camera and floor clearance.
8. Update audio, storm state and the once-per-scene VFX boundary.
9. Update optional socket gizmos and current bounds.
10. Begin the scene, draw arena/actors/effects/gizmos and end the checked draw transaction.
11. Draw overlays, present and handle close protection.

The first failure stays with session diagnostics at the stage that encountered it.
Directed battle playback and Beat preview adapt the same actors, clock boundaries
and renderer rather than adding a second frame loop. Each capture/view is a scoped
render operation; it must not double-advance live animation, audio or VFX.

## Calibration and edit transactions

Sin's accepted exported JSON in the versioned character package is authoritative.
The synchronizer receipt records the last JSON **content hash**; replaced JSON wins
regardless of timestamp and matching exports preserve its exact bytes. Native binary
Save Data is a working copy. Packaged defaults seed only missing storage, never an
existing intentionally cleared or rejected save. Arin and Orin have independent
keys, asset/clip/socket fingerprints and calibration banks.

`ViewerCalibration` owns tracks, strict codecs, checked primary/backup reads and
transactional writes. Editing/controls own draft/undo, coupling/grip compensation
and numeric changes; gizmos only project/pick and route mutations. Save Frame stores
all 20 channels. Failed writes preserve old bytes/track/undo and keep the temporary
preview for Retry/Cancel. A bad load blocks writes; the import recovery retry reads
through the same validator and cannot bypass identity. Unknown/corrupt data is never
silently replaced. Import requires explicit full-track replacement confirmation.
Concurrent processes/tabs do not merge.

Pose and Beat drafts block replacement by navigation until saved/canceled. Frame
steps consume the queued key event's modifier snapshot, so quick Ctrl+arrow taps
remain steps after Control is released. Timeline/gizmo capture stays exclusive
through off-window motion and the release frame. Scrolled inspector coordinates
must not leak into viewport picking. See the [calibration guide](characters-and-calibration.md)
for complete controls, schema/transfer limits and canonical package routes.

## Camera and reflection ownership

Double positions flow through shared `Precision3D`/`PrecisionCamera3D` and the
[precision boundary](../../../docs/libraries/precision3d-boundary.md); GPU float32
and depth limits remain. Do not rescale canonical assets to hide camera quantization.
Shared arena controls retain partial pointer motion, bounded target zoom and easing.
Town overview, authored initial camera, viewport-center orbit, Party following and
orthographic editing are separate policies over those shared operations.

`ViewerRendering` applies arena reflection preferences before scene Begin. The
renderer replays frozen opaque/masked submissions, then eligible transparent
meshes/particles/trails, using its reflection target and depth. It never updates an
actor twice or admits duplicate effects. The floor is the receiver; the backdrop
reflection uses only the screen-fixed art visible above the floor edge. Grid and
gizmos are excluded; the grid draws above the receiver. Heat distortion is excluded
and reflected particles do not sample the main-camera soft-intersection depth.

Floor and VFX reflection preferences are independent default-on session state;
Original/hidden floor suppresses capture without erasing preferences. Allocation
failure retains the matte scene and exposes Unavailable. Shared renderer callers
retain their existing opaque-only default unless they opt into VFX reflections.

## Effects and actor isolation

Each actor's effect context owns handles, endpoints, clip/time history, artistic
style, visibility and first error. `SceneVfx3D` advances shared families once after
endpoints are staged. Scene pause freezes choreography, while Fire/Lightning freeze
controls are independent. Hide/destroy is evaluated before freeze: old emissions,
trails or lights cannot survive a hidden owner or reappear from a stale snapshot.

Explicit cuts and discontinuous seeks clear/rebase incompatible history without
replaying skipped impact audio; unchanged frozen actions retain their snapshot.
Continuous/automatic demo transitions may let accepted tails fade. Resume never
replays catch-up thunder or discharge. `BattleAudio.CrossedCue` shares clip-time cue
policy; owned channels stop on pause/teardown. Recovery diagnostics are cosmetic
cue submissions, not proof of audible playback or combat outcomes.

Equipment uses final grounded/calibrated socket transforms and caller-provided
contours. Do not add character-shaped renderer branches or independent offsets to
hide geometry intersections. `ViewerEffects` owns session Weapon/Shield intensity
and style preferences; visibility, coupling and saved pose authority remain separate.
`OrinStorm` and `ArinShieldRim` retain independent generation-safe contexts and
transactional resource admission. Incomplete candidates clean up and can retry.

The scene shares twelve fire emitters and eight lightning effects across actors.
Native rendering has a 16,384-particle staging limit allocated on demand (Web: 8,192)
and a separate persistent GPU pool of 32,768 aggregate slots used by Fire; see the
[renderer particle contract](../../../docs/architecture/renderer3d-gpu-particles.md).
Budget actors and families together; one equipment style does not reserve the
renderer. Reflections consume no extra simulation/admission. The
scene owns the Full/Reduced/Off comfort ceiling; one actor cannot raise another's
flash/shake policy. Native GPU/fallback and cleanup evidence must remain distinct.

## Beat Camera persistence and sampling

`BattleCameraShots` owns actor-independent Double frames, motion/link interpolation
and bounded serialization. Shot-frame v2 keeps horizontal longitudinal distance
unbounded but limits its vertical influence to the head span. Ordinary v1 shots
retain their semantics; extreme v1 shots use built-in fallback until recaptured.
Linked directions follow a stateless bounded arc, with deterministic opposing-view
and parallel-up repairs. Socket/head bounds are framing references, not collision.

`BattleCameraTimeline` owns four main markers plus up to sixteen extra shots.
`EffectiveValid` checks final rounded times: positive main intervals, unchanged
total action duration, unique reachable markers and 16 ms spacing. Invalid retiming
leaves the draft intact; changed action timing can use safe four-marker playback
without overwriting stored bytes. Camera intervals never retime movement, impacts,
VFX, audio or gameplay. `ViewerBeatSequence` samples existing action poses with zero
live elapsed and restores prior turn, clips, times and playback on close/cancel.

`ViewerBeatEditor` owns sequence drafts and clipboard. `ViewerBeatTimeline` owns
timeline gestures; `ViewerBeatHeadPersistence` owns per-identity current/persisted
head state and independent retry/discard. Current head capacity is eight identities
(`CHARACTER_COUNT`), not the older seven-identity milestone description. Camera Save
does not clear a failed head warning. Head changes auto-save independently and camera
Cancel does not undo them. Pending head recovery participates in close deferral.
Raw viewport coordinates remain separate from scrolled inspector coordinates.

Sequence V2 records contain timing/markers/shots; legacy Beat1-4 records remain as
read-only defaults when no valid V2 exists. A saved reset intentionally supersedes
legacy custom shots. Head records, camera sequences and pose JSON are separate;
copying a shot never overwrites another character's head definition. Native and
browser-origin saves remain separate. The [battle guide](battles-and-cameras.md)
holds the full user operations; the [checkpoint](../../../docs/implementation/party-beat-camera-checkpoint.md)
holds failure/recovery evidence and its unresolved original rejection trigger.

## Directed Battle System

`BattlePlanning` owns orders, legal repetition/interruption and one-time session
EXP/reward application. `BattleProgression` owns curved stat/reward policy.
`BattlePresentation` borrows existing actors for approach/action/return, reactions
and feedback; it captures homes after formation placement. `BattleOutcome` owns
ending/reward display and clocks, not the authority to grant/revoke awards.
UI/HUD/growth-view owners draw and route input without resolving combat.

`BattleCinematics` selects lifecycle phases while `ViewerParty` supplies shared
formation and attack cameras. Directed mode uses the existing playback bridge,
then restores ordinary Party ownership when leaving. Orders wait in their accepted
view; camera-return and pan/orbit/zoom do not silently stop Auto. New orders and
inspector handoff wait for the round boundary. Pausing freezes combat/action/restart
clocks but keeps camera inspection. Detailed timings, reset/EXP policy and controls
remain in the [battle guide](battles-and-cameras.md#native-battle-system).

## Guardrails and checks

The coordinator exception preserves an ordered shared workflow, not permission
to put new feature algorithms there. Keep state and behavior together in focused
owners, and review public boundaries, import direction, resource lifetime and growth.
Do not relax test assertions, identity checks or resource limits to hide defects.
Use bounded native fixtures and a short changed-behavior inspection. Historical
Web/generated-source runs do not satisfy current native acceptance, and vice versa.
See [validation](build-and-validation.md#focused-validation),
[export acceptance](viewer-export.md#acceptance-boundary) and [open status](status.md).
