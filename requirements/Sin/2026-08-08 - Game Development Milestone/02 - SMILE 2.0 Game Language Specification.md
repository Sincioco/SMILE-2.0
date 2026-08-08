# SMILE 2.0 Game Language Specification

## General rules

- Case-insensitive language.
- Official examples use uppercase keywords and PascalCase identifiers.
- Apostrophe begins a comment.
- Newline terminates a statement.
- Runtime scalar storage remains signed 64-bit integer.
- Booleans use `FALSE = 0` and `TRUE = 1`.
- Colors and keys are scalar values.
- Text remains literal-only for this milestone.

## Existing syntax remains valid

```smile
Score = 0
DIM Values[100]

IF Score = 0 THEN
    PRINT "Ready"
END IF

FOR I = 0 TO 9
    Values[I] = I
END FOR

DO
    Score = Score + 1
LOOP UNTIL Score = 10
```

## Lexical additions

Add:

```text
,  *  /
```

Allow underscores in identifiers:

```smile
KEY_LEFT
GAME_CLOSED
```

## Constants

```smile
CONST ScreenWidth = 960
CONST CellSize = 20
CONST MaximumSegments = 35 * 24
```

Constants are compile-time, initialized once, and cannot be assigned later.

## Arithmetic

Support:

```text
+  -  *  /  MOD
```

Precedence:

1. parentheses;
2. unary `-`, `NOT`;
3. `*`, `/`, `MOD`;
4. `+`, `-`;
5. relational;
6. equality;
7. `AND`;
8. `OR`.

## Arrays

```smile
DIM SnakeX[840]
DIM Board[10, 20]

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
SUB DrawBlock(GridX, GridY, BlockColor)
    PixelX = GridX * CellSize
    PixelY = GridY * CellSize
    FILL RECTANGLE PixelX, PixelY, CellSize, CellSize, BlockColor
END SUB

CALL DrawBlock(X, Y, GREEN)
```

- zero to four scalar parameters;
- no overloads, optional arguments, nesting, or required recursion;
- top-level variables and arrays are global;
- new names first assigned inside a routine are local unless an existing global exists.

## Functions

```smile
FUNCTION CanMove(OffsetX, OffsetY, TestRotation)
    IF MoveIsBlocked = TRUE THEN
        RETURN FALSE
    END IF

    RETURN TRUE
END FUNCTION

IF CanMove(0, 1, Rotation) = TRUE THEN
    PieceY = PieceY + 1
END IF
```

Functions may appear in expressions and conditions. Missing returns produce diagnostics.

## Flow control

```smile
DO
    CALL UpdateGame()
LOOP

EXIT FOR
EXIT DO
END PROGRAM
```

## SELECT CASE

```smile
SELECT CASE PieceType
    CASE 1
        PieceColor = CYAN
    CASE 2
        PieceColor = YELLOW
    CASE ELSE
        PieceColor = WHITE
END SELECT
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
UP DOWN LEFT RIGHT
```

Input:

```smile
GET KEY Key

IF KEY_HELD(KEY_LEFT) = TRUE THEN
    PaddleX = PaddleX - PaddleSpeed
END IF
```

Alt+Enter is handled by the runtime, not returned as game input.

## Built-in functions

```smile
Elapsed = TIMER()
Color = RGB(40, 220, 30)
Distance = ABS(Value)
Smaller = MIN(A, B)
Larger = MAX(A, B)
Closed = GAME_CLOSED()
```

## Colors

```text
BLACK WHITE RED GREEN BLUE CYAN MAGENTA YELLOW ORANGE GRAY
DARK_RED DARK_GREEN DARK_BLUE DARK_GRAY
LIGHT_RED LIGHT_GREEN LIGHT_BLUE LIGHT_GRAY
```

## Game window

```smile
GAME WINDOW "SMILE Snake"
```

Equivalent:

```smile
GAME WINDOW "SMILE Snake" SIZE 960 BY 540
```

Only one window. Omitted size means 960×540. Game executables use the Windows GUI subsystem and show no console window.

## Graphics

```smile
CLEAR BLACK

FILL RECTANGLE X, Y, Width, Height, Color
DRAW RECTANGLE X, Y, Width, Height, Color

FILL ROUNDED RECTANGLE X, Y, Width, Height, Radius, Color
DRAW ROUNDED RECTANGLE X, Y, Width, Height, Radius, Color

FILL CIRCLE CenterX, CenterY, Radius, Color
DRAW CIRCLE CenterX, CenterY, Radius, Color

DRAW LINE X1, Y1, X2, Y2, Color

DRAW TEXT "GAME OVER" AT 480, 190 SIZE 54 COLOR RED CENTERED
DRAW NUMBER Score AT 820, 120 SIZE 36 COLOR YELLOW

SHOW SCREEN
```

`CLEAR SCREEN` remains the console statement. `CLEAR Color` clears the game back buffer.

## Sound

```smile
PLAY SOUND "Assets\Eat.wav"
STOP SOUND
```

Asynchronous WAV playback. Missing sounds do not crash.

## Persistence

```smile
LOAD HighScore FROM "HighScore" DEFAULT 0
SAVE HighScore TO "HighScore"
```

Integer values only. Storage is isolated per game and managed by the runtime.

## Diagnostics

Shared diagnostics must cover missing syntax, invalid constants, array dimensions, argument count, missing return, invalid exits, duplicate cases, duplicate windows, unknown built-ins, and invalid drawing/sound/storage use.

The Visual Studio extension must display these exact shared diagnostics.
