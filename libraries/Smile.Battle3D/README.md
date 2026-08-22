# Smile.Battle3D

`Smile.Battle3D` is a deterministic presentation bridge between `Smile.RPG.BattleView` and any 3D renderer. It owns no combat rules.

- `Actor` binds participants to caller-owned actor/animator handles and snapshots logical slots as world transforms.
- `Articulation` solves deterministic chained rigid-body limb poses, arbitrary endpoint-to-endpoint segments, locomotion cycles, foot lift, and action envelopes without adding character-specific concepts to Renderer3D.
- `Presentation` compiles BattleView cues into bounded timed commands for animation, movement, effects, numbers, messages, shake, sound, visibility, and rewards.
- `Camera` interpolates named position/target/FOV shots and seeded shake using fixed integer steps.
- `Effects` supplies data-driven alpha/additive billboard particles, color/size fades, flash, and requested shake from a bounded pool.
- Commands carry generic camera-shot hints, effect identifiers, and center/feet/head anchors. Rendering code remains free to interpret or override them.

The library keys all storage to the generation-safe RPG state handle. Destroying and recreating a state invalidates every prior binding and command timeline.

Run `scripts/test-battle3d.ps1` for cue mechanics parity and `scripts/test-battle-drama.ps1` for articulation/camera/VFX/material parity.
