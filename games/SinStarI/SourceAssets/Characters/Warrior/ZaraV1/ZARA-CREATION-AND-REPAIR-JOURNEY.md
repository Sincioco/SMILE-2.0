# Zara V1 Creation And Repair Journey

## September 10: full weapon contour, red storms and original audio

The descriptor's ten weapon sockets now span 99.97 percent of the measured weapon
Y extent, covering the handle/guard and full blade. The outline uses a white core
and red-tinted rim; the source gold emission remains authored. The GLB is unchanged:
`94f96990aed59cb347dee1d831e19587d21b92ed217a11d630a507cf8c1df67b`.

SwordAttack uses Lightning Lab's Godstorm Ultra layout (four StormCrown strikes);
SwordAttack2 uses Forked Judgment (four SkyStrike branches). Red-white palette and
reduced white spark intensity retain Zara's red theme. Charge begins at 5 percent,
and the storm/contact/audio cue at 25 percent of the actual animation duration.
Primary and Party contexts share the controller but own independent effects.

Only sound was imported from Unity. `unity-audio-import.json` records source GUIDs,
relative paths, original hashes and PCM playback copies. Original attacks' digital
discharge is layered with existing Lightning Lab thunder on separate channels.
No Unity visual effects, rig re-export or new audio download was used. Source and
playback audio stay under ignored `Private/Audio`; the standard preparation owner
stages runtime copies. Public-roster builds exclude them.

The previous SMILE appearance is retained locally in
`artifacts/deliverables/Zara-SMILE-before-Unity-VFX-20260909`, including sources,
descriptor and a native/Web preview archive. Current evidence is in the equipment
VFX checkpoint; earlier equipment descriptions below are historical.

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
clips per page. Measured equipment VFX sockets are authored for this revision; the equipment
stays part of the original animated mesh and pose editing remains unavailable. Arin/Orin pose saves are not
used for this character. Source emission textures remain intact.

Private/Previews contains native inspection and Party screenshots. The private
Blender source retains full skin weights; the Viewer derivative keeps the strongest
four, with measured losses in skin-weight-audit.json.

Pipeline scripts in Private/Pipeline preserve the local conversion recipe. Their
working-directory paths refer to artifacts/temp/unity-import; stage a working copy
there before rerunning. convert_full.py requires the original Unity project for
GUID/material lookup. Do not rerun the one-shot grounding code generator over an
already generated Profiles.smile. The canonical descriptor, GLB checksum and
grounding corrections must be updated together after any re-export.

## September 9: equipment glow

Weapon part 3 uses the Weapon bone. Ten sockets define the weapon center and
eight measured blade rim points. A pure white ribbon halo reinforces the skinned
white outline; the original gold emission texture remains intact. No shield or
fire emitter is allocated for Zara.

The same per-character effects run in the individual tab and Party, using the
final grounded transform and the actor’s animator. Hide/reload releases owned
effects. See equipment-vfx-attachments.json for source-model hashes and measured
points, and tools/Character3DViewer/ARCHITECTURE.md for current effect ownership and validation routes.
No model re-export, texture edit, grounding change or shared calibration was needed.

## September 11: Beat Camera Head Reference

The descriptor exposes a generic Head socket on the verified existing head bone.
The Party camera editor queries that animated socket through Character3D and lets
the user offset/resize its framing box independently of pose calibration. This is
attachment metadata only; the GLB, materials, animations and grounding are unchanged.
