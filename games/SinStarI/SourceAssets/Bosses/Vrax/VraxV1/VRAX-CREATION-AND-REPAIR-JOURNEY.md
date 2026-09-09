# Vrax V1 Creation And Repair Journey

This package owns the validated local Unity character import. Native and Chrome
inspection, full clip inventory and measured floor-contact validation are recorded
in the Unity import checkpoint. Original files and full-weight sources are preserved.

Original selected-prefab dependencies, textures and animation FBXs are preserved
under `Private/Originals`. The full animation Blender source and GLB derivative
are beside them. `Private` is ignored pending asset redistribution permission.
The original Unity project has not been migrated or edited.

Conversion transfers evaluated poses to the model's own bind rig, keys quaternion
rotations explicitly and preserves the armature transform. Removing Valor's single
armature object corrupted exported root animation; retain it, and place skinned
mesh nodes at scene identity. Do not correct that defect with Viewer rotation.

Zara and Vrax use approximately 45% of their original triangle count. Valor keeps
his original triangle budget. Derivatives use 1024-pixel textures and four skin
influences. Tiny triangles and undefined tangents are cleaned in the derivative.
Valor Attack3 has zero-scale shoulder keys; its derivative uses 0.000001 scale.

Use the animation manifest and grounding audit for this revision. Never copy
Arin/Orin grounding constants. Frame zero of every clip and full Block/Hit/Death contact curves have been
measured. Individual and Party native/Chrome previews were inspected. Corrections
resolve by clip name and preserve authored jumps; see grounding-validation.json.

The common image cooker must use pixel coordinates, independent of PNG DPI;
otherwise these texture atlases are enlarged and cropped despite correct UVs.

## Viewer Handoff

The descriptor contains the complete animation set; the inspector exposes nine
clips per page. Four VFX sockets now follow the head and forearm bones; imported
equipment stays part of the original animated mesh. There are no pose-calibration
channels, and Arin/Orin pose saves are not used for this character. Source emission
textures remain intact.

Private/Previews contains native inspection and Party screenshots. The private
Blender source retains full skin weights; the Viewer derivative keeps the strongest
four, with measured losses in skin-weight-audit.json.

Pipeline scripts in Private/Pipeline preserve the local conversion recipe. Their
working-directory paths refer to artifacts/temp/unity-import; stage a working copy
there before rerunning. convert_full.py requires the original Unity project for
GUID/material lookup. Do not rerun the one-shot grounding code generator over an
already generated Profiles.smile. The canonical descriptor, GLB checksum and
grounding corrections must be updated together after any re-export.

Vrax now uses 20000% scale, twice the first preview capped at 10000%. The same
profile applies in his tab and Party. Orin effects must not depend on a Vrax Chest
socket: the Party owner uses current world bounds as the fallback target.

## Mouth Fire And Arm Lightning — September 9, 2026

The canonical descriptor owns VraxMouth/VraxMouthAim on `head`, and VraxLeftArm/
VraxRightArm on `lowerarm_l`/`lowerarm_r`. The original rig's Root has 0.01 scale;
socket translations are in bone-local source units, not Viewer world units.
The two head points establish the animated forward direction. Forearm offsets
place the bolts outside the arm surfaces. Native and Chrome inspection confirmed
the fire origin inside the open mouth and both bolts following the arms.

Attack through Attack6 use their own duration, active from one-sixth to four-fifths
of clip time. `ViewerDragon` owns the effect policy and independent caller state;
the same operation runs in the individual tab and four-hero Party. Existing shared
FireEmitter3D and LightningVfx3D resources provide fire and blue-white bolts.
The model, textures, all 24 animations, grounding corrections and 20000% transform
scale are unchanged. The GLB remains SHA-256
`ec863933e6898fb81672aaa979eada343b3b2eedcdc433a8b2c65bfe7a8a84fb`.
See the equipment VFX checkpoint for validation and whole-scene budgets.
