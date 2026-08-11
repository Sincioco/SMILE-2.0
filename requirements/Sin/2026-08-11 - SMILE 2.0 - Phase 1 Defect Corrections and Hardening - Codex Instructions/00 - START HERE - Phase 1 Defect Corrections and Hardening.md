# SMILE 2.0 Phase 1 Defect Corrections and Hardening

## START HERE

**Repository:** `D:\SMILE 2.0`
**GitHub repository:** `Sincioco/SMILE-2.0`
**Reported Phase 1 commit:** `49b89cd77d2167c9fe3f3d0b5fd4ca150a70b419`

This package is a **corrective and hardening milestone for Phase 1**.

It is **not Phase 2**. Do not begin modules, imports, `.smilelib`, library projects, reusable menu components, RPG systems, image support, or multiple sound channels until this corrective milestone is complete and accepted.

The purpose is to close the user-visible defects and architecture gaps discovered after the first multi-file implementation.

## Confirmed user-visible defects

### 1. Native breakpoint stops, but F10 loses source

A breakpoint in `GameState.smile` binds and Visual Studio stops on it. When the user presses `F10`, Visual Studio opens:

```text
Source Not Available
Source information is missing from the debug information for this module.
```

The supplied screenshot is included at:

```text
Evidence\Source Not Available after F10.png
```

Breakpoint binding alone is not sufficient. Normal source-level stepping must remain in the real `.smile` files.

### 2. SMILE project hierarchy nodes have no context menus

In Solution Explorer:

- right-clicking the SMILE project node shows no menu;
- right-clicking `Program.smile` shows no menu;
- right-clicking `Program-NoDemo.smile` shows no menu;
- right-clicking a project-owned folder shows no menu;
- only the Solution node responds normally.

This prevents ordinary Visual Studio workflows such as:

- adding a new `.smile` support file;
- adding an existing `.smile` support file;
- selecting `Program-NoDemo.smile` as the startup file through the UI;
- identifying which source is the current startup source;
- removing a source from the project.

## Additional Phase 1 hardening included

The current multi-file workspace also needs hardening so that:

- unsaved edits in one open `.smile` file are visible to IntelliSense and diagnostics in the other open files of the same project;
- a non-selected `StartupOnly="true"` source such as `Program-NoDemo.smile` can still be analyzed as an alternate startup plus the ordinary support sources;
- source-set changes made through the UI immediately refresh hierarchy, workspace, diagnostics, IntelliSense, Windows builds, and Web builds;
- changing the startup source cannot launch a stale executable;
- project source entries are validated as real `.smile` files with normalized, portable project-relative paths;
- automated checks do not mistake “breakpoint binds” for “source stepping works.”

## Reading order

Read all files completely in this order:

1. `00 - START HERE - Phase 1 Defect Corrections and Hardening.md`
2. `01 - Phase 1 Report Findings and Corrective Scope.md`
3. `02 - Native SMILE Source Stepping Correction.md`
4. `03 - Visual Studio Project Context Menus Add Source and Startup UI.md`
5. `04 - Multi-File Workspace and Project State Hardening.md`
6. `05 - Autonomous Single-Agent Codex Execution Instructions.md`
7. `06 - Acceptance Regression Commit Push and Final Report.md`

Also read the current repository-root `AGENTS.md` completely before editing.

The repository may be newer than the reported Phase 1 commit. Inspect the current files and latest commit before choosing exact implementation details.

## One-agent rule

Use **one Codex agent only** for the entire milestone.

Do not create subagents. Do not delegate compiler/debugger work, Visual Studio work, testing, or documentation to another agent. One agent must retain full context through diagnosis, implementation, validation, commit, and push.

## Autonomous execution rule

Continue without asking Sin to confirm intermediate steps:

```text
inspect latest repository
-> reproduce the defects
-> confirm root causes
-> implement the corrections
-> build
-> install the refreshed VSIX
-> perform focused Visual Studio tests
-> run the normal smoke suite
-> stage all reviewed repository changes
-> commit
-> push
-> report
```

Ask Sin only when a true external blocker makes progress impossible.

## Validation style

Assume the happy path and use focused, proportional testing.

The reported debugger and project-UI defects justify targeted live Visual Studio testing. Do not run long soaks or unrelated stress campaigns.

Before any unusually long or broad test, record:

```text
Known problem being investigated:
Why the longer test is necessary:
Stop condition:
```

## Completion gate

Do not claim completion until all of these are true:

- startup-file and support-file breakpoints bind and hit;
- repeated `F10` advances through real `.smile` source without Source Not Available;
- project, file, and relevant folder nodes have useful context menus;
- a support `.smile` file can be added through the Visual Studio UI;
- `Program-NoDemo.smile` can be selected and run through the Visual Studio UI;
- the startup source is visibly identifiable;
- newly added/changed source sets immediately affect IntelliSense, diagnostics, Windows, and Web;
- all ten legacy games remain green;
- the refreshed VSIX is installed and tested;
- required tests pass;
- changes are committed and pushed.

At the very end of the final report, print exactly one bold line:

```text
**MANUAL TESTING REQUESTED FROM SIN: NONE.**
```

or:

```text
**MANUAL TESTING REQUESTED FROM SIN: <exact short checklist>.**
```
