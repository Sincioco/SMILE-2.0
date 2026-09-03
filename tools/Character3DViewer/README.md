# SMILE 2.0 - Character 3D Viewer

The SMILE Character 3D Viewer is a reusable tool for inspecting converted SM3D characters on the native or Web Renderer3D backend. Arin is the default character; Paladin is his party role. The original three-clip Arin model and the articulated technical fixture remain selectable with `PROFILE` or `4`. Compatibility project wrappers remain under `games\Dragonfall` because project asset publication currently confines source assets to the owning project tree; the viewer source and profiles live here under `tools`.

The viewer starts in a hands-free presentation at the authored `-16 deg` zoom: it auto-orbits and selects the next available animation every five seconds. Every available animation has a button in the right-side panel. Arin v5.5 also starts with an optional presentation-only equipment glow: a narrow icy additive strip follows only the sword blade, while a warm, mesh-fitted outline follows the animated shield silhouette. The sword glow is a single two-triangle strip, so it cannot create closed-ribbon connections across Arin's body. Unconnected fading glow particles follow the sword tip during the authored Sword Attack trail window and the shield center around the Shield Bash impact; motion-trail triangles never span between sampled poses. Trail history is reset at each effect window so animation loops cannot connect distant poses. The effect has no surrounding halo or idle trail. `GLOW` toggles all four elements without changing Arin's source textures, materials, or candidate asset. Right-click performs a complete presentation reset: it recenters and refits the camera at `-16 deg`, returns to Idle, restarts the five-second animation sequence, and resumes auto-orbit. Manual camera input stops auto-orbit; ten seconds without further camera input performs the same one-shot reset.

Controls:

- Hold the left mouse button and drag to pan.
- Hold the middle mouse button and drag left/right or up/down to orbit at direct pointer sensitivity. Vertical orbit supports a complete 360-degree revolution.
- Use the mouse wheel to set a bounded, responsive zoom target; each wheel step is visibly meaningful and the live camera eases toward it without stopping auto-orbit.
- Right-click to reset the view and presentation.
- Arrow keys orbit. `W`, `A`, `S`, and `D` pan.
- `1`, `2`, and `3` select the first three animations.
- `O` toggles auto-orbit. Turning it off starts the ten-second idle-reset timer.
- `F` hides or shows the floor and grid together. `G` hides or shows only the grid.
- `Space` pauses or resumes animation playback. `Enter` resets the presentation. `Esc` exits.
- `PROFILE` or `4` switches profiles.
- `GLOW` hides or shows the current profile's equipment aura. Arin v5.5 enables it by default; profiles without the required weapon sockets leave the toggle unavailable.
- `SOCKET` shows socket origins and RGB local-axis endpoints.
- `CHANNEL` cycles lit, base-color, normal, roughness, metallic, occlusion, and emissive inspection.
- `-` and `+` change playback speed from 25% through 200%; authored speed is 100%.

The bottom timeline is model-driven. It displays the exact current clip name, duration, sample rate/count, loop recommendation, authored event markers, selected event name/time/payload, and current animation time. Click the timeline to seek without firing skipped events. `-FRAME` and `+FRAME` step by one authored sample, while `EVENT <` and `EVENT >` seek to the previous or next event and pause for inspection. Seeking cancels an in-progress fade and clears queued event/root-motion state so native and Web inspection remain deterministic.

Desktop mouse motion is accumulated so slow drags are not lost between frames, and partial Windows wheel units are retained until they form a complete step. Each repository-owned profile supplies identity, asset, animation, desired view height, and an optional bounded ground offset. The reusable viewer helper derives actor scale, camera, target, floor, pan limits, lighting area, and shadow area from model bounds. A ground offset compensates only for a measured difference between a static bind AABB and the animated sole plane; it does not rewrite animation root motion.

The authored scene is 1600 by 640. On native desktop and Web, the opt-in responsive-window policy makes the logical canvas follow the live client size, so the scene fills wide, tall, maximized, and manually resized windows without letterbox bars. The header remains left aligned, controls and status remain right aligned, the top background panel is hidden, and the middle remains available to the orbiting scene. The remaining panels and button fills use 80% opacity while their labels stay fully opaque. The desktop executable preserves its native x, y, width, and height across an ordinary relaunch. The status panel reports the eased live zoom angle, current FPS, draw calls, and submitted triangles.

The studio floor is a character-neutral enlarged plane with an emissive blue line grid. The complete grid is one custom mesh, one object, and one shared material, so another viewer profile receives the same inspection environment without model-specific renderer code or per-line draw calls.

The viewer intentionally consumes SM3D rather than loading GLB at runtime. To inspect another character, add a bounded profile, clip/socket mappings, and project asset publication; the main viewer source does not need character-specific camera constants. The viewer camera, interaction, lighting, animation selection, diagnostics, recovery, cleanup, and idle-reset behavior remain character-neutral.
