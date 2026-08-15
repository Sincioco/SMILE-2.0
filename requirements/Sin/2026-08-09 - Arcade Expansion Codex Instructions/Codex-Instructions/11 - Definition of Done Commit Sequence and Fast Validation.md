# Definition of Done, Commit Sequence, and Fast Validation

This file governs the complete arcade expansion.

## 1. Starting checks

Before editing:

```text
cmd /c git status --short
cmd /c git log -1 --oneline
```

Expected baseline when this package was created:

```text
5a9f405 feat(game): add Dungeon Star I native dungeon showcase
```

If the repository is newer:

- do not reset;
- inspect newer commits;
- preserve working features;
- adapt these instructions;
- never discard uncommitted user work.

## 2. Approved implementation sequence

### Milestone A — permanent governance

Update `AGENTS.md` and current docs for:

- BASIC-first/C#-second language evolution;
- light happy-path testing;
- longer-test exception;
- automatic all-game focus muting;
- ZIP delivery for multiple Markdown artifacts.

If the shared focus behavior already fully satisfies the contract, avoid native code churn.

### Milestone B — generic text-file loading

Implement:

```smile
Load Text File "path" Into Array Count Variable
```

Add focused language/runtime tests and documentation.

### Milestone C — Dungeon Star I adjustments

Add:

- map authoring guide;
- three supplied map files;
- title map menu;
- default map loading;
- missing/invalid random fallback;
- pipe-style random generator;
- expanded map validator;
- blue title palette;
- five-second demo start.

### Milestone D — existing-game demos

Implement one game at a time:

```text
Brick Breaker
Falling Blocks
Snake
Paddle Ball
```

A coherent combined commit is acceptable only if all four remain easy to review and the smoke suite passes. Separate commits are preferred for debugging and rollback clarity.

### Milestone E — Maze Muncher

Create the original maze-chase game and integrate it as game six.

### Milestone F — Star Squadron

Create the original full-16:9 shooter and integrate it as game seven.

## 3. Suggested commits

```text
docs(governance): formalize velocity and default game-audio rules

feat(io): add bounded text-file loading for numeric arrays

feat(dungeon-star): add editable maps and pipe-style generation

feat(brick-breaker): add arcade attract demo

feat(falling-blocks): add placement AI attract demo

feat(snake): add pathfinding attract demo

feat(paddle-ball): add computer-vs-computer attract demo

feat(game): add Maze Muncher arcade sample

feat(game): add Star Squadron wide-screen arcade shooter
```

Each nontrivial commit must use the detailed body format in `AGENTS.md`.

Push every validated milestone.

Do not amend/rebase/force-push published history.

## 4. Current documentation updates

At final completion update:

```text
AGENTS.md
README.md
docs\architecture\README.md
docs\language\README.md
```

Current game list becomes seven.

Historical milestone reports should not be rewritten merely to change an old game count. Update current documentation, not historical facts.

## 5. Solution/project files

Follow the current per-game conventions.

Create project and `.slnx` files for:

```text
MazeMuncher
StarSquadron
```

Ensure Visual Studio 2026 project support can build/run them.

Do not create C#/C++ game projects. The games remain `.smile`.

## 6. Assets

All new visual content is runtime-drawn.

All new WAV effects are original deterministic files generated through:

```text
scripts\generate-sounds.ps1
```

No copied commercial assets.

Hash/copy verification should cover new committed assets.

Map files must be copied byte-for-byte to output.

## 7. Smoke integration

Update:

```text
scripts\smoke-test.cmd
scripts\verify-artifacts.ps1
```

Final smoke suite must compile and verify:

```text
Snake.exe
FallingBlocks.exe
PaddleBall.exe
BrickBreaker.exe
DungeonStarI.exe
MazeMuncher.exe
StarSquadron.exe
```

Every game executable remains:

- x64;
- PE32+;
- Windows GUI subsystem;
- no CLR header.

## 8. Required automated validation

Run after every coherent milestone:

```text
cmd /c scripts\smoke-test.cmd
```

Add only proportional tests.

For `Load Text File`, include quick checks for:

- valid analysis;
- target rank/type diagnostics;
- known tiny file;
- missing file;
- capacity truncation.

For map files, a small script validates:

- headers;
- dimensions;
- symbols;
- borders;
- connectivity;
- stairs;
- no 2-by-2 openings;
- door orientation.

For demos, do not create a complicated headless game simulator unless needed to fix a real bug.

## 9. Required manual validation

Use the concise happy paths in each specification.

Approximate intended duration:

```text
Dungeon adjustment      1–3 minutes
Each existing demo      about 1 minute
Maze Muncher            2–4 minutes
Star Squadron           2–4 minutes
Focus mute              under 1 minute
```

These are guidance, not mandatory stopwatch budgets.

Do not perform long soak tests by default.

## 10. Longer-test exception

Additional/longer tests are allowed only for a known reason.

Before starting one, record:

```text
Known problem:
Why short validation is insufficient:
Longer test:
Stop condition:
```

Examples:

- crash after repeated level transitions;
- suspected GDI/COM leak;
- demo AI hangs after extended play;
- performance benchmark requested by Sin;
- intermittent timing failure.

Stop when the investigation answers the known question.

## 11. Definition of Done

### Governance

- [ ] BASIC-first/C#-second language evolution is permanent.
- [ ] Happy-path light testing is permanent.
- [ ] Longer-test exception is documented.
- [ ] Multi-Markdown ZIP delivery is permanent.
- [ ] All-game focus muting is documented as shared runtime behavior.

### File loading

- [ ] `Load Text File ... Into ... Count ...` is implemented generically.
- [ ] Existing persistence `Load ... From ... Default ...` remains valid.
- [ ] Missing/invalid file access is safe.
- [ ] Shared language/compiler/VS documentation agree.

### Dungeon Star I

- [ ] `MAP_AUTHORING.md` is in the project.
- [ ] Three map files are included/copied.
- [ ] `default.map` is selected first.
- [ ] Title can select maps or random generation.
- [ ] Missing maps fall back to random generation.
- [ ] Random maps are pipe-like, not open rooms.
- [ ] Long corridors/door spacing/start enclosure validate.
- [ ] Title uses blue palette.
- [ ] Demo begins after five seconds.

### Existing games

- [ ] Brick Breaker AI demo.
- [ ] Falling Blocks placement-AI demo.
- [ ] Snake pathfinding demo.
- [ ] Paddle Ball two-AI demo.
- [ ] Each starts after five seconds.
- [ ] Each shows active play through 30 seconds.
- [ ] Each ends by 45 seconds normally.
- [ ] Demo terminal lasts five seconds.
- [ ] Any key cancels and is consumed.
- [ ] Demo never saves user records.

### New games

- [ ] Maze Muncher is complete and original.
- [ ] Star Squadron is complete and original.
- [ ] Both are written in SMILE.
- [ ] Both have sounds and demo AI.
- [ ] Star Squadron uses full 16:9 logical canvas.
- [ ] No commercial assets/branding are copied.

### Integration

- [ ] Seven games compile.
- [ ] Artifact verification passes.
- [ ] VSIX builds and includes current compiler/runtime.
- [ ] `cmd /c scripts\smoke-test.cmd` passes.
- [ ] Brief manual happy paths pass.
- [ ] Commits are pushed.

## 12. Final Codex report

Report:

1. Starting commit.
2. Final commit hashes.
3. Branch pushed.
4. Files changed/added/deleted.
5. New syntax and its exact semantics.
6. Focus-muting audit/result.
7. Dungeon map format and fallback behavior.
8. Demo AI summary for each game.
9. New game names and mechanics.
10. Generated executable paths.
11. VSIX path.
12. Smoke-suite result.
13. Brief manual happy-path results.
14. Any longer test run and the known problem that justified it.
15. Known limitations, or `None identified.`

Do not claim long-term stability from short happy-path tests. State exactly what was tested.
