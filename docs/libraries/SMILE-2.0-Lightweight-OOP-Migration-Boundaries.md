# SMILE 2.0 Lightweight OOP Library Migration Boundaries

**Status:** Approved library migration policy

## Purpose

Define which current official libraries should adopt the first object model and which proven module/handle APIs remain unchanged.

OOP is applied where it improves real call sites, not uniformly.

# Smile.UI

## Completed Migration

Current version:

```text
Smile.UI 1.1.3
```

The current package uses generation-safe handle engines for Menu, MenuNavigator, Dialogue, and other resources.

## Approved Migration

- keep style/configuration records as Types;
- use `With` for repeated style setup;
- expose Menu as a Class façade over the existing handle engine;
- use constructor, instance methods, properties, optional/named arguments;
- expose explicit idempotent `Destroy()` for the underlying slot resource;
- prefer one spanning `Smile.UI.Menu` Module for Menu and MenuNavigator Classes/private helpers;
- migrate Dialogue only as a small façade;
- keep Window/Text service Modules;
- review BitmapFont separately rather than forcing migration.

## Final Public Shape

Ordinary user code should no longer pass a Number Menu handle.

```smile
Import Smile.UI.Menu As Menus

Dim RootMenu As New Menus.Menu(
    MenuStyle,
    X:=200,
    Y:=250,
    Width:=600,
    Height:=200,
    VisibleRows:=5
)

StartIndex = RootMenu.AddItem("Start", 1)
RootMenu.SelectedIndex = StartIndex
```

## Version

Recommended:

```text
Smile.UI 2.0.0
```

Remove/privatize old procedural public APIs after all repository consumers migrate.

# Smile.Game

## Current Baseline

Current reviewed version:

```text
Smile.Game 2.0.0
```

Modules:

```text
Core
Animation
TileMap
Camera2D
Collision2D
```

## Applied Migration

### Convert

- cardinal direction constants to a nominal enum where signatures benefit;
- `CardinalMover` first-ByRef operations to Type methods;
- `CameraState` first-ByRef operations to Type methods.

### Preserve

- Animation generation-safe handle Module;
- TileMap generation-safe transactional handle Module;
- Collision2D stateless Module;
- asset-free/UI-free package policy;
- no dependency from Smile.RPG.

## Version

Shipped:

```text
Smile.Game 2.0.0
```

Update every world/dungeon/battle gallery and project/package consumer.

# Smile.RPG

## Current Baseline

Current reviewed version:

```text
Smile.RPG 1.2.0
```

The package contains fifteen bounded, generation-safe, transactional Modules covering management, world, story, encounters, saves, and battles.

## Approved Migration

Do not redesign the public API during the lightweight OOP milestone.

Required only:

- compile under the new language implementation;
- rebuild as `.smilelib` format 6;
- preserve every public module/signature/result code;
- preserve all transactional/query/rollback/invariant behavior;
- run all current native/Web/project/package/galleries/tests.

Do not:

- create `RpgState` as a milestone blocker;
- replace result/item/target constants with enums;
- add Smile.UI or Smile.Game dependencies;
- add battler/action/effect inheritance.

## Version

Recommended package-only patch:

```text
Smile.RPG 1.2.1
```

Adjust if the current version advances before implementation.

# `.smilelib` Format

All official libraries rebuild in package format 6.

Format 6 adds metadata for:

- enums/members;
- Type members/properties;
- Classes/constructors/members/properties;
- optional/default arguments;
- source locations;
- visibility;
- exact provider identity;
- `requiresGameWindow`.

Old formats 1-5 are rejected with a rebuild instruction.

Determinism, stable entry order, fixed timestamps, normalized source, exact hashes, and no absolute paths remain mandatory.

# Application-Local Modules

Application-local Modules remain valid even when Classes exist.

`SinStarI.TitleScreen` is the canonical current example:

- one application singleton;
- private assets/state;
- public lifecycle/input/draw routines.

It should remain a Module. Its action constants may become an enum.

# Decision Guide

Use a Module when:

- behavior is stateless;
- one singleton is intentional;
- the API coordinates global bounded storage;
- an existing transactional engine is proven and an object façade adds little.

Use a Type when:

- identity is unnecessary;
- copy semantics are meaningful;
- the current pattern repeatedly passes the value as first `ByRef` argument.

Use a Class when:

- multiple independent stateful instances are meaningful;
- identity/lifetime matter;
- instance dot syntax materially improves user code;
- a small façade can hide a generation-safe handle.

# Explicitly Deferred Library Work

- Animation Class façade;
- TileMap Class façade;
- BitmapFont Class façade;
- broad RPG State façade;
- public RPG enum redesign;
- renderer/entity inheritance hierarchy;
- package dependency restructuring.

These require separate evidence-based design reviews.
