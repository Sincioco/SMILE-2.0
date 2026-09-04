# Red Dragon V1.0 Source Asset

`RedDragonV1.0.original.glb` is the preserved Tripo3D source for the Sin Star I boss arena preview.

- Original source: `C:\Users\louie\Downloads\RedDragonV1.0.glb`
- SHA-256: `4A90AC7BCD5E0BEA9D0747CBB3E4B3B9379E1DCE2303DBA7797F6D0E72996D88`
- Source size: 1,994,132 bytes
- Geometry: 15,174 vertices and 9,916 triangles across 64 mesh objects
- Bounds: `(-0.499988, -0.414046, 0)` through `(0.499819, 0.414014, 0.355155)`
- Texture: one packed 2048 by 2048 base-color image
- Animation: static prop with no armature or animation actions

The Character Viewer cooked project converts the packed source texture to a runtime SM3D asset. Keep this original GLB as the reproducible source for future Dragon placement, materials, animation, and battle-scene work.

Run `scripts\prepare-red-dragon-static.ps1` to reproduce `RedDragonV1.0.static.glb` and its JSON report. The preparation step removes only sub-threshold degenerate faces rejected by the SM3D validator; it does not decimate or redesign the Dragon.

The prepared GLB is deterministic with SHA-256 `4909CD165FBA48EE64E73617E4B661DB7D4FA481BC0C9707243077D2C26B96AC`.
