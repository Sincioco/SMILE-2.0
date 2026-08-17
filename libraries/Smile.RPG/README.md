# Smile.RPG 1.2.1

`Smile.RPG` is the official source-authored package for reusable RPG data,
management, bounded world state, transactional saves, encounters, and battle
state. It contains fifteen ordinary SMILE Modules and deliberately owns no
window, drawing, input, audio, asset, Smile.UI, or Smile.Game dependency.

The package manages:

- generation-safe state contexts;
- character definitions and bounded dynamic progress;
- ordered parties and shared Gold;
- item definitions and inventory stacks;
- transactional equipment and bounded bonuses;
- ability definitions, learned abilities, and exact Magic Point costs;
- shop definitions, explicit prices, limited or unlimited stock, and transactional buy/sell operations;
- scenes, spawns, transitions, persistent actor progress, story flags/values, and deterministic encounter previews;
- deterministic SRPG format-2 writes with format-1/2 reads over Phase 4 `Save Data` and `Load Data`;
- bounded renderer-neutral battle definitions, effects, strategy, state, events,
  rewards, and logical presentation cues.

Version 1.2.1 preserves the complete 1.2.0 public module/handle API while
rebuilding it with exact `Smile.RPG@1.2.1` provider identity in deterministic
`.smilelib` format 6. It introduces no public Class or Enum façade and does not
change SRPG save payload format 2. Only actors marked persistent are serialized;
transient actor progress, reservations, and active battles remain transient.

Reference `Smile.RPG.smilelibproj` during development or a deterministic `Smile.RPG.smilelib` package for distribution. Register all stable definitions before loading a save.

See [API.md](API.md) for the public surface and [../../docs/libraries/smile-rpg-save-payload.md](../../docs/libraries/smile-rpg-save-payload.md) for the binary schema.

## SMILE grammar adjustments

The conceptual API names `Party.Clear`, `Party.Count`, `SaveGames.Save`, and `SaveGames.Load` collide with SMILE statement keywords. The callable names are therefore `ClearParty`, `PartyCount`, `SaveGame`, and `LoadGame`. Because fixed arrays cannot be passed as array parameters, the in-memory codec exposes `EncodedByteCount`, `EncodedByteAt`, `ClearEncodedBytes`, and `SetEncodedByte` around `Encode` and `Decode`.
