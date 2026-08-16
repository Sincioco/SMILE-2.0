# Smile.RPG save payload

`Smile.RPG.SaveGames` places a deterministic bounded SRPG payload inside the existing Phase 4 SMD4 persistent-data envelope. It writes format 2 and reads formats 1 and 2. SMD4 continues to provide SHA-256 integrity, atomic native replacement, and versioned Web storage; SRPG does not duplicate cryptography.

Every numeric field is a signed 32-bit little-endian value constrained to the shared safe RPG range. The only negative value is `RPG_STOCK_UNLIMITED = -1`.

Field order:

1. four bytes `SRPG`, format version, and positive game schema version;
2. character count, followed by Character ID, Level, Experience, Health, Maximum Health, Magic Points, Maximum Magic Points, Strength, Defense, Magic, Resistance, Agility, and Luck;
3. party count and ordered Character IDs, then Gold;
4. inventory count, then Item ID and Quantity;
5. equipment count, then Character ID, Equipment Slot ID, and Item ID;
6. learned-ability count, then Character ID and Ability ID;
7. shop-stock count, then Shop ID, Item ID, and Current Stock;
8. in format 2, Current Scene, Controlled Actor, Return Scene/X/Y/Facing;
9. persistent-actor count, then Actor ID, Scene ID, Cell X/Y, Facing, Visible, and Route Step;
10. story-flag count, then Flag ID and Boolean value;
11. story-value count, then Value ID and integer value;
12. Pending Encounter ID and encounter-zone count, then Zone ID, Step Count, and Seed.

The maximum Phase 7 format-2 payload is 32,436 bytes, below the package's 36,864-byte codec buffer and the 1 MiB Phase 4 Data limit. The decoder rejects bad magic/version/schema, truncation, trailing bytes, negative or excessive counts, duplicate keys, nonpersistent actor records, unknown or mismatched definitions, incoherent equipment, invalid ranges, capacity overflow, overlapping incoming visible-solid actors, conflicts with preserved transient visible-solid actors or reservations, and incoherent nonzero current-scene/controlled-actor pairs.

The 32,436-byte value is exercised by an executable maximum-state test containing the full Phase 6 definition/progress capacities plus 16 scenes, 64 spawns/transitions/persistent actors, 128 flags, 64 values, and 16 encounter zones. Format-2 Decode preflights the final world layout, hides persistent actors, places their final scene/cell/facing/route values as a batch, and then restores final visibility. It restores Character values and Party/Gold, restores Equipment through the normal transactional Equip operation while the bag is empty, then restores saved Inventory, learned Abilities, Shop stock, Story, and Encounter progress. This order avoids false actor-swap collisions, a temporary 65th Inventory entry, or an extra copy in a full stack. Any apply failure restores the exact pre-load snapshot across all modules and active reservations.

Save slots 1 through 8 map to literal keys `Smile.RPG.Save.1` through `Smile.RPG.Save.8`. The application's EffectiveApplicationId provides the outer native and Web persistence namespace. A format-1 load restores its Phase 6 prefix and resets Phase 7 progress to registered defaults. `SaveGames.Exists` probes the outer Data block through private scratch storage and preserves the public codec buffer and RPG state.

Phase 8 requires no new payload field. Dungeon floor/cell/facing and entrance return use existing World fields; opened doors/chests and discovered passages use persistent actors plus Story flags; trap/NPC conditions use Story; keys/items/Gold use Inventory and Party; and encounter counters/pending preview use Encounters. Writes therefore remain SRPG 2, reads remain SRPG 1 and 2, and the existing maximum-payload and transactional rollback evidence remains authoritative.
