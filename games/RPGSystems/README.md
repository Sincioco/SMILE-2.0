# RPG Systems

`RPGSystems` is the consolidated, extensible SMILE 2.0 RPG capability gallery.
It replaces the former standalone Battle, Dungeon, Management, and World gallery
projects with one executable and one launcher.

## Launcher

- Battle demonstrates formations, battle commands, effects, strategy, rewards,
  presentation cues, and exploration return flow.
- Dungeon demonstrates cardinal first-person and top-down exploration, doors,
  keys, treasure, traps, transitions, dialogue, saves, and encounters.
- Management demonstrates party, inventory, equipment, abilities, shops, and
  save/load state through reusable Smile.UI menus.
- World demonstrates map movement, collision, actors, dialogue, shops, story,
  encounters, followers, and world persistence.

Use arrows or WASD to select a system and Enter or Space to open it. Escape from
any system returns directly to the RPG Systems launcher. Escape on the launcher
closes the application.

## Application-local system contract

Battle, Dungeon, Management, and World remain modal application-local Modules.
Each public `Run()` owns one complete system visit and must:

1. reset its module globals to a deterministic baseline;
2. create its transient RPG state and load only its own assets/resources;
3. validate every critical definition, progress mutation, UI façade, map,
   animation, and initial presentation dependency cumulatively;
4. enter its interactive loop only after complete initialization;
5. treat Escape as return to this launcher, never as a normal `End Program`;
6. stop owned music and SFX, destroy UI façades, unload maps/animations/images,
   destroy the RPG state, and clear Class references before returning.

An ordinary capacity or resource failure fails closed, cleans every partial
acquisition, and returns to the launcher. A second same-process entry is a
required acceptance check; no system may depend on a process restart.

System assets stay under `Assets/<System>` and maps stay under `Maps/<System>`.
The launcher owns no direct knowledge of those resources.

## Persistence domains

The project has one ApplicationId, so independently persisted systems use the
nominal `RPGSystems.Storage.SaveDomain` mapping:

| Logical domain | Physical Smile.RPG slot | Schema owner |
| --- | ---: | --- |
| Management | 1 | Management |
| Dungeon | 2 | Dungeon |
| World | 3 | World |

The physical slot is an implementation detail. Schema versions remain separate
subsystem policy. Battle does not currently own a persisted domain.

## Adding another RPG system

1. Add an application-local module named `RPGSystems.<Name>System` with a public
   `Run()` subroutine.
2. Keep that system's images/audio under `Assets/<Name>` and maps under
   `Maps/<Name>` so filenames cannot collide with other systems.
3. Follow the all-or-nothing `Run()` lifecycle above and prove a clean second
   entry in the same process.
4. If the system persists independently, add one unique `SaveDomain`, physical
   slot mapping, and isolation assertion. Never reuse an existing domain slot.
5. Add the source to `RPGSystems.smileproj`, one launcher option to
   `Program.smile`, and focused integration acceptance.

Reusable mechanics remain in Smile.Game, Smile.RPG, and Smile.UI. This project
owns only the application composition and presentation used to demonstrate them.

Run `scripts\test-rpg-systems-integration.ps1` after `scripts\build.cmd` for the
non-destructive persistence, bounded initialization, lifetime, DirectX/GDI
compilation, and Web parity gate. Hands-on acceptance uses this one-process order:

```text
Battle -> Dungeon -> Management -> World -> Dungeon -> World -> Management -> Battle
```
