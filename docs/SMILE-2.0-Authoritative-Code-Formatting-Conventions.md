# SMILE 2.0 Authoritative Code Formatting Conventions

Version: 2026-08-15
Owner: Sin
Authoritative implementation partner: Codex

## Instructions for ChatGPT

Use this entire document whenever you generate, rewrite, explain, or format SMILE 2.0 code.

These conventions were worked out directly between Sin and Codex against real SMILE 2.0 programs. The Codex-maintained convention is authoritative. If ChatGPT, another assistant, an older prompt, or a generic BASIC style guide provides conflicting formatting guidance, follow this document instead.

Only a later explicit instruction from Sin may revise these conventions. Do not silently merge a conflicting style into them. If Sin supplies a new example, treat the example as a refinement and preserve every earlier rule that it does not explicitly replace.

Authority order:

1. Sin's latest explicit instruction.
2. The Codex-maintained rules in this document and the repository `AGENTS.md`.
3. The current canonical examples in `games\Snake` and `games\PaddleBall`.
4. ChatGPT suggestions or generic language-formatting advice.

When formatting existing source, preserve behavior. Do not add, remove, or rewrite program logic merely to satisfy layout rules.

## Scope

Apply these conventions to:

- `.smile` programs;
- modules and libraries;
- games and examples;
- Visual Studio templates and generated source;
- tutorials and embedded code samples;
- diagnostics that display SMILE code;
- future ChatGPT and Codex generated SMILE code.

Apply the style to new code and code being substantively edited or reorganized. Do not churn otherwise untouched legacy files solely for casing or routine ordering unless Sin requests a formatting pass.

## Capitalization and naming

Use Visual Basic-style initial capitalization and readable BASIC conventions.

- Write keywords as `Dim`, `As`, `If`, `Then`, `Else`, `End If`, `For`, `End For`, `Do`, `Loop`, `Sub`, `Function`, `Call`, and `Game Window`.
- Use PascalCase for ordinary variables, parameters, routines, modules, and fields.
- Never write keywords, variables, or parameters in all uppercase.
- Constants may remain uppercase, including `KEY_ENTER`, `KEY_ESCAPE`, and project constants such as `MAX_ITEMS`.
- Preserve established brands and acronyms such as SMILE, UI, VSIX, API, and IDE.
- Use title or initial capitalization for short menu items, labels, and instructional phrases.
- Use normal English sentence capitalization for complete sentences.
- Treat capitalization as an authoring convention, not a request to make SMILE case-sensitive.

## Indentation and blank-line foundation

- Use four spaces for every indentation and continuation level.
- Never indent SMILE code with tabs.
- Use exactly one blank line at a required separation point.
- Never leave double or triple blank lines.
- Keep statements that form one small logical group together.
- Separate different logical phases with one blank line.
- Follow the last consecutive `Const`, `Module`, `Import`, or `Dim` declaration in a group with one blank line.
- Put `Option Explicit` on a separated line with one blank line before and after it, except where the file boundary already supplies the separation.

## Startup-file structure

The startup file must tell the program's executable story near the top.

This is a permanent rule and convention for every executable SMILE program. Every executable startup file must create its `Game Window` near the top and place its primary `Do...Loop` game loop immediately after window creation and startup setup. Supporting `Sub` and `Function` implementations must follow the complete main executable flow; they must never make students scroll to find window creation or the game loop.

Use this order:

1. File header comment.
2. Options and imports.
3. Constants.
4. Shared variables and initial state.
5. Load statements and other minimum startup preparation.
6. `Game Window` near the top and before helper routine implementations.
7. Initial startup calls.
8. The primary `Do...Loop` game loop.
9. Top-level shutdown and cleanup.
10. Supporting `Sub` and `Function` implementations.
11. Final `End Program` when the program uses it.

Students should see that the game window is created and the main loop begins without scrolling past helper routines.

Keep input, state updates, drawing, `Show Screen`, and the loop condition together as the main program story. Put cohesive reusable helpers in their own clearly named modules when that makes the startup source easier to read.

Canonical structural references:

- `games\Snake\Program.smile`;
- `games\Snake\Program-NoDemo.smile`;
- `games\PaddleBall\Program.smile`;
- `games\PaddleBall\Program-NoDemo.smile`.

## Canonical startup skeleton

```smile
' SMILE 2.0 example game.
Const CanvasWidth = 960
Const CanvasHeight = 540

State = 0
Key = KEY_NONE

'----------------------------------------------------------------------------------------------------
Load HighScore From "HighScore" Default 0

Game Window "SMILE 2.0 Example"

Call EnterTitle()

Do
    Get Key Key

    If State = 0 Then
        Call DrawTitle()
    Else
        Call DrawBoard()
        Call DrawOverlay()
    End If

    Show Screen

Loop Until Game_Closed() = True

Stop Sound

'----------------------------------------------------------------------------------------------------

Sub EnterTitle()

    State = 0
    Key = KEY_NONE

End Sub

Sub DrawTitle()

    Clear BLACK

End Sub

Sub DrawBoard()

    Clear BLACK

End Sub

Sub DrawOverlay()

    Draw Text "Ready" At 480, 270 Size 32 Color WHITE Centered

End Sub

End Program
```

## Routine declarations and endings

- Follow every `Function`, `Sub`, or future procedure declaration with one blank line.
- Put one blank line before `End Sub`.
- Keep routines grouped by responsibility and, when practical, in the order the main flow introduces or calls them.

```smile
Sub ResetGame()

    Score = 0
    State = 1

End Sub
```

## Returning from functions

A function may directly return a variable, a constant, or a literal value such as `True`, `False`, a number, or a string.

Only a computed or evaluated expression must **not** be returned directly. Assign that expression to a clearly named local variable first. The student cannot know the value of the expression until it is evaluated; the variable lets the student print, hover over, or watch the evaluated value before it is returned.

Keep one blank line between the final `Return` and `End Function`.

```smile
Function IsInsideField(X, Y) As Boolean

    Dim ReturnValue As Boolean

    ReturnValue = X >= 0 And X < FieldWidth And Y >= 0 And Y < FieldHeight

    Return ReturnValue

End Function
```

These direct returns are also valid:

```smile
Return False
Return MAX_ITEMS
Return 0
Return "Ready"
```

Do not generate this:

```smile
Function IsInsideField(X, Y) As Boolean

    Return X >= 0 And X < FieldWidth And Y >= 0 And Y < FieldHeight

End Function
```

## If-block decision: compact or expanded

Decide compactness across the complete `If...Else If...Else...End If` block, not one branch at a time.

### Compact If block

Keep the whole block compact only when every branch:

- contains no more than two direct statements;
- contains no nested control block; and
- does not contain multiple logical phases that warrant separation.

In a compact block, do not put blank lines after branch headers or before the next `Else If`, `Else`, or `End If`.

```smile
If Key = KEY_ESCAPE Then
    End Program
Else If Key = KEY_ENTER Then
    Call ResetGame()
End If
```

A branch containing only one consecutive group of calls also remains compact:

```smile
If State = 0 Then
    Call DrawTitle()
Else
    Call DrawBoard()
    Call DrawGameOver()
End If
```

### Expanded If block

Expand the complete block when any branch:

- contains three or more direct statements;
- contains nested control flow; or
- contains multiple logical phases.

In an expanded block:

- put one blank line immediately after every `If...Then`, `Else If...Then`, and `Else` header;
- put one blank line before every following `Else If`, `Else`, and `End If`;
- preserve logical statement-group spacing inside each branch.

```smile
If BallY - BallRadius <= 8 Then

    BallY = 8 + BallRadius
    BallYSub = BallY * SubpixelsPerPixel
    BallVY = Abs(BallVY)

    Play Sound "Assets\Wall.wav"

Else If BallY + BallRadius >= CanvasHeight - 8 Then

    BallY = CanvasHeight - 8 - BallRadius
    BallYSub = BallY * SubpixelsPerPixel
    BallVY = -Abs(BallVY)

    Play Sound "Assets\Wall.wav"

End If
```

Another expanded example:

```smile
If Collision = True Then

    Call EndRound()

Else If SnakeX[0] = FoodX And SnakeY[0] = FoodY Then

    SnakeX[Length] = TailX
    SnakeY[Length] = TailY
    Length = Length + 1
    Score = Score + 10
    MoveDelay = Max(MinimumDelay, StartDelay - (Score / 50) * 4)

    Play Sound "Assets\Eat.wav"
    Call SpawnFood()

End If
```

Follow `End If` with one blank line unless the file or enclosing structure already supplies the boundary.

## Long If conditions

Keep `If ... Then` on one line only when:

- the complete rendered line is 100 characters or fewer; and
- the condition has no more than two top-level Boolean clauses.

Otherwise:

- enclose the complete condition in parentheses;
- put each continued condition on its own line one indentation level deeper;
- leave `And` or `Or` at the end of every continued line except the last.

```smile
If (Style.CursorWidth < 0 Or
    Style.CursorWidth > Core.UI_MAX_LAYOUT_VALUE Or
    Style.CursorHeight < 0 Or
    Style.CursorHeight > Core.UI_MAX_LAYOUT_VALUE) Then
    Return False
End If
```

Do not put the logical operator at the beginning of the continuation line.

## For...End For spacing

Put one blank line before every `For` statement unless a blank line already exists.

### Compact For loop

Keep a `For...End For` loop compact when its body:

- contains no more than four direct statements; and
- contains no nested control block.

Do not put a blank line after `For` or before `End For` in a compact loop.

```smile
For I = Length - 1 Down To 1
    SnakeX[I] = SnakeX[I - 1]
    SnakeY[I] = SnakeY[I - 1]
End For
```

### Expanded For loop

For a longer or structurally nested loop, put one blank line immediately after `For`. Nested block spacing naturally leaves a blank line before `End For`.

```smile
For I = 0 To Length - 1

    If FoodX = SnakeX[I] And FoodY = SnakeY[I] Then
        FoodValid = False
    End If

End For
```

## Do...Loop spacing

- Put one blank line before `Do` unless one already exists.
- Put one blank line before `Loop` unless one already exists.
- Follow `Loop` with one blank line unless a file boundary applies.
- Do not automatically insert a blank line immediately after every `Do`; use the surrounding logical structure.

```smile
Do
    Get Key Key
    Show Screen

Loop Until Game_Closed() = True
```

## Call groups

Treat consecutive `Call` statements as one visual group.

- Put one blank line before the first call when surrounding ordinary statements precede it.
- Keep consecutive calls together without blank lines between them.
- Put one blank line after the last call.
- Compact If-block spacing takes priority when the branch consists only of its compact statements or one call group.

```smile
Score = 0
MoveDelay = StartDelay

Call SpawnFood()

NextMoveTime = Timer() + StartDelay
State = 1
```

```smile
Call UpdatePaddles()
Call UpdateBall()

Accumulator = Accumulator - SimulationStep
```

## Play Sound groups

Treat consecutive `Play Sound` statements as one visual group.

- Put one blank line before the first `Play Sound`.
- Keep consecutive `Play Sound` statements together without blank lines between them.
- Put one blank line after the last `Play Sound`.
- `Play Sound` spacing overrides compact If-block spacing, including before `Else If`, `Else`, and `End If`.

```smile
NextMoveTime = Timer() + StartDelay
State = 1

Play Sound "Assets\Start.wav"

End Sub
```

An explicitly related sound and call may remain one logical action group:

```smile
Play Sound "Assets\Eat.wav"
Call SpawnFood()

End If
```

## Other statement groups

- Follow the last consecutive `Unload` statement in a group with one blank line.
- Preserve one blank line between separate initialization, input, update, drawing, audio, persistence, and cleanup phases.
- Do not insert decorative blank lines inside a short logical group.

## UI text, instructions, and prose

- Use title or initial capitalization for short labels, menu items, and instructional phrases.
- Use normal English sentence capitalization for complete sentences.
- Do not render ordinary UI words or phrases in all uppercase.
- Preserve SMILE and established technical acronyms.

Examples:

```smile
Draw Text "Game Over" At 480, 185 Size 50 Color RED Centered
Draw Text "Press Enter To Start" At 480, 335 Size 28 Color YELLOW Centered
Draw Text "Escape To Exit" At 480, 415 Size 18 Color LIGHT_GRAY Centered
```

## ChatGPT generation checklist

Before returning generated SMILE code, verify all of the following:

- Keywords use Visual Basic-style initial capitalization.
- Ordinary identifiers use PascalCase and are not all uppercase.
- Constants are the only ordinary code identifiers allowed to remain uppercase.
- Four spaces are used; no tabs exist.
- No double or triple blank lines exist.
- `Game Window` is near the top and the main loop immediately follows startup setup, before every helper routine implementation in each executable startup file.
- Each If block was classified using the complete-block compact/expanded rule.
- Long conditions use parenthesized continuation with trailing Boolean operators.
- Each For loop was classified as compact or expanded.
- Call groups have correct surrounding spacing.
- Play Sound groups have correct surrounding spacing and override compact branch spacing.
- Functions directly return variables, constants, and literal values; computed or evaluated expressions are never returned directly and first go into named variables.
- Routine declarations and endings have the required blank lines.
- UI text uses title capitalization or normal sentence capitalization as appropriate.
- Formatting did not alter program behavior.

## Repository enforcement and conflict handling

Within the SMILE 2.0 repository, the following are the Codex-maintained authorities:

- `AGENTS.md` for permanent project instructions;
- this document for the self-contained ChatGPT handoff;
- `scripts\format-smile-style.ps1` for mechanical formatting enforcement;
- `scripts\test-smile-formatter.ps1` for source-rewriter, scope, transaction, and idempotence safety;
- current canonical source in `games\Snake` and `games\PaddleBall` for concrete examples.

If ChatGPT produces a conflicting convention:

1. Do not apply the conflicting rule.
2. Keep the Codex-maintained convention.
3. Explain the conflict to Sin when necessary.
4. Change the convention only after Sin explicitly approves the new rule.

Codex's maintained version takes precedence over ChatGPT-generated formatting guidance unless Sin explicitly says otherwise.

The formatter uses the shared `Smile.Language` parser, syntax tree, semantic model, and symbol service for Return expressions, long If conditions, and contextual identifiers. Its default repository scope is tracked `.smile` files only. `-IncludeUntracked` opts into nonignored untracked sources, while `-Files` explicitly targets existing `.smile` paths, including untracked paths. `-Check` is read-only. Mutating runs analyze every output and verify source hashes before committing any atomic replacement; a failed preflight or concurrent change commits no formatter output. The focused formatter tests and repository-wide `-Check -FormatLongIf` run near the start of `scripts\smoke-test.cmd` as a permanent gate.
