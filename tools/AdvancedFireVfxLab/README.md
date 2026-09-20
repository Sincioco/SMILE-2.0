# Native Fire Lab

The native arena uses the [shared Viewer arena library](../../docs/libraries/arena3d.md).
F toggles Floor; G toggles Grid; B cycles the same five Viewer backgrounds.
Left drag pans, middle drag orbits, wheel zooms smoothly, and right-click resets
the camera. Grid color, spacing and dimensions remain library creation options.

Arin consumes the canonical Viewer-exported calibration JSON through its generated
asset. Saved equipment corrections use world axes, so the Arin adapter retains
the Viewer's authoring facing and grounding when placing him in this Lab. Do not
rotate the actor independently and expect identical equipment corrections.
`Test.ps1` compares all nine frame-zero poses with the Viewer's accepted socket
transform reference, in addition to comparing the complete calibration snapshot.

Build with `pwsh -File tools/AdvancedFireVfxLab/Build.ps1 -Target Native`, then launch
with `pwsh -File tools/AdvancedFireVfxLab/Launch.ps1`. The launcher closes only an
older copy of this Lab gracefully. Window placement is remembered.

The Lab starts on **Kael**, at speed **200**, with **Auto Orbit** enabled and **Demo** cycling:

- **Ember Strikes** (`FirePunch`): two chambered punches and forward fire jets.
- **Flame Sweep** (`FireSweep`): a raised, sweeping kick with fire attached to the foot.
- **Inferno Blast** (`FireBlast`): overhead gathering, a low lunge and two-hand release.

His sword stays hidden. The original motions were authored against Jared Koh's
[Firebending Animation](https://www.youtube.com/watch?v=bm35JrFHqPQ), Arcomade's
[live-action reference, 0:24–0:54](https://www.youtube.com/watch?v=jwwwfwCQl_A&t=24s),
and [Element Animation 2, 0:37–0:54](https://www.youtube.com/watch?v=T5vdPy7nbRQ&t=37s).
These are original, grounded interpretations, not imported reference animations.

**Arin / Kael** or **X** switches characters. Arin uses his canonical model and
saved calibration defaults, with the existing PaladinFlameBlade and LineFire
sword/shield flame settings. All eight original effect samples remain selectable
in the right panel. An attack button loops that one attack; **Demo** cycles all three.
Space pauses playback while the existing camera controls remain responsive.
Left drag pans, middle drag orbits, the wheel zooms, and right click resets the demo.

Kael's fire uses the new **KaelFireJet** thermal preset: flow-aligned flame tongues,
expanding outer fire, embers, heat distortion, and cooling smoke. Emission follows
the animated hand/foot sockets. Existing Arin/Dragon presets are unchanged. This
is the native feedback preview; adoption of these three fire attacks into Viewer
and Sin Star I rotations is a later task. His silver-gray hair is already adopted
in those applications through the active sixteen-clip model.

## Ownership and validation

`FireLabCharacters` owns actors, playback, four target pillars and character controls.
`KaelFire` owns only the caller-owned three jets and impact context and its timed
socket choreography. `FireLabArin` adapts the existing calibration module and
unchanged equipment flame parameters. The Lab loop advances the shared fire pool
once per frame. Switching or restarting releases the outgoing effects; normal
emission pauses retain the particle tail so it fades before the next attack.

`FireEmitter3D` adds one preset branch; existing presets, budgets and renderer
contracts do not change. No language, compiler, runtime, VSIX or dependency is
added. The existing sample/camera coordinator receives wiring only; new actor
algorithms stay in the focused owners above. No size limit or exclusion changes.

`Test.ps1` compiles/runs the focused native fixture: real nineteen-clip model,
three-part hair/sword preservation, all three automatic attacks, actual socket
motion, dual-hand emission, Arin calibration/flames, switching cleanup and native
CPU fallback. It also checks all four target handles and compares the complete
Arin calibration loaded by the actual Fire scene with the canonical package JSON.
The existing `FireEmitterTests` covers unchanged thermal contracts.
The fixture checks Begin3D, complete scene submission and End3DChecked on every
frame, including Arin flames and CPU fallback. `FireLabCharacters.Draw` returns
the combined actor/target/effect result and retains failure in its scene state.
Run `Test.ps1 -Fault Begin`, `-Fault Draw`, and `-Fault End` separately as deliberate
negative controls: each must fail the normal runner. They preserve actual frame
operations and inject one false result at the fixture's observation boundary.
The shared native Viewer/game checks cover silver-hair asset adoption.
Web adoption, publication and browser validation remain on hold.

The following Web notes document previously implemented behavior. New character-preview Web adoption and browser acceptance remain on hold.

The accepted [Studio design](../../docs/architecture/2026-09-09%20-%20SMILE%202.0%20Studio.md)
places this specialized workflow inside VFX. The standalone project and shared
effect modules remain authoritative. Generic saved `.smilevfx` authoring and Studio
hosting are future work; Studio development remains on hold.

## Existing build targets and sample controls

`Build.ps1` defaults to `-Configuration Release -Target All`: native first, then Web.
Run `bin\Release\AdvancedFireVfxLab.exe` for Desktop. The complete publishable Web
site is `bin\Release\Web` (upload all contents, not just `index.html`).
`-Configuration Debug` writes `bin\Debug\AdvancedFireVfxLab.exe` and `bin\Debug\Web`.
Use `-Target Native` or `-Target Web` for one target. Existing `-OutputPath` overrides
remain available for isolated native builds; they cannot be combined with Web/All.

From the repository root, serve Release Web with:

```powershell
python -m http.server 8767 --bind 127.0.0.1 --directory tools/AdvancedFireVfxLab/bin/Release/Web
```

Open `http://127.0.0.1:8767/`. Keep a stable host/port for browser persistence;
an origin change does not migrate or erase old saves. Application identity and
native settings remain unchanged by the build-folder layout.

The shared native `RememberWindowPlacement` setting saves the window's position, dimensions and maximized state under its stable application ID. Rebuilds preserve that placement. The native runtime retains its existing monitor/DPI safety handling. The in-program header is uppercase; the Windows title bar is unchanged.

In sample mode, Demo cycles Torch, Windy Torch, Brazier, Line Fire, Fireball, Fire Burst, and Dragon Breath. Selecting a sequence disables Demo. CPU/GPU comparison is available separately. FireBurstGen3 charges into a hot expanding ball before rapidly exploding; its accepted timing is preserved.

## Controls

The floor uses Water Lab's reflective arena and subdued blue grid. **Auto Orbit**
toggles continuous horizontal camera motion; manual pan, orbit and eased zoom remain
available. Bare Alt no longer interrupts the frame loop in rebuilt native programs.

- Backtick (the grave-accent key above Tab on US keyboards) cycles through panels hidden, all UI hidden, then the prior UI restored. The first tap keeps the header, FPS, and helper text. Hidden controls cannot intercept the mouse. The scene and camera continue normally. Right-click reset restores the UI.
- Left drag pans; middle drag orbits; wheel zooms smoothly.
- H Orbit, V Orbit, and Zoom sliders support hover-wheel adjustment and capture a drag until release, including outside the track.
- Right-click resets view/settings and restarts Demo; sample mode starts at Torch. Reset View only resets the camera.
- Space or P pauses/resumes. O toggles fire; R restarts the selected effect; Tab selects the next sequence.
- G cycles CPU/GPU/Auto; the quality button cycles Low/Medium/High.
- B or BG cycles landscape, title bitmap, black, green and purple. F toggles Floor and G toggles Grid independently. E switches the simulation backend. Right-click resets the camera.
- Ctrl toggles HDR/LDR; S toggles soft depth; D toggles heat distortion; O toggles turbulence; W toggles wind; 1 cycles debug layers.
- Click the equivalent labeled UI buttons. Cyan selection shows enabled options; HDR On/LDR and requested mode/quality are named explicitly. Fire Status reports the actual backend/quality and actual scene AA sample count.

## Shared presentation

`Smile.Simple3D.Arena3D` supplies the same reflective floor recipe as Water Lab:
1000 by 800 world units, 45-unit tiles and 1-unit blue grid lines. The coordinator
configures reflection before each scene begins. Both character previews share four
pillars with radii 12/24/36/48 and heights 60/95/130/165. Their bases sit on the
floor; the initial camera frames the wider arena. Decorative torches remain removed.

`Smile.Simple3D.StaticBackdrop3D` displays the Character Viewer's default Sin Star I landscape without its title. It is screen-fixed, not attached to camera motion. Localized heat refraction remains intentional. `Smile.UI.Controls` supplies the shared panels, buttons, sliders, hover hit-testing, and drag capture. Starting yaw, pitch and auto-orbit speed are constants at the top of `Program.smile`; the arena does not own the camera.

## Limits and validation

Native and capable WebGL2 High use five GPU systems and 1664 slots; Medium halves the slot budget. Six logical emitters and 32 shared GPU systems / 32,768 total slots are available; admission still falls back atomically when a complete layered effect cannot fit. CPU Low has simpler warm particles, not GPU turbulence/heat parity. Reported particle counts are logical schedules, not GPU readbacks. Web thermal forces, turbulence, cooling, bounds, soft depth and localized heat now use the existing transform-feedback path. Web scene MSAA and the Chrome High/GPU/fallback workflow are supported; quality remains capability-dependent.

`scripts\test-native-thermal-fire.ps1` checks deterministic assets, CPU dynamics, native GPU lifecycle/recovery/MSAA, high-level ownership, and generated-Web behavior. `FireEmitterTests.smile` checks the shared arena and a successful cross-target static-backdrop lifecycle. Actual Chrome has exercised both Labs at High/GPU, forced fallback, context recovery and focus/shutdown. Those observations are distinct from VM checks and do not certify every GPU or artistic preference. Chrome is the routine browser; other browsers need a specific reason.
