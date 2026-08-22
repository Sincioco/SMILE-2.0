# Dragonfall: The Ember Observatory

Dragonfall is an original low-poly 3D battle built entirely with reusable SMILE 2.0 systems. Four heroes—Arin the sword-and-shield defender, Tor the heavy gun attacker, Lyra the staff healer, and Mira the staff wizard—defeat three Cinderlings before Ashwing, the Caldera Tyrant, swoops into the enlarged arena. The encounter uses deterministic ATB rounds, a 70% health enrage transition, cinematic cameras, additive particles, equal-width multi-enemy HP/MP rows, three-gauge HP/MP/ATB party cards, sampled combat audio, soundtrack playback, and complete victory or defeat sequences.

No Final Fantasy assets, names, music, dialogue, fonts, models, or UI artwork are included. The delivery uses the broad staging language of cinematic console RPG battles as design inspiration while keeping all distributable content original.

The four heroes use reusable 56-part rigid rigs plus five independently animated face planes, with posed thighs, shins, boots, upper and lower arms, hands, shoulders, layered clothing, role-specific hair, and equipment. Deterministic blinks and synchronized attack, pain, victory, and defeat expressions make the tight actor cameras feel performed rather than static. Each Wave 1 combatant uses a distinct 30-part skeleton role rig. Ashwing uses a 97-part creature rig rendered at twice its original scale, with a two-link neck, articulated jaw, four two-link legs, rooted wing bones and membranes, a three-link tail, horns, eyes, layered facial planes, teeth, armor, and dorsal spikes. Shared meshes keep the expanded cast within the bounded Renderer3D object and mesh budgets. Impact flashes, target recoil, persistent grounded bodies, extinguished dragon eyes, distinct survivor dances, and the rotating result camera make damage, death, victory, and defeat visually unambiguous.

## Run the crowd demo

Open `Dragonfall.sln` in Visual Studio, then choose either `Windows 64-bit .exe` or `Web` from the platform dropdown. The solution declares both target platforms explicitly so `Web` publishes WebGL2 output and `Windows 64-bit .exe` builds the native DirectX executable. You may also build `Dragonfall.smileproj` directly from the command line or launch `artifacts\games\Dragonfall.exe`. The battle starts hands-free, adapts its healing and attacks, and automatically restarts after the ending sequence. Select `TAKE COMMAND` at any time to pause party automation; select `RESUME AUTO` to restore it without restarting the encounter.

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
- `Smile.Battle3D.Articulation` provides reusable deterministic segment solving, locomotion cycles, and action envelopes for rigid characters and creatures.
- `DragonfallScene.smile` owns the original procedural arena, articulated cast composition, bounds-driven and two-stage action cameras, fly-in/result tracking, bounded effect presets, transforms, rendering, and exact resource teardown.
- `DragonfallAudio.smile` maps presentation events to layered, four-channel sampled cues without affecting battle mechanics.
- `Program.smile` is the crowd demo and is the only file containing player-command demo AI and automatic replay.
- `Program-NoDemo.smile` is the complete manual-command startup with that demo implementation removed.
- `DragonfallTests.smile` proves win/loss routes, all commands, boss action variety, phase transition, 100 mechanics restarts, and 108,000 accelerated fixed ticks in native and Web.
- `DragonfallLifecycleTests.smile` creates, renders, and destroys the complete arena 100 times and requires every Renderer3D resource count to return to zero in native DirectX and the WebGL2 test host.

All combat assets are preloaded. Particle objects are preallocated and draw after opaque geometry. Presentation can advance independently without altering mechanics.

## Assets

`EmberObservatory.png` was generated with OpenAI's built-in image generation tool using the prompt: “Use case: stylized-concept; Asset type: tileable game texture; a seamless top-down volcanic obsidian floor with ancient circular bronze inlays and glowing magma cracks; dramatic warm orange emission against charcoal stone; hand-painted low-poly RPG style; square composition; no text, logos, characters, or copyrighted game imagery.”

Combat WAV files under `Assets\SFX` are processed from the CC0 packs and source files listed in `Assets\SFX\LICENSE.md`. The battle soundtrack uses the MP3 selected in `Program.smile` and `Program-NoDemo.smile`.

## Validation

Run:

```powershell
.\scripts\test-dragonfall.ps1
```

The command validates native/Web mechanics parity, native/Web Renderer3D lifecycle parity, the crowd-demo build, and the no-demo build.
