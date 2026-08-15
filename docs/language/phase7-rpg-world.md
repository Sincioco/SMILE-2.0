# Phase 7 RPG world language notes

Phase 7 is primarily a source-library milestone. It adds no map, camera, actor, dialogue, shop, or encounter keywords. These systems are ordinary modules, records, routines, arrays, and project assets.

Two small general language changes support the libraries:

- `Load Text File` now accepts any `Text` expression for the path, not only a literal. The path is still resolved executable-relative, bounded by the destination array, and subject to the existing missing-file, BOM, truncation, and zero-fill behavior.
- The existing `Game` keyword is accepted as an interior segment of a dotted module name, enabling `Smile.Game.Core`. `Game Window` remains the only statement use; an unqualified identifier named `Game` remains reserved.

Typical imports are:

```text
Import Smile.Game.Core As GameCore
Import Smile.Game.TileMap As TileMap
Import Smile.Game.Camera2D As Camera2D
Import Smile.RPG.World As World
Import Smile.RPG.Story As Story
Import Smile.RPG.Encounters As Encounters
```

Maps are declared as ordinary project assets. Applications load them through `TileMap.LoadMap(PathText)`, define tiles against an application-owned `Image`, and draw only visible layers. See [SMILE-MAP 1](../formats/smile-map-format-1.md) and the [Phase 7 architecture](../architecture/phase7-top-down-rpg-world.md).
