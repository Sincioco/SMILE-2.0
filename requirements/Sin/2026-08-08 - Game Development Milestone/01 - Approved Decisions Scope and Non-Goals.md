# Approved Decisions, Scope, and Non-Goals

## Product direction

SMILE 2.0 is a fun, beginner-friendly, BASIC-rooted native compiled language. When BASIC has no good precedent, prefer a simple C#-like idea.

## Public game projects

```text
games\Snake            SMILE Snake
games\FallingBlocks    SMILE Falling Blocks
games\PaddleBall       SMILE Paddle Ball
games\BrickBreaker     SMILE Brick Breaker
```

Use original visuals, code, sounds, and branding. Reference images are inspiration only.

## Standard display

```text
Logical resolution: 960 × 540
Aspect ratio:       16:9
Default mode:       Windowed
Full-screen toggle: Alt+Enter
1080p scaling:      Exact 2×
4K scaling:         Exact 4×
```

`GAME WINDOW "Title"` uses 960×540 automatically.

## Approved language additions

- constants;
- `*`, `/`, and `MOD`;
- underscores in identifiers;
- two-dimensional fixed arrays;
- `SUB`, `CALL`, `FUNCTION`, and `RETURN`;
- `SELECT CASE`;
- unconditional `DO ... LOOP`;
- `EXIT FOR`, `EXIT DO`, and `END PROGRAM`;
- native game window;
- rectangles, rounded rectangles, circles, lines, text, and numbers;
- named and RGB colors;
- frame presentation;
- timer and window-close functions;
- key events and held-key input;
- explicit key constants;
- asynchronous WAV playback;
- persisted integer values.

## Visual Studio scope

Add:

```text
SMILE 2.0 Console Application
SMILE 2.0 Game Application
```

Support project creation, Solution Explorer, highlighting, shared diagnostics, Build, Run, asset copying, Error List, and SMILE Output.

## Non-goals

Do not add:

- SMILE 1.0 compatibility;
- Linux, macOS, ARM64, LLVM, DirectX, SDL, MonoGame, Unity, or Godot;
- PNG/JPEG/sprite loading;
- MP3, music mixing, mouse, controller, networking, 3D, or physics;
- multiple source files per project;
- classes, inheritance, interfaces, generics, lambdas, async, or exceptions;
- floating point;
- source-level debugger;
- CI or broad test frameworks;
- advanced falling-block rules, power-ups, or particle systems.
