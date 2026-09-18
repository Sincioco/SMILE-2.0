# Character Viewer Architecture

## Kael silver hair and separate native Fire Lab

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
