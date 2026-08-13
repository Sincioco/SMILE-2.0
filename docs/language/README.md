# SMILE 2.0 language

`src\Smile.Language` is the sole authority for SMILE source documents, tokens, keyword facts, syntax, diagnostics, symbols, types, and semantic analysis. Both `smilec` and the Visual Studio extension consume the same `SmileLanguage.Analyze` result, whether a compilation contains one source file or several.

## Modules and imports

`MODULE dotted.name` ... `END MODULE` declares a module. Declarations are private unless prefixed with `PUBLIC`; `PRIVATE` is available when explicit intent helps. A physical source imports a module with `IMPORT dotted.name AS Alias`, then accesses exported constants, arrays, functions, and subroutines as `Alias.Member`. Imports are scoped to that physical source. One module may span files from one provider, while duplicate providers, import cycles, private access, unknown members, and module access to consumer globals are diagnosed by the shared binder.

```smile
IMPORT Smile.Math.Extras AS Math
PRINT Math.Clamp(150, 0, 100)
```

Library compilation requires every source to declare a module. Application projects may also contain local module sources without packaging them.

SMILE evolves only when current syntax cannot express a requirement clearly. New general-purpose features prefer readable, established BASIC wording; the smallest beginner-friendly C#-inspired concept is used only when BASIC has no suitable precedent. The language avoids aliases, multiple spellings, clever punctuation, and game-specific statements. Syntax, diagnostics, examples, and documentation change proportionally through the shared authority.

SMILE is case-insensitive and line-oriented. An apostrophe starts a comment. Phase 3A values are signed 64-bit `NUMBER`, `BOOLEAN`, and mutable UTF-8 `TEXT` values.

## Explicit declarations and built-in types

`OPTION EXPLICIT` disables implicit variables for one physical source. In an application or support source it must be the first non-comment statement. In a module it follows `MODULE` and precedes imports and declarations. Sources without it retain legacy implicit variables.

```smile
OPTION EXPLICIT

DIM Score AS NUMBER
DIM IsAlive AS BOOLEAN
DIM Caption AS TEXT
DIM Names[10] AS TEXT
DIM Flags[10] AS BOOLEAN
DIM LegacyGrid[20, 15]
```

Scalar `DIM` requires `AS NUMBER`, `AS BOOLEAN`, or `AS TEXT`. Arrays may use those types; an untyped legacy array remains a `NUMBER` array. Defaults are `0`, `FALSE`, and `""`. `TEXT` supports value assignment, `+` concatenation, `=`/`<>` ordinal equality, constants, arrays, routine parameters/returns, `PRINT`, text `SELECT CASE`, and any `TEXT` expression in `DRAW TEXT`. There are no implicit conversions between the three built-in types.

Routine parameters accept `[BYVAL | BYREF] Name [AS Type]`. Missing mode means `BYVAL`; missing type preserves the legacy numeric calling convention, including converting a `BOOLEAN` argument to `0` or `1` for old untyped `BYVAL` routines. Explicitly typed parameters still require an exact type. A function can declare `AS NUMBER`, `AS BOOLEAN`, or `AS TEXT`; legacy omitted return types are inferred consistently from every value return. `BYREF` requires an exact-type writable scalar, array element, or writable parameter. Routine-local `DIM` declarations are visible from their declaration to routine end and may shadow a global.

```smile
SUB Rename(BYREF Name AS TEXT, NewName AS TEXT)
    Name = NewName
END SUB

FUNCTION Join(Left AS TEXT, Right AS TEXT) AS TEXT
    DIM Result AS TEXT
    Result = Left + Right
    RETURN Result
END FUNCTION
```

Native and Web calls have no four-parameter language restriction; the regression matrix covers 0, 1, 4, 5, 8, and 16 parameters.

## Multi-file programs

A compilation may contain one selected startup source and any number of support sources. Every file is parsed separately and retains its real path, lines, tokens, diagnostics, and debug locations; all files share one case-insensitive global symbol and routine model.

The startup source owns executable top-level statements, `GAME WINDOW`, and `END PROGRAM`. A support source may contain only top-level `CONST`, `DIM`, `SUB`, and `FUNCTION` declarations. Routine bodies retain the complete normal statement surface. The command-line form is:

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

Implemented control flow comprises multiline `IF`/`ELSE IF`/`ELSE`, `FOR ... TO`, `FOR ... DOWN TO`, `DO ... LOOP`, `DO ... LOOP UNTIL`, `EXIT FOR`, `EXIT DO`, and `SELECT CASE`. Procedures and functions use `SUB`, `FUNCTION`, `CALL`, and `RETURN`, including typed `BYVAL`/`BYREF` parameters and typed returns.

The expression surface includes `+`, `-`, `*`, integer `/`, `MOD`, comparisons, parentheses, unary `-` and `NOT`, and boolean `AND`/`OR`. Built-in functions are `TIMER()`, `RGB(r, g, b)`, `ABS(value)`, `MIN(a, b)`, `MAX(a, b)`, `GAME_CLOSED()`, and `KEY_HELD(key)`.

## Game surface

```smile
GAME WINDOW "Example" SIZE 960 BY 540

LOAD HighScore FROM "HighScore" DEFAULT 0
PLAY SOUND "Assets\Start.wav"
MUSIC VOLUME 70
PLAY MUSIC "Assets\Background.mp3" LOOP

DO
    GET KEY Key
    CLEAR RGB(12, 18, 30)
    FILL ROUNDED RECTANGLE 380, 450, 200, 22, 7, LIGHT_BLUE
    FILL QUADRILATERAL 0, 0, 240, 80, 240, 460, 0, 540, DARK_GREEN
    DRAW QUADRILATERAL 0, 0, 240, 80, 240, 460, 0, 540, LIGHT_GREEN
    DRAW CIRCLE 480, 300, 12, WHITE
    DRAW ARC 480, 300, 40, 180, 90, LIGHT_BLUE
    DRAW LINE 40, 40, 920, 40, DARK_GRAY
    DRAW TEXT "SCORE" AT 40, 15 SIZE 18 COLOR CYAN
    DRAW NUMBER HighScore AT 130, 10 SIZE 28 COLOR YELLOW
    SHOW SCREEN
    WAIT 16 MILLISECONDS
LOOP UNTIL GAME_CLOSED() = TRUE

SAVE HighScore TO "HighScore"
STOP MUSIC
STOP SOUND
END PROGRAM
```

Drawing statements support filled or outlined rectangles, rounded rectangles, circles, and arbitrary four-corner quadrilaterals, plus outlined arcs, lines, text expressions, and numbers. Quadrilaterals take four perimeter-ordered `(X, Y)` points followed by a color. `SHOW SCREEN` presents the logical canvas. `PLAY SOUND` starts an asynchronous WAV effect and missing files are safe. `LOAD` and `SAVE` persist integer values in storage isolated by executable name.

## Phase 3A diagnostics

| Code | Meaning |
|---|---|
| `SML3300` | `OPTION EXPLICIT` is late or duplicated. |
| `SML3301` | A declaration names an unknown type. |
| `SML3302` | A scalar `DIM` omits `AS Type`. |
| `SML3303` | `OPTION EXPLICIT` requires a declaration. |
| `SML3304` | Assignment, argument, case, or return types do not match. |
| `SML3305` | A `BYREF` argument is not an exact-type writable location. |
| `SML3306` | A routine duplicates a parameter or local. |
| `SML3307` | A local is used before its `DIM`. |
| `SML3308` | `TEXT` is used with an unsupported or mixed-type operator. |
| `SML3309` | A legacy function has inconsistent inferred return types. |
| `SML3310` | A typed declaration or return-type context is unsupported. |

### Arc drawing

```smile
DRAW ARC CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

`DRAW ARC` draws only the curved outline using the normal one-logical-pixel graphics stroke. It does not fill a pie slice, draw a chord, or connect either endpoint to the center. `FILL ARC` is not part of the language.

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
DIM FileBytes[8192]
LOAD TEXT FILE "Maps\default.map" INTO FileBytes COUNT FileByteCount
```

The path must be a non-empty literal, the destination must be a one-dimensional numeric array, and `COUNT` must name a writable numeric variable. The runtime zero-fills the complete destination, reads UTF-8 bytes, skips an optional UTF-8 BOM, copies at most the array capacity as values from 0 through 255, and stores the copied byte count. Missing, inaccessible, empty, or unreadable files safely produce count zero. Existing integer persistence keeps its distinct `LOAD Value FROM "Key" DEFAULT 0` form.

Dungeon Star I provides the complete multi-floor game-side example: three literal-path loaders feed one bounded byte parser in `games\DungeonStarI\Program.smile`. Dungeon Star II uses the same generic statement for compatible one-floor room maps in `games\DungeonStarII\Program.smile`. Platform Quest parses and validates its 120-by-15 platform maps in `games\PlatformQuest\Program.smile` and falls back to safe source-defined chunks. The language/runtime only delivers bytes; headers, symbols, dimensions, topology, support rules, and fallback behavior remain ordinary SMILE source.

Background-music syntax is:

```smile
PLAY MUSIC "Assets\Background.mp3"
PLAY MUSIC "Assets\Background.mp3" LOOP
PAUSE MUSIC
RESUME MUSIC
STOP MUSIC
MUSIC VOLUME 50
```

Music paths are resolved relative to the generated executable. `MUSIC VOLUME` accepts a numeric expression; the native runtime clamps the requested level to 0 through 100. MP3 playback uses the Windows `Windows.Media.Playback.MediaPlayer` API through C++/WinRT and Windows Media Foundation, independently of the selected graphics backend. No third-party decoder is bundled. Windows installations missing required media components fail playback safely without terminating the game.

### Automatic focus behavior

Every `GAME WINDOW` program inherits the same native focus behavior without adding SMILE activation code:

- loss of application activation, top-level window activation, or minimization immediately silences that game's audio;
- MP3 playback continues silently at effective volume zero, preserving both playback position and the exact requested `MUSIC VOLUME`;
- restoring an active, non-minimized window reapplies the requested volume without restarting playback or resuming a track paused or stopped by the program;
- the current asynchronous WAV effect stops on focus loss, and new `PLAY SOUND` requests are suppressed while inactive rather than queued for later;
- Windows master volume and other applications are never changed;
- DirectX and GDI follow the identical shared runtime policy.

Named input constants include `KEY_W`, `KEY_A`, `KEY_S`, `KEY_D`, the four arrows, `KEY_ENTER`, `KEY_ESCAPE`, `KEY_SPACE`, `KEY_1`, `KEY_2`, `KEY_OTHER`, and `KEY_NONE`. `GET KEY` returns `KEY_OTHER` (value `19`) for an otherwise unnamed ordinary key event; `KEY_HELD(KEY_OTHER)` is always false. Named colors include the standard red/green/blue/cyan/magenta/yellow set plus orange, gray, dark variants, light variants, black, and white.

The executable examples are the most precise usage guide: `LanguageBasics.smile`, `StructuredLanguageBasics.smile`, `GraphicsBasics.smile`, `MultiFileBasics`, and the ten projects under `games`. These include Dungeon Star I's external-map parser and quadrilateral-based pseudo-3D renderer, Dungeon Star II's fixed-point DDA raycaster, Maze Muncher's arc-composed neon maze, Star Squadron's full-width formation shooter, Platform Quest's fixed-point tile platforming and safe chunk fallback, and Sky Hopper's recycled procedural gate stream. Each demo game also includes a complete player-focused `Program-NoDemo.smile` teaching source.
