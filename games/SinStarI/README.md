# Sin Star I

Sin Star I is a SMILE 2.0 game shell with a custom title scene, two explorable
town previews, a four-character animation gallery, a bounded Battle Arena Preview,
and placeholder Shop and Dungeon scenes.

## Architecture

`Program.smile` deliberately owns the game window, main loop, scene transitions,
and thin scene routing. `SinStarI.TitleScreen` remains an application-local Module:
there is one title-screen service with private assets and selection state, so a
Class would add identity without a useful second instance.

The module exports the typed `TitleAction` enum. Its explicit values preserve the
existing scene contract: `None=0`, `Character=1`, `Town=2`, `Town2=3`, `Shop=4`,
`Dungeon=5`, and `Battle=6`. Navigation uses explicit enum transitions rather
than enum arithmetic.

## Controls

- Title: Up/Down or W/S selects an item; Enter or Space opens it.
- Every scene: Escape returns to the title and restarts title music.
- Character: 1-4 or Tab selects the manual preview; arrows/WASD move; Space
  toggles its walk/run sheet.
- Town: arrows/WASD move; 1 and 2 switch the visible character.
- Town 2: arrows/WASD take manual control; hold Space to run as Character 1;
  Enter starts or pauses the edge tour; 1 and 2 switch characters.
- Battle Arena Preview: click `Floor Reflections: On / Off` or press R to toggle
  the shared polished floor; left-drag pans, middle-drag orbits, the wheel zooms,
  O toggles the default smooth auto-orbit, and Escape returns to the title.

Title music plays only while the title is active. Opening a scene stops it, and
returning with Escape restarts it. Screen images and scene resources are released
by their owning application-local Modules during shutdown.

## Battle Arena Preview

`SinStarI.BattleArenaPreview` replaces only the former Battle placeholder. It owns a
small shared `Arena3D`, the existing screen-fixed title backdrop, one live animated
and socket-grounded Arin v5.7 actor, smooth shared camera controls, a visible
reflection preference, and bounded lifecycle. Both
native and Web builds use the same `Smile.Simple3D.Graphics3D` planar-reflection
implementation as the Character Viewer. The game imports no Viewer module and owns no
GPU reflection algorithm, calibration editor, battle authority, damage model, or scene
schema. Leaving the preview destroys its actor, arena, and backdrop; re-entry creates a
fresh scene with reflections requested On.

## Content

The project publishes its accepted PNG, MP3, and `.smilemap` content through the
existing recursive asset rules. `CONTENT_PIPELINES.md` records the reusable town
and character authoring workflows; visual asset revision remains a separate,
reviewed art task.

## Paladin Combat Presentation Lab

`PaladinCombatLab.smileproj` is a separate technical presentation program for the
candidate Arin v5.4 character. It automatically cooks the repository-owned GLB
and descriptor, enumerates the model's exact clips, events, and sockets, and
exercises Ready, Run, Sword Attack, Shield Bash, Defend, Block Impact, Hit, KO,
and Victory without implementing battle authority. Authored events align VFX,
transient light, hit-stop, and caller-owned audio cues; they never authorize
damage. The candidate remains separate from Sin Star I release content until the
production texture, provenance, visual-acceptance, and explicit user gates pass.
