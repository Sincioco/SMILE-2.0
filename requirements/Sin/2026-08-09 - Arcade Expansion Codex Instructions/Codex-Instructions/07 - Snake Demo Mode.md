# Snake Demo Mode

The user’s original item 10 repeated “FallingBlocks” under the Snake heading. This specification correctly applies item 10 to:

```text
games\Snake\Program.smile
```

Preserve current Snake movement, scoring, speed progression, food, sounds, high-score persistence, and user controls.

## 1. Timing and state

Use the shared arcade constants:

```smile
Const TitleDemoDelay = 5000
Const DemoMinimumPlayTime = 30000
Const DemoMaximumPlayTime = 45000
Const DemoTerminalDuration = 5000
```

Add:

```smile
DemoMode = False
TitleStartedAt = 0
DemoStartedAt = 0
DemoTerminalStartedAt = 0
```

Title:

- Enter starts user mode.
- Escape exits.
- Inactivity for five seconds starts demo.
- Other input resets title inactivity.

## 2. Demo startup

`StartDemo()` should:

- call the normal reset logic without enabling high-score saving;
- set `DemoMode = True`;
- set the demo timer;
- show `DEMO` and `PRESS ANY Key To Return`;
- use the normal movement cadence and sounds.

## 3. Snake AI

The AI controls `NextDirection`.

Preferred approach:

1. Build a breadth-first path from the head to food.
2. Treat walls and current snake body as blocked.
3. The current tail may be considered available only when it will move away and the snake is not eating.
4. Reject an immediate reverse direction.
5. Before committing, verify the next cell is safe.
6. When no food path exists, choose a safe fallback direction.

Add fixed arrays sized to the grid for:

```text
BFS queue
visited/parent
first direction
flood-fill reachable area
```

### Safe fallback

For every legal non-reverse move:

- simulate the new head cell;
- flood-fill available board area;
- prefer the move with the largest reachable area;
- use distance to food as a tie-breaker.

This avoids simple wall following that eventually traps the snake.

Do not add native pathfinding.

## 4. Thirty-second safety

Before 30 seconds, the attract demo must not show game over.

If the AI finds no safe move or collision occurs:

- reset the snake to the center;
- spawn new food;
- preserve the original `DemoStartedAt`;
- continue demo mode;
- do not show the terminal overlay.

This recovery is demo-only.

User mode retains the normal death path.

## 5. Demo terminal

At 45 seconds:

- enter a demo game-over state;
- draw the normal board and a `DEMO OVER` or normal game-over overlay;
- play the game-over sound once;
- retain the overlay for five seconds;
- return to title.

A natural death after 30 seconds may start this terminal state earlier.

## 6. Persistence

`EndRound()` must not update/save high score during demo.

User-mode high score behavior remains exactly as before.

## 7. Input cancellation

Any ordinary key during demo returns to title and is consumed.

Escape during demo returns to title; Escape on title exits.

## 8. Audio focus

Use normal `Play Sound` calls.

The shared runtime suppresses/stops WAV effects while the game is inactive. Do not duplicate it in Snake.

## 9. Fast validation

Run:

```text
cmd /c scripts\smoke-test.cmd
```

Then one normal cycle:

1. Wait five seconds.
2. Confirm Snake begins moving itself.
3. Confirm it turns and pursues food.
4. Confirm at least one food pickup when practical.
5. Confirm it survives through 30 seconds.
6. Confirm demo terminal by 45 seconds and title after five seconds.
7. Confirm any key cancels a second demo.
8. Confirm user mode and high score still work.

Do not run hundreds of random food placements by default.
