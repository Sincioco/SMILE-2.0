# Dragonfall: The Ember Observatory

Dragonfall is an original low-poly 3D battle built entirely with reusable SMILE 2.0 systems. Four heroes—Arin the sword-and-shield defender, Tor the heavy gun attacker, Lyra the staff healer, and Mira the pointy-hat wizard—defeat three Cinderlings before Ashwing, the Caldera Tyrant, swoops into the enlarged arena. The encounter uses deterministic ATB rounds, a 70% health enrage transition, cinematic cameras, additive particles, equal-width multi-enemy HP/MP rows, three-gauge HP/MP/ATB party cards, soundtrack playback, and complete victory or defeat sequences.

No Final Fantasy assets, names, music, dialogue, fonts, models, or UI artwork are included. The delivery uses the broad staging language of cinematic console RPG battles as design inspiration while keeping all distributable content original.

## Run the crowd demo

Build `Dragonfall.smileproj` or launch `artifacts\games\Dragonfall.exe`. The battle starts hands-free, adapts its healing and attacks, and automatically restarts after the ending sequence. Select `TAKE COMMAND` at any time to pause party automation; select `RESUME AUTO` to restore it without restarting the encounter.

- `Space`: pause or resume.
- `Tab`: switch between automatic demo and manual party control.
- `1`: attack in manual mode.
- `2`: use the active hero's primary skill or spell in manual mode.
- `3`: defend in manual mode.
- `4`: use a Cinder Tonic in manual mode.
- `S`: cast Mira's Comet Frost in manual mode; toggle diagnostics in automatic mode.
- `A`: toggle the HUD in automatic mode.
- `D`: toggle the FPS display.
- `W`: restart immediately.
- Hold the primary mouse button on unoccupied arena geometry and drag to pan.
- Hold the middle mouse button on unoccupied arena geometry and drag to orbit.
- Use the mouse wheel to zoom; pan and zoom can operate simultaneously.
- `Escape`: exit.

## Run the playable version

Build `Dragonfall-NoDemo.smileproj` or launch `artifacts\games\Dragonfall-NoDemo.exe`. This startup contains no attract-mode lifecycle and no player-command AI; boss AI remains part of the shared encounter mechanics.

- `1` or `A`: attack.
- `2` or gamepad `X`: special ability.
- `3` or gamepad `Y`: heal when Lyra is ready.
- `4` or gamepad `B`: defend.
- `S`: use a Cinder Tonic.
- `Space`: pause or resume.
- `W`: restart.
- `D`: toggle the FPS display.
- Primary-drag unoccupied arena geometry to pan, middle-drag to orbit, and use the wheel to zoom.
- `Escape`: exit.

## Architecture

- `DragonfallBattle.smile` owns deterministic definitions, ATB submissions, boss policy, phase transitions, and presentation-neutral visual events.
- `DragonfallScene.smile` owns the original procedural arena/actors, bounds-driven and two-stage action cameras, fly-in/result tracking, bounded effect presets, transforms, rendering, and exact resource teardown.
- `Program.smile` is the crowd demo and is the only file containing player-command demo AI and automatic replay.
- `Program-NoDemo.smile` is the complete manual-command startup with that demo implementation removed.
- `DragonfallTests.smile` proves win/loss routes, all commands, boss action variety, phase transition, 100 mechanics restarts, and 108,000 accelerated fixed ticks in native and Web.
- `DragonfallLifecycleTests.smile` creates, renders, and destroys the complete arena 100 times and requires every Renderer3D resource count to return to zero in native DirectX and the WebGL2 test host.

All combat assets are preloaded. Particle objects are preallocated and draw after opaque geometry. Presentation can advance independently without altering mechanics.

## Original assets

`EmberObservatory.png` was generated with OpenAI's built-in image generation tool using the prompt: “Use case: stylized-concept; Asset type: tileable game texture; a seamless top-down volcanic obsidian floor with ancient circular bronze inlays and glowing magma cracks; dramatic warm orange emission against charcoal stone; hand-painted low-poly RPG style; square composition; no text, logos, characters, or copyrighted game imagery.”

The six WAV files are generated from mathematical waveforms by `scripts\generate-dragonfall-audio.ps1`. They have no sampled or copied source material.

## Validation

Run:

```powershell
.\scripts\test-dragonfall.ps1
```

The command validates native/Web mechanics parity, native/Web Renderer3D lifecycle parity, the crowd-demo build, and the no-demo build.
