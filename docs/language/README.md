# SMILE 2.0 language

`src\Smile.Language` is the sole authority for SMILE source text, tokens, keyword facts, syntax, diagnostics, symbols, types, and semantic analysis. Both `smilec` and the Visual Studio extension consume the same `SmileLanguage.Analyze` result.

SMILE evolves only when current syntax cannot express a requirement clearly. New general-purpose features prefer readable, established BASIC wording; the smallest beginner-friendly C#-inspired concept is used only when BASIC has no suitable precedent. The language avoids aliases, multiple spellings, clever punctuation, and game-specific statements. Syntax, diagnostics, examples, and documentation change proportionally through the shared authority.

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

Drawing statements support filled or outlined rectangles, rounded rectangles, circles, and arbitrary four-corner quadrilaterals, plus outlined arcs, lines, literal text, and numbers. Quadrilaterals take four perimeter-ordered `(X, Y)` points followed by a color. `SHOW SCREEN` presents the logical canvas. `PLAY SOUND` starts an asynchronous WAV effect and missing files are safe. `LOAD` and `SAVE` persist integer values in storage isolated by executable name.

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

The executable examples are the most precise usage guide: `LanguageBasics.smile`, `StructuredLanguageBasics.smile`, `GraphicsBasics.smile`, and the ten projects under `games`. These include Dungeon Star I's external-map parser and quadrilateral-based pseudo-3D renderer, Dungeon Star II's fixed-point DDA raycaster, Maze Muncher's arc-composed neon maze, Star Squadron's full-width formation shooter, Platform Quest's fixed-point tile platforming and safe chunk fallback, and Sky Hopper's recycled procedural gate stream. Each demo game also includes a complete player-focused `Program-NoDemo.smile` teaching source.
