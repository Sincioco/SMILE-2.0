# Neris Castle M06 V1 — detailed exterior

Current source: **Source/neris-castle-M06-r002.blend**. Portable candidate:
**neris-castle.glb** (M06 r002), with separate **castle.glb** and **drawbridge.glb**.
This is the authored west-side comparison castle for Sin Star I. The two existing
town castles and the user's live town document are preserved.

Sin explicitly requested autonomous continuation and native integration after the
original M01-only request. M02–M06 were completed in numbered order. Proposed
dimensions remain unapproved; autonomous execution is not explicit dimension or
artistic acceptance. There are no explorable palace interiors.

## Appearance and geometry

Ivory stone, teal roofs, gold trim, a dominant ribbed dome and crystal crown,
recessed pointed windows, layered entry surrounds, lanterns, bordered Neris
banners, wall corbels, courtyard arcades, balustrades, fountain, cypresses and
flowers are real scene geometry/materials. The supplied original front reference,
flag board, measured plan and elevations guided the work. All progress evidence
is actual Blender or native Viewer output, never generated reference imagery.

The big gateway leads into the courtyard before the palace. The 10×20 m leaf
hinges at Blender (0,-64,-0.25), rotates around local X, and lands on the normal
town road. No external landing block was added. The source has 251,729 evaluated
triangles and 11 shared asset materials. Export omits 479 zero-area pole triangles
only in disposable copies, leaving 251,250 triangles. One 1024×1024 masonry PNG
is packed into the source/portable GLB; source texture is also provided separately.

## Native ownership and placement

NerisCastlePreview loads Native/castle-00 through castle-07 plus drawbridge:
39 static parts and three moving material parts. prepare-native.mjs checks hashes,
part order, UV presence and the existing cooker limits before mirroring inputs.
The normal SMILE project cooker publishes the source-relative masonry image.
No compiler limits or asset protection checks were raised or bypassed.

The portable Site.Moat part is excluded only from native placement because the
existing town terrain owns the water surface. The source foundation, gateway and
leaf retain their measured relationship. Native scale is 1700 percent, origin
X=-3650, Z=-852, deck Y=23.12, hinge (-3650,18.87,-1940). The existing rectangular
land, normal-height road, moat and old castles are unchanged in this revision.

NerisCastleRoute retains approach/party occupancy and its smooth automatic
lowering/closing. Six planter footprints, the fountain and the closed palace
door are solid; twenty risers reach the door threshold. All three bridge materials
share one transform. No permanently placed extra engine scene is introduced.

## Evidence and recovery

Checkpoints/M02-r001 through M05-r001 contain source evidence, measured checks,
91-pose bridge clearance and 422 route cross sections at up to 0.25 m spacing.
Checkpoints/M06-r002 contains seven fixed-camera reimport views, bridge 0/45/90,
bounds/axis/pivot records and native acceptance. Earlier M06-r001 evidence is
retained as the initial candidate; r002 supersedes it for delivery.

Validation levels are recorded individually in the M06 checkpoint. No installed
Khronos validator was found, so level 2 was not run and no tool was installed.
Offline structural checks are not a substitute for Khronos validation. Native
evidence applies to the tested Windows Viewer, not Web adoption or arbitrary
glTF consumers. Static heraldry is intentionally flat; no cloth simulation,
emission/bloom, physics chains or interior gameplay is claimed.

Automation/README.md identifies operation boundaries and replay limitations.
Keep versioned .blend sources authoritative; use a fresh revision and modify
only the affected owner and real dependencies. Whole-stage idempotent replay is
not claimed. Source collections retain stable ownership IDs and editable geometry.

The previous provisional package remains in ../NerisCastleM01V1. The working
authoring directory and safely extracted source kit remain at
D:/Projects/Sin-Star-I-Assets/Neris-Castle. Do not replace live town edits with its
historical terrain checkpoint. Studio and Web adoption remain on hold.

Final native evidence, validation levels and changed-owner growth are recorded in
Checkpoints/M06-r002/checkpoint.md. The actual Viewer is rebuilt and launched; no
user rebuild or browser refresh is needed. Ornamental density remains simpler than
the painted concept; exact fidelity or human artistic acceptance is not claimed.
