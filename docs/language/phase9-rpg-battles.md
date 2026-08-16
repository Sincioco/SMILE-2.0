# Phase 9 RPG battles

Phase 9 advances `Smile.RPG` to 1.2.0 with the ordinary SMILE modules `BattleCore`, `BattleEffects`, `BattleStrategy`, and `BattleView`. It adds no language keyword, compiler intrinsic, native runtime helper, renderer command, or audio primitive.

The reusable boundary is deliberately presentation-neutral:

- BattleCore owns deterministic participants, rounds, actions, results, rewards, and events.
- BattleEffects owns bounded effect and battle-status definitions.
- BattleStrategy owns atomic standing orders, Fight/Run preparation, safe interruption, and deterministic enemy AI.
- BattleView maps events to logical slots and presentation cues without drawing them.
- Applications own images, menus, animation, particles, camera, music, sound effects, and exact world/dungeon transition presentation.

The application supplies an explicit nonnegative seed to `BeginBattle`. Given the same definitions, RPG progress, commands, and seed, Windows and Web resolve the same action order, random values, events, rewards, and outcome. Failed action or reward commits roll back mechanics, consumables, MP, status state, event count, rewards, and PRNG position together.

Only one battle can be active for an RPG state. Active battle sessions are transient and block Encode, Decode, Save, and Load with `RPG_RESULT_BATTLE_ACTIVE`; SRPG format 2 remains unchanged and formats 1 and 2 remain readable.

`examples\Phase9BattleStateTests` is the project/package native/Web contract proof. `examples\RpgBattleGallery` combines the modules with original public artwork and audio in overworld, top-down town, and first-person corridor presentations. Detailed capacities and formulas are in [the battle API](../libraries/smile-rpg-battle-api.md).
