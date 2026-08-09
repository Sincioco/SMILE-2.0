# Dungeon Star I Map Authoring Guide

Dungeon Star I can load a complete three-floor dungeon from a plain `.map` text file.

The game includes:

```text
Maps\default.map
Maps\sample-loops.map
Maps\sample-switchbacks.map
```

`default.map` is selected when the title screen first opens. Edit it, rebuild or copy it beside the executable, and start the default map to see your changes.

If a selected map is missing or invalid, Dungeon Star I safely generates a random dungeon instead.

# 1. Required file shape

A complete map file has exactly three floors.

Each floor must have:

```text
31 rows
31 symbols on every row
```

Use these headers:

```text
[FLOOR 1]
[FLOOR 2]
[FLOOR 3]
```

Headers must appear once and in that order.

Blank lines are allowed.

A line beginning with a semicolon is a comment:

```text
; This is a comment.
```

Comments and blank lines do not count as map rows.

# 2. Map symbols

| Symbol | Meaning |
|---|---|
| `#` | Solid wall |
| `.` | Walkable corridor |
| `D` | Closed door |
| `O` | Door that begins open |
| `N` | Player starts here facing north |
| `E` | Player starts here facing east |
| `S` | Player starts here facing south |
| `W` | Player starts here facing west |
| `U` | Stairs going up |
| `V` | Stairs going down |

`V` means down because `D` is already used for a closed door.

Only one of `N`, `E`, `S`, or `W` may appear in the complete three-floor file.

# 3. Stair requirements

Use this exact structure:

```text
Floor 1: one V, no U
Floor 2: one U and one V
Floor 3: one U, no V
```

The game links them automatically:

```text
Floor 1 V <-> Floor 2 U
Floor 2 V <-> Floor 3 U
```

Do not place a door and stair in the same cell.

# 4. The outside border

Every symbol on the outside edge must be `#`.

Correct:

```text
###############################
#.............................#
#.............................#
###############################
```

Incorrect:

```text
.##############################
```

An open outside edge would let the player leave the map, so the validator rejects it.

# 5. Build corridors, not rooms

Dungeon Star I is designed to feel like a pipe or tube.

Recommended:

```text
###############################
#####...........###############
###############.###############
###############.###############
###############...........#####
###############################
```

Avoid large open areas:

```text
###############################
#####.................#########
#####.................#########
#####.................#########
###############################
```

The game rejects any 2-by-2 square in which all four cells are walkable.

That rule keeps the dungeon one cell wide.

# 6. Long passages

Leave at least 5–10 walking cells between:

- turns;
- intersections;
- doors;
- stairs;
- the player’s starting point.

Long passages make the pseudo-3D movement feel like traveling through a tunnel.

A useful pattern is to place decision points ten cells apart.

# 7. Starting position

Place the start inside a straight passage.

For a south-facing start:

```text
#######.#######
#######.#######
#######S#######
#######.#######
#######.#######
```

The cells ahead and behind are open. The immediate left and right cells are walls.

Avoid starting at an intersection:

```text
#######.#######
#######.#######
#####..S..#####
#######.#######
#######.#######
```

The validator rejects starts that do not feel enclosed.

# 8. Doors

A door belongs in the middle of a straight corridor.

Horizontal door:

```text
#####.....D.....#####
```

Vertical door:

```text
#######.#######
#######.#######
#######D#######
#######.#######
#######.#######
```

A valid door has:

- walkable cells on two opposite sides;
- walls on the other two sides;
- no nearby turn/intersection;
- no neighboring door;
- no stair or start in the same cell.

Try to place a door 5–10 steps from the nearest turn or intersection.

# 9. Turns and intersections

Left or right turn:

```text
###########.###
###########.###
#####.......###
###############
```

T-junction:

```text
###########.###########
#####.............#####
###########.###########
```

Four-way intersection:

```text
###########.###########
#####.............#####
###########.###########
```

The visible options depend on the direction from which the player arrives.

Keep intersection centers one cell wide.

# 10. Small complete example

This example is intentionally smaller than a real floor. A real floor must still be 31 by 31.

```text
; Example only — not a complete 31 x 31 map
[FLOOR 1]
#############
#####.....###
#####.###.###
#####S###.###
#####.###.###
#####.....V##
#############
```

# 11. Complete file outline

```text
; My Dungeon Star I map

[FLOOR 1]
; 31 rows of exactly 31 symbols
###############################
...
###############################

[FLOOR 2]
; 31 rows of exactly 31 symbols
###############################
...
###############################

[FLOOR 3]
; 31 rows of exactly 31 symbols
###############################
...
###############################
```

Do not type the literal `...` in a real map. It is only shorthand in this guide.

# 12. Title-screen selection

The title menu offers:

```text
DEFAULT.MAP
SAMPLE-LOOPS.MAP
SAMPLE-SWITCHBACKS.MAP
RANDOM DUNGEON
```

Use Up/Down or W/S to select and Enter/Space to start.

Editing `default.map` is the easiest student workflow because it is selected first.

# 13. Validation checklist

Before running your map, check:

- [ ] Three floor headers exist in order.
- [ ] Each floor has 31 rows.
- [ ] Every row has 31 symbols.
- [ ] The outside border is all walls.
- [ ] Only documented symbols are used.
- [ ] There is exactly one player start.
- [ ] The start is inside a straight corridor.
- [ ] Floor 1 has one down stair.
- [ ] Floor 2 has one up and one down stair.
- [ ] Floor 3 has one up stair.
- [ ] Every corridor is connected.
- [ ] There are no 2-by-2 open areas.
- [ ] Every door lies in a straight corridor.
- [ ] Doors are several steps from turns/intersections.
- [ ] Stairs and doors are reachable.

When the game rejects a map, compare it with one of the supplied sample maps and make one change at a time.
