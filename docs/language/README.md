# SMILE 2.0 language

`src\Smile.Language` is the sole authority for SMILE source documents, tokens, keyword facts, syntax, diagnostics, symbols, types, and semantic analysis. Both `smilec` and the Visual Studio extension consume the same `SmileLanguage.Analyze` result, whether a compilation contains one source file or several.

## Modules and imports

`Module dotted.name` ... `End Module` declares a module. Declarations are private unless prefixed with `Public`; `Private` is available when explicit intent helps. A physical source imports a module with `Import dotted.name As Alias`, then accesses exported constants, arrays, functions, subroutines, and record types through that alias. Imports are scoped to that physical source. One module may span files from one provider. Inside a module, an unqualified record type must be built in or owned by that same module, including a type declared in another physical module source; external module types require explicit `Alias.Type` qualification. Project-global and ambient sibling-library types never enter a module's unqualified type scope. Duplicate providers, import cycles, private access, unknown members, and module access to consumer globals are diagnosed by the shared binder.

```smile
Import Smile.Math.Extras As Math
Print Math.Clamp(150, 0, 100)
```

Library compilation requires every source to declare a module. Application projects may also contain local module sources without packaging them.

SMILE evolves only when current syntax cannot express a requirement clearly. New general-purpose features prefer readable, established BASIC wording; the smallest beginner-friendly C#-inspired concept is used only when BASIC has no suitable precedent. The language avoids aliases, multiple spellings, clever punctuation, and game-specific statements. Syntax, diagnostics, examples, and documentation change proportionally through the shared authority.

SMILE is case-insensitive and line-oriented. An apostrophe starts a comment. Values include signed 64-bit `Number`, `Boolean`, mutable UTF-8 `Text`, and user-defined record types.

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

Scalar `Dim` requires `As Number`, `As Boolean`, or `As Text`. Arrays may use those types; an untyped legacy array remains a `Number` array. Defaults are `0`, `False`, and `""`. `Text` supports value assignment, `+` concatenation, `=`/`<>` ordinal equality, constants, arrays, routine parameters/returns, `Print`, text `Select Case`, and any `Text` expression in `Draw Text`. There are no implicit conversions between the three built-in types.

Routine parameters accept `[ByVal | ByRef] Name [As Type]`. Missing mode means `ByVal`; missing type preserves the legacy numeric calling convention, including converting a `Boolean` argument to `0` or `1` for old untyped `ByVal` routines. Explicitly typed parameters still require an exact type. A function can declare `As Number`, `As Boolean`, or `As Text`; legacy omitted return types are inferred consistently from every value return. `ByRef` requires an exact-type writable scalar, array element, or writable parameter. Routine-local `Dim` declarations are visible from their declaration to routine end and may shadow a global.

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

The native backend keeps routine-owned `For` limits and Number, Boolean, or Text `Select Case` selectors in each invocation's stack frame, so recursive and mutually recursive routines do not share compiler state. Owned Text selectors are move-assigned into zero-initialized slots and cleared in reverse nesting order on normal completion, `Return`, `Exit For`, `Exit Do`, `End Program`, and the routine epilogue. A function's owned Text return is preserved separately while its locals, arrays, ByVal parameters, and compiler temporaries are released.

`Print` preserves UTF-8 as the language representation. On an attached Windows console the runtime converts bounded complete UTF-8 chunks to UTF-16 and writes them with `WriteConsoleW`; redirected files and pipes receive the original UTF-8 bytes through chunked `WriteFile` calls without a BOM. Generated Web console output uses the same logical text and is compared against native output by the repository's dependency-free Node host.

## Multi-file programs

A compilation may contain one selected startup source and any number of support sources. Every file is parsed separately and retains its real path, lines, tokens, diagnostics, and debug locations; all files share one case-insensitive value/routine model and a separate type namespace.

The startup source owns executable top-level statements, `Game Window`, and `End Program`. A support source may contain only top-level `Const`, `Dim`, `Type`, `Sub`, and `Function` declarations. Routine bodies retain the complete normal statement surface. The command-line form is:

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

Newlines may appear after `(`, around arguments and commas, and before `)`. Routine declaration parameters and square-bracket array expressions remain line-oriented. These forms remain invalid because no opening expression parenthesis authorizes continuation:

```smile
If IsVisible Or
    IsEnabled Then
    Result = True
End If

Result = FirstValue +
    SecondValue
```

Functions may directly return a variable, constant, or literal value such as `True`, `False`, a number, or a string. A computed or evaluated expression must not be returned directly. Assign it to a correctly typed variable first, then return that variable. This keeps the evaluated value visible to Print, hover, and Watch while debugging.

## SMILE source readability style

Use Visual Basic-style initial capitalization for keywords and ordinary identifiers. Established constants may remain uppercase. Short interface labels and instructions use initial or title capitalization; sentences use normal English capitalization. Do not use all caps for ordinary keywords, variables, documentation headings, menu items, or instructional prose.

Use exactly one blank line between logical groups and never use double or triple blank lines. In SMILE source:

- separate the final consecutive `Module`, `Import`, `Dim`, `Call`, or `Unload` statement from the following group with one blank line;
- put one blank line after a `Function`, `Sub`, or procedure declaration;
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
| `SML3409` | A public API exposes an inaccessible record type. |
| `SML3410` | A type is used as a value, or a value as a type. |
| `SML3411` | A record layout exceeds the supported size. |

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

Named input constants include `KEY_W`, `KEY_A`, `KEY_S`, `KEY_D`, the four arrows, `KEY_ENTER`, `KEY_ESCAPE`, `KEY_SPACE`, `KEY_1`, `KEY_2`, `KEY_3`, `KEY_OTHER`, and `KEY_NONE`. `KEY_3` has value `20`; `Get Key` returns `KEY_OTHER` (value `19`) for an otherwise unnamed ordinary key event, and `Key_Held(KEY_OTHER)` is always false. Named colors include the standard red/green/blue/cyan/magenta/yellow set plus orange, gray, dark variants, light variants, black, and white.

Phase 5 adds `Text_Length`, `Text_Code_At`, and `Text_Slice`. Their zero-based indices and counts use Unicode scalar values rather than native UTF-8 bytes or Web UTF-16 code units. `Text_Code_At` returns `-1` outside the value, while `Text_Slice` safely clamps and returns empty text for negative starts, nonpositive counts, or starts beyond the end. Routine analysis also records direct and transitive `requiresGameWindow` capability; a Console consumer receives one `SML3704` at its own call site instead of diagnostics cascading from library source. Phase 5.2 adds the SMILE-authored `Smile.UI.MenuNavigator` and Unicode-safe menu overflow/marker/geometry foundation; Phase 5.2.1 hardens active-edge coherence, stack cursors, proportional scrollbars, and indicator presentation without adding language syntax or native menu-flow helpers. Full details are in `phase5-ui.md`.

Phase 6 adds optional stable `ApplicationId` project identity and the source-authored `Smile.RPG` data/management package without adding syntax or RPG runtime helpers. Phase 6.1 advances Smile.RPG to 1.0.1 with save-boundary, rollback, asset-manifest identity, formatter-context, and Shop-result hardening. See `phase6-rpg.md`.

The executable examples are the most precise usage guide: `LanguageBasics.smile`, `StructuredLanguageBasics.smile`, `GraphicsBasics.smile`, `MultiFileBasics`, and the seven projects under `games`. These include Dungeon Star I's external-map parser and quadrilateral-based pseudo-3D renderer, Dungeon Star II's fixed-point DDA raycaster, and Maze Muncher's arc-composed neon maze. Each demo game also includes a complete player-focused `Program-NoDemo.smile` teaching source.
