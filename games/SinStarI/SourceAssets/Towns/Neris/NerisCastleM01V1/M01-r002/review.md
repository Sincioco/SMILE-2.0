# Neris Castle M01 r002 — provisional, awaiting review

M00 and the requested M01 blockout are complete. No M02–M06 approval has been inferred. All numeric dimensions remain proposals.

One Blender scene supplies the seven fixed views. The gate leads through the southern courtyard to the northern palace. Cream stone, teal roof masses, a curved main dome, subordinate turrets and crystal placeholders preserve the design anchors. The original concept views disagree about hidden geometry; this version uses one consistent rear gallery.

## Evidence

- `source/neris-castle-M01-r002.blend` (relative to the workspace root): source model, fixed cameras, ownership tags, packed references and lowered independent bridge.
- `contact-sheet.png`: seven actual Blender renders, inspected after rendering.
- `measured-checks.json`: 62 passing measurements and evidence checks.
- `bridge-0.png`, `bridge-45.png`, `bridge-90.png`: actual pivot poses, inspected.
- `bridge-sweep.json`: 91 actual mesh intersection samples, no surface intersections. This is geometric sampling, not a rigid-body simulation.
- `exports/M01-r002/export-validation.json`: separate static castle and pivot-local bridge reimported; 7,538 + 12 triangles; bounds preserved; review helpers excluded.

## Narrow bridge revision

Sin separately authorized an automatic drawbridge in the native Viewer. Its sweep exposed the need for a 0.4 m arch setback and a matching heel-seat extension. Five gatehouse/site objects changed; all other 242 objects retained their geometry/transforms. Closed arch clearance is 0.15 m. This clearance work supports the requested preview and does not accept the later gatehouse/detail milestone.

The r001 source and checkpoint remain intact. The r002 model contains no completed façade windows, heraldry, chain hardware, texture bakes or interiors. The pointed opening and roof shapes are intentionally coarse. The lowered 10 m leaf is narrower than the 12 m passage and is not a sealed portcullis.

## Review decision still outstanding

Approve or revise the 108 × 132 m tower-inclusive footprint, 60 m peak, 12 m gateway, 10 × 20 m bridge, courtyard-to-palace arrangement and saved camera framing. Further castle milestones must resume in numbered order after their relevant approval. Rebuild only changed owners and their real dependencies.

The provisional native comparison was explicitly requested before approval. It is a preview delivery, not M06 acceptance.
