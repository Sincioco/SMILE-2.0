# Smile.Battle3D

`Smile.Battle3D` is a deterministic presentation bridge between `Smile.RPG.BattleView` and any 3D renderer. It owns no combat rules.

- `Actor` binds participants to caller-owned actor/animator handles and snapshots logical slots as world transforms.
- `Presentation` compiles BattleView cues into bounded timed commands for animation, movement, effects, numbers, messages, shake, sound, visibility, and rewards.
- Commands carry generic camera-shot hints, effect identifiers, and center/feet/head anchors. Rendering code remains free to interpret or override them.

The library keys all storage to the generation-safe RPG state handle. Destroying and recreating a state invalidates every prior binding and command timeline.

Run `scripts/test-battle3d.ps1` for native/Web mechanics parity.
