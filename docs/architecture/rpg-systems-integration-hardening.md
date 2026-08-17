# RPGSystems Integration Hardening

## Status

Implemented after the completed lightweight-OOP hardening. This milestone changes
application composition under `games\RPGSystems`; it does not reopen the language
object model or change a public package.

## Context

`games\RPGSystems` consolidates Battle, Dungeon, Management, and World capability
galleries into one executable and one ApplicationId. Each system remains a modal
application-local Module with a public `Run()` entry point.

## Integration contracts

### Persistence

One project ApplicationId means one application persistence namespace. The
application-local `RPGSystems.Storage.SaveDomain` enum maps independently saved
systems to unique physical `Smile.RPG.SaveGames` slots:

```text
Management -> 1
Dungeon    -> 2
World      -> 3
```

Schema versions remain subsystem policy and are not storage-domain identifiers.
Future independently persisted systems must receive a new domain and join the
isolation regression. The finite public save-slot capacity is not expanded by
this application-only design.

### Initialization

Every system resets its run state, accumulates critical initialization success,
and enters its loop only when complete. Critical work includes RPG state and
definitions plus the system's required progress, UI façades, bindings, maps,
animations, images, and initial presentation state.

An ordinary bounded failure cleans partial resources and returns to the launcher.
It does not draw or update one broken loop iteration and does not terminate the
full application.

### Re-entry

Each `Run()` must work more than once in one process. Shutdown stops owned audio,
destroys UI Class façades, unloads TileMap/Animation handles and images, destroys
the RPG state, clears Text state, resets numeric handles, and assigns application
Class roots to `Nothing`.

Acceptance includes a bounded two-entry sequence for every system. Dungeon and
World are intentionally revisited before the other second entries because their
small handle pools make map/animation leaks visible quickly.

### Ownership and namespaces

Systems own only their application presentation and composition resources.
Reusable mechanics stay in Smile.Game, Smile.RPG, and Smile.UI.

Assets remain under `Assets\<System>` and maps remain under `Maps\<System>`.
Persistent data receives the complementary SaveDomain namespace.

### Language model

The completed lightweight-OOP surface stays frozen:

```text
Module = shared service, bounded engine, namespace, or intentional singleton
Type   = nominal deep-copy value
Class  = nominal reference object with identity
```

RPGSystems uses existing Enum, Type, Class, Module, Optional/named call, and
cleanup behavior. Systems remain Modules because the application needs one modal
instance at a time, not multiple overlapping identities.

## Permanent regression

`scripts\test-rpg-systems-integration.ps1` proves:

- raw same-ApplicationId/same-slot replacement and mapped-domain isolation;
- native/Web save ordering and observational Exists behavior;
- RPG-state capacity failure for all four systems;
- Menu-capacity failure and recovery for Management and World;
- zero native Class, Image, and Text live counts after the failure fixture;
- production DirectX/GDI compilation and Web syntax/runtime publication;
- source/project lifecycle and namespace contracts.

The normal smoke workflow runs the completed lightweight-OOP hardening gate and
this RPGSystems gate exactly once before the broader regression matrix.
