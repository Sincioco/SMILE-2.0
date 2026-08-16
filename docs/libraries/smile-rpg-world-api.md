# Smile.RPG 1.1.1 world API

Phase 7 adds three asset-free modules to the existing RPG data package. All IDs are stable positive Numbers and definitions must be registered before loading progress.

## `Smile.RPG.World`

Capacities: 16 scenes, 64 spawns, 64 transitions, and 64 actors. Camera and facing constants are public.

Definitions:

- `SceneDefinition`: ID, application map ID, default spawn ID, camera mode
- `SpawnDefinition`: ID, scene ID, cell, facing
- `TransitionDefinition`: ID, source scene/region, destination scene/spawn
- `ActorDefinition`: ID, starting scene/cell/facing/visibility, solid/persistent flags, application animation and interaction IDs

Definition and enumeration operations include `DefineScene`, `DefineSpawn`, `DefineTransition`, `DefineActor`, `IsSceneDefined`, `IsActorDefined`, `SceneCount`, `ActorCount`, `ActorIdAt`, `PersistentActorCount`, and `PersistentActorIdAt`.

Progress operations include current/controlled actor access, actor field queries, `ActorIsPersistent`, facing/visibility setters, destination reservation/completion/cancellation, `FrontActor`, spawn/transition activation, return-location access, `SetActorProgress`, and `ResetProgress`. `ActorHasReservation`, `ActorReservedDestinationX`, and `ActorReservedDestinationY` are observational queries used by applications and transactional save infrastructure. A missing reservation returns False and coordinates `-1`.

Two different actors that are both visible and solid cannot occupy the same cell in the same scene. A visible-solid definition, reveal, spawn, transition, or direct progress replacement also cannot occupy another visible solid actor's active reserved destination. Rejected operations are mutation-free; hidden and non-solid overlaps remain legal. Registered starting definitions obey the same rule, so `ResetProgress` cannot manufacture an overlap. `ResetPersistentProgress` is public SaveGames infrastructure that atomically resets persisted world fields and persistent actors while leaving transient actors untouched, and rejects a persistent default that would conflict with preserved transient collision state.

When `CurrentScene` and `ControlledActor` are both nonzero, the controlled actor belongs to that scene. Zero remains legal for setup and clearing. `ApplySpawn` maintains this rule and a blocked spawn or transition preserves the complete prior actor, scene, and reservation state.

## `Smile.RPG.Story`

Capacities: 128 Boolean flags and 64 integer values. `DefineFlag`, `DefineValue`, definition queries, `SetFlag`, `Flag`, `SetValue`, `Value`, deterministic count/ID enumeration, and `Reset` support first/second dialogue and simple progression without quest-specific syntax.

## `Smile.RPG.Encounters`

Capacities: 16 zones and 64 weighted entries per zone. `ZoneDefinition` supplies ID, scene/region, required steps, and initial seed. Public operations define zones/entries, locate a zone, deterministically `Advance`, access/clear a pending encounter, access/set step and seed progress, and reset progress.

An encounter ID is presentation metadata. Full battle gameplay remains outside this module and Phase 7.

## Phase 8 dungeon mapping

No dungeon module was needed. A floor maps to a Scene, an entrance or vertical endpoint maps to a Spawn, region-based stairs/chutes/warps map to Transitions, and doors/chests/hidden walls/NPCs map to Actors with application-owned interaction IDs. Story holds one-time and conditional event state; Inventory and Party hold item and Gold rewards; Encounters holds deterministic dungeon-zone progress. First-person and top-down views consume these same records.

## Save integration

`SaveGames` writes SRPG format 2 and reads formats 1–2. Format 2 stores only actors whose definition has `Persistent = True`, plus story and encounter progress. The decoder validates the incoming final persistent layout against pairwise overlap, preserved transient visible-solid cells, transient reservations, and current-scene coherence before mutation. Persistent actors are hidden and placed as a batch before final visibility is restored, allowing legal swaps and rearrangements. Transient actor progress and reservations survive load unchanged. Complete preflight validation and cross-module rollback, including active actor reservations, preserve transactional load behavior.
