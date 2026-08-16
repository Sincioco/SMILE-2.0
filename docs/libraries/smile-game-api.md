# Smile.Game 1.0.0 API

`Smile.Game` is an asset-free, UI-free source package. Invalid handles and arguments fail safely. Its fixed capacities prevent allocation during update and drawing loops.

## `Smile.Game.Core`

- constants: four cardinal directions, three tile layers, Pixel/Smooth filter identifiers, and `GAME_MAX_VALUE`
- `CardinalMover`: authoritative cell, interpolation source/destination, progress/duration, facing, moving flag
- `Place`, `DirectionX`, `DirectionY`, `BeginMove`, `UpdateMove`, `CancelMove`, `VisualX`, `VisualY`

## `Smile.Game.Animation`

Capacities: 64 generation-safe animation definitions and 16 frames per definition.

- lifecycle: `Create`, `Destroy`, `IsValid`
- definition/query: `AddFrame`, `FrameCount`, `CurrentFrame`, source rectangle and anchor queries
- timing is integer, deterministic, and supports looping or one-shot clamping

## `Smile.Game.TileMap`

Capacities: 4 maps, 64×64/4,096 cells, tile IDs 0–255, region IDs 0–255, and 131,072 input bytes.

- lifecycle/parsing: `LoadMap(Text)`, `Unload`, `IsValid`
- metadata/cells: `Width`, `Height`, `CellWidth`, `CellHeight`, `TileAt`, `IsSolid`, `RegionAt`
- coordinate conversion: `CellToWorldX`, `CellToWorldY`, `WorldToCellX`, `WorldToCellY`
- tiles/rendering: `DefineTile`, `DrawLayerPixel`, `DrawLayerSmooth`

Parsing is transactional. An invalid replacement does not expose a partially populated map, and omitted Detail/Foreground layers are cleared.

## `Smile.Game.Camera2D`

- `CameraState`: offset, viewport dimensions, world dimensions
- `Configure`, `Follow`, `SmoothFollow`
- `FirstVisibleCellX/Y`, `LastVisibleCellX/Y`

Exact and smooth follow clamp at all map edges; worlds smaller than the viewport remain at offset zero.

## `Smile.Game.Collision2D`

- `OutsideMap`
- `CellsOverlap`
- `FootprintsOverlap`

The package does not choose game-specific collision layers, footboxes, physics, or pathfinding.

## Phase 8 composition note

No public API changed for Phase 8. `RpgDungeonGallery` uses `CardinalMover`, `Animation`, `TileMap`, and `Camera2D` for its four-floor top-down presentation. Its first-person cardinal projection remains application code because wall geometry and visual style are presentation policy, not reusable persistent state.
