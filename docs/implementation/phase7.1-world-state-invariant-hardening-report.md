# Phase 7.1 World-State Invariant Hardening Report

Phase 7.1 began from commit `b615543a5fa5ac136b6eaa8314d3d0659200a5b7` and is implemented by commit `fcfa91ade47e06628fef3a8dfae9ce987ea715da`. It hardens the existing Phase 7 world model and save boundary without starting another major feature phase.

## Confirmed invariant defects and pre-fix proof

The Phase 7 implementation did not enforce one invariant consistently across every public mutation path: a visible, solid actor must not share a scene cell with another visible, solid actor's current cell or active reserved destination. Definition-time starting positions also needed an equivalent independent uniqueness rule so a later reset could not recreate an overlap after an actor had moved.

The gaps were confirmed in `DefineActor`, `SetActorVisible`, `ApplySpawn`, `ActivateTransition`, `SetActorProgress`, persistent reset, and format-2 save restore. The original save application order also made a valid persistent actor swap impossible and did not preflight the decoded final layout against preserved transient actors or reservations. `CurrentScene` and `ControlledActor` could be assigned into a contradictory pair.

The final 1,089-check fixture was compiled once against a copied pre-fix `Smile.RPG` package. It failed 48 newly added assertions and ended with `Phase 7 world state tests: FAIL`. The failing check identifiers were 279-281, 288-289, 296-297, 304-305, 322-327, 343-345, 362-363, 370-373, 383-384, 410-413, 427-428, 435-436, 438-441, 893-896, 909-910, 1064-1065, and 1080-1081. This establishes that the regressions detect the starting implementation rather than merely describing the repaired behavior.

## Final world-state rules

- A visible, solid actor owns its current scene cell. An active destination reservation by that actor also owns the reserved cell for conflict checks.
- A mutation that would create two visible-solid claims in one scene fails before changing world state. Hidden actors, non-solid actors, and actors in different scenes may share coordinates.
- Actor starting definitions are checked independently from live progress. Moving the original actor does not make an overlapping starting definition legal.
- `DefineActor` rejects conflicting current placement, conflicting starting placement, and conflicting active reservations without leaving a phantom actor.
- `SetActorVisible` preflights a reveal against current cells and reservations. Hiding an actor clears its active reservation.
- `ApplySpawn` preflights the destination scene/cell, reservations, and controlled/current-scene coherence before applying any fields.
- `ActivateTransition` inherits the atomic `ApplySpawn` behavior. A blocked destination leaves scene, actor progress, facing, control, and return state unchanged.
- `SetActorProgress` preflights visible-solid placement, reservations, and controlled/current-scene coherence before applying the direct progress update.
- `TryReserveDestination` continues to reject solid current cells and active reservations. `ActorHasReservation`, `ActorReservedDestinationX`, and `ActorReservedDestinationY` provide read-only inspection without exposing mutable arrays.
- `ResetProgress` restores all actor defaults only from a definition set that was validated when built. `ResetPersistentProgress` additionally preflights persistent defaults against preserved transient current cells and reservations before changing anything.
- When `CurrentScene` and `ControlledActor` are both nonzero, the controlled actor must exist in the current scene. `SetCurrentScene`, `SetControlledActor`, spawn/progress mutation, and save restore all enforce that rule. Zero remains the explicit unassigned value for either field.

## SaveGames final-layout restore

Format-2 decoding now validates the complete proposed world before mutation. The scratch layout rejects persistent-versus-persistent visible-solid overlap, persistent-versus-transient current-cell conflict, persistent-versus-transient reservation conflict, and an incoherent final current-scene/controlled-actor pair.

A valid persistent actor swap or rearrangement is applied as one batch: persistent actors are hidden, their final hidden progress is placed, and their final visibility is restored only after every decoded record has passed preflight. This avoids false rejection from sequential application while preserving the invariant at every observable boundary.

The existing transactional rollback remains authoritative for any apply-time failure. The rollback snapshot now includes active actor reservations, and the private fault-injection fixture proves that the entire RPG state and reservation data are restored byte-for-byte in both native and Web execution.

Format-1 saves remain readable. Because they contain no Phase 7 world records, their compatibility path resets persistent world progress through the hardened reset operation. Writes remain deterministic SRPG format 2, reads accept formats 1 and 2, SMILE-MAP remains version 1, and `.smilelib` remains deterministic format 5. The maximum save fixture remains 32,436 bytes within the 36,864-byte package buffer and existing one-MiB SMD4 envelope.

## Versions

- `Smile.Game`: 1.0.0
- `Smile.RPG`: 1.1.1
- Visual Studio extension: 2.0.43 (`2.0.43.0` assembly/file version)
- SMILE-MAP: 1
- SRPG: writes 2; reads 1 and 2
- `.smilelib`: 5

## Automated validation

- The focused fixture passes 1,089 checks as a Windows project-reference consumer, Windows package-reference consumer, Web project-reference consumer, and Web package-reference consumer. Native project/package output and Web project/package console output are exact matches.
- The private rollback fault-injection fixture passes on native and Web with exact output parity and proves reservation-preserving rollback.
- `Smile.Game` and `Smile.RPG` deterministic package rebuild comparisons pass.
- The focused formatter check passes for all four substantively changed `.smile` files, and the complete style gate passes all 181 tracked `.smile` files.
- The complete repository smoke suite passes in 241.5 seconds: 220 managed language/compiler/project/completion/timing checks, eight formatter integration groups, 39 native graphics/audio-focus checks, 38 native Text checks, the full Phase 2-7 native/Web matrix, all seven game demo/no-demo native and Web builds, native x64 GUI verification, asset verification, and final VSIX verification.

## Gallery and Visual Studio acceptance

The RPGSystems World option builds with the same 12 declared assets on DirectX, GDI, and Web. DirectX manual acceptance covered title/new game, town walking, building and NPC collision, first and repeated dialogue, shop entry and exit, an atomic purchase, inventory and character statistics, visible party add/remove, stat mutation, town/overworld transitions, deterministic Encounter Preview, exact encounter return, save, world/party/stat mutation, transactional load restoration, and town re-entry. GDI was manually exercised through title, new game, town walking, and the field menu. Web was manually exercised through the equivalent title/start/walk/menu path and produced no browser warnings or errors.

VSIX 2.0.43 was installed into Visual Studio 2026 Enterprise instance `91f001b5` at `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\sgadibbd.ois`. The installed `Smile.VisualStudio.dll` is version 2.0.43.0, has SHA-256 `88719B956BF18B08E9284502EEC09D038E920991CCEA652A587961E1737AB8C6`, and matched the built payload. Visual Studio passed Web and native builds and launches, showed `ActorHasReservation` in `World.` completion, navigated by F12 to its source definition, displayed ordinary symbol Quick Info, bound and hit a native `.smile` breakpoint, and advanced between `.smile` statements with F10.

## Known limitations and phase boundary

The hardening does not add pathfinding, full battle resolution, combat rewards, a dungeon phase, a quest DSL, networking, physics, or 3D. No Phantasy Star I/II asset, map, audio, or capability-demo work was performed or committed in Phase 7.1. Those remain explicitly outside this invariant-hardening phase.
