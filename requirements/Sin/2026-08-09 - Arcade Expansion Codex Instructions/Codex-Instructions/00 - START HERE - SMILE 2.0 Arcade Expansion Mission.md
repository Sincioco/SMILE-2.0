# START HERE — SMILE 2.0 Arcade Expansion Mission

**Repository:** `Sincioco/SMILE-2.0`  
**Local repository:** `D:\SMILE 2.0`  
**Verified starting commit:** `5a9f4050ee5e1d1a2e68cbb957f6acd37baf1ada`

This package contains the approved Codex instructions for:

1. Permanent SMILE 2.0 development-governance updates.
2. Dungeon Star I map loading, student sample maps, pipe-like generation, a blue title palette, and a five-second attract-mode delay.
3. A shared arcade demo-mode behavior contract.
4. Demo modes for Brick Breaker, Falling Blocks, Snake, and Paddle Ball.
5. A new original Pac-Man-style maze-chase sample.
6. A new original Galaga-style full-16:9 space-shooter sample.
7. Fast validation, commit, and delivery rules.

The user’s item 10 said “FallingBlocks” a second time under the **Snake — Demo Mode** heading. Treat that as an obvious wording error. Item 10 applies to **Snake**.

## Public game names

The commercial games are visual and mechanical references only. Preserve SMILE’s existing original-branding policy.

Use these public SMILE sample names:

```text
User shorthand             Public SMILE project
Pack-Man / Pac-Man style   Maze Muncher
Galaga style               Star Squadron
```

Create:

```text
games\MazeMuncher
games\StarSquadron
```

Do not use commercial logos, maze layouts, sprites, sounds, music, names, or source code.

## Read and execution order

Codex must first read:

```text
AGENTS.md
```

Then read all files in this package before modifying the repository. Execute them in this order:

```text
01 - Permanent Governance Addendum...
02 - Dungeon Star I...
03 - Dungeon Star I Map Authoring Guide...
04 - Shared Arcade Demo Mode Contract...
05 - Brick Breaker Demo Mode...
06 - Falling Blocks Demo Mode...
07 - Snake Demo Mode...
08 - Paddle Ball Demo Mode...
09 - Pac-Man-Style Maze Muncher...
10 - Galaga-Style Star Squadron...
11 - Definition of Done...
```

Do not implement the entire package as one unreviewable change. Use coherent, validated milestones and push each completed milestone.

## Baseline facts to preserve

At the verified starting commit:

- Dungeon Star I already exists as a complete `.smile` game.
- `FILL QUADRILATERAL`, `DRAW QUADRILATERAL`, and `KEY_OTHER` already exist.
- MP3 background music already uses `Windows.Media.Playback.MediaPlayer`.
- The shared native runtime already tracks activation/minimization and silences MP3 and WAV audio while inactive.
- DirectX and GDI backends are both supported.
- All current game rules remain in `.smile` source.
- The shared smoke suite passes.

Do not reimplement completed features. Extend and formalize them.

## Permanent design authority

Sin authorizes language evolution where it improves clarity or makes the samples practical.

Use this decision order:

1. Express the feature with current SMILE syntax when the result remains understandable.
2. If a language addition is warranted, prefer a recognizable BASIC precedent.
3. If BASIC has no clean precedent, use the smallest clear C#-inspired idea.
4. Add a generic language/runtime capability, never a helper that secretly implements one game.
5. Update the shared lexer/parser/semantics/compiler and public documentation together.

The map milestone introduces one justified generic file-input statement:

```smile
LOAD TEXT FILE "Maps\default.map" INTO MapBytes COUNT MapByteCount
```

No `DEMO MODE`, `DRAW MAZE`, `PACMAN AI`, `GALAGA AI`, or dungeon-specific native statement is approved. Demo intelligence and game rules stay in `.smile`.

## Velocity rule

Assume the happy path.

Default validation must be brief:

- targeted compiler/language checks;
- the normal smoke suite;
- one short manual happy-path run per changed game/backend where needed;
- one normal attract-mode cycle, measured in seconds.

Do not run 30-minute soak tests, huge seed sweeps, exhaustive playthroughs, or broad performance campaigns by default.

Longer testing is allowed only when investigating:

- a known bug;
- a crash or hang;
- a suspected leak;
- a measured performance regression;
- a benchmark;
- a timing defect that requires real elapsed time;
- a bug that reproduces only after sustained execution.

## Artifact-delivery rule

When ChatGPT or Codex produces more than one Markdown instruction/specification file for Sin:

- provide the individual files;
- also package them in one ZIP archive;
- include any companion sample files in the same archive;
- preserve intended repository-relative paths where practical.

This ZIP package already follows that rule.

## Repository file copies

The ZIP contains ready-to-copy repository files under:

```text
Repository-Files\
```

Codex must add:

```text
games\DungeonStarI\MAP_AUTHORING.md
games\DungeonStarI\Maps\default.map
games\DungeonStarI\Maps\sample-loops.map
games\DungeonStarI\Maps\sample-switchbacks.map
```

Codex may adjust a sample map only if required by the final implemented parser or validator. Preserve its educational purpose and document any format change.

## Stop condition

Do not report completion after planning or compilation alone. Each milestone must build, receive its light happy-path validation, be committed with a detailed message, and be pushed.
