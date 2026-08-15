# Compatible Map Format, Title Loading, and Student Map Guide

Commit the supplied student guide as:

```text
games\DungeonStarII\MAP_AUTHORING.md
```

Commit the supplied starting maps as:

```text
games\DungeonStarII\Maps\default.map
games\DungeonStarII\Maps\custom.map
```

---

# 1. Compatibility objective

Dungeon Star II uses a compatible extension of Dungeon Star I’s text-map foundation.

Shared concepts:

```text
UTF-8 text
semicolon comments
[FLOOR 1] header
31 x 31 grid
# wall
. floor
D closed door
O open door
N/E/S/W player start
```

A student may copy a Dungeon Star I map to:

```text
games\DungeonStarII\Maps\custom.map
```

Dungeon Star II will load its first floor.

The result will initially look tube-like because the source map is tube-like. The student can then remove wall cells and create wider rooms.

---

# 2. Important difference from Dungeon Star I

Dungeon Star I deliberately rejects open 2-by-2 floor areas.

Dungeon Star II deliberately permits them.

Dungeon Star II maps may contain:

- open rooms;
- broad halls;
- multiple-cell intersections;
- loops;
- pillars;
- wall islands.

Do not apply Dungeon Star I’s one-cell-wide topology or long-door-spacing validator to Dungeon Star II.

---

# 3. Accepted file organization

Required first-floor header:

```text
[FLOOR 1]
```

Then exactly:

```text
31 rows
31 symbols per row
```

Blank lines are allowed.

Lines beginning with:

```text
;
```

are comments.

For partial compatibility with complete Dungeon Star I files:

- parse only `[FLOOR 1]`;
- when `[FLOOR 2]` is encountered after 31 valid floor-one rows, stop or ignore the rest;
- `[FLOOR 3]` is also ignored;
- do not require Dungeon Star I stair counts.

A Dungeon Star II file may contain only `[FLOOR 1]`.

---

# 4. Symbols

| Symbol | Meaning |
|---|---|
| `#` | Default wall type 1 |
| `1` | Wall type 1 |
| `2` | Wall type 2 |
| `3` | Wall type 3 |
| `4` | Wall type 4 |
| `5` | Wall type 5 |
| `6` | Wall type 6 |
| `.` | Walkable floor |
| `D` | Closed door |
| `O` | Open door/walkable doorway |
| `N` | Start facing north |
| `E` | Start facing east |
| `S` | Start facing south |
| `W` | Start facing west |
| `U` | Compatibility marker treated as walkable floor |
| `V` | Compatibility marker treated as walkable floor |

There must be exactly one start marker in floor 1.

`U` and `V` do not change floors in Stage 2.

They are accepted only so a Dungeon Star I floor can be imported without immediately failing.

---

# 5. Internal tile conversion

Recommended:

```text
# or 1  -> wall type 1
2       -> wall type 2
3       -> wall type 3
4       -> wall type 4
5       -> wall type 5
6       -> wall type 6
.       -> floor
O       -> floor
U/V     -> floor
N/E/S/W -> floor plus player start/direction
D       -> closed door
```

Do not preserve text bytes after parsing unless useful for a clear diagnostic.

---

# 6. Static validation

A valid loaded map must have:

- ordered `[FLOOR 1]`;
- exactly 31 rows;
- exactly 31 symbols per row;
- legal symbols only;
- solid outer border using `#` or `1`–`6`;
- exactly one start;
- start on a walkable tile;
- all floor/open-door/closed-door cells reachable from the start when closed doors are treated as traversable;
- every closed door in a valid opening;
- no door on the outside border;
- no out-of-range map access.

Do **not** reject:

- 2-by-2 floor;
- large rooms;
- large open regions;
- close turns;
- doors near rooms.

Those are normal in this game.

---

# 7. Door validation

A closed door must have one valid orientation:

Horizontal travel:

```text
wall above
floor - D - floor
wall below
```

Vertical travel:

```text
        floor
wall    D    wall
        floor
```

Exactly one orientation should be valid.

The game may infer animation orientation from this structure if needed.

---

# 8. Title loading

Title entries:

```text
Default.MAP
CUSTOM.MAP
Random MAP
```

Literal loaders:

```smile
Load Text File "Maps\default.map" Into MapFileBytes Count MapFileLength

Load Text File "Maps\custom.map" Into MapFileBytes Count MapFileLength
```

A title choice resets the five-second inactivity timer.

Starting the selected source always reloads the file.

This lets a student:

1. return to title;
2. edit `custom.map`;
3. rebuild or copy the map beside the executable;
4. select `CUSTOM.MAP`;
5. see the new layout.

No native file picker or mutable path string is required.

---

# 9. Fallback behavior

When `default.map` or `custom.map` is:

- missing;
- empty;
- truncated;
- malformed;
- disconnected;
- missing a start;
- open at the outside border;
- otherwise invalid;

the game:

1. discards the partial map;
2. generates a random rooms-and-corridors map;
3. validates it;
4. starts normally;
5. briefly displays:

```text
MAP MISSING Or INVALID
Random MAP USED
```

If the entire `Maps` directory is deleted, user and demo play remain available.

---

# 10. Supplied default map

The supplied `default.map` demonstrates:

- multiple large rooms;
- narrow corridors;
- several wall materials;
- several closed doors;
- one initially open door;
- interior pillars;
- loops;
- a valid east-facing start.

Do not replace it with a tube maze.

---

# 11. Supplied custom map

The supplied `custom.map` is a simpler safe template.

It demonstrates:

- one large room complex;
- internal dividing walls;
- doors;
- pillars;
- an editable start.

The student is expected to replace or modify it.

---

# 12. Student guide requirements

`MAP_AUTHORING.md` must explain:

```text
exact dimensions
headers and comments
symbol table
wall material colors
start directions
door orientation
open-room difference from Dungeon Star I
how to copy a Dungeon Star I map
how to load custom.map from title
fallback behavior
validation checklist
common mistakes
```

Use text diagrams.

Do not require external software beyond a text editor.

---

# 13. Map validator script

Add:

```text
scripts\validate-raycasting-maps.ps1
```

It should validate the committed:

```text
default.map
custom.map
```

Checks:

- header;
- row count and width;
- legal symbols;
- outer border;
- one start;
- connectivity;
- door orientation.

It must allow open 2-by-2 areas.

Add it to the smoke suite.

Keep the script focused and fast.

---

# 14. Artifact copying

`DungeonStarII.smileproj` and the loose-file smoke build must copy:

```text
Maps\default.map
Maps\custom.map
```

Verify byte equality between source and output copies.

Also verify the executable is native x64 Windows GUI output with no CLR header.
