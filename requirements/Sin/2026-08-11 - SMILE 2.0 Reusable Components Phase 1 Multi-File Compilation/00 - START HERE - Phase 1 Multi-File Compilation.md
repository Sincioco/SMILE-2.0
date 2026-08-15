# START HERE — SMILE 2.0 Reusable Components, Phase 1

## True Multi-File Compilation Foundation

**Repository:** `D:\SMILE 2.0`  
**GitHub:** `Sincioco/SMILE-2.0`  
**Package date:** August 11, 2026  
**Repository baseline inspected while preparing this package:** `d87ffbe677fb52fe1c9b2cbcdc3a5ec42b10da41`  
**Last completed Web-target implementation commit beneath that documentation commit:** `ee2874524c8b1a3dd2da08a776d728b47589146d`

This package starts the approved long-term plan for reusable SMILE 2.0 game libraries and the future original science-fantasy RPG.

The first implementation phase is deliberately narrow:

> Make one SMILE 2.0 program compile from several `.smile` source files while preserving one startup source, one shared semantic model, Windows native output, Web output, Visual Studio IntelliSense, diagnostics, normal file opening, and Windows `.smile` breakpoints.

This phase does **not** yet add `Module`, `Import`, `.smilelib`, user-defined `Type`, mutable `Text`, images, inventory, menus, MP abilities, or multiple sound-effect channels. Those are later gated phases. Phase 1 establishes the compiler, emitter, debugger, and project-system foundation they require.

---

## Required reading order for Codex

Read every file completely in this order before editing code:

1. Repository-root `AGENTS.md`.
2. `00 - START HERE - Phase 1 Multi-File Compilation.md`.
3. `01 - Permanent Reusable Components and RPG Architecture Contract.md`.
4. `02 - Phase 1 Multi-File Compilation Formal Specification.md`.
5. `03 - Autonomous Single-Agent Codex Implementation Instructions.md`.
6. `04 - Windows Web Visual Studio Acceptance and Regression Matrix.md`.
7. `05 - Completion Report Commit Push and Next-Phase Gate.md`.
8. The companion files under `Repository-Files\examples\MultiFileBasics`.
9. `MANIFEST.json` for package integrity.

The numbered documents are complementary. Later files do not override earlier permanent requirements unless they explicitly say so.

---

## How to place this package in the repository

Preserve the complete package in the repository under a requirements folder similar to:

```text
requirements\Sin\2026-08-11 - SMILE 2.0 Reusable Components Phase 1 Multi-File Compilation\
```

If Sin already copied or extracted the package somewhere else inside the repository, do not create a duplicate. Use the existing repository copy.

Copy the companion sample from:

```text
Repository-Files\examples\MultiFileBasics\...
```

into:

```text
examples\MultiFileBasics\...
```

unless the latest repository already contains an equivalent or better sample. The repository version may be adjusted during implementation to match the final compiler/project design, but preserve the supplied sample’s teaching purpose and cross-file coverage.

---

## Autonomous execution rule

Codex must implement this phase from beginning to end using **one agent only**.

Do not spawn subagents, delegate to parallel agents, or ask Sin to supervise intermediate work. Sin may be asleep or away from the computer.

Proceed autonomously through:

```text
repository inspection
    -> design adjustment to current code
    -> implementation
    -> focused tests
    -> build
    -> native compilation
    -> Web publication
    -> Visual Studio validation where safely possible
    -> documentation
    -> commit
    -> push
    -> final report
```

Ask Sin a question only if a genuine external blocker remains after reasonable independent attempts and no safe implementation path exists. A preference question, ordinary compiler error, failed test, code-design decision, or need to inspect more files is not a blocker.

If a test reveals a defect, diagnose and fix it rather than asking Sin what to do.

---

## Commit and push authorization

When the required automated/focused tests are green:

1. Review `git status` and the final diff.
2. Preserve all user work.
3. Stage all non-ignored modified, deleted, and untracked repository files with `git add -A` after confirming that no secret, credential, machine-local artifact, or accidental build output is being added.
4. Commit the complete coherent milestone.
5. Push normally to the current branch/upstream.
6. Do not amend, rebase, force-push, or rewrite public history.
7. End with a clean working tree unless a clearly identified external file cannot safely be committed.

Every Codex commit subject must begin exactly with:

```text
Sin and Codex:
```

A recommended subject for this phase is:

```text
Sin and Codex: feat(language): add true multi-file SMILE compilation
```

---

## Manual-testing report rule

Codex must perform everything it can itself. Manual testing by Sin must never be used as an excuse to stop before commit and push when the automated and Codex-controlled checks are green.

At the very end of the final report, include exactly one bold heading:

```text
**MANUAL TESTING REQUESTED From SIN:** None.
```

or, when a genuinely human-only or environment-blocked check remains:

```text
**MANUAL TESTING REQUESTED From SIN:**
- Exact step to perform.
- Exact expected result.
- Why Codex could not perform it safely itself.
```

Make that section visually prominent and place it after the commit/push report.

---

## Mission success in one sentence

Phase 1 is complete when `Program.smile`, `GameState.smile`, and `Drawing.smile` can form one project and one program on both Windows and Web; cross-file symbols work; support-file errors report the correct file; Windows breakpoints can stop inside a support file; and every legacy game still builds and runs through its selected startup source.

Do not begin Phase 2 (`Module`, `Import`, and SMILE libraries) in this milestone. Commit Phase 1, report it, and wait for Sin/ChatGPT to inspect the real repository before the next package is prepared.
