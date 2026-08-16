# Phase 8 RPG dungeon composition

Phase 8 is a capability proof rather than a language or library expansion. The permanent `examples\RpgDungeonGallery` project composes `Smile.Game` 1.0.0, `Smile.RPG` 1.1.1, and `Smile.UI` 1.1.3 through their existing project references. The same sources may instead consume the built packages; `examples\Phase8DungeonStateTests` verifies both dependency paths.

## State model

A dungeon floor is an ordinary `World` scene. Stable spawns connect entrances, stairs, chutes, warps, encounters, and the exact scene/cell/facing used when returning. Doors, chests, hidden walls, traps, and NPCs use World actors and interaction identifiers. Durable outcomes use `Story` flags/values, while rewards use `Inventory` and `Party`; trap damage uses `Characters`. Encounter preview uses `Encounters` and deliberately stops before battle resolution.

This model is presentation-independent. The first-person Prism Vault projects nearby cardinal grid cells with ordinary SMILE drawing statements. The top-down Sunken Archive loads four SMILE-MAP 1 files and uses `TileMap`, `Camera2D`, `CardinalMover`, and World collision/reservation rules. Both presentations use the same RPG save transaction.

## Persistence and formats

No format change was required. SMILE-MAP remains version 1. SaveGames writes SRPG version 2 and reads versions 1 and 2. Persistent World actors record opened doors and chests or discovered passages; Story, Inventory, Characters, Party, Encounters, current scene, controlled actor, position, and facing remain part of the same transaction. `.smilelib` remains format 5.

## Scope boundary

Phase 8 permits encounter preview and dungeon-specific visual composition. It does not add turn order, damage formulas, enemy AI, technique resolution, battle animations, victory or defeat resolution, combat rewards, loot tables, or bosses. Those remain Phase 9 work.
