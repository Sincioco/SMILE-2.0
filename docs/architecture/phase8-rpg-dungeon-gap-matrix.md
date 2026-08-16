# Phase 8 RPG dungeon gap matrix

This matrix was completed before Phase 8 production code changed. It compares the required dungeon behaviours with the reviewed Phase 7/7.1 packages and the two original `DungeonStar` presentations.

| Required behaviour | Existing authority | Decision |
|---|---|---|
| Floors, entrances, stairs, chutes, warps, and exact return | `Smile.RPG.World` scenes, spawns, transitions, current/controlled actor, and return location | Compose existing APIs. A dungeon floor is a scene and every vertical or spatial link has a stable spawn. |
| Closed, open, and locked doors | Persistent visible-solid World actors plus `Story` flags and `Inventory` key items | Compose existing APIs. Door map cells remain traversable; hiding the actor opens the passage and persists through SRPG 2. |
| Chests and one-time rewards | Persistent World actors, `Inventory.CanAddItem`, `Inventory.AddItem`, `Party.AddGold`, and Story flags | Compose existing APIs. Preflight capacity, grant once, set the flag, then hide the actor. |
| One-shot and repeating traps | Completed cell entry, Story flags/values, and `Characters.Damage` | Compose existing APIs. Trap policy stays in the application and never runs from turning or rendering. |
| Hidden passages | Persistent visible-solid World actors and Story flags | Compose existing APIs. The concealed wall presentation is application-owned; revealing it hides the blocking actor. |
| NPC and state-aware dialogue | World interaction IDs, Story flags/values, and `Smile.UI.Dialogue` | Compose existing APIs. Dialogue presentation and narrative text remain application-owned. |
| Encounter preview and exact return | `Smile.RPG.Encounters` plus World return location | Compose existing APIs. Phase 8 shows bounded preview only and adds no battle resolver. |
| Save/load inside any dungeon floor | Deterministic SRPG 2 writer/reader, persistent actors, World, Story, Inventory, Characters, Party, and Encounters | Compose existing APIs. No save-format field is missing. |
| Top-down multi-floor rendering and collision | SMILE-MAP 1, `TileMap`, `Camera2D`, `CardinalMover`, and World reservations | Compose existing APIs. Load at most four maps, matching the existing package bound. |
| Cardinal first-person navigation and rendering | Direction helpers in `Smile.Game.Core`; original pseudo-3D quadrilateral presentation in `DungeonStarI` | Keep presentation in the gallery. Relative-cell projection, palette, and wall geometry are not persistent RPG state and do not justify a second renderer or new public module. |
| Commercial PS1/PS2 validation | Private local projects outside Git | Extend only the private harnesses. No commercial asset, map, audio, screenshot, or extracted data enters public history. |

## Result

No generic API gap is proven. The Phase 8.1 audit likewise found application workflow defects rather than a reusable package-level absence. Transaction orchestration therefore remains in the application-local `RpgDungeonGallery.Workflow` module shared verbatim with focused tests; no `Smile.RPG.Dungeons` API or generic transaction framework was added.

Phase 8.1 keeps `Smile.Game` at 1.0.0, `Smile.RPG` at 1.1.1, SMILE-MAP writes/reads at 1, SRPG writes at 2 with reads at 1/2, and `.smilelib` at format 5. The repository had already advanced the VSIX to 2.0.46 for an unrelated Visual Studio fix before this pass; Phase 8.1 preserves that actual version and changes no shipped VSIX payload.

Explicitly deferred: pathfinding, a second actor/graph/inventory/persistence system, quest DSLs, raycasting or general 3D expansion, and all Phase 9 battle resolution.
