# Phase 7 top-down RPG world architecture

Phase 7 keeps presentation, reusable movement mechanics, and RPG state separate:

```text
RpgWorldGallery
  -> Smile.UI 1.1.3       menus and dialogue presentation
  -> Smile.Game 1.0.0     movement, animation, maps, camera, collision
  -> Smile.RPG 1.1.0      characters, party, shops, world/story/encounters/save
```

Neither source package opens a window, owns assets, or depends on `Smile.UI`. Applications select art, audio, controls, scene music, UI layout, dialogue text, map files, and encounter presentation.

## Movement and camera

`Smile.Game.Core.CardinalMover` keeps a cell-authoritative source and destination. A caller preflights map and actor collision, reserves the destination in `Smile.RPG.World`, then starts fixed-step integer interpolation. The authoritative cell changes only when interpolation completes. Cancelling preserves the source cell.

`Camera2D` operates in caller-selected integer world units. Exact follow centers and clamps in one operation; smooth follow moves by a bounded step. A map smaller than its viewport produces offset zero. Visible-cell helpers clamp at map edges and accept explicit overscan.

## Maps and collision

`TileMap` transactionally parses `SMILE-MAP 1` into four generation-safe slots. Width and height are bounded to 64, total cells to 4,096, tile and region IDs to 255, and source bytes to 131,072. Ground is required. Detail and Foreground are optional zero-filled layers. Collision and Regions are required.

Map collision is cell-authoritative. `World.TryReserveDestination` adds solid visible actor occupancy and reservation checks, preventing two actors from entering the same cell. `Collision2D` supplies generic map and rectangular-footprint predicates; an application remains responsible for choosing its footbox and map collision data.

## Scenes, actors, story, and encounters

`Smile.RPG.World` defines immutable scenes, spawns, transitions, and actors. Transitions validate that their destination spawn belongs to their destination scene. Actor progress includes scene, cell, facing, visibility, route step, and a persistence flag. Front-cell targeting supports menu-initiated Talk without embedding an interaction language.

`Story` is a bounded stable-ID Boolean/integer store. `Encounters` holds deterministic zone step/seed progress, weighted preview entries, and one pending encounter. It deliberately contains no attack, damage, reward, victory, or defeat rules.

## Persistence

SRPG format 2 retains the format-1 prefix and appends current/return location, persistent actor progress, story flags/values, and encounter progress. The decoder accepts formats 1 and 2. It parses and cross-validates the complete payload before mutation, preserves transient actor progress during both successful apply and rollback, and restores every persisted RPG/world/story/encounter field if apply fails.

The maximum format-2 payload is 32,436 bytes in a 36,864-byte package buffer, still below the Phase 4 one-MiB Data envelope.
