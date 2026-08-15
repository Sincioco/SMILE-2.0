# Phase 6.1 RPG hardening implementation report

## Scope and versions

Phase 6.1 starts after the reviewed Phase 6 commit `46af38dc3d076b0d5553b792b9085ad892498860` and the gallery live-state fix `93b009e85556d2aa455cf1815595b6e6c02b704a`. It advances Smile.RPG to 1.0.1 and the VSIX to 2.0.40 while keeping Smile.UI 1.1.3, `.smilelib` format 5, and SRPG payload format 1. Phase 7 is not included.

## RPG state and save hardening

The baseline SaveGames implementation already applies Equipment before saved bag Inventory. Phase 6.1 preserves that order and proves it with executable project-reference and package-reference cases covering:

- a MaximumStack-one Item equipped while another copy remains in the bag;
- all 64 Inventory entries occupied plus Equipment not present in the bag;
- multiple Characters equipped with the same Item definition;
- multiple Equipment Slots and bag stacks already at their maximum;
- byte-identical Encode, Decode, and second Encode;
- SaveGame, mutation, LoadGame, and exact state restoration;
- native and Web parity.

A separate test build copies the eight production modules, inserts one private fault after incoming Character, Party, Gold, and Equipment application, and exposes no production or public hook. Decode returns `RPG_RESULT_APPLY_FAILED`; rollback then restores every Character field, Party order, Gold, 64 Inventory entries, four Equipment assignments, learned Abilities, finite/unlimited Shop stock, and all registered definitions.

The generated legal maximum state contains 32 Characters, 8 Party members, 64 Inventory entries, 512 Equipment assignments, 1,024 learned Abilities, and 1,024 Shop-stock entries. Its tested encoded size is exactly 28,872 bytes, below `RPG_MAX_SAVE_BYTES = 32,768` and `DATA_BLOCK_MAX_BYTES = 1 MiB`; Decode succeeds and the second Encode is byte-identical.

## Application identity and publication

Legacy projects without explicit ApplicationId retain native `<OutputName>.smile-assets.json`. Explicit ApplicationId selects stable `<SafeApplicationId>.smile-assets.json`; Web remains `smile-assets.json`. Native publication scans only same-directory manifest candidates, validates format, exact application identity, `windows-x64` target, canonical unique managed paths, and output containment, and then uses matching 2.0.39 manifests for stale cleanup. Validated matching legacy manifests are removed only after successful stable publication. Mismatched manifests and malformed or unsafe manifests are left in place, and unrelated files remain untouched.

## Formatter determinism

Formatter discovery now keeps separately sorted tracked and untracked project paths. Default and explicit tracked-source runs use tracked project contexts only. `-IncludeUntracked` deliberately appends eligible untracked owners after tracked owners. Explicit untracked `-Files` may use an untracked project that actually owns the source. Multiple owners use ordinal repository-relative ordering, and focused temporary-Git tests prove isolation, deliberate widening, deterministic precedence, read-only Check behavior, and byte idempotence.

## Shop API precision

Smile.RPG 1.0.1 adds `RPG_RESULT_NOT_SELLABLE`. Buy and Sell return `RPG_RESULT_INVALID_STATE` for an invalid handle, actual stock or owned-quantity shortages remain `RPG_RESULT_INSUFFICIENT_QUANTITY`, and bounded collection or arithmetic overflow returns `RPG_RESULT_CAPACITY`. Exact result tests verify invalid arguments, undefined entries, stock, Gold, Inventory capacity, not-sellable Items, owned quantity, overflow, success, and mutation-free failures. The gallery maps the precise failure classes to clearer status text.

## Final artifacts and validation

The deterministic final packages are:

- `artifacts\libraries\Smile.RPG.smilelib`: SHA-256 `56AD3760C193CD25EF8220630FE518833FEDD57E4C4FAAC853AC5FA1236AB8D3`;
- `artifacts\vsix\Smile.VisualStudio.vsix`: SHA-256 `B76765E95DB60E414E19F27118F92E7D459789DE0BE00CCF5ADB4C8F451CD4CA`.

VSIX 2.0.40 was installed into Visual Studio 2026 Enterprise instance `91f001b5` at `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\jivh4vdr.vy4`. Every installed payload matched the final build byte-for-byte:

- `Smile.VisualStudio.dll`: SHA-256 `AEE372D67B7E56DEFE61AF9E5FC84616E0D3B04D33523FEEAD0C57841D5FBEA5`;
- `Smile.Language.dll`: SHA-256 `E1D4A415FA752FCDAAC62B3A9525CBDF4EA680570B9737601C780B47A66681C1`;
- `Compiler\smilec.exe`: SHA-256 `554F64EE2C506576BE3854A96BB05E17D4CC35328E9D9E1143B27189F5D25FBC`;
- `Compiler\Smile.Language.dll`: SHA-256 `021BB53F45333EC949C7FE0D4B1471D55E14CE7BD80B8AA7034691B0ABC7255C`;
- `Compiler\Smile.NativeRuntime.lib`: SHA-256 `37FB120660A1D558AF7246DE94202388FE43C6718BA721948565E39B947838AF`.

The final permanent smoke suite passed 217 managed language/compiler/project/completion/timing tests, 8 focused formatter groups, the 170-file style gate, 39 native graphics/audio-focus checks, 38 native Text checks, exact project/package native/Web RPG parity, the private native/Web rollback-fault proof, and every normal plus no-demo build for all seven games on native and Web. The explicit ApplicationId OutputName-rename fixture removed a stale managed asset, migrated the matching safe legacy manifest, preserved mismatched/malformed manifests and an unrelated sentinel, and reported `SML3605` for the malformed candidate.

Hands-on gallery acceptance passed on DirectX, GDI, and Web. DirectX visibly saved an equipped Bronze Sword plus a second MaximumStack-one copy in the bag, removed the bag copy, and loaded back exactly `Swords: 1` with `Hand: Bronze Sword`. GDI and Web each changed Live State Gold from 500 to 600. The Web canvas ran at device-pixel ratio 2 with a 1,200-by-750 backing store for a 600-by-375 CSS canvas and no browser warning or error.

Visual Studio 2026 Enterprise passed project build, RPG member completion, Quick Info identifying built-in `Smile.RPG@1.0.1`, F12 navigation to the source `Core.Create` definition, a bound and hit breakpoint, F10 stepping, a runtime `ReturnValue = 0` data tip, and debug launch of the gallery.

Known limitations remain the intentional Phase 6 boundary: no maps, movement, collision, NPCs, quests, battles, status effects, class/job systems, migrations, cloud saves, mouse, or touch.
