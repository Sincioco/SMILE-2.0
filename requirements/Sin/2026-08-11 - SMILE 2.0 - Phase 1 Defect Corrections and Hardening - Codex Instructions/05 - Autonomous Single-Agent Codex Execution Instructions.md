# Autonomous Single-Agent Codex Execution Instructions

## Repository identity

Work only in:

```text
D:\SMILE 2.0
```

and:

```text
Sincioco/SMILE-2.0
```

Do not inspect or modify the original SMILE/SMILE 1.0 repository.

## Read first

Before editing:

1. Read the repository-root `AGENTS.md`.
2. Read every numbered file in this package in order.
3. Inspect the latest commit and current implementation.
4. Run:

```bat
cmd /c git status --short
```

5. Preserve all unrelated user work.
6. Never reset, discard, overwrite, or clean uncommitted user changes.
7. Review the supplied screenshot.

## Single-agent requirement

Use one agent only.

Do not spawn, delegate to, or coordinate another agent.

## Continue autonomously

Do not stop at a plan.

Proceed through:

```text
reproduction
root-cause confirmation
implementation
focused automated tests
build
VSIX installation
live Visual Studio debugger test
live context-menu/project mutation test
normal smoke suite
documentation
git diff review
commit
push
final report
```

Do not ask Sin to confirm intermediate steps.

## Implementation priorities

1. Correct native Windows `.smile` source stepping.
2. Add usable Visual Studio project/file/folder context menus.
3. Add source and startup management UI.
4. Harden project workspace/open-buffer coherence.
5. Preserve Windows behavior.
6. Preserve Web behavior.
7. Preserve all legacy games and tooling.

## KISS

Use the smallest correct implementation that extends the existing architecture.

Do not add:

- a replacement project system;
- a large custom debugger unless absolutely necessary;
- an unrelated framework;
- CI or GitHub Actions;
- speculative package infrastructure;
- Phase 2 language syntax;
- game-specific native helpers.

## Testing rule

Use focused, light tests plus the normal smoke suite and brief live Visual Studio verification.

Because these are confirmed defects, deeper targeted debugger investigation is allowed.

Do not add permanent exhaustive UI automation merely to simulate Visual Studio if a short live check is more reliable.

## Required test order

### A. Baseline reproduction

Before fixing:

- reproduce Source Not Available after F10;
- reproduce absent project/file/folder context menus;
- record the actual root causes.

### B. Focused automated checks

Run language/compiler/project tests covering changed behavior.

### C. Build

Run:

```bat
cmd /c scripts\build.cmd
```

### D. Install refreshed extension

Run the repository-supported VSIX installation script.

Save/close user documents before any script that closes Visual Studio.

Bump the VSIX version appropriately.

### E. Live native debugger test

Perform the exact test in document 02.

### F. Live Solution Explorer test

Perform the exact Snake workflow in document 03.

### G. Workspace hardening test

Exercise unsaved cross-file changes and alternate startup analysis.

### H. Normal smoke suite

Run:

```bat
cmd /c scripts\smoke-test.cmd
```

Use the normal happy-path suite. Do not expand it into a long campaign.

### I. Diff review

Run:

```bat
cmd /c git diff --check
cmd /c git status --short
```

Review every changed/untracked file.

Remove temporary test files, screenshots, generated debug output, and local artifacts that do not belong in the commit.

## Commit and push authorization

When all required tests are green:

1. Stage **all reviewed unstaged and untracked repository files** belonging to this milestone.
2. Include any pre-existing user-authored repository files only after reviewing them and confirming they are intended repository work.
3. Do not include unrelated machine-local artifacts.
4. Create one coherent commit.
5. Push normally to the current upstream branch.

Commit subject must begin exactly:

```text
Sin and Codex:
```

Recommended subject:

```text
Sin and Codex: fix(visual-studio): complete multi-file debugging and project UI
```

Use a detailed body:

```text
Summary:
- ...

Changes:
- ...

Validation:
- ...

Known limitations:
- ...
```

Do not amend, rebase, force-push, or rewrite pushed history.

## If tests fail

Do not commit a broken milestone.

Continue diagnosing autonomously when possible.

Ask Sin only when a true external blocker prevents further progress.

## Required final report contents

Report:

- baseline and final commit hashes;
- confirmed root causes;
- files changed;
- debugger architecture changed;
- context menus/commands added;
- project XML behavior;
- workspace hardening;
- VSIX version and path;
- focused automated test results;
- live F10 test result;
- live Snake startup-selection result;
- Windows and Web results;
- smoke-suite result;
- commit hash;
- push result;
- known limitations.

End with the required bold manual-testing line.
