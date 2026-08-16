# Phase 8 RPG dungeon systems

Phase 8 proves dungeon exploration by composing the Phase 7/7.1 architecture. It adds no dungeon-specific public API, scene graph, movement stack, actor store, inventory, or save format. The pre-implementation decision record is [the Phase 8 gap matrix](phase8-rpg-dungeon-gap-matrix.md).

## Shared state, separate presentation

Both gallery modes treat each floor as a stable `Smile.RPG.World` scene. Spawns define exact floor, cell, and facing endpoints. `World` transitions join top-down region cells; the first-person application applies the same registered spawns after completed cardinal steps. The controlled hero remains the authoritative actor in either presentation, so Phase 7.1 visible-solid occupancy, destination reservations, and `CurrentScene`/`ControlledActor` coherence continue to apply.

The first-person Prism Vault stores only a bounded 9-by-9 traversability grid in the application. It derives visible forward and side cells from the authoritative World actor and projects walls, doors, chests, hidden walls, and an NPC with ordinary 2D drawing. Turning updates facing but does not enter a cell or trigger an event.

The top-down Sunken Archive loads four SMILE-MAP 1 floors. It uses `CardinalMover` for fixed-step interpolation, `TileMap` for collision/regions/layers, `Camera2D` for clamped following, `Animation` for deterministic directional walk frames, and World reservations for actor collision. Ground and detail render before actors; the foreground layer renders afterward for occlusion. The party follower is presentation derived from the same party and mover state rather than a second actor system.

## Topology

The Prism Vault has three floors with reciprocal B1/B2 and B2/B3 stair endpoints, a one-way B2-to-B3 chute, a B3-to-B1 warp, and an entrance that exits into the top-down archive entrance.

The Sunken Archive has four floors. Its main B1 stair reaches the lower half of B2, where a visible-solid locked door controls the only passage through a separating wall. An alternate reciprocal B1/B2 stair reaches the upper half. That route reaches the B3 dead-end item/key branch, allowing the retained key to open the B2 branch. B2 also has a one-way chute to B4; B3 has a warp to B1; normal B2/B3 and B3/B4 links remain reciprocal. The command menu returns either presentation from an upper floor to the known top-down entrance.

## Event composition

| Event | Existing state authority | Application policy |
| --- | --- | --- |
| Door | persistent visible-solid World actor | hide actor and set Story flag after interaction |
| Locked door | World, Inventory, Story | reject without key; retain key; expose the traversable base cell |
| Chest/reward | World, Inventory, Party, Story | preflight capacity, grant Gold/item once, hide actor |
| Trap | Characters, Story | mutate only after completed entry; flag one-shot or increment repeat count |
| Hidden passage | World, Story | present as wall until interaction hides the persistent blocker |
| Stairs/chute/warp | World scene/spawn/transition | reciprocal or explicitly one-way application topology |
| NPC dialogue | World interaction, Story, Inventory, Smile.UI | multi-page first dialogue and flag/item-aware repeat text |
| Encounter preview | Encounters, World return location | deterministic counter/selection, preview only, exact return |
| Escape | World spawn | place the hero at the stable entrance; preserve dungeon progress |

## Persistence and phase boundary

SRPG format 2 already stores the exact required Character, Party/Gold, Inventory, current scene, controlled actor, return location, persistent actor, Story, and Encounter progress. It restores the complete state transactionally under the Phase 7.1 final-layout checks. Format-1 reads remain supported and reset later world state to registered defaults. SMILE-MAP remains format 1 and `.smilelib` remains format 5.

Encounter Preview contains no attacks, damage formula, initiative, enemy AI, technique resolution, battle animation, victory/defeat, combat reward, loot table, or boss logic. Phase 9 and 3D work were not started.
