# Phase 8 RPG dungeon implementation report

Phase 8 began from `65c00058e840b6ca2dfa67ec18c2c04c81340f13` on `main`, with `HEAD` equal to `origin/main` and a clean worktree. The tracked implementation is commit `af51e280449fbab2be2198c395b75e9e769d7418`, pushed successfully to `origin/main`. The separate closure commit containing this report is identified in the completion handoff because a commit cannot contain its own SHA.

## Gap decision and reused architecture

The required pre-production [gap matrix](../architecture/phase8-rpg-dungeon-gap-matrix.md) found no missing generic API. Phase 8 reuses `World`, `Story`, `Inventory`, `Characters`, `Party`, `Encounters`, `SaveGames`, `CardinalMover`, `Animation`, `TileMap`, `Camera2D`, and `Smile.UI`. No second scene graph, actor system, movement stack, inventory, persistence system, quest DSL, pathfinding facility, or Phantasy-Star-specific public surface was created.

The permanent new work consists of the original `examples\RpgDungeonGallery`, the presentation-independent `examples\Phase8DungeonStateTests`, the 62-check map topology validator, smoke/artifact integration, and documentation. No generic library module or API was added.

## Versions and formats

- `Smile.Game`: 1.0.0
- `Smile.RPG`: 1.1.1
- Visual Studio extension: 2.0.45 (`2.0.45.0` assembly/file version)
- SMILE-MAP: writes and reads format 1
- SRPG: writes format 2; reads formats 1 and 2
- `.smilelib`: format 5

No version changed because no compiler, runtime, library source, public package API, serialized field, or installed VSIX payload changed.

## Dungeon architecture and topology

The Prism Vault is a three-floor first-person cardinal grid. `World` owns the hero's scene, cell, facing, persistent actors, and registered spawn endpoints. The application owns bounded 9-by-9 geometry and projects relative cells with ordinary 2D drawing. Forward/backward moves reserve and complete exactly one authoritative cell; left/right turn exactly 90 degrees without firing entry events. Its topology includes reciprocal B1/B2 and B2/B3 stairs, a one-way B2/B3 chute, a B3/B1 warp, doors, chests, traps, a hidden passage, a state-aware NPC, a deterministic encounter zone, an on-screen compass, and a direct exit into the top-down entrance.

The Sunken Archive is a four-floor top-down dungeon using four SMILE-MAP 1 files. `CardinalMover`, `TileMap`, `Camera2D`, `Animation`, World collision/reservations, layered art, foreground occlusion, and a party follower form the presentation. Main B1/B2 stairs reach the lower B2 section; alternate reciprocal stairs reach its upper section. A visible-solid locked door is the only connection through the dividing wall. B3 contains a one-approach dead-end key/item branch. Normal B2/B3 and B3/B4 stairs, a B2/B4 one-way chute, and a B3/B1 warp complete the topology. Escape from any floor applies the stable B1 entrance spawn.

## Event and persistence results

- Door and locked-door result: closed visible-solid actors block their traversable base cells; ordinary open hides the actor, locked open rejects without the key, and a valid retained key persistently opens the branch.
- Chest/reward result: separate Gold and item/key chests preflight capacity, grant 50 Gold or the defined items exactly once, record Story state, and hide only after successful reward application.
- Trap result: first-person and top-down one-shot traps damage once and persist their spent flags; repeatable traps damage once per completed cell entry and increment a Story value. Turning and drawing never invoke the entry handler.
- Hidden-passage result: the visible-solid concealed-wall actor appears closed, blocks movement, becomes hidden/traversable after interaction, and persists that state.
- Stairs/chute/warp result: stable scene/spawn endpoints preserve destination cell/facing; normal and alternate stairs are reciprocal, while the chute and warp are deliberately one-way edges.
- Dungeon NPC/dialogue result: multi-page initial dialogue sets a Story flag; repeat dialogue changes and branches on key inventory state.
- Dungeon escape result: the command works from upper floors in both presentations and places the controlled hero coherently at the known top-down entrance while preserving dungeon progress.
- Encounter-preview result: both modes use deterministic Encounter progress, set pending preview metadata and an exact World return location, implement no battle resolution, and restore the exact scene/cell/facing on return.
- Save/load result: SRPG 2 preserves current floor/cell/facing, Character and Party/Gold state, Inventory, persistent door/chest/passage actors, Story trap/NPC state, and Encounter progress transactionally. Existing format-1/2 compatibility, maximum-payload, final-layout preflight, transient preservation, reservation rollback, and deterministic-write tests remain authoritative.

## Gallery and focused validation

- DirectX: Release/debug-info project build passed, all nine declared original assets/maps published, native x64 GUI verification passed, and a two-second launch probe remained live.
- GDI: Release project build passed with the same nine files, native x64 GUI verification passed, and a two-second launch probe remained live.
- Web/DPR 2: build and JavaScript syntax check passed; the repository Node host executed 40 frames with `devicePixelRatio = 2` and no warning or error. Published asset/map bytes match their source files.
- Windows project/package state matrix: both executables passed all 79 deterministic checks with exact output parity.
- Web project/package state matrix: both generated applications passed all 79 checks with exact native console parity.
- Map topology: 62 checks passed across four complete maps, six nonempty foreground cells, ten traversable stair/chute/warp endpoints, the closed/open locked partition, the dead-end reward cell, and the exit.

## Private PS1 and PS2 validation

All work in `D:\SMILE 2.0 Local Reference Tests` remains outside Git and outside public Web output.

The PS1 project adds a first-person Camineet Warehouse with entrance/return facing, an ordinary door, locked door, 50 Meseta chest, and the unlimited-use Dungeon Key gated by an existing prior-dialogue flag. In-dungeon menu save/load uses schema 8. DirectX and GDI each compiled, published 15 private assets, and remained live through launch probes. The existing Camineet regression still exposes exactly 258 reachable road cells while keeping representative lawn cells blocked.

The PS2 project adds four top-down Shure floors loaded one at a time, reciprocal vertical links, a one-way chute, warp, doors, boxes, a progression key, traps, hidden passage, state-aware NPC, party follower, encounter zone/preview, upper-floor save/load, and return to Mota. DirectX and GDI each compiled, published 19 private assets including all four Shure maps, and remained live through launch probes. The starter party/direction/NPC-spacing regression and 222-cell town connectivity still pass.

Private provenance logs retain access dates, local filenames, hashes, commercial/reference-only flags, and the consulted PS1/PS2 map, walkthrough, and sprite-source URLs. No ROM was downloaded.

## Copyright and repository safety

The final SHA-256 audit hashed 145 private binary files and all five newly tracked public binary files and found zero matches. The five public PNG files are byte-identical reuse of the repository's existing original `RpgWorldGallery` art, not derivatives of private references. No private project is in the tracked solution; no private commercial asset, map, audio, screenshot, provenance log, evidence file, executable, or Web publication is tracked or copied into public artifacts.

## Complete regression result

The final normal repository smoke suite passed in 250.77 seconds. It includes 226 managed language/compiler/project/completion/timing tests, eight formatter integration groups, the 181-file SMILE style gate, 39 native graphics/audio-focus checks, 38 native Text checks, Phase 6/7 package and rollback coverage, the four Phase 8 state variants, 62 Phase 8 topology checks, all seven existing games and their no-demo teaching programs on native and Web, native x64 GUI inspection, asset byte verification, viewport/DPI verification, and final VSIX payload verification. Independent `verify-artifacts.ps1` execution also passed.

Visual Studio 2026 installation acceptance is not applicable: the compiler, language library, packages, project system, and VSIX payload did not change. The master solution includes both Phase 8 projects, the dedicated gallery solution is present, the full solution build passed, and the existing VSIX 2.0.45 payload was rebuilt and verified without requiring reinstall or IDE cache mutation.

## Known limitations and phase boundary

The original gallery intentionally demonstrates bounded dungeon exploration rather than a content-complete RPG. It has no battle turn order/ATB, combat damage formulas, enemy AI, technique/spell resolution, battle animation system, victory/defeat flow, combat XP/Gold, loot table, boss combat, continuous raycasting expansion, or 3D renderer. Phase 9 and 3D work were not started.

Remaining subjective review is limited to:

- the original Prism Vault corridor depth, palette, door/chest silhouettes, movement cadence, and overall visual readability;
- the original Sunken Archive tile composition, foreground occlusion, follower spacing, and multi-floor route readability;
- the private PS1 Camineet Warehouse wall perspective, door appearance, step/turn cadence, chest/key event feel, and exit correspondence;
- the private PS2 Shure major visual topology, floor links, chute/warp placement, boxes, encounter presentation, and walking-party fidelity against Sin's preferred source captures.

**SIN MANUAL ACCEPTANCE REQUIRED**
