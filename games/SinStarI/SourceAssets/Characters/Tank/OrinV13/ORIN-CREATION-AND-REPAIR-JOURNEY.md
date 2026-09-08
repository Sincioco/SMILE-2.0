# Orin v1.3 Creation And Repair Journey

Read this before changing Orin's model, rig, animation sources, equipment fit,
calibration, or VFX attachment points.

## Accepted source combination

The canonical result deliberately combines two compatible sources. The body
geometry, UVs, and JPEG materials come from `orin-v1.3.original.glb`. Skin
weights, inverse bind matrices, the 41-bone skeleton, and animation playback
come from the final Mixamo Idle export in
`Animations/orin-v1.3-mixamo-sword-and-shield-idle-with-skin.fbx`. Every other
accepted Mixamo clip must have the same rest-rig signature.

This transfer fixed the scrambled Mixamo body textures without changing vertex
order or skinning. The builder requires the pristine and Mixamo body meshes to
match by name, vertex count, face count, and local vertex positions. Do not
replace that check with a nearest-surface transfer.

## Equipment fit

The hammer and shield come only from `orin-v1.0-equipment-source.glb`. They are
rigid attachments to `R_Hand` and `L_Hand`. Orin holds the hammer at the butt of
its handle, upright and slightly forward. The shield is centered vertically on
his left hand and starts with a 40-degree outward flare. The correction matrices
in `scripts/build-orin-v1-3-mixamo.py` are the values Sin approved in Blender.
Do not reuse Arin's grip offsets.

## Animation set

The accepted runtime clips are Idle, SwordAttack, JumpAttack, ThorAttack,
Defend, Hit, Death, Victory, and Run. `ThorAttack` is the lightning-charge
contract. The pristine Angry clip exists only for armpit inspection and must
not be exported into the runtime checkpoint. Importing it accidentally creates
a tenth clip and causes strict profile validation to reject the model.

## Known geometry limits

Sin accepts the small armpit openings in v1.3. Do not close them during
animation or VFX work. The derived checkpoint removes only cooker-invalid
zero-area faces. The v1.0 body is rejected because of its cloth and forearm
deformation; its named equipment meshes remain valid.

## Viewer and calibration

Orin owns a separate profile, persistent-data key, calibration area, and JSON
snapshot. Export it with
`scripts/sync-arin-v5-7-calibration.ps1 -Character Orin -Mode Export -AllowMissing`
before commits. The Party tab evaluates Orin's own clips and corrections. His
arena pose uses a -55-degree visual yaw adjustment on top of live target facing
because the imported hammer stance's visible forward direction differs from
Arin's. The Character Viewer applies a shared standing correction plus measured
Block, Hit and Death contact curves by clip name, in both individual and Party
playback. The accepted Mixamo skeleton and source animations remain unchanged.

## Grounding lesson for the next character

Orin floated even at frame 0 of Block, Attack and Victory. This was a common
placement error, not three broken clips. Auto-fit used the bind-pose mesh's
minimum Y (about -0.116), while animated Idle began near +0.003. Applying that
bind-pose floor offset to the animated character raised him by about 0.119 model
units. The accepted Orin presentation correction is therefore -0.119 model
units before his additional measured contact curves. This number is specific
to Orin; never copy it to character 3.

Before accepting another character package:

1. Compare the skinned body minimum Y in the bind pose and animated Idle frame 0.
   Exclude weapons, shields and effects from the measurement. Record the model
   checksum, sample rate, coordinate units and measured values in the package.
2. Inspect every clip at frame 0 from a low, floor-level camera, with effects off.
   If all clips float by the same amount, fix the shared placement baseline first.
3. Sample Block and Hit contact through their final held poses, and Death through
   its settled pose. Check genuine jumping clips separately so intended flight
   is preserved. Do not floor-lock every animated sample indiscriminately.
4. Resolve corrections by runtime clip name, not UI button index. Their orders
   differ. Check both the character tab and Party's companion update path.
5. Place equipment overlays, sockets, light sources and VFX from the actor's
   final world transform after grounding and calibration. Orin's white hammer
   silhouette initially kept the old auto-fit Y, leaving it above the hammer
   after his body was lowered. Compare effects on/off at the same paused frame.

`scripts/measure-orin-grounding.py` reproduces this revision's body-contact
measurements in `Calibration/orin-v1.3-grounding.json`. Treat the procedure as
reusable; regenerate its data and asset-specific mesh selection for a new rig.

## Rebuild order

1. Run `scripts/build-orin-v1-3-mixamo.py` with Blender 5.2.
2. Confirm exactly nine actions and inspect `Previews/Idle.png` plus the changed
   action preview.
3. Run `tools/Character3DViewer/Build.ps1` to cook the SM3D asset.
4. Update `Calibration/orin-v1.3-profile.json` only when the canonical model,
   descriptor, or cooked SM3D hashes intentionally change.
5. Restore the calibration with `-Character Orin -Mode Restore -Force` only for
   an explicit profile migration, then launch through `Launch.ps1`.

## Lightning Attachment And Timing

The builder now runs `scripts/update-orin-lightning-sockets.py` after exporting
the accepted checkpoint. It verifies every equipment vertex is rigidly weighted
to the intended hand, applies the inverse bind matrix, derives eight shield
perimeter points and three hammer-head points, and updates only the descriptor.
Do not copy these points into a changed rig without re-deriving them.

Resolve those sockets through equipment parts 0 and 1 after calibration. A socket
queried through the body will ignore independent prop Move/Rotate edits. The
shield effect uses an exact closed polyline with no wandering branches; suppress
the old full-shield overlay so the face and back remain textured.

The viewer's SelectedClip and PartyCompanion.Clip are runtime indices. Resolve
their names with Character3D.ClipName, not the UI presentation-order table. This
distinction fixed initial lightning appearing during the wrong animation.

CPU charge contact/release are latched per action and independently tested.
Frame scrubbing previews visuals without repeated thunder or charge consumption.
Before the Death repair, the model checksum was 6DD3EC872CAD79FD28AD3B8D5A5228149CBC35C74652A69B6123922D94901936.

## September 5: Death root motion repair

Orin's Mixamo Death export stores the fall's global rotation/translation on the
armature object. Removing those object channels left him upright at the last
frame; a floor-height adjustment could never correct that orientation defect.
The accepted repair bakes that motion into the Root joint, aligned to the
accepted first pose, while retaining every other clip and all geometry, skin,
textures and equipment bytes. The source FBX is unchanged.

`scripts/repair-orin-death-root.py` reproduces the surgical repair from the
pre-repair checkpoint. `Calibration/orin-v1.3-death-root-repair.json` records
source/result hashes and joint-position alignment error. The normal builder now
bakes object motion before removing object channels. Inspect the whole
fall and its final horizontal pose, not just a mid-fall screenshot.

The regenerated grounding measurement and Death contact curve replace the old
upright-pose correction. The shared -0.119 baseline remains unchanged. Death
plays once and holds its settled pose. Defend is non-looping in both descriptor
and runtime policy; Party never restarts an already-held Orin guard.

Both individual and Party paths use the final grounded transform for equipment
and VFX. Current hashes live in `Calibration/orin-v1.3-profile.json`.

## September 5: per-actor storm ownership

`OrinStorm.smile` now issues generation-safe presentation contexts. Each context owns
only its actor's charge latches, clip/time history, effect handles, attack style, trail,
visibility, and error state. Destroying or recreating one context cannot invalidate a
second context. Shared Lightning initialization, frame advancement, drawing, and final
shutdown belong to the Viewer scene, and Orin borrows a scene-issued local-light lease.

Freezing Lightning or seeking while frozen rebases the context before playback resumes.
This prevents stale thunder, charge, or discharge thresholds from firing after a time
jump. The final grounded actor transform still drives every equipment socket and effect.
The canonical Orin model, descriptor, zero-key calibration snapshot, and package hashes
are unchanged by this runtime ownership repair.

## September 5: Jump Attack impact grounding

The Mixamo source confirms a single authored launch followed by a kneeling ground
smash and a grounded get-up. The accepted checkpoint had kept the skeletal pose but
discarded the armature-object descent, so samples 38–71 floated as much as 0.292
model units above the Idle sole height. This was not intentional jump motion.

`scripts/repair-orin-jump-attack-grounding.py` is a hash-gated surgical repair. It
preserves samples 0–36 and all non-JumpAttack data, then writes only the JumpAttack
Root translation from impact sample 37 through the last sample. The resulting 30 Hz
measurement keeps the launch arc and reports every impact/get-up sample at +0.003,
matching Idle. The accepted Death channels and contact report remain unchanged.

The model and cooked SM3D fingerprint migration retains Orin's zero-key calibration
identity; Arin's 23-key calibration is unrelated and remains unchanged.

## September 6: active-actor Party calibration

Orin's Party turn can now be paused and inspected with the shared timeline and
Pose panel. The inspector temporarily binds Orin's actor, key range and storage
metadata, then restores primary scene ownership. It never borrows Arin's saved
track. Native and generated-Web tests save and Undo only in disposable storage,
then verify complete per-character JSON identity and unchanged battle state.
The live Orin snapshot remains at zero keys. No model, grounding curve, animation
or fingerprint changed. Resume restores the demo clip/time that preceded preview.

Pose Calibration's Show Gizmo / Hide Gizmo control is shared with Arin. Handles
default off while numeric editing stays available. Visibility is a transient UI
choice, not a saved Orin correction or character-package change.

## September 6: live same-model correction and effect proof

`ActorIsolationTests.smile` reuses the current profile's standing baseline and
applies opposite temporary right-wrist corrections to two independent Orin
animators sharing one model. Each corrected hammer socket changes while the
other actor's socket stays exactly unchanged. Those fixture-local values are
never persisted. Real effect submissions are checked after freeze/hide, resume,
scene comfort Off and one context's destruction; stale light leases cannot
disable a replacement lease or the surviving actor.

The native Auto and forced shader-fallback runs pass. Visible Chrome and Edge
execute the same fixture with GPU trails; a disposable `fallback.html` uses the
existing shader-failure test hook and reports CPU fallback while the surviving
actor and lightning still render. The complete assertion output passes on both
paths. This is focused ownership evidence, not a hardware benchmark or a change
to canonical animations, equipment, package identity or calibration.

## September 8: precise runtime placement with unchanged calibration

The approved Double adoption keeps continuous Viewer/Party transforms, current-pose
socket anchors, equipment pivots and attached effects fractional through the existing
Character3D and renderer owners. GPU float32 acceptance remains the final rendering
precision limit. Saved wrist/equipment channel values, authored model scale, grounding
corrections, clip names, asset identities and animation sources remain integral and
unchanged. Do not copy another character's grounding numbers or bake runtime fractions
into the canonical model to reproduce this work.

The isolated native Viewer fixture still exports both character snapshots exactly and
round-trips their existing serialization. The real Party approach/return assertion
also reaches actual owned render submissions on native and Web. These are focused
runtime checks; the final interactive delivery state and source/artifact evidence are
recorded in `docs/implementation/double-precision-checkpoint.md`.

## September 8: enemy-centered ground discharge and shared lightning outline

Sin requested that Thor Attack strike farther ahead toward the enemy. The existing
Party owner already supplies the actual Dragon chest target, but `OrinStorm` had
centered the ground discharge on the hammer's X/Z. It now latches the target X/Z
at release and retains floor Y = 3.0. Charging remains at the hammer; release
timing, per-context ownership, sound cues and the existing ground-arc radius stay
unchanged. Released ground arcs do not follow later target motion.

Sin subsequently identified that a white surface coating washed out the hammer.
The reusable Lightning equipment appearance now uses a front-culled expanded
outline, retaining the underlying metal and grip details. `ViewerProfiles` selects
this style; `ViewerEffects.ConfigureEquipmentGlow` and `UpdateEquipmentGlow` apply
it to any primary actor or Party companion without an Orin-specific rendering path.
The Fire surface style remains available independently. The existing calibration
fixture checks both styles on Orin and Arin under fractional placement and three
calibrated poses. No character-specific glow offset is required.
Sin accepted the restored native material/outline, then requested stronger epic
lightning and a trail. The previous sparse attack-only trail moved into shared
`LightningVfx3D.WeaponTrail`; it now emits a denser edge corona and fading motion
trail. Orin supplies his calibrated SwordBase/SwordTip span; the shared operation
knows no model,
socket name, character identity or attack sequence. Other callers supply their own
precise edge points. Existing actor ownership, hide/freeze and capacity fallback
checks still pass on native/Web; the native attack capture shows the trailing arc
while the metal and grip remain visible.
Canonical model, animations, grounding and the zero-key calibration snapshot are
unchanged. Current visual and delivery evidence is in the Double checkpoint.

## September 8: three styles and reusable weapon outlines

Sin asked to preserve the original Lightning appearance while comparing Blue
Flame and Neon Arcs. Blue Flame uses Arin's shared fire family, blue/cyan/hot
white, emitted around the measured hammer-head perimeter with a world-space
trail and faint shield-edge flames. Sin accepted the native blue appearance,
then requested 40% stronger hammer flames: its baseline is now 140 versus Arin's
200 (70% of Arin), with the shield unchanged. Neon Arcs uses a stronger closed
neon rim plus short travelling arcs on outer edges, without star particles.
The original Lightning style and its star trail remain selectable.

Weapon and Shield intensity controls independently scale each character's
baseline from 0–200%. They are session presentation preferences, not pose keys.
The asset-local perimeter belongs in OrinEquipmentContours.smile; other weapons
author their own contour for the same Fire/Lightning operations. The generator
reads the accepted rigid head vertices and converts glTF Z to cooked SM3D Z
before applying the existing SwordBase socket matrix. Omitting this conversion
visibly detached the initial test flame; the corrected path is checked against
the actual authored HammerHead socket under an independently calibrated actor.

Do not add sockets to this accepted descriptor merely for VFX presentation:
descriptor changes invalidate its live calibration fingerprint. The current
separate contour data leaves the 21 sockets, model, cooked asset, animation,
grounding and calibration identity untouched. No historical Doctor/repair replay.
