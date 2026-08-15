# SMILE Paddle Ball Game Specification

## Project

```text
games\PaddleBall\PaddleBall.smileproj
games\PaddleBall\Program.smile
games\PaddleBall\Assets\
```

## Visual direction

Black field, white paddles, white ball, dashed center line, large scores, optional subtle accent.

Reference:

```text
https://www.reddit.com/media?url=https%3A%2F%2Fi.redd.it%2Fzval2c6qxv2a1.png
```

## Modes

Title menu:

```text
1 — One Player
2 — Two Players
Escape — Exit
```

One-player: human left paddle, beatable computer right paddle.

Two-player: W/S left paddle; Up/Down right paddle.

## Rules

- Ball starts centered.
- Top/bottom wall bounce.
- Paddle collision.
- Crossing an edge awards a point.
- First to 7 wins.
- Reset after point.
- Slight speed increase after several hits.
- Paddle-contact position affects vertical direction using integer math.
- Avoid zero vertical velocity.

## Best rally

In one-player mode persist longest consecutive paddle-contact rally:

```smile
Load BestRally From "BestRally" Default 0
```

## Sound

```text
Paddle.wav
Wall.wav
Score.wav
GameOver.wav (optional)
```

## Result screens

Show Player 1 Wins, Player 2 Wins, You Win, or Computer Wins, then Enter to replay and Escape for title.

## Requirement

AI, motion, collision, scoring, and state logic live in SMILE.

## Acceptance

Native x64 GUI; one-player and two-player; simultaneous held keys; beatable AI; first to 7; best rally persistence; sounds; rematch/title; resize; Alt+Enter.
