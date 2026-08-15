# Smile.RPG API

All IDs are caller-supplied stable positive Numbers. Invalid handles and arguments fail safely. Boolean mutations are atomic; shop and save operations return `RPG_RESULT_*` constants.

## `Smile.RPG.Core`

- `Create`, `Destroy`, `IsValid`
- `StateSlot`, `StateGeneration` for official sibling-module infrastructure
- fixed capacities, `RPG_MAX_VALUE`, item kinds, target modes, `RPG_STOCK_UNLIMITED`, and result codes

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

## `Smile.RPG.SaveGames`

- codec: `Encode(StateHandle, SchemaVersion)`, `Decode(StateHandle, ExpectedSchemaVersion)`
- codec buffer: `EncodedByteCount`, `EncodedByteAt`, `ClearEncodedBytes`, `SetEncodedByte`
- persistence: `SaveGame(StateHandle, SaveSlot, SchemaVersion)`, `LoadGame(StateHandle, SaveSlot, ExpectedSchemaVersion)`, `Exists`

`Decode` validates the complete payload and all references before mutation. It snapshots current progress and restores that snapshot if application fails unexpectedly. Definition records remain registered.
