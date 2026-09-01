# SMILE Character 3D Viewer

`Character3DViewer.smileproj` is a separate SMILE game project for inspecting converted SM3D characters on the native or Web Renderer3D backend. Arin is the included character; Paladin is his party role.

Controls:

- Primary drag or the bottom orbit buttons: horizontal and vertical orbit.
- `DRAG` or `Tab`: switch primary drag between orbit and pan.
- Secondary drag or the bottom pan buttons: pan.
- Middle drag: orbit.
- Mouse wheel or the bottom zoom buttons: zoom in and out.
- Arrow keys: orbit. `W`, `A`, `S`, and `D`: pan.
- `1`, `2`, and `3`: Idle, Walk, and Run animations.
- `O`: toggle smooth elapsed-time auto-orbit.
- `Space`: cycle inspection lighting. `Enter`: reset the view. `Esc`: exit.
- `PROFILE` or `4`: switch between Arin and the differently sized technical auto-fit fixture.
- `SOCKETS`: show prototype socket origins and RGB local-axis endpoints.
- `CHANNEL`: cycle lit, base-color, normal, roughness, metallic, occlusion, and emissive inspection.
- `-` and `+`: change playback speed from 25% through 200%; authored speed is 100%.

Desktop mouse motion is accumulated so slow drags are not lost between frames, and partial Windows wheel units are retained until they form a complete step. Each repository-owned profile supplies identity, asset, animation, and desired view-height data. The reusable viewer helper derives actor scale, camera, target, floor, pan limits, lighting area, and shadow area from the model bounds. Horizontal and vertical orbit and pan are bounded so an ordinary drag cannot immediately throw the character out of view. Wheel and button zoom update a bounded target FOV; elapsed-time fixed-point smoothing gives the same result at different frame rates.

The viewer intentionally consumes SM3D rather than loading GLB at runtime. To inspect another character, add a bounded profile, clip/socket mappings, and project asset publication; the main viewer source does not need character-specific camera constants. The viewer camera, interaction, lighting, animation selection, diagnostics, recovery, and cleanup remain character-neutral.
