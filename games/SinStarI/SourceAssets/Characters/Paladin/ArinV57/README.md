# Arin v5.7 Self-Contained Character Package

Inspected on September 4, 2026 with Blender 5.2.1 LTS.

This directory is the canonical repository home for the complete Arin v5.7 revision. It keeps the original and cleaned models, rig references, animation sources, accepted viewer checkpoint, runtime descriptor, human-readable pose calibration, previews, package manifest, checksums, and v5.8 handoff knowledge together. Sin Star I owns this package; the Character Viewer/editor consumes a disposable tool-local cooking copy prepared from it.

`arin-v5.7-package.json` is the machine-readable package index. `Calibration` holds the permanent pose-correction JSON. `arin-v57-idle-previews` holds the accepted checkpoint preview frames. Reusable build tooling remains in the repository `scripts` and `tools` directories rather than being duplicated inside the character package.

## Files

| File | Bytes | SHA-256 | Purpose |
| --- | ---: | --- | --- |
| `arin-v5.7-with-sword-and-shield.original.glb` | 3,475,428 | `87E9928CC0B80D1217C297001267D3B96D3BCD3380BFB251D74FCA505271857B` | Untouched equipped Tripo export |
| `arin-v5.7-no-sword-and-shield.original.glb` | 3,460,440 | `95A3C58CEC75D91D235122E456490FA3DD1B41784489DA9BED0CA9F94EC8F85B` | Untouched Tripo export labeled as unequipped |
| `arin-v5.7-no-equipment.cleaned.glb` | 3,424,928 | `B2168E7735140BEB0D3D65826BB85AACC74A9584E55F4C41A164063129886E54` | Working derivative with equipment meshes removed |
| `arin-v5.7-mixamo-rigged-t-pose.fbx` | 2,818,128 | `F9807FA88D9AC205A37CEA4568C86BFBA1123D4EA36D81F124CFABF47B67A742` | Approved Mixamo auto-rigged neutral reference |
| `arin-v5.7-mixamo-sword-and-shield-idle-with-skin.fbx` | 3,129,856 | `65B78FC6C06366E6B3D8619072A34277C2213C4B56FCEF8BFE5C77F2EA1654C6` | Skinned Mixamo reference used for the shared rig and weights |
| `arin-v5.7-idle-equipment-checkpoint.glb` | 6,742,636 | `393D82C06ECCEDF5A13CF3CA835700AA03A6E90ED74B1420569902885E3E1524` | Eight-clip viewer/editor checkpoint |
| `ArinV57.sm3d.json` | 1,043 | `EDD7B9D5811A32D22EDDBFEE86178264A7E45C04BFF74245CDACFF335B6FC3D2` | Runtime clips and socket descriptor |

## Permanent Pose-Calibration Workflow

`Save Frame` writes the live multi-keyframe track to the stable `smile.tools.character3d-viewer` application-data identity, so rebuilding or renaming the executable does not lose current work. Launching through `tools\Character3DViewer\Launch.ps1` watches that live file and converts every saved change into `Calibration\arin-v5.7-pose-calibration.json` in this folder.

Before any normal Arin v5.7 or Character Viewer calibration commit, Codex runs `scripts\sync-arin-v5-7-calibration.ps1 -Mode Export -AllowMissing`. This makes the readable calibration part of the ordinary repository commit and push. On a fresh workstation or after application-data removal, the launcher regenerates the runtime binary from repository JSON only when no live working copy exists; it never overwrites newer live edits automatically.

## Animation Sources

| File | Bytes | SHA-256 | Checkpoint use |
| --- | ---: | --- | --- |
| `arin-v5.7-mixamo-sword-and-shield-calm-idle-without-skin.fbx` | 302,096 | `434046D23E41ADC856AED5CF9E0DE7AEAC622900F7B13625F0A5A03D041367E2` | `Idle` |
| `arin-v5.7-mixamo-walk-without-skin.fbx` | 213,856 | `B84A5D5960049C9F54A0DFEFC7EECBE13F4681C4F0A686B0F2709A49481D6D3E` | `Walk` |
| `arin-v5.7-mixamo-run-without-skin.fbx` | 184,688 | `6B058650844EB8EC1E5BDE96E025BDBF616328562AEABDEA15ADB77B3B917C71` | `Run`; shield arm stabilized |
| `arin-v5.7-mixamo-defend-without-skin.fbx` | 229,296 | `1E00AF02F647675E7390B5445572D454A0F94C76D5D51A7A04C417E6B9622D2F` | `Defend` and shield-arm reference pose |
| `arin-v5.7-mixamo-sword-and-shield-slash-4-without-skin.fbx` | 232,608 | `CD58D062937ED5A4CCEFF99752538D6890C10BB953A73390411D15DCFB5094A9` | `SwordAttack`; Sword And Shield Slash (4), downloaded on the v5.7 Mixamo rig |
| `arin-v5.7-mixamo-sword-and-shield-hilt-melee-without-skin.fbx` | 201,216 | `92FC18033DA263BF1AC44C847A85E1A3D71CFEFBA2871E1F9C8E481921955852` | `SwordAttack2`; retained v5.7 compact hilt-melee strike |
| `arin-v5.7-mixamo-block-impact-without-skin.fbx` | 191,680 | `2ACCB7FF446CFEDA50CCE6395A7A2B3F2F1F55BDAB45ED3A990888E92B596355` | `BlockImpact`; shield arm stabilized |
| `arin-v5.7-mixamo-hit-without-skin.fbx` | 199,376 | `4E34878066F6139C8B51F939D011EE53EEF0345838200B45288DD853D9573B6F` | `Hit`; shield arm stabilized |
| `arin-v5.7-mixamo-ko-without-skin.fbx` | 272,816 | `21DAD8BC8D9B1B2AA9DD03FFA6E6F55FB6C6B54CC5BA75C1F6FD57693B8470CD` | Archived but rejected because the fall forces the equipment through the body |

## Verified Contents

- Both untouched exports are valid glTF 2.0 binary files and import successfully.
- Each has one armature with 41 bones and no animation clips.
- Their bone names, parent hierarchy, and rest transforms are identical.
- Each has three embedded 2048 by 2048 JPEG textures.
- The two untouched exports contain the same named mesh set and render identically.
- The export labeled `No Sword and Shield` still contains `Sword`, `Shield`, `Shield Strap Main`, and `Shield Strap 2`.
- The cleaned derivative removes those four objects only. It retains all 41 bones, the same hierarchy, zero animation clips, and all three embedded JPEG textures.
- Blender re-export changes rest-matrix values only by a maximum observed floating-point delta of `0.000004619`.
- The Tripo body is an open surface in several concealed areas. Blender reports 6,533 open/non-manifold boundary edges. Retopology is the correct source-side fix for visible hollow-shell gaps; it is not caused by the Character Viewer material path.

## Animation And Grip Notes

These files provide a substantially better v5.7 source baseline, but the two static exports alone do not prove that an animation will deform correctly.

The equipped pose has a convincing modeled right-hand grip. The original `Sword` mesh, however, contains erroneous skin weights to unrelated right toe and thigh bones in addition to the right hand. Do not animate that sword with its current weights. Attach or rigidly weight the sword to `R_Hand` before using it in the viewer. Treat the shield and straps similarly on the left hand.

For the first animation checkpoint, animate the cleaned derivative while preserving this exact v5.7 skeleton and hand geometry. A separate source T-pose is not required: a temporary neutral A-pose or T-pose can be created in Blender for Mixamo retargeting, or a Tripo animation exported for this exact skeleton can be used directly. Do not directly apply v5.5 animation tracks to the v5.7 rest pose.

## Mixamo Export Normalization

Every Mixamo animation FBX exported for Arin v5.7 must be normalized before GLB or SM3D cooking. Mixamo imports into Blender with a uniform `0.01` armature-object scale, mesh-object scale `100`, and bone-location animation values expressed in the pre-normalized armature units. Blender compensates for this combination, but leaving it intact makes Arin render approximately 100 times too small in the SMILE Character Viewer.

Use the following normalization procedure for every newly downloaded Mixamo animation:

1. Import the FBX and confirm that it contains exactly one armature and the expected Arin hand meshes.
2. Record the armature object's positive uniform scale.
3. Apply the armature scale so the armature and child meshes have object scale `1`.
4. Multiply every pose-bone `.location` animation key, handle, and sampled value by the recorded scale. Do not scale rotation or scale curves.
5. Confirm that `mixamorig:Hips` exports without a `0.01` node scale and that its descendant bone translations are in meter-scale units.
6. Re-import the exported GLB and visually verify at least the first, middle, and final animation poses before cooking it to SM3D.
7. Inspect the cooked SM3D bounds and launch the Character Viewer at its normal fit setting; do not hide a unit error with camera or zoom constants.

The Arin v5.7 checkpoint builder implements these steps automatically and reports the detected Mixamo scale in its diagnostics. Rigidly fitted sword and shield transforms must be calculated after normalization so they remain aligned with the normalized hand meshes.

## Repeatable Checkpoint Build

Run `scripts\build-arin-v5-7-idle-checkpoint.ps1` from the repository root. The builder:

- uses the approved rigged T-pose to straighten both wrist-to-forearm relationships across every clip;
- rolls the shield wrist 135 degrees outward around the forearm so its face protects Arin's forward view;
- rolls the sword wrist 135 degrees outward so the knuckles face away from the body while keeping the handle centered in the fist;
- independently realigns the rigid shield and sword around those corrected grips so the shield faces forward and the blade stays outside Arin's body;

1. Uses the skinned Mixamo FBX as the authoritative 65-bone rig and weight source.
2. Imports every action declared by `arin-v5.7-animation-set.json` and rejects any skeleton mismatch.
3. Restores the pristine Tripo body meshes, UVs, materials, and 2048 by 2048 embedded JPEG textures while transferring the Mixamo weights by exact nearest geometry.
4. Normalizes the Mixamo `0.01` object scale and the corresponding bone-location keys.
5. Rigidly attaches the sword to `mixamorig:RightHand` and the shield plus both straps to `mixamorig:LeftHand`. The sword receives a separate but visually identical material datablock so the cooker retains independently addressable shield, sword, and body parts without duplicating texture references.
6. Applies the approved centered grip and outward shield offsets. The sword correction is XYZ `(-15.51063048, -43.72768386, -81.06488564)` degrees, offset `(-0.04017985, 0.00752897, 0.01881249)`, pivot `(-0.01415075, -0.00344447, 0.01844119)`, and final attachment-axis correction `(0, 135, 0)`. The shield correction is XYZ `(0, 0, -75)` degrees with offset `(0, 0, -0.055)` and final attachment-axis correction `(0, -45, 0)`.
7. Holds the left shoulder, upper arm, forearm, and hand at the collision-free `Defend` frame 22 pose for actions marked `stabilizeShieldArm`, and holds the right equipment arm at the forward `Idle` frame 1 guard for non-attack actions marked `stabilizeSwordArm`.

The accepted checkpoint contains eight clips: `Idle`, `Walk`, `Run`, `Defend`, `SwordAttack`, `SwordAttack2`, `BlockImpact`, and `Hit`. `SwordAttack` uses Sword And Shield Slash (4), downloaded directly on the v5.7 Mixamo rig. `SwordAttack2` preserves the prior v5.7 Hilt Melee motion. `Idle`, `Walk`, `Run`, and `Defend` loop; the reactions and attacks do not.

After building, run Blender in background mode with `scripts\audit-arin-v5-7-animation-set.py`. The earlier September 4 audit evaluated all 261 frames across the previous seven-clip checkpoint and found zero shield-to-body contacts and zero critical sword-to-body contacts. The newly added `SwordAttack` must be manually reviewed in the viewer before its collision behavior is accepted. The audit intentionally permits the sword guard/handle to touch the adjacent right gauntlet because that constant contact is part of the modeled grip. All other sword/body contacts fail the checkpoint.

## Arin v5.8 Retopology Handoff

If Tripo retopology changes topology or vertex order, repeat Mixamo rigging and weight transfer rather than reusing v5.7 vertex weights blindly. The normalization, pristine-texture restoration, rigid equipment attachment, centered sword transform, outward shield transform, shield-arm stabilization, descriptor wiring, and full-frame audit are reusable. Re-run the builder against the v5.8 inputs, verify the hand-fit diagnostics, and inspect the generated first/middle/final renders before promotion.

## Retirement Condition

Arin v5.4, v5.5, and v5.6 are retained only as unsuccessful diagnostic history. After v5.7 passes animation deformation, right-arm and wrist continuity, sword-grip, equipment attachment, and Character Viewer checks, the earlier model sources, cooked assets, textures, and candidate-specific build records are superseded and may be safely deleted in a dedicated cleanup commit.
