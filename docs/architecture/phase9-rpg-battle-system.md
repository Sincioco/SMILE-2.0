# Phase 9 RPG battle-system architecture

The battle system is a four-layer source API inside `Smile.RPG`:

| Layer | Owns | Does not own |
| --- | --- | --- |
| BattleEffects | effect/status definitions and bounded metadata | participant mutation or presentation |
| BattleCore | active session, participants, deterministic mechanics, transactions, events, rewards | UI, images, audio, drawing, world presentation |
| BattleStrategy | standing orders, repeat/fallback, Fight/Run, interrupts, AI | mechanics formulas or menus |
| BattleView | logical X/Y/Z slots and event-to-cue translation | asset selection, rendering, audio playback |

`BattleCore` consumes the existing Characters, Party, Inventory, Equipment, and Abilities modules. SaveGames observes only whether a battle is active and blocks persistence at that boundary. World and Encounters remain independent: an application records the exact return scene/cell/facing, begins a battle from encounter metadata, and restores that location after victory, defeat, or escape.

The future Renderer3D seam is the combination of stable participant IDs, logical three-dimensional slots, facing/layer/anchor metadata, mechanics events, and presentation cues. A 2D or future 3D application may interpret those records differently without changing battle authority. Phase 9 implements no 3D renderer.

All collections are fixed-capacity, all mutations reject invalid IDs and values safely, one action is one transaction, and deterministic scheduling uses effective Agility followed by stable participant ID. Definitions are reusable during the state lifetime; active battle state is cleared by `EndBattle` and never enters SRPG data.
