# Syntax and routines

[Language reference](README.md) · Declarations, calls, expressions and source style.

SMILE evolves only when current syntax cannot express a requirement clearly. New general-purpose features prefer readable, established BASIC wording; the smallest beginner-friendly concept from another established language is used only when BASIC has no suitable precedent. The language avoids aliases, multiple spellings, clever punctuation, and game-specific statements. Syntax, diagnostics, examples, and documentation change proportionally through the shared authority.

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

Recovery from an incomplete lightweight-OOP block is bounded by the next unambiguous declaration or executable-statement boundary. A missing `End Enum`, `End Type`, `End Class`, `End Property`, accessor terminator, or `End With` therefore produces a precise diagnostic without consuming later top-level declarations. Malformed constructors, Properties, accessors, multiline Optional parameters, named arguments, `New`, and `Is Not` expressions likewise resume at the nearest safe line boundary; every recovery diagnostic retains a valid span within the physical source.

Declaration parentheses do not make the rest of SMILE free-form. Placing `(` on the next line, placing a Function's return `As Type` below `)`, or continuing an unparenthesized declaration remains invalid. Square brackets also retain their existing behavior: `[` alone never opens a continuation context, so array dimensions and indices do not become multiline merely because they are bracketed.

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

The expression surface includes `+`, `-`, `*`, integer `/`, `Mod`, comparisons, parentheses, unary `-` and `Not`, and boolean `And`/`Or`. Boolean `And` and `Or` evaluate left-to-right and short-circuit: `False And RightSide` and `True Or RightSide` do not evaluate `RightSide`. Common built-in functions include `Timer()`, `Rgb(r, g, b)`, `Abs(value)`, `Min(a, b)`, `Max(a, b)`, `Game_Closed()`, `Window_Title(title)`, `Window_Activate()`, and `Key_Held(key)`. `Window_Title` changes the live native title bar or Web document title. `Window_Activate()` restores the native Windows game window, requests foreground input focus, and returns whether Windows accepted the request; Web currently returns `False`. Both window operations are available only to `Game Window` programs. The game-window-only `Renderer3D(command, a, b, c, d, e, f, g, h, i, j)` built-in is the narrow compiler/runtime bridge used internally by `Smile.Simple3D.Graphics3D`; student programs should use that module rather than command values.

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

Follow the [authoritative formatting conventions](../SMILE-2.0-Authoritative-Code-Formatting-Conventions.md) for the complete rules and canonical startup structure. The summary below does not replace that document.

Use Visual Basic-style initial capitalization for keywords and ordinary identifiers. Established constants may remain uppercase. Short interface labels and instructions use initial or title capitalization; sentences use normal English capitalization. Do not use all caps for ordinary keywords, variables, documentation headings, menu items, or instructional prose.

Use exactly one blank line between logical groups and never use double or triple blank lines. In SMILE source:

- separate the final consecutive `Module`, `Import`, `Dim`, `Call`, or `Unload` statement from the following group with one blank line;
- put one blank line after a `Function`, `Sub`, or procedure declaration;
- keep enum members together between `Enum` and `End Enum`, with one member per indented line;
- put one blank line before `If`, `For`, `Do`, `End Sub`, and `Loop`, and after `End If` and `Loop`;
- keep the whole `If` block compact when every branch has at most two direct statements and no nested control block; otherwise separate every branch body with one blank line;
- keep a `For` body compact when it has at most four direct statements and no nested control block; otherwise add one blank line after `For` and before `End For`;
- keep `Option Explicit` separated from the statements before and after it;
- keep one blank line between a function's final `Return` and `End Function`;
- separate each `Play Sound` group from surrounding statements and branch boundaries with one blank line; this takes priority over compact `If` spacing.

The formatter and checker at `scripts\format-smile-style.ps1` apply these rules to current tracked SMILE sources while leaving historical requirement archives unchanged. It uses the shared parser and semantic model for complete Return expressions, long If conditions, and contextual identifiers. The default ignores untracked files; use `-IncludeUntracked` to include them or `-Files` to explicitly target named `.smile` files. `-Check` never writes. Mutating runs preflight every result, reject new diagnostics or concurrent hash changes, and commit atomic replacements only after every target is safe. Deliberately invalid diagnostic fixtures retain malformed return expressions when rewriting them would change the diagnostic being taught. The focused formatter safety suite and repository-wide style check run early in `scripts\smoke-test.cmd`.
