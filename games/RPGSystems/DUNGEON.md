# RPG Dungeon Gallery

The Dungeon option in `RPGSystems` is the permanent, original Phase 8 capability demo. Phase 8.1 hardens its event transactions and acceptance surface while continuing to compose the existing Smile.Game, Smile.RPG, and Smile.UI packages without introducing a second world model or a battle system.

The title starts either the three-floor cardinal first-person **Prism Vault** or the four-floor top-down **Sunken Archive**. Both routes use the same character, party, inventory, story, actor, encounter-preview, and SRPG 2 save state.

## Controls

- Title: Up/Down and Enter/Space.
- First person: Up/W forward, Down/S backward, Left/A and Right/D turn, Enter/Space interact, Escape menu.
- Top down: arrows/WASD walk, Enter/Space interact, Escape menu.
- Menus/dialogue: arrows, Enter/Space, Escape.

## Demonstrated behaviour

- three cardinal first-person floors and four top-down floors;
- deterministic directional walk animation, an on-screen compass, a visible party follower, and foreground occlusion;
- ordinary and locked doors, key items, one-time chests, Gold, one-shot and repeating traps;
- a hidden passage, main and alternate reciprocal stairs, a one-way chute, a warp, and a dead-end reward branch;
- flag/item-aware NPC dialogue and upper-floor escape to the known top-down entrance;
- encounter preview only, with no attacks, damage formula, AI, rewards, victory, or defeat;
- save/load while inside either dungeon, including floor, cell, facing, opened actors, story, inventory, party, stats, and encounter progress.

## Phase 8.1 event contract

`DungeonWorkflow.smile` is deliberately application-local. The gallery and `Phase8DungeonStateTests` compile the same source, so tests do not model a second approximation of the production workflow. It supplies explicit result codes for success, already-completed events, missing requirements, capacity rejection, blocked operations, invalid state, apply failure, missing data, and wrong schema.

Doors, locked doors, Gold/key/multi-item chests, hidden passages, traps, first NPC dialogue, encounter begin/return, transitions, spawns, escape, start, save, and load report success only after their authoritative state change succeeds. Multi-module mutations roll back on later failure, one-time events are idempotent through Story flags, and load treats Story as canonical while reconciling event-actor visibility before accepting the state. A private disposable test copy injects failures between real workflow commit steps; production contains no fault hook.

Top-down Escape and Interact commands are locked while the six-step movement interpolation owns a reserved destination. Initialization uses a cumulative failure latch so a later success cannot conceal an earlier failed definition, menu item, map, tile, animation frame, or handle creation.

The focused suite proves project/package native and Web parity, transaction rollback, capacity behavior, idempotency, result mapping, movement policy, encounter return, load reconciliation, and invalid-load rollback. The topology validator models complete legal progression across all four top-down floors and all three first-person floors, including every interaction and transition source/destination.

All images are reused from the repository's original RPG Systems World art set. No commercial game asset or reference data is included.
