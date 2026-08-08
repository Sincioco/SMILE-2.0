# SMILE Snake Game Specification

## Project

```text
games\Snake\Snake.smileproj
games\Snake\Program.smile
games\Snake\Assets\
```

Preserve the original as:

```text
examples\ConsoleSnake.smile
```

## Visual direction

- dark navy background;
- black playfield;
- bright green square segments with darker boundaries;
- bright red square food;
- clean retro layout.

Reference:

```text
https://img.itch.zone/aW1nLzkxODYzMjMucG5n/original/Bozndd.png
```

Do not copy the image.

## Layout

```text
Canvas:     960×540
Playfield:  X=20, Y=30, 700×480
Grid:       35×24
Cell:       20×20
Info panel: right side
```

## Controls

```text
W / Up       up
S / Down     down
A / Left     left
D / Right    right
Enter        start/retry
Escape       exit
Alt+Enter    automatic full-screen
```

Prevent immediate reversal.

## Rules

- Continuous movement.
- Wall/body collision ends round.
- Food adds one segment and 10 points.
- Food never spawns on snake.
- Start delay 100 ms.
- Every 50 points reduce delay by 4 ms.
- Minimum 45 ms.

## High score and sound

```smile
LOAD HighScore FROM "HighScore" DEFAULT 0
```

Assets:

```text
Eat.wav
GameOver.wav
Start.wav (optional)
```

## Screens

Title, playing, and large game-over overlay.

Game over must show final score, Enter to retry, and Escape to exit without restarting the process.

## Requirement

All movement, growth, food, collision, score, speed, drawing, and retry logic live in SMILE.

## Acceptance

Graphical native x64 game; WASD and arrows; green snake; red food; score; persistent high score; sounds; retry/exit; resize; Alt+Enter; exact 1080p/4K scaling.
