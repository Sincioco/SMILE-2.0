# Falling Blocks Demo Mode

Implement the AI and demo state in:

```text
games\FallingBlocks\Program.smile
```

Preserve the current board, seven pieces, rotation rules, row clearing, level progression, sounds, MP3 lifecycle, high-score behavior, and title/user controls.

## 1. Title and demo timing

Apply the shared arcade contract:

```smile
CONST TitleDemoDelay = 5000
CONST DemoMinimumPlayTime = 30000
CONST DemoMaximumPlayTime = 45000
CONST DemoTerminalDuration = 5000
```

Add:

```smile
DemoMode = FALSE
TitleStartedAt = 0
DemoStartedAt = 0
DemoTerminalStartedAt = 0
```

Title:

- Enter starts user play.
- Escape exits.
- Five seconds of inactivity starts demo.
- Other title input resets inactivity.
- Title remains silent.

## 2. Demo startup

Create `StartDemo()`:

- clear the board;
- reset score, lines, level, fall timing;
- set `DemoMode = TRUE`;
- start the same looping `Assets\Background.mp3`;
- spawn a piece;
- plan its placement immediately;
- show `DEMO` and `PRESS ANY KEY TO RETURN`.

Do not save demo score.

## 3. Piece-placement AI

The AI becomes the player.

For every newly spawned piece, evaluate:

```text
4 rotations
every horizontal position where the rotated piece fits
the final drop row for that position
```

Choose the highest-scoring placement.

### Suggested evaluation

Use integer weights:

```text
completed lines       large positive reward
holes below blocks    very large penalty
maximum stack height  penalty
aggregate height      penalty
surface bumpiness     penalty
covered wells         penalty
distance from center  small tie-break penalty
```

A practical starting score:

```text
+10000 per completed line
-800 per hole
-80 per maximum height
-10 per aggregate height
-20 per bumpiness
-2 per distance from center
```

Tune only enough to make the demo survive and clear rows.

### Implementation constraints

- Keep all logic in `.smile`.
- Do not copy the board for every candidate if a direct hypothetical-cell test is simpler.
- Use global candidate scratch values where the four-parameter routine limit requires it.
- Do not add a native Tetris solver.
- Do not change the user’s piece physics.

Useful routines:

```text
PlanDemoPiece
FindDropY
CandidateHasBlock
EvaluateCandidate
ExecuteDemoPlan
```

## 4. Executing the plan

The AI should visibly play rather than teleporting every frame.

At short intervals:

1. rotate toward the planned rotation;
2. move left/right toward the target X;
3. hard drop when aligned.

A 60–120 ms action interval is sufficient.

The normal fall timer continues to operate.

## 5. Thirty-second safety

Before 30 seconds, an early spawn failure must not display game over.

In demo mode only:

- if a new piece cannot spawn, clear/reset the board;
- preserve `DemoStartedAt`;
- optionally preserve demo score or reset it;
- spawn again;
- continue music and demo play.

Use the smallest visible recovery. A board reset is acceptable for an attract demo.

User mode keeps normal game-over behavior.

## 6. Demo terminal

At 45 seconds:

- call a demo-specific end routine;
- stop music;
- play the existing game-over effect;
- draw `GAME OVER` or `DEMO OVER`;
- keep it for five seconds;
- return to title.

A natural terminal after 30 seconds follows the same five-second rule.

`EndGame()` must branch so it does not save a demo high score.

## 7. Input cancellation

Any key during demo:

- stops music;
- returns to title;
- is consumed;
- does not start user play in the same frame.

Escape during user/title retains existing semantics.

## 8. Audio focus

Continue normal music and sounds in demo.

Do not add focus logic to the game. Shared runtime focus muting applies automatically.

## 9. Fast validation

Automated:

```text
cmd /c scripts\smoke-test.cmd
```

Manual:

1. Wait five seconds for demo.
2. Confirm the AI rotates, moves, and drops pieces.
3. Confirm at least one row can clear during ordinary observation when practical.
4. Confirm no game-over screen before 30 seconds.
5. Confirm demo terminal by 45 seconds and title return after five seconds.
6. Press a key during a second demo and confirm consumed return.
7. Start user play; confirm controls and MP3 lifecycle are unchanged.

Do not run a long AI tournament or exhaustive piece-sequence test.
