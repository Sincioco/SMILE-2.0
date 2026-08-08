# SMILE Falling Blocks Game Specification

## Project

```text
games\FallingBlocks\FallingBlocks.smileproj
games\FallingBlocks\Program.smile
games\FallingBlocks\Assets\
```

## Visual direction

Black background and bright solid-colored four-cell pieces.

Reference:

```text
https://user-images.githubusercontent.com/2433219/94984518-13818800-050a-11eb-938e-275156f905c8.png
```

Do not copy artwork or commercial branding.

## Board

```text
Canvas:     960×540
Board:      10×20 cells
Cell:       22×22
Board:      220×440
Origin:     approximately X=320, Y=50
```

Use side space for score, high score, level, lines, next piece, and controls.

## Controls

```text
A / Left      move left
D / Right     move right
S / Down      soft drop
W / Up        rotate clockwise
Space         hard drop
Enter         start/retry
Escape        exit
```

## Rules

- Seven classic four-cell shapes with original colors/UI.
- Automatic falling.
- Valid movement and rotation only.
- Lock on floor/stack.
- Clear full rows and shift above rows.
- Game over when a new piece cannot spawn.
- Next-piece preview.
- Simple random 1–7 is acceptable.

## Scoring

```text
1 line:  100 × Level
2 lines: 300 × Level
3 lines: 500 × Level
4 lines: 800 × Level
```

Level increases every 10 lines.

```smile
FallDelay = MAX(80, 700 - (Level - 1) * 60)
```

Persist high score.

## Sound

Original WAV files:

```text
Move.wav (optional)
Rotate.wav
LineClear.wav
GameOver.wav
```

## Deferred

No hold, ghost, advanced spin rules, multiplayer, particles, skins, or music.

## Acceptance

Native x64 GUI game; seven pieces; movement/rotation/drop; collision/locking; row clearing; score/lines/level/next; speed increase; persistent high score; sounds; retry/exit; WASD/arrows; Alt+Enter.
