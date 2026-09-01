# SMILE Character 3D Viewer

`Character3DViewer.smileproj` is a separate SMILE game project for inspecting one converted SM3D character on the native or Web Renderer3D backend. Arin / Paladin is the included sample asset.

Controls:

- Primary drag or the bottom orbit buttons: horizontal and vertical orbit.
- `DRAG` or `Tab`: switch primary drag between orbit and pan.
- Secondary drag or the bottom pan buttons: pan.
- Middle drag: orbit.
- Mouse wheel or the bottom zoom buttons: zoom in and out.
- Arrow keys: orbit. `W`, `A`, `S`, and `D`: pan.
- `1`, `2`, and `3`: Idle, Walk, and Run animations.
- `Space`: cycle inspection lighting. `Enter`: reset the view. `Esc`: exit.

The viewer intentionally consumes SM3D rather than loading GLB at runtime. To inspect another character, convert and publish its model and textures, then change `CHARACTER_ASSET`, the clip constants, and the project `Asset` items. The viewer camera, interaction, lighting, animation selection, diagnostics, and cleanup remain character-neutral.
