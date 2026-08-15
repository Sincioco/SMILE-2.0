# SMILE Brick Breaker Game Specification

## Project

```text
games\BrickBreaker\BrickBreaker.smileproj
games\BrickBreaker\Program.smile
games\BrickBreaker\Assets\
```

## Visual direction

Dark playfield, rows of bright colored bricks, light paddle, visible ball, retro score/lives.

Reference:

```text
https://cdn.mos.cms.futurecdn.net/aU7ujAsKWede8NTLPX3NNA.jpg
```

Do not copy commercial art or branding.

## Layout

```text
Canvas:        960×540
Brick rows:    7
Brick columns: 12
Paddle:        about 100×16 near Y=500
Ball:          about 12×12 or radius 7
```

Rows use red, orange, yellow, green, cyan, blue, and magenta.

Top-to-bottom point values:

```text
70, 60, 50, 40, 30, 20, 10
```

## Controls

```text
A / Left      paddle left
D / Right     paddle right
Space/Enter   launch/start/retry
Escape        exit/title
Alt+Enter     automatic full-screen
```

## Rules

- Three lives.
- Ball rests above paddle before launch.
- Ball bounces off walls and paddle.
- Brick hit removes one brick and awards row points.
- Missing ball loses a life.
- Clearing all bricks advances level.
- Three levels; speed increases each level.
- Clear level three to win.
- Keep integer velocity and safe speed caps.

## High score

Persist:

```smile
Load HighScore From "HighScore" Default 0
```

## Sound

```text
Paddle.wav
Wall.wav (optional)
Brick.wav
LoseLife.wav
LevelClear.wav
GameOver.wav
```

## Screens

Title, Ready after life loss, Victory, and Game Over. Enter retries; Escape exits.

## Requirement

Paddle, ball, brick collision, scoring, lives, levels, and state logic live in SMILE.

## Acceptance

Native x64 GUI; 7×12 colored bricks; held controls; launch/bounce/break behavior; row scoring; 3 lives; 3 levels; persistent high score; sounds; victory/game-over; retry/exit; resize; Alt+Enter.
