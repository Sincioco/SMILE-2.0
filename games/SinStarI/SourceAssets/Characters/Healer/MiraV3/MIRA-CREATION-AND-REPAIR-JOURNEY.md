# Mira3 — Hunyuan shape, paint and native comparison

## Generation and texture ownership

Mira3 uses the same supplied Mira reference as the other candidates, with Tencent
Hunyuan3D-2.1 for both shape and paint. ComfyUI produced the raw shape in BF16.
The official Hunyuan Paint pipeline runs locally in FP16, with a local DINOv2 giant
reference encoder, UniPC trailing scheduler, 15 steps, guidance 3 and seed 0.
Dependencies are isolated under `D:\AI\Mira3D\hunyuan-paint-libs`; SMILE has no new
runtime/build dependency. Blender substitutes for the paint package's CUDA rasterizer
when rendering normal/position conditions and baking the generated views.

The GLB importer selected Quaternion rotation mode. Setting Euler values alone did
not turn the shape: explicitly select XYZ rotation mode before the 180-degree turn.
Verify view 0 is the face. Normalize around the bounding-box center with a farthest
vertex diameter of 1.15, use orthographic width 1.2, six recorded camera poses,
world normals scaled by 0.5 plus 0.5, and positions encoded as 0.5 minus position/1.15.
The recorded view matrices and source images are the reproduction inputs.

Whole-character paint only allocated a few dozen pixels to the face. A separate
head/neck conditioning mesh gives the face most of a 512-pixel view. The final head
pass uses the original reference's head crop; no Mira1/Mira2 body or face is copied.
Generated views project by camera direction and position visibility. Bake color and
material channels directly onto the cleaned low mesh: the raw self-intersecting high
mesh caused triangular projection artifacts. Use a cleaned voxel surface for the
normal bake. Hunyuan's R channel is metallic and G is roughness; repack metal into
glTF's B channel. Keep skin/hair nonmetallic and avoid near-zero skin roughness.

## Closed low mesh

The raw shape contained 716,194 triangles, 196 boundary edges and 23,399 non-manifold
edges. Weld before reduction, voxelize at 0.0014 in normalized paint coordinates,
preserve face/hand detail with weighted reduction, and unwrap the 18,100-triangle body.
One four-face edge joined two closed hair surface fans; separate those fans at its
end vertices to recover closed topology without deleting the hair. Both final parts
must retain zero boundary/non-manifold edges.

The exporter repairs one narrow body triangle by moving its apex approximately
0.013 mm against the longest edge. The staff's tiny blue decorative tip caps needed
a minimum 1.1 mm radius. Do not weaken the cooker's area/tangent checks or repeatedly
move a shared cap center: that merely makes adjacent cap faces undersized in turn.
The final export has no zero tangents and remains at 19,652 equipped triangles.

## Own rig, casts and staff

Upload `Source/mira3-mixamo-input.obj` to Mixamo and place the chin, wrist, elbow,
knee and groin landmarks. Use the 25-bone No Fingers skeleton for this fused-hand
geometry. The With Skin HealOne export and No Character Pro Magic pack were downloaded
for Mira3's own character. Mixamo labels these meter-valued OBJ coordinates as
centimeters in FBX. Restore the complete rig to meters once, bake absolute source
joint poses through the destination rest matrices, and adjust location curves only
by the remaining applied object scale. Direct Action assignment is incorrect.

Transfer weights to the textured body in bind pose. Maximum nearest-vertex deviation
was below one micrometer; limit and normalize to four weights, with no unweighted
vertices. The neutral Idle rotations are preserved within this package and applied
to Mira3's own rest translations and bone lengths. The original-pose Idle comes from
the earlier authored Mira loop, not copied body geometry or another character's rig.

Curl Mira3's existing fingers in her RightHand bone frame: 217 vertices, local grip
(0, 0.073, 0.035) and 0.035 m bend radius. The closed staff uses one palette material
and rigid RightHand weights in that same skin. Attack and HealOne are mirrored through
bind-relative transforms so the free left hand casts. Bake a two-bone right-arm hold
for movement and casting, then remove temporary IK constraints/targets. The palm
keeps the staff upright above the floor. Death blends the wrist to a horizontal staff
as the pose settles. All corrections are portable animation keys, not runtime IK.

## Grounding and validation

Compare the body bind minimum and animated Idle frame 0, excluding equipment/VFX.
Apply Mira3's measured common placement first (effectively zero), then correct each
clip by name. Idle/Walk retain floor contact; other clips only lift negative body
minima, preserving Run's positive airborne frames. Record every sampled frame and
the exact model checksum. Inspect first/middle/final staff clearance and settled
Death separately; equipment must not drive the body's placement baseline.

Profiles owns Mira3 identity and nine clip names; ViewerSession/ViewerUi route her
tab. ViewerParty uses Mira3 as the current healer in both battles. MiraBattle and
MiraWater retain cast timing, bounded per-actor particles, clip-time sound and target
cleanup. Mira does not enter an Arin/Orin calibration bank. Socket queries use body
part 0. Original Mira water/chime cues are retained in Audio. No game code changes.

The final appearance is still a comparison, with limitations listed in README.
Do not describe this checkpoint as Sin's final visual acceptance.

On September 13, 2026, the final native cooker/build, focused Viewer hardening
fixture (58 graphics/pointer/audio checks), and real-asset calibration isolation
fixture passed. The fixture loads all three Mira candidates, exercises all three
casts in both battles, checks recipient cleanup and Beat Preview restoration, and
round-trips Arin/Orin calibration without touching live user storage. All eight
changed SMILE sources pass the formatter check. The running Viewer was replaced
through Launch.ps1, preserving the existing window placement and live calibration.
Native Idle, staff visibility, healing pose and settled Death were inspected;
screenshots retain the comparison and group battle evidence. No .NET, VSIX or Web
build is required for this native asset/Viewer update.
