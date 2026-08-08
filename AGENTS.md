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

## Games prove the language

Snake, Falling Blocks, Paddle Ball, Brick Breaker, and Dungeon Star I must be implemented in `.smile` source.

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

## Public commit-message policy

Every nontrivial commit must use a meaningful subject and a detailed body:

```text
feat(graphics): add native game-window drawing support

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
