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
