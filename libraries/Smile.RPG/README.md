# Smile.RPG 1.0.1

`Smile.RPG` is the official source-authored Phase 6 package for reusable RPG data and management. It contains eight ordinary SMILE modules and deliberately owns no window, drawing, input, audio, asset, battle, enemy, quest, class, or status-effect behavior.

The package manages:

- generation-safe state contexts;
- character definitions and bounded dynamic progress;
- ordered parties and shared Gold;
- item definitions and inventory stacks;
- transactional equipment and bounded bonuses;
- ability definitions, learned abilities, and exact Magic Point costs;
- shop definitions, explicit prices, limited or unlimited stock, and transactional buy/sell operations;
- deterministic SRPG version-1 save payloads over Phase 4 `Save Data` and `Load Data`.

Version 1.0.1 adds exact save/load capacity and rollback proofs, keeps Equipment restoration ahead of bag Inventory restoration, and distinguishes invalid-state and not-sellable Shop results. The SRPG binary layout remains version 1.

Reference `Smile.RPG.smilelibproj` during development or a deterministic `Smile.RPG.smilelib` package for distribution. Register all stable definitions before loading a save.

See [API.md](API.md) for the public surface and [../../docs/libraries/smile-rpg-save-payload.md](../../docs/libraries/smile-rpg-save-payload.md) for the binary schema.

## SMILE grammar adjustments

The conceptual API names `Party.Clear`, `Party.Count`, `SaveGames.Save`, and `SaveGames.Load` collide with SMILE statement keywords. The callable names are therefore `ClearParty`, `PartyCount`, `SaveGame`, and `LoadGame`. Because fixed arrays cannot be passed as array parameters, the in-memory codec exposes `EncodedByteCount`, `EncodedByteAt`, `ClearEncodedBytes`, and `SetEncodedByte` around `Encode` and `Decode`.
