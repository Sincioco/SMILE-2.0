# Phase 7 top-down RPG world architecture

Phase 7 keeps presentation, reusable movement mechanics, and RPG state separate:

```text
RpgWorldGallery
  -> Smile.UI 2.0.0       Menu/MenuNavigator/Dialogue Class presentation
  -> Smile.Game 2.0.0     typed value movement/camera, animation, maps, collision
  -> Smile.RPG 1.1.1      characters, party, shops, world/story/encounters/save
```

Neither source package opens a window, owns assets, or depends on `Smile.UI`. Applications select art, audio, controls, scene music, UI layout, dialogue text, map files, and encounter presentation.

## Movement and camera

`Smile.Game.Core.CardinalMover` keeps a cell-authoritative source and destination. A caller preflights map and actor collision, reserves the destination in `Smile.RPG.World`, then starts fixed-step integer interpolation with `Mover.BeginMove(...)`. The authoritative cell changes only when `Mover.UpdateMove(...)` completes. Cancelling with `Mover.CancelMove()` preserves the source cell. `CardinalDirection` provides exact nominal direction values; application adapters preserve Smile.RPG's independent Number-based facing boundary.

`CameraState` operates in caller-selected integer world units through `Camera.Configure(...)`, `Camera.Follow(...)`, and the visible-cell instance functions. Exact follow centers and clamps in one operation; smooth follow moves by a bounded step. A map smaller than its viewport produces offset zero. Visible-cell helpers clamp at map edges and accept explicit overscan.

## Maps and collision

`TileMap` transactionally parses `SMILE-MAP 1` into four generation-safe slots. Width and height are bounded to 64, total cells to 4,096, tile and region IDs to 255, and source bytes to 131,072. Ground is required. Detail and Foreground are optional zero-filled layers. Collision and Regions are required.

Map collision is cell-authoritative. `World.TryReserveDestination` adds solid visible actor occupancy and reservation checks, preventing two actors from entering the same cell. `Collision2D` supplies generic map and rectangular-footprint predicates; an application remains responsible for choosing its footbox and map collision data.

Phase 7.1 applies the same final-state rule to every world placement path: two different visible solid actors cannot occupy the same scene/cell, and a new visible-solid placement cannot invalidate another visible solid actor's active destination reservation. Hidden and non-solid overlaps remain legal. Failed definitions, reveals, spawns, transitions, progress replacements, and persistent resets are atomic.

## Scenes, actors, story, and encounters

`Smile.RPG.World` defines immutable scenes, spawns, transitions, and actors. Transitions validate that their destination spawn belongs to their destination scene. Actor progress includes scene, cell, facing, visibility, route step, and a persistence flag. Front-cell targeting supports menu-initiated Talk without embedding an interaction language.

`Story` is a bounded stable-ID Boolean/integer store. `Encounters` holds deterministic zone step/seed progress, weighted preview entries, and one pending encounter. It deliberately contains no attack, damage, reward, victory, or defeat rules.

## Persistence

SRPG format 2 retains the format-1 prefix and appends current/return location, persistent actor progress, story flags/values, and encounter progress. The decoder accepts formats 1 and 2. It validates the complete final persistent layout against itself and preserved transient actors/reservations before mutation. Persistent actors are hidden, placed as one batch, and then restored to final visibility, so valid swaps do not fail on intermediate old cells. Transient actor progress and reservations survive successful loads, while unexpected apply failure restores every RPG/world/story/encounter field and active reservation.

The maximum format-2 payload is 32,436 bytes in a 36,864-byte package buffer, still below the Phase 4 one-MiB Data envelope.
