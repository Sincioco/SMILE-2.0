# Smile 2.0 - Advance Native Fire Lab

Native Direct3D 11 thermal-fire demonstration. Build with `Build.ps1`, then run `bin\Debug\AdvancedFireVfxLab.exe`. Assets are copied from the repository's `TechnicalAssets\Generation3\Fire` and Sin Star I landscape into ignored tool-local inputs; project publication places them beside the executable.

The shared native `RememberWindowPlacement` setting saves the window's position, dimensions and maximized state under its stable application ID. Rebuilds preserve that placement. The native runtime retains its existing monitor/DPI safety handling. The in-program header is uppercase; the Windows title bar is unchanged.

Demo starts automatically and cycles Torch, Windy Torch, Brazier, Line Fire, Fireball, Fire Burst, and Dragon Breath. Selecting a sequence disables Demo. CPU/GPU comparison is available separately. FireBurstGen3 charges into a hot expanding ball before rapidly exploding; its accepted timing is preserved.

## Controls

- Backtick (the grave-accent key above Tab on US keyboards) cycles through panels hidden, all UI hidden, then the prior UI restored. The first tap keeps the header, FPS, and helper text. Hidden controls cannot intercept the mouse. The scene and camera continue normally. Right-click reset restores the UI.
- Left drag pans; middle drag orbits; wheel zooms smoothly.
- H Orbit, V Orbit, and Zoom sliders support hover-wheel adjustment and capture a drag until release, including outside the track.
- Right-click resets the view/settings and restarts Demo at Torch. Reset View only resets the camera.
- Space or P pauses/resumes. F toggles fire; R restarts the selected effect; Tab selects the next sequence.
- G cycles CPU/GPU/Auto; the quality button cycles Low/Medium/High.
- B or BG cycles landscape, title bitmap, black, green and purple. Floor / Grid hides/shows both together. Right-click restores landscape and visible floor/grid.
- Ctrl toggles HDR/LDR; S toggles soft depth; D toggles heat distortion; O toggles turbulence; W toggles wind; 1 cycles debug layers.
- Click the equivalent labeled UI buttons. Cyan selection shows enabled options; HDR On/LDR and requested mode/quality are named explicitly. Fire Status reports the actual backend/quality and actual scene AA sample count.

## Shared presentation

`Smile.Simple3D.Arena3D` is the same floor/grid recipe as Character Viewer. Fire Lab requests a 1000-by-1000 black arena, neon-orange lines, tile spacing 160 and line thickness 2 world units. The tile spacing is four times the previous 40-unit setting. All four pillars and decorative torches have been removed.

`Smile.Simple3D.StaticBackdrop3D` displays the Character Viewer's default Sin Star I landscape without its title. It is screen-fixed, not attached to camera motion. Localized heat refraction remains intentional. `Smile.UI.Controls` supplies the shared panels, buttons, sliders, hover hit-testing, and drag capture. Starting yaw, pitch and auto-orbit speed are constants at the top of `Program.smile`; the arena does not own the camera.

## Limits and validation

Native High uses five GPU systems and 1664 slots; Medium halves those budgets. Four logical emitters are allowed, but only eight GPU systems exist globally, so additional layered emitters can fall back atomically to CPU Low. CPU Low has simpler warm particles, not native turbulence/heat parity. Reported particle counts are logical schedules, not GPU readbacks. Web visual work is deferred; its existing backend remains supported and new high-level thermal fire has a CPU fallback.

`scripts\test-native-thermal-fire.ps1` checks deterministic assets, CPU dynamics, native GPU lifecycle/recovery/MSAA, high-level ownership, and the retained shared Web fallback. `FireEmitterTests.smile` also checks the shared arena and static-backdrop lifecycle. Native visual review remains the user's acceptance step.
