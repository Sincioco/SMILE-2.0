# Phase 6 RPG data and management

Phase 6 adds optional project `ApplicationId` identity and the ordinary SMILE-authored `Smile.RPG` 1.0.0 package. It adds no language keyword, RPG runtime API, or second compiler/Visual Studio language model.

## Application identity

Console and Game projects may declare one `ApplicationId`. It is 3 through 128 lowercase ASCII characters in two or more dot-separated segments. Every segment begins with a letter and contains only lowercase letters, digits, or non-trailing hyphens. Library projects cannot own application identity.

`EffectiveApplicationId` is the explicit value when present and otherwise falls back exactly to `OutputName`, preserving existing projects. The compiler accepts `--application-id`; it must match an explicit project value. Native/Web persistence and asset publication use the effective identity.

New Visual Studio Console and Game projects receive `smile.app.a` followed by 32 lowercase hexadecimal digits. Library templates omit the property.

## Smile.RPG

The eight package modules are Core, Characters, Party, Inventory, Equipment, Abilities, Shops, and SaveGames. All capacities are fixed, IDs are stable caller data, handles are generation-safe, and cross-component mutations preflight before commit. Static definition metadata stays registered while dynamic progress may be reset or loaded.

The [RPG Management Gallery](../../examples/RpgManagementGallery/README.md) composes Smile.RPG with Smile.UI's reusable MenuNavigator across DirectX, GDI, and Web output. Battle systems, enemies, quests, classes, status effects, skill trees, migrations, cloud saves, mouse, and touch remain deferred.
