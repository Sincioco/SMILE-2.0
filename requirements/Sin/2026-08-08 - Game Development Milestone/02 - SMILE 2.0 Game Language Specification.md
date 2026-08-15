# SMILE 2.0 Game Language Specification

## General rules

- Case-insensitive language.
- Official examples use uppercase keywords and PascalCase identifiers.
- Apostrophe begins a comment.
- Newline terminates a statement.
- Runtime scalar storage remains signed 64-bit integer.
- Booleans use `False = 0` and `True = 1`.
- Colors and keys are scalar values.
- Text remains literal-only for this milestone.

## Existing syntax remains valid

```smile
Score = 0
Dim Values[100]

If Score = 0 Then
    Print "Ready"
End If

For I = 0 To 9
    Values[I] = I
End For

Do
    Score = Score + 1
Loop Until Score = 10
```

## Lexical additions

Add:

```text
,  *  /
```

Allow underscores in identifiers:

```smile
KEY_LEFT
Game_Closed
```

## Constants

```smile
Const ScreenWidth = 960
Const CellSize = 20
Const MaximumSegments = 35 * 24
```

Constants are compile-time, initialized once, and cannot be assigned later.

## Arithmetic

Support:

```text
+  -  *  /  Mod
```

Precedence:

1. parentheses;
2. unary `-`, `Not`;
3. `*`, `/`, `Mod`;
4. `+`, `-`;
5. relational;
6. equality;
7. `And`;
8. `Or`.

## Arrays

```smile
Dim SnakeX[840]
Dim Board[10, 20]

Board[Column, Row] = RED
```

Rules:

- one or two dimensions;
- zero-based indices;
- dimensions are positive compile-time expressions;
- fixed lifetime storage;
- no array parameters or returns yet.

## Procedures

```smile
Sub DrawBlock(GridX, GridY, BlockColor)
    PixelX = GridX * CellSize
    PixelY = GridY * CellSize
    Fill Rectangle PixelX, PixelY, CellSize, CellSize, BlockColor
End Sub

Call DrawBlock(X, Y, GREEN)
```

- zero to four scalar parameters;
- no overloads, optional arguments, nesting, or required recursion;
- top-level variables and arrays are global;
- new names first assigned inside a routine are local unless an existing global exists.

## Functions

```smile
Function CanMove(OffsetX, OffsetY, TestRotation)
    If MoveIsBlocked = True Then
        Return False
    End If

    Return True
End Function

If CanMove(0, 1, Rotation) = True Then
    PieceY = PieceY + 1
End If
```

Functions may appear in expressions and conditions. Missing returns produce diagnostics.

## Flow control

```smile
Do
    Call UpdateGame()
Loop

Exit For
Exit Do
End Program
```

## Select Case

```smile
Select Case PieceType
    Case 1
        PieceColor = CYAN
    Case 2
        PieceColor = YELLOW
    Case Else
        PieceColor = WHITE
End Select
```

Exact scalar cases only. No fall-through.

## Keys

Predefined:

```text
KEY_NONE
KEY_W KEY_A KEY_S KEY_D
KEY_UP KEY_DOWN KEY_LEFT KEY_RIGHT
KEY_ENTER KEY_ESCAPE KEY_SPACE
KEY_1 KEY_2
```

Direction constants remain:

```text
UP Down LEFT RIGHT
```

Input:

```smile
Get Key Key

If Key_Held(KEY_LEFT) = True Then
    PaddleX = PaddleX - PaddleSpeed
End If
```

Alt+Enter is handled by the runtime, not returned as game input.

## Built-in functions

```smile
Elapsed = Timer()
Color = Rgb(40, 220, 30)
Distance = Abs(Value)
Smaller = Min(A, B)
Larger = Max(A, B)
Closed = Game_Closed()
```

## Colors

```text
BLACK WHITE RED GREEN BLUE CYAN MAGENTA YELLOW ORANGE GRAY
DARK_RED DARK_GREEN DARK_BLUE DARK_GRAY
LIGHT_RED LIGHT_GREEN LIGHT_BLUE LIGHT_GRAY
```

## Game window

```smile
Game Window "SMILE Snake"
```

Equivalent:

```smile
Game Window "SMILE Snake" Size 960 By 540
```

Only one window. Omitted size means 960×540. Game executables use the Windows GUI subsystem and show no console window.

## Graphics

```smile
Clear BLACK

Fill Rectangle X, Y, Width, Height, Color
Draw Rectangle X, Y, Width, Height, Color

Fill Rounded Rectangle X, Y, Width, Height, Radius, Color
Draw Rounded Rectangle X, Y, Width, Height, Radius, Color

Fill Circle CenterX, CenterY, Radius, Color
Draw Circle CenterX, CenterY, Radius, Color

Draw Line X1, Y1, X2, Y2, Color

Draw Text "Game OVER" At 480, 190 Size 54 Color RED Centered
Draw Number Score At 820, 120 Size 36 Color YELLOW

Show Screen
```

`Clear Screen` remains the console statement. `Clear Color` clears the game back buffer.

## Sound

```smile
Play Sound "Assets\Eat.wav"
Stop Sound
```

Asynchronous WAV playback. Missing sounds do not crash.

## Persistence

```smile
Load HighScore From "HighScore" Default 0
Save HighScore To "HighScore"
```

Integer values only. Storage is isolated per game and managed by the runtime.

## Diagnostics

Shared diagnostics must cover missing syntax, invalid constants, array dimensions, argument count, missing return, invalid exits, duplicate cases, duplicate windows, unknown built-ins, and invalid drawing/sound/storage use.

The Visual Studio extension must display these exact shared diagnostics.
