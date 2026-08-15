# Implementation Plan, Commit Sequence, and Velocity Rules

## Baseline

Before editing:

1. Inspect `git status`.
2. Preserve user changes.
3. Build current solution.
4. Run current smoke tests.
5. Compile/run current console Snake.
6. Build current VSIX.

## Milestone commits

### 1 — Governance and solution identity

- add requirements and AGENTS;
- rename solution;
- update paths.

Suggested subject:

```text
chore(repo): establish public governance and rename the SMILE solution
```

### 2 — Core language

- arithmetic additions;
- constants;
- 2D arrays;
- procedures/functions;
- Select Case;
- loop exits;
- built-ins and key/color constants.

```text
feat(language): add structured routines arrays and game-oriented expressions
```

### 3 — Native game runtime

- game window;
- graphics;
- scaling;
- DPI;
- Alt+Enter;
- input;
- sound;
- storage;
- GraphicsBasics.

```text
feat(graphics): add native SMILE windows drawing input audio and storage
```

### 4 — Visual Studio projects

- `.smileproj`;
- two templates;
- build/run;
- asset copy;
- Error List.

```text
feat(visual-studio): add SMILE project templates build and run support
```

### 5 — Snake

```text
feat(snake): add the graphical SMILE Snake game
```

### 6 — Falling Blocks

```text
feat(falling-blocks): add a complete graphical puzzle game in SMILE
```

### 7 — Paddle Ball

```text
feat(paddle-ball): add one-player and two-player SMILE arcade modes
```

### 8 — Brick Breaker

```text
feat(brick-breaker): add a colorful multi-level SMILE arcade game
```

### 9 — Documentation and final regression

```text
docs: publish the SMILE game-development milestone
```

## Velocity rules

- Keep current MASM native backend.
- Keep current VSIX unless impossible.
- Prefer one generic runtime call per SMILE statement.
- Keep game logic in SMILE.
- Use fixed global arrays where simple.
- Limit routines to four scalar parameters.
- Keep text literal-only; use Draw Number.
- Use integer movement.
- Add focused smoke cases, not a large framework.
- Build after small coherent changes.
- Fix root causes, not game-specific exceptions.
- Do not stop after partial implementation.
