# Phase 6.2 SaveGames query-purity hardening report

Date: August 16, 2026

## Scope and repository state

Phase 6.2 began from clean main at 21e303319c1963084bf981c9b4c2a5ee3d14ae88, exactly matching origin/main. The final tested implementation is dcac9bf0a904a0134db15195432343933a4d8b23. This report is committed afterward as documentation-only evidence, so that exact implementation SHA can be recorded without a self-referential commit hash.

The work is limited to SaveGames query purity, the requested pre-Phase-7 RPG audit, regression coverage, synchronized versions, current documentation, artifact verification, and hands-on gallery acceptance. Phase 7 was not begun.

## Confirmed defect and reproduction

SaveGames.Exists was documented and used as a query, but it reset BufferCount and loaded the probed save directly into EncodedBytes. A call could therefore replace the caller-visible codec payload even though it did not change RPG state.

A public-API regression was added before changing the implementation. Against the original Smile.RPG source, Phase6RpgStateTests failed with:

    Phase 6 RPG state tests: FAIL
    903

The test uses no private hooks. It proves preservation for an existing slot, an empty slot, invalid state, slot zero, a slot above the maximum, an initially empty buffer, and 64 repeated existing-slot queries. It also checks representative complete RPG state after probing: Character health and magic points, Party order and Gold, Inventory, Equipment, learned Abilities, and Shop stock.

## Correction

SaveGames.Exists now loads the outer Data block into the module's existing private IncomingBytes scratch array and records the result in a local ProbeCount. It no longer reads or writes EncodedBytes or BufferCount.

This is the smallest behavior-preserving correction:

- the save keys, outer persistence namespace, SRPG byte layout, schema version, and return contract are unchanged;
- existing and empty slots still return the same Boolean result;
- invalid handles and invalid slot numbers still return False;
- Encode, Decode, SaveGame, LoadGame, SetEncodedByte, and ClearEncodedBytes retain their intentional codec-buffer behavior;
- private scratch reuse is safe under the current synchronous, single-threaded SMILE execution model.

SRPG remains format version 1. No migration is needed.

## Smile.RPG query audit

All eight Smile.RPG modules were read and their public query surfaces were classified.

- SaveGames.Exists: defect confirmed and corrected; now observational with respect to both codec buffer and RPG state.
- SaveGames.EncodedByteCount and EncodedByteAt: direct observational reads; no change required.
- Core, Characters, Party, Inventory, Equipment, Abilities, and Shops getters and preflight queries: observational for existing valid handles; no defect found.
- StateSlot lazy generation initialization and cleanup: intentional initialization for new or reused handle storage, not mutation of an existing handle's RPG state; no change required.
- Create, Destroy, ResetProgress, registration routines, Party/Inventory/Equipment/Ability/Shop mutations, codec mutators, and save/load operations: intentional commands rather than queries.

No second query-purity defect was found.

## Recommendation disposition

1. Explicit SaveGames.Exists query contract: APPLIED.
2. Focused externally observable regression coverage: APPLIED.
3. Remove gallery-side workaround or compensation: VERIFIED — NO CHANGE REQUIRED; none existed.
4. Document codec-buffer ownership and mutating operations: APPLIED.
5. Change payload schema or add migration: VERIFIED — NO CHANGE REQUIRED; format 1 is unchanged.
6. Add Phase 7 world or quest behavior: DEFERRED TO PHASE 7 BY DESIGN.
7. Expand persistence into Phase 7 or local-asset concerns: DEFERRED TO PHASE 7 BY DESIGN.
8. Produce a clean, tested pre-Phase-7 handoff: APPLIED.

## Automated validation

Focused project/package validation passed:

- Smile.RPG 1.0.2 packaged deterministically.
- Phase6RpgStateTests compiled and ran through the project reference on Windows x64.
- The same test compiled and ran through the packaged library on Windows x64.
- Project-reference and packaged Windows outputs were byte-identical.
- Project-reference Web output matched the exact expected console result.
- Packaged Web output matched the exact expected console result.

The repository formatter was run only on the substantively edited SMILE regression file. The repository-wide style gate then passed all 171 tracked .smile files, and git diff --check passed.

The final complete scripts/smoke-test.cmd run exited 0 in 281.9 seconds. It included:

- 217 language, compiler, project, completion, and timing tests;
- 8 formatter integration groups;
- 171-file SMILE style verification;
- 39 native graphics and audio-focus tests;
- 38 native Text tests;
- Phase 6.2 project/package Windows and Web query-purity tests;
- rollback and maximum-state RPG tests;
- DirectX, GDI, and Web RPG Management Gallery builds;
- normal and no-demo native and Web builds for all seven games;
- final artifact and VSIX verification at version 2.0.41.

The native vcxproj NU1503 warning is expected for the non-NuGet project shape. The SML3803 ApplicationId conflict output is an intentional negative test. Neither is a failure.

## Hands-on RPG Management Gallery acceptance

The disposable Slot 1 save payload for the gallery's exact ApplicationId was removed before the run so the test began from Empty. The DirectX gallery then showed live state changes for the complete management path:

- initial Gold 500, Potions 4, Bronze Swords 1, Hand None, MP 70, Shop stock 12, Slot 1 Empty;
- Add 100 Gold changed Gold to 600;
- Add Potion changed Potions to 5;
- Equip Bronze Sword changed Swords to 0 and Hand to Bronze Sword;
- Cast Fire changed MP to 63;
- Buy Potion changed Gold to 575, Potions to 6, and stock to 11;
- Save changed Slot 1 to Saved;
- a later Add 100 Gold changed Gold to 675;
- Load restored Gold 575, Potions 6, Swords 0, Hand Bronze Sword, MP 63, stock 11, and Slot 1 Saved.

The Live State panel refreshed throughout. Repeated per-frame SaveGames.Exists calls did not produce a warning, crash, stale display, state mutation, or codec interference.

## Versions, artifacts, and installation

- Smile.RPG: 1.0.2
- Visual Studio extension: 2.0.41
- Visual Studio assembly and file version: 2.0.41.0
- Smile.RPG.smilelib SHA-256: C2360370762B42068DD0E238920AD0B921E211BC91B20D28FA5B49423B818CD8
- Smile.VisualStudio.vsix SHA-256: 6E0A4C89972F9C38BF6E43297FF22FC95045433F7E4CBD94ADD8D27FA87C49E1
- Smile.VisualStudio.dll SHA-256: D3934605E3174E9411DD68AA448145F7015BE7A0465B1CA2D5F8144AB3CDF10F
- smilec.exe SHA-256: A6AEBD1A278B954D0588D76517B5B94E2E9F76D86033E8B8B0D6C7AF1BB19F74

VSIX 2.0.41 was installed into Visual Studio 2026 Enterprise instance 91f001b5 at C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\gjdxltl0.sku. The installed Smile.VisualStudio.dll is version 2.0.41.0 and matched the final built DLL byte-for-byte.

## Handoff

Phase 6.2 is complete at implementation SHA dcac9bf0a904a0134db15195432343933a4d8b23. Smile.RPG queries are audited, SaveGames.Exists is observational, Windows/Web parity is proven, the gallery is verified hands-on, current artifacts are installed, and the Phase 6 persistence format remains stable.

STOP: Phase 7 has not been started.
