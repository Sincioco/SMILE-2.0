# Dungeon Star I
## Approved Specification and Codex Implementation Instructions

**Repository:** `Sincioco/SMILE-2.0`  
**Local repository:** `D:\SMILE 2.0`  
**Verified baseline commit:** `7d7f05a1e6bdf7a36b529270015c2d10f08fc724`  
**Game display name:** `Dungeon Star I`  
**Game folder:** `games\DungeonStarI`  
**Game executable:** `DungeonStarI.exe`

---

# 0. Codex operating directive

Read `AGENTS.md` first, then read this entire file before changing code.

Use the repository's existing KISS rules:

- Extend the working architecture instead of replacing it.
- Keep `src\Smile.Language` as the only language authority.
- Keep all dungeon generation, movement, animation, title-screen, attract-mode, idle-timeout, and demo-player rules in `.smile` source.
- Add only generic native runtime services that other SMILE programs could reuse.
- Do not add a game engine, third-party library, package, framework, scripting layer, duplicate parser, duplicate keyword list, or game-specific native dungeon helper.
- Do not stop after planning or scaffolding. Continue through implementation, build, native compilation, execution, debugging, validation, documentation, commit, and push.
- Never discard, reset, overwrite, or clean uncommitted user work.
- Do not amend, rebase, force-push, or rewrite pushed history.
- Do not ask Sin to choose between minor implementation details covered by this specification. Use the smallest correct design and continue.

Before editing:

1. Run `git status --short`.
2. Run `git log -1 --oneline`.
3. Record the actual starting commit.
4. The expected observed baseline is `7d7f05a1e6bdf7a36b529270015c2d10f08fc724`.
5. If `HEAD` is newer, do **not** reset to the expected commit. Inspect the newer work, preserve it, and adapt these instructions to the current architecture.
6. If the worktree is not clean, preserve all existing changes and avoid overlapping them unless they are clearly part of this task.

Store a permanent copy of the approved requirements in the repository at:

```text
requirements\Sin\2026-08-09 - Dungeon Star I Milestone\
    00 - START HERE - Dungeon Star I Approved Specification and Codex Instructions.md
```

The repository copy may omit the “Verified baseline” wording only if the actual starting commit is newer, but it must preserve all user-visible behavior and Definition of Done requirements in this document.

---

# 1. Verified starting architecture

The current SMILE 2.0 code already has the correct foundation:

- Native Windows x64 compilation through MASM and the native runtime.
- A backend-neutral graphics API.
- `Auto`, `DirectX`, and `GDI` project/backend selection.
- A DirectX backend using Direct3D 11, DXGI, Direct2D, and DirectWrite.
- A physical-output GDI backend.
- VSync-default presentation and QPC-based timing.
- A logical 960-by-540 game canvas with automatic resize, DPI, aspect-ratio, and Alt+Enter handling.
- Queued key-press events and held-state support.
- Fixed one- and two-dimensional arrays.
- Integer arithmetic, procedures, functions, loops, conditionals, and `SELECT CASE`.
- The existing game projects and smoke/artifact verification pipeline.

Do **not** redo the DirectX/GDI milestone. Build this game on top of it.

Two generic gaps must be filled before the dungeon can be implemented correctly:

1. SMILE has no filled arbitrary four-corner shape. Perspective walls, floors, ceilings, doors, and stairs need a reusable quadrilateral primitive.
2. `GET KEY` currently reports only the small predefined key set. It cannot truthfully implement **PRESS ANY KEY** because an otherwise valid key such as `Q`, `F`, `3`, Shift, or a function key currently becomes `KEY_NONE`.

Add the smallest generic language/runtime extensions described below.

---

# 2. Product objective

Create a fifth public SMILE 2.0 game sample named:

```text
Dungeon Star I
```

It must be an original, first-person, grid-based, pseudo-3D dungeon exploration sample inspired by the presentation style of early 1980s console dungeon crawlers.

The user-provided visual-motion reference is:

```text
https://www.youtube.com/watch?v=imlbwFg9Peo
```

Use that only to understand the feel of:

- centered first-person corridors;
- perspective walls, floor, and ceiling;
- brick-like line patterns;
- smooth forward/backward movement;
- smooth 90-degree left/right turns;
- palette-swapped dungeon areas.

Do **not** copy or extract:

- source code;
- frames;
- screenshots;
- textures;
- logos;
- character art;
- music;
- sound effects;
- map layouts;
- names or branding belonging to Sega or Phantasy Star.

All committed visuals and code must be original. Public documentation should call this an original pseudo-3D dungeon exploration sample, not a “Phantasy Star clone.”

---

# 3. Approved game scope

The game is intentionally only about walking around a dungeon.

Required:

- Title screen.
- Flashing `PRESS ANY KEY TO START`.
- Arcade-style attract/demo countdown and self-playing demo.
- A newly generated dungeon for each user run and demo run.
- Three connected dungeon floors.
- Random rectangular rooms.
- Random connecting corridors.
- Random closed doors at room/corridor boundaries.
- Stairs that allow travel down and back up.
- Floor-specific green, blue, and red palettes.
- First-person pseudo-3D rendering.
- Smooth forward movement.
- Smooth backward movement.
- Smooth 90-degree left and right turns.
- Door-opening animation.
- Stair/floor transition animation.
- User idle warning and automatic return to title.
- Windowed and full-screen operation through the existing runtime.
- Both DirectX and GDI backends.

Not required and explicitly out of scope:

- Combat.
- Enemies.
- NPCs.
- Party members.
- Player statistics.
- Health, magic, experience, levels, or classes.
- Inventory.
- Items.
- Treasure.
- Shops.
- Dialogue.
- Quests.
- Score.
- High score.
- Saving dungeon progress.
- Mouse input.
- Controller input.
- Networking.
- Real 3D models.
- Textures or image loading.
- Raycasting.
- Direct3D game-specific rendering code.
- A separate native dungeon renderer.
- Audio or music for this milestone.
- More than one `.smile` source file in the game project.
- Increasing the four-parameter limit for user-defined routines.
- Floating-point language support.

The pseudo-3D effect must be produced by ordinary 2D quadrilateral and line drawing from `.smile` code.

---

# 4. Required generic SMILE language additions

## 4.1 Quadrilateral drawing

Add these two official statements:

```smile
FILL QUADRILATERAL X1, Y1, X2, Y2, X3, Y3, X4, Y4, Color

DRAW QUADRILATERAL X1, Y1, X2, Y2, X3, Y3, X4, Y4, Color
```

Example:

```smile
FILL QUADRILATERAL 0, 0, 240, 80, 240, 460, 0, 540, DARK_GREEN
DRAW QUADRILATERAL 0, 0, 240, 80, 240, 460, 0, 540, LIGHT_GREEN
```

Official rules:

- `QUADRILATERAL` is one keyword.
- There are exactly nine numeric arguments.
- The first eight values are four `(X, Y)` points.
- The ninth value is the color.
- Points are connected in this order:

```text
Point 1 -> Point 2 -> Point 3 -> Point 4 -> Point 1
```

- Callers should provide points in clockwise or counterclockwise perimeter order.
- Self-intersecting point order has unspecified visual results but must never crash.
- Degenerate shapes must be safe.
- Coordinates may be outside the logical canvas; normal viewport clipping applies.
- `FILL QUADRILATERAL` fills with one solid color.
- `DRAW QUADRILATERAL` draws a one-logical-pixel outline, scaled through the active backend in the same way as existing lines.
- No alpha, gradients, textures, or additional shape options are part of this milestone.
- Both statements require `GAME WINDOW`.
- All arguments must type-check as `NUMBER`.

Do **not** use the shorter keyword `QUAD`, and do not add aliases. `QUADRILATERAL` is more descriptive and teaches the correct geometric term.

Do **not** increase user-defined routine parameters from four to nine. The graphics statement itself may have nine arguments because it is compiler-defined syntax, not a user routine.

### Required language/compiler changes

Update the shared implementation, not separate consumers:

#### `src\Smile.Language\Syntax.cs`

- Add `QuadrilateralKeyword` inside the normal keyword range, before the current final normal keyword boundary used by `SyntaxFacts.IsKeyword`.
- Add `"QUADRILATERAL"` to the shared keyword dictionary.
- Keep classification automatic through `SyntaxFacts`; do not add a Visual Studio-only keyword table.

#### `src\Smile.Language\GameSyntax.cs`

Add:

```text
GraphicsOperation.FillQuadrilateral
GraphicsOperation.DrawQuadrilateral
```

#### `src\Smile.Language\Parser.cs`

Extend `ParseGraphicsStatement`:

- `FILL QUADRILATERAL` parses exactly nine numeric expressions.
- `DRAW QUADRILATERAL` parses exactly nine numeric expressions.
- Improve the fill error text so it includes `QUADRILATERAL`.
- Preserve all existing graphics syntax and diagnostics.

#### `src\Smile.Language\Semantics.cs`

The existing generic graphics-argument numeric validation should remain the main path. Add special logic only if genuinely needed. Do not duplicate numeric argument rules.

#### `src\Smile.Compiler\MasmEmitter.cs`

Add compiler-facing native exports:

```text
smile_fill_quadrilateral
smile_draw_quadrilateral
```

Map the new graphics operations to those exports.

The current native-call emitter already supports arguments beyond the first four by placing remaining Windows x64 arguments on the stack. Verify that a nine-argument call is emitted correctly. Do not rewrite the call emitter or increase its reserved stack space unless a failing regression proves that a change is required.

## 4.2 Generic “other key” event

Add this built-in key constant:

```smile
KEY_OTHER
```

Assign it the stable numeric value:

```text
19
```

Official behavior:

- Known keys continue to return their existing named constants.
- Any otherwise unrecognized ordinary key-down event returns `KEY_OTHER`.
- `KEY_OTHER` is an event category, not the raw Windows virtual-key number.
- `KEY_OTHER` is returned only by `GET KEY`.
- `KEY_HELD(KEY_OTHER)` must return `FALSE`.
- Existing key values must not change.
- Key auto-repeat remains suppressed exactly as it is now.
- Alt+Enter remains a runtime-reserved full-screen shortcut and is not returned to the game.
- Alt+F4 and window Close retain normal Windows behavior.
- System key combinations that Windows reserves do not have to become `KEY_OTHER`.

Examples:

```smile
GET KEY Key

IF Key <> KEY_NONE THEN
    CALL StartGame()
END IF
```

```smile
IF Key = KEY_OTHER THEN
    PRINT "A non-named key was pressed."
END IF
```

### Required language/runtime changes

#### `src\Smile.Language\Syntax.cs`

- Add `KeyOtherKeyword` inside the built-in-constant range.
- Add `"KEY_OTHER"` to the keyword dictionary.
- Return numeric value `19` from `GetBuiltInConstantValue`.

#### `src\Smile.NativeRuntime\runtime.c`

Add:

```c
#define SMILE_KEY_OTHER 19
```

Change key mapping so an otherwise unrecognized valid key-down becomes `SMILE_KEY_OTHER` instead of `SMILE_KEY_NONE`.

Preserve:

- `SMILE_KEY_NONE` for “there is no queued key event.”
- existing values for every named key;
- no-repeat queue behavior;
- held-state behavior;
- focus-loss clearing;
- Alt+Enter handling.

`smile_key_virtual(SMILE_KEY_OTHER)` must resolve to zero so `KEY_HELD(KEY_OTHER)` remains false.

The graphical window path is the primary requirement. Matching console behavior is acceptable and preferred if it follows naturally from the shared mapper.

---

# 5. Required generic graphics backend additions

Add the following C ABI functions:

```c
void smile_fill_quadrilateral(
    long long x1, long long y1,
    long long x2, long long y2,
    long long x3, long long y3,
    long long x4, long long y4,
    long long color);

void smile_draw_quadrilateral(
    long long x1, long long y1,
    long long x2, long long y2,
    long long x3, long long y3,
    long long x4, long long y4,
    long long color);
```

Update these layers consistently:

```text
src\Smile.NativeRuntime\runtime.c
src\Smile.NativeRuntime\graphics\graphics_backend.h
src\Smile.NativeRuntime\graphics\graphics_common.h
src\Smile.NativeRuntime\graphics\graphics_common.c
src\Smile.NativeRuntime\graphics\graphics_directx.cpp
src\Smile.NativeRuntime\graphics\graphics_gdi.c
src\Smile.NativeGraphicsTests\NativeGraphicsTests.c
```

Do not change existing compiler-facing export names or behavior.

## 5.1 Backend vtable

Add `fill_quadrilateral` and `draw_quadrilateral` function pointers to `SmileGraphicsBackendVTable`.

Update every real and mock vtable in the exact same order.

The common router must:

- ensure a frame exists;
- call the selected backend;
- remain backend-neutral;
- preserve current frame invalidation behavior.

## 5.2 DirectX/Direct2D implementation

Use Direct2D geometry. Keep this generic and small.

Recommended first implementation:

1. Map all four logical points through the existing viewport mapping.
2. Create an `ID2D1PathGeometry`.
3. Open its geometry sink.
4. Begin the figure at point 1.
5. Add lines to points 2, 3, and 4.
6. End with `D2D1_FIGURE_END_CLOSED`.
7. Close the sink.
8. Use the existing cached solid brush.
9. Call `FillGeometry` or `DrawGeometry`.
10. Use the existing scaled stroke-width helper for the outline.
11. Release the sink and geometry on every success and failure path.
12. Report creation/open/close failures through the backend’s existing error mechanism.
13. Do not leak a COM object.

Start with the smallest correct implementation. Measure it with the existing diagnostics while Dungeon Star I is running.

Only if measured geometry creation materially prevents meeting the frame budget, add a small bounded geometry cache patterned after the existing brush/text caches. Do not add speculative unbounded caching. The dungeon animations should use a small number of quantized visual frames, which makes a bounded cache effective if one becomes necessary.

## 5.3 GDI implementation

Use the existing physical-output back buffer and viewport mapping.

Recommended implementation:

- Map four logical points to a local `POINT points[4]`.
- For fill:
  - select the cached color brush;
  - select `NULL_PEN`;
  - call `Polygon`.
- For outline:
  - select the cached scaled pen;
  - select `NULL_BRUSH`;
  - call `Polygon`, which closes the final edge.
- Restore every previously selected object.
- Do not create a brush or pen per frame outside the existing bounded caches.
- Do not leak GDI objects.

Both backends must accept offscreen points safely and display the same point order.

---

# 6. Game project structure

Create:

```text
games\DungeonStarI\
    DungeonStarI.smileproj
    Program.smile
    README.md
```

Do not add image, music, or sound assets for this milestone.

Use this project shape:

```xml
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Game</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>DungeonStarI</OutputName>
    <GraphicsBackend>Auto</GraphicsBackend>
    <VSync>true</VSync>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" />
  </ItemGroup>
</SmileProject>
```

The program must open:

```smile
GAME WINDOW "Dungeon Star I"
```

Use the default logical canvas:

```text
960 x 540
```

Do not add this game as a C#/C++ project to `SMILE 2.0.sln`. The existing games are SMILE projects compiled by the smoke pipeline, and Dungeon Star I should follow that pattern.

---

# 7. Game source organization

`Program.smile` will be large because SMILE currently supports one startup source file. Keep it readable using strong section comments and small routines.

Suggested source order:

```text
1. Constants
2. Global fixed arrays
3. Global game state
4. Coordinate and map helpers
5. Dungeon generation
6. Dungeon validation
7. Palette selection
8. Projection helpers
9. Wall/door/stair drawing
10. Complete dungeon-view renderer
11. Player action state machine
12. Demo route planner and controller
13. Title screen
14. User idle warning
15. State transition routines
16. GAME WINDOW
17. Main loop
```

Important current SMILE scoping rule:

- A top-level variable is global.
- A name first assigned inside a routine becomes local unless that global name already exists.

Therefore, initialize every shared scalar at top level before routine declarations. Do not accidentally create routine-local copies of player position, timing, animation, palette, or renderer state.

Keep arrays global.

Do not increase the four-scalar-parameter routine limit. Use:

- small helper routines with four or fewer parameters;
- global renderer scratch values;
- fixed arrays;
- functions with concise argument sets.

---

# 8. Core state model

Use two separate state dimensions.

## 8.1 Screen state

Recommended constants:

```smile
CONST STATE_TITLE = 0
CONST STATE_USER_DUNGEON = 1
CONST STATE_DEMO_DUNGEON = 2
```

## 8.2 Action/animation state

Recommended constants:

```smile
CONST ACTION_IDLE = 0
CONST ACTION_MOVE_FORWARD = 1
CONST ACTION_MOVE_BACKWARD = 2
CONST ACTION_TURN_LEFT = 3
CONST ACTION_TURN_RIGHT = 4
CONST ACTION_OPEN_DOOR = 5
CONST ACTION_STAIRS_OUT = 6
CONST ACTION_STAIRS_IN = 7
```

Do not encode title/demo/user behavior inside the renderer. The renderer should draw the view it is given. State transitions belong in SMILE game logic.

---

# 9. Controls and user behavior

Required controls:

| Key | User action |
|---|---|
| Up or W | Move forward one grid cell |
| Down or S | Move backward one grid cell |
| Left or A | Turn left 90 degrees |
| Right or D | Turn right 90 degrees |
| Enter or Space | Open/use a closed door directly ahead without immediately stepping, when applicable |
| Escape during user play | Return to the title screen |
| Escape on title | Exit the application |
| Alt+Enter | Existing runtime full-screen toggle |
| Any other key during user play | Counts as activity but performs no dungeon action |

Forward movement into a closed door may automatically open it and then continue through after the door animation. Enter/Space provides an explicit alternative.

Stairs are used by moving onto the stair tile. No separate menu or confirmation is required.

Movement is grid-based:

- The player occupies one integer map cell.
- Facing is exactly North, East, South, or West.
- No diagonal movement.
- No unrestricted camera angle.
- Only one primary action animation runs at a time.
- At most one movement/turn input may be buffered while an action is active.
- Escape and demo cancellation must remain immediately responsive even during an animation.

Recommended direction values:

```smile
CONST NORTH = 0
CONST EAST = 1
CONST SOUTH = 2
CONST WEST = 3
```

---

# 10. Exact title and arcade attract-mode behavior

Use these exact timing constants:

```smile
CONST TitleDemoDelay = 15000
CONST TitleDemoCountdownStart = 5
CONST TitleDemoStartTime = 21000
CONST DemoDuration = 60000
CONST FlashInterval = 500
CONST TitleInputArmDelay = 250
```

`TitleDemoStartTime` is 21 seconds because the user requested six visible countdown values: `5`, `4`, `3`, `2`, `1`, and `0`, with each visible for one full second after the initial 15-second delay.

## 10.1 Entering the title

Create one routine such as:

```smile
SUB EnterTitle()
```

It must:

- set `ScreenState = STATE_TITLE`;
- cancel any dungeon action and pending action;
- clear demo routing state;
- reset the title timer;
- reset the title countdown;
- drain pending key events;
- set a short `TitleAcceptInputAt` arm time so the key that canceled a demo cannot immediately start a user run;
- leave normal window Close and Alt+Enter behavior intact.

A helper may drain the current queue:

```smile
SUB DrainPendingKeys()
    DO
        GET KEY DrainKey
    LOOP UNTIL DrainKey = KEY_NONE
END SUB
```

Use the short arm delay as additional protection against a still-held or closely repeated key.

## 10.2 Title display

The title screen must show:

```text
DUNGEON STAR I
PRESS ANY KEY TO START
```

`PRESS ANY KEY TO START` flashes with a 500-millisecond on/off interval.

Before 15 seconds, show no demo countdown.

After 15 seconds, retain the flashing start message and also show:

```text
DEMO STARTS IN
```

plus a separately drawn number.

Because SMILE text is literal-oriented, use `DRAW TEXT` plus `DRAW NUMBER`; do not add mutable strings or string interpolation for this game.

Exact title timing:

| Time since entering title | Display/action |
|---:|---|
| 0–14,999 ms | Flash `PRESS ANY KEY TO START`; no countdown |
| 15,000–15,999 ms | Show demo countdown `5` |
| 16,000–16,999 ms | Show `4` |
| 17,000–17,999 ms | Show `3` |
| 18,000–18,999 ms | Show `2` |
| 19,000–19,999 ms | Show `1` |
| 20,000–20,999 ms | Show `0` |
| 21,000 ms and later | Start demo mode immediately |

Formula:

```text
Countdown = 5 - ((TitleElapsed - 15000) / 1000)
```

When it becomes less than zero, start the demo.

## 10.3 Title input

After `TitleAcceptInputAt`:

- `KEY_ESCAPE` exits the program.
- Any other value where `Key <> KEY_NONE` starts a new user dungeon.
- This includes `KEY_OTHER`.

Process title input before checking whether the demo timer has expired. A real key press at the exact transition moment must start user play instead of launching the demo.

## 10.4 Starting a demo

Starting demo mode must:

1. Generate and validate a new three-floor dungeon.
2. Place the demo player at the normal floor-one entrance.
3. Reset demo visit/path arrays.
4. Set `ScreenState = STATE_DEMO_DUNGEON`.
5. Set `DemoStartedAt = TIMER()` **after** generation is complete.
6. Start the first automatic action through the same action routines used by a human player.

While demo mode is active, draw a small original label such as:

```text
DEMO
PRESS ANY KEY TO RETURN
```

Do not obscure the dungeon view.

## 10.5 Demo duration

The demo runs for exactly 60 seconds measured from `DemoStartedAt`.

At:

```text
DemoElapsed >= 60000
```

return immediately to the title screen and restart the normal 15-second idle period.

The title returns with `PRESS ANY KEY TO START` flashing.

## 10.6 Canceling the demo

At any time during demo mode:

```smile
IF Key <> KEY_NONE THEN
    CALL EnterTitle()
END IF
```

The pressed key is consumed only as a demo-cancel action.

It must **not** also start a user dungeon in the same frame.

This includes Escape. Escape during the demo returns to title; Escape exits only after the user is actually on the title screen and presses Escape again.

Alt+Enter remains the reserved full-screen exception because the runtime handles it outside the game key queue.

---

# 11. Exact user-idle behavior

Use these constants:

```smile
CONST UserIdleWarningTime = 30000
CONST UserIdleExitTime = 40000
CONST UserIdleCountdownStart = 9
```

When user play begins:

```text
LastUserActivityAt = TIMER()
```

Reset `LastUserActivityAt` whenever:

- `GET KEY` returns any value other than `KEY_NONE`, even when the key does not map to a dungeon action;
- a supported movement/turn key is currently held;
- a buffered movement/turn action is accepted.

Process fresh user input before evaluating the idle deadline. A key arriving at the deadline cancels the pending idle exit.

## 11.1 No warning before 30 seconds

For:

```text
IdleElapsed < 30000
```

draw no idle warning.

## 11.2 Warning countdown

At 30 seconds, flash:

```text
WILL EXIT IN
9
SECONDS
```

Use `DRAW TEXT` and `DRAW NUMBER`.

The complete warning overlay flashes with the same 500-millisecond interval as the title prompt.

Exact timing:

| User idle duration | Display |
|---:|---|
| 30,000–30,999 ms | `9` |
| 31,000–31,999 ms | `8` |
| 32,000–32,999 ms | `7` |
| 33,000–33,999 ms | `6` |
| 34,000–34,999 ms | `5` |
| 35,000–35,999 ms | `4` |
| 36,000–36,999 ms | `3` |
| 37,000–37,999 ms | `2` |
| 38,000–38,999 ms | `1` |
| 39,000–39,999 ms | `0` |
| 40,000 ms and later | Return to title |

Formula:

```text
Countdown = 9 - ((IdleElapsed - 30000) / 1000)
```

## 11.3 Canceling the warning

Any key:

- resets `LastUserActivityAt`;
- removes the warning immediately;
- then performs its normal user-mode behavior.

Examples:

- A movement key cancels the warning and moves or turns.
- An unrelated `KEY_OTHER` cancels the warning but takes no dungeon action.
- Escape cancels user play and returns to title.

The demo player is never subject to the user idle timer.

---

# 12. Dungeon data model

Use a flattened map because SMILE currently supports at most two array dimensions and the game has three floors.

Recommended constants:

```smile
CONST MapWidth = 31
CONST MapHeight = 31
CONST FloorCount = 3
CONST CellsPerFloor = MapWidth * MapHeight
CONST TotalCells = CellsPerFloor * FloorCount
CONST MaximumRooms = 9
CONST MinimumRooms = 5
```

Recommended tile values:

```smile
CONST TILE_WALL = 0
CONST TILE_FLOOR = 1
CONST TILE_DOOR_CLOSED = 2
CONST TILE_DOOR_OPEN = 3
CONST TILE_STAIRS_UP = 4
CONST TILE_STAIRS_DOWN = 5
```

Recommended arrays:

```smile
DIM Dungeon[TotalCells]

DIM RoomLeft[MaximumRooms]
DIM RoomTop[MaximumRooms]
DIM RoomWidth[MaximumRooms]
DIM RoomHeight[MaximumRooms]
DIM RoomCenterX[MaximumRooms]
DIM RoomCenterY[MaximumRooms]

DIM StairUpX[FloorCount]
DIM StairUpY[FloorCount]
DIM StairDownX[FloorCount]
DIM StairDownY[FloorCount]

DIM FloodQueue[CellsPerFloor]
DIM FloodVisited[CellsPerFloor]

DIM DemoQueue[CellsPerFloor]
DIM DemoParent[CellsPerFloor]
DIM DemoRoute[CellsPerFloor]
DIM DemoVisits[TotalCells]
```

A map helper should flatten:

```text
Index = Floor * CellsPerFloor + Y * MapWidth + X
```

Required safety rules:

- All map access goes through bounds-aware helpers.
- Out-of-range coordinates behave as walls.
- The outer border of every floor remains solid wall.
- Closed doors are traversable for validation/pathfinding because the player can open them, but they block normal line of sight until opened.
- Stairs are walkable.

---

# 13. Random dungeon generation

Use a simple room-and-corridor generator. Do not build a general procedural-generation framework.

Generate all three floors at the beginning of a user run or demo run.

## 13.1 Floor generation

For each floor:

1. Fill every cell with `TILE_WALL`.
2. Attempt to place between five and nine non-overlapping rectangular rooms.
3. Keep at least one wall cell of separation between rooms.
4. Use random room widths and heights in a small range such as 5–9 cells.
5. Keep rooms inside the permanent outer wall.
6. Carve accepted room rectangles as `TILE_FLOOR`.
7. Connect each accepted room to the previous accepted room with an L-shaped corridor.
8. Randomly choose horizontal-first or vertical-first for each connection.
9. Add two to four extra connections between randomly selected rooms to create loops.
10. Place closed doors at selected room/corridor thresholds.
11. Place required stairs.
12. Validate the floor.
13. If invalid, retry with a bounded attempt count.
14. If all retries fail, create a small deterministic fallback floor rather than hanging.

Do not use recursion. Use loops and fixed arrays.

## 13.2 Connectivity by construction

Room centers connected in a chain guarantee a basic connected floor.

Extra connections add loops and reduce repetitive dead ends.

Do not rely only on luck. The generator must deliberately guarantee that every accepted room is connected.

## 13.3 Doors

Doors are ordinary map tiles for this first version.

Requirements:

- Place doors at room/corridor boundary cells, not in the middle of a room.
- A closed door must have two walkable cells on opposite sides.
- The orthogonal sides should normally be walls so the door reads visually as a doorway.
- Do not place a door on a stair or player start.
- Place at least three closed doors per floor when the generated layout permits.
- Doors are never locked.
- Opening a door changes `TILE_DOOR_CLOSED` to `TILE_DOOR_OPEN` for the rest of that run.
- Open doors retain a visible frame but do not block movement or sight.

## 13.4 Stairs and floor links

Use three floors:

| Floor | Theme | Required stairs |
|---:|---|---|
| 1 | Green/emerald | Down |
| 2 | Blue/sapphire | Up and Down |
| 3 | Red/crimson | Up |

Recommended placement:

- Floor 1 player start: first room.
- Floor 1 down stairs: a distant later room.
- Floor 2 up stairs: first room.
- Floor 2 down stairs: a distant later room.
- Floor 3 up stairs: first room.

All stair pairs must be reciprocal:

```text
Floor 1 Down <-> Floor 2 Up
Floor 2 Down <-> Floor 3 Up
```

When a player arrives on a destination stair tile, suppress immediate automatic retriggering. The stair transition occurs when the player moves **onto** a stair, not merely because the player is standing on one after a floor change.

The player must be able to:

- descend from floor 1 to floor 2;
- descend from floor 2 to floor 3;
- return from floor 3 to floor 2;
- return from floor 2 to floor 1.

## 13.5 Runtime validation

Every generated run must validate itself before play begins.

Flood-fill or breadth-first validation must verify:

- the starting cell is walkable;
- every room center is reachable;
- every stair on the floor is reachable;
- doors connect valid spaces;
- no stair is in a wall or door;
- the outer border remains closed;
- reciprocal stair links are valid;
- the player never starts trapped.

Treat closed doors as passable during connectivity validation.

Use bounded generation attempts. Never allow an infinite generation loop.

---

# 14. Pseudo-3D projection model

This is not a real 3D engine.

Render a small player-relative grid using perspective quadrilaterals.

Use:

```text
Canvas center X: 480
Horizon Y:       270
Visible depth:   approximately 6 cell boundaries
Visible sides:   approximately 2 cells left and right
```

A practical starting projection table is:

| Boundary depth | Half corridor width | Top Y | Bottom Y |
|---:|---:|---:|---:|
| 0 | 720 | -120 | 660 |
| 1 | 360 | 45 | 495 |
| 2 | 225 | 125 | 415 |
| 3 | 140 | 180 | 360 |
| 4 | 86 | 218 | 322 |
| 5 | 50 | 242 | 298 |
| 6 | 28 | 256 | 284 |

Store these in fixed arrays and tune them by eye.

For local side cell `S` and boundary depth `D`:

```text
LeftX  = 480 + (2 * S - 1) * HalfWidth[D]
RightX = 480 + (2 * S + 1) * HalfWidth[D]
TopY   = Top[D]
BottomY = Bottom[D]
```

This makes:

- side `0` the corridor directly ahead;
- side `-1` one cell left;
- side `1` one cell right.

The current-cell near plane may be offscreen. That is intentional.

## 14.1 Local-to-world mapping

Use player-relative `(Side, Forward)` coordinates.

For North:

```text
WorldX = PlayerX + Side
WorldY = PlayerY - Forward
```

For East:

```text
WorldX = PlayerX + Forward
WorldY = PlayerY + Side
```

For South:

```text
WorldX = PlayerX - Side
WorldY = PlayerY + Forward
```

For West:

```text
WorldX = PlayerX - Forward
WorldY = PlayerY - Side
```

Keep this logic in a small set of helpers.

## 14.2 Draw order

Use painter’s order:

1. Clear the canvas.
2. Draw far visible floor/ceiling surfaces.
3. Draw far wall faces.
4. Progress toward the player.
5. Draw near wall faces last.
6. Draw doors and stairs at their correct depth.
7. Draw small HUD/demo/idle overlays last.

The renderer must stop or occlude geometry behind a closed front wall or closed door.

## 14.3 Required scene cases

The view must correctly and distinctly show:

- straight corridor;
- dead end;
- left turn;
- right turn;
- T-junction;
- four-way intersection;
- narrow doorway;
- closed door;
- open door;
- small rectangular room;
- larger rectangular room;
- stairs up;
- stairs down.

A side opening must not look like an unexplained black hole. Draw the visible side floor, ceiling, return wall, and far wall within the limited side range.

## 14.4 Geometry per open cell

For each visible open cell section between boundaries `D` and `D + 1`, quadrilaterals can represent:

- ceiling;
- floor;
- left wall when the left neighbor is blocked;
- right wall when the right neighbor is blocked;
- front wall when the forward neighbor is blocked;
- lateral room surfaces when side cells are open.

All renderer decisions remain in `Program.smile`.

---

# 15. Original wall pattern and floor palettes

Do not use textures.

Create the dungeon look with:

- solid wall quadrilaterals;
- depth-based shading;
- perspective mortar lines;
- alternating vertical joints;
- door frames;
- floor and ceiling bands.

For each wall quadrilateral, compute horizontal line endpoints by interpolating along its two side edges. This keeps mortar lines inside the wall without requiring polygon clipping.

Use fewer pattern lines at greater depth.

## 15.1 Floor themes

Use distinct original palettes. These are starting suggestions, not mandatory exact values.

### Floor 1 — green/emerald

```text
Near wall:  RGB(20, 110, 72)
Mid wall:   RGB(16, 82, 57)
Far wall:   RGB(11, 50, 39)
Mortar:     RGB(55, 190, 125)
Floor:      RGB(7, 28, 21)
Ceiling:    RGB(5, 20, 15)
Door:       RGB(22, 75, 52)
Highlight:  RGB(100, 225, 160)
```

### Floor 2 — blue/sapphire

```text
Near wall:  RGB(28, 80, 150)
Mid wall:   RGB(20, 58, 112)
Far wall:   RGB(12, 34, 70)
Mortar:     RGB(90, 155, 225)
Floor:      RGB(7, 18, 38)
Ceiling:    RGB(5, 12, 28)
Door:       RGB(25, 55, 100)
Highlight:  RGB(135, 190, 255)
```

### Floor 3 — red/crimson

```text
Near wall:  RGB(145, 48, 45)
Mid wall:   RGB(102, 32, 35)
Far wall:   RGB(62, 20, 25)
Mortar:     RGB(230, 105, 85)
Floor:      RGB(35, 8, 12)
Ceiling:    RGB(24, 5, 8)
Door:       RGB(95, 32, 28)
Highlight:  RGB(255, 145, 120)
```

The geometry remains the same; the palette changes by current floor.

---

# 16. Smooth movement and animation

Use `TIMER()` and integer progress.

Do not use floating point.

Do not use a fixed amount of movement per rendered frame.

Do not put `WAIT 16 MILLISECONDS` in the Dungeon Star I main loop. Let the current VSync/presentation layer pace rendering. Animation state must be time-based.

Recommended durations:

```smile
CONST MoveDuration = 260
CONST TurnDuration = 240
CONST DoorDuration = 320
CONST StairOutDuration = 450
CONST StairBlackDuration = 120
CONST StairInDuration = 450
```

Use progress in the range `0` through `1000`:

```text
Progress = MIN(1000, (Now - ActionStartedAt) * 1000 / Duration)
```

Use integer interpolation:

```text
Value = Start + (End - Start) * Progress / 1000
```

Quantizing visual animation to approximately 8–12 distinct geometry frames is acceptable and may improve the Direct2D geometry-cache hit rate while retaining a smooth retro look.

## 16.1 Forward movement

Before starting:

- find target cell;
- reject a wall;
- if closed door, begin door action;
- otherwise begin forward action.

During animation:

- interpolate every depth boundary toward the next-nearer boundary;
- the nearest geometry expands beyond the screen;
- retain the old logical player coordinates until animation completes.

At completion:

- commit the new player coordinates;
- restore the base projection table;
- if the entered tile is stairs, begin stair transition;
- otherwise return to `ACTION_IDLE`.

## 16.2 Backward movement

Use the reverse depth interpolation.

Apply the same collision and door rules.

## 16.3 Turning

Use a two-view integer compression/slide effect rather than trigonometry.

For a turn with progress `P`:

```text
OldScale = 1000 - P
NewScale = P
```

Transform each projected X around center:

```text
TransformedX = 480 + (X - 480) * Scale / 1000 + Offset
```

For a left turn:

```text
OldOffset =  P * 480 / 1000
NewOffset = -(1000 - P) * 480 / 1000
```

For a right turn:

```text
OldOffset = -P * 480 / 1000
NewOffset =  (1000 - P) * 480 / 1000
```

Render the old and new facing views in an order that changes near the midpoint:

- before 50%, old view is visually dominant;
- after 50%, new view is visually dominant.

At completion, commit the new cardinal direction.

Tune the effect against the user’s motion reference, but use original geometry and timing.

## 16.4 Door animation

Draw a closed door as an inset panel and frame at the correct perspective plane.

Recommended opening effect:

- split the panel into left and right halves;
- move or shrink the halves outward over `DoorDuration`;
- retain the frame;
- at completion, change the tile to `TILE_DOOR_OPEN`;
- if the action began because of a movement attempt, begin the pending move through the doorway.

The demo controller must use this same door action.

## 16.5 Stair transition

When movement completes onto a stair:

1. begin `ACTION_STAIRS_OUT`;
2. darken or close the view using original geometric animation;
3. switch floor and destination coordinates at the black/midpoint;
4. select the destination floor palette;
5. begin `ACTION_STAIRS_IN`;
6. return to idle.

Do not add alpha blending solely for this. Use:

- precomputed darkened palette steps;
- a closing/opening geometric iris;
- or a short black frame between palette transitions.

Suppress immediate stair retriggering until the player leaves the destination stair tile.

---

# 17. Demo self-player

The 60-second demo must genuinely play the dungeon.

It must not:

- teleport;
- directly rewrite player coordinates to fake movement;
- skip door animation;
- skip stair transitions;
- use a private native helper;
- use a different renderer.

It must call the same SMILE action routines used by user input.

## 17.1 Route planning

Use a small breadth-first search on the current floor.

Treat:

- floor;
- open doors;
- closed doors;
- stairs

as traversable for route planning.

Recommended demo plan:

1. On floor 1, target the down stairs.
2. On floor 2, target the down stairs.
3. On floor 3, target the up stairs.
4. Continue alternating up/down goals for the remaining demo time.
5. If no route is found unexpectedly, fall back to a least-visited random walk.

Build a route as cardinal steps.

For each route step:

- turn until facing the desired direction;
- move one cell;
- allow automatic door opening;
- allow normal stair transition;
- wait until the current action is idle before choosing the next action.

Recalculate the route:

- after a stair transition;
- when a closed/opened door changes the path;
- when the next planned tile is no longer usable.

## 17.2 Natural-looking behavior

The self-player should:

- pause briefly between completed actions, approximately 80–160 milliseconds;
- avoid repeatedly turning in place;
- avoid immediate reversals unless necessary;
- keep moving;
- show doors and stairs naturally;
- never remain stuck for more than two seconds.

A small `DemoVisits` array may be used for fallback tie-breaking.

The hard 60-second demo deadline overrides the route. At the deadline, return to title immediately.

---

# 18. Title-screen visual design

Create an original title screen using runtime geometry only.

Suggested elements:

- black or very dark background;
- small generated starfield;
- a perspective tunnel/doorway motif built with quadrilaterals and lines;
- `DUNGEON STAR I` centered;
- floor-theme colors cycling slowly through green, blue, and red;
- controls in smaller text;
- flashing `PRESS ANY KEY TO START`;
- demo countdown after 15 seconds.

Suggested text:

```text
DUNGEON STAR I

ARROWS OR W A S D
MOVE / BACK / TURN

ENTER OR SPACE - OPEN DOOR
ESCAPE - EXIT
ALT+ENTER - FULL SCREEN

PRESS ANY KEY TO START
```

Do not use or imitate a commercial logo.

---

# 19. In-dungeon HUD

Keep the game focused on exploration.

Allowed minimal overlay:

- `FLOOR` plus floor number;
- one-letter compass direction (`N`, `E`, `S`, `W`);
- `DEMO` label while the self-player is active;
- demo return instruction;
- idle-exit warning.

Do not add a minimap, party panel, statistics panel, score, inventory, or quest text.

The dungeon view should remain the dominant visual element.

---

# 20. Main-loop order

Use one continuous native game loop.

Recommended order:

```text
1. Now = TIMER()
2. GET KEY Key
3. Check GAME_CLOSED()
4. Process screen-state input
5. Apply user-activity reset before idle checks
6. Process exact title/demo/user deadlines
7. Update the active animation
8. When idle, start user-buffered or demo-planned action
9. Draw title or dungeon
10. Draw overlays
11. SHOW SCREEN
```

Pseudo-structure:

```smile
GAME WINDOW "Dungeon Star I"

CALL EnterTitle()

DO
    Now = TIMER()
    GET KEY Key

    IF ScreenState = STATE_TITLE THEN
        CALL UpdateTitle()
        CALL DrawTitle()
    ELSE IF ScreenState = STATE_DEMO_DUNGEON THEN
        CALL UpdateDemo()
        IF ScreenState = STATE_TITLE THEN
            CALL DrawTitle()
        ELSE
            CALL DrawDungeonScene()
        END IF
    ELSE
        CALL UpdateUserDungeon()
        IF ScreenState = STATE_TITLE THEN
            CALL DrawTitle()
        ELSE
            CALL DrawDungeonScene()
            CALL DrawIdleWarning()
        END IF
    END IF

    SHOW SCREEN
LOOP UNTIL GAME_CLOSED() = TRUE

END PROGRAM
```

Do not duplicate the complete renderer between user and demo modes.

---

# 21. Documentation changes

Update current public documentation where it describes the current product:

## `AGENTS.md`

Change the “games prove the language” list so Dungeon Star I is also required to remain in `.smile` source.

Do not weaken the generic-runtime restriction.

## `README.md`

Update:

- “four complete games” to “five complete games”;
- included game list;
- runnable artifact list;
- smoke-suite description;
- language graphics list to include quadrilaterals;
- input list to include `KEY_OTHER`;
- a short Dungeon Star I description.

Suggested description:

```text
games\DungeonStarI — an original three-floor pseudo-3D dungeon
exploration sample with random rooms, doors, stairs, attract mode,
and green/blue/red floor palettes.
```

## `docs\language\README.md`

Add:

- `FILL QUADRILATERAL`;
- `DRAW QUADRILATERAL`;
- `KEY_OTHER`;
- Dungeon Star I to the executable examples.

## `docs\architecture\README.md`

Add quadrilateral routing to the generic backend description and make it explicit that dungeon rules and projection remain in `.smile`.

## New game README

`games\DungeonStarI\README.md` must document:

- purpose;
- controls;
- three palettes/floors;
- random-generation behavior;
- title/demo timing;
- 60-second demo;
- user idle warning;
- no combat/items;
- DirectX/GDI compatibility;
- original asset-free geometry.

Do not rewrite historical milestone reports merely to change “four games” to “five games.” Update current documentation, not historical records of earlier milestones.

---

# 22. Automated tests and smoke integration

## 22.1 Shared-language tests

Extend `src\Smile.Tests\Program.cs` using `SmileLanguage.Analyze`.

Add at least these checks:

1. Valid filled quadrilateral analyzes without errors.
2. Valid outlined quadrilateral analyzes without errors.
3. The syntax operation is `FillQuadrilateral` or `DrawQuadrilateral` as expected.
4. Too few quadrilateral arguments report a parser error.
5. Too many quadrilateral arguments report a parser error rather than silently ignoring values.
6. A non-number quadrilateral argument reports a semantic error.
7. `KEY_OTHER` analyzes as a built-in number constant with value `19`.
8. Existing key constants retain their values.
9. Existing graphics statements remain valid.

Update the printed test count accurately.

## 22.2 Native graphics tests

Update the mock vtable in:

```text
src\Smile.NativeGraphicsTests\NativeGraphicsTests.c
```

Add mock quadrilateral functions and counters.

Verify:

- common-router fill dispatch reaches the active backend;
- common-router outline dispatch reaches the active backend;
- beginning a quad draw starts a frame exactly as existing primitives do;
- Auto/DirectX/GDI selection tests still pass;
- vtable layout is complete in every backend.

Update the final check count accurately.

## 22.3 Graphics example

Extend:

```text
examples\GraphicsBasics.smile
```

to draw:

- one filled quadrilateral;
- one outlined quadrilateral;
- a small message indicating that an unnamed key produces `KEY_OTHER`, if practical.

Keep the example simple and visually inspectable.

## 22.4 Invalid graphics diagnostic

Add a dedicated invalid sample or extend the existing invalid-game sample so malformed quadrilateral syntax is exercised.

The smoke suite must verify the expected parser/semantic diagnostic. Do not rely only on a successful valid compile.

## 22.5 Game smoke build

Update:

```text
scripts\smoke-test.cmd
```

Add:

```text
artifacts\games\DungeonStarI\DungeonStarI.exe
```

Compile:

```text
games\DungeonStarI\Program.smile
```

No asset copy is needed.

Print a clear success message.

## 22.6 Artifact verification

Update:

```text
scripts\verify-artifacts.ps1
```

Add Dungeon Star I to the native GUI executable list.

Verify it remains:

- x64;
- PE32+;
- Windows GUI subsystem;
- no CLR header.

Do not add an empty asset requirement.

## 22.7 Complete automated regression

The final implementation must pass:

```text
cmd /c scripts\smoke-test.cmd
```

This includes:

- solution build;
- shared tests;
- native graphics tests;
- language/diagnostic regressions;
- graphics examples;
- all five game builds;
- native artifact verification;
- VSIX verification.

---

# 23. Required manual validation

Automated compilation is not enough for this milestone.

Build and run separate DirectX and GDI executables.

Suggested commands from the repository root:

```text
cmd /c artifacts\compiler\smilec.exe games\DungeonStarI\Program.smile -o artifacts\games\DungeonStarI-DirectX\DungeonStarI.exe --graphics directx --vsync true
```

```text
cmd /c artifacts\compiler\smilec.exe games\DungeonStarI\Program.smile -o artifacts\games\DungeonStarI-GDI\DungeonStarI.exe --graphics gdi --vsync true
```

Perform and record all checks below.

## 23.1 Title and attract mode

- Title appears correctly.
- `PRESS ANY KEY TO START` flashes.
- No countdown appears for the first 15 seconds.
- `5`, `4`, `3`, `2`, `1`, and `0` each display for approximately one second.
- Demo begins after `0` has been visible.
- Pressing a named key on title starts user play.
- Pressing an otherwise unnamed key such as `Q`, `F`, `3`, Shift, or F2 starts user play through `KEY_OTHER`.
- Escape on title exits.
- Alt+Enter still toggles full screen and does not start/cancel the game.

## 23.2 Demo

- Demo creates a valid dungeon.
- Demo visibly moves, turns, opens doors, and uses stairs when reached.
- Demo uses the normal animation path.
- Demo never teleports.
- Demo never remains stuck longer than two seconds.
- Demo returns to title after 60 seconds.
- Returned title flashes `PRESS ANY KEY TO START`.
- Any ordinary key during demo returns to title.
- The canceling key does not immediately start user play.
- Escape during demo returns to title rather than exiting directly.

## 23.3 User idle

- No warning before 30 seconds.
- At 30 seconds the flashing warning starts at `9`.
- Countdown reaches `0`.
- At 40 seconds total idle time, the game returns to title.
- A movement key during warning cancels it and moves/turns normally.
- An unrelated `KEY_OTHER` cancels it without moving.
- Escape during user play returns to title.
- Holding a supported movement key counts as activity.

## 23.4 Dungeon generation

Repeat new user starts enough times to inspect varied layouts.

Confirm:

- rooms vary;
- corridors vary;
- doors vary;
- all three floors are reachable;
- stairs work in both directions;
- no spawn is inside a wall or door;
- no stair is unreachable;
- no generated run hangs;
- no room is isolated;
- no invalid border opening appears;
- deterministic fallback works if generation retries are deliberately forced during local testing.

The committed game must run its own validator before accepting every generated map.

## 23.5 Rendering and animation

Confirm:

- straight corridors;
- dead ends;
- corners;
- T-junctions;
- intersections;
- rooms;
- closed/open doors;
- stairs up/down

all read clearly.

Confirm:

- forward animation is smooth;
- backward animation is smooth;
- left/right turns look intentionally pseudo-3D;
- door animation is smooth;
- stair palette transition is clear;
- green, blue, and red floors are visually distinct;
- walls do not leave cracks or unexplained black gaps;
- text remains readable;
- 960×540 logical layout scales correctly;
- window resize works;
- multiple Alt+Enter cycles work;
- minimization/restoration works;
- moving the window between monitors works;
- DirectX and GDI give equivalent geometry.

## 23.6 Stability/performance

Run with graphics diagnostics enabled.

Confirm:

- the expected backend is selected;
- no unexpected fallback;
- no DirectX device-removal reason;
- stable frame pacing;
- no escalating GDI object count;
- no escalating memory use;
- no COM/resource leak during repeated door, movement, turn, stair, title, and demo cycles.

Run at least:

- 10 minutes of mixed user/demo operation;
- 20 title-to-demo-to-title cycles;
- 50 Alt+Enter toggles;
- repeated window resizes.

If Direct2D path-geometry creation materially misses the target frame budget, add only the smallest bounded cache justified by the measurements.

---

# 24. Implementation sequence and commits

Use coherent commits. Each commit must have the detailed public body required by `AGENTS.md`.

## Commit 1 — generic language/runtime capability

Suggested subject:

```text
feat(graphics): add quadrilateral drawing and generic key events
```

Include:

- `QUADRILATERAL` keyword and syntax;
- fill/draw graphics operations;
- parser and semantic behavior;
- emitter exports/calls;
- C ABI and backend vtable;
- Direct2D implementation;
- GDI implementation;
- `KEY_OTHER`;
- language/native tests;
- GraphicsBasics update;
- language/architecture documentation.

Build, test, commit, and push before starting the game commit.

## Commit 2 — Dungeon Star I game

Suggested subject:

```text
feat(game): add Dungeon Star I pseudo-3D dungeon sample
```

Include:

- `.smileproj`;
- complete `Program.smile`;
- title screen;
- exact attract-mode timing;
- 60-second self-playing demo;
- user idle warning;
- generation/validation;
- rooms/corridors/doors/stairs;
- three palettes;
- perspective renderer;
- movement/turn/door/stair animation;
- game README;
- root README and AGENTS current-game updates;
- smoke/artifact integration.

Build, run, debug, validate, commit, and push.

## Commit 3 — validation hardening, only if needed

Use a third commit only for genuine fixes discovered during full manual validation.

Suggested subject:

```text
test(dungeon-star): harden attract mode and backend validation
```

Do not create an empty or cosmetic third commit merely to follow this outline.

---

# 25. Definition of Done

The work is complete only when every item is true.

## Language/runtime

- [ ] `FILL QUADRILATERAL` is official, parsed, analyzed, emitted, linked, and documented.
- [ ] `DRAW QUADRILATERAL` is official, parsed, analyzed, emitted, linked, and documented.
- [ ] DirectX draws both forms correctly.
- [ ] GDI draws both forms correctly.
- [ ] No graphics resource leak is observed.
- [ ] `KEY_OTHER = 19` is official and documented.
- [ ] Unnamed ordinary keys produce `KEY_OTHER`.
- [ ] Existing key values and behaviors remain unchanged.
- [ ] `KEY_HELD(KEY_OTHER)` is false.
- [ ] Shared tests and native graphics tests pass.
- [ ] No duplicate parser or Visual Studio keyword list was added.

## Game

- [ ] Folder is exactly `games\DungeonStarI`.
- [ ] Project display/window name is exactly `Dungeon Star I`.
- [ ] Output is `DungeonStarI.exe`.
- [ ] Game logic is entirely in `Program.smile`.
- [ ] There are three connected floors.
- [ ] Floors use green, blue, and red themes.
- [ ] Rooms, corridors, and doors are randomized.
- [ ] Stairs work down and up.
- [ ] Every accepted map is validated.
- [ ] Generation is bounded and has a fallback.
- [ ] Perspective rendering shows corridors, rooms, doors, and stairs clearly.
- [ ] Movement and turns animate smoothly.
- [ ] No combat/items/stats/score were added.

## Attract/demo mode

- [ ] Title start message flashes.
- [ ] Initial idle delay is 15 seconds.
- [ ] Countdown visibly shows 5 through 0.
- [ ] Demo begins after the countdown.
- [ ] Demo genuinely self-plays.
- [ ] Demo lasts 60 seconds.
- [ ] Demo naturally uses normal movement/actions.
- [ ] Any ordinary key during demo returns to title.
- [ ] Cancel key is consumed and does not immediately start user play.
- [ ] Natural demo completion returns to flashing title.

## User idle

- [ ] No warning before 30 seconds.
- [ ] Warning flashes from 9 through 0.
- [ ] Return to title occurs at 40 seconds total idle time.
- [ ] Any key cancels/reset the idle countdown before normal handling.
- [ ] Demo mode is exempt.

## Integration

- [ ] Root/current docs describe five games.
- [ ] Historical milestone records were not rewritten inaccurately.
- [ ] Smoke test compiles Dungeon Star I.
- [ ] Artifact verifier checks Dungeon Star I.
- [ ] `cmd /c scripts\smoke-test.cmd` passes.
- [ ] DirectX manual validation passes.
- [ ] GDI manual validation passes.
- [ ] Alt+Enter, resize, DPI, close, and focus behavior remain correct.
- [ ] Updated VSIX is built and verified.
- [ ] Every validated commit is pushed.

---

# 26. Required final Codex report

After pushing, report:

1. Starting commit.
2. Final commit hash or hashes.
3. Branch pushed.
4. Exact files added, changed, moved, or deleted.
5. Exact language syntax added.
6. `KEY_OTHER` behavior and value.
7. Native C ABI additions.
8. DirectX implementation summary.
9. GDI implementation summary.
10. Dungeon generation algorithm.
11. Renderer/animation design.
12. Exact title/demo/idle behavior implemented.
13. Generated executable paths.
14. VSIX path.
15. Exact automated command results.
16. Manual DirectX results.
17. Manual GDI results.
18. Timing validation results for:
    - 15-second delay;
    - 5-to-0 countdown;
    - 60-second demo;
    - 30-second idle warning;
    - 9-to-0 warning;
    - 40-second title return.
19. Resource/performance observations.
20. Known limitations, or `None identified.`

Do not report the task complete if the game was only compiled but not manually exercised.
