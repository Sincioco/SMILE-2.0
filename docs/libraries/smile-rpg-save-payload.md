# Smile.RPG save payload

`Smile.RPG.SaveGames` places a deterministic bounded SRPG version-1 payload inside the existing Phase 4 SMD4 persistent-data envelope. SMD4 continues to provide SHA-256 integrity, atomic native replacement, and versioned Web storage; SRPG does not duplicate cryptography.

Every numeric field is a signed 32-bit little-endian value constrained to the shared safe RPG range. The only negative value is `RPG_STOCK_UNLIMITED = -1`.

Field order:

1. four bytes `SRPG`, format version, and positive game schema version;
2. character count, followed by Character ID, Level, Experience, Health, Maximum Health, Magic Points, Maximum Magic Points, Strength, Defense, Magic, Resistance, Agility, and Luck;
3. party count and ordered Character IDs, then Gold;
4. inventory count, then Item ID and Quantity;
5. equipment count, then Character ID, Equipment Slot ID, and Item ID;
6. learned-ability count, then Character ID and Ability ID;
7. shop-stock count, then Shop ID, Item ID, and Current Stock.

The maximum Phase 6 payload is 28,872 bytes, below the package's 32,768-byte codec buffer and the 1 MiB Phase 4 Data limit. The decoder rejects bad magic/version/schema, truncation, trailing bytes, negative or excessive counts, duplicate keys, unknown definitions, incoherent equipment, invalid ranges, and capacity overflow.

The 28,872-byte value is exercised by an executable maximum-state test containing all 32 Characters, 64 bag entries, 512 Equipment assignments, 1,024 learned Abilities, and 1,024 Shop-stock entries. Phase 6.1 keeps the binary field order and format version unchanged. Decode resets progress, restores Character values and Party/Gold, restores Equipment through the normal transactional Equip operation while the bag is empty, then restores saved bag Inventory, learned Abilities, and Shop stock. This order avoids a temporary 65th Inventory entry or a temporary extra copy in a full stack. An apply failure restores the exact pre-load snapshot.

Save slots 1 through 8 map to literal keys `Smile.RPG.Save.1` through `Smile.RPG.Save.8`. The application's EffectiveApplicationId provides the outer native and Web persistence namespace.
