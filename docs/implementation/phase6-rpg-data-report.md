# Phase 6 RPG data implementation report

## Shipped baseline

- Commit: `46af38dc3d076b0d5553b792b9085ad892498860`
- VSIX: 2.0.39
- Smile.RPG: 1.0.0
- Smile.UI: 1.1.3
- `.smilelib` package format: 5
- SRPG payload format: 1

Phase 6 added optional stable `ApplicationId` to the shared project model, compiler, Visual Studio project system, native output, Web output, persistence, templates, and asset-publication metadata. Explicit Console and Game identities are validated lowercase segmented ASCII values; legacy projects fall back to `OutputName`. New Visual Studio application templates generate an identity while Library templates omit it.

## Smile.RPG 1.0.0

The ordinary source package shipped exactly eight modules: Core, Characters, Party, Inventory, Equipment, Abilities, Shops, and SaveGames. It added generation-safe state handles, fixed capacities, stable caller-owned IDs, Character progress, ordered Party and Gold, Inventory stacks, transactional Equipment, learned Abilities and Magic Points, transactional Shops, and deterministic saves. It added no game-specific runtime helper, battle, enemy, quest, map, class, or status-effect system.

SaveGames shipped the little-endian SRPG version-1 field order documented in `docs/libraries/smile-rpg-save-payload.md`. Complete parsing and referential validation precede mutation, definitions remain registered, and unexpected apply failure invokes rollback. The computed legal maximum is 28,872 bytes, below the 32,768-byte codec buffer and the 1 MiB Data-block limit.

## Acceptance completed at Phase 6

- Project-reference and package-reference RPG state consumers passed on native Windows and Web with exact console parity.
- The deterministic Smile.RPG package built twice with byte-identical output and published eight source modules with no `requiresGameWindow` member.
- The RPGSystems Management option compiled and ran with DirectX, GDI, and DPR-aware Web output while composing Smile.RPG with Smile.UI MenuNavigator.
- ApplicationId validation, CLI/project agreement, persistence isolation, template generation, asset metadata, and native/Web propagation passed.
- The full smoke suite passed 216 managed checks, 7 focused formatter groups, the 170-file tracked SMILE style gate, 39 native graphics/audio checks, 38 native Text checks, and all seven normal plus seven no-demo games on native and Web.
- Visual Studio 2026 Enterprise accepted completion, Quick Info, F12, build, breakpoint, F10, and live game rendering with the installed 2.0.39 VSIX.

## Findings carried into Phase 6.1

The shipped `ApplyScratch` source already restored Equipment before saved bag Inventory, but Phase 6 lacked exact regression coverage for a stack-one equipped-plus-bag Item, a full 64-entry bag plus Equipment, multiple Characters/slots, maximum payload bytes, and rollback after a controlled partial apply. Phase 6.1 therefore treats the ordering as accepted behavior and adds the missing executable proofs.

Other findings were an OutputName-based native asset-manifest filename even for explicit ApplicationId, untracked project contexts participating in normal formatter discovery, coarse Shop invalid-state/not-sellable result codes, and the gallery's initially static Live State prose. The gallery presentation was corrected separately in commit `93b009e85556d2aa455cf1815595b6e6c02b704a` before Phase 6.1 began.
