# Tripo Mira authoring workflow

The raw HD source is a preservation master, not a Character Viewer runtime asset.
The original download already has real 4K maps; increasing their dimensions cannot
fix generated geometry or improve the information in the source artwork.

Inspect PBR, color-only, and clay views separately. The source's nostril rims were
folded, while eye surface irregularities produced bright reflections outside the
expected eye highlights. Color-only rendering showed clean eye artwork. Correct
geometry and skin material response before considering texture repainting.

Local smoothing can drag the existing eyebrow/iris texture. The correction script
uses original front-surface ray intersections to reproject the affected UV corners,
preserving artwork placement while relaxing the nose and eye geometry. The original
HD GLB and all previous Mira candidates remain unchanged.

Weld UV-seam duplicates only on the reduction copy. The source contained 22 edges
with non-manifold topology after coincident-seam welding. Small folded connections
need local collapse; faces touching only at a pinched point need separate vertex
fans. Validate both edge and vertex manifold state, not merely the absence of holes.
The bake script stops if a local repair would alter a larger surface.

Rebake all PBR maps after changing topology/UVs. Pack local ambient occlusion into the
ORM red channel, with roughness in green and metallic in blue. Keep the original
clean color separate from lighting. Facial skin roughness and normal-map strength
are authoring material decisions, baked into the reduced package; they require no
special character-specific renderer behavior.

Mixamo returned a 33-bone rig despite the Standard Skeleton selection. Preserve the
actual bone inventory. Its animation imports use centimeters: convert joint
translations to meters and normalize each joint basis to unit scale. Scaling both
translation and basis collapses the skinned surface. Assembly checks the resulting
global joint matrices against each source sample.

The cape needs its own hinge because leg-weight transfer drags the long rear cloth
through the floor during kneeling casts and Death. Body grounding excludes cape
vertices and equipment, preserves intentional Run airtime, and keeps Walk/Run in
place. A whole-clip bounded angle solve anticipates cape contact; a greedy per-frame
solve caused a visible 44-degree snap during Death. The accepted solver limits
adjacent samples to five degrees. A single 2.6 mm seam clearance adjustment is
recorded separately. Never copy another character revision's grounding offset.

Run uses a diagonal back carry. During Death the staff releases and settles beside
the body; simply lifting a back-mounted staff to clear the floor made it float.
Floor validation includes the staff at every sample and compares body bind minimum
with Idle frame 0. Equipment and effect sockets consume the same final actor transform.

Also validate the complete equipped bind bounds. A grip-centered staff mesh at the
world origin extended 0.676 meters below the feet even though every animated sample
was grounded. Viewer auto-fit then lifted the entire character. Move the staff's
bind bone and bind vertices together to a positive-height rear placement; keep all
animated global staff poses unchanged. The export now requires the bind staff to
remain above the body's bind minimum, and the real-asset native fixture checks the
standalone and both party framing offsets.

The staff reducer left 52 disconnected, zero-volume double-sided triangle shells.
These pass edge-manifold checks but fail Blender mesh validation. Remove both faces
of each isolated duplicate shell before unwrapping and baking. Validate mesh records
as well as topology. Repair imported corner normals that produce zero tangents;
do not publish a normal-mapped asset with invalid tangent vectors.

The first authored staff-hold pass introduced an arm defect after Mixamo: the IK
pole angle selected the opposite elbow bend (Idle elbow was above the shoulder),
and an independently forced world-space hand orientation sharply bent the wrist.
This was our equipment-fitting error, not a Tripo generation setting. For this rig,
the corrected pole angle is 180 degrees. Align the forearm to elbow-to-wrist, carry
pronation in the forearm, preserve the hand's neutral relative rest rotation, and
orient the staff from the hand's thumb axis. Do not copy this pole angle to another
rig without inspecting its bone axes. Keep all held-clip staff floor corrections
at zero so a floor clamp cannot detach the grip.

For future characters: center and inspect the neutral mesh, rig without equipment,
place Mixamo elbow/wrist/knee/groin markers carefully, and keep all clips tied to
one accepted skeleton revision. Preserve source exports before changing rest pose,
axes, scale or weights. Fit a complete shoulder/elbow/wrist/finger chain and the
weapon together, then inspect front/side/back views at neutral, extreme and contact
poses. The current 33-bone Mixamo skeleton has one index chain per hand; it is not
a full independent-finger rig. Higher triangle count and texture resolution do not
repair a joint orientation or weight problem.

`Source/check-mira-roundtrip.py` checks neutral relative wrists and attached grip
positions throughout all seven held clips. It then re-imports the actual GLB and
compares body/staff bounds and right arm/staff joints at first/middle/last frames
of all nine clips, within 0.05 mm. Compare relative clip time, because Blender can
preserve a nonzero exported start-frame offset. Native visual acceptance remains
separate from successful authoring/export checks.

The staff head occupies bone-local Y 0.15–0.375 m, with radial extents about
0.089 m. StaffTip is at Y 0.28 m. The native head-only shimmer derives an enclosing
radius from StaffGrip–StaffTip distance, keeping it around the head rather than
spreading along the shaft. Build its frame from the animated sockets so it follows
back carry and the settled staff during Death. Body glow and all attachments use
the same grounded actor transform.
