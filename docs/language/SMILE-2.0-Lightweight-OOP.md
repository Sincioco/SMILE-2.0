# SMILE 2.0 Lightweight OOP

**Status:** Approved implementation specification  
**Owner:** Sin  
**Primary syntax inspiration:** Visual Basic  
**Secondary semantic influence:** C# where BASIC has no clearer precedent

## Purpose

SMILE 2.0 adds a deliberately small object model to improve readability, encapsulation, reuse, and teaching value.

The design does not attempt to reproduce the complete OOP surface of Visual Basic or C#.

## Mental Model

```text
Module = shared service or deliberate singleton
Type   = nominal deep-copy value
Class  = reference object with identity
```

Examples:

```text
Math                         -> Module
Collision2D                  -> Module
SinStarI.TitleScreen         -> Module
Smile.RPG.SaveGames          -> Module

Rect                         -> Type
MenuStyle                    -> Type
CardinalMover                -> Type
CameraState                  -> Type
GridPoint                    -> Type

Menu                         -> Class
Dialogue                     -> Class
Snake                        -> Class
```

## General Rules

- SMILE remains case-insensitive.
- Generated and maintained source uses Visual Basic-style initial capitalization.
- Constants may remain uppercase.
- `src\Smile.Language` is the sole syntax/semantic authority.
- Native and Web targets expose identical language semantics.
- Project and package references expose identical public semantics.
- Breaking improvements may migrate all repository consumers together.

# Multiline Routine Declarations

Balanced declaration parentheses allow line continuation:

```smile
Public Function AddItem(
    Label As Text,
    UserValue As Number,
    Optional Enabled As Boolean = True
) As Number
```

```smile
Public Sub New(
    ByRef Style As Core.MenuStyle,
    X As Number,
    Y As Number,
    Width As Number,
    Height As Number,
    Optional VisibleRows As Number = 5
)

    ...

End Sub
```

Rules:

- newlines are whitespace only inside balanced declaration parentheses;
- commas remain required;
- generated multiline declarations place one parameter per line;
- the closing `)` is on its own line;
- a Function's `As ReturnType` follows the closing `)`;
- physical source lines remain exact for diagnostics and debugging.

# `With...End With`

`With` reduces repeated qualifiers.

```smile
With MenuStyle
    .Window = SkinWindow
    .NormalText = SystemText
    .RowHeight = 43
    .WrapSelection = True
End With
```

Nested form:

```smile
With SkinWindow
    .UseSkin = True
    .Skin = WindowSkin

    With .Padding
        .Left = 34
        .Top = 28
        .Right = 28
        .Bottom = 26
    End With
End With
```

Rules:

- a target must be a stable/addressable variable, parameter, array element, field, class location, or leading-dot member of an enclosing `With`;
- evaluate the target exactly once on entry;
- leading-dot members bind to the innermost active target;
- nested blocks are legal;
- a leading dot outside `With` is diagnosed;
- a Type target uses its original writable location;
- a Class target retains the reference for the block and releases it on every exit path;
- arbitrary temporary value targets are not supported initially.

# Enums

Enums are nominal value types.

```smile
Public Enum GameState
    Title
    Playing
    GameOver
End Enum
```

Explicit values:

```smile
Public Enum CardinalDirection
    Up = 1
    Right = 2
    Down = 3
    Left = 4
End Enum
```

Rules:

- implicit numbering begins at zero;
- numbering continues by one;
- after an explicit value, implicit numbering continues from it;
- explicit values are compile-time Number constants;
- runtime storage is Number-compatible, but binding is type-safe;
- assignment/comparison requires the same enum type;
- `Select Case` accepts members of the selected enum;
- enums may be variables, fields, arrays, parameters, returns, property types, and optional defaults;
- no implicit Number/enum or cross-enum conversion;
- enum members use qualified syntax.

Local/project-global:

```smile
State = GameState.Playing
```

Imported Module:

```smile
Action = TitleScreen.TitleAction.NewGame
```

# Optional Parameters

```smile
Public Function AddItem(
    Label As Text,
    UserValue As Number,
    Optional Enabled As Boolean = True
) As Number
```

Rules:

- optional parameters follow required parameters;
- first-version optional parameters are `ByVal` only;
- each optional parameter has an explicit type and default;
- defaults are compile-time literals/constants or a same-enum member;
- Type/Class optional defaults are not supported initially;
- the binder supplies omitted defaults before emission.

# Named Arguments

Named arguments use Visual Basic-style `:=`.

```smile
Dim RootMenu As New Menus.Menu(
    MenuStyle,
    X:=200,
    Y:=250,
    Width:=600,
    Height:=200,
    VisibleRows:=5
)
```

```smile
DisabledIndex = RootMenu.AddItem(
    "Disabled",
    12,
    Enabled:=False
)
```

Rules:

- names match case-insensitively;
- positional arguments precede named arguments;
- named arguments may be reordered;
- no parameter may be supplied twice;
- every required parameter must be supplied;
- explicit arguments are evaluated exactly once in source order;
- evaluated values are then mapped into declaration/ABI order;
- all call forms share one binder implementation.

# Methods on `Type`

`Type` remains a value type but may contain behavior.

```smile
Public Type Point

    X As Number
    Y As Number

    Public Sub MoveBy(Dx As Number, Dy As Number)

        Me.X = Me.X + Dx
        Me.Y = Me.Y + Dy

    End Sub

End Type
```

Usage:

```smile
Dim Position As Point

Position.X = 10
Position.Y = 20

Call Position.MoveBy(5, -2)
```

Rules:

- existing deep-copy assignment remains unchanged;
- existing `ByVal` receives a deep copy;
- existing `ByRef` targets the caller's value;
- an instance member has an implicit exact-Type writable receiver named `Me`;
- a mutating instance call requires an addressable receiver;
- calls on array elements and nested writable fields are legal;
- calls on temporary Type results are deferred;
- static/shared Type methods and overload sets are not supported.

A compiler may lower the receiver to a hidden `ByRef` parameter; that ABI is not user-visible.

# `Me`

`Me` denotes the current Type/Class instance.

```smile
Me.CurrentLength = 5
```

Rules:

- valid only inside instance methods, properties, and constructors;
- cannot be reassigned;
- uses ordinary member visibility/type checks;
- no `MyBase`/inheritance equivalents are added.

# Properties

Properties use readable Visual Basic-inspired accessors.

```smile
Public Property SelectedIndex As Number

    Get

        Dim Result As Number

        Result = Me.CurrentIndex

        Return Result

    End Get

    Set

        Me.CurrentIndex = Value

    End Set

End Property
```

Read-only:

```smile
Public Property Length As Number

    Get

        Dim Result As Number

        Result = Me.CurrentLength

        Return Result

    End Get

End Property
```

Usage:

```smile
Index = RootMenu.SelectedIndex
RootMenu.SelectedIndex = 2
```

Rules:

- a property has at least one accessor;
- `Value` is a contextual implicit setter parameter, not a globally reserved word;
- getter/setter code is ordinary bound SMILE code;
- read-only property assignment is diagnosed;
- indexed/default and auto-properties are deferred;
- accessor-specific visibility is deferred;
- debugger hover does not execute arbitrary property getters.

# Classes

A Class is a reference type with identity.

```smile
Public Class Counter

    Private CurrentValue As Number

    Public Sub New(StartValue As Number)

        Me.CurrentValue = StartValue

    End Sub

    Public Sub Increment()

        Me.CurrentValue = Me.CurrentValue + 1

    End Sub

    Public Property Value As Number

        Get

            Dim Result As Number

            Result = Me.CurrentValue

            Return Result

        End Get

    End Property

End Class
```

Usage:

```smile
Dim First As New Counter(10)
Dim Second As Counter

Second = First

Call Second.Increment()

Print First.Value
```

Both variables refer to the same object.

## Construction

Each Class supports zero or one `Sub New` initially.

```smile
Dim Menu As New Menus.Menu(Style, 0, 0, 320, 240)
```

or:

```smile
Dim Menu As Menus.Menu
Menu = New Menus.Menu(Style, 0, 0, 320, 240)
```

If no constructor is declared, an implicit parameterless constructor default-initializes fields.

Constructor overloads are deferred.

## Supported Class Fields

A Class may contain:

- Number;
- Boolean;
- Text;
- Enum;
- Type;
- fixed one/two-dimensional arrays of those non-class values.

Initially unsupported:

- Class-reference fields;
- arrays of Class references;
- Class references inside Type fields;
- dynamic arrays.

These restrictions prevent reference cycles in the first native lifetime model.

## Reference Semantics

- assignment aliases;
- `ByVal` copies/retains the reference and refers to the same object;
- `ByRef` may replace the caller's reference;
- Class-valued Functions transfer a scalar reference using the documented native/Web ownership rule;
- Class self-assignment is safe.

# `Nothing` and Identity

Class references default to `Nothing`.

```smile
Dim CurrentMenu As Menus.Menu

If CurrentMenu Is Nothing Then
    CurrentMenu = New Menus.Menu(MenuStyle, 0, 0, 320, 240)
End If

If CurrentMenu Is Not Nothing Then
    Call CurrentMenu.ClearItems()
End If
```

Rules:

- `Nothing` is assignable only to Class references;
- `Is` and `Is Not` test reference identity;
- `=` and `<>` on Class references are diagnosed initially;
- member access on `Nothing` fails safely and deterministically;
- `Nothing` is not a generic value for Number, Boolean, Text, Enum, or Type.

# Visibility

Current Module rules remain unchanged: declarations are private unless explicitly `Public`.

Inside Type:

- fields remain public;
- methods/properties default public;
- `Private` is allowed.

Inside Class:

- fields default private;
- methods/properties default public;
- `Private` is allowed;
- `Sub New` is public initially.

Official public APIs should use explicit visibility for clarity.

No `Protected`, `Friend`, `Internal`, interfaces, or inheritance access rules are added.

# Declaration Placement

Application support sources may declare project-global:

```text
Const
Dim
Enum
Type
Class
Sub
Function
```

Library sources still declare exactly one Module. Public enums, Types, and Classes are declared inside that Module.

A Module may span multiple physical files from the same provider.

No nested/partial/local Classes or Enums are added.

# Module Qualification

The existing import model remains canonical.

```smile
Import Smile.UI.Menu As Menus

Dim RootMenu As New Menus.Menu(...)
```

Public enum members use:

```smile
Menus.MenuResult.Accepted
```

Do not add a second import syntax solely to make Class names unqualified.

# Native Lifetime Model

Native Classes use automatic reference counting.

Required behavior:

- allocate/default-initialize;
- retain on owned alias creation;
- release on overwrite/scope exit;
- clear Text/Type/fixed-array fields exactly once at final release;
- free the object allocation;
- release on every existing control-flow cleanup path;
- no tracing garbage collector.

The language initially forbids reference-containing fields/cycles, so ARC is sufficient.

# Web Representation

The Web backend may use JavaScript object references but preserves:

- `Nothing`;
- identity;
- alias assignment;
- `ByVal` reference behavior;
- constructor defaults;
- null access failure;
- SMILE visibility rules;
- collision-safe generated storage keys.

Type values continue to deep-clone on value-copy operations.

# `requiresGameWindow`

Constructors, methods, Functions, and property accessors participate in the existing capability model.

A member that directly/transitively draws or uses game-window-only services carries `requiresGameWindow` through:

- source analysis;
- project references;
- `.smilelib` metadata;
- editor analysis;
- compiler validation.

Object syntax does not bypass Console/Game safety.

# `.smilelib` Format 6

The object model advances package format 5 to format 6.

Format 6 public API metadata includes:

- enum identities and ordered members/values;
- Type fields and members;
- Classes and constructors;
- methods/Functions;
- properties/accessors;
- parameter names/types/modes;
- optional/default values;
- return/property types;
- visibility;
- `requiresGameWindow`;
- exact provider identities;
- physical source locations.

Formats 1 through 5 are rejected with the existing unsupported-format diagnostic path and a rebuild instruction.

Packages remain deterministic and contain no absolute/project/cache/temp paths.

# Teaching and Design Guidance

Good first-wave object candidates are obvious stateful nouns:

```text
Menu
Dialogue
Snake
```

Good Type-method candidates are values already passed as the first `ByRef` parameter:

```text
CardinalMover
CameraState
```

Good Modules include stateless/shared/singleton services:

```text
Collision2D
SaveGames
SinStarI.TitleScreen
```

Do not add manager/factory/controller layers to small programs merely to use OOP.

# Explicitly Deferred

This milestone does not add:

- inheritance;
- abstract/virtual/override;
- interfaces;
- generics;
- delegates/lambdas;
- events/callback syntax;
- operator overloading;
- reflection/attributes;
- extension methods;
- overload sets;
- auto-properties;
- indexed/default properties;
- user finalizers;
- tracing garbage collection;
- class-reference fields/cycles.

A later feature requires a concrete SMILE program demonstrating the need.

