# SMILE 2.0 Neon Cycles

Neon Cycles is the reference true-Simple3D game. It supports one player against deterministic AI and two local human players, fixed-step simulation, swept trail/arena collision, first-to-five scoring, same-tick draw handling, true vertical 3D trail walls, and a 2D HUD.

## Controls

- Player 1: Left/Right arrows or virtual X/B
- Player 2: A/D
- Space: pause
- Escape: menu

The title menu offers one-player, two-player, and instructions. Round results advance after a short hold or Enter/A; a completed match offers rematch or return to menu.

## Architecture

`NeonCyclesSimulation.smile` owns authoritative integer gameplay geometry, fixed turns, trail segments, swept collision, round/match scoring, and same-tick draw resolution. `NeonCyclesAI.smile` observes the same state and submits only `TURN_LEFT` or `TURN_RIGHT` through `Simulation.RequestTurn`; it cannot set position or bypass collision.

`Program.smile` owns presentation and input. It runs a bounded 60 Hz accumulator with at most six catch-up steps, preallocates the arena/cycles/130 trail object instances once, changes only transform/visibility while playing, and draws the HUD through Renderer2D after Renderer3D. Round resets allocate no new GPU resource. Audio follows the standard SMILE game-focus lifecycle.

Run `scripts\test-true-simple3d-neon-cycles.ps1` from the repository root for the native/Web math, simulation, AI, conformance, renderer, build, and launch gate.
