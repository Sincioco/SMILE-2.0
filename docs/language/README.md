# SMILE 2.0 language

`src\Smile.Language` is the sole authority for SMILE source text, tokens, keyword facts, syntax, diagnostics, symbols, types, and semantic analysis. Both `smilec` and the Visual Studio extension consume the same `SmileLanguage.Analyze` result.

SMILE is case-insensitive and line-oriented. An apostrophe starts a comment. Values are signed 64-bit numbers, booleans, or text literals; runtime game state is normally stored in numeric variables and fixed arrays.

## Structured example

```smile
CONST Width = 12
CONST Height = 7
DIM Bricks[Width, Height]

SUB SetBrick(Column, Row, Value)
    Bricks[Column, Row] = Value
END SUB

FUNCTION PointsFor(Row)
    RETURN 70 - Row * 10
END FUNCTION

CALL SetBrick(0, 0, 1)

SELECT CASE PointsFor(0)
    CASE 70
        PRINT "TOP ROW"
    CASE ELSE
        PRINT "OTHER ROW"
END SELECT
```

Implemented control flow comprises multiline `IF`/`ELSE IF`/`ELSE`, `FOR ... TO`, `FOR ... DOWN TO`, `DO ... LOOP`, `DO ... LOOP UNTIL`, `EXIT FOR`, `EXIT DO`, and `SELECT CASE`. Procedures and functions use `SUB`, `FUNCTION`, `CALL`, and `RETURN`, with at most four scalar parameters.

The expression surface includes `+`, `-`, `*`, integer `/`, `MOD`, comparisons, parentheses, unary `-` and `NOT`, and boolean `AND`/`OR`. Built-in functions are `TIMER()`, `RGB(r, g, b)`, `ABS(value)`, `MIN(a, b)`, `MAX(a, b)`, `GAME_CLOSED()`, and `KEY_HELD(key)`.

## Game surface

```smile
GAME WINDOW "Example" SIZE 960 BY 540

LOAD HighScore FROM "HighScore" DEFAULT 0
PLAY SOUND "Assets\Start.wav"

DO
    GET KEY Key
    CLEAR RGB(12, 18, 30)
    FILL ROUNDED RECTANGLE 380, 450, 200, 22, 7, LIGHT_BLUE
    FILL QUADRILATERAL 0, 0, 240, 80, 240, 460, 0, 540, DARK_GREEN
    DRAW QUADRILATERAL 0, 0, 240, 80, 240, 460, 0, 540, LIGHT_GREEN
    DRAW CIRCLE 480, 300, 12, WHITE
    DRAW LINE 40, 40, 920, 40, DARK_GRAY
    DRAW TEXT "SCORE" AT 40, 15 SIZE 18 COLOR CYAN
    DRAW NUMBER HighScore AT 130, 10 SIZE 28 COLOR YELLOW
    SHOW SCREEN
    WAIT 16 MILLISECONDS
LOOP UNTIL GAME_CLOSED() = TRUE

SAVE HighScore TO "HighScore"
STOP SOUND
END PROGRAM
```

Drawing statements support filled or outlined rectangles, rounded rectangles, circles, and arbitrary four-corner quadrilaterals, plus lines, literal text, and numbers. Quadrilaterals take four perimeter-ordered `(X, Y)` points followed by a color. `SHOW SCREEN` presents the logical canvas. `PLAY SOUND` is asynchronous and missing files are safe. `LOAD` and `SAVE` persist integer values in storage isolated by executable name.

Named input constants include `KEY_W`, `KEY_A`, `KEY_S`, `KEY_D`, the four arrows, `KEY_ENTER`, `KEY_ESCAPE`, `KEY_SPACE`, `KEY_1`, `KEY_2`, `KEY_OTHER`, and `KEY_NONE`. `GET KEY` returns `KEY_OTHER` (value `19`) for an otherwise unnamed ordinary key event; `KEY_HELD(KEY_OTHER)` is always false. Named colors include the standard red/green/blue/cyan/magenta/yellow set plus orange, gray, dark variants, light variants, black, and white.

The executable examples are the most precise usage guide: `LanguageBasics.smile`, `StructuredLanguageBasics.smile`, `GraphicsBasics.smile`, and the four projects under `games`.
