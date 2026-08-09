# Dungeon Star I Adjustments
## Student Maps, Pipe-Like Generation, Blue Title, and Five-Second Demo

**Baseline:** Dungeon Star I already exists at `games\DungeonStarI` and currently uses a three-floor, room-and-corridor generator, green title art, a 15-second title delay plus countdown, and a 60-second demo.

Implement this milestone without replacing the working pseudo-3D renderer, movement animations, doors, stairs, music lifecycle, DirectX/GDI support, or shared focus-muting runtime.

## 1. Required repository files

Add:

```text
games\DungeonStarI\MAP_AUTHORING.md
games\DungeonStarI\Maps\default.map
games\DungeonStarI\Maps\sample-loops.map
games\DungeonStarI\Maps\sample-switchbacks.map
```

The approved initial content is supplied in this ZIP under:

```text
Repository-Files\games\DungeonStarI\
```

Update `DungeonStarI.smileproj` to copy:

```xml
<Asset Include="Maps\**\*" />
```

Keep the existing music asset copying.

## 2. Generic SMILE language addition

Add a small general-purpose BASIC-style text-file loading statement:

```smile
LOAD TEXT FILE "Maps\default.map" INTO MapBytes COUNT MapByteCount
```

### Meaning

- The path is a non-empty text literal.
- `MapBytes` must be a one-dimensional numeric array.
- `MapByteCount` must be a scalar numeric variable.
- The runtime resolves a relative path from the generated executable’s directory.
- The target array is zero-filled first.
- The file is read as UTF-8 bytes.
- A UTF-8 BOM, when present, is skipped.
- Each byte is stored as a zero-through-255 numeric array element.
- At most the array’s capacity is copied.
- `COUNT` receives the number of bytes copied.
- Missing, inaccessible, empty, or unreadable files return count zero and never crash.
- Reading a file larger than the destination array safely truncates to capacity.
- No text-file saving is added in this milestone.

The statement is generic and may be used by console or game programs. It is not a dungeon parser.

### Parser compatibility

The existing persistence form remains unchanged:

```smile
LOAD HighScore FROM "HighScore" DEFAULT 0
```

Disambiguate:

```text
LOAD TEXT FILE ...     generic file input
LOAD Identifier FROM   integer persistence
```

### Shared implementation

Update proportionally:

```text
src\Smile.Language\Syntax.cs
src\Smile.Language\GameSyntax.cs or a suitable shared syntax file
src\Smile.Language\Parser.cs
src\Smile.Language\Semantics.cs
src\Smile.Compiler\MasmEmitter.cs
src\Smile.NativeRuntime\runtime.c
src\Smile.Tests\Program.cs
docs\language\README.md
README.md
```

New keywords are:

```text
FILE
INTO
COUNT
```

`TEXT` and `LOAD` already exist.

Recommended native ABI:

```c
long long smile_load_text_file(
    const char* path,
    long long path_length,
    long long* destination,
    long long capacity);
```

The MASM emitter stores the return value into the `COUNT` variable.

Reuse the existing executable-relative asset-path resolver rather than creating another path implementation.

## 3. Map file contract

A map file contains exactly three floors.

Each floor contains exactly:

```text
31 rows
31 symbols per row
```

Allowed structural lines:

```text
; comment
[FLOOR 1]
[FLOOR 2]
[FLOOR 3]
```

Blank lines and lines beginning with `;` are ignored.

Allowed map symbols:

```text
#  solid wall
.  walkable corridor
D  closed door
O  already-open door
N  player start, facing north
E  player start, facing east
S  player start, facing south
W  player start, facing west
U  stairs up
V  stairs down
```

`V` means down because `D` is reserved for a closed door.

Exactly one player-start symbol is required in the complete file, normally on floor 1.

Required stair structure:

```text
Floor 1: one V, no U
Floor 2: one U and one V
Floor 3: one U, no V
```

Floor links are:

```text
Floor 1 V <-> Floor 2 U
Floor 2 V <-> Floor 3 U
```

## 4. SMILE-side map parser

Keep map interpretation in `Program.smile`.

Add a sufficiently large numeric byte array, for example:

```smile
CONST MapFileCapacity = 8192
DIM MapFileBytes[MapFileCapacity]
MapFileLength = 0
```

Create literal-path loader routines:

```smile
SUB LoadDefaultMap()
    LOAD TEXT FILE "Maps\default.map" INTO MapFileBytes COUNT MapFileLength
    CALL ParseLoadedMap()
END SUB
```

Do the same for the two other supplied files.

The parser must:

- ignore carriage return, line feed, blank lines, comments, and recognized floor headers;
- reject unknown symbols;
- reject wrong row length;
- reject wrong row count;
- reject missing/duplicate/out-of-order floor headers;
- reject multiple or missing starts;
- populate `Dungeon`, start direction, and stair coordinates;
- run the normal runtime map validator;
- return a success/failure flag;
- never read beyond `MapFileLength` or array capacity.

Do not parse the map in native C/C++.

## 5. Title-screen map selection

Change the Dungeon Star I title screen into a simple menu.

Initial selection:

```text
DEFAULT.MAP
```

Menu entries:

```text
DEFAULT.MAP
SAMPLE-LOOPS.MAP
SAMPLE-SWITCHBACKS.MAP
RANDOM DUNGEON
```

Controls:

```text
Up / W       previous entry
Down / S     next entry
Enter/Space  start selected entry
Escape       exit
Alt+Enter    existing full-screen behavior
```

Any title-menu activity resets the five-second inactivity timer.

When a selected file is missing or invalid:

1. Do not crash.
2. Generate a random dungeon.
3. Display a brief message such as:

```text
MAP NOT AVAILABLE - RANDOM DUNGEON USED
```

The selected file remains available for the student to fix and retry.

The demo uses the currently selected entry. Since the title initially selects `DEFAULT.MAP`, the normal automatic demo loads `default.map`.

## 6. Default-map behavior

User starts and demo starts should call one routine such as:

```smile
SUB BuildSelectedDungeon()
```

Behavior:

```text
Selected map loads and validates -> use it
Selected file missing/invalid    -> generate random pipe dungeon
Random menu entry                -> generate random pipe dungeon
```

If all three `.map` files are deleted, the game remains fully playable through internal random generation.

Do not silently embed `default.map` into the executable as a second hidden copy. The purpose is for students to edit the external file and immediately see the result.

## 7. Pipe/tube dungeon generation

Replace the current open-room generator. Preserve the three-floor map size unless a failing implementation requires a documented change.

The random dungeon must feel like a connected pipe or tube:

- one-cell-wide corridors;
- solid walls immediately to the corridor’s sides;
- no rectangular rooms;
- no 2-by-2 group of walkable cells;
- long straight runs;
- turns and intersections spaced apart;
- no large open visual spaces.

### Generation model

Use a coarse connected graph over logical junction nodes.

A practical 31-by-31 design uses junction coordinates around:

```text
5, 15, and 25
```

with optional small safe jitter that keeps adjacent junctions at least 8 cells apart.

Generation sequence:

1. Fill the floor with walls.
2. Choose a connected set of coarse horizontal/vertical edges.
3. Ensure a loop backbone so the map is not a single dead-end tree.
4. Carve each edge as a one-cell-wide corridor.
5. Add a few extra graph edges for T-junctions and four-way intersections.
6. Remove or reconnect excessive dead ends.
7. Place stairs at separated connected locations.
8. Place doors only on long straight edge interiors.
9. Choose a start inside a straight segment.
10. Validate; retry a small bounded number of times.
11. Use a deterministic pipe-style fallback on failure.

### Corridor length

Between a turn/intersection and the next turn/intersection, target:

```text
5 to 10 or more walking paces
```

The common sample spacing of ten cells is acceptable.

Do not place adjacent turns that make the player feel they are walking through open rooms.

### Junction appearance

The player may encounter:

```text
straight corridor
left turn
right turn
left/right T-junction
left/right/straight four-way intersection
```

Walking backward and turning around remain possible.

### Start placement

The player begins inside the dungeon, not in an open room.

At the start tile:

- forward and backward corridor cells are open;
- immediate left and right cells are walls;
- the first rendered frame already has left and right walls;
- place the start at least several steps from an intersection or door.

### Door placement

Every generated door must:

- lie on a straight, one-cell-wide corridor;
- have walkable cells on opposite sides;
- have walls on its perpendicular sides;
- be approximately 5–10 paces from the nearest turn/intersection;
- not overlap a start or stair;
- not be immediately adjacent to another door.

The supplied maps intentionally place doors near the midpoint of ten-cell segments.

## 8. Expanded validation

For loaded and random maps verify:

- exact dimensions for loaded maps;
- legal symbols only;
- closed outer border;
- one-cell-wide topology;
- no 2-by-2 walkable block;
- complete connectivity;
- valid start and facing;
- immediate side walls at start;
- reciprocal floor stairs;
- reachable stairs;
- correctly oriented doors;
- door-to-junction spacing;
- no door/start/stair overlap.

Use a bounded retry count. Do not test hundreds of seeds by default.

## 9. Blue title palette

Change the title’s tunnel art from green to the existing Floor 2 blue/sapphire family.

Suggested title colors:

```text
Background       RGB(3, 8, 20)
Near wall        RGB(28, 80, 150)
Middle wall      RGB(20, 58, 112)
Far wall         RGB(12, 36, 72)
Line/highlight   RGB(135, 190, 255)
Secondary text   LIGHT_BLUE / CYAN
```

The title should remain original and readable.

This title-palette change does not force floor 1 gameplay to become blue. Loaded map gameplay retains floor-specific green, blue, and red palettes.

## 10. Five-second attract delay

Remove the existing 15-second delay and 5-through-0 title countdown.

Use:

```smile
CONST TitleDemoDelay = 5000
```

After five seconds with no title-menu activity:

```text
start demo immediately
```

No additional countdown is required.

The title still flashes an appropriate start/select prompt.

Demo duration may remain 60 seconds for Dungeon Star I unless the shared arcade contract is deliberately applied consistently. Do not make it longer.

Any ordinary key during demo returns to title and is consumed, preserving the current behavior.

## 11. Audio focus

Do not add focus-muting code to `Program.smile`.

Dungeon Star I should continue using normal:

```smile
PLAY MUSIC ...
STOP MUSIC
PLAY SOUND ...
```

The shared runtime automatically silences all audio when inactive or minimized.

## 12. Documentation

Update:

```text
games\DungeonStarI\README.md
games\DungeonStarI\MAP_AUTHORING.md
README.md
docs\language\README.md
docs\architecture\README.md
```

Explain:

- external map selection;
- missing-file random fallback;
- generic `LOAD TEXT FILE`;
- pipe-style random generation;
- five-second attract mode;
- title blue palette.

## 13. Fast validation

Automated:

```text
cmd /c scripts\smoke-test.cmd
```

Add only focused checks for:

- valid `LOAD TEXT FILE` syntax;
- invalid target array/rank diagnostics;
- missing file returns zero;
- a tiny known text file loads expected bytes;
- map files are copied unchanged;
- the three supplied maps pass a lightweight script validator.

Manual happy path:

1. Launch Dungeon Star I.
2. Confirm blue title.
3. Start `default.map`.
4. Walk a straight corridor, turn, open a door, and use one stair.
5. Return to title and select one other sample.
6. Temporarily rename `default.map`; confirm random fallback.
7. Leave title idle for five seconds; confirm demo starts.
8. Press a key; confirm return to title.

One short DirectX run is sufficient unless the file/runtime code touches rendering. Run GDI briefly only if visual behavior changed in a backend-specific way.

No long soak or large seed sweep.

## 14. Suggested commits

Generic file input:

```text
feat(io): add bounded text-file loading for numeric arrays
```

Dungeon adjustment:

```text
feat(dungeon-star): add editable maps and pipe-style generation
```
