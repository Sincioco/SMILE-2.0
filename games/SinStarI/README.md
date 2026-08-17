# Sin Star I

Sin Star I is a SMILE 2.0 game shell with a custom title scene, two explorable
town previews, a four-character animation gallery, and placeholder Shop,
Dungeon, and Battle scenes.

## Architecture

`Program.smile` deliberately owns the game window, main loop, scene transitions,
and placeholder flow. `SinStarI.TitleScreen` remains an application-local Module:
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

Title music plays only while the title is active. Opening a scene stops it, and
returning with Escape restarts it. Screen images and scene resources are released
by their owning application-local Modules during shutdown.

## Content

The project publishes its accepted PNG, MP3, and `.smilemap` content through the
existing recursive asset rules. `CONTENT_PIPELINES.md` records the reusable town
and character authoring workflows; visual asset revision remains a separate,
reviewed art task.
