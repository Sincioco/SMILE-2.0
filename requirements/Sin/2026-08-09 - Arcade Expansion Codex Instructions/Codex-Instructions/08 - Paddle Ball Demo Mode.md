# Paddle Ball Demo Mode

Implement demo behavior in:

```text
games\PaddleBall\Program.smile
```

Preserve one-player mode, two-player mode, fixed-step ball physics, scores, rally tracking, sounds, best-rally persistence, and existing controls.

## 1. Mode/state extension

The current game uses:

```text
Mode 1  player vs computer
Mode 2  local two-player
```

Add:

```text
Mode 3  demo, computer vs computer
```

or retain the existing modes and use `DemoMode = TRUE`. Either is acceptable; keep the code clear.

Use shared timing constants:

```smile
CONST TitleDemoDelay = 5000
CONST DemoMinimumPlayTime = 30000
CONST DemoMaximumPlayTime = 45000
CONST DemoTerminalDuration = 5000
```

## 2. Title

Preserve:

```text
1 - ONE PLAYER
2 - TWO PLAYERS
```

Add a flashing or small:

```text
DEMO STARTS AUTOMATICALLY
```

Behavior:

- key 1 starts one-player;
- key 2 starts two-player;
- Escape exits;
- title input resets inactivity;
- five seconds without input starts demo.

## 3. Starting demo

`StartDemo()` must:

- set both paddles to AI;
- reset scores, rally, ball, accumulator;
- set `DemoStartedAt`;
- start the normal serve automatically;
- show `DEMO` and `PRESS ANY KEY TO RETURN`;
- not save best rally.

## 4. Two-paddle AI

Both AI paddles should move using fixed-step subpixel state.

For the paddle toward which the ball is moving:

- track the ball’s Y;
- optionally predict vertical wall bounces before the ball reaches the paddle;
- move with a small dead zone;
- clamp to the field.

For the paddle away from the ball:

- drift toward center;
- or anticipate the likely return.

Use a demo speed sufficient for long rallies, approximately:

```text
360–480 pixels/second
```

Do not teleport during normal demo play.

## 5. Thirty-second safety

The match must not end before 30 seconds.

Before `DemoMinimumPlayTime`:

- if a point would make either score reach `WinningScore`, reset both scores to a lower value or begin a fresh scoreless rally;
- continue the same demo timer;
- do not show the result overlay.

Ordinary points are allowed; the safety only prevents a premature terminal match.

Another acceptable approach is a demo-only effective winning score large enough to last 30 seconds.

User modes retain `WinningScore = 7`.

## 6. Demo terminal

At 45 seconds:

- stop live simulation;
- draw `DEMO OVER` with the current score;
- retain it for five seconds;
- return to title.

If a normal result occurs after 30 seconds, show it for five seconds and return to title.

Do not offer rematch controls on a demo terminal.

## 7. Rally persistence

`RecordRally()` must not save `BestRally` during demo.

User one-player behavior remains unchanged.

## 8. Input cancellation

Any key during demo returns to title and is consumed.

This includes Escape.

Alt+Enter remains handled by the runtime.

## 9. Audio focus

Use existing wall/paddle/score sounds normally.

No per-game focus muting is permitted; the shared runtime owns it.

## 10. Fast validation

Run:

```text
cmd /c scripts\smoke-test.cmd
```

Manual:

1. Wait five seconds.
2. Confirm both paddles move automatically.
3. Confirm an extended rally and scoring.
4. Confirm no result before 30 seconds.
5. Confirm `DEMO OVER` or result by 45 seconds.
6. Confirm title return five seconds later.
7. Confirm any key cancels.
8. Confirm modes 1 and 2 still start and play normally.

No long rally endurance test is required.
