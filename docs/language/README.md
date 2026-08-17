# SMILE 2.0 language

`src\Smile.Language` is the sole authority for SMILE source documents, tokens, keyword facts, syntax, diagnostics, symbols, types, and semantic analysis. Both `smilec` and the Visual Studio extension consume the same `SmileLanguage.Analyze` result, whether a compilation contains one source file or several.

## Modules and imports

`Module dotted.name` ... `End Module` declares a module. Declarations are private unless prefixed with `Public`; `Private` is available when explicit intent helps. A physical source imports a module with `Import dotted.name As Alias`, then accesses exported constants, arrays, functions, subroutines, record types, and enums through that alias. Imports are scoped to that physical source. One module may span files from one provider. Inside a module, an unqualified nominal type must be built in or owned by that same module, including a type declared in another physical module source; external module types require explicit `Alias.Type` qualification. Project-global and ambient sibling-library types never enter a module's unqualified type scope. Duplicate providers, import cycles, private access, unknown members, and module access to consumer globals are diagnosed by the shared binder.

```smile
Import Smile.Math.Extras As Math
Print Math.Clamp(150, 0, 100)
```

Library compilation requires every source to declare a module. Application projects may also contain local module sources without packaging them.

SMILE evolves only when current syntax cannot express a requirement clearly. New general-purpose features prefer readable, established BASIC wording; the smallest beginner-friendly C#-inspired concept is used only when BASIC has no suitable precedent. The language avoids aliases, multiple spellings, clever punctuation, and game-specific statements. Syntax, diagnostics, examples, and documentation change proportionally through the shared authority.

SMILE is case-insensitive and normally line-oriented. Balanced expression parentheses and balanced routine-declaration parameter parentheses provide the documented continuation contexts; newlines remain significant everywhere else. An apostrophe starts a comment. Values include signed 64-bit `Number`, `Boolean`, mutable UTF-8 `Text`, and user-defined nominal record and enum types.

## Explicit declarations and built-in types

`Option Explicit` disables implicit variables for one physical source. In an application or support source it must be the first non-comment statement. In a module it follows `Module` and precedes imports and declarations. Sources without it retain legacy implicit variables.

```smile
Option Explicit

Dim Score As Number
Dim IsAlive As Boolean
Dim Caption As Text
Dim Names[10] As Text
Dim Flags[10] As Boolean
Dim LegacyGrid[20, 15]
```

Scalar `Dim` requires `As Type`. Arrays may use built-in or visible nominal types; an untyped legacy array remains a `Number` array. Built-in defaults are `0`, `False`, and `""`; records default recursively and enums default to their underlying zero value. `Text` supports value assignment, `+` concatenation, `=`/`<>` ordinal equality, constants, arrays, routine parameters/returns, `Print`, text `Select Case`, and any `Text` expression in `Draw Text`. There are no implicit conversions among built-in or nominal types.

Routine parameters accept `[ByVal | ByRef] Name [As Type]`. Missing mode means `ByVal`; missing type preserves the legacy numeric calling convention, including converting a `Boolean` argument to `0` or `1` for old untyped `ByVal` routines. Explicitly typed parameters still require an exact type. A function can return any visible supported type; legacy omitted return types are inferred consistently from every value return. `ByRef` requires an exact-type writable scalar, array element, record field, or writable parameter. Routine-local `Dim` declarations are visible from their declaration to routine end and may shadow a global.

```smile
Sub Rename(ByRef Name As Text, NewName As Text)

    Name = NewName

End Sub

Function Join(Left As Text, Right As Text) As Text

    Dim Result As Text

    Result = Left + Right
    Return Result

End Function
```

Native and Web calls have no four-parameter language restriction; the regression matrix covers 0, 1, 4, 5, 8, and 16 parameters.

## Optional parameters and named arguments

An Optional parameter uses `Optional Name As Type = Default`. `Optional` implies `ByVal`; an explicit `Optional ByVal` is accepted, while `Optional ByRef` is rejected. Optional parameters require an explicit type and default, must follow every required parameter, and may use `Number`, `Boolean`, `Text`, or an enum type. A default is a compile-time literal, `Const`, or exact-type enum member. Enum defaults preserve the selected member name as well as its signed value, including when a `Const` names an alias.

```smile
Sub Present(
    Value As Number,
    Optional Caption As Text = "ready",
    Optional DirectionValue As Direction = Direction.Left
)

    Print Value
    Print Caption

End Sub

Call Present(Value:=3)
Call Present(
    DirectionValue:=Direction.Right,
    Value:=4
)
```

Calls may mix positional and named arguments, but every positional argument must precede the first named argument. Names are case-insensitive, identify declared parameters rather than new variables, and use the single canonical `Name:=Expression` spelling. A parameter may be supplied only once. Omitted required parameters, unknown names, duplicate arguments, and named arguments on built-in functions are diagnosed.

Every explicit argument is evaluated exactly once in source order. The completed captures are then passed in parameter-declaration order, and omitted Optional values are supplied from their bound constants. A named `ByRef` argument captures its writable location when that argument is encountered, so a later argument cannot redirect an earlier array element or record field. A `ByVal` record is deep-copied at capture time, so later argument side effects cannot mutate the value the callee receives. Native and Web calls release already captured owned values if a later explicit argument terminates before the call; successful calls transfer their Text, Image, and record captures to the normal callee ownership path.

The formatter removes whitespace around `:=` without changing expression layout. In the editor, named-label completion is offered alongside ordinary expression completion after `(` or a top-level comma. Labels display and insert as `Name:=`; Quick Info and F12 resolve them to the declared parameter without attempting to read a caller-frame variable of the same name.

## Multiline routine declarations

A `Sub` or `Function` parameter list may span physical lines when its opening `(` remains on the declaration line and a matching `)` closes the list. Between those balanced declaration parentheses, newlines act as whitespace: they may appear after `(`, between complete parameter declarations, around commas, and between a parameter's mode, name, `As`, and type. The continuation context ends at `)`. For a typed `Function`, the return `As Type` remains on the same physical line as the closing `)`.

Canonical repository formatting puts one parameter on each line, indents parameters by four spaces, uses a trailing comma on every parameter except the last, aligns `)` with the declaration, and leaves one blank line after the complete header:

```smile
Function Add(
    LeftValue As Number,
    RightValue As Number
) As Number

    Dim Result As Number

    Result = LeftValue + RightValue
    Return Result

End Function
```

The parser and semantic model retain each token's original physical source position. Diagnostics therefore point to the actual parameter line: for example, a missing comma is reported at the following parameter, while a missing `)` is reported at the first token that cannot belong to the declaration. CRLF and LF sources preserve the same physical line and column meanings.

Declaration parentheses do not make the rest of SMILE free-form. Placing `(` on the next line, placing a Function's return `As Type` below `)`, or continuing an unparenthesized declaration remains invalid. Square brackets also retain their existing behavior: `[` alone never opens a continuation context, so array dimensions and indices do not become multiline merely because they are bracketed.

## Record types

`Type Name` ... `End Type` declares a nominal value type. A project-global type is shared across physical sources. A type directly inside a module is private by default and may be marked `Public` or `Private`. Fields use `FieldName As Type`; field types may be built-in, another visible record type, or an imported public type such as `Models.Actor`. Fields cannot be arrays and cannot have initializers.

```smile
Type Point2D
    X As Number
    Y As Number
End Type

Type Actor
    Name As Text
    Position As Point2D
    Active As Boolean
End Type

Dim Hero As Actor
Dim Party[4] As Actor
Hero.Name = "Alyssa"
Party[0] = Hero
Print Party[0].Position.X
```

Record identity is exact and nominal: separately declared types with identical fields are not interchangeable. Records default each field recursively, use deep value-copy assignment, preserve safe self-assignment, and allow nested field locations and fixed one- or two-dimensional arrays. `ByVal` receives an independent deep copy. `ByRef` may target a record variable, record array element, nested record field, or scalar field. Functions may return records, including recursively and with 0, 1, 4, 5, 8, or 16 explicit parameters.

Native records use deterministic inline 8-byte-aligned layouts and generated initialize, clear, and deep-copy helpers. Record results use a hidden caller-owned return buffer; invocation-local result temporaries keep recursive calls reentrant and release nested `Text` exactly once. Web records use fresh default objects and deep clones so assignment, arrays, `ByVal`, `ByRef`, and returns do not leak JavaScript object aliases. Generated JavaScript stores fields under deterministic private keys derived from the bound record-field symbol and ordinal, never under source spelling. Fields such as `__proto__`, `constructor`, `prototype`, `toString`, and `valueOf` therefore behave like ordinary SMILE fields while IntelliSense and package metadata continue to show their original names.

### Type methods and properties

A `Type` may also contain instance `Sub` and `Function` members plus `Property` declarations. Members are Public by default and may be marked `Public` or `Private`. Fields are always Public: explicit `Public` is allowed, while `Private` is not. Fields, methods, and properties share one case-insensitive member namespace. An instance member uses `Me` to read or replace fields on its hidden `ByRef` receiver. `Me` is not a declared parameter and cannot be assigned or passed `ByRef` as a whole.

Smile.Game 2.0.0 is the official value-Type migration: `CardinalMover.Place`, `BeginMove`, `UpdateMove`, `CancelMove`, `VisualX`, and `VisualY`, plus every `CameraState` operation, use instance syntax while preserving inline deep-copy assignment, ByVal isolation, ByRef mutation, and addressable array-element receivers. `CardinalDirection` provides nominal `None`, `Up`, `Right`, `Down`, and `Left` values. Animation and TileMap remain handle Modules, and Collision2D remains stateless.

```smile
Type Counter
    Label As Text
    StoredValue As Number

    Public Sub Advance(Optional Delta As Number = 1)
        Me.StoredValue = Me.StoredValue + Delta
    End Sub

    Public Function Shifted(Optional Delta As Number = 1) As Counter
        Dim Result As Counter
        Result = Me
        Call Result.Advance(Delta)
        Return Result
    End Function

    Public Property Total As Number
        Get
            Return Me.StoredValue
        End Get
        Set
            Me.StoredValue = Value
        End Set
    End Property
End Type

Dim Current As Counter
Call Current.Advance(Delta:=2)
Current.Total = 9
Print Current.Total
```

A Property declares `Get`, `Set`, or both. `Get` returns the declared property type. `Set` receives the contextual hidden `ByVal` local `Value`; it is not a public parameter. Reading a write-only property or assigning a read-only property is an error. Private members are available only from another method or accessor of the same containing Type.

The receiver must be a stable writable Type location: a variable or parameter, array element, nested field, or active `With` target. A Function result, Property result, or other temporary is not a valid receiver, even when its value has the right nominal Type. A method evaluates and captures its receiver before evaluating explicit arguments. Explicit arguments still evaluate exactly once in source order before declaration-order ABI placement. A property assignment evaluates its right-hand side first, then resolves the receiver location; replacing a root through `ByRef` during either stage is therefore visible at the specified point. Returned and `ByVal` Type values remain deep copies, not object aliases.

Game Window capability flows through methods and each Property accessor independently. A Console consumer may assign through a safe setter even when that property's getter requires a Game Window. Format-version 6 packages preserve public nested method/function signatures, Optional defaults, structured nominal types, stable runtime identities, source locations, and accessor-specific capabilities. Hidden `Me` and `Value` locals and Private members never enter package API metadata.

Completion after an addressable Type value lists accessible fields, methods, Functions, and properties; named-label completion for a method lists only its declared parameters. A record-valued Property result may still expose readable nested fields, but it does not offer invalid method/property calls because the result is not addressable. Quick Info shows the containing Type, exact project/package provider, signature, accessor availability, and separate getter/setter capability. F12 navigates to the original project declaration or extracted package source. Hovering a property does not invoke its getter. The formatter traverses Type routines and Property accessors, canonicalizes contextual `Me`/`Value`, safely rewrites computed Returns in Functions and getters, and remains idempotent.

Diagnostics use `SML3440` for Type member collisions and illegal Private fields, `SML3441` for malformed Properties/accessors, `SML3442` for invalid `Me`, `SML3443` for a missing/noncallable member or non-instance receiver, `SML3444` for a nonaddressable Type receiver, `SML3445` for an unavailable accessor, and `SML3446` for access to a Private member from outside its containing Type or Class.

## Class references

`Class Name` ... `End Class` declares a nominal reference type. A module Class follows the same private-by-default module visibility rule as Type and Enum declarations. Class fields are Private by default and may be marked `Public`; methods and properties are Public by default and may be marked `Private`. All fields, methods, and properties share one case-insensitive member namespace.

```smile
Class Counter
    Private Label As Text
    Private StoredValue As Number
    Public Samples[2] As Number

    Public Sub New(Label As Text, Optional Start As Number = 0)
        Me.Label = Label
        Me.StoredValue = Start
    End Sub

    Public Sub Advance(Optional Delta As Number = 1)
        Me.StoredValue = Me.StoredValue + Delta
    End Sub

    Public Property Total As Number
        Get
            Return Me.StoredValue
        End Get
        Set
            Me.StoredValue = Value
        End Set
    End Property
End Class

Dim Current As New Counter("main", Start:=2)
Dim Alias As Counter

Alias = Current
Call Alias.Advance()
Print Alias Is Current

Alias = Nothing
Print Alias Is Nothing
```

`Sub New` is the one Public constructor and may use the normal required, Optional, positional, and named arguments. A Class without an explicit constructor receives an implicit Public parameterless constructor. `New Counter(...)` creates an object; `Dim Value As New Counter(...)` declares and initializes a scalar reference. Constructor arguments evaluate once in source order before allocation and declaration-order argument placement. Constructors have a hidden `Me` receiver, but it is not a source parameter, named argument, or package parameter.

Only scalar Class references are supported. Class arrays, Class fields that directly contain another Class, Class fields inside a Type, and direct Image fields are rejected. A Class field may contain Number, Boolean, Text, Enum, or Type values, including fixed one- or two-dimensional arrays. A contained Type may itself own Text or Image resources. Class fields have deterministic native layouts and collision-safe generated Web keys; source names such as `__proto__`, `constructor`, `prototype`, `toString`, and `valueOf` remain ordinary SMILE fields.

Class assignment and `ByVal` arguments preserve object identity by retaining the same reference. `ByRef` targets a writable scalar reference and may rebind it. Class-valued Functions transfer a reference under the same ownership contract. An uninitialized Class variable is `Nothing`; `Nothing` is assignable only to a Class reference. `Is` and `Is Not` compare exact-Class identity or a Class reference with `Nothing`; `=` and `<>` are not Class identity operators. Known literal `Nothing` member access is rejected at compile time, while a runtime `Nothing` receiver fails deterministically with `Object reference is Nothing`. Native Class allocation failure is a distinct deterministic `Class allocation failed` runtime error rather than a `Nothing` dereference.

A Class receiver captures and retains the object identity before method arguments. Unlike a Type location, a Class-valued Function or Property result may be a receiver. Property assignment preserves SMILE assignment order: the right-hand side is evaluated first, then the Class receiver is captured, while the accessor ABI still receives the receiver before hidden `Value`. `With` on a Class captures and retains one object identity for the whole block, so rebinding the source variable inside the block does not retarget leading-dot members.

Native objects use deterministic reference counting and generated finalizers; Web uses matching manual ARC metadata rather than relying on garbage-collection timing. Finalization clears direct Text fields, contained Type fields, and fixed arrays in deterministic reverse declaration/element order before freeing the object. `End Program`, normal scope exits, returns, overwritten references, staged call failures, and null failures release owned references. Native non-local termination unwinds every active routine frame from newest to oldest, clearing owned Text, Image, Type, and Class values separately from partially staged call values. Set `SMILE_CLASS_LIFETIME_DIAGNOSTICS=1` to require `SMILE_CLASS_LIVE=0`; the Web runner exposes the same count through `smile.classLiveCount()` and `smile.mediaDiagnostics().classLiveCount`.

Format-version 6 packages serialize each Public Class with its stable identity, public fields, always-present explicit or synthesized constructor, public methods/properties, exact TypeRefs, locations, parameters, and accessor-specific capabilities. Instance size, offsets, private fields/members, hidden `Me`, and setter `Value` are implementation details and never appear in public API metadata. Completion distinguishes Classes and constructors, offers only Classes after `New`, includes constructor named labels, permits same-Class private members, and preserves exact project/package provider navigation. Quick Info never evaluates a Property getter. The formatter traverses constructors, methods, Functions, and accessors and remains idempotent.

This milestone intentionally does not add inheritance, virtual dispatch, user-defined destructors/finalizers, static Class members, indexed/default properties, Class arrays, or general exception unwinding.

## Enum types

`Enum Name` ... `End Enum` declares a closed nominal value type. Enums may be project-global or direct module declarations and follow the same private-by-default module visibility rules as records. Each member begins on its own declaration line and uses either `Name` or `Name = NumberConstantExpression`. The first implicit value is zero; every later implicit value is the checked previous value plus one. Explicit values accept signed 64-bit compile-time `Number` expressions, including forward `Const` resolution and the normal constant arithmetic and numeric built-ins; constant cycles are diagnosed. Overflow, division by zero, non-Number values, and enum-typed operands are rejected rather than wrapped or converted.

```smile
Const FIRST_DIRECTION = 10

Enum Direction
    None = FIRST_DIRECTION
    Up
    Down
    Left = -1
    Right = -1
End Enum

Dim Facing As Direction
Dim History[4] As Direction

Facing = Direction.Up
History[0] = Facing
```

Member names are case-insensitive and unique. Duplicate numeric values are legal aliases, as `Left` and `Right` demonstrate. Contextual names such as `None`, `Up`, `Down`, `Left`, and `Right` are accepted only where a member name is valid; their existing unqualified built-in-constant meanings do not change. A local value is written `Direction.Up`; an imported public enum member is written `Alias.Direction.Up`.

Enum identity is exact and nominal. Two enum declarations are never interchangeable even when their member names and values match, and an enum does not implicitly convert to or from `Number`. The only enum operators are `=` and `<>` between values of the exact same enum type. Enums work as constants, scalars, fixed-array elements, record fields, `ByVal` or `ByRef` parameters, and function returns. `Select Case` accepts an enum selector and exact-type enum members; aliases with the same numeric value count as duplicate cases and receive `SML3019`. Whole enum values are not accepted by `Print` or numeric built-ins.

Native code stores an enum as one qword and preserves every signed 64-bit bit pattern. Web code uses JavaScript `BigInt`, including for zero defaults, arrays, record fields, constants, calls, and selectors, so values beyond JavaScript's safe `Number` range remain exact. Format-version 6 library metadata records the enum identity, provider, ordered member names, values, ordinals, and source locations. Project-reference and packaged-library consumers therefore bind the same nominal identity and declaration locations.

The syntax-aware formatter preserves `Enum` blocks, canonicalizes contextual member spelling, treats a member as a direct constant return, and remains idempotent. Completion after `Direction.` or `Alias.Direction.` lists members in declaration order. Quick Info shows the containing enum and signed value; Go To Definition (F12) navigates both enum types and members to their original physical source.

The native backend keeps routine-owned `For` limits and Number, Boolean, Text, or enum `Select Case` selectors in each invocation's stack frame, so recursive and mutually recursive routines do not share compiler state. Owned Text selectors are move-assigned into zero-initialized slots and cleared in reverse nesting order on normal completion, `Return`, `Exit For`, `Exit Do`, `End Program`, and the routine epilogue. A function's owned Text return is preserved separately while its locals, arrays, ByVal parameters, and compiler temporaries are released.

`Print` preserves UTF-8 as the language representation. On an attached Windows console the runtime converts bounded complete UTF-8 chunks to UTF-16 and writes them with `WriteConsoleW`; redirected files and pipes receive the original UTF-8 bytes through chunked `WriteFile` calls without a BOM. Generated Web console output uses the same logical text and is compared against native output by the repository's dependency-free Node host.

## With blocks

`With Target` ... `End With` shortens repeated access to one writable record location. A leading-dot field starts from the innermost active `With` target, and ordinary field suffixes may continue from it. Blocks may be nested, including by using a leading-dot record field as the nested target:

```smile
With Party[SelectPartyIndex()]
    .Active = False
    .Position.X = .Position.X + 9

    With .Health
        .Current = Max(0, .Current - 7)
    End With
End With
```

The target must be a stable, writable record location: a record variable or parameter, a record array element, a writable record field, or a leading-dot record field from an enclosing block. A function result or other temporary value is not a valid target. The target location is evaluated exactly once on entry, so `SelectPartyIndex()` in the example runs once even though the block uses the target repeatedly.

`With` retains that original location rather than copying its record value. If the root is a `ByRef` record parameter, replacing the root remains visible to the caller and to later leading-dot accesses in the same block:

```smile
Sub ReplaceCurrent(ByRef Value As Actor, Replacement As Actor)

    With Value
        Value = Replacement
        Print .Name
    End With

End Sub
```

The active target exposes fields plus accessible Type methods and properties. `Call .Advance(...)`, `.Total`, and `.Total = Value` use the same stable target location; nested leading-dot field chains remain valid. A leading dot outside a `With` block, an unknown member, a non-record target, or a target that is not a stable writable location produces a diagnostic. A method call through `With` evaluates the already-captured target before its explicit arguments, while a property assignment evaluates its right-hand side before resolving that target location.

The syntax-aware formatter treats `With` as structured control flow, preserves `With Target`, `End With`, and body indentation, and applies its normal nested transformations idempotently. In the editor, completion after a leading dot offers accessible fields, methods, and properties from the innermost active record and follows chains such as `.Position.`. Quick Info and Go To Definition (F12) on a leading-dot member resolve to that member's declaration.

## Multi-file programs

A compilation may contain one selected startup source and any number of support sources. Every file is parsed separately and retains its real path, lines, tokens, diagnostics, and debug locations; all files share one case-insensitive value/routine model and a separate type namespace.

The startup source owns executable top-level statements, `Game Window`, and `End Program`. A support source may contain only top-level `Const`, `Dim`, `Type`, `Enum`, `Sub`, and `Function` declarations. Routine bodies retain the complete normal statement surface. The command-line form is:

```text
smilec Program.smile --source GameState.smile --source Drawing.smile -o Program.exe
smilec Program.smile --source GameState.smile --source Drawing.smile --target web --output-dir Web
```

In a `.smileproj`, ordinary sources become support sources. Complete alternative programs stay visible but are excluded unless selected through `<StartupFile>`:

```xml
<SmileSource Include="Program.smile" StartupOnly="true" />
<SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
<SmileSource Include="GameState.smile" />
```

In Visual Studio, use **Set as Startup** on either complete program; the project system changes `<StartupFile>`, retains both alternatives as `StartupOnly="true"`, refreshes the editor workspace, and marks the selection with `(Startup)`. Editing the XML directly remains valid for automation. When an unselected alternative is open, the editor analyzes it as a hypothetical startup plus the ordinary support files, excluding the selected complete program so its diagnostics remain meaningful. `examples\MultiFileBasics` demonstrates startup-to-support calls, support-to-support calls, shared constants and arrays, and a support routine reading a startup global on both Windows and Web.

## Structured example

```smile
Const Width = 12
Const Height = 7

Dim Bricks[Width, Height]

Sub SetBrick(Column, Row, Value)

    Bricks[Column, Row] = Value

End Sub

Function PointsFor(Row)

    Dim ReturnValue As Number

    ReturnValue = 70 - Row * 10
    Return ReturnValue

End Function

Call SetBrick(0, 0, 1)

Select Case PointsFor(0)
    Case 70
        Print "Top Row"
    Case Else
        Print "Other Row"
End Select
```

Implemented control flow comprises multiline `If`/`Else If`/`Else`, `For ... To`, `For ... Down To`, `Do ... Loop`, `Do ... Loop Until`, `Exit For`, `Exit Do`, and `Select Case`. Procedures and functions use `Sub`, `Function`, `Call`, and `Return`, including typed `ByVal`/`ByRef` parameters and typed returns.

The expression surface includes `+`, `-`, `*`, integer `/`, `Mod`, comparisons, parentheses, unary `-` and `Not`, and boolean `And`/`Or`. Built-in functions are `Timer()`, `Rgb(r, g, b)`, `Abs(value)`, `Min(a, b)`, `Max(a, b)`, `Game_Closed()`, and `Key_Held(key)`.

## Multiline parenthesized expressions

SMILE remains line-oriented: a physical newline normally ends an expression or statement. Inside balanced expression parentheses only, one or more newlines act as whitespace. Parentheses are therefore the visible signal that an expression continues; newlines are not ignored globally.

Both leading- and trailing-operator layouts are legal. SMILE 2.0 source generated or substantively formatted by this repository uses the trailing-operator layout preferred by Sin:

```smile
If (Style.CursorWidth < 0 Or
    Style.CursorWidth > Core.UI_MAX_LAYOUT_VALUE Or
    Style.CursorHeight < 0 Or
    Style.CursorHeight > Core.UI_MAX_LAYOUT_VALUE) Then
    Result = False
End If
```

The equivalent leading-operator form also compiles:

```smile
If (Style.CursorWidth < 0
    Or Style.CursorWidth > Core.UI_MAX_LAYOUT_VALUE
    Or Style.CursorHeight < 0
    Or Style.CursorHeight > Core.UI_MAX_LAYOUT_VALUE) Then
    Result = False
End If
```

Keep `If ... Then` on one line when the complete rendered line is at most 100 characters and has no more than two top-level Boolean clauses. Use a parenthesized multiline condition when the line would exceed 100 characters or has three or more top-level clauses. Put one continuation clause on each line, use four spaces rather than tabs, and keep `Then` on the same physical line as the closing `)`. When `And` and `Or` are mixed, retain explicit nested grouping so formatting never changes precedence:

```smile
If ((IsVisible And IsEnabled) Or
    (IsSelected And IsAvailable)) Then
    Result = True
End If
```

The same rule works for `Else If`, arithmetic and comparison expressions, nested calls, assignments, ordinary calls, qualified calls, and `Call` statements:

```smile
Result = CalculateValue(
    FirstValue,
    SecondValue,
    ThirdValue
)

Call Menu.UpdateItem(
    MenuHandle,
    ItemIndex,
    Result
)
```

Newlines may appear after `(`, around arguments and commas, and before `)` in an expression continuation. Routine declaration parameters use the separate balanced declaration-parenthesis rule above. Square brackets alone remain line-oriented and do not authorize continuation. These forms remain invalid because no applicable opening parenthesis authorizes continuation:

```smile
If IsVisible Or
    IsEnabled Then
    Result = True
End If

Result = FirstValue +
    SecondValue

Dim Values[
    2
]
```

Functions may directly return a variable, constant, or literal value such as `True`, `False`, a number, or a string. A computed or evaluated expression must not be returned directly. Assign it to a correctly typed variable first, then return that variable. This keeps the evaluated value visible to Print, hover, and Watch while debugging.

## SMILE source readability style

Use Visual Basic-style initial capitalization for keywords and ordinary identifiers. Established constants may remain uppercase. Short interface labels and instructions use initial or title capitalization; sentences use normal English capitalization. Do not use all caps for ordinary keywords, variables, documentation headings, menu items, or instructional prose.

Use exactly one blank line between logical groups and never use double or triple blank lines. In SMILE source:

- separate the final consecutive `Module`, `Import`, `Dim`, `Call`, or `Unload` statement from the following group with one blank line;
- put one blank line after a `Function`, `Sub`, or procedure declaration;
- keep enum members together between `Enum` and `End Enum`, with one member per indented line;
- put one blank line before `If`, `For`, `End For`, `Do`, `End Sub`, and `Loop`, and after `End If` and `Loop`;
- keep `Option Explicit` separated from the statements before and after it;
- keep one blank line between a function's final `Return` and `End Function`;
- allow a one-statement `If ... Then ... End If` body without additional interior blank lines.

The formatter and checker at `scripts\format-smile-style.ps1` applies these rules to current tracked SMILE sources while leaving historical requirement archives unchanged. It uses the shared parser and semantic model for complete Return expressions, long If conditions, and contextual identifiers. The default ignores untracked files; use `-IncludeUntracked` to include them or `-Files` to explicitly target named `.smile` files. `-Check` never writes. Mutating runs preflight every result, reject new diagnostics or concurrent hash changes, and commit atomic replacements only after every target is safe. Deliberately invalid diagnostic fixtures retain malformed return expressions when rewriting them would change the diagnostic being taught. The focused formatter safety suite and repository-wide style check run early in `scripts\smoke-test.cmd`.

## Game surface

```smile
Game Window "Example" Size 960 By 540

Load HighScore From "HighScore" Default 0
Play Sound "Assets\Start.wav"
Music Volume 70
Play Music "Assets\Background.mp3" Loop

Do
    Get Key Key
    Clear Rgb(12, 18, 30)
    Fill Rounded Rectangle 380, 450, 200, 22, 7, LIGHT_BLUE
    Fill Quadrilateral 0, 0, 240, 80, 240, 460, 0, 540, DARK_GREEN
    Draw Quadrilateral 0, 0, 240, 80, 240, 460, 0, 540, LIGHT_GREEN
    Draw Circle 480, 300, 12, WHITE
    Draw Arc 480, 300, 40, 180, 90, LIGHT_BLUE
    Draw Line 40, 40, 920, 40, DARK_GRAY
    Draw Text "Score" At 40, 15 Size 18 Color CYAN
    Draw Number HighScore At 130, 10 Size 28 Color YELLOW
    Show Screen
    Wait 16 Milliseconds
Loop Until Game_Closed() = True

Save HighScore To "HighScore"
Stop Music
Stop Sound
End Program
```

Drawing statements support filled or outlined rectangles, rounded rectangles, circles, and arbitrary four-corner quadrilaterals, plus outlined arcs, lines, text expressions, and numbers. Quadrilaterals take four perimeter-ordered `(X, Y)` points followed by a color. `Show Screen` presents the logical canvas. `Play Sound` starts an asynchronous WAV effect and missing files are safe. `Load` and `Save` persist integer values in storage isolated by executable name.

## Phase 3 diagnostics

| Code | Meaning |
|---|---|
| `SML3300` | `Option Explicit` is late or duplicated. |
| `SML3301` | Reserved for compatibility with pre-record typed diagnostics. |
| `SML3302` | A scalar `Dim` omits `As Type`. |
| `SML3303` | `Option Explicit` requires a declaration. |
| `SML3304` | Assignment, argument, case, or return types do not match. |
| `SML3305` | A `ByRef` argument is not an exact-type writable location. |
| `SML3306` | A routine duplicates a parameter or local. |
| `SML3307` | A local is used before its `Dim`. |
| `SML3308` | `Text` is used with an unsupported or mixed-type operator. |
| `SML3309` | A legacy function has inconsistent inferred return types. |
| `SML3310` | A typed declaration or return-type context is unsupported. |

Record-specific diagnostics are stable and source-located:

| Code | Meaning |
|---|---|
| `SML3400` | A `Type` is duplicated. |
| `SML3401` | A record type reference is unknown, or a module uses an external type without explicit `Alias.Type` qualification. |
| `SML3402` | A field is duplicated or malformed. |
| `SML3403` | A `Type` is misplaced or a field uses an unsupported form. |
| `SML3404` | Nested value types form a recursive layout cycle. |
| `SML3405` | A field does not exist on the record type. |
| `SML3406` | Field access is applied to a non-record value. |
| `SML3407` | A whole record is used in an unsupported operation. |
| `SML3408` | An imported record type is private or inaccessible. |
| `SML3409` | A public API exposes an inaccessible nominal type. |
| `SML3410` | A type is used as a value, or a value as a type. |
| `SML3411` | A record layout exceeds the supported size. |
| `SML3412` | A `With` target has a record type but is not a stable writable location. |
| `SML3413` | Leading-dot member access is used outside `With...End With`. |
| `SML3414` | `Call .Member(...)` names no callable member on the active `With` target. |
| `SML3415` | A `With` target is neither a Type value nor a Class reference. |

Enum-specific diagnostics are stable and source-located. Exact assignment, argument, case, return, and `ByRef` mismatches continue to use the shared `SML3304` and `SML3305` diagnostics; duplicate numeric aliases in one `Select Case` use `SML3019`.

| Code | Meaning |
|---|---|
| `SML3420` | An enum nominal name is duplicated, or an `Enum` declaration is misplaced. |
| `SML3421` | An enum is empty, a member is malformed, or a case-insensitive member name is duplicated. |
| `SML3422` | An explicit member value is not a checked compile-time signed 64-bit `Number`, or an implicit successor overflows. |
| `SML3423` | A named member does not exist on the enum type. |
| `SML3424` | An enum is used with an unsupported operator or compared with a different enum type. |

Optional-parameter and named-argument diagnostics are stable and source-located. Exact argument-type and `ByRef` location mismatches continue to use `SML3304` and `SML3305`.

| Code | Meaning |
|---|---|
| `SML3430` | An Optional declaration is malformed, uses `ByRef`, omits its explicit type/default, or precedes a required parameter. |
| `SML3431` | An Optional default is unsupported or is not a compile-time value of the exact declared type. |
| `SML3432` | A positional argument follows a named argument. |
| `SML3433` | A named argument names no parameter, or a built-in function is called with a named argument. |
| `SML3434` | A parameter is supplied more than once. |
| `SML3435` | A required parameter is omitted. |

The Web target additionally reports `SML5102` when an Optional `Number` default is outside JavaScript's exact safe-integer range. Enum defaults use `BigInt` and retain the complete signed 64-bit range.

Type/Class member diagnostics are stable and source-located:

| Code | Meaning |
|---|---|
| `SML3440` | A Type member collides, a Type field is Private, or visibility syntax is malformed. |
| `SML3441` | A Type/Class Property or accessor is malformed. |
| `SML3442` | `Me` is used outside an instance member or as an assignable/`ByRef` whole value. |
| `SML3443` | An instance member is missing, noncallable, or used on a non-instance receiver. |
| `SML3444` | A Type method/property receiver is not an addressable stable location. |
| `SML3445` | A Property read lacks `Get`, or a Property assignment lacks `Set`. |
| `SML3446` | A Private member is accessed outside its exact containing Type or Class. |
| `SML3450` | A Class declaration or member statement is malformed or misplaced. |
| `SML3451` | A constructor is invalid, duplicated, Private, or collides in the Class member namespace. |
| `SML3452` | A Class field/layout or scalar-only Class storage form is unsupported. |
| `SML3453` | `New` or `Dim As New` does not name a constructible Class. |
| `SML3454` | `Nothing` is assigned or returned where the exact Class-compatible type is not allowed. |
| `SML3455` | Class identity operands are incompatible, or `=`/`<>` is used instead of `Is`/`Is Not`. |
| `SML3456` | Reserved for future Class storage diagnostics. |
| `SML3457` | Member access is known at compile time to use literal `Nothing`. |

### Arc drawing

```smile
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

`Draw Arc` draws only the curved outline using the normal one-logical-pixel graphics stroke. It does not fill a pie slice, draw a chord, or connect either endpoint to the center. `Fill Arc` is not part of the language.

Angles are integer degrees in screen coordinates:

| Angle | Direction |
|---:|---|
| `0` | right |
| `90` | down |
| `180` | left |
| `270` | up |

Positive sweeps move clockwise and negative sweeps move counterclockwise. Start angles normalize to `0` through `359`. A zero sweep or non-positive radius draws nothing; an absolute sweep of at least `360` draws one complete circle. `examples\ArcBasics.smile` demonstrates four joined rounded corners, both sweep directions, a long arc, and a complete circle.

Generic executable-relative text input uses:

```smile
Dim FileBytes[8192]
Load Text File "Maps\default.map" Into FileBytes Count FileByteCount
```

The path must be a non-empty literal, the destination must be a one-dimensional numeric array, and `Count` must name a writable numeric variable. The runtime zero-fills the complete destination, reads UTF-8 bytes, skips an optional UTF-8 BOM, copies at most the array capacity as values from 0 through 255, and stores the copied byte count. Missing, inaccessible, empty, or unreadable files safely produce count zero. Existing integer persistence keeps its distinct `Load Value From "Key" Default 0` form.

Dungeon Star I provides the complete multi-floor game-side example: three literal-path loaders feed one bounded byte parser in `games\DungeonStarI\Program.smile`. Dungeon Star II uses the same generic statement for compatible one-floor room maps in `games\DungeonStarII\Program.smile`. The language/runtime only delivers bytes; headers, symbols, dimensions, topology, support rules, and fallback behavior remain ordinary SMILE source.

Background-music syntax is:

```smile
Play Music "Assets\Background.mp3"
Play Music "Assets\Background.mp3" Loop
Pause Music
Resume Music
Stop Music
Music Volume 50
```

Music paths are resolved relative to the generated executable. `Music Volume` accepts a numeric expression; the native runtime clamps the requested level to 0 through 100. MP3 playback uses the Windows `Windows.Media.Playback.MediaPlayer` API through C++/WinRT and Windows Media Foundation, independently of the selected graphics backend. No third-party decoder is bundled. Windows installations missing required media components fail playback safely without terminating the game.

### Automatic focus behavior

Every `Game Window` program inherits the same native focus behavior without adding SMILE activation code:

- loss of application activation, top-level window activation, or minimization immediately silences that game's audio;
- MP3 playback continues silently at effective volume zero, preserving both playback position and the exact requested `Music Volume`;
- restoring an active, non-minimized window reapplies the requested volume without restarting playback or resuming a track paused or stopped by the program;
- the current asynchronous WAV effect stops on focus loss, and new `Play Sound` requests are suppressed while inactive rather than queued for later;
- Windows master volume and other applications are never changed;
- DirectX and GDI follow the identical shared runtime policy.

Named input constants include `KEY_W`, `KEY_A`, `KEY_S`, `KEY_D`, the four arrows, `KEY_ENTER`, `KEY_ESCAPE`, `KEY_SPACE`, `KEY_1`, `KEY_2`, `KEY_3`, `KEY_4`, `KEY_TAB`, `KEY_OTHER`, and `KEY_NONE`. `KEY_3` has value `20`, `KEY_TAB` has value `21`, and `KEY_4` has value `22`; `Get Key` returns `KEY_OTHER` (value `19`) for an otherwise unnamed ordinary key event, and `Key_Held(KEY_OTHER)` is always false. Named colors include the standard red/green/blue/cyan/magenta/yellow set plus orange, gray, dark variants, light variants, black, and white.

Phase 5 adds `Text_Length`, `Text_Code_At`, and `Text_Slice`. Their zero-based indices and counts use Unicode scalar values rather than native UTF-8 bytes or Web UTF-16 code units. `Text_Code_At` returns `-1` outside the value, while `Text_Slice` safely clamps and returns empty text for negative starts, nonpositive counts, or starts beyond the end. Routine analysis also records direct and transitive `requiresGameWindow` capability; a Console consumer receives one `SML3704` at its own call site instead of diagnostics cascading from library source. Phase 5.2 adds the Unicode-safe menu overflow/marker/geometry and hierarchical-navigation foundation. Smile.UI 2.0 keeps those bounded engines private behind `Menu`, `MenuNavigator`, and `Dialogue` Class facades with constructors, methods, properties, named/default arguments, and idempotent destruction. Full details are in `phase5-ui.md`.

Phase 6 adds optional stable `ApplicationId` project identity and the source-authored `Smile.RPG` data/management package without adding syntax or RPG runtime helpers. Phase 6.1 advances Smile.RPG to 1.0.1 with save-boundary, rollback, asset-manifest identity, formatter-context, and Shop-result hardening. Phase 6.2 advances Smile.RPG to 1.0.2 with observational `SaveGames.Exists` query behavior while preserving SRPG format 1. See `phase6-rpg.md`.

Phase 7 adds the ordinary `Smile.Game` source package and advances `Smile.RPG` with world, story, encounter-preview, and format-2 persistence modules. Smile.Game 2.0.0 now applies the shared Enum and Type-member language features to its cardinal movement and camera values without adding RPG-specific syntax or a Smile.RPG dependency. `Load Text File` accepts a `Text` expression path and dotted module names may contain the reserved `Game` segment. See `phase7-rpg-world.md`.

Phase 7.1 advances `Smile.RPG` to 1.1.1 with world-state invariant and transactional save hardening. It changes no language syntax, SMILE-MAP fields, SRPG fields, or `.smilelib` package format.

Phase 8 adds no language syntax, native runtime primitive, package API, or file-format revision. It demonstrates dungeon exploration by composing the existing source-authored packages in the `RPGSystems` Dungeon option; see [phase8-rpg-dungeons.md](phase8-rpg-dungeons.md).

Phase 9 advances `Smile.RPG` to 1.2.0 with four ordinary deterministic battle modules and no new language syntax, compiler/runtime helper, rendering primitive, or persistence format. Active battles block Save/Load and remain transient. See [phase9-rpg-battles.md](phase9-rpg-battles.md).

The lightweight-OOP compatibility release rebuilds that unchanged fifteen-Module
surface as `Smile.RPG` 1.2.1 in deterministic `.smilelib` format 6. It adds no
RPG Class façade or Enum conversion, no Smile.UI/Smile.Game dependency, and no
SRPG save-payload revision.

The executable examples are the most precise usage guide: `LanguageBasics.smile`, `StructuredLanguageBasics.smile`, `GraphicsBasics.smile`, `MultiFileBasics`, and the seven projects under `games`. These include Dungeon Star I's external-map parser and quadrilateral-based pseudo-3D renderer, Dungeon Star II's fixed-point DDA raycaster, and Maze Muncher's arc-composed neon maze. Each demo game also includes a complete player-focused `Program-NoDemo.smile` teaching source.
