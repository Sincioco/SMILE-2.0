# Smile.Game 2.0.0 API

`Smile.Game` is an asset-free, UI-free format-6 source package. Invalid handles and arguments fail safely. Its fixed capacities prevent allocation during update and drawing loops.

## `Smile.Game.Core`

- `CardinalDirection`: `None = 0`, `Up = 1`, `Right = 2`, `Down = 3`, and `Left = 4`
- constants: three tile layers, Pixel/Smooth filter identifiers, and `GAME_MAX_VALUE`
- stateless functions: `DirectionX(CardinalDirection)` and `DirectionY(CardinalDirection)`
- `CardinalMover`: authoritative cell, interpolation source/destination, progress/duration, typed facing, and moving flag

`CardinalMover` is a deep-copy value `Type`. Its public instance members are:

- `Place(CellX, CellY, Facing)`
- `BeginMove(Direction, Duration) As Boolean`
- `UpdateMove(Steps) As Boolean`
- `CancelMove()`
- `VisualX(CellWidth) As Number`
- `VisualY(CellHeight) As Number`

The caller preflights map and actor collision before `BeginMove`. The authoritative cell changes only when `UpdateMove` completes. `None` represents no input and is rejected as a movement direction.

## `Smile.Game.Animation`

Capacities: 64 generation-safe animation definitions and 16 frames per definition.

- lifecycle: `Create`, `Destroy`, `IsValid`
- definition/query: `AddFrame`, `FrameCount`, `CurrentFrame`, `CurrentFrameInCycle`, source rectangle, and anchor queries
- timing is integer, deterministic, and supports looping or one-shot clamping

Animation remains a bounded handle Module; it was not rewritten for the lightweight-OOP migration.

## `Smile.Game.TileMap`

Capacities: 4 maps, 64×64/4,096 cells, tile IDs 0–255, region IDs 0–255, and 131,072 input bytes.

- lifecycle/parsing: `LoadMap(Text)`, `Unload`, `IsValid`
- metadata/cells: `Width`, `Height`, `CellWidth`, `CellHeight`, `TileAt`, `IsSolid`, `CollisionAt`, `RegionAt`
- runtime editing: `SetTile` replaces one Ground/Detail/Foreground tile; `SetCollision` changes one collision category
- coordinate conversion: `CellToWorldX`, `CellToWorldY`, `WorldToCellX`, `WorldToCellY`
- tiles/rendering: `DefineTile`, `DrawLayerPixel`, `DrawLayerSmooth`

Parsing is transactional. An invalid replacement does not expose a partially populated map, and omitted Detail/Foreground layers are cleared. TileMap remains a bounded handle Module.

## `Smile.Game.Camera2D`

`CameraState` is a deep-copy value `Type` containing offset, viewport dimensions, and world dimensions. Its public instance members are:

- `Configure(ViewWidth, ViewHeight, WorldWidth, WorldHeight) As Boolean`
- `Follow(TargetX, TargetY)`
- `SmoothFollow(TargetX, TargetY, MaximumStep)`
- `FirstVisibleCellX(CellWidth, Overscan) As Number`
- `FirstVisibleCellY(CellHeight, Overscan) As Number`
- `LastVisibleCellX(CellWidth, MapWidth, Overscan) As Number`
- `LastVisibleCellY(CellHeight, MapHeight, Overscan) As Number`

Exact and smooth follow clamp at all map edges; worlds smaller than the viewport remain at offset zero.

## `Smile.Game.Collision2D`

- `OutsideMap`
- `CellsOverlap`
- `FootprintsOverlap`

Collision2D remains a stateless Module. The package does not choose game-specific collision layers, footboxes, physics, or pathfinding.

## Composition boundary

`CardinalMover` and `CameraState` are Types because they are small values with deep-copy semantics and no heap identity. Animation and TileMap retain their proven generation-safe handle engines. Collision2D remains a Module because it is stateless. `Smile.RPG` keeps its Number-based facing API and has no dependency on `Smile.Game`; composing applications use explicit direction adapters at that package boundary.
