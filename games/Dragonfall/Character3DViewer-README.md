# SMILE 2.0 - Character 3D Viewer

`Character3DViewer.smileproj` is a separate SMILE game project for inspecting converted SM3D characters on the native or Web Renderer3D backend. Arin is the default character; Paladin is his party role. The original three-clip Arin model and the articulated technical fixture remain selectable with `PROFILE` or `4`.

The viewer starts in a hands-free presentation at the authored `-16 deg` zoom: it auto-orbits and selects the next available animation every five seconds. Every available animation has a button in the right-side panel. Right-click performs a complete presentation reset: it recenters and refits the camera at `-16 deg`, returns to Idle, restarts the five-second animation sequence, and resumes auto-orbit. Manual camera input stops auto-orbit; ten seconds without further camera input performs the same one-shot reset.

Controls:

- Hold the left mouse button and drag to pan.
- Hold the middle mouse button and drag left/right or up/down to orbit at direct pointer sensitivity. Vertical orbit supports a complete 360-degree revolution.
- Use the mouse wheel to set a bounded, responsive zoom target; each wheel step is visibly meaningful and the live camera eases toward it without stopping auto-orbit.
- Right-click to reset the view and presentation.
- Arrow keys orbit. `W`, `A`, `S`, and `D` pan.
- `1`, `2`, and `3` select the first three animations.
- `O` toggles auto-orbit. Turning it off starts the ten-second idle-reset timer.
- `F` hides or shows the floor and grid together. `G` hides or shows only the grid.
- `Space` cycles inspection lighting. `Enter` resets the presentation. `Esc` exits.
- `PROFILE` or `4` switches profiles.
- `SOCKET` shows socket origins and RGB local-axis endpoints.
- `CHANNEL` cycles lit, base-color, normal, roughness, metallic, occlusion, and emissive inspection.
- `-` and `+` change playback speed from 25% through 200%; authored speed is 100%.

Desktop mouse motion is accumulated so slow drags are not lost between frames, and partial Windows wheel units are retained until they form a complete step. Each repository-owned profile supplies identity, asset, animation, desired view height, and an optional bounded ground offset. The reusable viewer helper derives actor scale, camera, target, floor, pan limits, lighting area, and shadow area from model bounds. A ground offset compensates only for a measured difference between a static bind AABB and the animated sole plane; it does not rewrite animation root motion.

The default scene is 1600 by 640. On native desktop, the opt-in responsive-window policy makes the logical canvas follow the live client size, so the scene fills wide, tall, maximized, and manually resized windows without letterbox bars. The header remains left aligned, controls and status remain right aligned, the top background panel is hidden, and the middle remains available to the orbiting scene. The remaining panels and button fills use 80% opacity while their labels stay fully opaque. The desktop executable preserves its native x, y, width, and height across an ordinary relaunch. The status panel reports the eased live zoom angle, current FPS, draw calls, and submitted triangles.

The studio floor is a character-neutral enlarged plane with an emissive blue line grid. It is created from ordinary `Smile.Simple3D` planes and one shared material, so another viewer profile receives the same inspection environment without model-specific renderer code.

The viewer intentionally consumes SM3D rather than loading GLB at runtime. To inspect another character, add a bounded profile, clip/socket mappings, and project asset publication; the main viewer source does not need character-specific camera constants. The viewer camera, interaction, lighting, animation selection, diagnostics, recovery, cleanup, and idle-reset behavior remain character-neutral.
