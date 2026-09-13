# Mira1 creation and repair workflow

Mira1 is Sin's selected Pixal3D base after the three-candidate comparison.
Read README.md for the current repair, validation and pending native visual review.

Generate dense geometry before reducing it. Direct QEM decimation of the generated
triangle soup collapsed the silhouette. Remesh the surface first, then decimate and
bake its textures. Preserve both the raw result and the usable derivative.

Mira1's original generation used the supplied turnaround and Pixal3D multi-view.
Its repaired cape, staff and grip fingers are authored Blender geometry. Preserve
the exact selected comparison source before changing its texture, skin or animations.

Mixamo action-only and With Skin exports can have different rest matrices despite
matching bone names. Transfer absolute animated joint matrices into the target bind
rig; compare against a With Skin export before accepting the transfer. The archived
retarget script operates on the documented D:\AI\Mira3D workspace and an existing
Mira scene; it is not a standalone package builder.

Normalize Mixamo's 0.01 armature scale before SM3D cooking. Scale location curves
and their handles across all action channel bags, never rotation/scale channels.
Preserve mesh/equipment world transforms and verify animated vertices before/after.
The centimeter source is retained separately for recovery.

Every animated SM3D part must use the same skin. The removable staff therefore uses
100% RightHand weights and its own mesh part, instead of only a bone parent.
Palette UVs still need nonzero area for tangent generation. Tiny triangle islands
around the original palette sample preserve appearance while satisfying that rule.

Grounding must compare bind and Idle minima without equipment, then every clip's
first frame plus Defend/Hit contact and settled Death. Mira1 has no common placement
error in the original comparison; several action poses deformed or penetrated the
floor. The selected repair measures its own common correction again after costume
relaxation, then corrects body contact without using the cape or staff as foot minima.

## Selected texture and cape repair

The old atlas was already 4096 pixels square. Increasing resolution would preserve
its baked projection streaks. Rebuild UVs and bake directly from the calibrated
reference views; do not fall back to the damaged atlas at silhouette mismatches.
Pad those mismatches in projection space. A side-view reference hand occludes skirt
cloth; exclude that projection on hidden cloth and the raised staff arm, or a second
hand appears in the costume. Preserve Mira1's original face, hands and cape artwork.
The four-view reference contains roughly 800 pixels of character height, so the
resulting 4K atlas still has limited fine detail. Do not claim an upscale adds detail.

Before welding UV seams, average coincident vertices' skin weights and retain four
normalized influences. The old pair at the rear waist separated by 3.55 mm in Attack.
Welding without reconciling weights leaves an animation defect hidden at bind pose.

The cape is a closed waist panel, not a shoulder cape. Extend its upper rows beneath
the belt, use nearby waist skin at the seam, and add a single Hips-child hinge for the
remaining panel. Bake clearance per clip, independently of foot contact. Source
reports retain maximum hinge changes; inspect fast Hit/Death transitions visually.

The old 1,475-triangle staff had 947 boundary edges despite looking continuous. Its
decimated cylinders and caps no longer met. Reconstruct the original authored oval
staff with closed primitives before reducing their segment counts. Do not copy a
different candidate's staff or bind-hand rotation. The replacement is 1,412 triangles.

Mirror the two one-handed casts onto LeftHand and derive the staff carry orientation
from Mira1's own Idle pose. A temporary two-bone IK hold is baked into portable keys;
no runtime IK or new Viewer animation format is required. Verify every clip frame,
including staff clearance in Death, then the actual native render. Blender checks
and a passing native build do not replace that final visual review.
