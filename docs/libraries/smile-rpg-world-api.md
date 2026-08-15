# Smile.RPG 1.1.0 world API

Phase 7 adds three asset-free modules to the existing RPG data package. All IDs are stable positive Numbers and definitions must be registered before loading progress.

## `Smile.RPG.World`

Capacities: 16 scenes, 64 spawns, 64 transitions, and 64 actors. Camera and facing constants are public.

Definitions:

- `SceneDefinition`: ID, application map ID, default spawn ID, camera mode
- `SpawnDefinition`: ID, scene ID, cell, facing
- `TransitionDefinition`: ID, source scene/region, destination scene/spawn
- `ActorDefinition`: ID, starting scene/cell/facing/visibility, solid/persistent flags, application animation and interaction IDs

Definition and enumeration operations include `DefineScene`, `DefineSpawn`, `DefineTransition`, `DefineActor`, `IsSceneDefined`, `IsActorDefined`, `SceneCount`, `ActorCount`, `ActorIdAt`, `PersistentActorCount`, and `PersistentActorIdAt`.

Progress operations include current/controlled actor access, actor field queries, `ActorIsPersistent`, facing/visibility setters, destination reservation/completion/cancellation, `FrontActor`, spawn/transition activation, return-location access, `SetActorProgress`, and `ResetProgress`. `ResetPersistentProgress` is public SaveGames infrastructure that resets persisted world fields and persistent actors while leaving transient actors untouched.

## `Smile.RPG.Story`

Capacities: 128 Boolean flags and 64 integer values. `DefineFlag`, `DefineValue`, definition queries, `SetFlag`, `Flag`, `SetValue`, `Value`, deterministic count/ID enumeration, and `Reset` support first/second dialogue and simple progression without quest-specific syntax.

## `Smile.RPG.Encounters`

Capacities: 16 zones and 64 weighted entries per zone. `ZoneDefinition` supplies ID, scene/region, required steps, and initial seed. Public operations define zones/entries, locate a zone, deterministically `Advance`, access/clear a pending encounter, access/set step and seed progress, and reset progress.

An encounter ID is presentation metadata. Full battle gameplay remains outside this module and Phase 7.

## Save integration

`SaveGames` writes SRPG format 2 and reads formats 1–2. Format 2 stores only actors whose definition has `Persistent = True`, plus story and encounter progress. Transient actor position, visibility, facing, and route progress survive load unchanged. Complete preflight validation and cross-module rollback preserve transactional load behavior.
