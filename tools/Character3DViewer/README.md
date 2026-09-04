# SMILE 2.0 - 3D Viewer, Animation Editor

The SMILE 2.0 - 3D Viewer, Animation Editor is a reusable tool for inspecting and lightly editing converted SM3D characters on the native or Web Renderer3D backend. Arin is the default character; Paladin is his party role. The original three-clip Arin model and the articulated technical fixture remain selectable with `PROFILE` or `4`.

This folder is the permanent home of the editor source, `.smileproj`, build entry point, and launcher. Sin Star I owns the canonical Arin v5.7 and Red Dragon assets. `Prepare-BuildAssets.ps1` copies only the required cooking inputs into the ignored `BuildAssets` directory so SMILE's project-confined asset rules remain intact without making Dragonfall own the editor.

Build and launch from this folder:

```powershell
& .\Build.ps1
& .\Launch.ps1
```

The native executable is `bin\Character3DViewer.exe`. `Launch.ps1` closes older viewer instances before launch, restores repository calibration JSON only when no live working copy exists, and mirrors subsequent `Save Frame` changes back to Arin v5.7's canonical JSON while the editor is running.

Arin v5.4, v5.5, and v5.6 are preserved diagnostic candidates rather than approved character results. Each has unacceptable right sword-arm, wrist, hand-connection, or grip defects. Arin v5.7 is the active validated checkpoint with seven approved clips and a full-frame sword/shield collision audit.

The viewer starts in a hands-free presentation with `DEMO ON`: it auto-orbits and advances through the available animations. Demo always completes at least three full loops of the current animation and stays on it for at least five seconds. Short clips repeat additional whole loops until the five-second minimum is reached; long clips finish their third full loop. Demo never cuts an animation off mid-loop. Arin v5.7 defaults to a widened `20 deg` arena view while the Red Dragon is visible; hiding the Dragon restores the authored `-16 deg` character-inspection view. Its title bar includes the current source-model filename, and every available animation has a fitted, centered button in the right-side panel. Arin v5.7 also starts with an optional presentation-only equipment glow: enlarged additive copies of the animated sword and shield meshes are drawn behind the opaque character, leaving white/cyan and warm-gold silhouette outlines that remain visible from every angle. Unconnected fading glow particles follow the sword tip only during the authored Sword Attack trail window and the shield center around the Block Impact window; motion-trail triangles never span between sampled poses. Trail history is reset at each effect window so animation loops cannot connect distant poses. The effect has no flame emitter, surrounding halo, flat blade ribbon, or idle motion trail. `GLOW` toggles all four elements without changing Arin's source textures, materials, or candidate asset. The on-screen `RESET` button performs a complete presentation reset: it recenters and refits the current character/arena view, returns to Idle, enables Demo, and resumes auto-orbit without changing the current pause state. Manual camera input stops auto-orbit; ten seconds without further camera input performs the same one-shot reset while also preserving pause state.

Controls:

- Hold the left mouse button and drag to pan.
- Hold the middle mouse button and drag left/right or up/down to orbit at direct pointer sensitivity. Vertical orbit supports a complete 360-degree revolution.
- Use the mouse wheel to set a bounded, responsive zoom target; each wheel step is visibly meaningful and the live camera eases toward it without stopping auto-orbit. The close-inspection range extends to `-48 deg`.
- Right-click to pause or resume all scene movement. It is the only pause-state control; camera pan, orbit, and zoom remain active while paused.
- Click `RESET` beside the timeline to reset the view and presentation without changing pause state.
- Click `DEMO ON/OFF` to enable or disable the automatic animation presentation. Clicking an individual animation disables Demo and plays that animation continuously in a loop.
- Arrow keys orbit. `Ctrl+Left` and `Ctrl+Right` step the selected animation backward or forward by one frame. `W`, `A`, `S`, and `D` pan.
- `1`, `2`, and `3` select the first three animations.
- `O` toggles auto-orbit. Turning it off starts the ten-second idle-reset timer.
- `B` or the `BG` button cycles the scene background through black, green, and purple.
- `DRAGON` hides or shows the Red Dragon. The Dragon is visible by default for Arin v5.7; toggling it also switches between the face-to-face arena staging and the normal isolated-character view.
- `Sword` and `Shield` independently hide or show each equipment mesh together with its matching glow and particle trail.
- `F` hides or shows the floor and grid together. `G` hides or shows only the grid.
- `Enter` resets the presentation without changing pause state. Close the window to exit.
- `PROFILE` or `4` switches profiles.
- `GLOW` hides or shows the current profile's equipment aura. Arin v5.7 enables it by default; profiles without the required weapon sockets leave the toggle unavailable.
- `SOCKET` shows socket origins and RGB local-axis endpoints.
- `CHANNEL` cycles lit, base-color, normal, roughness, metallic, occlusion, and emissive inspection.
- `-` and `+` change playback speed from 25% through 200%; authored speed is 100%.

The bottom timeline is model-driven. It displays the exact current clip name, duration, sample rate/count, loop recommendation, authored event markers, saved pose-keyframe markers, selected event name/time/payload, and current animation time. Click or drag the timeline to seek without firing skipped events, or hover over it and use the wheel to move one authored frame at a time. `0-FRAME` jumps to the start, `-FRAME` and `+FRAME` step by one authored sample, and `EVENT <` and `EVENT >` seek to the previous or next event. None of these controls changes pause state; right-click first when a stationary pose is required. Seeking cancels an in-progress fade and clears queued event/root-motion state so native and Web inspection remain deterministic.

Pose Calibration stores as many as 256 correction keyframes per animation clip. `Save Frame` adds a keyframe at the displayed authored frame or updates the existing keyframe there. The viewer linearly interpolates wrist, sword, and shield corrections between saved frames; rotational channels use the shortest angular path, and looping clips blend cyclically from the last saved keyframe back to the first. A single saved keyframe is held throughout the clip. Green timeline ticks identify saved keyframes. `Reset Clip` clears only the selected animation's keyframes after one confirmation, while `Reset All` clears every animation's keyframes after two confirmations. The complete multi-clip track persists across viewer relaunches.

Desktop mouse motion is accumulated so slow drags are not lost between frames, and partial Windows wheel units are retained until they form a complete step. The native runtime also reconciles its retained left, right, and middle button state from every Windows mouse-move message. A drag therefore repairs its own missing press or release transition and can reacquire an otherwise unowned mouse capture instead of leaving pan or orbit unresponsive. Each repository-owned profile supplies identity, asset, animation, desired view height, and an optional bounded ground offset. The reusable viewer helper derives actor scale, camera, target, floor, pan limits, lighting area, and shadow area from model bounds. A ground offset compensates only for a measured difference between a static bind AABB and the animated sole plane; it does not rewrite animation root motion.

The authored scene is 1600 by 640. On native desktop and Web, the opt-in responsive-window policy makes the logical canvas follow the live client size, so the scene fills wide, tall, maximized, and manually resized windows without letterbox bars. The header remains left aligned, controls and status remain right aligned, the top background panel is hidden, and the middle remains available to the orbiting scene. The remaining panels and button fills use 80% opacity while their labels stay fully opaque. The desktop executable preserves its native x, y, width, and height across an ordinary relaunch. The status panel reports the eased live zoom angle, current FPS, draw calls, and submitted triangles.

The studio floor is an enlarged plane with an emissive blue line grid. Arin v5.7 expands it to a 1000 by 1000 shared arena for the default-visible Red Dragon; other profiles retain their fitted character-inspection floor. The complete grid remains one custom mesh, one object, and one shared material without per-line draw calls.

The Red Dragon is a static prototype prop loaded through the same validated SM3D/PBR path as other 3D assets. It is scaled to 50,000%, placed about 240 world units directly ahead of Arin, and rotated so its forward direction exactly opposes Arin's. The camera and orbit remain targeted on Arin, while the Dragon is intentionally much larger and may extend beyond the edge of the frame.

The viewer intentionally consumes SM3D rather than loading GLB at runtime. To inspect another character, add a bounded profile, clip/socket mappings, and project asset publication; the main viewer source does not need character-specific camera constants. The viewer camera, interaction, lighting, animation selection, diagnostics, recovery, cleanup, and idle-reset behavior remain character-neutral.
