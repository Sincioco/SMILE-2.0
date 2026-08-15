# Smile.RPG API

All IDs are caller-supplied stable positive Numbers. Invalid handles and arguments fail safely. Boolean mutations are atomic; shop and save operations return `RPG_RESULT_*` constants.

## `Smile.RPG.Core`

- `Create`, `Destroy`, `IsValid`
- `StateSlot`, `StateGeneration` for official sibling-module infrastructure
- fixed capacities, `RPG_MAX_VALUE`, item kinds, target modes, `RPG_STOCK_UNLIMITED`, and result codes

Shop callers can distinguish `RPG_RESULT_INVALID_STATE`, `RPG_RESULT_INVALID_ARGUMENT`, `RPG_RESULT_INSUFFICIENT_QUANTITY`, `RPG_RESULT_INSUFFICIENT_GOLD`, `RPG_RESULT_CAPACITY`, `RPG_RESULT_NOT_SELLABLE`, `RPG_RESULT_APPLY_FAILED`, and `RPG_RESULT_OK`.

## `Smile.RPG.Characters`

`CharacterDefinition` contains `Id`, `Name`, starting Level, Maximum Health, Maximum Magic Points, and six base statistics.

- definitions: `DefineCharacter`, `IsCharacterDefined`, `CharacterDefinitionCount`, `CharacterIdAt`, `Name`
- progress: `Level`, `Experience`, `Health`, `MaximumHealth`, `MagicPoints`, `MaximumMagicPoints`, and matching setters
- statistics: `Strength`, `Defense`, `Magic`, `Resistance`, `Agility`, `Luck`, and matching setters
- operations: `AddExperience`, `Damage`, `Heal`, `SpendMagicPoints`, `RestoreMagicPoints`, `IsAlive`, `ResetCharacter`, `ResetProgress`

## `Smile.RPG.Party`

- `AddCharacter`, `RemoveCharacter`, `MoveCharacter`, `ClearParty`
- `Contains`, `PartyCount`, `CharacterIdAt`
- `Gold`, `SetGold`, `AddGold`, `SpendGold`

## `Smile.RPG.Inventory`

`ItemDefinition` contains stable name/kind/stack/price/effect/slot metadata and eight equipment bonuses.

- definitions and field queries: `DefineItem`, `IsItemDefined`, `ItemDefinitionCount`, `ItemIdAtDefinitionIndex`, `Name`, `Kind`, `MaximumStack`, `BasePrice`, `CanSell`, `EffectId`, `EquipmentSlotId`, and bonus queries
- progress: `CanAddItem`, `AddItem`, `CanRemoveItem`, `RemoveItem`, `Quantity`, `EntryCount`, `ItemIdAtEntryIndex`, `ClearInventory`

## `Smile.RPG.Equipment`

`EquipmentSlotDefinition` contains `Id` and `Name`.

- `DefineSlot`, `IsSlotDefined`, `SlotCount`, `SlotIdAt`, `SlotName`
- `CanEquip`, `Equip`, `CanUnequip`, `Unequip`, `EquippedItem`
- `ClearCharacterEquipment`, `ClearAllEquipment`
- `HealthBonus`, `MagicPointBonus`, `StrengthBonus`, `DefenseBonus`, `MagicBonus`, `ResistanceBonus`, `AgilityBonus`, `LuckBonus`

`Equip` preflights inventory removal and replacement return before changing either component. Equipping the same item is a successful no-op. Clear operations are progress-reset infrastructure and clear assignments without moving items.

## `Smile.RPG.Abilities`

`AbilityDefinition` contains `Id`, `Name`, `MagicPointCost`, `Power`, `TargetMode`, and caller-owned `EffectId` metadata.

- definitions and fields: `DefineAbility`, `IsAbilityDefined`, `AbilityDefinitionCount`, `AbilityIdAtDefinitionIndex`, `Name`, `MagicPointCost`, `Power`, `TargetMode`, `EffectId`
- learned sets: `LearnAbility`, `ForgetAbility`, `KnowsAbility`, `LearnedAbilityCount`, `LearnedAbilityIdAt`, `ClearCharacterAbilities`, `ClearAllLearnedAbilities`
- MP transactions: `CanPayMagicPointCost`, `PayMagicPointCost`

## `Smile.RPG.Shops`

`ShopDefinition` contains `Id` and `Name`.

- definitions: `DefineShop`, `IsShopDefined`, `ShopCount`, `ShopIdAt`, `Name`
- entries: `AddShopItem`, `RemoveShopItem`, `HasShopItem`, `ShopItemCount`, `ShopItemIdAt`, `BuyPrice`, `SellPrice`, `Stock`, `ResetShopStock`
- transactions: `Buy`, `Sell`
- `SetStock` is validated SaveGames apply infrastructure

`Buy` and `Sell` return `RPG_RESULT_INVALID_STATE` for an invalid handle. `Sell` returns `RPG_RESULT_NOT_SELLABLE` for a defined Item whose `CanSell` field is False. Actual missing owned quantity or finite stock returns `RPG_RESULT_INSUFFICIENT_QUANTITY`; bounded collection or arithmetic overflow returns `RPG_RESULT_CAPACITY`. Every rejected transaction is mutation-free.

## `Smile.RPG.SaveGames`

- codec: `Encode(StateHandle, SchemaVersion)`, `Decode(StateHandle, ExpectedSchemaVersion)`
- codec buffer: `EncodedByteCount`, `EncodedByteAt`, `ClearEncodedBytes`, `SetEncodedByte`
- persistence: `SaveGame(StateHandle, SaveSlot, SchemaVersion)`, `LoadGame(StateHandle, SaveSlot, ExpectedSchemaVersion)`, `Exists`

`EncodedByteCount`, `EncodedByteAt`, and `Exists` are observational queries. `SaveGames.Exists` preserves the public codec buffer byte-for-byte, including an empty buffer, and does not change RPG state. `Encode`, `Decode`, `SaveGame`, `LoadGame`, `SetEncodedByte`, and `ClearEncodedBytes` intentionally replace or manipulate the public codec buffer as part of their documented work.

`Decode` validates the complete payload and all references before mutation. It snapshots current progress and restores that snapshot if application fails unexpectedly. During apply, Equipment assignments are restored into an empty temporary bag before saved Inventory entries, avoiding transient stack or entry-capacity requirements. Definition records remain registered.

## Phase 7 world modules

`Smile.RPG.World` adds bounded scene, spawn, transition, actor, collision-reservation, front-cell interaction, return-location, and persistent-actor progress APIs. `Smile.RPG.Story` adds 128 stable Boolean flags and 64 stable integer values. `Smile.RPG.Encounters` adds 16 deterministic zones with up to 64 weighted entries each and one pending preview encounter.

`World.ResetProgress` restores all world progress to definition defaults. `World.ResetPersistentProgress` is SaveGames infrastructure that resets persisted world fields and persistent actors without changing transient actors.

SaveGames writes SRPG format 2, reads formats 1 and 2, and transactionally includes persistent World actors, Story state, and Encounter progress. See [the world API](../../docs/libraries/smile-rpg-world-api.md) and [payload layout](../../docs/libraries/smile-rpg-save-payload.md).
