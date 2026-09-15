# Yalis v1 authoring package

Sin supplied Yalis's body, sword and five orthographic reference images on
September 15, 2026. This folder is the canonical, self-contained Yalis v1 package
for Sin Star I and the native SMILE 2.0 Character Viewer.

## Current state

Yalis is a native candidate fighter with a quick, one-handed sword style. Her raw
body contains 1,086,254 triangles and her raw sword contains 1,648,018 triangles.
Neither source contains a skin or animation. The unchanged originals are preserved
under `Source`; their SHA-256 hashes are recorded in `package-manifest.json` and
`checksums.sha256`.

The runtime body has 44,907 triangles and retains its three 4096 x 4096 PBR maps.
The runtime sword has 5,996 triangles and three 2048 x 2048 PBR maps. Together they
use 50,903 triangles and 240 MiB of decoded texture data. The exported body expands
to 62,565 glTF vertices, leaving 2,970 vertices below the current SM3D per-primitive
65,535-vertex limit. No source scale or compiler workaround is required.

`yalis-v1-animation-checkpoint.glb`, `YalisV1.sm3d.json` and
`Blender/yalis-v1-rigged-animation-checkpoint.blend` are the portable runtime source,
descriptor and accepted Blender checkpoint. The model has 29 bones, ten clips and
ten sockets. The ten native clips are Idle, Walk, Run, Attack, Attack2, Dodge,
Defend, Hit, Death and Victory. Idle, Walk and Run loop; all action clips hold or
finish normally.

Mixamo could not accept the detailed generated body reliably. A clean, 20,000-triangle,
Yalis-proportioned proxy supplied the 25-bone No Fingers skeleton and all ten motion
sources. The proxy is never shipped. Blender transfers its skin weights to the
reduced body by nearest-face barycentric interpolation, limits the result to four
influences and verifies that no runtime body vertex is unweighted.

The sword is fitted to a dedicated `YalisSword` bone and follows the final right-hand
pose. The glove fingertips were curled around the hilt instead of leaving an open
T-pose hand. Defend uses a sword-forward guard, Victory raises the blade overhead,
and Death retains it through the initial reaction before dropping it to the floor.
Attack, Attack2, Dodge, Hit, Idle, Walk and Run keep the grip attached.

Three authored hair bones (`YalisHairRoot`, `YalisHairMid`, and `YalisHairTip`) add
small clip-specific follow-through. Their maximum added skin weight is 0.34, and
their keyed amplitudes range from 1.2 degrees in Idle to 7 degrees in Attack2 and
Death. This is deliberately subtle secondary animation, not runtime cloth or hair
physics.

## Native delivery and validation

The normal Character Viewer and Sin Star I projects both cook the same canonical
GLB and descriptor to byte-identical SM3D files. The cooked result has two parts,
69,584 vertices, 50,903 triangles, six textures, 29 bones, ten clips and ten sockets.
Its SHA-256 is `6ead3d8cea6d4bc7e78357479413e51c756c83ebc8c212109f2086f198758b87`.

Validation on September 15, 2026:

- Blender evaluated every frame of all ten clips. Body floors remain within the
  recorded grounding tolerance, equipment is evaluated after the grounded actor
  transform, and Walk/Run root translation remains locked in place.
- GLB round-trip validation found exactly the expected body and sword meshes,
  29-bone rig, ten named actions, four maximum body influences, zero unweighted
  vertices and all six packed PBR maps.
- Export validation found no degenerate triangles or invalid tangents. Thirty
  collapsed body UV faces, sixteen imported corner normals and three microtriangle
  vertices required localized repair; the maximum positional repair was 1.162 mm.
- The repository-wide 462-file SMILE style check and all 13 formatter regression
  groups passed. Native Viewer hardening passed 58 graphics, input and audio-focus
  checks with the Web branch explicitly skipped.
- Release builds of both applications passed. Sin Star I's real-asset fixture opened,
  drew, advanced and released all eight character presentations, including Yalis,
  plus both battle simulations without leaked actors, animators, particles or ribbons.

The generated body is a layered surface rather than a watertight sculpt. Its accepted
runtime copy has 59,216 boundary edges and 74,278 non-manifold edges, but no degenerate
triangles; the sword has 12 boundary/non-manifold edges and no degenerates. Those
facts are preserved in the reports instead of being hidden by destructive remeshing.
Final body, grip, action and equipment-drop renders are under `Previews`.

All Web adoption, publication and Chrome validation remain on hold. Shared source
inventory is aligned so a later explicitly authorized Web milestone can start from
the same asset rather than a duplicate character implementation.

## Reproduction

Use installed Blender 5.2 with `--background --python` and run these package scripts
in order:

1. `Source/prepare_yalis.py` imports the preserved originals, merges the body only
   on a working copy, removes degenerate faces, reduces the body to the cooker-safe
   target, voxel-remeshes the sword and prepares its 2K PBR maps.
2. `Source/prepare_yalis_rig_proxy.py` creates the clean Mixamo-only proxy. Upload
   `Source/yalis-v1-mixamo-rig-proxy.fbx`, select No Fingers and download the T-pose
   plus the ten animation FBX files into `Source/Mixamo` without skin.
3. `Source/assemble_yalis_animation.py` transfers proxy weights and assembles the
   ten 30 FPS Mixamo actions on Yalis's reduced body.
4. `Source/fit_yalis_equipment.py` fits the sword and glove grip, authors the guard,
   victory and drop behavior, and adds the three-bone hair follow-through.
5. `Source/ground_yalis_animation.py` removes unwanted root travel and grounds every
   clip without copying another character's numeric offsets.
6. `Source/export_yalis.py` repairs localized export blockers, validates all frames,
   packs the Blender checkpoint and writes the canonical GLB.
7. `Source/validate_yalis_roundtrip.py` reimports the GLB and verifies meshes, rig,
   clips, weights, textures and representative deformation samples.

The reports in `Source` are generated evidence for each stage. Intermediate `.blend`
scenes and Blender backup files are disposable; the reduced source and accepted rigged
checkpoint are sufficient to reproduce or continue the package.
