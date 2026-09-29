# Neris Spaceport 01 — southwest harbor

This package contains the completed M00–M09 full-scale spaceport and a separate
expanded Neris Town revision. Sin waived the original approval stops and requested
the southwest placement, water, western land and road connections on September 28.

`Source/NSP01-final-r01.blend` is the editable standalone asset; `NSP01-preview.glb`
is its complete portable export. `Town/Neris-Town-Spaceport-SW-r001.blend` preserves
the existing 361 town placements and adds the spaceport. Its matching `.town`
document owns the expanded terrain, roads and latest saved lighting. Original
castle assets and Neris Town r005 remain unchanged. Nothing is committed or pushed.

## Placement and connections

- Blender meters: origin (-550, -760, 0.212), Z rotation 180 degrees, scale 1.
- Native: origin (-5500, 23.12, -7600), Y rotation 180 degrees, scale 10 world units/m.
- Standalone dimensions: 720 × 620 × 356 m, including the 56 m underside.
- Southwest is water. The west extension is land north of Y=-355 m.
- A 20 m western road joins Royal Court's approach and the town southwest street.
  A 40 m arrival causeway meets the spaceport exactly at Y=-370 m.
- The 478 × 508 terrain grid retains all old cell edges; new outer cells are 10 m.
- The walking deck meets road elevation. Underside pylons remain beneath harbor
  water; they were not deleted or scaled to hide them.

## Native ownership

`NerisSpaceportPreview.smile` incrementally owns three models and eleven draw
objects. `NerisSpaceportRoute.smile` owns placement and a conservative central
arrival-to-hall walking path. The existing town coordinator owns its lifecycle;
existing shared camera modules provide orbit, pan and zoom. `Orbit Spaceport`
frames the harbor. Earlier town revisions without the expanded footprint keep
their original view and do not load this asset.

`Automation/pack-native.mjs` retains all 239,864 portable triangles, positions,
normals and eight materials. It bakes translations, consolidates material parts,
and supplies planar UVs plus orthonormal tangents for untextured PBR. There is no
geometry simplification. The original 1,991 Blender production objects evaluate
to 239,912 triangles; its portable export removes 48 zero-area artifacts.
`prepare-native.mjs` checks hashes and the existing per-part/per-model ceilings.
No SMILE runtime/compiler, addon or native pool limit is changed.

## Limits and evidence

Doors and cabins are static in this game preview. Their separate editable source,
anchors and route graph remain available; elevator gameplay is not implemented.
The expanded town uses three extra native model slots. The optional 14-model
Tripo Old Castle cannot be added alongside every current town/party/spaceport
resource within the existing 64-model pool. Earlier town revisions retain that
headroom. Do not raise the runtime limit or remove another castle to conceal this
constraint; a future asset-consolidation task can address that optional composition.

`Checkpoints/M09` contains actual fixed-camera Blender renders and measurements.
`Checkpoints/Spaceport-*` shows the actual assembled town, using temporary daylight
for legibility. Road connectivity, original-object preservation and source parity
are recorded alongside them. Native results are recorded in the integration handoff.

The original immutable launch package and complete checkpoint history remain at
`D:/Projects/Sin-Star-I-Assets/Neris-Spaceport-01`. Its original authoring manifest
uses that workspace's relative paths; use this README for this repository package.
