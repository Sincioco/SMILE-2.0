# Dragonfall: The Ember Observatory

Dragonfall is an original low-poly 3D boss encounter built entirely with reusable SMILE 2.0 systems. Three heroes—Arin, Mira, and Tor—fight Ashwing, the Caldera Tyrant through deterministic ATB rounds, a 50% health enrage transition, cinematic cameras, additive particles, an always-on 2D HUD, procedural music, and a complete victory or defeat sequence.

No Final Fantasy assets, names, music, dialogue, fonts, models, or UI artwork are included. The delivery uses the broad staging language of cinematic console RPG battles as design inspiration while keeping all distributable content original.

## Run the crowd demo

Build `Dragonfall.smileproj` or launch `artifacts\games\Dragonfall.exe`. The battle starts hands-free, adapts its healing and attacks, and automatically restarts after the ending sequence.

- `Space`: pause or resume.
- `1`: restart immediately.
- `2`: toggle the HUD.
- `D`: toggle renderer diagnostics.
- `Escape`: exit.

## Run the playable version

Build `Dragonfall-NoDemo.smileproj` or launch `artifacts\games\Dragonfall-NoDemo.exe`. This startup contains no attract-mode lifecycle and no player-command AI; boss AI remains part of the shared encounter mechanics.

- `1` or `A`: attack.
- `2` or gamepad `X`: special ability.
- `3` or gamepad `Y`: heal when Mira is ready.
- `4` or gamepad `B`: defend.
- `S`: use a Cinder Tonic.
- `Space`: pause or resume.
- `W`: restart.
- `Escape`: exit.

## Architecture

- `DragonfallBattle.smile` owns deterministic definitions, ATB submissions, boss policy, phase transitions, and presentation-neutral visual events.
- `DragonfallScene.smile` owns the original procedural arena/actors, fixed camera shots, bounded effect presets, transforms, rendering, and exact resource teardown.
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
