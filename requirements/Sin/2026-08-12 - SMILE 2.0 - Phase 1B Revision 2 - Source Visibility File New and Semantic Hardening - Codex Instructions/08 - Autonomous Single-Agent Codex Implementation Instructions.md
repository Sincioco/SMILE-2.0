# Autonomous Single-Agent Codex Implementation Instructions

## Operating rules

1. Work only in `D:\SMILE 2.0`.
2. Use one agent only.
3. Read root `AGENTS.md`.
4. Read all numbered files in this package before editing.
5. Inspect the latest repository; it may be newer than the reviewed baseline.
6. Preserve unrelated user work.
7. Do not ask for intermediate confirmation.
8. Assume the happy path and use focused testing.
9. Install and test the refreshed VSIX.
10. Commit and push only after all mandatory checks are green.

---

# Implementation order

## A. Reproduce and diagnose hierarchy invisibility

Use Snake and inspect:

```text
physical file
project XML
SmileProjectSourceSet.Items
hierarchy model
root child traversal
Visual Studio notifications
restart behavior
```

Record the confirmed cause.

## B. Fix hierarchy projection/persistence

Make every project source visible exactly once on initial load and live mutation.

## C. Implement locked names and File > New template

Use exactly:

```text
New SMILE 2.0 Source Code
SMILE 2.0 Source Code
Add Existing SMILE 2.0 Source Code...
```

## D. Clean context menus

Remove Connected Services and New EditorConfig from the SMILE project menu.

## E. Complete semantic hardening

Order-independent constants, cycles, unified namespace.

## F. Complete workspace/governance hardening

Buffer unregistration, multi-project ownership, root AGENTS update.

---

# Testing responsibility

Codex must personally perform the live Visual Studio sequence in `09`.

Do not claim success from:

- seeing an XML entry;
- seeing `SourceSet.Items`;
- opening the file in an editor;
- inspecting `Commands.vsct`;
- an Add Existing duplicate message.

The source must be visibly present in Solution Explorer immediately and after restart.

---

# Commit and push

When green:

```bat
cmd /c git status --short
cmd /c git diff --check
cmd /c git add -A
```

Review the staged diff.

Commit subject must begin exactly:

```text
Sin and Codex:
```

Suggested subject:

```text
Sin and Codex: fix(visual-studio): make added SMILE sources visible
```

Use a detailed body with Summary, Changes, Validation, and Known limitations.

Push normally. Do not amend, rebase, force-push, or discard user work.

The final report must end with one bold manual-testing line.
