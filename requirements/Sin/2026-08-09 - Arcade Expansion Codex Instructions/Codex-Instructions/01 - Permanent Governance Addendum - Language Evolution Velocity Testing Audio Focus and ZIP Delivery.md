# Permanent Governance Addendum
## Language Evolution, Velocity Testing, Default Audio Focus, and ZIP Delivery

This is a permanent SMILE 2.0 development rule. Merge its durable requirements into `AGENTS.md` and the appropriate public architecture/language documentation.

## 1. Language-evolution hierarchy

SMILE 2.0 may evolve as needed to create clear educational programs and complete games.

For every proposed addition:

1. First determine whether current SMILE syntax can express the requirement clearly.
2. When a new feature is justified, prefer established BASIC ideas and readable BASIC-style wording.
3. When BASIC has no suitable precedent, use the smallest beginner-friendly C#-inspired concept.
4. Avoid aliases and multiple spellings.
5. Avoid clever punctuation when readable words are clearer.
6. Keep additions general-purpose.
7. Never add game-specific native helpers.
8. Implement language rules once in `src\Smile.Language`; compiler and Visual Studio consume that authority.
9. Add only proportional diagnostics, tests, examples, and documentation.

Examples of acceptable evolution:

```smile
Play Music "Assets\Background.mp3" Loop
Fill Quadrilateral ...
Load Text File "Maps\default.map" Into MapBytes Count MapByteCount
```

Examples that must not be added:

```smile
GENERATE DUNGEON
DEMO MODE 45
PACMAN AI
Draw GALAGA ENEMY
AUTO Play BRICK BREAKER
```

Those are game rules and belong in `.smile`.

## 2. Permanent velocity-testing rule

SMILE 2.0 development assumes the happy path by default.

### Default validation

Use the lightest evidence that reasonably proves the milestone:

- focused shared-language tests for new syntax;
- focused native tests for a shared runtime change;
- `cmd /c scripts\smoke-test.cmd`;
- a brief manual launch;
- a short interaction with the changed behavior;
- one DirectX and/or GDI run only where that backend is touched;
- one ordinary attract-mode timing cycle where demo behavior changes.

Do not automatically add:

- 30-minute runs;
- hour-long stability tests;
- hundreds or thousands of random seeds;
- exhaustive game completion;
- large fuzzing campaigns;
- repeated 100-cycle window/full-screen tests;
- broad hardware matrices;
- speculative benchmarks.

### Exception allowing longer tests

Longer or broader testing is permitted when it is directly needed to investigate:

- a known bug;
- a crash;
- a hang;
- a resource leak;
- a timing failure;
- a performance regression;
- a formal performance benchmark;
- a defect that appears only after prolonged operation;
- an intermittent issue requiring repetition.

When invoking this exception, Codex must state:

```text
Known problem being investigated:
Why the longer test is necessary:
Stop condition:
```

Do not turn a one-time troubleshooting test into the new default regression burden unless Sin explicitly approves it.

## 3. Default game-audio focus behavior

This is a shared runtime contract for **every** SMILE program containing:

```smile
Game Window ...
```

No per-game SMILE code is required.

When the SMILE game:

- loses foreground application activation;
- loses top-level window activation;
- is minimized;
- is no longer the active game window;

the shared native runtime must immediately silence all of that game’s audio.

Required behavior:

### MP3 music

- Effective music volume becomes zero.
- Requested `Music Volume` remains remembered.
- Playback position continues unless the game explicitly paused/stopped it.
- Regaining active, non-minimized focus restores the exact requested volume.
- A manually paused track remains paused.
- Focus restoration must not restart the track.

### WAV effects

- The currently playing asynchronous WAV effect stops when focus is lost.
- New `Play Sound` requests are suppressed while inactive.
- Suppressed effects are not replayed when focus returns.

### System isolation

- Do not change Windows master volume.
- Do not change another application’s volume.
- Do not require every game to implement activation handlers.
- DirectX and GDI must behave identically.

The verified baseline already contains shared activation/minimize tracking and native focus-state tests. Codex must audit that implementation, formalize this as the documented default, fill only real gaps, and avoid duplicating the logic inside Dungeon Star I or Falling Blocks.

Add this rule to:

```text
AGENTS.md
README.md
docs\architecture\README.md
docs\language\README.md
```

Update the game template documentation so new SMILE games inherit it automatically.

## 4. Multiple-Markdown ZIP rule

When a task produces two or more Markdown requirement, specification, handoff, or instruction files:

1. Keep each Markdown file individually usable.
2. Package all Markdown files into one ZIP.
3. Include companion sample/configuration/map files in the same ZIP.
4. Include a `START HERE` file describing order and purpose.
5. Preserve intended repository-relative paths under a `Repository-Files` folder where useful.

Add this as a permanent project-delivery preference in `AGENTS.md`.

This rule concerns delivery artifacts. It does not require adding ZIP support to the SMILE language.

## 5. Minimal validation for this governance milestone

Perform only:

```text
cmd /c scripts\smoke-test.cmd
```

plus a brief live focus check using:

- one game with MP3 music, such as Dungeon Star I or Falling Blocks;
- one WAV effect;
- focus loss and restoration;
- minimize and restore.

Do not retest every game for focus muting because the behavior is shared runtime functionality.

## 6. Suggested commit

```text
docs(governance): formalize velocity and default game-audio rules

Summary:
- Makes happy-path light testing, BASIC-first language evolution,
  automatic all-game focus muting, and ZIP delivery permanent rules.

Changes:
- Update AGENTS.md and current public documentation.
- Clarify that focus muting is shared runtime behavior.
- Preserve longer testing only for known investigations.

Validation:
- cmd /c scripts\smoke-test.cmd
- Brief MP3/WAV focus-loss happy-path check.

Known limitations:
- None identified.
```
