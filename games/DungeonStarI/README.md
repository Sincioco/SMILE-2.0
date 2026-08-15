# Dungeon Star I

Dungeon Star I is an original first-person, grid-based dungeon exploration game written entirely in SMILE 2.0 source. It demonstrates student-editable external maps, random pipe-style generation, closed and open doors, reciprocal stairs across three floors, palette-swapped pseudo-3D rendering, time-based movement and transitions, a self-playing attract mode, user-idle handling, and MP3 background music.

`Program.smile` is the normal demo-enabled source. `Program-NoDemo.smile` is the complete playable student edition with the attract state, route planner, timers, cancellation, and demo UI removed. To make the student edition the Visual Studio startup source, change `<StartupFile>` in `DungeonStarI.smileproj` to `Program-NoDemo.smile`.

## Build

From the repository root:

```text
artifacts\compiler\smilec.exe games\DungeonStarI\Program.smile -o artifacts\games\DungeonStarI\DungeonStarI.exe
```

Copy both `games\DungeonStarI\Assets` and `games\DungeonStarI\Maps` beside the executable. Visual Studio builds the `.smileproj` and copies both declared wildcard asset trees automatically.

## Controls

- Up or W: move forward.
- Down or S: move backward.
- Left or A: turn left.
- Right or D: turn right.
- Enter or Space: open the closed door directly ahead.
- Escape: return from player exploration to the title; Escape again exits.
- Alt+Enter: toggle true borderless full screen and windowed mode.

## Title map selection

The blue title screen initially selects `Default.MAP`. Up/Down or W/S chooses among `Default.MAP`, `SAMPLE-LOOPS.MAP`, `SAMPLE-SWITCHBACKS.MAP`, and `Random DUNGEON`; Enter/Space starts the selection. Any title activity restarts the five-second attract timer. The automatic demo uses the currently selected entry, and any demo key returns to the title.

The three external files under `Maps` are plain text. The generic `Load Text File` statement reads their executable-relative UTF-8 bytes into a bounded numeric array; all header, row, symbol, topology, start, door, stair, and connectivity interpretation remains in `Program.smile`. See `MAP_AUTHORING.md` for the complete format and student workflow.

If a selected file is missing or invalid, the game displays `MAP Not AVAILABLE - Random DUNGEON USED` briefly and remains playable with an internally generated dungeon. The selected entry is retained so the file can be fixed and retried. No hidden copy of `default.map` is embedded in the executable.

## Floors and generation

Every map has three 31-by-31 floors. Random generation carves a connected coarse graph around coordinates 5, 15, and 25, producing one-cell-wide corridors, long straight runs, loops, and spaced turns or intersections without rooms or 2-by-2 open blocks. Doors are placed inside straight edge interiors, the start has immediate side walls, reciprocal stairs are reachable, retries are bounded, and failure falls back to a deterministic pipe grid instead of hanging.

Loaded and generated maps share the same runtime validator for closed borders, complete connectivity, one-cell topology, start facing, exact reciprocal stairs, door orientation and spacing, and feature separation.

Floor 1 uses an emerald palette, floor 2 sapphire, and floor 3 crimson. The first-person view is drawn entirely with SMILE quadrilaterals, rectangles, lines, text, and numbers; there are no textures or game-specific native helpers.

The game is deliberately about exploration only: it has no combat, items, character statistics, score, or minimap. Its original runtime-drawn geometry works through both the DirectX and GDI graphics backends and requires no image assets.

## Demo and idle timing

After five seconds without title-menu activity, the game immediately begins a 60-second self-playing demo of the selected map with no countdown. Any demo key returns to the title and is consumed.

During player exploration there is no warning before 30 seconds of inactivity. A flashing `9` through `0` warning runs from 30 through 39 seconds, and the game returns to the title at 40 seconds. Any key or held movement control resets that timer before the deadline check.

## Music

The exact repository-owner-supplied `Assets\Background.mp3` loops only while the demo or player is traversing the dungeon. The title is silent, and every route back to the title stops music. SMILE's generic focus policy silences music and WAV effects whenever the window is inactive or minimized.
