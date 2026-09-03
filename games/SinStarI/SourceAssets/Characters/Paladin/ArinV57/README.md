# Arin v5.7 Tripo Source Archive

Inspected on September 3, 2026 with Blender 5.2.1 LTS.

## Files

| File | Bytes | SHA-256 | Purpose |
| --- | ---: | --- | --- |
| `arin-v5.7-with-sword-and-shield.original.glb` | 3,475,428 | `87E9928CC0B80D1217C297001267D3B96D3BCD3380BFB251D74FCA505271857B` | Untouched equipped Tripo export |
| `arin-v5.7-no-sword-and-shield.original.glb` | 3,460,440 | `95A3C58CEC75D91D235122E456490FA3DD1B41784489DA9BED0CA9F94EC8F85B` | Untouched Tripo export labeled as unequipped |
| `arin-v5.7-no-equipment.cleaned.glb` | 3,424,928 | `B2168E7735140BEB0D3D65826BB85AACC74A9584E55F4C41A164063129886E54` | Working derivative with equipment meshes removed |

## Verified Contents

- Both untouched exports are valid glTF 2.0 binary files and import successfully.
- Each has one armature with 41 bones and no animation clips.
- Their bone names, parent hierarchy, and rest transforms are identical.
- Each has three embedded 2048 by 2048 JPEG textures.
- The two untouched exports contain the same named mesh set and render identically.
- The export labeled `No Sword and Shield` still contains `Sword`, `Shield`, `Shield Strap Main`, and `Shield Strap 2`.
- The cleaned derivative removes those four objects only. It retains all 41 bones, the same hierarchy, zero animation clips, and all three embedded JPEG textures.
- Blender re-export changes rest-matrix values only by a maximum observed floating-point delta of `0.000004619`.

## Animation And Grip Notes

These files provide a substantially better v5.7 source baseline, but the two static exports alone do not prove that an animation will deform correctly.

The equipped pose has a convincing modeled right-hand grip. The original `Sword` mesh, however, contains erroneous skin weights to unrelated right toe and thigh bones in addition to the right hand. Do not animate that sword with its current weights. Attach or rigidly weight the sword to `R_Hand` before using it in the viewer. Treat the shield and straps similarly on the left hand.

For the first animation checkpoint, animate the cleaned derivative while preserving this exact v5.7 skeleton and hand geometry. A separate source T-pose is not required: a temporary neutral A-pose or T-pose can be created in Blender for Mixamo retargeting, or a Tripo animation exported for this exact skeleton can be used directly. Do not directly apply v5.5 animation tracks to the v5.7 rest pose.
