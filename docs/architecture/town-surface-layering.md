# Town surface layering

[Town authoring](town-authoring.md) · [Terrain elevation](terrain-elevation.md)

This is the current surface policy, checked against
[`TownSurfaceLayers.json`](../../tools/Character3DViewer/TownSurfaceLayers.json).
The original September 30 evidence remains in the
[town-authoring archive](archive/town-authoring-2026-09-26-to-10-01.md#surface-layering-policy-september-30).

`tools/Character3DViewer/TownSurfaceLayers.json` is the shared numeric authority.
Asset preparation projects it into `TownSurfaceLayers.smile`; Blender reads the
same profile. Tests reject a stale projection. Do not add independent surface
height literals to render, navigation, picking, or export owners.

| Layer | Native elevation | Rule |
| --- | ---: | --- |
| Submerged bed and skirts | 20.70 | Below every visible surface |
| Ground | 20.90 | Below authored building foundations |
| Water | 21.85 | One surface per painted cell; submerged bed receives shadows |
| Road and painted bridge paving | 23.12 | Shared physical and walking elevation |
| Movable structural deck | Road + 2.00 | Its lowest exposed recessed top must clear the road by at least 0.60 |
| Guides and Map Load borders | Projected from road level | Draw once as 2D overlays after the scene; no competing coplanar mesh |

Ten native units equal one metre. The legacy ground walking baseline remains
22.50 to preserve the accepted actor grounding; rendered structural deck changes
must also change their navigation height. Skirt tops stop at their owning surface.
Painted surface kinds replace one another in the grid instead of stacking full
duplicate planes. Existing civic bridge supports remain below their paving.

New imported decks must be measured against this profile, including recesses,
trim and authored scale. Shift the coherent assembly and its walking surface,
or omit hidden substrate; never alter the user's road layout to hide overlap.
Do not solve solid-surface conflicts by draw order or transparent materials.
Retain the camera's scene-relative depth range. Decorative overlays may use a
controlled overlay pass; they must not change collision or walking heights.

This policy prevents the demonstrated terrain/deck conflicts. It cannot make
arbitrary intersecting imported meshes valid automatically; those still need
an explicit support/overlap rule when added. Geometry regressions verify native
and Blender elevations, all drawbridge planks and recessed support, and unchanged
navigation across the bridge and gatehouse.


For prepared recipes, cache admission and incremental replacement ownership, use
the [Viewer architecture](../../tools/Character3DViewer/ARCHITECTURE.md). Catalog
counts and resource measurements from September 26 are historical snapshots;
preflight current runtime availability before admitting additional scenes.
