# Phase 6 RPG data and management

Phase 6 adds optional project `ApplicationId` identity and the ordinary SMILE-authored `Smile.RPG` package. Phase 6.1 advances the package to 1.0.1 without adding a language keyword, RPG runtime API, or second compiler/Visual Studio language model.

## Application identity

Console and Game projects may declare one `ApplicationId`. It is 3 through 128 lowercase ASCII characters in two or more dot-separated segments. Every segment begins with a letter and contains only lowercase letters, digits, or non-trailing hyphens. Library projects cannot own application identity.

`EffectiveApplicationId` is the explicit value when present and otherwise falls back exactly to `OutputName`, preserving existing projects. The compiler accepts `--application-id`; it must match an explicit project value. Native/Web persistence and asset publication use the effective identity. Legacy projects without an explicit identity retain `<OutputName>.smile-assets.json` for native publication. Explicit identities use `<SafeApplicationId>.smile-assets.json`, so an OutputName rename keeps one stable manifest. A validated matching 2.0.39 OutputName manifest is migrated once; malformed, mismatched, or unsafe manifests are left untouched.

New Visual Studio Console and Game projects receive `smile.app.a` followed by 32 lowercase hexadecimal digits. Library templates omit the property.

## Smile.RPG

The eight package modules are Core, Characters, Party, Inventory, Equipment, Abilities, Shops, and SaveGames. All capacities are fixed, IDs are stable caller data, handles are generation-safe, and cross-component mutations preflight before commit. Static definition metadata stays registered while dynamic progress may be reset or loaded.

Smile.RPG 1.0.1 proves stack-one equipped-plus-bag saves, full 64-entry Inventory with Equipment, multiple Characters and slots, exact rollback after a private test-only apply fault, the 28,872-byte maximum payload, deterministic Encode/Decode/Encode bytes, persistent SaveGame/LoadGame, project/package consumers, and native/Web parity. Shop results now distinguish invalid state and not-sellable Items. SRPG remains format version 1.

The [RPG Management Gallery](../../examples/RpgManagementGallery/README.md) composes Smile.RPG with Smile.UI's reusable MenuNavigator across DirectX, GDI, and Web output. Battle systems, enemies, quests, classes, status effects, skill trees, migrations, cloud saves, mouse, and touch remain deferred.
