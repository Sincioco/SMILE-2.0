# Renderer3D Post-Processing Lab

This M5 lab exercises one directional shadow map, an animated Character3D actor, a masked imported part, a simple-material object, linear HDR rendering, ACES tone mapping, and bounded bloom. The 2D HUD is drawn after `Scene3D.EndScene`, so it remains outside post-processing.

Controls:

- `Up`: cycle Low, Medium, High, and Auto quality.
- `S`: toggle shadows.
- `A`: toggle HDR and post-processing together.
- `W`: toggle bloom.
- `Right`: cycle exposure from 100% through 175%.
- `Space`: cross-fade the actor between Bend and AttackLike.
- `D`: toggle diagnostics.
- `Enter`: restore the High-profile showcase.
- mouse/touch drag and wheel: orbit, pitch, and zoom.
- `Escape`: exit.

The diagnostics distinguish logical submissions from shadow, main, and post draws and show the effective target, sample count, shadow/bloom dimensions, and fallback flags.
