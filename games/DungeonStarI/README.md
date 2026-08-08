# Dungeon Star I

Dungeon Star I is an original first-person, grid-based dungeon exploration game written entirely in SMILE 2.0 source. It demonstrates random-but-validated map generation, closed and open doors, reciprocal stairs across three floors, palette-swapped pseudo-3D rendering, time-based movement and transitions, a self-playing attract mode, user-idle handling, and MP3 background music.

## Build

From the repository root:

```text
artifacts\compiler\smilec.exe games\DungeonStarI\Program.smile -o artifacts\games\DungeonStarI\DungeonStarI.exe
```

Copy `games\DungeonStarI\Assets` beside the executable. Visual Studio builds the `.smileproj` and copies the declared wildcard assets automatically.

## Controls

- Up or W: move forward.
- Down or S: move backward.
- Left or A: turn left.
- Right or D: turn right.
- Enter or Space: open the closed door directly ahead.
- Escape: return from player exploration to the title; Escape again exits.
- Alt+Enter: toggle true borderless full screen and windowed mode.

## Floors and generation

Each run creates three 31-by-31 floors. Every normal floor attempts five through nine separated rooms, connects them in a guaranteed chain, adds extra loops, places at least three valid doorway tiles when possible, places reciprocal stairs, and validates reachability with breadth-first search. Generation is bounded and falls back to a deterministic five-room floor instead of hanging.

Floor 1 uses an emerald palette, floor 2 sapphire, and floor 3 crimson. The first-person view is drawn entirely with SMILE quadrilaterals, rectangles, lines, text, and numbers; there are no textures or game-specific native helpers.

The game is deliberately about exploration only: it has no combat, items, character statistics, score, or minimap. Its original runtime-drawn geometry works through both the DirectX and GDI graphics backends and requires no image assets.

## Title, demo, and idle timing

The title flashes `PRESS ANY KEY TO START`. After 15 seconds it shows `5`, `4`, `3`, `2`, `1`, and `0` for one second each, then begins a freshly generated 60-second self-playing demo at 21 seconds. Any demo key returns to the title and is consumed.

During player exploration there is no warning before 30 seconds of inactivity. A flashing `9` through `0` warning runs from 30 through 39 seconds, and the game returns to the title at 40 seconds. Any key or held movement control resets that timer before the deadline check.

## Music

The exact repository-owner-supplied `Assets\Background.mp3` loops only while the demo or player is traversing the dungeon. The title is silent, and every route back to the title stops music. SMILE's generic focus policy silences music and WAV effects whenever the window is inactive or minimized.
