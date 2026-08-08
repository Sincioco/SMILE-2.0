# START HERE — SMILE 2.0 Game Language and Four Games Mission

## Mission

Evolve the working SMILE 2.0 native compiler and Visual Studio 2026 extension into a beginner-friendly native game-programming environment.

Compile four complete graphical games written in SMILE:

1. **SMILE Snake**
2. **SMILE Falling Blocks**
3. **SMILE Paddle Ball**
4. **SMILE Brick Breaker**

All four must compile into standalone native Windows x64 executables.

## Read order

1. `AGENTS.md`
2. `00 - START HERE - SMILE 2.0 Game Language and Four Games Mission.md`
3. `01 - Approved Decisions Scope and Non-Goals.md`
4. `02 - SMILE 2.0 Game Language Specification.md`
5. `03 - Native Game Runtime Graphics Input Audio and Storage.md`
6. `04 - Visual Studio SMILE Project System and Templates.md`
7. `05 - SMILE Snake Game Specification.md`
8. `06 - SMILE Falling Blocks Game Specification.md`
9. `07 - SMILE Paddle Ball Game Specification.md`
10. `08 - SMILE Brick Breaker Game Specification.md`
11. `09 - Repository Structure Solution Rename and Public Documentation.md`
12. `10 - Implementation Plan Commit Sequence and Velocity Rules.md`
13. `11 - Definition of Done Smoke Tests and Manual Validation.md`
14. `12 - Technical References.md`

Read all files before implementation, then execute them in milestone order.

## Locked facts

```text
Project:           SMILE 2.0
Local folder:      D:\SMILE 2.0
GitHub repository: Sincioco/SMILE-2.0
Platform:          Windows 11 x64
IDE:               Visual Studio 2026 Enterprise
Compatibility:     No SMILE 1.0 requirement
```

## Locked decisions

- Real native graphics, not console characters, for new game projects.
- Default logical canvas: **960 × 540**.
- Exact 2× scaling at 1920×1080 and 4× at 3840×2160.
- Windowed by default; automatic Alt+Enter borderless full-screen.
- Automatic scaling, redraw, aspect ratio, DPI, and resize handling.
- Win32 plus a small double-buffered native graphics runtime.
- Explicit `KEY_` constants.
- Original asynchronous WAV effects.
- Persistent integer scores.
- Preserve the original console Snake as `examples\ConsoleSnake.smile`.
- Public names: SMILE Snake, SMILE Falling Blocks, SMILE Paddle Ball, SMILE Brick Breaker.
- One `.smile` startup source file per Visual Studio project for this milestone.
- Shapes, colors, and text now; images and sprites later.
- Rename the solution to `SMILE 2.0.sln`.
- Add Visual Studio templates for SMILE Console Application and SMILE Game Application.

## Mandatory architecture

```text
                         Smile.Language
                               |
              +----------------+----------------+
              |                                 |
              v                                 v
       Smile.Compiler                   Smile.VisualStudio
              |                                 |
              v                                 v
     Native code generation          Editing + project support
              |
              v
       Smile.NativeRuntime
              |
              v
          Native games
```

All game rules live in SMILE source. The runtime supplies generic OS services only.

## Complete only when

- `SMILE 2.0.sln` builds.
- VSIX installs.
- Both project templates create, build, and run.
- Console examples still compile.
- All four graphical games compile to native x64 executables.
- All four are playable.
- Resizing and Alt+Enter work.
- WASD and arrow controls work as specified.
- Sound, scoring, persistence, retry, and exit work.
- Repository is clean, committed, and pushed.
