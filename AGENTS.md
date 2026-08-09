# SMILE 2.0 Repository Instructions for Codex

These instructions apply to the entire SMILE 2.0 repository.

## Project identity

- `SMILE` always means **SMILE 2.0**, the new native compiler project.
- Do not maintain SMILE 1.0 compatibility unless Sin explicitly requests it.
- Local repository: `D:\SMILE 2.0`
- Public GitHub repository: `Sincioco/SMILE-2.0`
- Sin authorizes Codex to create, edit, rename, move, delete, build, run, commit, and push files belonging to this repository.

## KISS and velocity

Use KISS: Keep It Simple, Stupid.

- Prefer the smallest correct implementation.
- Avoid unnecessary abstractions, frameworks, libraries, dependencies, projects, files, folders, classes, methods, patterns, tests, and features.
- Extend the working architecture rather than replacing it.
- Do not add CI, GitHub Actions, a large automated test suite, cross-platform support, a package manager, or speculative infrastructure.
- Do not stop after planning or scaffolding. Continue through implementation, build, native compilation, execution, debugging, validation, commit, and push.

### Permanent validation rule

Assume the happy path and use the lightest focused evidence that reasonably proves the current milestone. Prefer targeted language or native checks, the normal smoke suite, one brief manual launch, and one short interaction with changed behavior.

Do not run long soaks, large seed sweeps, exhaustive playthroughs, broad hardware matrices, speculative benchmarks, or repeated stress cycles by default. Longer or broader testing is allowed only to investigate a known bug, crash, hang, leak, timing defect, performance regression, benchmark, intermittent failure, or problem that requires sustained execution. Before invoking that exception, record:

```text
Known problem being investigated:
Why the longer test is necessary:
Stop condition:
```

Do not turn a one-time investigation into a permanent regression burden unless Sin explicitly approves it.

## One authoritative language implementation

`src/Smile.Language` remains the single source of truth for:

- lexer, tokens, and keywords;
- parser and syntax tree;
- syntax rules;
- diagnostics;
- symbols and type rules;
- semantic analysis and semantic model.

The compiler and Visual Studio extension must consume this shared implementation.

Never create a compiler-only parser, extension-only parser, duplicate keyword table, duplicate syntax rules, or duplicate semantic rules.

When SMILE needs to evolve:

1. First use current SMILE syntax when it remains clear.
2. If a new feature is justified, prefer an established, readable BASIC precedent.
3. Use the smallest beginner-friendly C#-inspired concept only when BASIC has no suitable precedent.
4. Avoid aliases, multiple spellings, and clever punctuation when readable words are clearer.
5. Keep every addition general-purpose and add only proportional diagnostics, tests, examples, and documentation.

## Games prove the language

Snake, Falling Blocks, Paddle Ball, Brick Breaker, Dungeon Star I, and Maze Muncher must be implemented in `.smile` source.

The native runtime may provide only generic services:

- window creation;
- graphics primitives;
- frame presentation and scaling;
- keyboard input;
- timing;
- WAV playback;
- MP3 background-music playback and automatic focus muting;
- simple integer persistence.

Do not add game-specific native helpers.

### Student no-demo source rule

Whenever a game includes an attract or demo mode, the same game folder must also include `Program-NoDemo.smile` as a complete playable teaching version.

- Keep `Program.smile` as the normal demo-enabled startup source unless Sin directs otherwise.
- Remove demo lifecycle, demo AI, demo-only timers and safety rules, demo UI, automatic title launch, terminal return, and demo cancellation from `Program-NoDemo.smile`; do not merely disable them with a flag.
- Preserve the user game rules, controls, rendering, scoring, persistence, levels, and assets in both versions.
- Include both files in the game project and compile both in repository validation.
- Document how a student can switch the project startup file to the no-demo source.

### Attract-mode return rule

Every attract/demo mode, including those in future games, must return directly to the title screen when its time limit expires or its natural demo run ends.

- Do not show `DEMO OVER`, demo victory, demo game-over, rematch, retry, or other terminal overlays between the demo and title screen.
- Keep normal player game-over, victory, retry, and rematch screens unchanged.
- Continue to let any user input cancel an active demo and return directly to the title screen.

## Default game-audio focus contract

Every program containing `GAME WINDOW` automatically inherits shared native audio focus behavior. Do not duplicate activation handling in `.smile` games.

- Losing application activation, top-level window activation, or becoming minimized immediately silences that game's audio.
- MP3 playback continues at effective volume zero while the exact requested `MUSIC VOLUME` remains remembered.
- Restoring an active, non-minimized window reapplies that volume without restarting or resuming a manually paused or stopped track.
- The current asynchronous WAV effect stops on focus loss; new `PLAY SOUND` requests are suppressed while inactive and are not replayed later.
- The runtime never changes Windows master volume or another application's volume.
- DirectX and GDI use the same shared focus policy.

## Multi-Markdown delivery

When a task produces two or more Markdown requirement, specification, handoff, or instruction files:

- keep each Markdown file individually usable;
- package all of them in one ZIP archive;
- include companion sample, configuration, or map files;
- include a `START HERE` file describing purpose and reading order;
- preserve intended repository-relative paths under `Repository-Files` where useful.

This is an artifact-delivery rule, not a SMILE language feature.

## Public commit-message policy

Every Codex-created commit subject must begin exactly with:

```text
Sin and Codex:
```

Follow that prefix with a meaningful subject, and give every nontrivial commit a detailed body:

```text
Sin and Codex: feat(graphics): add native game-window drawing support

Summary:
- User-visible or architectural result.

Changes:
- Important language, compiler, runtime, Visual Studio, game, and documentation changes.

Validation:
- Exact builds, generated executables, and manual checks performed.

Known limitations:
- Deferred behavior, or "None identified."
```

Do not use vague messages such as `chore: update files`, `fix: changes`, or `feat: improvements`.

- Each milestone gets a coherent commit.
- Do not commit broken milestones.
- Push each validated milestone.
- Do not amend, rebase, force-push, or rewrite pushed history unless Sin explicitly directs it.
- Never discard uncommitted user work.

## Final report

Report commit hashes, files changed, syntax added, generated executables, VSIX path, validation results, and remaining manual checks.
