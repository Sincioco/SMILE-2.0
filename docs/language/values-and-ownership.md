# Values and ownership

[Language reference](README.md) · Records, Classes, enums, With blocks and lifetime contracts.

## Record types

`Type Name` ... `End Type` declares a nominal value type. A project-global type is shared across physical sources. A type directly inside a module is private by default and may be marked `Public` or `Private`. Fields use `FieldName As Type` or the fixed-array form `FieldName[Count] As Type`; one- and two-dimensional fixed arrays accept positive compile-time `Number` expressions. Field element types may be built-in, another visible record type, or an imported public type such as `Models.Actor`. Fields cannot have initializers. Every native and Web array access validates each source dimension before flattening the index. Invalid native access reports the invalid value, one-based dimension and declared size, then exits with status 4 after deterministic cleanup; Web reports the invalid value and one-based dimension. A fixed-array element passed `ByRef` is validated and captured when that source argument is evaluated, before any later argument runs.

```smile
Type Point2D
    X As Number
    Y As Number
End Type

Type Actor
    Name As Text
    Position As Point2D
    Waypoints[4] As Point2D
    Active As Boolean
End Type

Dim Hero As Actor
Dim Party[4] As Actor
Hero.Name = "Alyssa"
Party[0] = Hero
Print Party[0].Position.X
```

Record identity is exact and nominal: separately declared types with identical fields are not interchangeable. Records default each field and fixed-array element recursively, use deep value-copy assignment, preserve safe self-assignment, and allow nested field locations plus fixed one- or two-dimensional arrays both as fields and as declared variables. `ByVal` receives an independent deep copy. `ByRef` may target a record variable, record array element, nested record field, fixed-array field element, or scalar field. Functions may return records, including recursively and with 0, 1, 4, 5, 8, or 16 explicit parameters.

Native records use deterministic inline 8-byte-aligned layouts and generated initialize, clear, and deep-copy helpers. Those helpers use bounded runtime loops for fixed-array fields, so emitted helper size follows the record's field structure rather than its element count. Record results use a hidden caller-owned return buffer; invocation-local result temporaries keep recursive calls reentrant and release nested `Text` exactly once. Selecting an owned `Text`, `Image`, Class, or nested record from a returned record's fixed array retains or clones the selected value before clearing the temporary owner; failed indexing clears the same owner. Web records use fresh default objects and deep clones so assignment, arrays, `ByVal`, `ByRef`, and returns do not leak JavaScript object aliases. Generated JavaScript stores fields under deterministic private keys derived from the bound record-field symbol and ordinal, never under source spelling. Fields such as `__proto__`, `constructor`, `prototype`, `toString`, and `valueOf` therefore behave like ordinary SMILE fields while IntelliSense and package metadata continue to show their original names.

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

Game Window capability flows through methods and each Property accessor independently. A Console consumer may assign through a safe setter even when that property's getter requires a Game Window. Format-version 7 packages preserve public nested method/function signatures, Optional defaults, structured nominal types, stable runtime identities, source locations, and accessor-specific capabilities. Hidden `Me` and `Value` locals and Private members never enter package API metadata.

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

Format-version 7 packages serialize each Public Class with its stable identity, public fields, always-present explicit or synthesized constructor, public methods/properties, exact TypeRefs, locations, parameters, and accessor-specific capabilities. Instance size, offsets, private fields/members, hidden `Me`, and setter `Value` are implementation details and never appear in public API metadata. Completion distinguishes Classes and constructors, offers only Classes after `New`, includes constructor named labels, permits same-Class private members, and preserves exact project/package provider navigation. Quick Info never evaluates a Property getter. The formatter traverses constructors, methods, Functions, and accessors and remains idempotent.

This milestone intentionally does not add inheritance, virtual dispatch, user-defined destructors/finalizers, static Class members, indexed/default properties, Class arrays, or general exception unwinding.

### Module-level Class initialization

On native Windows, an accepted scalar Class initializer in a support source or
module executes exactly once before the selected startup source begins. Imported
modules initialize before their importers. Otherwise library providers retain
resolved dependency order, consuming-project sources retain project or command-line
source order, and declarations in one source retain declaration order. An
initializer failure uses normal deterministic termination and clears completed,
partially constructed, and still-uninitialized global references safely.

The selected startup source continues to execute its own `Dim As New` declarations
at their source positions. Entry/local initialization and explicit assignment keep
their existing behavior. Circular module imports remain rejected by `SML3108`, and
project or package dependency cycles remain rejected by `SML3205`; neither case
defines a cyclic initialization order.

Sin Star I keeps `Preview = New Viewer.Session()` inside its explicit `Enter`
operation because that game lifecycle owns actor/resource creation and teardown.
The generic compiler support does not move application-session ownership into
global startup.

Web adoption of support/module Class initialization remains open and is not native
parity: the Web emitter still executes only the selected startup tree. Do not rely
on a support/module `Dim As New` initializer for Web until that held target adopts
the same ordering contract.

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

Member names are case-insensitive and unique. Duplicate numeric values are legal aliases, as `Left` and `Right` demonstrate. Contextual names such as `None`, `Up`, `Down`, `Left`, and `Right` are accepted as member names. A local value is written `Direction.Up`; an imported public enum member is written `Alias.Direction.Up`.

`Left` and `Right` can also name ordinary variables, arrays, constants, and parameters. A declaration in the current scope takes precedence in both reads and writes, regardless of capitalization. Without a visible declaration, their legacy built-in values remain 12 and 13. Module members resolve inside their own module; consuming-program declarations do not leak into imported modules. For explicit direction constants inside a scope that declares `Left` or `Right`, use `KEY_LEFT` or `KEY_RIGHT`. Shared language binding owns this rule for native compilation, editor services, and other emitters.

Enum identity is exact and nominal. Two enum declarations are never interchangeable even when their member names and values match, and an enum does not implicitly convert to or from `Number`. The only enum operators are `=` and `<>` between values of the exact same enum type. Enums work as constants, scalars, fixed-array elements, record fields, `ByVal` or `ByRef` parameters, and function returns. `Select Case` accepts an enum selector and exact-type enum members; aliases with the same numeric value count as duplicate cases and receive `SML3019`. Whole enum values are not accepted by `Print` or numeric built-ins.

Native code stores an enum as one qword and preserves every signed 64-bit bit pattern. Web code uses JavaScript `BigInt`, including for zero defaults, arrays, record fields, constants, calls, and selectors, so values beyond JavaScript's safe `Number` range remain exact. Format-version 7 library metadata records the enum identity, provider, ordered member names, values, ordinals, and source locations. Project-reference and packaged-library consumers therefore bind the same nominal identity and declaration locations.

The syntax-aware formatter preserves `Enum` blocks, canonicalizes contextual member spelling, treats a member as a direct constant return, and remains idempotent. Completion after `Direction.` or `Alias.Direction.` lists members in declaration order. Quick Info shows the containing enum and signed value; Go To Definition (F12) navigates both enum types and members to their original physical source.

The native backend keeps routine-owned `For` limits and Number, Boolean, Text, or enum `Select Case` selectors in each invocation's stack frame, so recursive and mutually recursive routines do not share compiler state. Owned Text selectors are move-assigned into zero-initialized slots and cleared in reverse nesting order on normal completion, `Return`, `Exit For`, `Exit Do`, `End Program`, and the routine epilogue. A function's owned Text return is preserved separately while its locals, arrays, ByVal parameters, and compiler temporaries are released.

`Print` preserves UTF-8 as the language representation. On an attached Windows console the runtime converts bounded complete UTF-8 chunks to UTF-16 and writes them with `WriteConsoleW`; redirected files and pipes receive the original UTF-8 bytes through chunked `WriteFile` calls without a BOM. Generated Web console output uses the same logical text and is compared against native output by the repository's dependency-free Node host.

## With blocks

`With Target` ... `End With` shortens repeated access to a Type value or Class reference. For a Type, it captures one writable record location; for a Class, it retains one object identity as described above. A leading-dot field starts from the innermost active `With` target, and ordinary field suffixes may continue from it. Blocks may be nested, including by using a leading-dot record field as the nested target:

```smile
With Party[SelectPartyIndex()]
    .Active = False
    .Position.X = .Position.X + 9

    With .Health
        .Current = Max(0, .Current - 7)
    End With
End With
```

A Type target must be a stable, writable record location: a record variable or parameter, a record array element, a writable record field, or a leading-dot record field from an enclosing block. A function result or other temporary value is not a valid target. The target location is evaluated exactly once on entry, so `SelectPartyIndex()` in the example runs once even though the block uses the target repeatedly.

`With` retains that original location rather than copying its record value. If the root is a `ByRef` record parameter, replacing the root remains visible to the caller and to later leading-dot accesses in the same block:

```smile
Sub ReplaceCurrent(ByRef Value As Actor, Replacement As Actor)

    With Value
        Value = Replacement
        Print .Name
    End With

End Sub
```

The active target exposes fields plus accessible Type methods and properties. `Call .Advance(...)`, `.Total`, and `.Total = Value` use the same stable target location; nested leading-dot field chains remain valid. A leading dot outside a `With` block, an unknown member, a target of neither Type nor Class, or a Type target that is not a stable writable location produces a diagnostic. A method call through `With` evaluates the already-captured target before its explicit arguments, while a property assignment evaluates its right-hand side before resolving that target location.

The syntax-aware formatter treats `With` as structured control flow, preserves `With Target`, `End With`, and body indentation, and applies its normal nested transformations idempotently. In the editor, completion after a leading dot offers accessible fields, methods, and properties from the innermost active record and follows chains such as `.Position.`. Quick Info and Go To Definition (F12) on a leading-dot member resolve to that member's declaration.

## Record-array locations and ownership

Indexed ByRef arguments capture a writable SMILE location, not an obsolete backing
JavaScript array. Capture indices once in source order and check each dimension
before evaluating a later index. Value records follow their captured location;
Class roots retain the original object identity. Assignments/ByVal copy values,
and checked failures release owned resources without altering earlier output.

`src/Smile.Language` binds the types/dimensions; native and Web emitters implement
that same contract. After changing these owners, run the existing
`node scripts/test-record-array-location-parity.js --repo "D:\SMILE 2.0"` and
`scripts/test-fixed-array-hardening.ps1`. The real compiled fixtures cover nested,
forwarded and With locations, single evaluation, copies, bounds, Class identity,
exact error traces and returned-image cleanup; no assembly rewriting is required.
