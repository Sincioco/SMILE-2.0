# Valor V1 Creation And Repair Journey

## September 10: Unity / SMILE material comparison

The original Unity BattleSystem scene was inspected without migration or edits.
Valor's metallic surfaces show stronger environment reflections there. The source
material/GLB audit confirms that the exported armor, body, helmet, sword and shield
retain normal maps and packed metallic/roughness maps. The GLB remains
`87089404dc88e6fb23d63500808c4d3c115a8a43974bd940444270e42e420dd5`.

Two import differences remain: the Unity armor/equipment `_Color` multiplier is
0.8 RGB while the derivative uses the default 1.0; eyes have source smoothness 1
but the no-map fallback roughness is 0.5. These are recorded comparison findings,
not accepted material changes. A bounded material-only conversion review should
preserve the original derivative, transfer those factors, and compare matching
poses/lighting before changing the canonical checksum. Do not rerun rig conversion.

Unity's scene also uses a skybox reflection environment. SMILE currently evaluates
ambient plus direct PBR lights, without environment-image lighting on armor; the
planar floor reflection is a separate feature. Exact visual parity therefore needs
a reusable environment-reflection capability, beyond this comparison request.
The corrected Web face-normal orientation helps the current exported materials
without adding GPU work. No Valor asset was edited during this comparison.

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

Weapon part 10 and shield part 9 use Sword_Joint1 and Shield_Joint. Fourteen
sockets follow the measured blade segment and shield perimeter. Sword and faint
shield flames reuse Arin behavior with Orin blue/cyan/white palette. The shield
has an independent white-core, blue-halo ribbon outline.

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
