# Smile.RPG 1.1.0

`Smile.RPG` is the official source-authored package for reusable RPG data, management, and bounded top-down world state. It contains eleven ordinary SMILE modules and deliberately owns no window, drawing, input, audio, asset, battle-resolution, quest-language, class, or status-effect behavior.

The package manages:

- generation-safe state contexts;
- character definitions and bounded dynamic progress;
- ordered parties and shared Gold;
- item definitions and inventory stacks;
- transactional equipment and bounded bonuses;
- ability definitions, learned abilities, and exact Magic Point costs;
- shop definitions, explicit prices, limited or unlimited stock, and transactional buy/sell operations;
- scenes, spawns, transitions, persistent actor progress, story flags/values, and deterministic encounter previews;
- deterministic SRPG format-2 writes with format-1/2 reads over Phase 4 `Save Data` and `Load Data`.

Version 1.1.0 preserves the Phase 6 APIs and adds `World`, `Story`, and `Encounters`. Save/load remains transactional across the combined state; only actors marked persistent are serialized.

Reference `Smile.RPG.smilelibproj` during development or a deterministic `Smile.RPG.smilelib` package for distribution. Register all stable definitions before loading a save.

See [API.md](API.md) for the public surface and [../../docs/libraries/smile-rpg-save-payload.md](../../docs/libraries/smile-rpg-save-payload.md) for the binary schema.

## SMILE grammar adjustments

The conceptual API names `Party.Clear`, `Party.Count`, `SaveGames.Save`, and `SaveGames.Load` collide with SMILE statement keywords. The callable names are therefore `ClearParty`, `PartyCount`, `SaveGame`, and `LoadGame`. Because fixed arrays cannot be passed as array parameters, the in-memory codec exposes `EncodedByteCount`, `EncodedByteAt`, `ClearEncodedBytes`, and `SetEncodedByte` around `Encode` and `Decode`.
