# Permanent Reusable Components and RPG Architecture Contract

## Status

This document records approved decisions that apply to Phase 1 and all later reusable-component/RPG phases unless Sin explicitly changes them.

SMILE 2.0 is still under active development. There are no external users whose source compatibility must be protected at the expense of a better language. Correct, clear evolution is preferred over preserving an early design mistake.

---

# 1. Platform priorities

## Priority 1 — Windows native games

Windows remains SMILE 2.0’s reference and primary target:

- native Windows x64 `.exe`;
- MASM x64 backend;
- DirectX/Direct2D/DirectWrite primary graphics path;
- GDI fallback;
- Visual Studio 2026 Enterprise integration;
- source-level `.smile` breakpoints;
- F5 and Ctrl+F5;
- IntelliSense and diagnostics;
- windowed and borderless full-screen operation.

## Priority 2 — Web games

The working Web target remains a first-class secondary target:

- the same SMILE source;
- the same project source set;
- Canvas 2D today, with WebGL available for later generic capabilities where useful;
- browser keyboard, audio, asset, timing, and persistence services;
- static publish output;
- Visual Studio Web build and browser launch.

Each new general-purpose language/runtime feature must be designed for both targets in the same phase unless the phase specification explicitly records a narrow temporary limitation. Windows is implemented and validated first; Web follows before the phase closes.

Do not create separate Windows and Web dialects of SMILE.

---

# 2. One language authority

`src\Smile.Language` remains the single authority for:

- source documents;
- tokens and keyword facts;
- syntax trees;
- parser rules;
- diagnostics;
- symbols and types;
- semantic analysis;
- completion data and other shared language services.

The MASM emitter, Web emitter, command-line compiler, tests, and Visual Studio extension consume that shared result.

Never solve a target problem by creating:

- a Web-only parser;
- a compiler-only symbol table with different rules;
- an editor-only semantic model;
- duplicate keyword catalogs;
- target-specific language syntax with the same meaning.

---

# 3. Language evolution and legacy migration

Backward compatibility with early SMILE 2.0 syntax is not sacred while the language has no external users.

A language change is allowed when it makes SMILE:

- clearer for beginners;
- more internally coherent;
- more reusable;
- more capable of supporting future games;
- easier to teach;
- more consistent across Windows and Web;
- less dependent on game-specific workarounds.

When an approved change affects existing code, Codex must update all affected repository content in the same coherent milestone:

- the ten legacy games;
- every `Program-NoDemo.smile` teaching source;
- examples;
- templates;
- tests;
- documentation;
- tutorials when directly affected;
- Windows and Web build paths.

Do not leave compatibility shims, aliases, or duplicate spellings merely to avoid updating repository-owned source unless the current phase explicitly requires a transition period.

The ten existing games are now part of SMILE 2.0’s legacy and proof suite:

1. Snake.
2. Falling Blocks.
3. Paddle Ball.
4. Brick Breaker.
5. Dungeon Star I.
6. Dungeon Star II.
7. Maze Muncher.
8. Star Squadron.
9. Platform Quest.
10. Sky Hopper.

They may be rewritten to use better language features, but they must remain playable and continue proving the language’s core capabilities.

---

# 4. Reusable components are written in SMILE

The long-term goal is to let a programmer build and improve reusable game components independently from the games that consume them.

Examples include:

```text
Smile.Game.Scene
Smile.Game.Input
Smile.Game.Tween
Smile.Game.Animation
Smile.Game.Sprite
Smile.Game.TileMap
Smile.Game.Camera2D
Smile.Game.Actor

Smile.UI.Window
Smile.UI.Menu
Smile.UI.Dialogue
Smile.UI.BitmapFont

Smile.RPG.Character
Smile.RPG.Party
Smile.RPG.Inventory
Smile.RPG.Equipment
Smile.RPG.Abilities
Smile.RPG.Shop
Smile.RPG.Encounter
Smile.RPG.BattleCore
Smile.RPG.BattleStrategy
Smile.RPG.BattleView
Smile.RPG.FirstPersonDungeon
Smile.RPG.Save
Smile.RPG.Events
```

These components must be implemented in SMILE source and compiled into games. They must not become genre-specific native runtime functions or compiler keywords.

The runtime may add only generic services such as:

- images and source-region drawing;
- clipping;
- audio channels;
- input;
- timing;
- storage blocks;
- asset loading;
- target-neutral graphics operations.

The game/component layer owns:

- menus;
- inventory rules;
- characters;
- equipment;
- abilities;
- battle formulas;
- encounters;
- story flags;
- dungeon events;
- shops;
- party management.

---

# 5. Approved RPG terminology

The reusable ability library is named:

```text
Smile.RPG.Abilities
```

Do not name the general library `Smile.RPG.Techniques`.

The character resource is:

```text
Magic Points
MP
CurrentMP
MaximumMP
MPCost
RestoreMP
```

Do not use Technique Points or TP as the general resource name.

An ability may still be described as a spell, technique, skill, special move, enemy ability, or item effect. The general component is `Abilities`, and resource-consuming abilities normally consume MP.

---

# 6. Approved RPG inspiration and originality

The planned game combines broad design ideas:

- Phantasy Star II-inspired top-down world presentation, party systems, windowed menus, rear-facing party battle composition, and repeatable battle strategy;
- Phantasy Star I-inspired first-person dungeon exploration and illustrated/environmental battle backgrounds.

The implementation must remain an original SMILE 2.0 game with original:

- title;
- characters;
- story;
- world;
- maps;
- enemies;
- art;
- music;
- dialogue;
- names;
- data.

Do not copy Sega game assets, maps, scripts, characters, music, or other protected content.

---

# 7. Additional sound-effect channels are approved

Multiple sound-effect channels are part of the approved roadmap and must be implemented in the later cross-platform media phase before the battle-system phase.

The future system must:

- preserve the existing simple `Play Sound` form through a default channel or an intentional repository-wide migration;
- allow explicit independent effect channels;
- work in Windows and Web;
- remain separate from the one background-music stream;
- apply the shared focus-loss policy to all active effect channels;
- never replay suppressed effects after focus returns.

Phase 1 must not implement the channel syntax/runtime yet. It must avoid architectural decisions that would prevent it.

---

# 8. Approved gated roadmap

Each phase is implemented, committed, pushed, inspected, and reassessed before the next detailed Codex package is written.

## Phase 1 — True multi-file compilation

- Multiple source documents in one compilation.
- One selected startup source.
- Support source files.
- Cross-file symbols and diagnostics.
- Native and Web emission.
- Project-wide IntelliSense.
- Multi-file Windows breakpoints.

## Phase 2 — Modules, imports, and SMILE library projects

Planned concepts:

```text
Module
End Module
Import
As
Public
Private
Option Explicit
.smilelibproj
.smilelib
ProjectReference
```

Exact syntax is not implemented until Phase 1 is inspected.

## Phase 3 — Component-friendly types and text

Planned concepts include:

- scalar declarations with `As`;
- `Number`, `Boolean`, and proper `Text` values;
- user-defined record-style `Type`;
- field access;
- arrays of records;
- `ByRef` and `ByVal`;
- more than four parameters;
- proper local scope.

## Phase 4 — Cross-platform media/runtime primitives

Planned capabilities include:

- PNG/image loading;
- image-region/sprite-sheet drawing;
- transparency and scaling;
- clipping;
- text measurement;
- persistent data blocks;
- asset preload/validation;
- multiple sound-effect channels.

## Phase 5 — Reusable UI components

```text
Smile.UI.Window
Smile.UI.Menu
Smile.UI.Dialogue
Smile.UI.BitmapFont
MenuGallery
```

## Phase 6 — RPG data and management components

```text
Characters
Party
Inventory
Equipment
Smile.RPG.Abilities
MP
Shops
Save games
```

## Phase 7 — Top-down world components

```text
Sprites
Animation
Tile maps
Camera
Actors
NPCs
Triggers
Encounters
```

## Phase 8 — Reusable first-person dungeon component

Extract and generalize the appropriate Dungeon Star I systems without breaking the legacy game.

## Phase 9 — Battle system

- battle mechanics;
- MP-based abilities;
- repeatable per-character strategy;
- party viewed from behind;
- enemy and party animation;
- environment-specific battle backgrounds;
- first-person dungeon corridor battles;
- overlapping sound effects.

## Phase 10 — Complete RPG vertical slice

One finishable chapter with town, overworld, first-person dungeon, party, inventory, shop, save/load, encounters, boss, and ending.

## Phase 11 — Complete native/Web parity and publication review

Web is tested throughout every phase; this final gate verifies the finished vertical slice as one product on both targets.

---

# 9. Phase-gate rule

Do not create or implement the detailed next phase while completing the current phase.

At the end of each phase:

1. Commit and push the coherent result.
2. Report exact behavior and remaining limitations.
3. Let Sin/ChatGPT inspect the actual committed repository.
4. Adjust the next phase to the real architecture rather than an old prediction.

This is not hesitation. It is how the project avoids compounding foundational mistakes.

---

# 10. Component design rules for later phases

Reusable components should generally:

- expose initialization, update, draw, enter, exit, and reset operations as appropriate;
- avoid owning the application game loop;
- keep update logic separate from drawing;
- return event/status values to the game;
- use fixed capacities initially where practical;
- have independent samples and documentation;
- work from the same source on Windows and Web;
- avoid platform checks in ordinary component logic;
- keep game content/data outside generic component implementations.

These principles are recorded now so Phase 1’s compilation model remains compatible with them.
