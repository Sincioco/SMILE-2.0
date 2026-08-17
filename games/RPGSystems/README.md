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

## Adding another RPG system

1. Add an application-local module named `RPGSystems.<Name>System` with a public
   `Run()` subroutine.
2. Keep that system's images/audio under `Assets/<Name>` and maps under
   `Maps/<Name>` so filenames cannot collide with other systems.
3. Load state and resources inside `Run()`, intercept Escape to end that run, and
   destroy/unload all owned state and resources before returning.
4. Add the source to `RPGSystems.smileproj` and one launcher option to
   `Program.smile`.

Reusable mechanics remain in Smile.Game, Smile.RPG, and Smile.UI. This project
owns only the application composition and presentation used to demonstrate them.
