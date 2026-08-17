# SMILE 2.0 Lightweight OOP Implementation Plan

**Status:** Approved architecture plan  
**Reviewed baseline:** `fc0b91bfd2a9751cf815e1fc81a817f12c40e1d8`

## Goal

Add the approved lightweight object model without replacing the current compiler, package, runtime, editor, library, or game architecture.

## Architectural Invariants

- `src\Smile.Language` owns syntax and semantics.
- Native and Web consume the same bound meaning.
- `.smilelib` project/package consumers behave identically.
- Type remains an inline deep-copy value.
- Class is a distinct scalar reference type.
- Modules remain first-class.
- Current source maps/debug locations remain physical and exact.
- Current Visual Studio async/single-build/concurrency behavior remains intact.
- Current formatter remains syntax-aware and transactional.
- Current deterministic package model remains deterministic.

## Delivery Waves

### Wave A - Readable Declarations and Basic Structure

- multiline declaration parameter lists;
- `With...End With`;
- Enum.

Complete lexer/parser/syntax/binder/formatter/editor/native/Web/package work.

### Wave B - Calls

- Optional ByVal parameters;
- compile-time defaults;
- named arguments with `:=`;
- source-order argument evaluation;
- shared call binder.

### Wave C - Type Members

- methods/Functions inside Type;
- implicit receiver/`Me`;
- properties;
- package metadata and editor navigation.

### Wave D - Class Runtime

- Class declarations;
- constructors/New;
- scalar references/Nothing/identity;
- native ARC;
- Web references;
- fixed non-class array fields;
- full cleanup/source-map/package/editor support.

### Wave E - Migrations

- Smile.UI Menu façade;
- selected Smile.Game Type APIs;
- Snake;
- Sin Star action enum;
- official package rebuilds;
- full RPG compatibility matrix.

## Front End

Add explicit syntax nodes for:

- multiline parameters;
- arguments with optional names;
- With and leading-dot members;
- Enum/member declarations;
- Type members;
- Property/accessors;
- Class/fields/constructors;
- New;
- identity tests;
- generalized member chains.

Add semantic symbols for:

- Enum/EnumMember;
- Class;
- Property;
- containing type and receiver;
- constructor/member signatures.

Extend module member inventory for public Enum and Class declarations.

## Binding

### `With`

- target must be stable/addressable;
- evaluate once;
- maintain nested target stack;
- retain/release Class target across all exits.

### Named Arguments

- bind explicit arguments in source order;
- evaluate once;
- map to parameter order;
- fill omitted defaults;
- use one algorithm for every call form.

### Type Members

- implicit exact-Type writable receiver;
- preserve deep-copy semantics;
- require addressable receiver initially.

### Properties

- bind read to getter;
- bind assignment to setter with contextual `Value`;
- no debugger getter execution.

### Classes

- distinct reference type;
- identity via `Is`;
- no `=` initially;
- no reference-containing fields initially;
- public/private filtering;
- exact provider identity.

## Shared Lowering

Prefer shared bound/lowered operations for:

- With target temporaries;
- enum numeric storage;
- named/default argument ordering;
- hidden Type receiver;
- property calls;
- constructor calls;
- class retain/release ownership.

Emitters should not independently infer language semantics.

## Native Runtime

Use ARC because the first language version forbids language-level reference cycles.

Required generated/runtime operations:

- allocate/default-initialize;
- retain;
- release;
- final field clear;
- free;
- null-call failure.

Release owned references on every routine/control-flow cleanup path already handled for Text/records.

Add focused lifetime diagnostics proving zero live objects.

## Web Runtime

Use JS references while preserving SMILE semantics.

Use deterministic collision-safe generated field keys, not source spelling.

Keep Type deep clones distinct from Class reference aliases.

## Package Format

Advance `.smilelib` from 5 to 6.

Format 6 must serialize the complete final OOP schema before release:

- enums/members;
- Type members/properties;
- Classes/constructors/members/properties;
- named parameter identities;
- optional/default values;
- exact types/providers;
- source locations;
- visibility;
- `requiresGameWindow`.

Reject formats 1-5 with a rebuild diagnostic.

Keep deterministic package bytes and fingerprinting.

## Visual Studio

Extend shared semantic services for:

- completion after Object/Me/With/Enum;
- constructor and named-argument suggestions;
- Quick Info signatures;
- F12 for all new declarations/members;
- private filtering;
- package-source navigation;
- formatter/indentation;
- diagnostics;
- native source maps.

Preserve:

- comment commands;
- async build;
- one prelaunch build;
- UI responsiveness;
- unique intermediates;
- identical-output serialization;
- Set as Startup;
- debugging/hover.

## Library Migration Boundaries

### Smile.UI

- Class façade over existing handle engines;
- styles remain Type;
- preferred Menu/MenuNavigator shared spanning Module;
- explicit idempotent resource Destroy;
- recommended version 2.0.0.

### Smile.Game

- CardinalDirection Enum;
- CardinalMover/CameraState Type methods;
- Animation/TileMap unchanged;
- Collision2D remains Module;
- recommended version 2.0.0.

### Smile.RPG

- no public API redesign;
- format-6 rebuild and complete regression;
- recommended package-only patch 1.2.1.

## Application Migration Boundaries

### Snake

- one support source;
- GameState/MoveDirection;
- GridPoint Type;
- one Snake Class;
- main loop/drawing/audio/game lifecycle remain outside.

### Sin Star I

- preserve TitleScreen Module;
- optionally replace action constants with enum;
- preserve custom rendering/assets/music.

## Validation

Prove every feature through:

- focused managed tests;
- native compilation/run;
- Web compilation/run/check;
- project reference;
- package reference;
- deterministic package;
- formatter/style;
- Visual Studio completion/Quick Info/F12;
- debug stepping/source maps;
- lifetime diagnostics.

Then run current full smoke and RPG/game/artifact matrices at central milestones and completion.

## Explicit Non-Goals

No inheritance, interfaces, generics, delegates, events, overload sets, operator overloads, reflection, finalizers, tracing GC, reference-containing fields, wholesale RPG migration, or framework-style object hierarchy.

