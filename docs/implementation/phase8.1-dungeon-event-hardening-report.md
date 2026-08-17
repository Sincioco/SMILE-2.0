# Phase 8.1 dungeon event hardening report

## Closure status

All Phase 8.1 P0 items passed. Focused native/Web tests passed, the complete smoke suite passed, independent artifact verification passed, tracked commits were pushed, and the final worktree was clean. Phase 9 was not started during this pass.

- Starting SHA: `abcfc22c2ea4bea3e1ba983b65970e9132974c07`
- Implementation SHA: `9092fea205e66673d56d436d4d01a5c69086f731`
- Documentation/evidence SHA: the commit containing this report; its exact SHA is recorded in the completion handoff
- Branch: `main`
- Push result: `origin/main` accepted the Phase 8.1 implementation and documentation commits
- Final worktree state: clean and synchronized with `origin/main`

The controlling handoff named the older reviewed SHA `7ae9697792a50c4044e7b8aa8309ed0de28c766a`. Repository verification found the legitimate later `abcfc22` Visual Studio 2.0.46 fix at both `HEAD` and `origin/main`, so that later clean commit is the actual Phase 8.1 baseline.

## Confirmed defects

The baseline gallery composed valid library primitives but left multi-step event mutations open to partial commit, let some UI text claim success without authoritative results, accepted Escape/Interact before the top-down movement lock, used an overwrite-prone initialization expression, trusted persistent actor projection after load, duplicated workflow behavior between presentation and tests, and tested local map shapes without proving a complete progression.

Live DirectX acceptance also exposed two concrete integration defects: encounter-zone advancement set pending state before the gallery attempted a second pending write, and first-person chute spawn 113 landed on the static wall at `(6,6)`. The shared workflow now owns advancement/begin as one transaction, and spawn 113 lands on traversable `(7,6)`.

## Pre-fix failing tests

An explicit red audit ran against `abcfc22` and produced 16 expected failures: absent shared workflow; unprotected ordinary-door, locked-door, Gold-chest, key-chest, multi-item-chest, hidden-passage, one-shot-trap, repeat-trap, and encounter-begin commit sequences; unconditional transition and spawn/escape success; load without app-semantic validation/projection; Escape/Interact before movement lock; overwrite-prone initialization; and topology without complete progression-state search.

The final executable regression source would fail those same baseline behaviors and now passes 121 checks through both project and package dependency paths on native Windows and Web. The topology model now passes 92 complete-progression checks.

## Shared production/test workflow structure

`games/RPGSystems/DungeonWorkflow.smile` defines `RPGSystems.DungeonWorkflow` and is compiled directly by the consolidated gallery, `Phase8DungeonStateTests.smileproj`, and `Phase8DungeonStateTests.Package.smileproj`. It is application-local and depends only on the existing Smile.RPG API. No public library module, compiler/runtime helper, persistence field, or fault-injection hook was introduced.

`scripts/test-phase8-dungeon-workflow-rollback.ps1` creates a GUID-named disposable directory under `artifacts/temp`, copies the real shared workflow and focused test program, instruments five private post-mutation failure points, executes native and Web parity tests, and removes the verified temporary directory.

## Event result contract

The local result values are `OK` (0), `ALREADY_COMPLETED` (1), `MISSING_REQUIREMENT` (2), `CAPACITY` (3), `BLOCKED` (4), `INVALID_STATE` (5), `APPLY_FAILED` (6), `NOT_FOUND` (7), and `WRONG_SCHEMA` (8). Gallery messages branch on those authoritative results; generic failure mapping never reports success.

### Door transaction result

Ordinary doors preflight valid state/actor/flag, treat Story completion as idempotent authority, hide the actor, set the flag, validate the projection, and restore visibility/flag if a later step fails.

### Locked-door transaction result

Locked doors return `MISSING_REQUIREMENT` without mutation when the key is absent, retain the key on success, use the same atomic barrier commit, and return `ALREADY_COMPLETED` from Story state on repeats.

### Gold chest transaction result

Gold capacity is preflighted. The transaction captures prior Gold, Story flag, and visibility; reward, flag, and projection either all commit or all restore. Repeats return `ALREADY_COMPLETED` without another reward.

### Key chest transaction result

Inventory capacity is preflighted and Story completion is checked before reward. Quantity, flag, and visibility roll back together after an injected later failure; a completed chest cannot duplicate the key.

### Multi-item chest transaction result

Both item outcomes are preflighted as one unit. Existing key quantity is preserved, a missing key and tonic commit together, and any failure after the first add restores both quantities, flag, and actor projection.

### Hidden-passage transaction result

The hidden passage uses the same atomic/idempotent barrier workflow as an ordinary door. Story is durable authority and actor visibility is only its projected collision/presentation state.

### One-shot trap result

Damage and the spent flag commit together. Repeat entry returns `ALREADY_COMPLETED`; injected flag failure restores health and leaves the trap unspent.

### Repeat-trap result

One entry applies one bounded damage mutation and one counter increment. Failure to update the counter restores health and the previous Story value.

### NPC dialogue result

Initial NPC Story completion commits only if the Dialogue UI actually starts. Failed start leaves the NPC unspoken; repeats are idempotent and select altered dialogue from durable Story/inventory state.

### Encounter begin/return result

Zone progress, deterministic seed, exact return scene/cell/facing, pending encounter, preview transition, and return form one result-aware workflow. Begin failure restores earlier return/pending state. Return restores the exact actor location before clearing pending state and rolls back if the resulting dungeon state is invalid.

## Transition/spawn/escape result

Transitions and spawns validate identifiers and resulting gallery state. Presentation synchronization and success text occur only after the authoritative operation succeeds. Escape records whether the source was first-person before applying the known top-down entrance spawn, so its message is accurate after the scene changes.

## Save/load status result

Save accepts only a coherent gallery state and maps RPG result codes locally. Load retains the library's transactional schema/codec behavior, then projects canonical Story flags onto ten event actors and validates app semantics. A wrong schema, missing slot, or invalid semantic state receives a truthful failure and does not remain applied.

## Movement-input policy

Top-down movement uses a six-step visual interpolation while World owns one reserved destination. `FieldCommandsAllowed` rejects field Escape and Interact throughout that interval. Movement completion either commits the reserved cell and processes arrival or cancels/resynchronizes without claiming movement success.

## Initialization gating

`InitializationSucceeded` starts true and can only transition to false. Every character/item/story/encounter/scene/spawn/transition/actor definition, every loaded map/tile definition, every animation and frame, every menu item, and every required UI handle contributes to the latch. Start/presentation and the game loop run only while the accumulated result remains true.

## Load-time event-state coherence

Story flags are canonical for five first-person and five top-down door/chest/hidden actors. Load reconciles all ten projections before validation. The focused suite deliberately saves a mismatched visibility projection and proves successful repair, then saves an app-invalid controlled actor and proves rejection plus rollback to the pre-load state.

## Top-down topology result

The validator retains format, dimensions, section, collision, actor, endpoint, partition, foreground, and dead-end assertions, then explores state tuples containing floor, cell, key ownership, and opened-event bits. One legal route reaches all four floors, approaches every event, traverses all ten transition sources/destinations, completes every required event, and returns to the exit. Result: 92 combined topology checks passed.

## First-person topology result

The validator models the exact three 9-by-9 collision grids, closed/open actor blocking, key gating, six stairs/chute/warp edges, every transition endpoint, and every event approach. One legal route reaches all floors, completes all five persistent route actors while retaining the key, and returns to the B1 exit. The corrected chute destination `(7,6)` is explicitly traversable.

## DirectX full walkthrough

A real Release DirectX run completed both presentations. The walkthrough exercised ordinary/locked doors, key retention, Gold/key/multi-item chests, hidden passages, first/repeat NPC dialogue, one-shot and repeat traps, two encounter previews with exact return, in-dungeon save/load, reciprocal stairs, chute, warp, escape, and return to the title. It found and verified the chute and encounter fixes above; process survival was not used as the acceptance claim.

## GDI representative walkthrough

A real Release GDI run exercised first-person movement/turning/collision and an ordinary door, then top-down party rendering, interpolated collision movement, and an ordinary door. Rendering and interaction remained coherent without DirectX-specific behavior.

## Web DPR-2 representative walkthrough

The generated gallery ran in a real browser with device-pixel-ratio 2: a 1280-by-720 backing canvas over a 640-by-360 CSS canvas. First-person movement/turning/door interaction and top-down movement/collision/door interaction were exercised. The page error surface remained hidden and empty, and browser warning/error logs were empty.

## Private PS1 regression

Before Sin's later instruction pausing all PS1/PS2 work, the already-private native project was rebuilt and its Camineet Warehouse path was exercised through entry, movement, ordinary-door interaction, guide-gated key-chest rejection, and transactional in-dungeon save/load. No private file was added to Git or public output. No further PS1 work occurred after the pause.

## Private PS2 regression

Before the pause, the already-private native project was rebuilt and Shure was exercised through entry with the trailing party visible, collision movement, ordinary-door interaction, and transactional in-dungeon save/load. No private Web output or tracked/public file was produced. No further PS2 work occurred after the pause.

## Copyright-safety audit

The SHA-256 boundary audit compared 65 canonical private source/reference files (62 unique hashes) against 3,632 public repository/artifact files and every entry in 52 public VSIX/ZIP/NuGet archives. It found zero raw-file matches and zero archive-entry matches. It also found zero private Web directories. Commercial/reference material remained outside Git and public artifacts.

## Focused test counts

- Project-reference native: 121 checks, pass.
- Package-reference native: 121 checks, pass with exact project output parity.
- Project-reference Web: exact native console parity, pass.
- Package-reference Web: exact native/package console parity, pass.
- Complete top-down/first-person topology: 92 checks, pass.

## Injected-failure native/Web results

The disposable copy injected failure after actor hiding, after Gold mutation, after the first multi-item mutation, after trap damage, and after encounter return-location mutation. All five paths restored exact pre-event state in native and Web executions, with exact console parity. No injection symbol exists in production source.

## Full smoke counts and duration

`cmd /c scripts\smoke-test.cmd` passed in 261.42 seconds. It completed 228 managed language/compiler/project/completion/timing tests, 8 formatter integration tests, the 183-file style gate, 39 native graphics/audio-focus checks, 38 native Text checks, all RPG rollback/package/native/Web matrices, the Phase 8.1 121-check project/package native/Web matrix, five-point injected rollback parity, 92 topology checks, all seven native/Web game demo and no-demo builds, native x64 GUI inspection, asset-byte checks, Web viewport/DPR checks, and final VSIX payload/version verification.

## Artifact verification

Independent `verify-artifacts.ps1` execution passed. It verified every required native x64 GUI, copied game assets, compiler/shared-language/project-template VSIX payload, synchronized VSIX 2.0.46 identity/assembly/file/product versions, seven required viewport sizes, and DPI calculations at 100, 125, 150, and 200 percent. The final formatter check and `git diff --check` also passed.

## Versions and formats

- Smile.Game: 1.0.0
- Smile.RPG: 1.1.1
- Smile.UI: 1.1.3
- VSIX: 2.0.46 (`2.0.46.0` assembly/file version), preserved from the actual baseline
- SMILE-MAP: writes/reads format 1
- SRPG: writes format 2; reads formats 1 and 2
- `.smilelib`: format 5

No compiler, runtime, library source, public package API, serialized field, or installed VSIX payload changed in Phase 8.1.

## Known limitations

The gallery remains a bounded dungeon capability proof. Encounter Preview intentionally has no turn order, actions, damage formula, enemy AI, battle rewards, victory, or defeat. Its corridor renderer is an application-owned cardinal projection rather than raycasting or 3D. Subjective pacing, visual, audio, and private source-fidelity comparisons remain Sin's manual acceptance surface.

## Phase boundary

Phase 9 was not started. No battle system, 3D renderer, or Phase 9/10 feature was added during Phase 8.1.
