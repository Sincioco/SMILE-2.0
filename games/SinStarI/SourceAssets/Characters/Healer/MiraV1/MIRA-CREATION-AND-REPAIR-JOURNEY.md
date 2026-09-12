# Mira1 creation and repair workflow

Mira1 is the preserved Pixal3D comparison, not the accepted final healer.
Read README.md for the current package, validation and unresolved contacts.

Generate dense geometry before reducing it. Direct QEM decimation of the generated
triangle soup collapsed the silhouette. Remesh the surface first, then decimate and
bake its textures. Preserve both the raw result and the usable derivative.

Mira1's original generation used the supplied turnaround and Pixal3D multi-view.
Its repaired cape, staff and grip fingers are authored Blender geometry. The next
candidate must be judged before repeating the substantial repair effort.

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
error; several action poses deform or penetrate the floor. Keep those limitations
visible in this comparison and repair them on the chosen final candidate.
