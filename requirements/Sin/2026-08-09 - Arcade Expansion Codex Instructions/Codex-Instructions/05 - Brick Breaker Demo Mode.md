# Brick Breaker Demo Mode

Modify only generic documentation/runtime where required by the permanent rules. Brick Breaker demo behavior belongs in:

```text
games\BrickBreaker\Program.smile
```

Preserve the existing fixed-step physics, levels, scoring, sounds, high-score persistence, DirectX/GDI rendering, and user controls.

## 1. Existing state model

The current game uses:

```text
State 0  title
State 1  live play
State 2  game over
State 3  victory
```

Keep those states if convenient. Add an explicit:

```smile
DemoMode = False
```

and standard title/demo timers from the shared contract.

## 2. Title behavior

Add `EnterTitle()` and call it at startup and whenever a demo finishes/cancels.

Title requirements:

- display existing title art;
- flash or retain the start prompt;
- begin demo after 5,000 milliseconds of no activity;
- Enter or Space starts user play;
- Escape exits;
- unrelated title input resets the five-second timer;
- drain the key that canceled demo.

No countdown is required.

## 3. Starting demo

Create:

```smile
Sub StartDemo()
```

It must:

- set `DemoMode = True`;
- reset score, lives, level, bricks, paddle, ball, accumulator;
- set `DemoStartedAt = Timer()`;
- auto-launch the ball after a short ready delay, approximately 400–700 ms;
- draw `DEMO` and `PRESS ANY Key To Return`;
- not write high score.

## 4. Demo paddle AI

The demo AI controls the existing player paddle.

Minimum implementation:

- while the ball travels downward, move paddle center toward the ball’s predicted or current X;
- while the ball travels upward, drift toward screen center or projected return X;
- honor paddle boundaries;
- use fixed-step subpixel motion;
- use a demo paddle speed fast enough to play credibly, such as 520–650 pixels/second;
- do not teleport during ordinary demo play.

A better but still simple prediction may simulate the ball’s X movement to `PaddleY`, reflecting from left/right walls. It may ignore brick collisions; current-X tracking remains an acceptable fallback.

Use a small dead zone so the paddle does not jitter every step.

## 5. Thirty-second safety

The demo must not show game over before:

```text
DemoMinimumPlayTime = 30000
```

In `LoseLife()`:

- user mode remains unchanged;
- in demo before 30 seconds, do not decrement into terminal game over;
- reset the ball over the AI paddle;
- auto-launch after a brief delay;
- continue the same demo timer.

The HUD may still show demo lives, but do not create a persisted infinite-lives feature.

If the AI naturally keeps the ball alive, this safety path may never execute.

## 6. Demo terminal

At 45 seconds:

```text
DemoMaximumPlayTime = 45000
```

transition to a demo terminal overlay.

Use either:

```text
DEMO OVER
```

or the normal `Game OVER` presentation with a small `DEMO` label.

Keep it visible for:

```text
DemoTerminalDuration = 5000
```

Then call `EnterTitle()`.

If a natural game over or victory occurs between 30 and 45 seconds, start the same five-second terminal timer immediately.

If terminal occurs before 30 seconds, perform safety recovery instead.

## 7. Input cancellation

Process this before AI/physics:

```smile
If DemoMode = True And Key <> KEY_NONE Then
    Call EnterTitle()
    Return
End If
```

The canceling key is consumed.

Escape during demo returns to title, not directly out of the application.

## 8. Persistence

Change `UpdateHighScore()` or its call sites so:

```text
DemoMode = True -> never Save
DemoMode = False -> existing behavior
```

Do not let demo score become the displayed persisted high score.

## 9. Audio focus

Use the existing WAV effects normally.

Do not add focus checks in Brick Breaker. The shared runtime mutes every inactive SMILE game.

## 10. Fast validation

Automated:

```text
cmd /c scripts\smoke-test.cmd
```

Manual happy path:

1. Launch title.
2. Wait five seconds; demo starts and auto-launches.
3. Observe AI paddle track and hit the ball.
4. Confirm no terminal before 30 seconds.
5. Confirm terminal by 45 seconds and title five seconds later.
6. Start demo again; press a key and confirm return to title.
7. Start a normal user game and confirm existing controls/high score remain intact.

One DirectX run is sufficient because no backend-specific drawing change is required.
