# Shared Arcade Demo Mode Contract

Apply this behavior to:

```text
Brick Breaker
Falling Blocks
Snake
Paddle Ball
Maze Muncher
Star Squadron
```

Dungeon Star I keeps its existing self-playing dungeon controller but changes its title inactivity delay to five seconds under its separate specification.

## 1. Keep demo mode in SMILE source

Do not add a `DEMO` keyword or native autoplay service.

Every game must implement:

- demo state;
- demo AI;
- demo timers;
- demo terminal handling;
- title transitions;

inside its own `Program.smile`.

The native runtime remains generic.

## 2. Standard timings

Use these defaults:

```smile
CONST TitleDemoDelay = 5000
CONST DemoMinimumPlayTime = 30000
CONST DemoMaximumPlayTime = 45000
CONST DemoTerminalDuration = 5000
CONST TitleInputArmDelay = 250
```

Interpretation:

- Title remains idle for five seconds.
- Demo begins at five seconds.
- Do not show a demo game-over/result screen before 30 seconds.
- Demo normally ends at 45 seconds.
- A demo game-over/result screen remains for five seconds.
- Then return to title.

Forty-five seconds is the approved normal duration because the user requested 30–60 seconds.

## 3. Standard state behavior

Games may retain their existing numeric `State` values, but add explicit demo flags or states.

Recommended fields:

```smile
DemoMode = FALSE
TitleStartedAt = 0
TitleAcceptInputAt = 0
DemoStartedAt = 0
DemoTerminalStartedAt = 0
```

Recommended concepts:

```text
Title
User play
Demo play
User terminal
Demo terminal
```

## 4. Entering title

Use one routine such as:

```smile
SUB EnterTitle()
```

It must:

- stop game-specific music that should not play on title;
- clear demo mode;
- clear pending controls;
- drain queued key events;
- reset title idle time;
- apply a short input-arm delay;
- preserve existing high score display;
- not exit the program.

The key that canceled a demo must not immediately start user play.

## 5. Title input and inactivity

Existing user start controls remain valid:

```text
Brick Breaker    Enter or Space
Falling Blocks   Enter
Snake            Enter
Paddle Ball      1 or 2
Maze Muncher     Enter or Space
Star Squadron    Enter or Space
```

Any title input that does not start/exit a game should still reset the five-second inactivity timer.

Escape on title exits.

When title inactivity reaches five seconds:

```text
start demo
```

No 5-to-0 countdown is required.

## 6. Canceling demo

During demo mode:

```smile
IF Key <> KEY_NONE THEN
    CALL EnterTitle()
    RETURN
END IF
```

Requirements:

- any ordinary key cancels;
- Escape returns to title rather than immediately exiting;
- the canceling key is consumed;
- Alt+Enter remains the runtime full-screen shortcut;
- title input-arm delay prevents accidental immediate restart.

## 7. Minimum visible play

The AI should play credibly.

Before 30 seconds, a natural loss must not show terminal/game-over.

Use the smallest game-appropriate safety:

- stronger demo AI;
- seamless demo-only life/reset recovery;
- ignore an early terminal and restart the round;
- clear a dangerous board area;
- reset the ball/ship/snake without leaving demo play.

Do not visibly grant infinite-life UI or change user mode.

At or after 30 seconds, normal terminal behavior may occur.

## 8. Maximum duration

At 45 seconds:

- if still playing, transition to a demo terminal/result screen;
- display an appropriate `GAME OVER`, `DEMO OVER`, or match-result overlay;
- retain it for five seconds;
- return to title.

A natural terminal reached between 30 and 45 seconds follows the same five-second terminal rule.

## 9. Persistence isolation

Demo play must not:

- overwrite a real high score;
- update best rally;
- save a demo score;
- consume persisted lives/credits;
- change user configuration.

Display demo score if useful, but do not call `SAVE` from demo achievements.

## 10. Audio

Demo mode may use the same normal gameplay WAV effects and music.

Title and terminal music behavior remains game-specific.

Focus muting is automatic shared runtime behavior. Do not implement it in game code.

## 11. Visual label

During demo play draw a small unobtrusive label:

```text
DEMO
PRESS ANY KEY TO RETURN
```

The label must not cover critical gameplay.

## 12. Fast validation

For each game, one normal cycle is enough:

1. Wait five seconds on title.
2. Confirm demo begins.
3. Observe representative AI play.
4. Confirm it remains active through 30 seconds.
5. Confirm demo terminal by 45 seconds.
6. Confirm title return five seconds later.
7. Start demo again and press one key; confirm consumed return to title.

This is roughly one minute per game, not a long soak.

The normal smoke suite must still pass.
