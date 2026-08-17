# SMILE 2.0 Lightweight OOP Library Migration Boundaries

**Status:** Implemented lightweight-OOP migration policy

## Purpose

Define which current official libraries should adopt the first object model and which proven module/handle APIs remain unchanged.

OOP is applied where it improves real call sites, not uniformly.

# Smile.UI

## Current Result

Current shipped version:

```text
Smile.UI 2.0.0
```

The package retains generation-safe handle engines internally while exposing Class façades for Menu, MenuNavigator, and Dialogue. Window and Text remain service Modules, and style/configuration records remain Types.

## Applied Migration

- style/configuration records remain Types;
- repeated style setup uses `With` where it improves readability;
- Menu is a Class façade over the existing handle engine;
- constructors, instance methods, properties, and optional/named arguments form the public object API;
- explicit `Destroy()` is idempotent for the underlying slot resource;
- one spanning `Smile.UI.Menu` Module lets the Menu and MenuNavigator Classes share private helpers;
- Dialogue is a small Class façade;
- Window, Text, and BitmapFont remain service/handle Modules.

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

Shipped:

```text
Smile.UI 2.0.0
```

Obsolete procedural Menu, MenuNavigator, and Dialogue entry points and raw-handle queries are no longer part of the public surface. Repository consumers use the Class façades, while explicit `Destroy()` remains idempotent for the bounded underlying resources.

# Smile.Game

## Current Baseline

Current shipped version:

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

Every world/dungeon/battle gallery and project/package consumer uses the migrated API.

# Smile.RPG

## Current Baseline

Current shipped version:

```text
Smile.RPG 1.2.1
```

The package contains fifteen bounded, generation-safe, transactional Modules covering management, world, story, encounters, saves, and battles.

## Applied Compatibility Migration

Do not redesign the public API during the lightweight OOP milestone.

The applied compatibility work:

- compiles under the new language implementation;
- rebuilds as `.smilelib` format 6;
- preserves every public module, signature, and result code;
- preserves all transactional, query, rollback, and invariant behavior;
- passes the current native, Web, project, package, gallery, and rollback matrix.

Do not:

- create `RpgState` as a milestone blocker;
- replace result/item/target constants with enums;
- add Smile.UI or Smile.Game dependencies;
- add battler/action/effect inheritance.

## Version

Shipped package-only patch:

```text
Smile.RPG 1.2.1
```

The fifteen-Module source API is unchanged from 1.2.0; 1.2.1 is the deterministic format-6 compatibility package.

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

It remains a Module. Its public `TitleAction` enum now gives the title selection, update result, and main scene dispatcher one exact nominal action type without forcing the custom screen through the generic Menu Class.

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
