# Dragonfall2: Arin at the Ember Observatory

Dragonfall2 is a separate SMILE 2.0 project that preserves Dragonfall's complete two-wave battle, balance, party, boss AI, cameras, HUD, audio, victory, defeat, demo, and playable flows. The visual for the first party member is replaced by the repository-owned Arin v5.4 Character3D candidate.

The scene keeps Dragonfall's original bounded procedural effects and layers selected current `Smile.Simple3D.Effects3D` presets over confirmed sword, shield, elemental, and dragon impacts. Those effects use the current batched particle and ribbon renderer; Dragonfall itself remains unchanged because its startup never enables Dragonfall2 mode. Dragonfall2 fits Arin to 120 percent of the procedural party height, grounds his imported bounds, faces him toward the enemy line, and uses his looping `Idle` clip between actions.

Automatic battle cameras derive Arin's center and framing radius from the current imported actor bounds. Close-ups therefore adapt to the subject instead of assuming the procedural heroes' fixed origin and size. Scene restart readiness uses current handles, exact ownership counts, object headroom, and a submitted-frame check so a sticky diagnostic from an optional animation or VFX fallback cannot force a later encounter into the 2D fallback.

Open `Dragonfall2.sln` in Visual Studio or build `Dragonfall2.smileproj`. The native executable is published as `artifacts\games\Dragonfall2.exe`; Web output uses the same source and asset set. Build `Dragonfall2-NoDemo.smileproj` for the complete playable teaching version without attract-mode automation.

Controls remain the same as Dragonfall: `Space` pauses, `R` or `W` restarts, `D` toggles FPS, `Tab` switches automatic/manual battle control, and the existing pointer camera controls remain available. The battle begins with the original Boneguard, Necromancer, and Dread Archer wave before Ashwing enters.

The native and Web Renderer3D object capacity is 1,024. Dragonfall2's Wave 1 scene owns 445 live objects; the Wave 2 boss scene owns 452, leaving 572 object slots of headroom. A complete shutdown returns Character3D actors and every Renderer3D resource count to zero, and a second encounter must initialize and draw without resetting the renderer.

Arin v5.4 remains a prototype candidate. Dragonfall2 deliberately does not change the production-release gate recorded in `docs\implementation\paladin-production-readiness-m7c-b.md`.
