# Mira2 — TRELLIS.2 creation and repair

This independent candidate uses Microsoft TRELLIS.2 BF16 in local ComfyUI and the
supplied T-pose reference. It is not a rename of Mira1/Pixal3D. Original generation,
remeshing and cooking workflows are preserved in Source; generator weights stay
in D:\AI\Mira3D, outside the game and compiler dependencies.

Direct decimation of raw triangle soup destroyed the surface. A 768-resolution
remesh first produced a clean detailed source. For the game body, Blender weighted
decimation retains facial detail, then a new UV unwrap receives baked color,
normal and roughness maps from that source. Base color must use an emission bake:
a diffuse bake darkens metallic gold and incorrectly changes the reference colors.
The cleaned body currently has 18,193 triangles, leaving room for a separate staff.

Mixamo rejected the first mesh-only FBX as an unmappable existing skeleton. A clean
OBJ upload reached the automatic rigging controls. The original baked textures are
kept locally and must be reassigned to the returned rigged body after import.
Rig landmarks: chin, wrists, elbows, knees and groin; 25-bone body skeleton.

## Closed surface repair

The first reduced mesh had many disconnected boundary edges. Welding after
decimation could not recover those boundaries. Merely voxelizing the thin source
also fragmented its cape. The working repair welds before reduction, gives thin
surfaces 6 mm of thickness, then voxel-remeshes at 2.5 mm. Remove tiny disconnected
specks before allocating the triangle budget. The final 18,100-triangle body has
zero boundary/non-manifold edges and receives a fresh color/normal/roughness bake.
The final bake source and generation source remain separate.

Transfer weights from Mira2's own uncurled Mixamo body in bind pose, using nearest
polygon interpolation. Limit/normalize to four influences and check every vertex
has a weight. Curl the existing right-hand geometry in its own hand-bone frame.
The new closed 1,552-triangle staff uses that same skin, weighted entirely to
RightHand, and one palette material so it remains one independently hidden part.

## Rig and animation

Mixamo's No Skin clips and With Skin upload have different rest matrices despite
matching bone names. Bake absolute joint poses into the destination rest frame;
direct action assignment is incorrect. OBJ upload produced meter-valued coordinates
labelled centimeters in FBX. Restore the complete imported rig to meters, then apply
its remaining object scale while adjusting location curves by that remaining scale.
Do not multiply already meter-valued animation translations by 100 again.

Idle transfers neutral global rotations while retaining Mira2's own rest translations
and lengths; the hand keeps her staff vertical. Eight other named actions come from
Mira2's Mixamo pack and mirrored With Skin HealOne. Preserve their names in the
descriptor and resolve corrections by clip name rather than numeric clip ordering.

## Grounding and export

The repaired body's bind minimum was -0.0026767419 m. Apply its measured common
actor translation first. Then correct root translation from the skinned body only,
excluding staff. Idle/Walk keep floor contact; other actions lift only negative
minima, retaining Run's positive airborne phases. The largest measured correction
was 0.09119235 m in Death. Measurements for every sampled frame and the exported
model checksum are stored in Source. This does not by itself establish pleasing
cloth motion; inspect the actual settled pose and foot contacts in the Viewer.

The cooker rejects cross-product squared triangle area at or below 1e-12. Correct
the few narrow triangles by increasing altitude against their longest edge rather
than scaling the entire skinny triangle. Body vertex changes stay below 0.075 mm;
staff tip changes stay below 0.94 mm. Keep the closed topology intact.

MikkTSpace initially produced zero tangents at three sharp body corners and one
neighbor after smoothing changed. Mark those four faces flat and separate the final
UV corner chart by a tiny sub-pixel amount. Export Blender tangents explicitly and
verify none are zero. Do not weaken the SM3D geometry/UV/tangent validation gates.

Mira1 remains untouched in its independent package. Arin/Orin numeric placement and
pose keys are never defaults for Mira. Final appearance acceptance and both Party
battle roles remain outstanding; this package supports the native comparison first.
