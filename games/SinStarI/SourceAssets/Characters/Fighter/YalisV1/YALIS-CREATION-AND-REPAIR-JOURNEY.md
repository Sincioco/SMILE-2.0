# Yalis v1 creation and repair journey

Read this before future Yalis model, rig, animation, sword, grip, grounding, hair or
VFX work. Every number below belongs to Yalis v1 and must not be copied to another
character revision.

## Source inspection

The supplied body is a one-meter, unrigged, layered generated mesh with 17 objects,
642,897 imported vertices, 1,086,254 triangles and three real 4K PBR maps. The sword
is also unrigged and contains 1,105,579 imported vertices, 1,648,018 triangles and
three 4K PBR maps. The body and sword originals remain byte-identical under `Source`.
Front, back, left, right and sword references remain together under `References`.

The generated body's disconnected clothing, hair and armor layers are intentional
appearance structure. Welding or voxel-remeshing the complete body destroys thin
silhouettes and UV artwork. Reduce a copy while retaining its original UV layout;
validate degenerates, normals, tangents and exported accessor counts separately.

## Runtime reduction and cooker boundary

The first 79,910-triangle body derivative looked acceptable in Blender but exported
92,882 body vertices. The current SM3D cooker uses 16-bit primitive indices and
rejected that accessor above 65,535 vertices. This is a real current runtime boundary,
not a Blender failure. Reducing the body target to 45,000 triangles yields 44,907
triangles and 62,565 exported vertices, 2,970 below the limit. Keep that exported
accessor check in future revisions; Blender's edit-mode vertex count is not sufficient.

The sword tolerates a different repair because it is a rigid prop. A 1.5 mm voxel
remesh, 6,000-triangle target and 2K rebake produce a stable 5,996-triangle sword.
The body keeps 4K PBR; the sword uses 2K PBR. Together their decoded texture budget is
240 MiB, below the current 256 MiB model-cooker budget.

## Mixamo and skin transfer

Mixamo rejected the detailed, disconnected Yalis body. A clean 20,000-triangle
mannequin proxy was therefore built to Yalis's measured chin, shoulder, wrist, groin
and knee positions. It exists only to obtain a consistent Mixamo skeleton; it is not
a visible replacement mesh. The accepted auto-rig uses the No Fingers preset and
contains 25 Mixamo bones.

All ten animations were downloaded without skin against that same skeleton. Import
translations are converted consistently, then each action is baked at 30 FPS. The
largest source-to-assembled joint error is about 0.0011 mm. Transfer weights from the
proxy's nearest face using barycentric interpolation, prune to four influences and
renormalize. Direct nearest-vertex copying created visible seam/ladder deformation and
must not be restored. The final body has no unweighted vertices.

## Sword, hands and action posing

The accepted sword scale is 0.72. Its measured source grip height is 0.875 m and its
Yalis-specific palm offset is `(-0.023, -0.037, -0.015)` m. The right glove's generated
fingertip surface was curled 105 degrees around the hilt; the final repair affects 47
vertices with a maximum displacement of about 49.95 mm. These values depend on this
hand and sword geometry.

The shared Mixamo sword-and-shield motions provide useful lower-body and combat timing,
but Yalis owns no shield. Defend's right arm is authored as a sword-forward guard.
Victory uses a safe overhead sword raise. Death keeps the grip through frame 30 and
drops the sword by frame 48 so it settles beside the body. The remaining clips keep
`YalisSword` attached to the right hand. Inspect the glove, hilt and blade together at
extreme frames; a hand-only check can miss a detached or inverted weapon.

## Hair follow-through

Yalis's long generated hair needs some response to fast motion, but the current runtime
does not provide cloth or hair simulation. Three authored bones divide selected rear
hair vertices into root, middle and tip regions. Their weights are capped at 0.34 so
the Mixamo head/spine skin remains primary. Clip amplitudes are intentionally bounded:
Idle 1.2 degrees, Walk 2.8, Run 4.5, Attack 6, Attack2 7, Dodge 6, Defend 1.8, Hit 5,
Death 7 and Victory 3.5. This keyed follow-through is deterministic on native playback.

Do not increase those angles merely to make the effect obvious in a still image. Check
the whole clip for neck detachment, armor intersection and floor contact first. Any
future physics solution should replace these keys deliberately rather than layering a
second uncontrolled owner over them.

## Grounding and export

Compare the skinned body bind minimum with animated Idle frame 0 before adding a
per-clip correction. Yalis's accepted bind-to-Idle delta is about 1 mm. Root X/Y travel
is locked so Viewer and game placement remain authoritative. Every frame of every clip
was checked against the arena floor; intentional airborne motion is preserved while
the settled Death body and dropped sword remain above it. Equipment and future VFX
must use the final grounded actor transform.

The equipped bind sword rises to 1.331 m while the body rises to about 0.999 m. Viewer
profile height is therefore 133 world units, which makes the full bind bounds fit at
the established 10,000 percent imported scale while keeping the body near the normal
100-unit character height. Do not shrink the actor to a 100-unit full bound or Yalis
will appear about 25 percent shorter than intended.

Export validation found 30 collapsed UV faces and 16 imported corner normals that
produced invalid tangents, plus three microtriangle vertices needing a localized
positional repair. The accepted repair changes at most 1.162 mm and leaves zero
degenerate triangles and zero invalid tangents. The layered body remains open and
non-manifold by design; do not claim it is watertight.

`Source/validate_yalis_roundtrip.py` is the final portable check. It ignores Blender's
generated custom-bone display helper and requires exactly the armature-driven body and
sword meshes, 29 bones, all ten actions, four maximum influences, zero unweighted body
vertices and six packed maps. The Icosphere visible after reimport is a Blender display
object, not a third GLB mesh node or runtime asset.
