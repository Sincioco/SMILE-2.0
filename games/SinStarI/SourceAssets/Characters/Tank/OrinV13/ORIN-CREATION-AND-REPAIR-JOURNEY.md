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
Arin's. The Character Viewer also applies a measured presentation-only ground
curve to Death frames 42 through 118. It removes the visible airborne pause
during the fall and lets the final pose rest on the arena floor without changing
the accepted Mixamo skeleton or source animation.

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
The accepted model checksum remains 6DD3EC872CAD79FD28AD3B8D5A5228149CBC35C74652A69B6123922D94901936.
